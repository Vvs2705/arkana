# arte — a matéria-prima

Concepts, modelos, áudio e as ferramentas que os produzem. **Nada aqui é
específico de engine**, e é por isso que esta pasta alimenta as três bases.

| Pasta | O que é |
|---|---|
| `personagens/` | as 8 vistas de concept dos 20 magos (160 PNGs) |
| `cenario/` | concepts e `.glb` do castelo, luvas, manopla, baú, ruínas, torre, altar, e o pacote da Ilha Fraturada |
| `audio/` | vozes |
| `prompts/` | prompts de geração |
| `tools/` | `meshy/` (API) e `blender/` (fusão, otimização) |

## Diretrizes

**A arte é do Diretor.** Ele solta os arquivos na subpasta do personagem ou do
cenário; a equipe usa a partir dali.

**Guardar o ORIGINAL, sempre.** As peças do Meshy vêm com ~3 milhões de faces e
textura 2K. O mobile precisou decimar para 800–4.000 faces e 512 — mas o que se
guarda aqui é o original, porque o Unreal com Nanite usa quase cru. Versão
reduzida é derivada, e derivada se refaz.

**Antes de mandar gerar de novo, ABRIR O ARQUIVO.** Em 27/08 a equipe escreveu 20
prompts para refazer do zero uma arte que existia desde 24/08, porque repetiu um
achado de auditoria sem conferir. A auditoria dizia "não existe perfil de 90° no
lote"; o perfil da Véu é um 90° de verdade, o do Basalto não. Era caso a caso.

**`tools/meshy/meshy.py` RECUSA criar personagem pela API** — a API não tem a
etapa de marcação de articulações do rig, e o resultado anima como robô.
Personagem se cria no site, um por um. `prop` (cenário) segue liberado.
