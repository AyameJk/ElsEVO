using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ElsEvo
{
    /// <summary>
    /// Escurece a barra de título NATIVA do Windows (a que o próprio SO desenha) usando a
    /// API do DWM. Usado nas janelas que não têm chrome 100% customizado (Preferências,
    /// Gerenciar Mods, Sobre) — assim elas acompanham o tema Claro/Escuro do app mesmo
    /// mantendo os botões nativos de minimizar/maximizar/fechar e redimensionamento.
    /// Funciona no Windows 10 versão 2004+ e Windows 11.
    /// </summary>
    public static class BarraTituloNativa
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        /// <summary>Chame no evento Loaded (ou SourceInitialized) da janela.</summary>
        public static void AplicarTema(Window janela, bool temaEscuro)
        {
            try
            {
                var helper = new WindowInteropHelper(janela);
                IntPtr hwnd = helper.Handle;
                if (hwnd == IntPtr.Zero)
                    return;

                int valor = temaEscuro ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref valor, sizeof(int));
            }
            catch
            {
                // Windows mais antigo sem suporte a essa API — ignora silenciosamente,
                // a janela só fica com a barra de título clara mesmo.
            }
        }
    }
}
