using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace ElsEvo
{
    public partial class PreferenciasWindow : Window
    {
        // Começa TRUE de propósito: o valor padrão do XAML (SelectedIndex="0" no idioma,
        // IsChecked="True" no tema Escuro) dispara os eventos de mudança durante o
        // InitializeComponent(), ANTES da gente ler a configuração salva de verdade.
        // Sem isso, abrir a janela já sobrescrevia o idioma/tema salvos.
        private bool _carregando = true;

        // Texto de exemplo do campo de argumentos (usado como PLACEHOLDER de verdade agora:
        // aparece cinza quando o campo está vazio e sem foco, e some completamente assim
        // que o usuário clica pra digitar — não precisa mais apagar nada na mão).
        private const string PlaceholderArgumentos = "argumentos | ex: 8f9slxa02nkp29ak1u26mqpcms";

        // Liga enquanto o próprio código está trocando o texto do placeholder (focar/desfocar),
        // pra não marcar "Aplicar" como pendente só por causa disso.
        private bool _ajustandoPlaceholder;

        public PreferenciasWindow()
        {
            InitializeComponent();
            ThemeManager.AplicarTemaSalvo(); // reforço de segurança
            CarregarConfiguracoes();
            AplicarIdioma();
            _carregando = false;

            SourceInitialized += (_, _) =>
                BarraTituloNativa.AplicarTema(this, !Properties.Settings.Default.TemaClaro);

            ConectarDeteccaoDeAlteracoes();

            TxtArgumentos.GotFocus += TxtArgumentos_GotFocus;
            TxtArgumentos.LostFocus += TxtArgumentos_LostFocus;
        }

        /// <summary>
        /// "Aplicar" começa desabilitado (nada mudou ainda) e só liga de novo quando o
        /// usuário mexe em algum campo — igual o comportamento padrão de configurações do
        /// Windows. Depois de salvar, desabilita de novo (permanece assim até nova mudança).
        /// </summary>
        private void ConectarDeteccaoDeAlteracoes()
        {
            void Marcar(object? sender, EventArgs e)
            {
                if (!_carregando && !_ajustandoPlaceholder)
                    BtnAplicar.IsEnabled = true;
            }

            foreach (var chk in new[]
                     {
                         ChkNaoExecutarLauncher, ChkPularElsword, ChkBloquearLogs,
                         ChkMinimizarBandeja, ChkIniciarMinimizado, ChkIniciarComWindows,
                         ChkBuscarAtualizacoes, ChkBetaApenas
                     })
            {
                chk.Checked += Marcar;
                chk.Unchecked += Marcar;
            }

            RadioTemaClaro.Checked += Marcar;
            RadioTemaEscuro.Checked += Marcar;
            CmbIdioma.SelectionChanged += Marcar;
            TxtArgumentos.TextChanged += Marcar;

            BtnAplicar.IsEnabled = false;
        }

        private void AplicarIdioma()
        {
            Title = Idiomas.T("TituloConfiguracoes");
            AbaElsword.Header = Idiomas.T("AbaElsword");
            AbaInicializador.Header = Idiomas.T("AbaInicializador");
            BtnOk.Content = Idiomas.T("BotaoOk");
            BtnCancelar.Content = Idiomas.T("BotaoCancelar");
            BtnAplicar.Content = Idiomas.T("BotaoAplicar");

            GrpLocalizacaoJogo.Header = Idiomas.T("GrpLocalizacaoJogo");
            GrpOpcoesInicializacao.Header = Idiomas.T("GrpOpcoesInicializacao");
            ChkNaoExecutarLauncher.Content = Idiomas.T("ChkNaoExecutarLauncher");
            TxtRecomendadoCoreano.Text = Idiomas.T("TxtRecomendadoCoreano");
            ChkPularElsword.Content = Idiomas.T("ChkPularElsword");
            GrpSeguranca.Header = Idiomas.T("GrpSeguranca");
            ChkBloquearLogs.Content = Idiomas.T("ChkBloquearLogs");
            TxtAvisoLogs.Text = Idiomas.T("TxtAvisoLogs");

            GrpIdiomas.Header = Idiomas.T("GrpIdiomas");
            GrpTema.Header = Idiomas.T("GrpTema");
            RadioTemaClaro.Content = Idiomas.T("RadioClaro");
            RadioTemaEscuro.Content = Idiomas.T("RadioEscuro");
            GrpIconeBandeja.Header = Idiomas.T("GrpIconeBandeja");
            ChkMinimizarBandeja.Content = Idiomas.T("ChkMinimizarBandeja");
            ChkIniciarMinimizado.Content = Idiomas.T("ChkIniciarMinimizado");
            ChkIniciarComWindows.Content = Idiomas.T("ChkIniciarComWindows");
            GrpAtualizacoes.Header = Idiomas.T("GrpAtualizacoes");
            ChkBuscarAtualizacoes.Content = Idiomas.T("ChkBuscarAtualizacoes");
            ChkBetaApenas.Content = Idiomas.T("ChkBetaApenas");
            TxtAvisoBetaApenas.Text = Idiomas.T("TxtAvisoBetaApenas");
        }

        private void CarregarConfiguracoes()
        {
            var cfg = Properties.Settings.Default;

            bool temCaminhoReal = !string.IsNullOrWhiteSpace(cfg.ElswordDirectory);
            TxtLocalizacaoJogo.Text = temCaminhoReal
                ? Path.Combine(cfg.ElswordDirectory, "elsword.exe")
                : "ex: C:\\Elsword\\elsword.exe";
            AtualizarAparenciaPlaceholder(temCaminhoReal);

            ChkBloquearLogs.IsChecked = cfg.BlockLogs;
            ChkNaoExecutarLauncher.IsChecked = cfg.WebLoginNeeded;

            ChkPularElsword.IsChecked = cfg.SkipLauncher;
            TxtArgumentos.IsEnabled = cfg.SkipLauncher;

            // Placeholder de verdade: só mostra o texto de exemplo (cinza) quando não há
            // argumento salvo. Ao focar o campo, ele já some sozinho — ver TxtArgumentos_GotFocus.
            _ajustandoPlaceholder = true;
            if (string.IsNullOrWhiteSpace(cfg.X2Args))
            {
                TxtArgumentos.Text = PlaceholderArgumentos;
                TxtArgumentos.Foreground = (System.Windows.Media.Brush)FindResource("CorTextoSecundario");
            }
            else
            {
                TxtArgumentos.Text = cfg.X2Args;
                TxtArgumentos.Foreground = (System.Windows.Media.Brush)FindResource("CorTextoPrimario");
            }
            _ajustandoPlaceholder = false;

            RadioTemaClaro.IsChecked = cfg.TemaClaro;
            RadioTemaEscuro.IsChecked = !cfg.TemaClaro;

            ChkBetaApenas.IsChecked = !cfg.IgnoreBetaReleases;
            ChkBuscarAtualizacoes.IsChecked = cfg.CheckForProgramUpdates;

            ChkMinimizarBandeja.IsChecked = cfg.MinimizarParaBandeja;
            ChkIniciarMinimizado.IsChecked = cfg.StartHidden;
            ChkIniciarComWindows.IsChecked = cfg.IniciarComWindows;

            CmbIdioma.SelectedIndex = cfg.Idioma switch
            {
                "en" => 1,
                "zh" => 2,
                _ => 0
            };
        }

        private void BtnProcurarJogo_Click(object sender, RoutedEventArgs e)
        {
            var dialogo = new OpenFileDialog
            {
                Title = "Selecione o elsword.exe",
                Filter = "elsword.exe|elsword.exe|Executáveis (*.exe)|*.exe",
                FileName = "elsword.exe"
            };

            if (dialogo.ShowDialog() == true)
            {
                TxtLocalizacaoJogo.Text = dialogo.FileName;
                AtualizarAparenciaPlaceholder(temCaminhoReal: true);
                if (!_carregando)
                    BtnAplicar.IsEnabled = true;
            }
        }

        private void ChkPularElsword_CheckedChanged(object sender, RoutedEventArgs e)
        {
            TxtArgumentos.IsEnabled = ChkPularElsword.IsChecked == true;
        }

        /// <summary>Deixa o texto do campo mais apagado quando é só o placeholder de exemplo.</summary>
        private void AtualizarAparenciaPlaceholder(bool temCaminhoReal)
        {
            TxtLocalizacaoJogo.Foreground = temCaminhoReal
                ? (System.Windows.Media.Brush)FindResource("CorTextoPrimario")
                : (System.Windows.Media.Brush)FindResource("CorTextoSecundario");
        }

        /// <summary>
        /// Ao focar o campo de argumentos: se estiver mostrando só o texto de exemplo,
        /// some com ele (não precisa mais selecionar tudo e apagar na mão).
        /// </summary>
        private void TxtArgumentos_GotFocus(object sender, RoutedEventArgs e)
        {
            if (TxtArgumentos.Text == PlaceholderArgumentos)
            {
                _ajustandoPlaceholder = true;
                TxtArgumentos.Text = string.Empty;
                TxtArgumentos.Foreground = (System.Windows.Media.Brush)FindResource("CorTextoPrimario");
                _ajustandoPlaceholder = false;
            }
        }

        /// <summary>
        /// Ao sair do campo: se o usuário não digitou nada, volta a mostrar o texto de
        /// exemplo (cinza), igual estava antes de focar.
        /// </summary>
        private void TxtArgumentos_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtArgumentos.Text))
            {
                _ajustandoPlaceholder = true;
                TxtArgumentos.Text = PlaceholderArgumentos;
                TxtArgumentos.Foreground = (System.Windows.Media.Brush)FindResource("CorTextoSecundario");
                _ajustandoPlaceholder = false;
            }
        }

        private void RadioTema_Checked(object sender, RoutedEventArgs e)
        {
            // Só guarda a escolha visualmente — o tema de verdade só aplica quando
            // clicar em "Aplicar" ou "OK" (ver SalvarConfiguracoes).
        }

        private void CmbIdioma_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Só guarda a escolha visualmente por enquanto — o idioma de verdade só troca
            // quando clicar em "Aplicar" ou "OK" (igual o tema).
        }

        private void SalvarConfiguracoes()
        {
            var cfg = Properties.Settings.Default;

            string caminhoExe = TxtLocalizacaoJogo.Text;
            cfg.ElswordDirectory = File.Exists(caminhoExe)
                ? Path.GetDirectoryName(caminhoExe) ?? string.Empty
                : cfg.ElswordDirectory;

            cfg.BlockLogs = ChkBloquearLogs.IsChecked == true;
            cfg.WebLoginNeeded = ChkNaoExecutarLauncher.IsChecked == true;
            cfg.SkipLauncher = ChkPularElsword.IsChecked == true;
            cfg.X2Args = TxtArgumentos.Text == PlaceholderArgumentos ? string.Empty : TxtArgumentos.Text;
            cfg.TemaClaro = RadioTemaClaro.IsChecked == true;
            cfg.IgnoreBetaReleases = ChkBetaApenas.IsChecked != true;
            cfg.CheckForProgramUpdates = ChkBuscarAtualizacoes.IsChecked == true;
            // IsBetaRelease não é mais editável pelo usuário — fica fixo enquanto o
            // ElsEVO estiver mesmo em fase beta (ver Properties/Settings.cs).

            cfg.MinimizarParaBandeja = ChkMinimizarBandeja.IsChecked == true;
            cfg.StartHidden = ChkIniciarMinimizado.IsChecked == true;

            bool iniciarComWindows = ChkIniciarComWindows.IsChecked == true;
            cfg.IniciarComWindows = iniciarComWindows;
            InicializacaoComWindows.Aplicar(iniciarComWindows);

            string codigoIdioma = CmbIdioma.SelectedIndex switch
            {
                1 => "en",
                2 => "zh",
                _ => "pt"
            };

            cfg.Save();

            // Tema e idioma só são aplicados de fato aqui (não em tempo real ao interagir).
            ThemeManager.AplicarTema(cfg.TemaClaro);
            BarraTituloNativa.AplicarTema(this, !cfg.TemaClaro);
            Idiomas.DefinirIdioma(codigoIdioma); // já salva de novo e dispara o evento pras janelas abertas
            AplicarIdioma(); // atualiza esta própria janela também

            BtnAplicar.IsEnabled = false; // volta a ficar "apagado" até algo mudar de novo
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SalvarConfiguracoes();
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível salvar as configurações:\n{ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnAplicar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SalvarConfiguracoes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Não foi possível salvar as configurações:\n{ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
