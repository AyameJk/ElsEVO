"""
Gera o version.json a partir dos dados da Release publicada no GitHub.

Antes esse código vivia como um heredoc Bash dentro do workflow (.yml), o que
quebrava intermitentemente em runners Windows por causa de CRLF no arquivo do
workflow (o marcador de fechamento do heredoc, "PYEOF", virava "PYEOF\r" e o
bash deixava de reconhecer o fim do heredoc). Como script de verdade, isso não
importa mais: o interpretador Python lida com CRLF/LF igual (universal
newlines), então não tem mais nenhuma sintaxe sensível a terminação de linha
no meio do caminho.

Variáveis de ambiente esperadas (definidas no step do workflow):
    TAG_NAME      -- tag da Release publicada (ex.: "v1.0.4")
    RELEASE_NOTES -- corpo/descrição da Release (pode vir vazio)
    REPO          -- "owner/repo", ex.: "AyameJk/ElsEvo"
"""

import json
import os


def main() -> None:
    tag = os.environ["TAG_NAME"]
    versao = tag[1:] if tag.startswith("v") else tag

    # Mesmo formato/nome de asset de sempre -- case-sensitive, tem que bater
    # exatamente com OutputBaseFilename no .iss.
    repo = os.environ["REPO"]
    url_instalador = f"https://github.com/{repo}/releases/download/{tag}/ElsEvo-Setup.exe"

    dados = {
        "versao": versao,
        "url": url_instalador,
        "notas": os.environ.get("RELEASE_NOTES") or "",
    }

    with open("version.json", "w", encoding="utf-8") as f:
        json.dump(dados, f, indent=2, ensure_ascii=False)
        f.write("\n")

    print(f"version.json atualizado para a versão {versao} ({url_instalador})")


if __name__ == "__main__":
    main()
