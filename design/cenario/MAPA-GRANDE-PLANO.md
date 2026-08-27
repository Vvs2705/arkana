# MAPA GRANDE — plano de execução da ilha de 2,4 km

> **A DECISÃO JÁ FOI TOMADA.** Diretor, 27/08/2026, verbatim: *"1º C — vamos
> começar a desenhar um mapa muito maior, mais explorável e com maior chance de
> termos sucesso, e desenhar ela no Meshy para garantir uma qualidade maior,
> mesmo que fuja do nosso plano inicial de consumo."*
>
> É a opção **(C)** de `docs/cenario/ILHA-FRATURADA-VALIDACAO.md` §6.1: envelope
> de **2,4 × 2,2 km**, **3,4 km² de terra**. Ele reafirmou depois de ler os
> números contra. **Este documento não rediscute a decisão.** Ele diz o que ela
> exige, em que ordem, e qual medição decide cada passo.
>
> **Regras deste documento:** nenhum `.gd` foi tocado; nada foi commitado. Todo
> número ou é **MEDIDO** (com o caminho do arquivo ao lado) ou vem marcado
> **PROPOSTA** com a conta à vista.

---

## 1. O QUE A DECISÃO EXIGE — cinco linhas, sem drama

1. **Streaming de terreno (chunking).** Hoje a ilha inteira nasce em `_ready()`
   → `_build()` (`godot/world/Island.gd:215` e `:231`), uma malha só, e vive na
   memória. A 2,4 km isso é 2,2 M triângulos e ~40 s de carga. Não existe.
2. **LOD de terreno.** A queda começa a 320 m de altura (`godot/world/Castelo.gd:28`)
   e o frustum pega o mapa inteiro. Sem malha grossa de longe, a queda desenha
   tudo. Não existe.
3. **Colisão por região.** O chão usa `create_trimesh_shape()` sobre a malha
   inteira (`Island.gd:543`). Uma `ConcavePolygonShape3D` de 2,2 M triângulos
   custa dezenas de MB e uma BVH que se constrói no load. Não existe.
4. **Culling da decoração.** Só a grama passa pelo `_multimesh_grid`
   (`Island.gd:1053`); rochas, pedrinhas, moitas, flores, juncos e as árvores
   são **um MultiMesh único que nunca é cortado pelo frustum** — o comentário do
   próprio arquivo diz isso (`Island.gd:1363-1366`). Não existe.
5. **Rede.** 3,4 km² só faz sentido com 20 jogadores, e o jogo não tem uma linha
   de rede (`docs/infra/01-MULTIJOGADOR.md` §1: "um processo só, no aparelho").
   Não existe.

**E o portão zero, que não é uma dessas cinco:** o FPS no Poco F4 **nunca foi
medido** (`docs/PROJETO.md:301` e `:646-647` — "nenhum aparelho apareceu em
`adb devices`"). Nada abaixo tem régua enquanto esse número não existir.

---

## 2. A ESTRATÉGIA DE TERRENO — o coração deste documento

### 2.1 O que existe hoje, medido

O cabeçalho do `Island.gd` traz a medição feita com `world/_shot.gd` (não
chutada — `Island.gd:15-19`):

```
SIZE 300 | 133×133 verts | 34.848 tris de chão | 220.986 no mundo
         | 85 nós desenháveis | PIOR quadro 30 draw calls / 159k prims | 621 ms
```

Disso saem as duas unidades de custo que este plano usa o tempo inteiro:

| Unidade | Conta | Valor |
|---|---|---|
| **Custo por vértice de chão** | 621 ms ÷ (133×133 = 17.689 verts) | **0,0351 ms/vert** |
| **Custo de uma chamada de `height()`** | 621 ms ÷ ~230.000 chamadas (`Island.gd:329`) | **~2,7 µs** |

O vértice custa 13 chamadas de `height()` porque `_terrain_ao` (`:483`) e a
normal (`:510`) reamostram a vizinhança. Esse é o número que manda.

### 2.2 A arquitetura proposta: chunk simples, três anéis, uma malha grossa

> **PROPOSTA.** Escolhida por ser a coisa mais simples que resolve — nada de
> quadtree adaptativa, nada de geomorphing, nada de octree. Três anéis fixos e
> uma malha de fundo.

**O chunk.** Lado de **150 m**, **33 quads por lado** → quad de **4,55 m**.

*Por que 4,55 m:* é exatamente o quad que `ILHA-FRATURADA-VALIDACAO.md` §3
(ONDA 2) já provou passar no Nyquist — o detalhe mais fino que a malha precisa
carregar é a ondulação curta de ~12 m (`Island.gd:296-301`), cujo Nyquist é 6 m
de amostragem. 4,55 m passa com folga; 6,06 m (o quad de 800 m com `QUADS 132`)
cruza a linha.

*Por que 150 m:* é o maior chunk que o custo por vértice deixa gerar em uma
fatia razoável de trabalho, e a 7,5 m/s (`godot/core/Balance.gd:15`) o jogador
leva **20 s** para atravessar um — folga enorme para a thread de fundo.

| Por chunk | Conta | Valor |
|---|---|---|
| Vértices | 34 × 34 | **1.156** |
| Triângulos | 33² × 2 | **2.178** |
| Tempo de geração | 1.156 × 0,0351 ms | **~41 ms** |
| Memória da malha | 1.156 × 40 B + 2.178×3×4 B | **~72 KB** |
| Memória com colisão trimesh | + faces e BVH | **~230 KB** |

O envelope de 2,4 km cabe em **16 × 16 = 256 chunks**.

**Os três anéis** (PROPOSTA — os raios são knobs, não leis):

| Anel | Chunks | Quad | Tris | Colisão | Envelope | Geração |
|---|---:|---:|---:|:---:|---:|---:|
| **L0** — chão que se pisa | 3×3 = 9 | 4,55 m | 19.602 | **SIM** | 450 m | 366 ms |
| **L1** — chão que se vê | anel de 16 | 9,1 m | 9.248 | não | 750 m | 182 ms |
| **L2** — a ilha inteira, grossa | 1 malha | 18,2 m | 34.848 | não | 2.400 m | 621 ms |

**L2 é a peça mais importante desta tabela e a mais barata de explicar:** uma
malha de quad 18,2 m sobre 2.400 m dá 132 quads por lado — ou seja,
**exatamente a malha que o jogo tem hoje**, com os mesmos 34.848 triângulos e os
mesmos 621 ms. Ela é gerada uma vez no load, nunca é liberada, não tem colisão,
e é **o que a queda vê**. O mapa de 2,4 km custa, de longe, o mesmo que o mapa
de 300 m custa hoje de perto.

### 2.3 Os números que decidem — chunk vs. malha única

| | Malha única a 2,4 km | **Chunk proposto** | Hoje (300 m) |
|---|---:|---:|---:|
| Triângulos de chão (pior quadro) | 2.235.000 | **63.698** | 34.848 |
| … como fração do MUNDO de hoje (220.986) | 10× | **29 %** | 16 % |
| Memória de malha de terreno | ~72 MB | **~3,5 MB** | ~1,1 MB |
| Memória de colisão | dezenas de MB + BVH | **~1,4 MB** (só L0) | ~2 MB |
| Tempo de carga do chão | **~40 s** | **~1,2 s** | 621 ms |
| Triângulos de colisão | 2.235.000 | **19.602** | 34.848 |

Leia a última linha: **a colisão do mapa de 2,4 km fica 44 % mais barata que a
do mapa de 300 m.** Não é otimismo — é o que "colisão por região" significa.

O custo de streaming em partida: andando em linha reta a 7,5 m/s, a troca de
anel pede no máximo 5 chunks novos a cada 20 s → 5 × 41 ms = **205 ms de
trabalho a cada 20 s**, ou ~1 % de um núcleo. **Regra dura: nada disso na thread
principal.** A geração vai para `WorkerThreadPool`; a única coisa que roda na
main é o `add_child` da malha já pronta.

### 2.4 O contrato de `height()` — a parte que NÃO pode mudar

`Island.height(x, z)` (`Island.gd:276`) é uma **função pura**: lê `_noise` de
seed fixo (`:170`) e devolve a cota. Ela não depende de nó nenhum, de malha
nenhuma, de chunk nenhum.

Isso não é sorte, é a propriedade mais valiosa do arquivo, e é o que torna esta
proposta barata. Conferi os quatro consumidores, um a um:

| Quem chama | Onde | Precisa de chunk carregado? |
|---|---|---|
| `Queda` (o pouso) | `godot/gameplay/queda/Queda.gd:363-364` | **não** — escreve a posição e lê `height()` |
| `Loot` (onde a arma nasce) | `godot/gameplay/Loot.gd:325` | **não** |
| `Zona` (onde o círculo cai) | `godot/gameplay/Zona.gd:194` | **não** |
| `TerrainSystem` (a grade do fogo) | `godot/terrain/TerrainSystem.gd:197` | **não** |

**Conclusão, e é a boa notícia deste documento: o chunking é INVISÍVEL para os
quatro sistemas.** `height()` continua respondendo para qualquer x,z, inclusive
dentro de um chunk que nunca foi gerado — porque ela nunca leu o chunk. A malha
é uma **aproximação** de `height()`; `height()` é a verdade.

**A regra que fica escrita a sangue:** se algum dia alguém trocar `height()` por
uma consulta a um campo de altura guardado por chunk (a otimização que o próprio
`Island.gd:29-31` já cogita), **os quatro sistemas acima quebram no mesmo
commit**. Se essa otimização entrar, ela entra como *cache por trás da mesma
assinatura*, nunca como API nova.

### 2.5 Costura, pawns fora do envelope e decoração

**Costura entre LODs.** L0 e L2 discordam na cota (o quad de 18,2 m perde a
ondulação de 12 m, ±0,5 m). Isso vira fresta de céu no horizonte. Solução
padrão e barata: **saia vertical de 1,5 m** na borda de cada chunk. Custo:
4 × 33 × 2 = 264 tris por chunk, **+12 %**. Não vale inventar nada mais esperto.

**Pawns fora do envelope L0.** `Pawn` é `CharacterBody3D`: sem colisão embaixo,
cai para sempre. A saída barata **já existe no repo** e não é código novo — é o
que `Queda.gd` faz o tempo inteiro (`:363`): escrever a posição e ler `height()`.
**PROPOSTA:** bot fora do envelope L0 anda em modo cinemático sobre `height()` e
volta à física ao cruzar a borda. Efeito colateral bem-vindo: num mapa grande a
maioria dos bots está longe, então **19 bots num mapa de 2,4 km custam menos
física que 6 bots num mapa de 300 m**.

**Decoração.** `_multimesh_grid` (`Island.gd:1375`) **já é a resposta e já está
escrita** — só a grama a usa. Sem passar as outras famílias por ela, o fator de
62× de área (`ILHA-FRATURADA-VALIDACAO.md` §2) vira isto:

| Família | Hoje | Mesma densidade a 3,4 km² | Cortada pelo frustum hoje? |
|---|---:|---:|:---:|
| Árvores (`Island.gd:653` + `:671`) | 176 | ~10.900 | **não** |
| Rochas (`:917`) | 58 | ~3.600 | **não** |
| Pedrinhas (`:1106`) | 380 | ~23.600 | **não** |
| Moitas (`:1146`) | 260 | ~16.100 | **não** |
| Flores (`:1211`) | 480 | ~29.800 | **não** |
| Grama (`:1028`) | 30.000 tufos | — | **sim** (`vis_end` 80 m) |

*(Nota de higiene: `docs/PROJETO.md` §3.5 fala em "158 árvores"; o código pede
150 na mata + 26 avulsas = **176**. O número do doc envelheceu.)*

**PROPOSTA:** decoração **nasce e morre com o chunk**, e cada família vira um
MultiMesh por chunk. O custo por quadro deixa de depender do tamanho do mapa e
passa a depender só do envelope L0 — que é fixo. **A grama já prova que isso
funciona:** hoje ela tem 30.000 tufos e só ~2.500–3.300 caem no frustum
(`Island.gd:1011`), porque o corte de 80 m manda. Num mapa 62× maior, o custo de
grama por quadro é **exatamente o mesmo**.

### 2.6 A regra do "procedural e sem binário" — o que sobrevive e o que muda

**Sobrevive inteira, e é o que faz a proposta caber:** o chunk é gerado pela
`height()` de seed fixo dentro da sua footprint. **Nenhum arquivo. Nenhum
binário. Nenhum heightmap salvo.** A ilha continua sendo sempre a MESMA
(`Island.gd:5-7`), e o chunk 137 gerado hoje é idêntico ao chunk 137 gerado
daqui a um ano. Determinismo intacto, spawns confiáveis intactos.

**O que muda quando o Meshy entra (§4):** o **kit modular traz `.glb`**, e isso
é binário no repo. A linha que separa as duas coisas é a mesma que
`docs/DIRECAO.md` §10 já traçou e que o Diretor já aprovou: **o terreno —
altura, colisão, água, praia, cor — nunca sai do código; o Meshy faz o que fica
EM CIMA dele.** Este plano não move essa linha um centímetro.

---

## 3. AS FIAÇÕES QUE ARREBENTAM AO MUDAR `SIZE`

Boa notícia primeiro: **duas das quatro já foram consertadas** e o
`ILHA-FRATURADA-VALIDACAO.md` §2(d) está desatualizado a respeito delas. Má
notícia: **as outras duas estão quebradas AGORA, em 300 m**, e ninguém percebeu.

### 3.1 ZONA — a geometria escala; os TEMPOS não

**O que já funciona.** `Zona.escala_do_mapa()` (`Zona.gd:132-138`) lê
`Island.SIZE` em runtime e multiplica todos os raios por `SIZE / ILHA_REF`
(`:54`, ILHA_REF = 180). A 2,4 km o fator é **13,33** e o raio de abertura sai
correto: 90 × 13,33 = **1.200 m** = meia-ilha. Nada a fazer aqui.

**O que quebra, e são três coisas:**

**(a) Os tempos.** A tabela `FASES` (`:88-94`) soma **165 s** de espera+fechamento,
calibrada contra `Balance.MATCH.duration_s = 180` (`godot/core/Balance.gd:291`).
Os tempos **não** entram na multiplicação — e não devem mesmo: tempo não é
distância. Mas a 2,4 km eles ficam absurdos: a fase 1 fecharia de 1.200 m para
827 m em 20 s, uma parede andando a 18,6 m/s contra um jogador de 7,5 m/s.

**(b) O círculo final.** 4 m × 13,33 = **53 m de raio**. O duelo final passa a
acontecer numa arena de 106 m de diâmetro — três vezes o alcance da arma mais
longa (38 m, `Balance.gd:67`). Deixa de ser duelo.

**(c) A janela de cota do sorteio.** `_sortear_centro` (`:195`) só aceita centro
com `h >= 1.4 and h <= 8.5`, e desiste depois de 12 tentativas devolvendo o
último palpite — **possivelmente no mar**. Isso já falha hoje: o platô das
ruínas está em 9,0 m (`Island.gd:318`) e o pico chega a 28 m (`Island.gd:81`),
ou seja, **nenhum círculo da zona pode cair nos dois POIs mais altos do mapa**.

**Correção proposta — em fórmula, não em número novo cravado:**

- **(a)+(b):** tabela nova de **7 fases**, escrita na mesma régua de `ILHA_REF`
  (o mecanismo de escala continua sendo o de hoje, só a tabela muda), para uma
  partida **PROPOSTA de 800 s (13,3 min)** — dentro dos 13–16 min da
  especificação, e a mais curta que o tamanho comporta.

| Fase | espera (s) | fecha (s) | raio real a 2,4 km | raio na régua 180 | dps |
|---:|---:|---:|---:|---:|---:|
| abertura | — | — | 1.200 m | 90,0 | — |
| 1 | 120 | 60 | 800 m | 60,0 | 1,5 |
| 2 | 90 | 55 | 540 m | 40,5 | 3,0 |
| 3 | 75 | 50 | 350 m | 26,3 | 5,0 |
| 4 | 60 | 45 | 210 m | 15,8 | 8,0 |
| 5 | 45 | 40 | 110 m | 8,3 | 12,0 |
| 6 | 30 | 30 | 50 m | 3,8 | 16,0 |
| 7 | 20 | 20 | 14 m | 1,1 | 22,0 |

  **Soma: 740 s.** Com 800 s de partida sobram **60 s** para o duelo final no
  círculo de 14 m — a mesma ideia dos 15 s que sobram hoje (`Zona.gd:82-86`),
  na proporção certa (7,5 % contra 8,3 %).

  **Conferência de sobrevivência** (a que importa, feita à mão): pior caso da
  fase 1 é quem está na borda oposta ao novo centro — 400 m de encolhimento mais
  300 m de deslocamento (`DESLOCAMENTO = 0.75`, `:100`) = 700 m = **93 s** a
  7,5 m/s, contra **180 s** de janela. Passa. Fase 3: 190 + 142 = 332 m = 44 s
  contra 125 s. Passa. **`DESLOCAMENTO` fica em 0,75** — foi verificado, não
  chutado.

- **(c):** a janela de cota vira uma pergunta que a ilha **já sabe responder**:
  `Island.agua_y(x, z)` (`Island.gd:265`) devolve `SECO` onde não há água.
  Trocar `h >= 1.4 and h <= 8.5` por "não é água e está acima da lâmina"
  resolve, funciona em qualquer escala, e é **reuso, não código novo**.

### 3.2 TERRAINSYSTEM — o `76.0` já é só fallback; o problema é a grade

**O que já funciona.** `_build_grid` (`TerrainSystem.gd:179-190`) lê
`_island.LAND_R`, `_island.LAKE_DISC_R` e `_island.MARSH_DISC_R` da ilha
entregue. O `LAND_R_PADRAO := 76.0` (`:41`) é fallback declarado para rodar sem
mundo, não a fonte da verdade. **O `ILHA-FRATURADA-VALIDACAO.md` §2(d) está
desatualizado neste ponto.**

**O que quebra.** `_n = SIZE / cell_size` (`:102`), com `cell_size = 3,0 m`
(`Balance.gd`, `TERRAIN.cell_size`):

| | 300 m (hoje) | 2.400 m |
|---|---:|---:|
| Células por lado | 100 | **800** |
| Células totais | 10.000 | **640.000** |
| Memória (`_mat` + `_state`) | 20 KB | **1,28 MB** |
| Chamadas de `height()` no build | 10.000 | **640.000** |
| Tempo do build (× 2,7 µs) | 27 ms | **~1,7 s** |

**Correção proposta:** **manter `cell_size = 3,0 m` e mover o build da grade
para a mesma thread de fundo dos chunks.** 1,28 MB é barato e 1,7 s cabe atrás
da tela de carga junto com o resto.

*Por que NÃO derivar `cell_size` do lado da ilha* (a saída "elegante" de
`cell_size = SIZE/100`, que daria 24 m a 2,4 km): o comentário do próprio
`_build_grid` (`:204-207`) explica que a floresta inteira é marcada `M_FUEL`
para que a mancha seja **contígua**, e é a contiguidade que faz o orçamento de
fogo ser a lei (GDD §14). Célula de 24 m destrói a resolução do incêndio. **A
mecânica vale mais que 1,28 MB.**

*Por que NÃO construir a grade só nos chunks L0*: pela mesma razão. Um incêndio
que cruza a borda do chunk apagaria sozinho.

### 3.3 LOOT — a fiação mais quebrada do repo, e ela está quebrada HOJE

Esta não é uma projeção para 2,4 km. **É um defeito vivo em 300 m.**

**(a) Os POIs do loot são uma cópia congelada da ilha de 180 m.** `Loot.POIS`
(`Loot.gd:29-34`) é declarado como "ESPELHO de world/Island.gd" — e o espelho
envelheceu calado quando a ilha cresceu de 180 para 300 m. Confira:

| POI | `Loot.POIS` | `Island.gd` real | Razão |
|---|---|---|---:|
| lago | `(45, 18)` | `LAKE = (75, 30)` (`:55`) | 0,60 |
| alagado | `(-42, 36)` | `MARSH = (-70, 60)` (`:58`) | 0,60 |
| floresta | `(-36, -40)` | `FOREST = (-60, -66)` (`:72`) | 0,60 |
| ruínas | `(38, -44)` | `RUINS = (63, -73)` (`:74`) | 0,60 |

**Os quatro erram pelo mesmo fator: 0,60 = 180/300.** Os quatro cajados — o loot
raro, o que faz "ir ao POI ter que pagar" (`:38`) — nascem hoje **em campo
aberto, longe dos quatro POIs**. A recompensa de explorar não existe.

**(b) O corte cravado.** `_por()` rejeita qualquer ponto com
`p.length() > 70.0` (`:325`), num mapa cujo raio de terra é **132 m**
(`Island.gd:52`). **Metade da ilha atual é inalcançável pelo loot.** As varinhas
já nascem num anel de 14–62 m (`:288`), então na prática **todo o loot do jogo
vive num disco de 62 m no meio de uma ilha de 264 m de diâmetro.**

**(c) A mesma janela de cota do §3.1(c)** — `h < 1.4 or h > 8.5` (`:326`), o que
proíbe loot no platô das ruínas (9,0 m) e no pico. E ela aparece uma terceira
vez em `BauCelestial.gd:96`, com `RAIO_MIN/RAIO_MAX = 18/48 m` cravados ao lado
(`BauCelestial.gd:47-48`), cujo comentário até admite a dependência: *"o loot
corta em 70"*.

**Correção proposta:**
- `POIS` deixa de ser cópia e passa a **perguntar à ilha** (o mesmo padrão que
  `TerrainSystem._build_grid` já usa e que o `Zona.escala_do_mapa` já usa).
  Cópia congelada é exatamente o defeito que este caso prova.
- Os raios viram fração de `Island.LAND_R`: anel das varinhas em
  **0,10 – 0,90 × LAND_R**, corte duro em **1,0 × LAND_R**, `BauCelestial` em
  **0,14 – 0,60 × LAND_R**.
- A janela de cota vira a pergunta a `agua_y()` do §3.1(c) — **uma correção,
  três arquivos** (`Zona.gd:195`, `Loot.gd:326`, `BauCelestial.gd:96`).

**Densidade.** Hoje: 12 varinhas + 4 cajados = **16 peças** (`Loot.gd:42-43`).
Para manter 1 peça a cada 3.400 m² em 3,4 km² seriam **~994 peças**
(`ILHA-FRATURADA-VALIDACAO.md` §2c). **PROPOSTA:** não espalhar 994 — **agrupar**.
Loot de BR é aglomerado, não espalhado. 20 POIs × ~12 peças por POI = **240
peças**, com o corredor entre POIs deliberadamente vazio. Isso é sistema novo (o
anel radial de hoje não sabe fazer clusters) e é trabalho da Onda 4, não da 1.

### 3.4 QUEDA / CASTELO — a rota escala; o alcance do planeio NÃO

**O que já funciona.** `Castelo.lado(island)` (`Castelo.gd:64-70`) lê
`Island.SIZE` em runtime, e `rota()` (`:76-82`) deriva `MARGEM = 0.62` e
`DESVIO = 0.18` do lado real. `Queda` cacheia `Castelo.lado(_island) * 0.5`
(`Queda.gd:191`). Nada a fazer aqui.

**O que quebra.** As três constantes que **não** são função do lado:

**(a) O alcance do salto.** Derivando das constantes de `Queda.gd`:

| Trecho | Conta | Tempo | Horizontal |
|---|---|---:|---:|
| Aceleração até a terminal | 55²/(2×40) = 37,8 m (`:32`, `:38`) | 1,38 s | 30 m |
| Queda livre restante | (320−60−37,8) = 222 m a 55 m/s (`:28` Castelo, `:49`) | 4,04 s | 89 m |
| Planeio | 60 m a 12 m/s (`:53`), a 16 m/s horizontal (`:58`) | 5,00 s | 80 m |
| **Total** | | **10,4 s** | **~199 m** |

**Do ponto de salto, o jogador alcança ~200 m laterais. Só.** Hoje isso é mais
que a ilha inteira (raio de terra 132 m) — por isso ninguém notou. A 2,4 km, a
rota do castelo abre um corredor de **400 m de largura num mapa de 2.400 m**:
**17 % da largura do mapa é alcançável**, e os outros 83 % nunca recebem um
jogador na abertura. Cinco dos seis domínios elementais ficariam mortos no
pouso.

**(b) A duração da rota.** `Castelo.DURACAO = 22,0 s` (`:36`) com
`MARGEM = 0.62`: a 300 m o castelo percorre 372 m em 22 s = 17 m/s. A 2,4 km
percorre 2.976 m em 22 s = **135 m/s**, e a janela de salto útil
(`SALTO_BOT_MIN/MAX` 0,18–0,86, `Queda.gd:98-99`) vira 15 s.

**(c) A altura.** `ALTURA = 320,0 m` (`Castelo.gd:28`) foi escolhida contra uma
ilha de 18 m de pico. Com o pico do pacote a 240 m, a margem de sobrevoo some.

**Correção proposta — as três em fórmula:**
- **`ALTURA` vira função do lado:** o que a queda precisa é de **tempo de voo
  suficiente para atravessar o mapa lateralmente**. Para cobrir metade do lado
  (1.200 m) a partir da rota, o alcance lateral precisa de 1.200 m em vez de
  200 m — **6×**. Isso vem de altura, e altura pura seria 320 × 6 ≈ 1.900 m, o
  que é absurdo. **A saída honesta é a que todo BR usa: não é o salto que cobre
  o mapa, é a ROTA.** PROPOSTA: `DESVIO` sobe de 0,18 para **0,45** (a rota
  passa a variar muito mais entre partidas), `ALTURA` vira **`lado × 0,25`**
  (600 m a 2,4 km → alcance lateral ~330 m) e o corredor alcançável por partida
  passa a ser ~660 m de 2.400 m = 28 %. **Continua não cobrindo o mapa, e isso é
  correto:** o resto do mapa é o que se explora andando, que é o pedido do
  Diretor ("mais explorável").
- **`DURACAO` vira função do lado:** `lado × 2 × MARGEM / vel_castelo`, com uma
  velocidade constante (PROPOSTA: **34 m/s**, o dobro da de hoje). A 2,4 km isso
  dá **88 s de rota** — que é a janela de escolha de pouso, e 88 s num mapa de
  13 min é a proporção certa (11 %, contra 12 % hoje).
- **Consequência de projeto que precisa ser dita:** com 88 s de rota + 10 s de
  queda, a abertura passa de 32 s para ~98 s. É por isso que a partida precisa
  dos 800 s.

---

## 4. O PAPEL DO MESHY NUM MAPA DE 3,4 km²

### 4.1 O que o Meshy NÃO faz — outra vez, porque é a pergunta que volta

**O Meshy não gera terreno.** Não gera a ilha, não gera a costa, não gera as
alturas, não gera a colisão. Isso é código — `Island.height()`, `ArrayMesh`,
trimesh — e continua sendo. `docs/DIRECAO.md` §10 já diz isso e o Diretor já
aprovou: *"o terreno em si: altura, colisão, água, praia, grama... Três sistemas
dependem de `Island.height()` ser determinístico e barato"*.

Mandar o mapa aéreo para o Meshy devolveria **um relevo esculpido de peça
única**: sem `height()`, sem spawn confiável, sem colisão editável, sem
determinismo — e, para este plano especificamente, **sem chunking**, porque não
se pode gerar por região o que veio como um objeto só.

**O que o Meshy faz é o KIT MODULAR que veste 3,4 km².** É nisso que ele é bom.

### 4.2 O KIT — 24 peças, seis famílias

> **PROPOSTA.** O critério é o do `docs/PROJETO.md` §3.5: maior salto de "parece
> jogo publicado" por crédito **E** não presa a sistema nenhum.

| # | Família | Peças | Orçamento de tris/peça |
|---|---|---|---:|
| 1 | **Rocha** | rocha de basalto modular · lasca alta · degrau largo · matacão baixo · rocha vulcânica de cobertura | ≤ 900 |
| 2 | **Parede / cânion** | parede reta de 12 m · parede em canto de 90° · coluna de basalto isolada · topo de crista | ≤ 900 |
| 3 | **Passagem** | arco de calcário de Nymara · arco partido · ponte-raiz de Aeris | ≤ 1.500 |
| 4 | **Ruína** | muro de cobertura · coluna · piso de runa reativa · pedestal de arma · **casa de vigia** | ≤ 1.200 (casa: ≤ 4.000) |
| 5 | **Vegetal-herói** | **árvore-catedral de Aeris** · árvore carbonizada renascendo · juncos de Nymara | ≤ 4.000 (juncos: ≤ 300) |
| 6 | **Cristal / elemental** | pilar condutor de Fulgar · cristal de água · fumarola de brasa · cristal de vento | ≤ 800 |
| — | **Landmark** | **estátua inacabada dos colossos** | ≤ 4.000 |

**24 peças.** Onze delas já têm concept aprovada e prompt escrito em
`ILHA-FRATURADA-VALIDACAO.md` §5 e §1.5 — não são invenção nova.

**A cláusula da árvore-catedral continua valendo, sem emenda**
(`ILHA-FRATURADA-VALIDACAO.md` §5, PROMPT 6): ela entra como **landmark único**
e **não encosta nas 176 árvores em MultiMesh**. `docs/PROJETO.md` §3.5 item 3:
`tree_count()`/`tree_pos()`/`set_tree_burned()` (`Island.gd:1423-1435`) são o que
permite queimar a floresta, e o `TerrainSystem` lê os três
(`TerrainSystem.gd:210-212`). Modelo importado quebra isso.

**Meshy entrega hi-poly de ~2 M triângulos** (`docs/MESHY.md` §4). **Toda peça
passa pelo Blender antes de encostar no jogo.** Não é opcional e não é knob.

### 4.3 Como 24 peças cobrem 3,4 km²

Instanciamento, e nada mais — o mesmo padrão que `_build_rocks`
(`Island.gd:917`) já usa com 58 rochas hoje:

- **Rotação livre em Y** (`Island.gd:718` já faz isso nas árvores) e **escala
  0,7–1,6** (`:719` faz 0,75–1,16).
- 8 rotações discerníveis × 3 faixas de escala = **24 aparências por peça**.
  Cinco rochas × 24 = **120 silhuetas de rocha distintas** a partir de 5
  gerações.
- **Nada é colocado à mão.** O chunk sorteia as suas peças por seed dentro da
  própria footprint, como `_build_rocks` já faz — e por isso a ilha continua
  sendo sempre a MESMA.

### 4.4 A conta de crédito

`docs/PROJETO.md:156` registra o preço medido: **30 créditos** por geração
nativa Multi-View no site. Saldo atual: **2.964** (`docs/PROJETO.md:111`).

| Item | Créditos |
|---|---:|
| 24 peças × 30 | **720** |
| Margem de regeração (taxa histórica ~1 em 6 — `ILHA-FRATURADA-VALIDACAO.md` §4.3) → 4 × 30 | **120** |
| **Total previsto** | **840** |
| **Saldo depois** | **~2.124** |

**28 % do saldo** para vestir 3,4 km². É a maior encomenda já feita ao site — a
leva de 21/08 gastou 210 (`docs/PROJETO.md:311`). É caro e é honesto dizer que é.

### 4.5 A conta de DESENHOS POR QUADRO — o risco nº 1 registrado no §3.5

Esta é a conta que decide se o kit é viável, e ela é o motivo pelo qual o
`docs/PROJETO.md` §3.5 item 1 chama isso de "o risco número um no celular".

**Onde estamos:** pior quadro medido = **30 draw calls**, 85 nós desenháveis
(`Island.gd:19`). A régua herdada do projeto está escrita no mesmo cabeçalho
(`:21`): **"não pode DOBRAR"** → teto de **60**.

**O jeito errado (e é o jeito padrão):** 24 peças = 24 materiais. Cada família
visível paga ao menos 1 draw call por célula da grade. Com 24 famílias × 4
células visíveis = **96 draw calls novos**. Estoura em 3×.

**O jeito que este repo já provou funcionar** (`Island.gd:173-193`): **3
materiais, 1 shader.** `_toon`, `_toon_terrain` e `_toon_stone` são
`duplicate()` do mesmo `toon.gdshader` — o comentário do arquivo é explícito:
*"duplicate() não recompila nada, só troca uniforms (uma mudança de estado por
draw call, não um programa novo na GPU)"*.

**PROPOSTA aplicada ao kit:** as 24 peças compartilham **um atlas de textura** e
o **mesmo `toon.gdshader`**, em **3 materiais** por natureza de superfície —
*pedra*, *vegetal*, *cristal*.

| | Conta | Draw calls |
|---|---|---:|
| Hoje, pior quadro | medido (`Island.gd:19`) | 30 |
| Kit: 3 materiais × 4 células visíveis por material | MultiMesh por material por célula | **+12** |
| **Pior quadro projetado** | | **42** |
| Régua herdada (não pode dobrar) | | 60 |

**42 < 60. Passa — no papel.** E "no papel" é exatamente a razão pela qual a
Onda 4 tem que MEDIR isso no aparelho antes de a peça 13 ser gerada.

---

## 5. ONDAS DE EXECUÇÃO, com portão de FPS entre elas

> **PROPOSTA.** Cada onda entrega, mede e só então libera a seguinte. O portão é
> sempre um número no Poco F4 (Snapdragon 870 / Adreno 650 / 120 Hz —
> `docs/PROJETO.md:36`), nunca uma opinião.

### ONDA 0 — MEDIR (o portão zero, e ele não tem entrega de mapa)

**Entrega:** o número que este projeto nunca teve. FPS no Poco F4, no mapa de
300 m de hoje, em **três cenas fixas**: (1) queda a 320 m com a ilha inteira no
frustum, (2) chão aberto nas dunas, (3) dentro da floresta. Mais ms de carga e
draw calls do pior quadro, com o `world/_shot.gd` que já existe.

**Mede:** FPS mínimo, FPS médio, ms do quadro mais lento.

**Pode ir para a próxima quando:** os três números estão anotados num arquivo do
repo, com o método descrito. **Sem isso, todas as ondas abaixo estão proibidas**
— é a regra do Diretor de 25/08 (`docs/PROJETO.md:33-35`: *"diga isso com
MEDIÇÃO no aparelho, nunca com estimativa"*) e o portão declarado da
`DIRECAO.md` §10.1.

**Bloqueio conhecido:** nenhum aparelho apareceu em `adb devices`
(`docs/PROJETO.md:646-647`). Isso é uma tarefa de cabo USB, não de código, e é
hoje a coisa mais barata do projeto com o maior retorno.

### ONDA 1 — consertar o que já está quebrado (ganho JÁ, zero rede, zero streaming)

**Entrega, no mapa de 300 m que existe:**
1. Os quatro POIs do `Loot` deixam de ser cópia congelada (§3.3a) — **os
   cajados voltam a nascer nos POIs**. É gameplay recuperado, não refactor.
2. O `p.length() > 70.0` (§3.3b) vira fração de `LAND_R` — **a metade externa da
   ilha volta a existir para o loot**.
3. A janela `1.4–8.5` vira pergunta a `agua_y()` nos três arquivos (§3.1c) —
   **zona, loot e baú voltam a poder usar as ruínas e o pico**.
4. Rochas, pedrinhas, moitas, flores e juncos passam pelo `_multimesh_grid` que
   já existe — **culling de frustum de graça** para 1.178 instâncias que hoje a
   GPU paga sempre.
5. *(Opcional, 60 créditos)* as **duas peças-herói já aprovadas** no §6.3 do doc
   de validação — Estátua dos Colossos e Rocha de Basalto Modular — entram como
   teste do pipeline e da conta de draw call do §4.5.

**Mede:** FPS nas três cenas da Onda 0, draw calls do pior quadro, e quantos
loots nascem nos POIs (o selftest sabe contar).

**Pode ir para a próxima quando:** FPS ≥ o da Onda 0 **e** os 4 cajados nascem
nos 4 POIs reais. **Nada aqui depende de rede, de streaming ou do Meshy.**

### ONDA 2 — chunking a 600 m (a arquitetura, num tamanho onde dá para comparar)

**Por que 600 m:** é o degrau que `ILHA-FRATURADA-VALIDACAO.md` §3 já provou que
a malha absorve, e — crucial — é um tamanho em que a malha **única** ainda cabe.
Isso permite rodar **chunk vs. malha única lado a lado, no mesmo aparelho, no
mesmo mapa**. É a única onda em que a arquitetura pode ser validada contra o
que ela substitui.

**Entrega:** chunks de 150 m, anel L0 3×3 com colisão, L2 grosso da ilha, saia
de costura, geração em `WorkerThreadPool`, decoração nascendo e morrendo com o
chunk. Sem L1 ainda (a 600 m o L0 já cobre quase tudo).

**Mede:** FPS nas três cenas; ms de carga; pico de memória; e o número novo,
que é o que mata este tipo de sistema — **o quadro mais lento durante 60 s de
caminhada em linha reta** (o *hitch* da troca de chunk).

**Pode ir para a próxima quando:** FPS ≥ 90 % do de 300 m **E nenhum quadro
acima de 33 ms durante a caminhada de 60 s**. Se o hitch aparecer, o botão é o
raio de pré-carga, não a arquitetura.

### ONDA 3 — 1.200 m, LOD completo e a Zona nova

**Por que 1.200 m:** é o primeiro tamanho em que o mapa **não cabe** na memória
e o L2 grosso deixa de ser luxo. É 25× o mapa de hoje e metade do envelope
final — se algo na arquitetura for falso, falha aqui, com metade do custo.

**Entrega:** os três anéis; bots em modo cinemático fora do L0 (§2.5); a tabela
de 7 fases da Zona (§3.1) escalada para 1.200 m; `Castelo.ALTURA` e `DURACAO`
como função do lado (§3.4); loot em cluster por POI (§3.3).

**Mede:** FPS **na queda** — este é o pior caso da vida do jogo, porque o
frustum pega o mapa inteiro e o L2 é a única coisa entre o jogador e 10 mil
draw calls. Mais: memória, ms de carga, e **tempo até o primeiro contato com um
inimigo** em partida com bots.

**Pode ir para a próxima quando:** FPS na queda ≥ 90 % do FPS no chão; carga
≤ 3 s; tempo até o primeiro contato ≤ 90 s.

### ONDA 4 — 2.400 m e o kit Meshy

**Entrega:** o envelope final; as 24 peças instanciadas nos chunks; os 10 POIs
e as 10 zonas menores do mapa tático; a partida de 800 s.

**Mede:** **draw calls no pior quadro** (a régua do §4.5: teto de 60), FPS na
queda, memória, e o tempo até o primeiro contato com 20 pawns.

**Pode ir para a próxima quando:** ≤ 60 draw calls **E** FPS ≥ 90 % do de
1.200 m. **E a regra do §3.5, que não muda:** uma família de peças por vez,
medindo entre elas. Se a família 1 estourar o orçamento, as famílias 2 a 6 nem
são geradas — economia de 600 créditos por uma medição.

### ONDA 5 — rede

Fora do escopo deste documento. Ver §6.

---

## 6. A REDE — o elefante, e o que dá para jogar antes dele

### 6.1 O fato

`docs/infra/01-MULTIJOGADOR.md` §1: *"um processo só, no aparelho... 6 `Pawn`
controlados por IA local"*. `Balance.MATCH.bots = 6` (`Balance.gd:292`). Não
existe uma linha de rede, e o documento existe justamente *"para impedir um erro
caro e comum: contratar infraestrutura achando que ela traz o multijogador"*.

### 6.2 O que é jogável antes da rede, num mapa grande

A conta de esparsidade é a de `ILHA-FRATURADA-VALIDACAO.md` §2c — vizinho mais
próximo ≈ 0,5·√(A/N), contra um alcance de arma de 38 m (`Balance.gd:67`):

| Cenário | Área | Pawns | Vizinho médio | Em alcances de arma |
|---|---:|---:|---:|---:|
| Jogo hoje | 0,0547 km² | 7 | 44 m | 1,2× |
| 3,4 km² com os 7 de hoje | 3,4 km² | 7 | **348 m** | 9,2× |
| **3,4 km² com 20 pawns (PROPOSTA)** | 3,4 km² | 20 | **206 m** | 5,4× |
| Para restaurar os 44 m de hoje | 3,4 km² | **439** | 44 m | 1,2× |

**PROPOSTA: 20 pawns antes da rede — 1 humano + 19 bots.** Três razões:

1. **É o número em que o mapa vai viver.** Testar 3,4 km² com 7 pawns é medir um
   mapa que ninguém vai jogar.
2. **Num mapa grande, bot é mais barato, não mais caro.** Com o modo cinemático
   do §2.5, um bot fora do envelope L0 não paga física. Num mapa de 2,4 km a
   grande maioria dos bots está fora o tempo todo. **19 bots num mapa grande
   custam menos que 6 bots num mapa pequeno.** Isso é medível na Onda 3.
3. **Os 439 pawns são a resposta errada para a pergunta certa.** A densidade não
   se conserta com quantidade — conserta-se com **a zona**, que o jogo já tem.
   Na fase 3 da tabela do §3.1 o círculo tem 350 m de raio = 0,38 km²; com 20
   pawns dentro, o vizinho médio cai para **69 m** — mais denso que o jogo de
   hoje já foi em metade da partida. **A esparsidade do mapa grande é um
   problema dos primeiros 4 minutos, não da partida inteira.**

### 6.3 Onde a rede entra no roadmap

**Depois da Onda 4, e o caminho já está escrito.** Não invento nada aqui:

- `docs/infra/01-MULTIJOGADOR.md` §4 já decidiu a arquitetura por degrau:
  **degrau 2 (1v1/2v2) = host-cliente**, para aprender netcode barato; **degrau
  3 (mini-BR 16–20) = servidor autoritativo dedicado, obrigatoriamente** — *"com
  20 jogadores e itens no chão, host-cliente vira festa de trapaça"*.
- §5: a rede é `SceneMultiplayer` + `ENetMultiplayerPeer` do próprio Godot.
  Nenhuma dependência nova.
- §6 lista os **três sinais** de prontidão, e **o terceiro é "o FPS foi medido
  no aparelho"** — literalmente o mesmo portão da Onda 0 deste plano. As duas
  frentes têm o mesmo bloqueio, e ele custa um cabo USB.
- `docs/infra/02-SERVIDORES.md` §1: o servidor fica em **São Paulo**, e nada ali
  se contrata antes dos três sinais.

**O que este plano acrescenta ao roadmap de rede:** o mapa grande **ajuda** a
rede em dois pontos concretos, e vale registrar. (a) A queda já é determinística
e semeada (`Queda.gd:96`, `Castelo.SEED_ROTA`), e o `01-MULTIJOGADOR.md` §3 já
conta isso como crédito. (b) **O chunking é uma vantagem de rede, não um custo:**
um servidor autoritativo headless nunca desenha nada e só precisa de `height()`
— que, por §2.4, não depende de chunk nenhum. **O servidor roda o mapa de 2,4 km
sem gerar uma única malha.**

---

## 7. RISCOS E O QUE PODE DAR ERRADO

| # | Risco | Medição que decide | Plano B |
|---|---|---|---|
| 1 | **O aparelho não aguenta** | FPS na queda, Onda 3 | Em ordem de custo: (a) baixar densidade de decoração por chunk (é knob, zero código); (b) encolher L0 de 3×3 para 2×2 e puxar a névoa; (c) **parar em 1.200 m** (25× o mapa de hoje); (d) parar em 600 m. **Nenhum é "voltar atrás"** — são degraus da MESMA arquitetura, e é exatamente por isso que as ondas vão 600 → 1.200 → 2.400. |
| 2 | **Mapa vazio** — o risco nº 1 do doc de validação (§2c: *"desempenho se otimiza e tédio não"*) | Tempo até o primeiro contato, Onda 3 | Se passar de 90 s: **apertar as fases da Zona**, não aumentar o número de bots. A tabela do §3.1 é o botão. |
| 3 | **Hitch de troca de chunk** | Quadro mais lento em 60 s de caminhada, Onda 2 | Aumentar o raio de pré-carga; gerar 2 chunks à frente da direção do movimento. Se persistir, o chunk é grande demais → 100 m. |
| 4 | **Costura visível entre LODs** | Inspeção visual no horizonte, Onda 2 | Saia vertical (§2.5). Se ainda aparecer, aumentar a saia — não inventar geomorphing. |
| 5 | **Carga da grade do fogo** (+1,7 s, §3.2) | ms de carga, Onda 3 | Mover para a thread de fundo. Se não bastar, `cell_size` derivado do lado — **e aceitar perder resolução de incêndio**, que é o preço declarado. |
| 6 | **O kit chega com 24 materiais** | Draw calls do pior quadro, **antes da peça 2** | A peça 1 (rocha modular) passa pelo Blender, vira o material compartilhado e é medida no aparelho **antes de a peça 2 ser gerada**. É a lei do §3.5: uma família por vez. |
| 7 | **Regressão silenciosa por constante cravada** | O selftest | `Loot.POIS` já provou que cópia congelada envelhece calada por dois meses. **Toda constante nova de mapa sai de `Island.SIZE` ou `LAND_R`**, e o selftest reprova cravado. |
| 8 | **A Onda 0 nunca acontece** | — | É o risco mais provável de todos, porque não é código. Sem o FPS medido, este plano inteiro vira ficção, e o mapa de 2,4 km é aprovado sobre um número que ninguém viu. |

**O cenário que precisa ser dito com todas as letras:** se a Onda 3 mostrar que
o Poco F4 não segura 1.200 m com o L2 desenhando na queda, **o mapa de 2,4 km
não acontece neste aparelho** — e a decisão volta ao Diretor com um número, não
com uma opinião. O trabalho das ondas 1 e 2 **não se perde** em nenhum
cenário: as fiações consertadas e o culling da decoração valem em 300 m, em
600 m e em 2,4 km.

---

## 8. DECISÕES DO DIRETOR

Curta. Nada abaixo foi decidido.

1. **TAMANHO DE CHUNK E ONDA INICIAL.**
   **PROPOSTA:** chunk de **150 m / 33 quads** (quad de 4,55 m), anel L0 de
   **3×3**. E a onda inicial **não é o streaming** — é a **ONDA 0 (medir FPS)**
   seguida da **ONDA 1 (consertar as fiações quebradas + culling da decoração)**.
   *Por quê:* a Onda 1 dá ganho de gameplay imediato no mapa que já existe (os
   cajados voltam aos POIs, metade da ilha volta a receber loot), não depende de
   rede nem de streaming, e é o que vai gerar a linha de base contra a qual toda
   onda seguinte é medida.

2. **Nº DE BOTS NO MAPA GRANDE ANTES DA REDE.**
   **PROPOSTA: 20 pawns — 1 humano + 19 bots** (`Balance.MATCH.bots` de 6 → 19).
   *Por quê:* é o número em que o mapa vai viver, e com o modo cinemático fora
   do envelope L0 os 19 custam menos que os 6 de hoje. O vizinho médio no
   segundo zero fica em 206 m e cai para 69 m na fase 3 da zona.

3. **O KIT MODULAR DO MESHY VEM ANTES OU DEPOIS DO STREAMING?**
   **PROPOSTA: DEPOIS — Onda 4.**
   *Por quê:* `docs/PROJETO.md` §3.5 é lei (funcionalidade antes de arte de
   cenário), e há uma razão prática mais dura que a lei: **enquanto o chunk não
   existe, não há onde instanciar o kit**. As peças ficariam num MultiMesh único
   não-cortado, que é exatamente o defeito que o §1 lista.
   **Exceção proposta, e é barata:** as **duas peças-herói já aprovadas** no §6.3
   do doc de validação (Estátua dos Colossos e Rocha de Basalto Modular) entram
   na **Onda 1**, por **60 créditos**, como teste do pipeline e da conta de draw
   call do §4.5. É o preço de descobrir cedo se 24 peças cabem no aparelho — em
   vez de descobrir depois de gastar 840.

4. **EM ABERTO, sem proposta forte:** a resolução da grade do terreno reativo no
   mapa grande (§3.2). Manter `cell_size = 3 m` custa 1,28 MB e 1,7 s de carga;
   derivá-lo do lado da ilha é grátis mas **degrada o incêndio**, que é pilar do
   GDD §14. Inclino-me a **pagar os 1,28 MB**, mas é decisão de gameplay, não de
   arquitetura.

---

## 9. O QUE ESTE DOCUMENTO FEZ NO REPOSITÓRIO

**Nada de código.** Nenhum `.gd` foi tocado. Nada foi commitado — o coordenador
commita.

**Correções de fato que este documento registra sobre documentos anteriores**
(para ninguém procurar defeito já consertado nem confiar em número velho):

1. `ILHA-FRATURADA-VALIDACAO.md` §2(d) diz que `Zona.gd` tem `RAIO_INICIAL`
   defasado. **Está desatualizado:** `Zona.escala_do_mapa()` (`Zona.gd:132`) já
   lê `Island.SIZE`. O que quebra na Zona são os **tempos** e o **círculo
   final**, não os raios (§3.1).
2. O mesmo §2(d) diz que `TerrainSystem.gd` tem `p.length() < 76.0` cravado.
   **Está desatualizado:** `LAND_R_PADRAO := 76.0` (`TerrainSystem.gd:41`) é
   fallback declarado, e `_build_grid` lê `_island.LAND_R` (`:190`). O que
   quebra é o tamanho da **grade** (§3.2).
3. `docs/PROJETO.md` §3.5 fala em **158 árvores**; o código pede 150 + 26 = **176**
   (`Island.gd:653`, `:671`).
4. **Defeito vivo, não projeção:** `Loot.POIS` (`Loot.gd:29`) aponta para os POIs
   da ilha de **180 m** — os quatro erram por exatamente 0,60 = 180/300. Os
   cajados não nascem nos POIs **hoje** (§3.3a).
5. **Defeito vivo, não projeção:** `Loot._por` corta em `p.length() > 70.0`
   (`:325`) num mapa de raio de terra 132 m — **metade da ilha atual não recebe
   loot** (§3.3b).
6. **Defeito vivo, não projeção:** a janela `h >= 1.4 and h <= 8.5` aparece
   cravada em três arquivos (`Zona.gd:195`, `Loot.gd:326`, `BauCelestial.gd:96`)
   e **proíbe zona, loot e baú no platô das ruínas (9,0 m) e no pico (28 m)**
   (§3.1c).
