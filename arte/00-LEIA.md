# arte — a matéria-prima

Concepts, modelos, áudio e as ferramentas que os produzem. **Nada aqui é
específico de engine**, e é por isso que esta pasta sobreviveu ao Godot, ao
Unreal e alimenta o Unity.

| Pasta | O que é |
|---|---|
| `personagens/` | as 8 vistas de concept dos 20 magos (160 PNGs) |
| `cenario/` | concepts e `.glb` do castelo, luvas, manopla, baú, ruínas, torre, altar, e o pacote da Ilha Fraturada |
| `audio/` | vozes |
| `prompts/` | prompts de geração |
| `tools/` | `meshy/` (API, só para props) e `blender/` (fusão, decimação) |

## Diretrizes

**A arte é do Diretor.** Ele solta os arquivos na subpasta do personagem ou do
cenário; a equipe usa a partir dali.

**Guardar o ORIGINAL, sempre.** As peças do Meshy vêm com ~3 milhões de faces e
textura 2K. Celular precisa de 800 a 30 mil faces: a decimação é feita no
Blender (`tools/blender/otimizar.py`) e a versão reduzida é **derivada**.
Derivada se refaz; original não. Os originais ficam em
`cenario/ilha-fraturada/_originais-3d/` (fora do git, 1,1 GB).

**Para o Unity, baixe FBX do Meshy quando houver esqueleto.** O Unity importa
FBX nativamente e retargeta animação pelo rig Humanoid. Para `.glb` sem
esqueleto (props, cenário) o pacote glTFast resolve, mas FBX é o caminho de
menor atrito para personagem animado.

**Antes de mandar gerar de novo, ABRIR O ARQUIVO.** Em 27/08 a equipe escreveu 20
prompts para refazer do zero uma arte que existia desde 24/08, porque repetiu um
achado de auditoria sem conferir. A auditoria dizia "não existe perfil de 90° no
lote"; o perfil da Véu é um 90° de verdade, o do Basalto não. Era caso a caso.

**`tools/meshy/meshy.py` RECUSA criar personagem pela API** — a API não tem a
etapa de marcação de articulações do rig, e o resultado anima como robô.
Personagem se cria no site, um por um. `prop` (cenário) segue liberado.

## Blender + Claude Code (24/09/2026)

Blender 5.2.1 em `C:\Program Files\Blender Foundation\Blender 5.2\blender.exe` (fora do PATH).
Duas pontes, as duas provadas:

- **Script (reproduzível):** `blender.exe -b --python tools/blender/<script>.py -- args`. É o caminho
  para tudo que é determinístico (decimar, fundir, medir, exportar).
- **MCP (conversa com a cena aberta):** add-on `mcp-for-blender` (Ahuja, ex-`blender-mcp`) instalado em
  `%APPDATA%\Blender Foundation\Blender\5.2\scripts\addons\blender_mcp.py` e ativado; servidor registrado
  no Claude Code em escopo de usuário (`claude mcp get blender`), com `BLENDER_MCP_SAFE_MODE=1`. Para usar:
  abrir o Blender, painel `N` → aba **MCP for Blender** → **Start MCP Server** (porta 9876, só localhost).
  A tool `execute_blender_code` roda Python dentro do Blender: nunca aponte para o master, sempre para cópia.

Os 20 magos já vêm rigados da Meshy (28 ossos, 13–15 clipes); o Blender entra para retoque, escala/eixo,
retarget entre magos e clipe pontual — não para refazer rig. `tools/blender/fundir_animacoes.py` precisa ser
revalidado no 5.x antes de qualquer uso (mudança de API das ações).
