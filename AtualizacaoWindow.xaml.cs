using System.Windows;

namespace ElsEvo
{
    /// <summary>
    /// Substitui o MessageBox nativo do fluxo "uma atualização está disponível" por uma
    /// janela que segue o tema Claro/Escuro do app (mesmos DynamicResource usados no
    /// resto da interface — CorFundoPrincipal, CorBotaoFlat, etc., ver ThemeManager.cs).
    /// DialogResult == true significa "o usuário quer atualizar agora".
    /// </summary>
    public partial class AtualizacaoWindow : Window
    {
        public AtualizacaoWindow(AtualizacaoDisponivel atualizacao)
        {
            InitializeComponent();
            ThemeManager.AplicarTemaSalvo(); // reforço de segurança, igual as outras janelas

            SourceInitialized += (_, _) =>
                BarraTituloNativa.AplicarTema(this, !Properties.Settings.Default.TemaClaro);

            TxtVersaoNova.Text = $"Versão {atualizacao.VersaoNova} disponível";

            TxtNotas.Text = string.IsNullOrWhiteSpace(atualizacao.Notas)
                ? "Sem notas de lançamento para esta versão."
                : atualizacao.Notas.Trim();

            ContainerAvisoBeta.Visibility = atualizacao.EhCanalBeta ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnAtualizar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnAgoraNao_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
