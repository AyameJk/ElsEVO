using System;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Controls;

namespace ElsEvo
{
    public partial class MainWindow : Window
    {
        private bool _modsLigados;
        private CancellationTokenSource? _cancelamentoAtual;
        private GerenciadorBandeja? _bandeja;

        // Bounds guardados antes de "maximizar" (nosso maximize falso, que respeita a barra de tarefas)
        private double _larguraAntesDeMaximizar;
        private double _alturaAntesDeMaximizar;
        private double _topoAntesDeMaximizar;
        private double _esquerdaAntesDeMaximizar;
        private bool _estaMaximizada;

        public MainWindow()
        {
            InitializeComponent();
            _modsLigados = Properties.Settings.Default.ModsEnabled;

            Idiomas.IdiomaMudou += AplicarIdioma;
            ThemeManager.TemaMudou += _ => AtualizarVisualToggle(); // reage na hora, mesmo com Configurações ainda aberta

            Loaded += (_, _) =>
            {
                ThemeManager.AplicarTemaSalvo(); // reforço: garante que o tema bateu, mesmo que o do App.xaml.cs tenha falhado por algum motivo
                AtualizarListaDeModsAtivos();
                AtualizarVisualToggle();
                AplicarIdioma();
                ConfigurarBandeja();
                BadgeBeta.Visibility = Visibility.Collapsed; // versão estável — badge BETA nunca aparece aqui

                // Checagem de atualização: roda em segundo plano, sem travar a abertura da
                // janela. Se achar uma versão nova, ela mesma cuida de perguntar ao usuário.
                _ = VerificarAtualizacaoAsync();
            };

            Closing += MainWindow_Closing;
        }

        private void ConfigurarBandeja()
        {
            string caminhoExe = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
            if (!string.IsNullOrEmpty(caminhoExe))
                _bandeja = new GerenciadorBandeja(this, caminhoExe);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Properties.Settings.Default.MinimizarParaBandeja && _bandeja != null)
            {
                e.Cancel = true;
                Hide();
                _bandeja.Mostrar();
            }
            else
            {
                _bandeja?.Dispose();
            }
        }

        // ===================== IDIOMA =====================

        private void AplicarIdioma()
        {
            MenuItemAcoes.Header = Idiomas.T("MenuAcoes");
            MenuItemConfiguracoes.Header = Idiomas.T("MenuConfiguracoes");
            MenuItemSobre.Header = Idiomas.T("MenuSobre");
            ItemReiniciar.Header = Idiomas.T("AcaoReiniciar");
            ItemLimparCache.Header = Idiomas.T("AcaoLimparCache");
            ItemLimparConfiguracoes.Header = Idiomas.T("AcaoLimparConfiguracoes");
            ItemExcluirMods.Header = Idiomas.T("AcaoExcluirMods");
            BtnGerenciarMods.Content = Idiomas.T("BtnGerenciarMods");
            TxtModsAtivos.Text = Idiomas.T("ModsAtivos");
            TxtListaVazia.Text = Idiomas.T("ListaVazia");
            StatusBadge.Text = _modsLigados ? Idiomas.T("Ligado") : Idiomas.T("Desligado");

            // Texto do botão principal também depende do idioma — reaplica aqui pra cobrir
            // o caso de trocar de idioma sem tocar no toggle.
            AtualizarTextoBotaoJogar();
        }

        // ===================== MODS ATIVOS (agrupado por pack) =====================

        private void AtualizarListaDeModsAtivos()
        {
            var ativos = GerenciadorDeMods.Carregar();

            ListaModsAtivos.Items.Clear();

            // Um item por PACK (não um item por arquivo) — mostra quantos arquivos vêm de cada pack.
            var porPack = ativos.GroupBy(m => m.NomeDoPack);

            foreach (var grupo in porPack)
            {
                int quantidade = grupo.Count();
                var item = new ListBoxItem
                {
                    Padding = new Thickness(6),
                    Content = quantidade == 1
                        ? grupo.Key
                        : $"{grupo.Key}  ({quantidade} arquivos)"
                };
                ListaModsAtivos.Items.Add(item);
            }

            bool temMods = ativos.Count > 0;
            ListaModsAtivos.Visibility = temMods ? Visibility.Visible : Visibility.Collapsed;
            TxtListaVazia.Visibility = temMods ? Visibility.Collapsed : Visibility.Visible;
        }

        // ===================== BARRA DE TÍTULO CUSTOM =====================

        private void BarraTitulo_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                BtnMaximizar_Click(sender, e);
                return;
            }

            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnMinimizar_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        /// <summary>
        /// "Maximiza" manualmente respeitando a área de trabalho (SystemParameters.WorkArea),
        /// porque com WindowStyle="None" o WindowState.Maximized nativo cobre a tela inteira
        /// (inclusive por cima da barra de tarefas).
        /// </summary>
        private void BtnMaximizar_Click(object sender, RoutedEventArgs e)
        {
            if (!_estaMaximizada)
            {
                _larguraAntesDeMaximizar = Width;
                _alturaAntesDeMaximizar = Height;
                _topoAntesDeMaximizar = Top;
                _esquerdaAntesDeMaximizar = Left;

                var areaUtil = SystemParameters.WorkArea;
                Left = areaUtil.Left;
                Top = areaUtil.Top;
                Width = areaUtil.Width;
                Height = areaUtil.Height;

                _estaMaximizada = true;
            }
            else
            {
                Width = _larguraAntesDeMaximizar;
                Height = _alturaAntesDeMaximizar;
                Top = _topoAntesDeMaximizar;
                Left = _esquerdaAntesDeMaximizar;

                _estaMaximizada = false;
            }
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            // Se o usuário restaurar via outro meio (ex.: barra de tarefas), garante estado normal.
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
        }

        private void BtnFechar_Click(object sender, RoutedEventArgs e) => Close();

        // ===================== TOGGLE LIGADO/DESLIGADO (ModsEnabled) =====================

        private void BtnToggleLigado_Click(object sender, RoutedEventArgs e)
        {
            _modsLigados = !_modsLigados;
            AtualizarVisualToggle();

            var cfg = Properties.Settings.Default;
            cfg.ModsEnabled = _modsLigados;
            cfg.Save();
        }

        private void AtualizarVisualToggle()
        {
            var bc = new BrushConverter();
            bool temaClaro = Properties.Settings.Default.TemaClaro;

            if (_modsLigados)
            {
                StatusBadge.Text = Idiomas.T("Ligado");
                if (temaClaro)
                {
                    StatusBadge.Foreground = Brushes.White;
                    BadgeBorder.Background = (Brush)bc.ConvertFrom("#2E7D32")!;
                    BadgeBorder.BorderThickness = new Thickness(0);
                }
                else
                {
                    StatusBadge.Foreground = (Brush)bc.ConvertFrom("#69E292")!;
                    BadgeBorder.Background = (Brush)bc.ConvertFrom("#263D30")!;
                    BadgeBorder.BorderBrush = (Brush)bc.ConvertFrom("#4E9F6D")!;
                    BadgeBorder.BorderThickness = new Thickness(1);
                }
            }
            else
            {
                StatusBadge.Text = Idiomas.T("Desligado");
                if (temaClaro)
                {
                    StatusBadge.Foreground = Brushes.White;
                    BadgeBorder.Background = (Brush)bc.ConvertFrom("#C62828")!;
                    BadgeBorder.BorderThickness = new Thickness(0);
                }
                else
                {
                    StatusBadge.Foreground = (Brush)bc.ConvertFrom("#F28B82")!;
                    BadgeBorder.Background = (Brush)bc.ConvertFrom("#3D2626")!;
                    BadgeBorder.BorderBrush = (Brush)bc.ConvertFrom("#9F4E4E")!;
                    BadgeBorder.BorderThickness = new Thickness(1);
                }
            }

            AtualizarTextoBotaoJogar();
        }

        /// <summary>
        /// Com os mods LIGADOS, o botão principal diz "Aplicar e Jogar" (aplica os mods e
        /// abre o jogo). Com os mods DESLIGADOS, não tem nada pra "aplicar" de verdade —
        /// então o botão vira "Executar o Launcher", deixando claro de cara que vai abrir
        /// o jogo puro, sem precisar de um popup de aviso no meio do caminho.
        /// </summary>
        private void AtualizarTextoBotaoJogar()
        {
            BtnJogar.Content = _modsLigados ? Idiomas.T("BtnAplicarJogar") : Idiomas.T("BtnExecutarLauncher");
        }

        // ===================== MENU: AÇÕES =====================

        private void MenuReiniciar_Click(object sender, RoutedEventArgs e)
        {
            string caminhoExeAtual = Process.GetCurrentProcess().MainModule?.FileName
                                      ?? Environment.ProcessPath
                                      ?? string.Empty;

            if (!string.IsNullOrEmpty(caminhoExeAtual))
                Process.Start(caminhoExeAtual);

            Application.Current.Shutdown();
        }

        private void MenuLimparCache_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string pastaCache = Paths.Main.Cache;
                foreach (var arquivo in Directory.GetFiles(pastaCache))
                    File.Delete(arquivo);

                MessageBox.Show("Cache de arquivos limpo com sucesso.", "ElsEvo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível limpar o cache:\n{ex.Message}", "ElsEvo",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuLimparConfiguracoes_Click(object sender, RoutedEventArgs e)
        {
            var resposta = MessageBox.Show(
                "Isso vai restaurar todas as configurações do ElsEvo para o padrão. Continuar?",
                "Limpar configurações",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resposta != MessageBoxResult.Yes)
                return;

            Properties.Settings.Default.Reset();
            Properties.Settings.Default.Save();

            ThemeManager.AplicarTemaSalvo();
            InicializacaoComWindows.Aplicar(Properties.Settings.Default.IniciarComWindows);
            AplicarIdioma();

            MessageBox.Show("Configurações restauradas para o padrão.", "ElsEvo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void MenuExcluirMods_Click(object sender, RoutedEventArgs e)
        {
            var resposta = MessageBox.Show(
                "Isso vai excluir TODOS os packs de mods importados. Essa ação não pode ser desfeita. Continuar?",
                "Excluir todos os mods",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (resposta != MessageBoxResult.Yes)
                return;

            try
            {
                string pastaPacks = Paths.Main.Packs;
                if (Directory.Exists(pastaPacks))
                    Directory.Delete(pastaPacks, recursive: true);
                Directory.CreateDirectory(pastaPacks);

                GerenciadorDeMods.Salvar(new List<ModAtivo>());
                AtualizarListaDeModsAtivos();

                MessageBox.Show("Todos os mods foram excluídos.", "ElsEvo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível excluir os mods:\n{ex.Message}", "ElsEvo",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===================== MENU: CONFIGURAÇÕES / SOBRE =====================

        private void MenuConfiguracoes_Click(object sender, RoutedEventArgs e)
        {
            var janela = new PreferenciasWindow { Owner = this };
            janela.ShowDialog();

            // Reaplica tudo que pode ter mudado nas Configurações: tema, idioma,
            // cor do LIGADO/DESLIGADO. Badge BETA não existe mais nessa versão (estável).
            AtualizarVisualToggle();
            AplicarIdioma();
        }

        private void MenuSobre_Click(object sender, RoutedEventArgs e)
        {
            var janela = new SobreWindow { Owner = this };
            janela.ShowDialog();
        }

        private void BtnGerenciarMods_Click(object sender, RoutedEventArgs e)
        {
            var janela = new GerenciarModsWindow { Owner = this };
            janela.ShowDialog();

            AtualizarListaDeModsAtivos();
        }

        // ===================== APLICAR E JOGAR =====================

        private async void BtnJogar_Click(object sender, RoutedEventArgs e)
        {
            if (!Paths.Elsword.IsValidElswordDir(Properties.Settings.Default.ElswordDirectory))
            {
                MessageBox.Show(
                    "A pasta do Elsword configurada não é válida (precisa ter \"elsword.exe\" e a pasta \"data\").\n" +
                    "Configure em Configurações → Elsword → Localização do jogo.",
                    "ElsEvo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var listaDePatches = new List<PatchInfo>();

            if (_modsLigados)
            {
                var ativos = GerenciadorDeMods.Carregar();
                listaDePatches = ativos
                    .Where(m => File.Exists(m.CaminhoCompleto))
                    .Select(m => new PatchInfo(m))
                    .ToList();
            }
            // Quando DESLIGADO, listaDePatches fica vazia mesmo — o próprio texto do botão
            // ("Executar o Launcher") já deixa claro que o jogo vai abrir sem modificações,
            // então não precisamos mais interromper o fluxo com um popup de aviso aqui.

            BtnJogar.IsEnabled = false;
            BtnJogar.Content = "Aguardando o launcher...";

            ProgressoContainer.Visibility = Visibility.Visible;
            BarraProgresso.Value = 0;
            TxtProgresso.Text = "0%";

            var progresso = new Progress<int>(percentual =>
            {
                BarraProgresso.Value = percentual;
                TxtProgresso.Text = $"{percentual}%";
            });

            var statusProgresso = new Progress<EstadoPatch>(estado =>
            {
                BtnJogar.Content = estado switch
                {
                    EstadoPatch.PreparandoArquivos => "Preparando arquivos...",
                    EstadoPatch.AguardandoElswordAbrir => "Aguardando o launcher fechar...",
                    EstadoPatch.FazendoBackup => "Fazendo backup...",
                    EstadoPatch.Aplicando => "Aplicando mods...",
                    EstadoPatch.AguardandoElswordFechar => "Mods ativos — divirta-se! 🎮",
                    EstadoPatch.RestaurandoBackup => "Restaurando backup...",
                    _ => "Concluído"
                };
            });

            _cancelamentoAtual = new CancellationTokenSource();

            try
            {
                await PatcherService.ExecutarFluxoPatchAsync(
                    listaDePatches, progresso, statusProgresso, _cancelamentoAtual.Token);
            }
            catch (OperationCanceledException)
            {
                // cancelado pelo usuário
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocorreu um erro durante o patch:\n{ex.Message}",
                    "ElsEvo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnJogar.IsEnabled = true;
                AtualizarTextoBotaoJogar();
                ProgressoContainer.Visibility = Visibility.Collapsed;
                _cancelamentoAtual = null;
            }
        }

        // ===================== ATUALIZAÇÃO AUTOMÁTICA =====================

        /// <summary>
        /// Roda no Loaded da janela (se "Buscar atualizações ao iniciar" estiver marcado).
        /// Checa o version.json remoto (respeitando o canal estável/beta escolhido) e, se
        /// achar uma versão mais nova, pergunta ao usuário se quer baixar e instalar agora.
        /// Qualquer falha na CHECAGEM em si é sempre silenciosa (ver AtualizacaoService) —
        /// não faz sentido incomodar o usuário toda vez que abrir o app sem internet.
        /// </summary>
        private async Task VerificarAtualizacaoAsync()
        {
            if (!Properties.Settings.Default.CheckForProgramUpdates)
                return;

            var atualizacao = await AtualizacaoService.VerificarAsync();
            if (atualizacao == null)
                return;

            string notas = string.IsNullOrWhiteSpace(atualizacao.Notas)
                ? string.Empty
                : $"{atualizacao.Notas}\n\n";

            string avisoCanal = atualizacao.EhCanalBeta
                ? "ATENÇÃO: essa é uma versão BETA (canal de testes) — pode ter bugs que a " +
                  "versão estável não tem. Ela vai ser instalada por cima da sua instalação " +
                  "atual.\n\n"
                : string.Empty;

            var resposta = MessageBox.Show(
                $"Uma nova versão do ElsEvo está disponível: {atualizacao.VersaoNova}.\n\n" +
                notas +
                avisoCanal +
                "Deseja baixar e instalar agora? O ElsEvo vai fechar durante a instalação.",
                "Atualização disponível",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (resposta != MessageBoxResult.Yes)
                return;

            await BaixarEInstalarAtualizacaoAsync(atualizacao);
        }

        /// <summary>
        /// Baixa o instalador (.exe do Inno Setup) pra uma pasta temporária, mostrando
        /// progresso na mesma barra usada pelo "Aplicar e Jogar". Ao terminar, executa o
        /// instalador (visível, com o assistente normal do Inno Setup — sem silent) e
        /// fecha o próprio ElsEvo em seguida, já que o instalador precisa sobrescrever o
        /// próprio .exe em execução. Erros de rede durante o download são tratados sem
        /// travar o app: mostra um aviso amigável e deixa tudo continuar funcionando na
        /// versão atual.
        /// </summary>
        private async Task BaixarEInstalarAtualizacaoAsync(AtualizacaoDisponivel atualizacao)
        {
            string caminhoInstalador = Path.Combine(Path.GetTempPath(), "ElsEvo-Setup.exe");

            ProgressoContainer.Visibility = Visibility.Visible;
            BarraProgresso.Value = 0;
            TxtProgresso.Text = "Baixando atualização... 0%";

            bool baixouComSucesso = false;

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
                using var resposta = await http.GetAsync(atualizacao.UrlInstalador, HttpCompletionOption.ResponseHeadersRead);
                resposta.EnsureSuccessStatusCode();

                long? tamanhoTotal = resposta.Content.Headers.ContentLength;

                await using var streamOrigem = await resposta.Content.ReadAsStreamAsync();
                await using var streamDestino = File.Create(caminhoInstalador);

                var buffer = new byte[81920];
                long totalLido = 0;
                int lido;

                while ((lido = await streamOrigem.ReadAsync(buffer)) > 0)
                {
                    await streamDestino.WriteAsync(buffer.AsMemory(0, lido));
                    totalLido += lido;

                    if (tamanhoTotal is > 0)
                    {
                        int percentual = (int)(totalLido * 100 / tamanhoTotal.Value);
                        BarraProgresso.Value = percentual;
                        TxtProgresso.Text = $"Baixando atualização... {percentual}%";
                    }
                }

                baixouComSucesso = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Não foi possível baixar a atualização automaticamente:\n{ex.Message}\n\n" +
                    "O ElsEvo vai continuar funcionando normalmente na versão atual. Você pode " +
                    "tentar de novo mais tarde, ou baixar manualmente pela página de Releases no GitHub.",
                    "Falha ao atualizar", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                ProgressoContainer.Visibility = Visibility.Collapsed;
            }

            if (!baixouComSucesso)
                return;

            try
            {
                // Sem argumentos = assistente normal do Inno Setup, visível (não silencioso).
                // "/SP-" só pula a telinha inicial de "Isso vai instalar... Continuar?",
                // já que o usuário acabou de confirmar isso no MessageBox acima — o resto
                // do assistente (pasta de destino, atalho, etc.) continua aparecendo normal.
                Process.Start(new ProcessStartInfo
                {
                    FileName = caminhoInstalador,
                    Arguments = "/SP-",
                    UseShellExecute = true
                });

                // O Inno Setup detecta e fecha o ElsEvo sozinho via Restart Manager antes de
                // sobrescrever os arquivos (ver AppMutex no .iss) — mas fechamos por conta
                // própria aqui também, de forma limpa, pra não depender só disso e evitar
                // qualquer conflito de arquivo em uso durante a instalação.
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"O instalador foi baixado, mas não foi possível executá-lo automaticamente:\n{ex.Message}\n\n" +
                    $"Você pode rodar ele manualmente em:\n{caminhoInstalador}",
                    "Falha ao iniciar instalador", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
