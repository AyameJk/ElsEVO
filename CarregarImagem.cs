using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace ElsEvo
{
    /// <summary>
    /// Carrega imagens direto da pasta Assets (ao lado do .exe) em vez de depender de
    /// recursos empacotados/pack URI — mais simples de garantir que funciona.
    /// </summary>
    public static class CarregarImagem
    {
        private static string PastaAssets => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets");

        /// <summary>Procura um arquivo cujo nome (sem extensão) bate com "nomeBase" dentro de Assets/.</summary>
        public static BitmapImage? BuscarPorNomeBase(string nomeBase)
        {
            try
            {
                if (!Directory.Exists(PastaAssets))
                    return null;

                string? caminho = Directory.GetFiles(PastaAssets)
                    .FirstOrDefault(f => Path.GetFileNameWithoutExtension(f)
                        .Equals(nomeBase, StringComparison.OrdinalIgnoreCase));

                if (caminho == null)
                    return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(caminho, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                return null;
            }
        }
    }
}
