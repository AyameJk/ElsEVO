namespace ElsEvo
{
    /// <summary>
    /// Versão exibida na janela "Sobre" (formato simples, "1.0") e versão usada
    /// internamente pelo AtualizacaoService pra comparar com o version.json remoto
    /// (formato completo, "1.0.0" — precisa ter sempre 3 dígitos pra bater exatamente
    /// com o version.json e evitar falso positivo de atualização).
    /// </summary>
    public static class AppVersion
    {
        /// <summary>Versão exibida pro usuário na tela "Sobre" — formato curto.</summary>
        public const string Numero = "1.0";

        /// <summary>
        /// Versão usada pelo AtualizacaoService pra comparar com o version.json remoto.
        /// Sempre com 3 dígitos (Major.Minor.Build), pra bater exatamente com o formato
        /// usado no version.json e não gerar falso positivo de atualização disponível.
        /// </summary>
        public const string VersaoParaAtualizacao = "1.0.4";
    }
}
