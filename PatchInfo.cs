using System.IO;

namespace ElsEvo
{
    /// <summary>
    /// Réplica do PatchInfo(Preset) original: calcula onde o arquivo modificado deve
    /// ir (destino real no jogo) e onde fica o backup do original — quando existe.
    /// BGM não tem backup porque cai numa pasta própria (Media) sem sobrescrever nada.
    /// </summary>
    public class PatchInfo
    {
        public string ArquivoModificado { get; }     // arquivo de origem, dentro do pack
        public string ArquivoTemporario { get; }     // cópia em cache (staging)
        public string ArquivoDestino { get; }         // onde entra de fato no jogo
        public string? ArquivoBackup { get; }         // null para BGM

        public PatchInfo(ModAtivo mod)
        {
            ArquivoModificado = mod.CaminhoCompleto;
            ArquivoTemporario = Path.Combine(Paths.Main.Cache, mod.Arquivo);

            switch (mod.Categoria)
            {
                case CategoriaMod.Video:
                    ArquivoDestino = Path.Combine(Paths.Elsword.Movie, mod.Arquivo);
                    ArquivoBackup = Path.Combine(Paths.Elsword.Backup, mod.Arquivo);
                    break;
                case CategoriaMod.BGM:
                    ArquivoDestino = Path.Combine(Paths.Elsword.Media, mod.Arquivo);
                    ArquivoBackup = null;
                    break;
                default: // Geral
                    ArquivoDestino = Path.Combine(Paths.Elsword.Data, mod.Arquivo);
                    ArquivoBackup = Path.Combine(Paths.Elsword.Backup, mod.Arquivo);
                    break;
            }
        }
    }
}
