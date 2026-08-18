using System;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElsEvo
{
    /// <summary>
    /// Conteúdo do version.json — UM SÓ objeto por repositório (a separação estável/beta
    /// não é mais uma chave dentro do mesmo arquivo, e sim dois repositórios diferentes,
    /// cada um com seu próprio version.json na raiz).
    /// </summary>
    public class InfoVersao
    {
        [JsonPropertyName("versao")]
        public string Versao { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("notas")]
        public string Notas { get; set; } = string.Empty;
    }

    /// <summary>Resultado de uma checagem que encontrou uma versão mais nova disponível.</summary>
    public class AtualizacaoDisponivel
    {
        public string VersaoNova { get; set; } = string.Empty;
        public string UrlInstalador { get; set; } = string.Empty;
        public string Notas { get; set; } = string.Empty;

        /// <summary>true quando essa atualização veio do canal BETA (repositório irmão),
        /// não do canal estável — usado pra avisar o usuário antes de instalar por cima.</summary>
        public bool EhCanalBeta { get; set; }
    }

    /// <summary>
    /// Verifica se existe uma versão mais nova do ElsEvo publicada. Cada canal é um
    /// REPOSITÓRIO GITHUB SEPARADO, cada um com seu próprio version.json na raiz:
    ///   - Canal estável (esta build): repositório ElsEvo
    ///   - Canal beta (build irmã):    repositório ElsEvoBeta
    ///
    /// ATENÇÃO: os nomes reais dos repositórios são "ElsEvo" e "ElsEvoBeta" (essa
    /// capitalização exata) — NÃO "ElsEVO"/"ElsEVOBeta". O GitHub redireciona URLs da
    /// página normal (github.com/...) ignorando maiúscula/minúscula, mas o
    /// raw.githubusercontent.com pode não redirecionar da mesma forma — por isso é
    /// essencial usar aqui exatamente o nome real do repositório, sem depender de
    /// redirecionamento.
    ///
    /// Com "Beta apenas" DESMARCADO (padrão nesta build estável), o app consulta só o
    /// próprio canal. Com "Beta apenas" MARCADO, o app passa a consultar o repositório
    /// irmão e oferece baixar/instalar a build beta por cima da instalação estável atual.
    /// </summary>
    public static class AtualizacaoService
    {
        private const string UrlVersionJsonEstavel =
            "https://raw.githubusercontent.com/AyameJk/ElsEvo/main/version.json";

        private const string UrlVersionJsonBeta =
            "https://raw.githubusercontent.com/AyameJk/ElsEvoBeta/main/version.json";

        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// Retorna os dados da atualização se houver uma versão mais nova que a atual no
        /// canal certo (estável ou beta, conforme "Beta apenas" nas Configurações).
        /// Retorna null se já estiver na versão mais recente, ou se a checagem falhar por
        /// qualquer motivo (sem internet, GitHub fora do ar, JSON mudou de formato, etc.)
        /// — checagem de atualização NUNCA deve travar ou incomodar o usuário ao abrir o
        /// app, então qualquer erro aqui é silencioso.
        /// </summary>
        public static async Task<AtualizacaoDisponivel?> VerificarAsync()
        {
            try
            {
                bool buscarBeta = !Properties.Settings.Default.IgnoreBetaReleases;
                string urlManifesto = buscarBeta ? UrlVersionJsonBeta : UrlVersionJsonEstavel;

                // Cache-busting: o raw.githubusercontent.com às vezes serve uma cópia em
                // cache por alguns minutos depois do push — o parâmetro garante que a
                // gente sempre pega a versão mais recente de verdade.
                string url = $"{urlManifesto}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

                string json = await _http.GetStringAsync(url);
                var info = JsonSerializer.Deserialize<InfoVersao>(json);

                if (info == null || string.IsNullOrWhiteSpace(info.Versao) || string.IsNullOrWhiteSpace(info.Url))
                    return null;

                if (!VersaoEhMaisNova(info.Versao, AppVersion.VersaoParaAtualizacao))
                    return null;

                return new AtualizacaoDisponivel
                {
                    VersaoNova = info.Versao,
                    UrlInstalador = info.Url,
                    Notas = info.Notas,
                    EhCanalBeta = buscarBeta
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Compara duas versões no formato "1.0.XXX" numericamente (Major.Minor.Build),
        /// não como texto — "1.0.9" precisa ser considerado MENOR que "1.0.010", por
        /// exemplo, o que uma comparação de string acertaria por acaso mas não de forma
        /// confiável em todos os casos. Sempre manter os dois lados (version.json remoto
        /// e AppVersion.VersaoParaAtualizacao local) com a MESMA quantidade de dígitos
        /// (Major.Minor.Build, 3 números) — "1.0" e "1.0.0" NÃO são iguais pro
        /// Version.TryParse (Build fica -1 vs 0), o que pode gerar falso positivo de
        /// atualização disponível mesmo estando na versão certa.
        /// </summary>
        private static bool VersaoEhMaisNova(string versaoRemota, string versaoAtual)
        {
            if (Version.TryParse(versaoRemota, out var vRemota) && Version.TryParse(versaoAtual, out var vAtual))
                return vRemota > vAtual;

            // Formato inesperado (não parseável como Version) — fallback simples: só
            // considera "mais nova" se for literalmente diferente da atual.
            return !string.Equals(versaoRemota, versaoAtual, StringComparison.OrdinalIgnoreCase);
        }
    }
}
