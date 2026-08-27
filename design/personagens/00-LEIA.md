# PERSONAGENS — elenco (20 magos: 10 de lançamento + 10 de temporadas)

> Um arquivo por personagem. **A arte é do Diretor**: quando criar a arte de um
> personagem, solte os arquivos na subpasta `arte/` dele (crie se não existir) —
> ex.: `personagens/01-pyra/arte/concept-frente.png` — e a equipe usa a partir
> daí para a modelagem 3D (Godot, régua Spellbreak).
>
> **01–10**: elenco de LANÇAMENTO — kits oficiais no GDD §3 (fonte da verdade).
> Em **20/08** o Diretor mandou DESCOLAR do Apex: as lendas viraram ponto de
> partida, não gabarito. Cada mago ganhou identidade mágica própria (cicatriz,
> membro/órgão substituído por magia, pacto, maldição) e o kit reescrito a partir
> dela — GDD §3 já revisado junto.
> **11–20**: elenco de TEMPORADAS (raças fantásticas, ordem do Diretor 19/08) —
> kits são PROPOSTA DA EQUIPE até o Diretor aprovar; quando aprovar, sobem ao GDD.
>
> Arte que ajuda a modelagem: frente/costas/lado em T-pose ou A-pose; paleta em
> qualquer canto; qualquer formato. O que vier, a equipe aproveita.

## Funções (a taxonomia do Diretor)
**Suporte/Vida** · **Ataque/Perseguição** · **Longa distância** · **Curta distância**

## Status das artes (25/08/2026)

**Os 20 magos têm conjunto completo de arte.** O Diretor entregou em 24/08 um lote
novo com 8 vistas por personagem, substituindo o retrato único anterior.

### Onde cada coisa vive

| O quê | Onde | No git? |
|---|---|---|
| As 8 vistas de concept (frente, 3/4, lateral, costas, close de busto, equipamento isolado, marcas isoladas, paleta) | `personagens/NN-slug/arte/_originais/` | **Não** — 307 MB, é ateliê local |
| Paleta em vetor | `personagens/NN-slug/arte/paleta.svg` | Sim |
| Prompt de geração | `personagens/NN-slug/arte/prompt.txt` | Sim |
| O retrato que o jogo usa (512px) | `godot/menu/art/NN.png` | Sim |

O ateliê fica fora do git pelo mesmo motivo dos modelos 3D: é pesado e é
matéria-prima, não produto. O produto é o retrato de 512px que entra no APK.
A arte da direção anterior foi removida do disco em 25/08 e vive no commit
`9643113` — para recuperar uma:
`git show 9643113:personagens/01-pyra/arte/concept.png > saida.png`

### Manifestos e armazenamento (25/08/2026)

Cada personagem tem `arte/manifesto.md` com as oito vistas, dimensoes e SHA-256
**do que esta' em disco**. Foram gerados por inspecao, e os 160 PNGs abrem sem
erro — nenhum corrompido, nenhum faltando.

**O que NAO temos, e por isso a norma de arte segue aberta:**

1. o **documento mestre** da nova direcao aprovada pelo Diretor;
2. o **`MANIFESTO.md`** e o **`SHA256SUMS.txt`** da entrega original.

Sem o item 2 os hashes acima descrevem o que temos, mas **nao confirmam** que e'
o aprovado. Sem o item 1 nao da' para afirmar que estas oito vistas sao as
canonicas da direcao nova.

**Os 10 zips da entrega nao existem mais**: foram apagados por mim na
higienizacao de 25/08 (commit `6e9836e`) depois de conferir por MD5 que os 180
arquivos eram byte a byte identicos ao disco. Nada se perdeu, mas a redundancia
acabou — hoje os 160 PNGs existem em UM lugar so'.

**Onde eles vao morar e' DECISAO DO DIRETOR** (Git LFS ou armazenamento externo
duravel). As duas rotas estao descritas em `docs/ART.md`; nenhuma foi escolhida
pela equipe.

### O que a auditoria de 25/08 mediu

**O elenco tem três linguagens visuais diferentes, não uma.**

| Linguagem | Quantos | Quem |
|---|---|---|
| Escultura 3D (o que o Diretor aprovou) | 7 | 04, 09, 13, 14, 16, 17, 18 |
| Pintura semi-realista | 7 | 01, 03, 05, 06, 07, 10, 12 |
| Anime / manhwa | 6 | 02, 08, 11, 15, 19, 20 |

> ⚠️ **CORRIGIDO EM 27/08/2026 — o paragrafo abaixo NAO vale como regra geral.**
> Conferido imagem a imagem: o perfil da **Veu** e' um **90 graus de verdade**
> (um olho, uma orelha, nenhum peito visivel); o do **Basalto** e' mesmo um 3/4.
> E' **caso a caso**. Repetir esta frase sem abrir o arquivo levou a equipe a
> escrever 20 prompts para REFAZER do zero uma arte que existia desde 24/08 —
> e o Diretor teve que perguntar *"por que isso dos personagens se eles existem
> dentro da pasta do projeto?"*. **Antes de citar isto, abra o PNG.**

**Defeito de vistas, vale para os 20:** a `vista-3-4` é a frontal repetida em
12 de 12 conferidos, e a `vista-lateral` é um três-quartos de ~60-70°. **Não
existe perfil de 90° no lote.** Consequência prática: o fluxo multi-imagem da
Meshy recebe 4 imagens e aproveita 2 ângulos, perdendo justamente a espessura
lateral do corpo. Corrigir isso vale mais que a discussão de estilo e custa uma
fração do preço.

**Divergências entre arte e ficha** (a ficha é a fonte da verdade do
personagem; onde divergem, a arte é que está errada): o kintsugi da Ceifadora
virou malha regular em vez de porcelana rachada; Véu aparenta 25 anos quando a
ficha diz que ela voltou aos 19 sem ter envelhecido; faltam as lâminas gêmeas
da Umbra e os sinos do Olho-de-Éter (que são como ele percebe o próprio andar);
os olhos de Maris vieram com esclera branca quando deveriam ser verde-mar
inteiros. Mais fiéis à ficha: Tessa e Vex.

**A direção de arte está PENDENTE DE DECISÃO do Diretor** — o eixo não é
"realismo" (o rosto do Brok, aprovado, é mais realista que o da Pyra,
rejeitada), e sim superfície pintada × esculpida, sombra de contato e
proporção exagerada. Ver `docs/ART.md`.

| # | Personagem | Raça | Função | O que o torna único |
|---|---|---|---|---|
| 01 | Pyra, a Chama de Guerra | Humana | Ataque / média dist. | Braço esquerdo de chama viva (perdeu o de carne salvando a legião) |
| 02 | Ceifadora, a Voz do Vazio | Humana (tocada pelo Vazio) | Ataque / Perseguição | Morreu e voltou; não projeta sombra; rachaduras emendadas em luz |
| 03 | Véu, a Andarilha | Humana (planos) | Perseguição / fuga | 7 anos no plano espectral sem envelhecer; mão esquerda semi-espectral |
| 04 | Corvus, o Caçador | Humano tribal | Ataque / Perseguição | Cego; "vê" cheiros como cores pelo espírito-lobo que o cegou |
| 05 | Corvomante, o Olho Distante | Humano | Longa distância / recon | Trocou o olho direito: o corvo o carrega (Odin) |
| 06 | Olho-de-Éter, o Observador | Humano | Longa distância / recon | Surdo; as mariposas-de-éter são os ouvidos dele |
| 07 | Vitalis, a Mão que Cura | Humana | Suporte / Vida | A fada Lúmen é a irmã gêmea, presa entre mundos |
| 08 | Ilusionista, o Espelho | Humano | Suporte / engano | Saiu invertido de 3 anos preso num espelho; um reflexo é o original |
| 09 | Vex, o Alquimista da Peste | Humano | Curta dist. / área | Sem pulmões: um fole alquímico no peito respira por ele |
| 10 | Tessa, a Tecelã de Raios | Humana | Curta dist. / defesa | Cicatrizes de Lichtenberg + marca-passo rúnico forjado por ela |
| 11 | Aelion, o Arco do Crepúsculo | Alto Elfo | **Longa distância** | — |
| 12 | Umbra, a Lâmina da Noite | Elfa Negra (Drow) | **Ataque / Perseguição** | — |
| 13 | Brok, o Ferreiro de Runas | Anão | **Suporte / Proteção** | — |
| 14 | Gromm, o Xamã da Tempestade | Orc | **Suporte / Vida** | — |
| 15 | Maris, a Voz das Marés | Nereida | **Longa distância** | — |
| 16 | Fizz, o Artífice de Bolso | Gnomo | **Longa distância** | — |
| 17 | Sylva, a Filha da Floresta | Dríade | **Suporte / Vida** | — |
| 18 | Basalto, o Desperto | Golem rúnico | **Curta distância** | — |
| 19 | Noctus, o Sedento de Éter | Vampiro arcano | **Ataque / Perseguição** | — |
| 20 | Pip, a Centelha Selvagem | Fada | **Ataque / Perseguição** | — |

Regra 10+ que vale para TODOS: sem sangue/gore (o Noctus drena ÉTER, não sangue);
counter sempre em cor + FORMA + SOM (GDD §10); todo poder tem preço (⚖️).
