using System.Collections.Generic;
using System.ComponentModel;

namespace ElsEvo
{
    /// <summary>
    /// Representa uma linha da tabela de mods (aba Geral/BGM/Vídeo) na janela "Gerenciar Mods".
    /// Implementa INotifyPropertyChanged para que o ComboBox de cada linha
    /// possa atualizar o valor selecionado sem precisar recarregar a grid inteira.
    /// </summary>
    public class ModItem : INotifyPropertyChanged
    {
        private string _modSelecionado = string.Empty;

        public string Arquivo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;

        /// <summary>Categoria: "Geral", "BGM" ou "Video" — usada para filtrar por aba.</summary>
        public string Categoria { get; set; } = "Geral";

        /// <summary>Caminho real do arquivo no disco (necessário pro patch de verdade).</summary>
        public string CaminhoCompleto { get; set; } = string.Empty;

        /// <summary>Lista de packs de mod disponíveis pra essa linha (ex.: "[Vozes] Brasil - Cor...", "Nenhum").</summary>
        public List<string> OpcoesDisponiveis { get; set; } = new() { "Nenhum" };

        public string ModSelecionado
        {
            get => _modSelecionado;
            set
            {
                if (_modSelecionado != value)
                {
                    _modSelecionado = value;
                    OnPropertyChanged(nameof(ModSelecionado));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string nome) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nome));
    }
}
