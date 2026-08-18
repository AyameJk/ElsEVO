using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ElsEvo
{
    /// <summary>Estados equivalentes ao enum BackgroundFilePatcher.States original.</summary>
    public enum EstadoPatch
    {
        PreparandoArquivos,
        AguardandoElswordAbrir,
        FazendoBackup,
        Aplicando,
        AguardandoElswordFechar,
        RestaurandoBackup,
        Concluido
    }

    /// <summary>
    /// Fluxo do patch, com duas garantias importantes que faltavam:
    ///   1) Um arquivo falhar ao mover (em uso, bloqueado por antivírus, etc.) NÃO trava
    ///      os outros — cada arquivo tenta sozinho, com algumas tentativas rápidas.
    ///   2) A RESTAURAÇÃO DO BACKUP SEMPRE roda no final (try/finally), mesmo que algo
    ///      dê errado no meio do caminho — nunca fica arquivo original preso na pasta
    ///      de backup por causa de um erro em outro arquivo.
    /// </summary>
    public static class PatcherService
    {
        public static async Task ExecutarFluxoPatchAsync(
            List<PatchInfo> listaPatches,
            IProgress<int>? progresso = null,
            IProgress<EstadoPatch>? statusProgresso = null,
            CancellationToken cancelamento = default)
        {
            void Reportar(EstadoPatch estado, int percentual)
            {
                statusProgresso?.Report(estado);
                progresso?.Report(percentual);
            }

            int CalcularPercentual(int indiceAtual, int total) => total == 0 ? 100 : indiceAtual * 100 / total;

            // 1) Copia cada mod pra pasta de cache (staging)
            Reportar(EstadoPatch.PreparandoArquivos, 0);
            for (int i = 0; i < listaPatches.Count; i++)
            {
                cancelamento.ThrowIfCancellationRequested();
                var patch = listaPatches[i];

                if (File.Exists(patch.ArquivoModificado) && !ArquivosIguais(patch.ArquivoModificado, patch.ArquivoTemporario))
                    TentarComRetentativa(() => File.Copy(patch.ArquivoModificado, patch.ArquivoTemporario, overwrite: true));

                Reportar(EstadoPatch.PreparandoArquivos, CalcularPercentual(i, listaPatches.Count));
            }

            // 2) Logs client-side
            if (Properties.Settings.Default.BlockLogs)
                Paths.Elsword.BlockLogs();
            else
                Paths.Elsword.UnblockLogs();

            // 3) Abre o launcher e ESPERA ELE FECHAR
            var cfg = Properties.Settings.Default;
            Process? processoLauncher = null;

            if (!(cfg.SkipLauncher && !string.IsNullOrWhiteSpace(cfg.X2Args)))
            {
                if (!cfg.WebLoginNeeded)
                {
                    processoLauncher = Paths.Elsword.RunLauncher();
                    Reportar(EstadoPatch.AguardandoElswordAbrir, 0);

                    if (processoLauncher != null)
                        await Task.Run(() => processoLauncher.WaitForExit(), cancelamento);
                }
            }

            // A partir daqui, garantimos que a restauração sempre roda no final,
            // não importa o que aconteça (erro de arquivo, cancelamento, exceção qualquer).
            try
            {
                // 4+5) Backup + aplica, arquivo por arquivo (junto, não em duas passadas
                //      separadas) — reduz a janela de tempo em que o jogo pode ler um
                //      arquivo pela metade do processo.
                Reportar(EstadoPatch.FazendoBackup, 0);
                for (int i = 0; i < listaPatches.Count; i++)
                {
                    var patch = listaPatches[i];

                    if (patch.ArquivoBackup != null && File.Exists(patch.ArquivoDestino))
                        TentarComRetentativa(() => MoverSubstituindo(patch.ArquivoDestino, patch.ArquivoBackup));

                    if (File.Exists(patch.ArquivoTemporario))
                        TentarComRetentativa(() => MoverSubstituindo(patch.ArquivoTemporario, patch.ArquivoDestino));

                    Reportar(EstadoPatch.Aplicando, CalcularPercentual(i, listaPatches.Count));
                }

                LimparPasta(Paths.Main.Cache);

                // 6) Se ainda não tiver processo de jogo (SkipLauncher+X2Args), abre agora;
                //    senão, só aguarda o x2.exe aparecer.
                Process? processoJogo;
                if (cfg.SkipLauncher && !string.IsNullOrWhiteSpace(cfg.X2Args))
                {
                    processoJogo = Paths.Elsword.RunClient();
                }
                else
                {
                    processoJogo = Paths.Elsword.GetClientProcess();
                    int tentativas = 0;
                    // Até 2 minutos esperando o processo aparecer — tempo de sobra pro launcher
                    // fechar, mover os arquivos e o cliente (x2.exe OU x2_dx11.exe) abrir.
                    while (processoJogo == null && tentativas < 120)
                    {
                        cancelamento.ThrowIfCancellationRequested();
                        await Task.Delay(1000, cancelamento);
                        processoJogo = Paths.Elsword.GetClientProcess();
                        tentativas++;
                    }
                }

                // 7) Espera o jogo fechar
                Reportar(EstadoPatch.AguardandoElswordFechar, 100);
                if (processoJogo != null)
                    await Task.Run(() => processoJogo.WaitForExit(), cancelamento);
            }
            finally
            {
                // 8) Restaura tudo — SEMPRE roda, mesmo se algo acima deu errado.
                //    Cada arquivo tenta algumas vezes (o processo do jogo pode levar um
                //    instante pra soltar o handle do arquivo depois de fechar).
                Reportar(EstadoPatch.RestaurandoBackup, 0);
                for (int i = 0; i < listaPatches.Count; i++)
                {
                    var patch = listaPatches[i];

                    if (File.Exists(patch.ArquivoDestino))
                        TentarComRetentativa(() => MoverSubstituindo(patch.ArquivoDestino, patch.ArquivoTemporario));

                    if (patch.ArquivoBackup != null && File.Exists(patch.ArquivoBackup))
                        TentarComRetentativa(() => MoverSubstituindo(patch.ArquivoBackup, patch.ArquivoDestino));

                    Reportar(EstadoPatch.RestaurandoBackup, CalcularPercentual(i, listaPatches.Count));
                }

                ExcluirPastaSeExistir(Paths.Elsword.Media);
                ExcluirPastaSeExistir(Paths.Elsword.Backup);

                LimparRegistroDoElsword();

                Reportar(EstadoPatch.Concluido, 100);
            }
        }

        /// <summary>
        /// Tenta a operação até 5 vezes com uma pausa curta entre elas (o arquivo pode estar
        /// momentaneamente em uso — pelo antivírus, pelo próprio Windows, ou pelo jogo ainda
        /// soltando o handle). Se todas as tentativas falharem, desiste desse arquivo
        /// específico SEM travar o resto do processo (só ignora e segue pro próximo).
        /// </summary>
        private static void TentarComRetentativa(Action acao, int tentativas = 5, int esperaMs = 150)
        {
            for (int i = 0; i < tentativas; i++)
            {
                try
                {
                    acao();
                    return;
                }
                catch (IOException) when (i < tentativas - 1)
                {
                    Thread.Sleep(esperaMs);
                }
                catch (UnauthorizedAccessException) when (i < tentativas - 1)
                {
                    Thread.Sleep(esperaMs);
                }
                catch
                {
                    return;
                }
            }
        }

        private static void LimparRegistroDoElsword()
        {
            try { LimparSubchave(@"Software\ElswordINT"); } catch { }
            try { LimparSubchave(@"Software\Nexon\Elsword\PatcherOption"); } catch { }
        }

        private static void LimparSubchave(string caminhoChave)
        {
            using var chave = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(caminhoChave, writable: true);
            if (chave == null)
                return;

            if (chave.Name.EndsWith(@"\MARK_INVALID", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var nomeValor in chave.GetValueNames())
                    chave.DeleteValue(nomeValor, throwOnMissingValue: false);
            }

            foreach (var subchave in chave.GetSubKeyNames())
                LimparSubchave(caminhoChave + "\\" + subchave);
        }

        private static bool ArquivosIguais(string caminhoA, string caminhoB)
        {
            if (!File.Exists(caminhoA) || !File.Exists(caminhoB))
                return false;

            return new FileInfo(caminhoA).Length == new FileInfo(caminhoB).Length;
        }

        private static void MoverSubstituindo(string origem, string destino)
        {
            string? pastaDestino = Path.GetDirectoryName(destino);
            if (!string.IsNullOrEmpty(pastaDestino) && !Directory.Exists(pastaDestino))
                Directory.CreateDirectory(pastaDestino);

            if (File.Exists(destino))
                File.Delete(destino);

            File.Move(origem, destino);
        }

        private static void LimparPasta(string caminho)
        {
            if (!Directory.Exists(caminho))
                return;

            foreach (var arquivo in Directory.GetFiles(caminho))
            {
                try { File.Delete(arquivo); }
                catch { }
            }
        }

        private static void ExcluirPastaSeExistir(string caminho)
        {
            try
            {
                if (!string.IsNullOrEmpty(caminho) && Directory.Exists(caminho))
                    Directory.Delete(caminho, recursive: true);
            }
            catch { }
        }
    }
}
