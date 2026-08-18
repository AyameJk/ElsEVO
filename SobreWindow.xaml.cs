using System.Windows;

namespace ElsEvo
{
    public partial class SobreWindow : Window
    {
        public SobreWindow()
        {
            InitializeComponent();
            ThemeManager.AplicarTemaSalvo(); // reforço de segurança

            SourceInitialized += (_, _) =>
                BarraTituloNativa.AplicarTema(this, !Properties.Settings.Default.TemaClaro);

            ThemeManager.TemaMudou += AoTemaMudar;
            Closed += (_, _) => ThemeManager.TemaMudou -= AoTemaMudar; // evita vazamento de memória

            AplicarIdioma();

            // Versão estável: sem badge BETA. O título grande (TxtVersao) continua
            // mostrando o número curto (AppVersion.Numero, ex.: "1.0"), mas aqui embaixo
            // mostramos o número COMPLETO com patch (AppVersion.VersaoParaAtualizacao,
            // ex.: "1.0.1"), pra quem quiser conferir a build exata instalada.
            BadgeBeta.Visibility = Visibility.Collapsed;
            TxtVersaoBeta.Text = $"Versão estável: {AppVersion.VersaoParaAtualizacao}";
            TxtVersaoBeta.Visibility = Visibility.Visible;
        }

        /// <summary>Roda toda vez que o tema muda enquanto esta janela está aberta.</summary>
        private void AoTemaMudar(bool temaClaro)
        {
            BarraTituloNativa.AplicarTema(this, !temaClaro);
        }

        private void AplicarIdioma()
        {
            Title = Idiomas.T("TituloSobre");
            TxtVersao.Text = AppVersion.Numero;
            TxtDescricao.Text = Idiomas.T("SobreDescricao");
            TxtRotuloAutor.Text = Idiomas.T("SobreAutor");
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e) => Close();
    }
}
