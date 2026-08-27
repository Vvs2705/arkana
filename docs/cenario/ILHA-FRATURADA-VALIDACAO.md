# ILHA FRATURADA — validação do pacote de concept art do Diretor (27/08/2026)

**Pedido do Diretor:** *"leia o arquivo, veja e valide, se boa a ideia, inclua
nas artes concept do nosso projeto e veja a possibilidade de criar isso em 3d de
alta qualidade, ou até criar um prompt para o Meshy criar para nosso time
utilizar"*.

**Origem (só leitura):** `C:\Users\VINICIUS\Downloads\ARKANA-ILHA-01-MASTER-MAPAS\`
· 26 PNG únicos em `concept/` + o mapa tático em `ARKANA-ILHA-FRATURADA/02-MAPAS/`
· 3 documentos (`ESPECIFICACAO-MESTRA.md`, `CADERNO-DE-PROMPTS.md`,
`README-APROVACAO.md`, todos duplicados em `08-DOCUMENTOS/` — conferido com
`diff`, idênticos).

**Espelhado no repo em:** `cenario/ilha-fraturada/arte/` — 27 PNG + 1 SVG,
**61,7 MB**, LFS confirmado (detalhe no §7).

**Veredito de uma linha:** a arte é a melhor coisa que entrou neste projeto
desde as concepts dos magos, e a estrutura que ela propõe (pentágono elemental
+ vale central + Altar no meio) é adotável JÁ. O que **não** é adotável é a
escala: o pacote pede uma ilha **62× maior em área** do que a que o jogo tem,
para uma partida **5× mais longa** e um número de jogadores que **não existe
porque o jogo não tem rede**. Este documento separa as duas coisas para que a
segunda não afunde a primeira.

---

## 1. VEREDITO DA ARTE, peça por peça

O `README-APROVACAO.md` dele (§2) define uma **hierarquia de autoridade** entre
as imagens, e ela é boa — julgar todas com o mesmo peso seria o erro. Avaliei
cada peça pela função que ELE deu a ela, mais os critérios de `cenario/00-VALIDACAO.md`
(o padrão que usamos nas 55 concepts de 26/08).

### 1.1 MASTER REFERENCE ambiental (as três que mandam)

| # | Peça | Função na hierarquia | Veredito |
|---|---|---|---|
| 01 | `01-key-art-campo-de-batalha.png` | atmosfera e identidade da arena | **APROVADA** |
| 02 | `02-visao-aerea-geral.png` | leitura relativa dos 5 domínios | **APROVADA COM RESSALVA** |
| 11 | `11-vale-da-convergencia.png` | centro neutro e Altar | **APROVADA COM RESSALVA** |

**01 — APROVADA, sem emenda.** É a peça que justifica o pacote inteiro. O que
ela **entrega para o jogo**, concretamente: (a) o Castelo Voador ancorado à
esquerda com as correntes e a âncora visíveis — a nossa peça já modelada
aparece em contexto pela primeira vez; (b) os cinco domínios legíveis **por cor
e por silhueta**, sem uma única letra na imagem — fogo (laranja, cones
quebrados) no sudoeste, água (ciano, cascatas em leque) no sudeste, terra
(ocre, mesas e ruínas) a oeste, vento (verde-turquesa, copas altas) a nordeste,
raio (violeta, agulhas nevadas com relâmpago) ao norte; (c) o vale central
dourado, mais claro e mais baixo que tudo em volta, que resolve sozinho a
pergunta "para onde eu olho quando caio". Isso é exatamente o critério 3 do
`ESPECIFICACAO-MESTRA.md` §12, e ela passa.

**02 — APROVADA COM RESSALVA (a densidade urbana).** Como leitura relativa dos
domínios está correta e é utilizável como planta. O problema é que a imagem
desenha **doze praças radiais** de estradas pavimentadas cruzando a ilha inteira.
Isso não é uma ilha antiga parcialmente retomada pela natureza — é a planta de
uma cidade, e lê como mapa de jogo de estratégia. Contradiz a própria
`ESPECIFICACAO-MESTRA.md` §3 ("silhueta natural assimétrica: evita aparência de
tabuleiro"). **Ressalva, não reprovação**, por um motivo prático: a estrada é a
camada mais fácil de ignorar na tradução para o greybox — o relevo por baixo
dela está bom. Não regerar; usar como planta de relevo e **descartar a malha de
estradas**.

**11 — APROVADA COM RESSALVA (contradiz o Altar canônico).** Como panorama de
vale, é ótima e é a melhor imagem de GAMEPLAY do lote: depressão irregular
(não arena plana), muretas de ruína baixas espalhadas a cada 15–20 m, riachos,
arcos partidos, morros que cortam linha de visão. Cumpre a regra dele mesmo
("cobertura a cada 12–25 m"). **A ressalva é séria:** o Altar de Sintonia que
ela mostra é uma plataforma circular com **três agulhas de cristal coloridas**.
O Altar **canônico** — aprovado em 26/08 e registrado em
`cenario/00-VALIDACAO.md` + `cenario/08-altar-sintonia/arte/keyart-conjunto.png`
— é outra coisa: plataforma **pentagonal**, **cinco** obeliscos de pedra sendo
**três acesos e DOIS TOMBADOS com gemas mortas**, braseiro central com a
**esfera trançada em duas cores** e **dois discos de pegadas** (as marcas da
Sintonia). Os dois obeliscos mortos são o ponto: *o mundo esqueceu a Sintonia*.
A versão do pacote apaga a história. **Não substituir o canônico.** Usar a 11
pelo VALE (relevo, cobertura, riachos) e manter o Altar como está.

### 1.2 Mapas de level design

| # | Peça | Veredito |
|---|---|---|
| 03 | `03-mapa-topografico-sem-texto.png` | **APROVADA COM RESSALVA** (mesma malha de estradas da 02) |
| 04 | `04-mapa-tatico-identificado.png` (+ `.svg`) | **APROVADA** — é o melhor documento do pacote |
| 05 | `05-corte-lateral-altitudes-cavernas.png` | **APROVADA COMO ARTE · REPROVADA COMO PLANO** |

**04 — APROVADA e é a peça mais útil de todas.** É a única imagem do lote com
**texto legível de verdade** (nenhum daquele alfabeto falso que a IA costuma
devolver), e o `CADERNO-DE-PROMPTS.md` dele explica por quê: o mapa tático foi
**composto de forma controlada** sobre a topografia, não gerado. É uma decisão
de produção correta e vale registrar como método. Entrega: 10 POIs numerados,
10 zonas menores marcadas com disco branco, rotas separadas por tipo (anel
intermediário, rede subterrânea, radial elemental) coloridas pelos hex
elementais da nossa paleta, e um painel de métricas. As posições batem com o
texto da especificação em 10 de 10 POIs (conferi um a um). **Ressalva menor:**
o "anel intermediário" está desenhado como um **octógono geométrico** de linha
sólida, e o texto insiste que a organização é *pentagonal e oculta na
geografia*. É defeito de diagrama, não de desenho — o anel real será a curva
que o relevo permitir.

**05 — APROVADA COMO ARTE, REPROVADA COMO PLANO.** É bonita e o próprio README
dele já se protege ("não é um corte geológico métrico"). Registro o porquê da
reprovação como plano, com número: no corte, o **volume subterrâneo é
aproximadamente igual ao volume de superfície** — três galerias gigantes (fogo,
central, água) com vários níveis, escadas e passarelas. Isso não é "rede
subterrânea parcial": é **um segundo mapa completo**, com colisão própria,
navegação própria, oclusão própria e iluminação própria. O jogo hoje não tem
sistema de cavernas nenhum (conferido: nada em `godot/world/` nem em
`godot/gameplay/` trata interior). Usar como poesia de verticalidade; não usar
como escopo.

### 1.3 Panorâmicas de bioma

| # | Peça | Veredito | O porquê visual, em uma linha |
|---|---|---|---|
| 06 | Caldeira de Cineris (Fogo) | **APROVADA** | chão escuro e caminhável de verdade, veios vermelhos como ACENTO e não como banho; blocos de lava e paredes da forja dando cobertura em degraus; poças de água azul já contando o contrajogo (água resfria fissura) |
| 07 | Terraços de Nymara (Água) | **APROVADA COM RESSALVA GRAVE** | virou **cidade de mármore branco** com dezenas de torres — não os "terraços de calcário" da ficha; escapa da paleta (pedra cinza-azulada + bronze + ouro contido) e o custo de construção é de outro jogo |
| 08 | Espinha de Basalto (Terra) | **APROVADA — a melhor do lote** | basalto colunar em degraus (cobertura escalonada de graça), **estátuas de colossos inacabadas entalhadas na própria montanha**, guindastes de madeira, bandeirolas com o Selo, e um chão de cânion que é literalmente uma rota |
| 09 | Bosque Suspenso de Aeris (Vento) | **APROVADA COM RESSALVA** | árvore-catedral com as três plataformas exatas da ficha, raízes-ponte e correntes de ar VISÍVEIS em turquesa (o vento anunciado, GDD); ressalva: os santuários pequenos têm telhado de pagode — deriva de linguagem |
| 10 | Picos de Fulgar (Raio) | **APROVADA COM RESSALVA GRAVE** | crista, santuário e pilares condutores acorrentados excelentes, MAS a **Torre Arcana está errada** (ver abaixo) |

**Sobre a 10 e a Torre.** A torre que a imagem põe no pico é uma agulha gótica
alta e fina com coroa e facho. A Torre Arcana **canônica** está em
`docs/prompts/07-TORRE-ARCANA.md` e em `cenario/07-torre-arcana/arte/`: **22 m**,
**octogonal**, base de 7 m afinando para 4,5 m, três andares com **cornijas e
cintas de bronze**, **escada externa em espiral** com sacada de mureta de 1,1 m
(que é cobertura jogável), e no topo dois dentes de pedra com um **cristal
octaédrico âmbar flutuando ENTRE eles**. A `ESPECIFICACAO-MESTRA.md` §11 dele
manda respeitar as referências já aprovadas; nesta imagem ela não foi
respeitada. Mesmo caso do Altar na 11: **o pacote não define landmark, o pacote
define bioma.** Nas duas peças, o canônico do repo vence.

### 1.4 POIs, rotas e gameplay

| # | Peça | Veredito | Nota |
|---|---|---|---|
| 12 | Zona inicial de equipamento | **APROVADA** | o melhor micro-POI do lote e o mais diretamente construível: casa de vigia + observatório quebrado, muretas, **duas saídas visíveis**, duas figuras humanas dando escala real |
| 13 | Rotas de travessia | **APROVADA COM RESSALVA** | as quatro rotas (baixa, intermediária, ponte alta, corrente de vento) leem em um segundo; ressalva: densidade de bandeirolas/pilares outra vez urbana |
| 14 | Costa e cavernas | **APROVADA** | praia estreita, passagem de falésia, boca de caverna com cristais e **segunda saída visível ao fundo** — cumpre a regra "toda zona oferece ao menos duas saídas" |
| 15 | Tempestade arcana (fechamento) | **APROVADA COM RESSALVA** | a espiral de veios dourados é bela vista de fora, mas o jogador vê a zona **de dentro e do chão**; a nossa parede é violeta `#b06cff` com 55 m de altura (`godot/gameplay/Zona.gd`), e a imagem não mostra essa leitura |
| 16 | Prancha de escala | **APROVADA — e é a peça que denuncia o problema** | ela põe dois jogadores de 1,8 m ao lado de ponte, torre e castelo: e é olhando para ela que se vê que tudo no pacote é monumental demais para um humano de 1,8 m com alcance de arma de 38 m |

### 1.5 Módulos isolados (os candidatos ao Meshy)

Estes são os que interessam para 3D, porque seguem a regra do nosso pipeline
sem que ninguém tenha pedido: **um objeto por imagem, fundo neutro, sombra de
contato**. É a lição de 30 créditos de 21/08 (`docs/MESHY.md`) aplicada por
conta própria — mérito dele.

| # | Peça | Ficha dele | Veredito |
|---|---|---|---|
| 17 | Rocha de basalto modular | 8 × 5 × 6 m | **APROVADA** — colunas em degraus = acesso escalonado e cobertura no mesmo objeto; runas douradas dão identidade Arkana |
| 18 | Rocha vulcânica de cobertura | 4,5 × 2,5 × 2,2 m | **APROVADA COM RESSALVA** — a forma serve, mas o render é **fotorrealista**, contra a âncora de estilo (superfície pintada à mão) |
| 19 | Arco de calcário de Nymara | 6 × 2 × 5 m | **APROVADA — a melhor peça isolada do lote** — pintada à mão de verdade, cinza-azulado + bronze + gema de água, hera, dano; pronta para o Meshy sem retoque |
| 20 | Ponte-raiz de Aeris | 18 × 3,5 m | **APROVADA COM RESSALVA** — leve deriva fotorrealista; a forma (arco de raízes trançadas com âncoras de pedra rúnica) está certa |
| 21 | Pilar condutor de Fulgar | 1,4 × 1,4 × 5 m | **APROVADA COM RESSALVA** — desenho excelente, mas a imagem lê como torre de 12 m; vale a regra dele mesmo: **a dimensão escrita manda** |
| 25 | Casa de vigia | base 5 × 5 m, 7 m | **APROVADA COM RESSALVA — TROCAR O BRASÃO** — a bandeirola traz uma **estrela de seis pontas** dentro de um círculo. O Selo de Arkana é um **PENTÁGONO** (está no piso rúnico nº 32 desta mesma entrega, e no portão do castelo). Hexagrama não é a nossa marca e carrega leitura religiosa que a §9 dele mesmo exclui. Corrigir antes de virar modelo |
| 28 | Pedestal de arma | 0,9 × 0,9 × 1,1 m | **APROVADA** — octogonal, gemas azuis, anel flutuante; conversa direto com o nosso Baú |
| 29 | Árvore de folhas douradas | 4,5 m | **APROVADA** |
| 30 | Árvore carbonizada renascendo | 5 m | **APROVADA** — conta a história do fogo do GDD §14 em um objeto só |
| 31 | Juncos de Nymara | 2,2 × 1,4 × 1,5 m | **APROVADA** — turquesa `#8FE8C9` exato da paleta |
| 32 | Piso de runa reativa | 4 × 4 × 0,25 m | **APROVADA** — e é a peça que carrega o **Selo pentagonal** correto; vira o vocabulário visual de "terreno reativo" |

### 1.6 O que NÃO veio no pacote (higiene de arquivo — verificado)

Isto não é crítica de arte, é aviso ao coordenador para ninguém procurar em vão:

1. **O `README-APROVACAO.md` dele inventaria 32 arquivos em oito pastas**
   (`03-BIOMAS/`, `04-POIS/`, `05-ROTAS/`, `06-ASSETS/`, `07-GAMEPLAY/`,
   `09-REFERENCIAS-APROVADAS/`). **Essas pastas não existem neste pacote.** Só
   vieram `01-MASTER/` e `02-MAPAS/` — o nome da pasta raiz já avisa
   (`ARKANA-ILHA-01-MASTER-MAPAS`), e o `SHA256SUMS(2).txt` lista **cinco
   .zip**, dos quais só o primeiro chegou. Todo o resto veio solto em
   `concept/`, com nome de gerador (`ChatGPT Image 27 de ago...`).
2. **O `SHA256SUMS(2).txt` não verifica nada aqui:** ele soma os `.zip`, e não
   há `.zip` na pasta. Conferência de integridade deste lote: impossível com o
   que veio. Fiz o que dava — MD5 de tudo, e os 26 concepts são 26 imagens
   distintas, com 4 delas repetidas dentro de `01-MASTER/`+`02-MAPAS/`.
3. **Cinco das 32 peças não vieram em arquivo nenhum** (conferi visualmente
   todas as 26): o **muro de ruína de cobertura** (nº 22), a **ponte de pedra
   de Arkana** (nº 23), a **entrada de caverna costeira isolada** (nº 24), o
   **observatório quebrado isolado** (nº 26) e o **baú comum fechado** (nº 27).
   Os três primeiros aparecem dentro de panorâmicas, mas não como módulo
   isolado — ou seja, não estão prontos para o Meshy.
4. **`04-mapa-tatico-identificado.png` é a única imagem ausente de `concept/`** —
   coerente com o caderno dele, que diz que ela foi composta em etapa
   controlada. Bate.

---

## 2. O CHOQUE DE ESCALA, dito de frente

Esta é a seção que importa. A arte está aprovada; a **especificação numérica que
vem com ela** descreve um jogo diferente do que existe. Os dois lados, medidos:

| | Pacote (`ESPECIFICACAO-MESTRA.md` §2) | Jogo HOJE (medido no código) |
|---|---|---|
| Envelope | 2,4 × 2,2 km = **5,28 km²** | `Island.SIZE = 300` m → **0,09 km²** |
| Terra jogável | **3,4 km²** | `LAND_R = 132` m → π·132² = **0,0547 km²** |
| Altitude máxima | **240 m** | colinas `1,8 + 10,5·n` ≈ 12 m · platô das ruínas 9 m · `PEAK_H = 18` m → **~18 m** |
| Partida | **13–16 min** (780–960 s) | `Balance.MATCH.duration_s = 180` s |
| Pawns | **20 jogadores** (10 duplas) | `Balance.MATCH.bots = 6` + 1 humano = **7** |
| POIs | 10 principais + 10 zonas menores | 7 (`FOREST`, `LAKE`, `MARSH`, `RUINS`, `PEAK`, `DUNES`, `BAY`) |

**A razão de área: 3,4 / 0,0547 = 62×.** Pelo envelope, 5,28 / 0,09 = 59×.
A vertical: 240 / 18 = **13×**. Não é "um pouco maior". É duas ordens de
grandeza de terra e uma de altura.

### (a) Desempenho no Poco F4 — a conta que o próprio `Island.gd` ensina a fazer

O cabeçalho do `godot/world/Island.gd` traz a medição que autorizou a ampliação
de 180 → 300 m, feita com `world/_shot.gd` (não chutada):

```
SIZE 300 | 133×133 verts | 34.848 tris de chão | 220.986 no mundo
         | pior quadro 30 draw calls / 159k prims | 621 ms de geração
```

Aplicando a **mesma malha** (quad de 2,27 m) a 2,4 km de lado:

- 2400 / 2,27 = **1.057 quads por lado** → 1.057² × 2 = **~2.235.000 triângulos
  só de chão**. Hoje o **mundo inteiro** tem 220.986. É **64× o chão** e ~10×
  todo o mundo atual.
- **Tempo de geração:** o custo é linear no número de vértices (a `height()` é
  chamada 13× por vértice, por causa de `_terrain_ao`/`_terrain_ny`).
  621 ms × 64 ≈ **40 segundos** de tela de carga, por partida. Numa partida que
  hoje dura 180 s.
- **Colisão é o assassino silencioso**, e ninguém costuma contar: o chão usa
  colisão trimesh. Uma `ConcavePolygonShape3D` com 2,2 M triângulos precisa
  construir a BVH no load e ocupa memória de física da ordem de dezenas de MB.
  O terreno atual passa porque tem 34,8 k.
- **Decoração escala com área:** as 158 árvores na mesma densidade viram
  **~9.800**. A grama sofre menos (`visibility_range_end = 80 m` corta por
  célula — o custo dela é limitado pelo que está a 80 m da câmera, não pela
  área total), mas o número de células do grid e de instâncias de MultiMesh
  cresce linear.
- **Streaming não existe.** Conferido: a ilha é construída inteira em
  `_ready()`, uma vez, e vive na memória. Não há carga por região, não há LOD
  de terreno, não há descarte de distância além do `visibility_range` da grama.
  Sem streaming, 3,4 km² não é "pesado": é impossível.
- **E o portão que ninguém passou ainda:** `docs/PROJETO.md` registra que o
  **FPS no Poco F4 continua SEM MEDIÇÃO** — é a pendência aberta e é o portão
  declarado da repaginação do chão (DIREÇÃO §10.1). **Não há base de comparação
  para aprovar mudança de escala nenhuma.** Aprovar 62× sem ter medido 1× é
  apostar o projeto num número que ninguém viu.

### (b) Tempo de travessia a pé, com a velocidade real

`Balance.PLAYER.speed = 7,5 m/s` (não há multiplicador de corrida — conferi o
dicionário inteiro). Com esse número:

| Travessia | Distância | Tempo a pé | % de uma partida de 180 s |
|---|---:|---:|---:|
| Ilha de hoje, diâmetro de terra | 264 m | **35 s** | 19 % |
| Ilha de hoje, diagonal do envelope | 424 m | **57 s** | 31 % |
| Pacote, centro → borda | 1.200 m | **160 s** | **89 %** |
| Pacote, borda → borda oposta | 2.400 m | **320 s** | **178 %** |

Ou seja: **uma única travessia da ilha proposta custa 1,8 vezes a partida
inteira de hoje.** E note que a especificação dele estima "4–5 minutos" para a
mesma travessia — isso implica 8–10 m/s, mais rápido que o jogador real, e em
linha reta, ignorando relevo. Com a velocidade de verdade e rota sinuosa, são
mais de 6 minutos.

Para que a travessia custasse a mesma **fração** de partida que custa hoje
(19 %), a partida precisaria de 320 / 0,19 ≈ **1.684 s ≈ 28 minutos**. Mesmo
nos 13–16 min que ele propõe, atravessar consome **33–41 %** do tempo total —
que é, aliás, um número normal de battle royale grande. O problema não é a
matemática dele; é a distância entre ela e os 180 s que existem.

### (c) Densidade de encontro e de loot — o risco de mapa vazio

A distância média até o vizinho mais próximo, para N pontos espalhados numa
área A, é aproximadamente **0,5·√(A/N)**. O alcance de arma mais longo do jogo é
**38 m** (`Balance.LIGHTNING.range`).

| Cenário | Área | Pawns | Vizinho mais próximo | Em alcances de arma |
|---|---:|---:|---:|---:|
| **Jogo hoje** | 0,0547 km² | 7 | **44 m** | 1,2× |
| Pacote, com os 7 pawns de hoje | 3,4 km² | 7 | **348 m** | **9,2×** |
| Pacote, com os 20 jogadores dele | 3,4 km² | 20 | **206 m** | 5,4× |

Leia a primeira linha: hoje o vizinho médio está a **1,2 alcance de arma**. Você
cai e alguém já está quase no alcance — é por isso que 180 s funcionam e a
partida é densa.

Agora a segunda: com **7 pawns em 3,4 km²**, o vizinho médio está a 348 m, que
são **46 segundos de caminhada** — se você soubesse onde ele está, e você não
sabe. Em 180 s, o jogador médio **não encontra ninguém**. O mapa não fica
"grande": fica **vazio**. É o risco número um desta proposta, e é maior que o
risco de desempenho, porque desempenho se otimiza e tédio não.

Terceira linha, para ser justo com ele: **mesmo com os 20 jogadores da
especificação**, o mapa é 4,7× mais esparso que o jogo de hoje. Ele compensa
isso com partida de 13–16 min e uma zona que fecha — que é exatamente o desenho
certo. A especificação **é coerente por dentro**. Ela só está a 62× de nós.
(Para restaurar os 44 m de hoje em 3,4 km² seriam necessários **~439 pawns**.)

**Loot.** Hoje: `Loot.QTD_VARINHA = 12` + `QTD_CAJADO = 4` = **16 peças** em
0,0547 km², ou **1 peça a cada 3.400 m²** — um círculo de 33 m de raio. Mantendo
as 16 peças em 3,4 km², passa a ser 1 a cada 212.500 m², **um raio de 260 m por
item**: o jogador anda ~35 s para achar a primeira varinha, num mapa em que ele
já anda 160 s para chegar ao centro. Para densidade paritária seriam **~994
peças**. A resposta dele (10 POIs + 10 zonas menores = 20 aglomerados de loot,
cada um sustentando uma dupla) é **estruturalmente a resposta certa** — o loot
de BR não é espalhado, é aglomerado. Mas isso é sistema novo: hoje o loot nasce
num **anel radial em volta do centro** com seed fixo, não em clusters de POI.

### (d) Os acoplamentos que uma mudança de escala reabre (fatos, do código)

O cabeçalho do `Island.gd` já avisa que `SIZE` é **contrato entre raias**, e que
mudá-lo de 180 para 300 deixou dois arquivos desatualizados. Mexer nele outra
vez reabre:

- `godot/gameplay/Zona.gd` — `RAIO_INICIAL = 90.0` e a tabela `FASES` foram
  escritas contra `ILHA_REF = 180.0`, e **já estão defasadas para os 300 m
  atuais** (o próprio comentário do `Island.gd` diz que 6 dos 14 spawns nascem
  fora do primeiro círculo).
- `godot/terrain/TerrainSystem.gd` — tem `p.length() < 76.0` cravado (o raio da
  ilha antiga; hoje é 132) e espelha `LAKE_DISC_R`/`MARSH_DISC_R` antigos.
- `godot/gameplay/Loot.gd` — POIs e o anel radial de spawn.
- A queda: **não existe colisão aérea no castelo**, e uma ilha 62× maior muda a
  rota de queda inteira.

---

## 3. PROPOSTA DE ADOÇÃO EM ONDAS

> Tudo nesta seção é **PROPOSTA** minha. Nada aqui foi decidido.

O que salva a ideia é separar a **organização** (que é escalável e barata) da
**escala** (que é caríssima). A leitura pentagonal funciona igual em 300 m e em
2,4 km — pentágono é uma relação entre cinco setores, não uma distância.

### ONDA 1 — adotável JÁ, sem tocar em escala (PROPOSTA)

Custo: reorganização de constantes e de cor em `Island.gd`. Zero terreno novo,
zero binário novo, zero crédito Meshy. **Não implementar aqui** — este documento
não toca `.gd`; é proposta para a raia MUNDO.

1. **Reatribuir os 7 POIs que já existem aos 5 domínios elementais**, em
   disposição pentagonal em volta do vale central que o `height()` já cava
   (`valley = 1 - smoothstep(7, 50, r)`, ou seja, já existe um vale circular de
   50 m no meio da ilha — a estrutura do pacote **já está lá**, sem nome):
   *(uso os nomes das constantes e as coordenadas XZ reais, não pontos
   cardeais — o eixo Z do Godot inverte a intuição e não quero cravar norte/sul
   num documento de arte. Os cinco já estão em cinco setores distintos, que é o
   que a leitura pentagonal precisa.)*
   - Fogo → `DUNES` `(10, -98)` vira campo de cinza/obsidiana: é troca de **cor
     de vértice**, `COL_DUNE` → cobre queimado. Custo: uma constante.
   - Água → `LAKE` `(75, 30)` já é água; recebe a leitura de Nymara.
   - Terra → `RUINS` `(63, -73)`, o platô de 9 m, vira a Pedreira dos Colossos.
   - Vento → `FOREST` `(-60, -66)` vira o Bosque Suspenso.
   - Raio → `PEAK` `(12, 84)`, os 18 m, vira os Picos de Fulgar. Já é o ponto
     mais alto e já tem **uma única encosta subível** — que é exatamente o
     desenho de POI de topo que o pacote pede.
   - Neutro → o vale central, que ganha nome: **Vale da Convergência**.
   - `MARSH` e `BAY` seguem como zonas menores.
2. **Batizar tudo com os nomes do pacote.** Nome é grátis e é metade da
   identidade. O mapa tático dele passa a ser a fonte da verdade dos nomes.
3. **Cor por domínio, não geometria por domínio.** As cinco cores elementais já
   estão na paleta e três já estão no `Island.gd` (`COL_MUD` = Terra,
   `COL_REED` = Vento). Ler a ilha por cor de cima é o critério 3 do pacote, e
   sai de graça.
4. **Altar de Sintonia no centro do vale** — ver §6, é decisão do Diretor.
5. **Cobertura a cada 12–25 m nas zonas de confronto** (regra §6 dele). Essa é
   uma regra de qualidade que o jogo pode adotar já, e é o gancho natural das
   peças-herói do §5.

### ONDA 2 — a ilha intermediária (PROPOSTA: envelope de 600 m)

Proponho **600 m** de lado, e o número não é estético — é o maior salto que a
malha atual absorve sem custo de triângulo:

- **Chão de graça.** Mantendo `QUADS = 132`, o quad passa de 2,27 m para
  **4,55 m**, e os 34.848 triângulos de chão **não mudam**. O cabeçalho do
  `Island.gd` diz que o detalhe mais fino que a malha precisa carregar é a
  ondulação curta de ~12 m de comprimento de onda — o Nyquist disso é 6 m de
  amostragem, e 4,55 m **passa com folga**. A 800 m o quad vira 6,06 m e cruza a
  linha: aí seria preciso `QUADS = 176` (quad de 4,55 m) e **61.952 tris**,
  1,8× o chão. **600 m é o degrau barato; 800 m é o primeiro degrau pago.**
- **Área:** raio de terra 132 → 264 m, terra ≈ **0,219 km²** = **4,0× hoje**.
- **Travessia:** diâmetro de terra 528 m → **70 s** a 7,5 m/s. Para manter os
  19 % de partida de hoje, a partida quer ~370 s. **PROPOSTA: partida de 300 s
  (5 min)** → travessia = 23 % da partida. Comparável e testável.
- **Encontros:** 0,5·√(218.983/7) = **88 m** de vizinho médio = 2,3 alcances de
  arma. Duas vezes mais esparso que hoje — sentível, mas jogável. Com **12
  pawns** cai para **68 m**; com 20, para **52 m**, praticamente os 44 m de
  hoje. Conclusão honesta e útil: **600 m é o tamanho de ilha que 20 jogadores
  querem.** O que bloqueia não é o terreno — é a rede.
- **O custo real** não é o chão: é decoração (158 → ~630 árvores), o grid de
  colisão, e **reescrever `Zona.gd`** (`RAIO_INICIAL` e a tabela `FASES` ×2 a
  partir de 300 m, ×3,33 a partir do `ILHA_REF = 180` contra o qual foram
  escritas).
- **Pré-requisito absoluto:** medir FPS no Poco F4 **antes**. Sem a medição de
  1×, não se aprova 4×.

### ONDA 3 — claramente pós-rede / pós-MVP (PROPOSTA de recusa por ora)

Não são ideias ruins. São ideias cujo pré-requisito não existe:

| Item | O que falta primeiro |
|---|---|
| 2,4 × 2,2 km / 3,4 km² | streaming de terreno + LOD de terreno + FPS medido; sem os três, é impossível, não "pesado" |
| **20 jogadores** | **rede.** `docs/infra/` não descreve servidor nenhum; hoje são 1 humano + 6 bots. É o pré-requisito de tudo o mais |
| Rede subterrânea de cavernas | um sistema de interior/oclusão que não existe; e o corte lateral pede um segundo mapa completo |
| 10 POIs + 10 zonas menores | loot em cluster por POI (hoje é anel radial), e área para separá-los |
| Altitude de 240 m | 13× a vertical atual; muda a queda, a zona e o alcance visual |
| Terraços de Nymara como a 07 mostra | é uma cidade; é escopo de estúdio, não de fase |

---

## 4. CAMINHO 3D, honesto

### 4.1 O que o Meshy NÃO faz — dito explicitamente

**O Meshy não faz o terreno, não faz a ilha, não faz a costa e não faz as
cavernas.** Isso é **código procedural** — `Island.height()`, `ArrayMesh`,
colisão trimesh — e continuará sendo. Mandar a `02-visao-aerea-geral.png` para
o Meshy devolveria um **relevo esculpido de peça única**: sem `height()`, ou
seja, sem spawn confiável, sem colisão editável, sem consulta de altura, sem
determinismo, e sem a possibilidade de mudar `SIZE` outra vez. Perderíamos a
propriedade mais valiosa da ilha atual, que está escrita no topo do arquivo:
**"a ilha é sempre a MESMA"**. É a mesma lição da folha de personagem de 21/08,
em escala de mapa: o Meshy trata a imagem como **um objeto**.

Além disso, `docs/MESHY.md` §4 é claro sobre o que ele **não** entrega, e vale
para prop igual: topologia irregular, nenhum LOD, nenhuma colisão de jogo
(nunca usar trimesh de IA), e hi-poly de ~2 M triângulos que **precisa** passar
pelo Blender. O Meshy faz **props**. É bom nisso.

### 4.2 O que dá para modelar HOJE, com qualidade — e o critério de escolha

O critério vem da §3.5 do `docs/PROJETO.md`, que já pagou essa lição: as
ruínas foram escolhidas para abrir a fila porque **davam o maior salto de
"parece jogo publicado" por crédito E não estavam presas a sistema nenhum** —
se dessem errado, joga-se fora sem quebrar nada. Aplicando o mesmo filtro:

| Peça-herói | Retorno visual | Preso a sistema? | Veredito |
|---|---|---|---|
| **Estátua Inacabada dos Colossos** | altíssimo — é a silhueta que se vê da queda | não (cenário puro) | **FILA** |
| **Arco de Calcário de Nymara** | alto e barato — concept já pronta para o Meshy | não (cobertura) | **FILA** |
| **Rocha de Basalto Modular** | alto por reuso — uma peça, a ilha inteira | não (cobertura) | **FILA** |
| **Casa de Vigia** | alto — transforma zona menor em lugar | não | **FILA** (corrigir o brasão antes) |
| **Piso de Runa Reativa** | médio-alto, e é plano = baratíssimo | não | **FILA** |
| **Árvore-catedral de Aeris** | altíssimo | **CUIDADO** | **FILA, com cláusula** |
| Torre Arcana | — | — | **FORA**: já tem concept aprovada e canônica (`cenario/07-torre-arcana/`); regerar do pacote **bifurca o canon** |
| Altar de Sintonia | — | — | **FORA do pacote**: o canônico está aprovado (`cenario/08-altar-sintonia/`); se entrar, entra por `docs/prompts/08` |
| Forja Sepultada | alto | **sim** — é INTERIOR | **FORA**: interior pede colisão, navegação e oclusão; não há sistema |

**A cláusula da árvore-catedral, que é a armadilha do lote.** A §3.5 item 3 do
`PROJETO.md` diz que `tree_count()` / `tree_pos()` / `set_tree_burned()` são o
que permite **queimar a floresta e abrir caminho** — pilar do GDD §14 — e que as
158 árvores custam praticamente **um desenho por quadro** porque compartilham a
mesma malha via MultiMesh. **Um modelo importado tem material próprio e quebra
isso.** Portanto: a árvore-catedral entra **como LANDMARK ÚNICO**, um objeto
solitário no POI do Vento, **sem tocar nas 158**. Se alguém trocar a árvore de
MultiMesh por modelo Meshy, perde-se o fogo e o desempenho no mesmo commit.

### 4.3 Custo estimado

`cenario/00-MODELOS-3D.md` registra a medição real: **7 gerações × 30 = 210
créditos**, saldo **2.984** naquele fechamento (o coordenador reporta ~2.964
hoje — a diferença não muda nada da conta).

| Item | Créditos |
|---|---:|
| 6 peças-herói × 30 | **180** |
| Margem de 1 regeração (a taxa histórica é ~1 em 6: a Conjurador v2) | 30 |
| **Total previsto** | **~210** |
| Saldo depois | **~2.750** |

**7 % do saldo** para as seis peças que dão identidade visual à ilha inteira.
As **imagens** das vistas são grátis (gerador de imagem), e é nelas que está o
trabalho — não no crédito.

---

## 5. PROMPTS PRONTOS PARA O MESHY

### 5.1 O que aproveitar do `CADERNO-DE-PROMPTS.md` dele, e o que corrigir

**Aproveitar, sem mudar:**

1. **A âncora de estilo comum.** É praticamente a nossa, palavra por palavra
   (superfície pintada à mão, sombra de contato forte, silhueta exagerada,
   legível em mobile, mundo antigo retomado pela natureza).
2. **A paleta.** Os dez hex são **idênticos** aos do `docs/prompts/00-LEIA.md`.
   Nada a fazer.
3. **O template de objeto isolado.** *"Exactly one [ASSET], dimensions [X × Y ×
   Z], centered and fully visible... clean dark neutral gray gradient background
   with a soft contact shadow... no environment, characters, text, labels, UI,
   watermark, extra objects"* — está certo e é a nossa regra nº 2 já escrita.
4. **"As dimensões escritas têm prioridade sobre a perspectiva da imagem"**
   (README §2, item 7) — é a nossa regra nº 5, **Dimensões são LEI**. Mérito.
5. **Uma composição principal por arquivo** e **4 vistas em arquivos separados
   antes de gastar crédito** (README §8) — é a nossa regra nº 1, a de 30
   créditos. Ele chegou nela sozinho.

**Corrigir, porque está em desacordo com o pipeline provado:**

1. **O 3/4 como vista principal — este é o erro que já nos custou dinheiro.**
   O template dele manda *"three-quarter orthographic-like view"* e **nunca pede
   perfil de 90°**. A auditoria registrada em `docs/MESHY.md` §3 mediu o
   resultado disso: a `vista-3-4.png` saiu **frontal repetida em 12 de 12
   arquivos conferidos**, e a "lateral" saiu um três-quartos de 60–70°. **Não
   existia perfil de 90° no lote**, e perfil é justamente a informação que a
   frontal não carrega — a espessura. É por isso que o `/multi-image-to-3d`
   está bloqueado até hoje. **Correção:** a vista de LADO é comandada como
   *"true 90-degree side elevation, camera exactly perpendicular to the object's
   side, zero rotation toward the viewer"*, e o negative prompt dela ganha
   *"three-quarter view, rotated, angled, perspective"*. O 3/4 vira a **quarta**
   vista, opcional, nunca a única.
2. **A proibição da folha não está DENTRO do prompt.** Ele separa as vistas em
   arquivos, mas o texto do prompt não proíbe painel/grade. Nossos arquivos
   carregam a proibição no corpo, porque é ali que o gerador lê.
3. **Negative prompt por peça.** O caderno dele dilui as negativas na prosa da
   âncora. Nossa regra nº 6: cada peça tem o seu, explícito e no fim.
4. **Faltam os parâmetros do job.** Nenhuma palavra sobre configuração. O que o
   pipeline provado usa: **Ultra** + **Textura** ligada (PBR) + **Multi-View**
   com as vistas ortogonais aprovadas + licença **PRIVADO** (asset de jogo
   comercial não fica público na galeria) + geração **no SITE, na conta do
   Diretor** (DIREÇÃO §10.2, processos visíveis) + **revisão NO VIEWER antes de
   baixar** (regra nascida em 26/08, depois de o Diretor pegar dois defeitos na
   Conjurador v1 que eu ia deixar passar). **"Pose"** só se aplica a
   personagem — prop não leva T-pose.
5. **Sem regra de proporção de quadro por peça.** Ele fixa 16:9 para panorama.
   Peça ALTA precisa de **9:16 vertical** (é o que o `07-TORRE-ARCANA.md` faz),
   senão a peça nasce pequena no meio do quadro. Peça larga vai em quadrado.
6. **Sem referência de escala verificável.** "8 × 5 × 6 m" não é conferível
   olhando a imagem. A saída **não** é pôr uma pessoa (quebra a regra do objeto
   único): é **cravar na ficha um elemento de medida conhecida** — um degrau de
   0,30 m, um bloco de 1 m — e exigi-lo no prompt.

> **Nota de organização, para o Diretor decidir (§6):** a ordem de 26/08 em
> `docs/prompts/00-LEIA.md` diz que *"todo prompt solicitado vive AQUI, nesta
> pasta"*. Os prompts abaixo estão neste documento porque o coordenador pediu
> o veredito e o prompt no mesmo entregável. **Ao aprovar a fila, promover para
> `docs/prompts/09-ILHA-FRATURADA-PECAS-HEROI.md`** e deixar aqui só o
> apontamento — para não existirem duas cópias divergindo.

### 5.2 Âncora comum (abre TODOS os prompts abaixo)

> Stylized high-end 3D game art, hand-painted painterly textures, soft painterly
> surfaces, strong contact shadows, slightly exaggerated proportions, fantasy
> game asset concept, deep night-blue and gold palette (#0B1026, #F0C75E),
> blue-grey stone (#8E97AD) and weathered bronze with green patina, ancient
> magical world partially reclaimed by nature but still arcane-alive, mobile-
> readable large shapes before micro detail, no photorealism.

### 5.3 Âncora das VISTAS de produção (abre as quatro vistas de toda peça)

> [ÂNCORA COMUM] + single object centered and fully in frame, plain dark grey
> neutral gradient background, soft even lighting, one soft contact shadow, no
> ground scenery, no horizon, no people, no other objects, no character sheet,
> no grid of views, no multiple angles in one image, no text, no labels, no
> dimension lines, no watermark.

**As quatro vistas são quatro gerações separadas. Sempre.**

- **FRENTE** — *front elevation, camera exactly perpendicular to the front face.*
- **LADO** — *true 90-degree side elevation, camera exactly perpendicular to the
  object's side, zero rotation toward the viewer, perfect profile silhouette.*
- **COSTAS** — *back elevation, camera exactly perpendicular to the back face.*
- **TRÊS-QUARTOS** — *three-quarter view rotated exactly 45 degrees, slightly
  above eye level.*

### 5.4 Negative prompt comum (some ao específico de cada peça)

> photorealistic, photo, photograph, people, character, hands, text, letters,
> real alphabet, watermark, signature, UI, HUD, dimension lines, blueprint,
> multiple objects, object sheet, grid of views, several angles, collage, split
> screen, sci-fi, metal panels, modern concrete, brick, neon, chibi, childish,
> skull, gore, occult pentagram, hexagram, six-pointed star, religious symbol,
> blurry, low quality, cropped, cut off

---

### PROMPT 1 — ESTÁTUA INACABADA DOS COLOSSOS (Terra · landmark)

**Por que abre a fila:** é a silhueta mais forte do pacote (a `08-espinha-de-basalto-terra.png`),
é **cenário puro** — não está presa a sistema nenhum, exatamente o filtro da
§3.5 — e é o tipo de peça que se vê da queda e faz o mapa parecer publicado.

**Ficha física (é LEI):**

- **Altura total 14 m** contando o plinto. A figura tem **11 m** do plinto ao
  topo da cabeça; **4,5 m** de largura nos ombros; **3,0 m** de profundidade.
- **Está INACABADA, e é isso que ela conta:** esculpida do peito para cima em
  detalhe (rosto liso e sem feições finas, ombros, um braço só, o outro ainda
  um bloco bruto); da cintura para baixo continua **dentro de um bloco de rocha
  viva de 6 × 4 × 4 m**, com marcas de cinzel e degraus de desbaste.
- **Plinto:** 5 × 5 × **1,2 m** de blocos irregulares de basalto — a altura de
  1,2 m é deliberada: **cobertura de agachar** de verdade.
- **Materiais:** basalto cinza-azulado (`#8E97AD`) em colunas verticais
  fraturadas, veios de ocre (`#A8763E`) e ouro fosco (`#8A7336`) nas fendas,
  musgo verde-escuro na face norte.
- **Escala conferível:** três **degraus de desbaste de 0,30 m cada** na face do
  bloco bruto. É por eles que se mede a peça na imagem.
- **Vida e dano:** um dos ombros lascado com estilhaços no pé do plinto;
  argolas de bronze patinado cravadas na pedra (onde o andaime esteve); uma
  bandeirola azul-noite rasgada com o **Selo pentagonal de Arkana**.
- **O andaime NÃO faz parte da peça.** Vira objeto separado ou nada — madeira
  fina destrói a silhueta no LOD e no celular.

**PROMPT MESTRE (apresentação · 9:16 VERTICAL — a peça é alta):**

> [ÂNCORA COMUM]
>
> One single colossal unfinished statue, 14 meters tall including its plinth, at
> amber dusk. The figure is carved only from the chest up — smooth featureless
> face, defined shoulders, one arm finished, the other arm still a raw
> rectangular block — and from the waist down it is still trapped inside a raw
> block of living rock 6 by 4 by 4 meters, covered in chisel marks and three
> roughing-out steps 30 centimeters each. It stands on a 5 by 5 by 1.2 meter
> plinth of irregular basalt blocks. Blue-grey fractured columnar basalt
> (#8E97AD) with ochre (#A8763E) and matte gold (#8A7336) veins in the cracks,
> dark green moss on one side. One shoulder is chipped with stone splinters
> fallen at the plinth's foot; patinated bronze rings are driven into the stone;
> a torn night-blue pennant with a golden pentagon seal hangs from one ring.
> Single landmark, no scaffolding, no people, silhouette readable from very far
> away and from above.

**VISTAS:** as quatro do §5.3, prefixando *"one single unfinished colossal
statue, 14 meters tall, raw stone block from the waist down"*. Na de **LADO**,
somar: *"the 3-meter depth and the raw block behind the figure must be clearly
readable in true profile"*.

**NEGATIVE (soma ao comum):** *finished statue, complete legs, smooth polished
marble, classical Greek nude, religious icon, angel, scaffolding, wooden
cranes, ropes, multiple statues, cliff face, mountain, terrain*

**CRITÉRIOS DE APROVAÇÃO:**
1. Lê como **INACABADA** em 1 segundo (bloco bruto embaixo, detalhe em cima)?
2. O plinto de 1,2 m lê como cobertura de agachar, e não como pedestal de museu?
3. Os três degraus de 0,30 m estão lá para conferir a escala?
4. A silhueta vista **de cima** (a queda) é reconhecível?
5. Zero andaime, zero corda, zero pessoa na malha?
6. Cabe em `#8E97AD` + `#A8763E` + `#8A7336` — sem mármore branco?

---

### PROMPT 2 — ARCO DE CALCÁRIO DE NYMARA (Água · cobertura atravessável)

**Por que entra:** a `19-arco-calcario-nymara.png` já está **no padrão do
Meshy** — objeto único, fundo neutro, pintada à mão, paleta certa. É o crédito
mais barato do lote e o menor risco. É cobertura **e** passagem no mesmo objeto.

**Ficha física (é LEI):** dimensão dele, mantida — **6 × 2 × 5 m**
(largura × profundidade × altura).

- Vão livre de **2,6 m de largura × 3,4 m de altura** — passagem folgada para
  um mago em movimento; as pernas do arco dão **cobertura de pé** dos dois
  lados.
- Calcário claro (`#C9D4DE`) com **escorrimento azul-ciano** (`#2AA7FF`)
  descendo das juntas — a marca de água de Nymara.
- **Três cintas de bronze patinado** e uma **pedra-chave** com a gema em gota
  d'água acesa em `#2AA7FF`.
- Base em dois degraus de **0,30 m** (escala conferível). Hera verde-escura
  numa perna só; três blocos do topo faltando de um lado (assimetria).

**PROMPT MESTRE (4:3):**

> [ÂNCORA COMUM]
>
> Exactly one weathered stone archway, 6 meters wide by 2 deep by 5 tall, pale
> limestone (#C9D4DE) in irregular courses with cyan-blue water staining
> (#2AA7FF) running down from the joints. Clear opening 2.6 meters wide and 3.4
> tall. Three patinated bronze bands wrap the arch, and the keystone holds a
> glowing water-drop gem (#2AA7FF) set in a bronze bezel with geometric Arkana
> runes. Two-step base, each step 30 centimeters. Dark green ivy climbs one leg
> only; three stones are missing from the top on one side, lying at its foot.
> Single object, freestanding, no wall attached, no environment, no people.

**VISTAS:** as quatro do §5.3. No **LADO**: *"true profile showing the 2-meter
depth of the arch and the thickness of both legs"* — é a espessura que a
frontal não informa.

**NEGATIVE (soma ao comum):** *white marble, polished, temple, church, gate with
doors, wall attached, ruins field, triumphal arch, roman, several arches*

**CRITÉRIOS DE APROVAÇÃO:**
1. O vão é **atravessável** (2,6 × 3,4 m lido na imagem) e as pernas dão
   cobertura de pé?
2. A profundidade de 2 m aparece na vista de LADO, em perfil verdadeiro de 90°?
3. Escorrimento ciano presente, sem virar arco azul inteiro?
4. Assimetria (blocos faltando de UM lado, hera em UMA perna)?
5. Os degraus de 0,30 m estão lá?

---

### PROMPT 3 — ROCHA DE BASALTO MODULAR (Terra · a peça de maior reuso)

**Por que entra:** é a peça com o **melhor retorno por crédito de todo o
pacote**, porque não é um landmark — é uma **peça modular** que se repete pela
ilha inteira em rotação e escala. Uma geração, cobertura para todos os cinco
domínios. E é cobertura: não está presa a sistema nenhum.

**Ficha física (é LEI):** dimensão dele, mantida — **8 × 5 × 6 m**.

- **Colunas hexagonais verticais** de basalto, alturas diferentes: face alta de
  **6 m** (parede que bloqueia visão), degraus de **1,2 m** e **2,2 m**
  (cobertura de agachar e de pé), e uma **rampa escalonada** de degraus de
  0,60 m subindo por um flanco até o topo — porque a regra §6 dele exige que
  toda posição alta tenha acesso, e toda cobertura tenha rota.
- **Topo aproximadamente plano** de 3 × 2 m: dá para ficar em cima.
- Nicho/abrigo baixo de **1,8 m de altura** numa face (esconderijo).
- Basalto cinza-azulado `#8E97AD`, líquen ocre `#A8763E` nas quinas, musgo nas
  fendas de sombra. **Duas runas geométricas douradas** (`#F0C75E`) gravadas
  em colunas diferentes.
- **Base plana, apoiada em y = 0** — é peça de instanciar; não pode nascer
  flutuando nem afundada.

**PROMPT MESTRE (4:3):**

> [ÂNCORA COMUM]
>
> Exactly one modular basalt rock formation, 8 meters wide by 5 deep by 6 tall,
> made of vertical hexagonal basalt columns broken at different heights: one
> tall 6-meter face, ledges at 1.2 and 2.2 meters, and a stepped ramp of
> 60-centimeter steps climbing one flank to a roughly flat 3 by 2 meter top. A
> low 1.8-meter-tall shelter niche opens in one face. Blue-grey basalt
> (#8E97AD), ochre lichen (#A8763E) on the edges, moss in the shadowed cracks,
> two geometric golden Arkana runes (#F0C75E) carved into two different columns.
> Flat base sitting on the ground. Single object, no environment, no other
> rocks, no people.

**VISTAS:** as quatro do §5.3. No **LADO**: *"true 90-degree profile showing the
5-meter depth and the stepped ramp in silhouette"*.

**NEGATIVE (soma ao comum):** *photorealistic rock scan, boulder field, several
rocks, lava, glowing lava cracks, floating rock, round pebble, sand, cliff,
terrain, mountain*

**CRITÉRIOS DE APROVAÇÃO:**
1. As três alturas de cobertura (1,2 / 2,2 / 6 m) são distinguíveis a olho?
2. A rampa de subida existe e lê como **subível**?
3. A base é plana e assentada (peça de instanciar, não pedra flutuante)?
4. **Estilizada, não fotorrealista** — este é o defeito que reprovou a nº 18 do
   pacote.
5. Repetível: rodando 90° ela ainda parece uma pedra diferente?

---

### PROMPT 4 — CASA DE VIGIA (micro-POI · o brasão CORRIGIDO)

**Por que entra:** é o que transforma "zona menor de pouso" em **lugar** —
silhueta reconhecível na queda, interior curto, duas saídas. E carrega a
correção mais importante da revisão: **o brasão errado**.

**Ficha física (é LEI):** dimensão dele, mantida — base **5 × 5 m**, altura
**7 m**.

- Dois pavimentos: térreo de 2,6 m de altura livre com **duas portas em faces
  opostas** (a regra "toda zona oferece ao menos duas saídas" resolvida na
  arquitetura); sacada de vigia no segundo com **mureta de 1,1 m** (cobertura).
- Telhado cônico de ardósia azul-noite, **rasgado num quadrante** — mostra o
  interior e dá a leitura de "abandonado mas em pé".
- Alvenaria cinza-azulada `#8E97AD`, cantos e cintas de **bronze patinado**,
  seteiras acesas em âmbar `#F0C75E`.
- **CORREÇÃO OBRIGATÓRIA:** a bandeirola azul-noite leva o **SELO DE ARKANA —
  um PENTÁGONO de contorno dourado com um "A" geométrico dentro**. A concept
  nº 25 do pacote trouxe uma **estrela de seis pontas**, que não é a nossa
  marca e carrega leitura religiosa que a §9 da especificação dele mesmo
  exclui. Nada de hexagrama, nada de estrela de cinco pontas, nada de
  pentagrama.
- Escala conferível: **três degraus de 0,30 m** na porta principal.

**PROMPT MESTRE (9:16 VERTICAL):**

> [ÂNCORA COMUM]
>
> Exactly one small abandoned watch house, 5 by 5 meters at the base and 7
> meters tall, two floors. Ground floor with 2.6-meter clear height and two
> ogival doorways on opposite faces; second floor with a lookout balcony and a
> 1.1-meter stone parapet. Conical night-blue slate roof, torn open in one
> quadrant revealing the interior beams. Blue-grey stone masonry (#8E97AD) with
> patinated bronze corner bands, narrow arrow-slit windows glowing amber
> (#F0C75E). A night-blue pennant hangs on the wall bearing the Arkana seal: a
> single golden outlined PENTAGON with a geometric letter A inside it. Dark green
> ivy climbs one face; three stones fallen at the doorway; three 30-centimeter
> steps at the main door. Single building, no surrounding scenery, no people.

**VISTAS:** as quatro do §5.3. **FRENTE** = a porta principal com os degraus;
**COSTAS** = a segunda porta (provar que ela existe); **LADO** = perfil
verdadeiro com a sacada e o rasgo do telhado.

**NEGATIVE (soma ao comum):** *six-pointed star, hexagram, Star of David,
pentagram, five-pointed star, cross, religious symbol, church, chapel, house
with chimney, cottage, village, thatch, several buildings*

**CRITÉRIOS DE APROVAÇÃO:**
1. **O brasão é um PENTÁGONO com "A"?** Qualquer outra coisa = reprovado, sem
   discussão.
2. As **duas portas em faces opostas** existem (conferir na vista de COSTAS)?
3. A mureta de 1,1 m da sacada lê como cobertura jogável?
4. O telhado rasgado deixa ver o interior sem a casa parecer ruína total?
5. Silhueta reconhecível **de cima** (é micro-POI de pouso)?

---

### PROMPT 5 — PISO DE RUNA REATIVA (Sintonia · modular e baratíssimo)

**Por que entra:** é **plano** — o modelo mais barato que existe, o mais fácil
de decimar, o de menor risco de topologia — e é o que dá vocabulário visual ao
**terreno reativo** e à Sintonia, que é o pilar do GDD. A nº 32 do pacote já
está certa e **já traz o Selo pentagonal correto**: é a peça que prova que ele
tem a marca certa em mão.

**Ficha física (é LEI):** dimensão dele, mantida — **4 × 4 × 0,25 m**.

- Laje quadrada de lajotas irregulares de pedra cinza-azulada `#8E97AD`,
  moldura de blocos de **0,25 m** em volta (a espessura é a própria régua) e
  **quatro cantoneiras** de bronze patinado com runas geométricas.
- No centro, um **canal de bronze** formando o **PENTÁGONO** do Selo, com um
  sulco saindo dele para cada borda — **é por esse sulco que a energia corre**,
  e é isso que o jogador precisa ler antes de ativar.
- **Duas variantes na mesma geração de imagens** (o mesmo modelo, material
  trocado no motor): sulco **APAGADO** (bronze seco, fosco, com musgo) e sulco
  **ACESO** (`#F0C75E`, com brilho fraco). Gerar as vistas com o sulco
  APAGADO — o aceso é material dinâmico no jogo, não geometria.
- Musgo e cascalho em **dois cantos opostos**, para a laje encaixar em rotação
  sem repetição óbvia.
- **Base plana em y = 0**, borda reta: é peça de encaixar lado a lado.

**PROMPT MESTRE (1:1 quadrado, vista de cima em ~60°):**

> [ÂNCORA COMUM]
>
> Exactly one modular stone floor tile, 4 by 4 meters and 25 centimeters thick,
> seen from a high three-quarter angle. Irregular blue-grey stone flagstones
> (#8E97AD) inside a border of 25-centimeter blocks, with four patinated bronze
> corner pieces carved with geometric Arkana runes. At the center, a bronze
> channel forms a PENTAGON, with one straight groove running from the pentagon
> to each edge of the tile. The grooves are DARK and dry — dull unlit bronze with
> moss growing in them. Moss and gravel in two opposite corners only. Flat
> bottom, straight edges, tileable. Single object, no environment, no people, no
> glow.

**VISTAS:** **TOPO** (ortográfica de cima, o padrão inteiro legível),
**LADO** (perfil verdadeiro de 90° mostrando os 0,25 m de espessura e a
moldura), **TRÊS-QUARTOS ALTO**. Aqui não há frente/costas — a peça é
simétrica; declarar isso é melhor que gerar duas imagens iguais.

**NEGATIVE (soma ao comum):** *glowing runes, lit magic circle, neon, five-
pointed star, pentagram, hexagram, occult symbol, mandala, mosaic, tiles floor
of a room, walls, several tiles, seamless texture, top-down flat texture map*

**CRITÉRIOS DE APROVAÇÃO:**
1. O símbolo central é um **PENTÁGONO**, e não um pentagrama (linhas cruzadas)?
2. Os sulcos chegam **às quatro bordas** (a laje precisa emendar na vizinha)?
3. Os 0,25 m de espessura leem na vista de LADO?
4. O sulco está **APAGADO** (bronze seco), sem brilho gerado na textura?
5. A borda é reta e a base plana, para encaixar lado a lado?

---

### PROMPT 6 — ÁRVORE-CATEDRAL DE AERIS (Vento · landmark, COM CLÁUSULA)

> **CLÁUSULA, e ela é a mais importante de todo este documento:** esta peça
> entra como **LANDMARK ÚNICO** do POI do Vento. Ela **NÃO substitui, não
> encosta e não conversa** com as **158 árvores em MultiMesh** do `Island.gd`.
> `docs/PROJETO.md` §3.5 item 1 e 3: as 158 custam praticamente **um desenho por
> quadro** porque compartilham a mesma malha procedural, e
> `tree_count()`/`tree_pos()`/`set_tree_burned()` são o que permite **queimar a
> floresta e abrir caminho** (GDD §14). Modelo importado tem material próprio,
> quebra o MultiMesh e obriga a refazer essa fiação. **Uma árvore-herói: sim.
> Trocar a floresta: NÃO.**

**Ficha física (é LEI):**

- **Altura total 26 m.** Tronco de **5,5 m de diâmetro** na base, afinando para
  3 m aos 9 m de altura, onde **se parte em três limbos** grossos.
- **As três plataformas são o gameplay** (é a leitura da ficha dele: "copa forma
  três plataformas naturais"), nas alturas **9 m, 14 m e 19 m**, cada uma de
  **4 a 6 m** de diâmetro, formada pelo entrelaçamento dos limbos — **chão
  plano de verdade**, não galho inclinado.
- **Cada plataforma tem parapeito de raiz de 1,1 m** (cobertura) e **duas
  aproximações**: uma **rampa de raiz** larga de 1,2 m subindo em espiral pelo
  tronco, e um **toco de ponte-raiz** saindo lateralmente (por onde a
  `20-ponte-raiz-aeris.png` se conecta). Regra §6 dele: toda posição alta
  precisa de dois acessos.
- **Está PARTIDA:** o limbo mais alto está tombado e quebrado, com a fratura
  clara de madeira e a copa daquele lado morta.
- Casca cinza-marrom com veios turquesa `#8FE8C9` nas fendas (o vento é
  visível); folhas **verde profundo com pontas douradas**; raízes-arco de 2 m de
  altura na base, dando abrigo embaixo.
- Escala conferível: os **degraus de 0,30 m** entalhados na rampa de raiz.

**PROMPT MESTRE (9:16 VERTICAL):**

> [ÂNCORA COMUM]
>
> One single colossal cathedral tree, 26 meters tall, trunk 5.5 meters in
> diameter at the base narrowing to 3 meters at 9 meters up, where it splits
> into three thick limbs. The interlacing limbs form three genuinely flat
> natural platforms at 9, 14 and 19 meters, each 4 to 6 meters across, each
> ringed by a 1.1-meter parapet of woven roots. A wide 1.2-meter root ramp with
> 30-centimeter carved steps spirals up the trunk, and a stub of a root-bridge
> reaches out sideways from each platform. The highest limb is BROKEN and fallen,
> its wood fracture exposed and its canopy dead on that side. Grey-brown bark
> with turquoise veins (#8FE8C9) glowing faintly in the fissures, deep green
> leaves with golden tips, two-meter arching roots at the base forming a shelter
> underneath. Single tree, no forest, no other trees, no people, silhouette
> readable from very far away and from above.

**VISTAS:** as quatro do §5.3. No **LADO**: *"true 90-degree profile — the three
platforms must read at three different heights and the broken limb must be
unmistakable"*.

**NEGATIVE (soma ao comum):** *forest, several trees, background trees, tree
house, wooden planks, ladders, ropes, treehouse village, cherry blossom, autumn
forest, pagoda, oriental shrine, glowing magic leaves, fairy lights*

**CRITÉRIOS DE APROVAÇÃO:**
1. As **três plataformas** estão nas três alturas e têm **chão plano**?
2. Cada plataforma tem **parapeito de 1,1 m** e **dois acessos** (rampa + toco
   de ponte)?
3. O limbo **partido** é inequívoco (a árvore é "catedral **partida**")?
4. Zero pagode, zero santuário oriental — foi a deriva da nº 09 do pacote.
5. Silhueta reconhecível **de cima** e a 200 m (é landmark de queda)?
6. Uma árvore só, sem floresta atrás.

---

## 6. DECISÕES DO DIRETOR

Lista curta. Nada abaixo foi decidido; tudo é PROPOSTA aguardando veredito.

1. **ESCALA FINAL.** Três caminhos, e não há um quarto:
   - **(A) Manter 300 m** e adotar só a ONDA 1 (nomes, pentágono elemental, cor
     por domínio). Custo perto de zero, ganho de identidade grande, nenhum risco
     de desempenho. **É a minha recomendação até o FPS ser medido.**
   - **(B) Ir para 600 m** com partida de **300 s** — o degrau que a malha
     absorve sem custo de triângulo (§3, ONDA 2). Reabre `Zona.gd`. **Só depois
     da medição de FPS no Poco F4.**
   - **(C) 2,4 km** — recusar por ora e registrar como visão pós-rede: pede
     streaming, LOD de terreno e 20 jogadores, e nenhum dos três existe.
2. **O ALTAR DE SINTONIA ENTRA ANTES DO PLAYTEST?** O canônico já está aprovado
   e com concept pronta (`cenario/08-altar-sintonia/arte/`), e no pacote ele é o
   coração do Vale da Convergência. **Minha proposta: NÃO antes do playtest.**
   A §3.5 é lei — funcionalidade antes de arte de cenário — e o Altar é a peça
   que **mais** encosta em sistema (a Sintonia). Entrar antes de a Sintonia ser
   jogada e medida é arriscar modelar em cima de regra que vai mudar. O que
   **pode** entrar já, de graça: o **nome** e o **lugar** (centro do vale que o
   `height()` já cava).
3. **QUAIS DUAS PEÇAS-HERÓI ABREM A FILA.** Minha proposta, pelo critério da
   §3.5 (maior salto visual por crédito **E** presa a sistema nenhum):
   - **1ª — Estátua Inacabada dos Colossos** (Prompt 1). Maior retorno de
     silhueta do pacote, cenário puro, risco zero de quebrar sistema.
   - **2ª — Rocha de Basalto Modular** (Prompt 3). Maior retorno por **reuso**:
     uma geração de 30 créditos que veste os cinco domínios inteiros e resolve
     a regra "cobertura a cada 12–25 m".
   - *(Se quiser uma terceira barata, o **Arco de Nymara** — a concept dele já
     está pronta para o Meshy sem retoque.)*
4. **A CORREÇÃO DO BRASÃO.** A `25-casa-vigia.png` traz **estrela de seis
   pontas** onde deveria estar o **Selo pentagonal**. Confirmar que o Selo é
   pentágono + "A" antes de qualquer geração — está errado em 1 das 26 imagens,
   e é o tipo de erro que se propaga se virar modelo.
5. **ONDE VIVEM ESTES PROMPTS.** A ordem de 26/08 (`docs/prompts/00-LEIA.md`)
   manda todo prompt viver em `docs/prompts/`. Autorizar a promoção do §5 para
   `docs/prompts/09-ILHA-FRATURADA-PECAS-HEROI.md`, para não haver duas cópias.
6. **PEDIR OS QUATRO .ZIP QUE FALTAM.** Cinco das 32 peças do inventário dele
   não vieram (§1.6), e o `SHA256SUMS(2).txt` lista cinco arquivos dos quais só
   um chegou.

---

## 7. O QUE ESTE DOCUMENTO FEZ NO REPOSITÓRIO

**Nada de código.** Nenhum `.gd` foi tocado. Nada foi commitado — o coordenador
commita.

### 7.1 Arte espelhada

**Destino:** `cenario/ilha-fraturada/arte/` — segue o espelho
`cenario/<peça>/arte/` que as outras oito peças já usam (`01-castelo-voador`,
`02-luva-comum`, … `08-altar-sintonia`).

**27 PNG + 1 SVG · 61,7 MB.** Conferido: 27 MD5 distintos, nenhuma duplicata.

**Sobre os nomes.** Os cinco arquivos de `01-MASTER/`+`02-MAPAS/` foram copiados
**com o nome exato**. Os 26 de `concept/` vinham com nome de gerador
(`ChatGPT Image 27 de ago. de 2026, 09_09_17 (13).png`) — nome que não sobrevive
a nenhuma revisão futura e que já contradiz a convenção do repo. Como o
**`README-APROVACAO.md` dele nomeia todas as 32 peças**, identifiquei cada
imagem visualmente (abri as 26, uma a uma) e apliquei **o nome canônico que ele
mesmo escolheu**. Ou seja: os nomes são dele; o que fiz foi colar o nome no
arquivo certo. A tabela de identificação:

| Nome no repo | Origem em `concept/` (sufixo) | O que a imagem é |
|---|---|---|
| `06-caldeira-de-cineris-fogo.png` | `09_09_16 (5)` | panorama Fogo |
| `07-terracos-de-nymara-agua.png` | `09_09_16 (6)` | panorama Água |
| `08-espinha-de-basalto-terra.png` | `09_09_16 (7)` | panorama Terra |
| `09-bosque-suspenso-de-aeris-vento.png` | `09_09_16 (8)` | panorama Vento |
| `10-picos-de-fulgar-raio.png` | `09_09_17 (9)` | panorama Raio |
| `11-vale-da-convergencia.png` | `09_09_17 (10)` | vale central + altar |
| `12-zona-inicial-equipamento.png` | `09_09_17 (11)` | micro-POI de pouso |
| `13-rotas-de-travessia.png` | `09_09_17 (12)` | as quatro rotas |
| `14-costa-e-cavernas.png` | `09_09_17 (13)` | praia + boca de caverna |
| `15-tempestade-arcana-fechamento.png` | `09_09_17 (14)` | zona fechando |
| `16-prancha-escala-integracao.png` | `09_09_17 (15)` | prancha de escala |
| `17-rocha-basalto-modular.png` | `09_09_17 (16)` | módulo isolado |
| `18-rocha-vulcanica-cobertura.png` | `09_09_17 (17)` | módulo isolado |
| `19-arco-calcario-nymara.png` | `09_09_17 (19)` | módulo isolado |
| `20-ponte-raiz-aeris.png` | `09_09_17 (18)` | módulo isolado |
| `21-pilar-condutor-fulgar.png` | `09_09_18 (20)` | módulo isolado |
| `25-casa-vigia.png` | `09_09_18 (26)` | módulo isolado (brasão errado) |
| `28-pedestal-de-arma.png` | `09_09_20 (27)` | módulo isolado |
| `29-arvore-folhas-douradas.png` | `09_09_24 (29)` | módulo isolado |
| `30-arvore-carbonizada-renascendo.png` | `09_09_22 (28)` | módulo isolado |
| `31-juncos-nymara.png` | `09_09_24 (30)` | módulo isolado |
| `32-piso-runa-reativa.png` | `09_09_24 (31)` | módulo isolado (Selo correto) |

As lacunas na numeração (22, 23, 24, 26, 27) são as **cinco peças que não
vieram** — ver §1.6.

### 7.2 Git LFS — conta da cota, porque o projeto monitora

`.gitattributes` já cobre este lote: a regra
`cenario/**/arte/*.png filter=lfs diff=lfs merge=lfs -text` pega
`cenario/ilha-fraturada/arte/*.png`. **Verificado com `git check-attr filter`:
`filter: lfs` nos dois extremos do lote.** Nenhuma edição de `.gitattributes`
foi necessária.

| | |
|---|---:|
| Objetos LFS já rastreados | 225 |
| Loja LFS local hoje | **538 MB** |
| Este lote | **+61,7 MB** (27 PNG) |
| **Novo total** | **~600 MB** |
| Cota gratuita do GitHub | 1 GB de armazenamento · 1 GB de banda/mês |
| **Clones limpos por mês antes de estourar a banda** | **~1,7** |

O `04-mapa-tatico-identificado.svg` (8 KB) **não** entra no LFS — é texto, e
`git check-attr` confirma `filter: unspecified`. Correto assim.

**O aviso que o `.gitattributes` já dava fica mais apertado:** a cada lote de
arte o número de clones limpos por mês cai. Em ~600 MB de 1 GB, estamos em
**60 % do armazenamento** e a banda dá **menos de dois clones**. As saídas
continuam sendo duas, e só duas: **data pack** ou **storage externo**. Vale a
pena o Diretor saber que este lote consumiu ~6 % da cota total de uma vez.
