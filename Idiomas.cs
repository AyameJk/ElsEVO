using System;
using System.Collections.Generic;

namespace ElsEvo
{
    /// <summary>
    /// Sistema de idiomas simples: um dicionário de textos por idioma, com evento
    /// pra avisar as janelas abertas quando o idioma muda (assim elas se atualizam
    /// sem precisar reabrir). Cobre as strings principais da interface; dá pra
    /// expandir facilmente adicionando mais chaves nos três dicionários abaixo.
    /// </summary>
    public static class Idiomas
    {
        public static event Action? IdiomaMudou;

        private static readonly Dictionary<string, Dictionary<string, string>> _textos = new()
        {
            ["pt"] = new()
            {
                ["MenuAcoes"] = "Ações",
                ["MenuConfiguracoes"] = "Configurações",
                ["MenuSobre"] = "Sobre",
                ["AcaoReiniciar"] = "Reiniciar Programa",
                ["AcaoLimparCache"] = "Limpar Cache",
                ["AcaoLimparConfiguracoes"] = "Limpar Configurações",
                ["AcaoExcluirMods"] = "Excluir Todos os Mods",
                ["BtnGerenciarMods"] = "Gerenciar Mods",
                ["BtnAplicarJogar"] = "Aplicar e Jogar",
                ["BtnExecutarLauncher"] = "Executar o Launcher",
                ["ModsAtivos"] = "Mods Ativos",
                ["ListaVazia"] = "Nenhum mod ativo. Use \"Gerenciar Mods\" para importar.",
                ["Ligado"] = "LIGADO",
                ["Desligado"] = "DESLIGADO",
                ["TituloConfiguracoes"] = "Configurações",
                ["AbaElsword"] = "Elsword",
                ["AbaInicializador"] = "Inicializador",
                ["BotaoOk"] = "OK",
                ["BotaoCancelar"] = "Cancelar",
                ["BotaoAplicar"] = "Aplicar",
                ["TituloGerenciarMods"] = "Mods",
                ["AbaGeral"] = "Geral",
                ["AbaBgm"] = "BGM",
                ["AbaVideo"] = "Vídeo",
                ["TituloSobre"] = "Sobre o ElsEvo",
                ["SobreDescricao"] = "O ElsEvo é um aplicativo que automatiza o processo de modificação do jogo Elsword. Compatível com mods criados pelos próprios jogadores, também oferece suporte a packs de voz e outras personalizações. Criado para que os jogadores pudessem matar a saudade dos tempos em que o Elsword brasileiro ainda estava no ar, trazendo de volta a experiência com uma ferramenta moderna, rápida e mais segura de usar.",
                ["SobreAutor"] = "Autor:",
                ["GrpLocalizacaoJogo"] = "LOCALIZAÇÃO DO JOGO",
                ["GrpOpcoesInicializacao"] = "OPÇÕES DE INICIALIZAÇÃO",
                ["ChkNaoExecutarLauncher"] = "Não executar o launcher",
                ["TxtRecomendadoCoreano"] = "(recomendado para o servidor coreano)",
                ["ChkPularElsword"] = "Pular elsword.exe (avançado)",
                ["GrpSeguranca"] = "SEGURANÇA",
                ["ChkBloquearLogs"] = "Bloquear arquivos de log",
                ["TxtAvisoLogs"] = "Aviso: somente logs client-side são bloqueados",
                ["GrpIdiomas"] = "IDIOMAS",
                ["GrpTema"] = "TEMA",
                ["RadioClaro"] = "Claro",
                ["RadioEscuro"] = "Escuro",
                ["GrpIconeBandeja"] = "ÍCONE DA ÁREA DE NOTIFICAÇÃO",
                ["ChkMinimizarBandeja"] = "Minimizar para a área de notificação",
                ["ChkIniciarMinimizado"] = "Iniciar minimizado",
                ["ChkIniciarComWindows"] = "Iniciar com o Windows",
                ["GrpAtualizacoes"] = "ATUALIZAÇÕES",
                ["ChkBuscarAtualizacoes"] = "Buscar por atualizações ao iniciar",
                ["ChkBetaApenas"] = "Beta apenas",
                ["TxtAvisoBetaApenas"] = "Desmarcado: o programa busca apenas versões estáveis"
            },
            ["en"] = new()
            {
                ["MenuAcoes"] = "Actions",
                ["MenuConfiguracoes"] = "Settings",
                ["MenuSobre"] = "About",
                ["AcaoReiniciar"] = "Restart Program",
                ["AcaoLimparCache"] = "Clear Cache",
                ["AcaoLimparConfiguracoes"] = "Clear Settings",
                ["AcaoExcluirMods"] = "Delete All Mods",
                ["BtnGerenciarMods"] = "Manage Mods",
                ["BtnAplicarJogar"] = "Apply and Play",
                ["BtnExecutarLauncher"] = "Run Launcher",
                ["ModsAtivos"] = "Active Mods",
                ["ListaVazia"] = "No active mods. Use \"Manage Mods\" to import.",
                ["Ligado"] = "ON",
                ["Desligado"] = "OFF",
                ["TituloConfiguracoes"] = "Settings",
                ["AbaElsword"] = "Elsword",
                ["AbaInicializador"] = "Launcher",
                ["BotaoOk"] = "OK",
                ["BotaoCancelar"] = "Cancel",
                ["BotaoAplicar"] = "Apply",
                ["TituloGerenciarMods"] = "Mods",
                ["AbaGeral"] = "General",
                ["AbaBgm"] = "BGM",
                ["AbaVideo"] = "Video",
                ["TituloSobre"] = "About ElsEvo",
                ["SobreDescricao"] = "ElsEvo is an application that automates the process of modifying the game Elsword. Compatible with mods created by the players themselves, it also offers support for voice packs and other customizations. Created so players could relive the days when the Brazilian Elsword server was still online, bringing back that experience with a modern, fast, and safer tool.",
                ["SobreAutor"] = "Author:",
                ["GrpLocalizacaoJogo"] = "GAME LOCATION",
                ["GrpOpcoesInicializacao"] = "STARTUP OPTIONS",
                ["ChkNaoExecutarLauncher"] = "Don't run the launcher",
                ["TxtRecomendadoCoreano"] = "(recommended for the Korean server)",
                ["ChkPularElsword"] = "Skip elsword.exe (advanced)",
                ["GrpSeguranca"] = "SECURITY",
                ["ChkBloquearLogs"] = "Block log files",
                ["TxtAvisoLogs"] = "Warning: only client-side logs are blocked",
                ["GrpIdiomas"] = "LANGUAGE",
                ["GrpTema"] = "THEME",
                ["RadioClaro"] = "Light",
                ["RadioEscuro"] = "Dark",
                ["GrpIconeBandeja"] = "NOTIFICATION AREA ICON",
                ["ChkMinimizarBandeja"] = "Minimize to notification area",
                ["ChkIniciarMinimizado"] = "Start minimized",
                ["ChkIniciarComWindows"] = "Start with Windows",
                ["GrpAtualizacoes"] = "UPDATES",
                ["ChkBuscarAtualizacoes"] = "Check for updates on startup",
                ["ChkBetaApenas"] = "Beta only",
                ["TxtAvisoBetaApenas"] = "Unchecked: the program only looks for stable releases"
            },
            ["zh"] = new()
            {
                ["MenuAcoes"] = "操作",
                ["MenuConfiguracoes"] = "设置",
                ["MenuSobre"] = "关于",
                ["AcaoReiniciar"] = "重启程序",
                ["AcaoLimparCache"] = "清除缓存",
                ["AcaoLimparConfiguracoes"] = "重置设置",
                ["AcaoExcluirMods"] = "删除所有模组",
                ["BtnGerenciarMods"] = "管理模组",
                ["BtnAplicarJogar"] = "应用并游玩",
                ["BtnExecutarLauncher"] = "运行启动器",
                ["ModsAtivos"] = "已启用的模组",
                ["ListaVazia"] = "没有已启用的模组。使用“管理模组”导入。",
                ["Ligado"] = "开启",
                ["Desligado"] = "关闭",
                ["TituloConfiguracoes"] = "设置",
                ["AbaElsword"] = "Elsword",
                ["AbaInicializador"] = "启动器",
                ["BotaoOk"] = "确定",
                ["BotaoCancelar"] = "取消",
                ["BotaoAplicar"] = "应用",
                ["TituloGerenciarMods"] = "模组",
                ["AbaGeral"] = "常规",
                ["AbaBgm"] = "背景音乐",
                ["AbaVideo"] = "视频",
                ["TituloSobre"] = "关于 ElsEvo",
                ["SobreDescricao"] = "ElsEvo 是一款自动化 Elsword 游戏修改流程的应用程序。兼容玩家自制的模组,同时支持配音包和其他自定义内容。它的诞生是为了让玩家们重温巴西 Elsword 服务器仍在运行的那段时光,用一款现代、快速且更安全的工具带回那份体验。",
                ["SobreAutor"] = "作者:",
                ["GrpLocalizacaoJogo"] = "游戏位置",
                ["GrpOpcoesInicializacao"] = "启动选项",
                ["ChkNaoExecutarLauncher"] = "不运行启动器",
                ["TxtRecomendadoCoreano"] = "(推荐用于韩国服务器)",
                ["ChkPularElsword"] = "跳过 elsword.exe (高级)",
                ["GrpSeguranca"] = "安全",
                ["ChkBloquearLogs"] = "阻止日志文件",
                ["TxtAvisoLogs"] = "警告:仅阻止客户端日志",
                ["GrpIdiomas"] = "语言",
                ["GrpTema"] = "主题",
                ["RadioClaro"] = "浅色",
                ["RadioEscuro"] = "深色",
                ["GrpIconeBandeja"] = "通知区域图标",
                ["ChkMinimizarBandeja"] = "最小化到通知区域",
                ["ChkIniciarMinimizado"] = "启动时最小化",
                ["ChkIniciarComWindows"] = "随 Windows 启动",
                ["GrpAtualizacoes"] = "更新",
                ["ChkBuscarAtualizacoes"] = "启动时检查更新",
                ["ChkBetaApenas"] = "仅测试版",
                ["TxtAvisoBetaApenas"] = "取消勾选:程序只查找稳定版本"
            }
        };

        public static string T(string chave)
        {
            string idioma = Properties.Settings.Default.Idioma;
            if (!_textos.TryGetValue(idioma, out var dicionario))
                dicionario = _textos["pt"];

            return dicionario.TryGetValue(chave, out var texto) ? texto : chave;
        }

        public static void DefinirIdioma(string codigoIdioma)
        {
            Properties.Settings.Default.Idioma = codigoIdioma;
            Properties.Settings.Default.Save();
            IdiomaMudou?.Invoke();
        }
    }
}
