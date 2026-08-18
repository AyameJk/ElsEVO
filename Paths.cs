using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ElsEvo
{
    /// <summary>
    /// Réplica fiel do Paths.cs real do gPatcher (decompilado). Detalhes importantes
    /// preservados de propósito:
    ///   - O executável do CLIENTE é "x2.exe" dentro da pasta "data" (não "elsword.exe"!).
    ///   - "elsword.exe" na raiz é o LAUNCHER.
    ///   - O cache não fica dentro da pasta do jogo: fica na RAIZ DO DISCO onde o jogo
    ///     está instalado (ex.: se o jogo está em D:\Jogos\Elsword, o cache é D:\gPatcher cache).
    ///   - BlockLogs "bloqueia" substituindo os arquivos de log por PASTAS somente-leitura
    ///     com o mesmo nome (truque pra impedir o jogo de recriar o arquivo).
    /// </summary>
    public static class Paths
    {
        public static class Elsword
        {
            public static string Root => Properties.Settings.Default.ElswordDirectory;

            public static string Data => CriarSeValido(Path.Combine(Root, "data"));

            /// <summary>Executável do CLIENTE (não é o elsword.exe!).</summary>
            public static string ClientExe => Path.Combine(Data, "x2.exe");

            /// <summary>Executável do LAUNCHER, na raiz da instalação.</summary>
            public static string LauncherExe => Path.Combine(Root, "elsword.exe");

            public static string Backup => CriarSeValido(Path.Combine(Root, "backup"));
            public static string Media => CriarSeValido(Path.Combine(Data, "media"));
            public static string Movie => CriarSeValido(Path.Combine(Data, "movie"));
            public static string Music => CriarSeValido(Path.Combine(Data, "music"));

            private static string[] ArquivosDeLog => new[]
            {
                Path.Combine(Data, "Crash_ScreenShot.jpg"),
                Path.Combine(Data, "log.htm")
            };

            public static bool IsValidElswordDir(string dir)
            {
                if (string.IsNullOrWhiteSpace(dir))
                    return false;

                string exe = Path.Combine(dir, "elsword.exe");
                string dataDir = Path.Combine(dir, "data");
                return File.Exists(exe) && Directory.Exists(dataDir);
            }

            /// <summary>"Bloqueia" os arquivos de log virando pasta oculta/somente-leitura no lugar deles.</summary>
            public static void BlockLogs()
            {
                foreach (string arquivo in ArquivosDeLog)
                {
                    if (Directory.Exists(arquivo))
                        continue; // já bloqueado

                    if (File.Exists(arquivo))
                        File.Delete(arquivo);

                    var pasta = Directory.CreateDirectory(arquivo);
                    pasta.Attributes = FileAttributes.ReadOnly | FileAttributes.Hidden
                        | FileAttributes.System | FileAttributes.Directory;
                }
            }

            /// <summary>Desfaz o BlockLogs, removendo as pastas-armadilha.</summary>
            public static void UnblockLogs()
            {
                foreach (string arquivo in ArquivosDeLog)
                {
                    if (!Directory.Exists(arquivo))
                        continue;

                    try
                    {
                        var pasta = new DirectoryInfo(arquivo) { Attributes = FileAttributes.Directory };
                        pasta.Delete(recursive: true);
                    }
                    catch { /* ignora se ainda estiver marcado somente-leitura */ }
                }
            }

            public static Process? RunClient()
            {
                if (!File.Exists(ClientExe))
                    return null;

                return Process.Start(new ProcessStartInfo
                {
                    FileName = ClientExe,
                    Arguments = " " + (Properties.Settings.Default.X2Args ?? string.Empty),
                    WorkingDirectory = Data,
                    UseShellExecute = true
                });
            }

            public static Process? RunLauncher()
            {
                if (!File.Exists(LauncherExe))
                    return null;

                return Process.Start(new ProcessStartInfo
                {
                    FileName = LauncherExe,
                    WorkingDirectory = Root,
                    UseShellExecute = true
                });
            }

            /// <summary>
            /// Procura o processo do cliente já em execução. Primeiro tenta os nomes
            /// conhecidos (x2 = DirectX 9, x2_dx11 = DirectX 11 — o launcher deixa escolher),
            /// que é uma checagem rápida e não esbarra em permissão de anti-cheat. Se não
            /// achar nenhum dos dois, cai pra busca genérica por qualquer processo rodando
            /// de dentro da pasta do jogo (cobre outras variações de nome).
            /// </summary>
            public static Process? GetClientProcess()
            {
                foreach (var nomeConhecido in new[] { "x2", "x2_dx11" })
                {
                    var processos = Process.GetProcessesByName(nomeConhecido);
                    if (processos.Length > 0)
                        return processos[0];
                }

                string raiz = Root;
                if (string.IsNullOrWhiteSpace(raiz))
                    return null;

                try
                {
                    foreach (var processo in Process.GetProcesses())
                    {
                        try
                        {
                            string? caminhoExe = processo.MainModule?.FileName;
                            if (string.IsNullOrEmpty(caminhoExe))
                                continue;

                            bool rodaDeDentroDaPasta = caminhoExe.StartsWith(raiz, StringComparison.OrdinalIgnoreCase);
                            bool naoEhOLauncher = !caminhoExe.Equals(LauncherExe, StringComparison.OrdinalIgnoreCase);

                            if (rodaDeDentroDaPasta && naoEhOLauncher)
                                return processo;
                        }
                        catch
                        {
                            // Processo protegido/sem permissão de acesso (comum com anti-cheat
                            // tipo XIGNCODE3 bloqueando introspecção) — ignora e continua.
                        }
                    }
                }
                catch { }

                return null;
            }

            private static string CriarSeValido(string caminho)
            {
                if (!IsValidElswordDir(Root))
                    return string.Empty;

                Directory.CreateDirectory(caminho);
                return caminho;
            }
        }

        public static class Main
        {
            /// <summary>Cache fica na RAIZ DO DISCO onde o jogo está instalado, não dentro da pasta do jogo.</summary>
            public static string Cache
            {
                get
                {
                    string raizDisco = Path.GetPathRoot(Elsword.Root) ?? AppDomain.CurrentDomain.BaseDirectory;
                    string caminho = Path.Combine(raizDisco, "gPatcher cache");
                    Directory.CreateDirectory(caminho);
                    return caminho;
                }
            }

            /// <summary>
            /// Pasta onde ficam os packs de mod importados. Fica DENTRO da própria instalação
            /// do Elsword (não mais ao lado do .exe do ElsEVO) — assim ela está garantidamente
            /// no MESMO disco que a pasta "data\" do jogo, e mover arquivos de lá pra cá na
            /// hora do patch é um MOVE rápido (mesmo volume), não uma cópia lenta entre discos.
            /// </summary>
            public static string Packs
            {
                get
                {
                    string raiz = Elsword.Root;
                    string caminho = !string.IsNullOrWhiteSpace(raiz) && Directory.Exists(raiz)
                        ? Path.Combine(raiz, "cacheElsEvo")
                        : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cacheElsEvo"); // fallback se o jogo ainda não foi configurado

                    Directory.CreateDirectory(caminho);
                    MigrarPacksAntigosSeNecessario(caminho);
                    return caminho;
                }
            }

            /// <summary>
            /// Versões anteriores do ElsEVO guardavam os packs ao lado do próprio .exe
            /// (pastas "packs\" ou "cacheElsEvo\" ali). Se isso existir e a pasta nova (dentro
            /// do jogo) ainda não tiver nada, move automaticamente pra não obrigar reimportar.
            /// </summary>
            private static void MigrarPacksAntigosSeNecessario(string pastaNova)
            {
                try
                {
                    string baseDoApp = AppDomain.CurrentDomain.BaseDirectory;

                    foreach (var nomeAntigo in new[] { "cacheElsEvo", "packs" })
                    {
                        string pastaAntiga = Path.Combine(baseDoApp, nomeAntigo);

                        // Nunca migra a própria pasta nova de/pra ela mesma (acontece se o jogo
                        // estiver instalado dentro da mesma pasta do ElsEVO, caso raro).
                        if (string.Equals(pastaAntiga, pastaNova, StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!Directory.Exists(pastaAntiga))
                            continue;

                        var subpastasAntigas = Directory.GetDirectories(pastaAntiga);
                        if (subpastasAntigas.Length == 0)
                            continue;

                        foreach (var pastaPack in subpastasAntigas)
                        {
                            string nomePack = Path.GetFileName(pastaPack);
                            string destino = Path.Combine(pastaNova, nomePack);

                            if (!Directory.Exists(destino))
                                Directory.Move(pastaPack, destino);
                        }

                        // Se a pasta antiga ficou vazia depois de mover tudo, remove ela.
                        if (Directory.Exists(pastaAntiga) && !Directory.EnumerateFileSystemEntries(pastaAntiga).Any())
                            Directory.Delete(pastaAntiga);
                    }
                }
                catch
                {
                    // Falhou a migração automática (permissão, arquivo em uso, etc.) — não é
                    // crítico, só significa que o usuário precisa reimportar manualmente.
                }
            }
        }

        /// <summary>Onde fica o "usrmods" (equivalente aos mods ativos) — AppData\Local\ElsEvo.</summary>
        public static string LocalApplicationData { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ElsEvo");

        public static string UserMods { get; } =
            Path.Combine(LocalApplicationData, "usrmods.json");
    }
}
