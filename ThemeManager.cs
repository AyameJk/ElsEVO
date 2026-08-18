using System;
using System.Windows;
using System.Windows.Media;

namespace ElsEvo
{
    /// <summary>
    /// Aplica o tema Claro/Escuro em tempo real, trocando os brushes registrados em
    /// Application.Current.Resources. As janelas usam DynamicResource pras cores-chave,
    /// então mudam sozinhas assim que isso roda — sem precisar reabrir a janela.
    /// </summary>
    public static class ThemeManager
    {
        /// <summary>Disparado toda vez que o tema é aplicado — janelas abertas podem se
        /// inscrever pra reagir na hora (ex.: recolorir badges, escurecer barra de título
        /// nativa), em vez de só atualizar quando forem reabertas.</summary>
        public static event Action<bool>? TemaMudou;

        public static void AplicarTema(bool temaClaro)
        {
            var recursos = Application.Current.Resources;

            if (temaClaro)
            {
                recursos["CorFundoPrincipal"] = Cor("#F3F3F3");
                recursos["CorFundoCartao"] = Cor("#FFFFFF");
                recursos["CorFundoCampo"] = Cor("#FFFFFF");
                recursos["CorBorda"] = Cor("#D0D0D0");
                recursos["CorTextoPrimario"] = Cor("#1A1A1A");
                recursos["CorTextoSecundario"] = Cor("#5A5A5A");
                recursos["CorBotaoFlat"] = Cor("#E4E4E4");
                recursos["CorBotaoFlatHover"] = Cor("#D6D6D6");
            }
            else
            {
                recursos["CorFundoPrincipal"] = Cor("#1E1E1E");
                recursos["CorFundoCartao"] = Cor("#252526");
                recursos["CorFundoCampo"] = Cor("#1E1E1E");
                recursos["CorBorda"] = Cor("#3F3F46");
                recursos["CorTextoPrimario"] = Cor("#E3E3E3");
                recursos["CorTextoSecundario"] = Cor("#858585");
                recursos["CorBotaoFlat"] = Cor("#2D2D30");
                recursos["CorBotaoFlatHover"] = Cor("#3A3A3D");
            }

            TemaMudou?.Invoke(temaClaro);
        }

        private static SolidColorBrush Cor(string hex) =>
            new((Color)ColorConverter.ConvertFromString(hex));

        /// <summary>Chama isso uma vez no início do app (App.xaml.cs) pra já sair no tema salvo.</summary>
        public static void AplicarTemaSalvo()
        {
            AplicarTema(Properties.Settings.Default.TemaClaro);
        }
    }
}
