using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ElsEvo
{
    /// <summary>
    /// Substitui o MessageBox nativo do fluxo "uma atualização está disponível" por uma
    /// janela que segue o tema Claro/Escuro do app (mesmos DynamicResource usados no
    /// resto da interface — CorFundoPrincipal, CorBotaoFlat, etc., ver ThemeManager.cs).
    /// DialogResult == true significa "o usuário quer atualizar agora".
    ///
    /// As notas de lançamento vêm em Markdown/HTML puro do GitHub Releases (podem ter
    /// uma tag &lt;img&gt; de capa e uma linha de citação em "&gt; texto"). Como o
    /// TextBlock não renderiza Markdown/HTML, essa janela faz um parsing simples pra:
    ///   1) baixar e mostrar a imagem de verdade (não a tag &lt;img&gt; crua);
    ///   2) destacar a citação num bloco separado, sem o "&gt;" solto;
    ///   3) deixar só o texto normal no corpo das notas.
    /// Qualquer coisa que não seja reconhecida (formato inesperado) simplesmente não
    /// aparece destacada — nunca quebra a janela.
    /// </summary>
    public partial class AtualizacaoWindow : Window
    {
        // Ex.: <img width="546" height="569" alt="..." src="https://..." />
        private static readonly Regex RegexImagem =
            new(@"<img[^>]*\ssrc=[""']([^""']+)[""'][^>]*/?>", RegexOptions.IgnoreCase);

        // Ex.: > "Eu serei o cavaleiro do rei... e o escudo de todos!"
        private static readonly Regex RegexCitacao =
            new(@"^\s*>\s*(.+)$", RegexOptions.Multiline);

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public AtualizacaoWindow(AtualizacaoDisponivel atualizacao)
        {
            InitializeComponent();
            ThemeManager.AplicarTemaSalvo(); // reforço de segurança, igual as outras janelas

            SourceInitialized += (_, _) =>
                BarraTituloNativa.AplicarTema(this, !Properties.Settings.Default.TemaClaro);

            TxtVersaoNova.Text = $"Versão {atualizacao.VersaoNova} disponível";

            ContainerAvisoBeta.Visibility = atualizacao.EhCanalBeta ? Visibility.Visible : Visibility.Collapsed;

            PrepararNotas(atualizacao.Notas);
        }

        /// <summary>
        /// Extrai imagem e citação das notas (se existirem) e deixa só o texto puro pro
        /// TextBlock principal. A imagem é baixada em segundo plano — a janela não trava
        /// esperando o download.
        /// </summary>
        private void PrepararNotas(string notasBrutas)
        {
            string texto = notasBrutas ?? string.Empty;

            // 1) Imagem: acha a primeira tag <img>, guarda a URL e remove a tag do texto.
            string? urlImagem = null;
            var matchImagem = RegexImagem.Match(texto);
            if (matchImagem.Success)
            {
                urlImagem = matchImagem.Groups[1].Value;
                texto = texto.Remove(matchImagem.Index, matchImagem.Length);
            }

            // 2) Citação: acha a primeira linha "> texto", mostra separada, remove do corpo.
            var matchCitacao = RegexCitacao.Match(texto);
            if (matchCitacao.Success)
            {
                string citacao = matchCitacao.Groups[1].Value.Trim().Trim('"', '“', '”');
                TxtCitacao.Text = $"“{citacao}”";
                ContainerCitacao.Visibility = Visibility.Visible;
                texto = texto.Remove(matchCitacao.Index, matchCitacao.Length);
            }

            // 3) Sobra do texto: some linhas em branco extras deixadas pela remoção acima.
            texto = Regex.Replace(texto, @"(\r?\n){3,}", "\n\n").Trim();
            TxtNotas.Text = string.IsNullOrWhiteSpace(texto)
                ? "Sem notas de lançamento adicionais para esta versão."
                : texto;

            if (!string.IsNullOrWhiteSpace(urlImagem))
                _ = CarregarImagemAsync(urlImagem);
        }

        /// <summary>
        /// Baixa a imagem de capa em segundo plano e mostra ela de verdade. Se falhar
        /// por qualquer motivo (sem internet, URL inválida, etc.), simplesmente não
        /// mostra nada — nunca trava nem quebra a janela por causa disso.
        /// </summary>
        private async Task CarregarImagemAsync(string url)
        {
            try
            {
                byte[] dados = await _http.GetByteArrayAsync(url);

                var bitmap = new BitmapImage();
                using (var stream = new MemoryStream(dados))
                {
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();
                }
                bitmap.Freeze();

                ImgCapa.Source = bitmap;
                ContainerImagem.Visibility = Visibility.Visible;
            }
            catch
            {
                // Sem imagem — a janela continua funcionando normalmente sem ela.
            }
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
