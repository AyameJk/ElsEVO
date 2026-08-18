using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ElsEvo
{
    /// <summary>Categorias de mod, espelhando o enum ModTypes real do gPatcher.</summary>
    public enum CategoriaMod
    {
        Geral,
        BGM,
        Video
    }

    /// <summary>
    /// Um mod ativo (equivalente a um "Preset" no gPatcher original: qual arquivo do jogo
    /// deve vir de qual pack). Guarda o caminho real do arquivo pra o patch conseguir
    /// copiar de verdade, não só o nome.
    /// </summary>
    public class ModAtivo
    {
        public string Arquivo { get; set; } = string.Empty;       // nome do arquivo do jogo (ex.: data079.kom)
        public string Descricao { get; set; } = string.Empty;
        public string NomeDoPack { get; set; } = string.Empty;    // nome do ModPack escolhido
        public string CaminhoCompleto { get; set; } = string.Empty; // onde o arquivo modificado está no disco
        public CategoriaMod Categoria { get; set; } = CategoriaMod.Geral;
    }

    /// <summary>
    /// Lê/grava a lista de mods ativos. No original isso é o "usrmods.xml" salvo em
    /// %LocalAppData%\ElsEVO; aqui usamos JSON no mesmo lugar (Paths.UserMods).
    /// </summary>
    public static class GerenciadorDeMods
    {
        public static List<ModAtivo> Carregar()
        {
            try
            {
                if (!File.Exists(Paths.UserMods))
                    return new List<ModAtivo>();

                string json = File.ReadAllText(Paths.UserMods);
                return JsonSerializer.Deserialize<List<ModAtivo>>(json) ?? new List<ModAtivo>();
            }
            catch
            {
                return new List<ModAtivo>();
            }
        }

        public static void Salvar(List<ModAtivo> modsAtivos)
        {
            Directory.CreateDirectory(Paths.LocalApplicationData);

            var opcoes = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(modsAtivos, opcoes);
            File.WriteAllText(Paths.UserMods, json);
        }
    }
}
