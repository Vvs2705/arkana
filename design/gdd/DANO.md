# DANO — o modelo de combate do ARKANA

> Estudo de balanceamento. Fontes da verdade: `docs/GDD.md` §3, §4, §5, §9, §10,
> §14, §16.2, §16.3, §16.4 · `godot/core/Balance.gd` · `godot/gameplay/Combat.gd`
> · `godot/gameplay/Projectile.gd` · `godot/terrain/TerrainSystem.gd` ·
> `personagens/01..20`.
>
> **Este documento não muda código.** Tudo o que ele recomenda está isolado nas
> seções **"O QUE IMPLEMENTAR"** (§5) e na **tabela de KNOBs** (§7), em formato
> pronto para aplicar em `Balance.gd`. Nenhum número do GDD foi alterado: onde há
> tensão entre GDD e código, o documento aponta a tensão e propõe a decisão.
>
> Classificação 10+: em todo o documento, um alvo com vida zerada é
> **"derrubado"**. Não existe dano localizado, mutilação ou gore no modelo.

---

## 0. Como ler os números daqui

Três definições que faltavam no projeto e que fazem os números pararem de brigar:

| Termo | Definição operacional |
|---|---|
| **TTK de cadência** | do primeiro *cast* ao último *cast*. `(tiros − 1) × cadência`. Não conta viagem. É a régua com a qual o GDD §5 escreveu "1,5–2,5s". |
| **TTK a 12m** | TTK de cadência **+ o tempo de viagem do último projétil** a 12m (a `ATTACK_DIST` do bot, `gameplay/Bot.gd:9`). É o que o jogador sente. Sempre maior que o de cadência. |
| **DPS sustentado** | `dano ÷ max(cadência, custo_de_mana ÷ regen)`. É o DPS real numa luta longa, porque **a mana, não a cadência, é o teto de verdade** (GDD §4.2: economia única). |

> **A ambiguidade que custou caro.** O GDD §5 diz "TTK alvo ~1,5–2,5s contra a
> vida base" sem dizer se a viagem conta. Com projétil de tempo de viagem (§4.1,
> lei do jogo) a diferença é de 0,3 a 0,7s — mais de um quarto da faixa inteira.
> **Decisão proposta:** a faixa do GDD §5 é a de **cadência**; toda medição passa
> a publicar as duas colunas. Assim nenhum número do GDD muda e o relatório para
> de comparar réguas diferentes.

Referência de alcance para toda medição: **12m**. Corpo a corpo e 30m aparecem
nas tabelas de viagem, mas o alvo de balanceamento é 12m.

---

## 1. ESTADO ATUAL MEDIDO

Extraído de `godot/core/Balance.gd` em 21/08/2026. Vida do player: **100,0**.
Mana: **100,0**, regen **14,0/s**.

### 1.1 A tabela real dos 5 elementos

| Elemento | dano | cadência (s) | mana | vel. projétil (m/s) | alcance (m) |
|---|---|---|---|---|---|
| Fogo | 13,0 | 0,27 | 9,0 | 24 | 40 |
| Água | 11,0 | 0,30 | 8,0 | 20 | 38 |
| Raio | 15,0 | 0,34 | 10,0 | 34 | 44 |
| Terra | 16,0 | 0,36 | 11,0 | 18 | 34 |
| Vento | 9,0 | 0,24 | 7,0 | 26 | 36 |

### 1.2 DPS — e o achado que quebra o modelo

| Elemento | DPS bruto | mana/s gasta | saldo de mana | tiros até secar | **DPS sustentado** | dano por mana |
|---|---|---|---|---|---|---|
| Fogo | 48,1 | 33,3 | −19,3 | 19 | **20,2** | 1,44 |
| Água | 36,7 | 26,7 | −12,7 | 26 | **19,2** | 1,38 |
| Raio | 44,1 | 29,4 | −15,4 | 19 | **21,0** | 1,50 |
| Terra | 44,4 | 30,6 | −16,6 | 16 | **20,4** | 1,45 |
| Vento | 37,5 | 29,2 | −15,2 | 27 | **18,0** | 1,29 |

**O DPS sustentado dos cinco elementos cabe numa faixa de 18,0 a 21,0 — 16% de
diferença entre o melhor e o pior.** O `dano por mana` é ainda mais plano: 1,29 a
1,50. Nenhum elemento tem efeito secundário no código. Portanto, hoje, **a escolha
de elemento não é uma decisão de jogo** — é uma escolha de cor e de forma de
projétil. É exatamente o que `gameplay/selftest.gd:_test_elements` verifica: ele
prova que os cinco *diferem*, nunca que a diferença *importa*.

### 1.3 TTK atual (tiros até derrubar / TTK de cadência)

Escudo aplicado como EHP linear, para poder comparar — **lembrando que o escudo
não existe em código** (ver §2.2).

| Elemento | sem escudo (100) | N1 (150) | N2 (175) | N3 (200) | N4 (225) |
|---|---|---|---|---|---|
| Fogo | 8t / **1,89s** | 12t / 2,97s | 14t / 3,51s | 16t / 4,05s | 18t / 4,59s |
| Água | 10t / **2,70s** | 14t / 3,90s | 16t / 4,50s | 19t / 5,40s | 21t / 6,00s |
| Raio | 7t / **2,04s** | 10t / 3,06s | 12t / 3,74s | 14t / 4,42s | 15t / 4,76s |
| Terra | 7t / **2,16s** | 10t / 3,24s | 11t / 3,60s | 13t / 4,32s | 15t / 5,04s |
| Vento | 12t / **2,64s** | 17t / 3,84s | 20t / 4,56s | 23t / 5,28s | 25t / 5,76s |

### 1.4 Tempo de viagem e TTK a 12m

| Elemento | 8m | 12m | 20m | 30m | **TTK a 12m (100)** | **TTK a 12m (N1=150)** |
|---|---|---|---|---|---|---|
| Fogo | 0,33 | 0,50 | 0,83 | 1,25 | **2,39s** | 3,47s |
| Água | 0,40 | 0,60 | 1,00 | 1,50 | **3,30s** | 4,50s |
| Raio | 0,24 | 0,35 | 0,59 | 0,88 | **2,39s** | 3,41s |
| Terra | 0,44 | 0,67 | 1,11 | 1,67 | **2,83s** | 3,91s |
| Vento | 0,31 | 0,46 | 0,77 | 1,15 | **3,10s** | 4,30s |

### 1.5 O que mais existe hoje (medido, não estimado)

| Sistema | Onde | Números reais |
|---|---|---|
| Dano de terreno queimando | `Balance.TERRAIN.burn_dps` | 6,0/s enquanto pisar; a célula queima 8,0s |
| Dano de terreno eletrificado | `Balance.TERRAIN.electrify_dps` | 10,0/s; a célula fica 3,0s → **30 de dano total** |
| Lama | `Balance.TERRAIN.mud_slow` | fator 0,55 de velocidade por 6,0s. **Não causa dano.** |
| Muro de pedra | `Balance.TERRAIN.wall_hp` | 60,0 de vida, 12,0s de duração |
| Dano no muro | `TerrainSystem._element_dmg` | **o mesmo dano do projétil** — terra (16) racha mais rápido que raio (15) |
| i-frames de esquiva | `Balance.DODGE.iframes` | 0,12s de imunidade total; `Combat.deal` barra o dano |
| Empurrão no acerto | `Projectile.KNOCKBACK` | **2,2 m/s, igual para os 5 elementos** — constante local, fora do `Balance` |
| Cadência dos bots | `Bot.FIRE_RATE_MULT` | 4,0× a cadência do player. Bot de fogo = 12,0 DPS bruto |
| Erro de mira do bot | `Bot.AIM_SPREAD` | ±0,12 rad |

### 1.6 Feedback de dano que já existe

| Peça | Arquivo | O que faz |
|---|---|---|
| Número flutuante | `Projectile._damage_number` | Label3D dourado fixo `(1.0, 0.82, 0.25)`, 0,6s, sobe 1,0m |
| Hitmarker | `ui/Hud.gd:_on_damage` | 4 tracinhos na mira por 0,16s |
| Flash de dano recebido | `juice/MatchJuice._on_health` | vinheta a 0,45 de alfa, esvai em 0,15s |
| Screen shake | `juice/MatchJuice` | só acima de 15,0 de queda de vida num evento |
| Som de acerto | `audio/Sfx._on_damage_dealt` | um som `hit` único, **igual para todo dano** |
| Indicador direcional | — | **não existe** |

---

## 2. DIAGNÓSTICO — onde o modelo está furado

### 2.1 O elemento é cosmético (gravidade: ALTA)

Medido em §1.2: 16% de spread no DPS sustentado, 16% no dano por mana, e **zero
efeito secundário implementado**. `Projectile._on_hit` faz exatamente três coisas
para todos os cinco elementos: `Combat.deal`, `apply_knockback(dir * 2.2)` e
`Bus.terrain_hit`. Não há queimadura, lentidão, condução, dispersão ou empurrão
diferenciado.

O pedra-papel-tesoura do GDD §4.6 **existe só no terreno** (`TerrainSystem`),
nunca no alvo. Água num inimigo em chamas não faz nada. Raio num inimigo molhado
não faz nada. Vento numa névoa do Vex dispersa a névoa (§14 está implementado),
mas vento num alvo empurra tanto quanto a pedra.

**Consequência:** o carrossel de elementos é um seletor de skin. O pilar de
inovação (§9, Sintonia) está sendo construído sobre elementos que ainda não têm
identidade mecânica para combinar.

### 2.2 O Escudo de Magia Evolutivo não existe (gravidade: ALTA)

`Balance.gd` não tem entrada de escudo. `Pawn.gd` não tem campo de escudo.
`Combat.deal` vai direto no `hp`. A única menção a escudo em todo o `godot/` está
em textos de UI (`menu/Elenco.gd`, descrevendo os kits do Corvomante, da Tessa, do
Vitalis e do Brok — kits que dependem de um sistema que não existe).

Isso é grave por três motivos encadeados:

1. **A régua de TTK do GDD §5 mede contra um alvo que o jogo não produz.** Todos
   os números com escudo em §1.3 são projeção, não medição.
2. **Quatro kits do elenco ficam sem chão.** A Suprema do Corvomante ("50 de dano
   em escudo, derruba 1 nível") e a passiva da Tessa ("regenera escudo; quando o
   escudo quebra, +15% de velocidade") não têm o que tocar. A Suprema da Tessa e a
   Suprema do Brok também não.
3. **A progressão da partida some.** O escudo evolutivo é o único sistema de
   progressão *dentro* da partida no design atual. Sem ele, o mago do minuto 3 é
   idêntico ao do minuto 0 — e um battle royale sem curva interna vira deathmatch.

### 2.3 O dano de terreno é invisível e ruidoso (gravidade: ALTA — é defeito, não desbalanço)

`Pawn.move_velocity` aplica `Combat.deal(self, dps_at(pos) * delta, "terrain")` a
cada passo de física. A 60fps, `6,0 × 0,0167 = 0,1` de dano por chamada. E
`Combat.deal` emite `Bus.damage_dealt.emit(target, int(round(amount)), element)`.

`round(0,1) = 0`. Três efeitos, todos por leitura de código:

| Efeito | Onde | Resultado |
|---|---|---|
| Número de dano | `Bus.damage_dealt` carrega `0` | quem observa o sinal recebe **zero**, 60 vezes por segundo |
| Som | `Sfx._on_damage_dealt` toca `hit` sem throttle | **~60 sons de acerto por segundo** enquanto o mago está no fogo; o pool round-robin corta cada um no meio — vira um zumbido |
| Vinheta + shake | `MatchJuice._on_health` | `health_changed` sai a cada frame; o tween da vinheta é **morto e recriado por frame** enquanto queima |

O jogador leva 6 dps (fogo) ou 10 dps (eletrificado) **sem número, sem cor e sem
direção** — exatamente a queixa do Diretor ("o jogo não comunica"). E o pilar §14
é o lugar onde ela mais dói, porque o terreno é onde o dano deveria assustar.

### 2.4 O `Bus.damage_dealt` não carrega quem atirou (gravidade: ALTA — é o gargalo)

`signal damage_dealt(target: Node, amount: int, element: String)`.

Sem a **fonte** do dano, ficam impossíveis, todos de uma vez:

- indicador direcional de quem te acertou (não há de onde tirar o ângulo);
- hitmarker correto — o `Hud.gd` usa hoje a heurística "alvo não é o player, logo
  fui eu", marcada como `ponytail:` no próprio arquivo, com a ressalva de que
  quebra quando bot atacar bot;
- crédito de **dano causado** para a evolução do escudo (o §5 exige saber quem
  causou e exige excluir dano em aliado);
- kill feed com autor;
- som por tipo de acerto (escudo × vida × DoT).

E sem uma flag de **acerto em escudo**, o feedback central do §5 ("quebrei o
escudo dele") não tem como existir.

### 2.5 Contradições diretas entre GDD e código

| Item | GDD | Código | Veredito |
|---|---|---|---|
| Vida do muro de pedra | §14: "~200hp" | `TERRAIN.wall_hp = 60,0` | 60 é jogável (4 a 5 varinhadas). **200 é o número errado, e está no GDD** — foi escrito antes da grade de 3m existir. Um muro de 200 contra dano 13 leva 16 tiros: a cobertura vira invencível. Recomendação: corrigir o GDD. |
| "Raio racha o muro mais rápido" | §14 | `_element_dmg` devolve o dano do projétil: terra 16 > raio 15 | contradição real. Exige multiplicador antiestrutura por elemento. |
| Vida base | §5: "Vida base: 100" | `PLAYER.hp = 100,0` | bate |
| Pip: "55 de vida base" | `personagens/20-pip.md:50` | `Pawn.hp` é fixo em `Balance.PLAYER.hp` | vida por mago não existe. Precisa entrar como multiplicador de ficha, não como número solto. |

### 2.6 A manopla, como está codificada, derruba em 0,65s (gravidade: ALTA)

`godot/gameplay/Arma.gd` (raia de loot, ainda não commitado) define a manopla com
`dmg 1.25`, `fire_rate 0.45` e `mana_cost 0.95`. Aplicado sobre o `Balance` de
hoje, isso produz:

| Elemento | dano | cadência | **tiros p/ 100** | **TTK de cadência** | TTK a 12m | DPS sust. |
|---|---|---|---|---|---|---|
| Fogo | 16,2 | 0,122s | 7 | **0,73s** | 1,16s | 26,6 |
| Água | 13,8 | 0,135s | 8 | **0,95s** | 1,47s | 25,3 |
| Raio | 18,8 | 0,153s | 6 | **0,77s** | 1,07s | 27,6 |
| Terra | 20,0 | 0,162s | 5 | **0,65s** | 1,23s | 26,8 |
| Vento | 11,2 | 0,108s | 9 | **0,86s** | 1,27s | 23,7 |

**Cinco tiros e 0,65 segundo para derrubar um alvo de vida cheia.** Isso é menos
de metade do piso de qualquer battle royale do gênero e menos de um terço da faixa
do próprio GDD §5. Nesse tempo o alvo não consegue nem começar uma esquiva (a
janela de reação humana com entrada de toque é ~0,25s, e a esquiva dura 0,18s).

**E o limitador declarado não morde.** O comentário no próprio `Arma.gd` diz "arma
de RAJADA, não de sustentação" e aponta o dreno de mana como o freio. Medido: o
**DPS sustentado da manopla é 23,7 a 27,6 — MAIOR que o da varinha** (18,0 a
20,2 hoje). Ela é a melhor arma em burst **e** em sustentação. O freio da mana só
apareceria depois de dois abates, porque um abate custa ~60 de mana de uma barra
de 100.

**A correção não é no dano — é nos dois multiplicadores que o comentário do
arquivo já identificou como os botões certos**, só que com valores que não fecham
a conta:

| Chave | Hoje | Proposto | Efeito medido |
|---|---|---|---|
| `ARMAS.manopla.fire_rate` | 0,45 | **0,80** | TTK de cadência sobe de 0,65–0,95s para **1,40–1,84s** — dentro do piso de §3.7 |
| `ARMAS.manopla.mana_cost` | 0,95 | **1,45** | DPS sustentado cai de 23,7–27,6 para **17,0–20,2** — abaixo da varinha, que é o desenho declarado |

> **A fantasia não se perde.** O "estalar de dedos" do §16.2 é vendido pela
> **ausência de tempo de preparo** (a magia sai no instante do toque, sem
> canalização, com a assinatura sonora do estalo) — não pelo intervalo *entre*
> dois estalos. `fire_rate 0,80` mantém a manopla como a arma mais rápida do jogo
> e continua sendo a única com cast sem preparo; só para de ser uma metralhadora.

### 2.7 Furos menores, mas reais

- **Empurrão é constante para os 5** (`Projectile.KNOCKBACK = 2.2`, fora do
  `Balance`). O vento, cuja identidade inteira é empurrar (§4.6), empurra igual à
  pedra.
- **Não há teto de DoT somado.** Hoje só existe terreno, então não morde; com
  queimadura + névoa do Vex + terreno eletrificado, o jogador cai de três fontes
  invisíveis somadas.
- **A regra anti-farm do §5 é incompleta.** Ela exclui dano em aliado; não diz
  nada sobre dano em muro, torreta, totem, corvo, casulo, bigorna ou frasco —
  todos alvos com vida no elenco. Um jogador sobe o escudo martelando o próprio
  muro.
- **`mana_regen = 14,0` com custos de 7 a 11 por tiro** dá saldo negativo em todos
  os elementos: toda luta é limitada por mana e o **DPS sustentado** (§1.2) é o
  número que manda. Isso é *bom design* — mas não está escrito em lugar nenhum,
  então ninguém está calibrando o número que importa.
- **Não há bônus nem penalidade situacional** (nem zona de acerto, nem queda de
  dano por distância). O alcance do projétil é o único limitador. Isso também é
  bom, e o §3.6 formaliza que deve continuar assim.

---

## 3. O MODELO PROPOSTO

### 3.0 A regra de arquitetura

> **Personagem = habilidades. Arma arcana = chassi de dano. Elemento =
> modificador + efeito secundário. Terreno = perigo persistente.**
> Nenhuma camada escreve na outra. É a mesma fronteira que já segura o
> `TerrainSystem`: quem muda o mundo nunca aplica dano.

Consequência prática: hoje o `Balance` tem **cinco tabelas soltas** com 25 números
sem relação entre si. O modelo proposto tem **um chassi** (3 armas × 5 campos = 15
números) **+ uma matriz** (5 elementos × 8 fatores adimensionais). Rebalancear o
jogo inteiro vira mexer no chassi da varinha; mudar a identidade de um elemento
vira mexer numa linha da matriz.

### 3.1 Chassi: as armas arcanas (§16.2)

> **Reconciliação com a raia de loot.** `godot/gameplay/Arma.gd` **já existe** e
> já resolve esta camada, com a fatoração **inversa** à que este estudo tinha
> rascunhado: lá o **elemento** carrega os números absolutos (`Balance.FIRE`..
> `WIND`) e a **arma** é um multiplicador de tier. A fatoração do `Arma.gd` é a
> adotada — ela mantém `Balance` como dono único dos números (regra do projeto) e
> já está codificada e testável headless. Este estudo **não pede que ela mude**;
> ele retuna os cinco perfis de elemento (§7.2), acrescenta os campos que faltam
> (§7.1) e corrige dois multiplicadores da manopla (§2.6).

| Arma | Papel | mult. dano | mult. cadência | mult. vel. | mult. alcance | mult. mana | runas |
|---|---|---|---|---|---|---|---|
| **Varinha** | SMG — todo mundo dropa com uma | 1,00 | 1,00 | 1,00 | 1,00 | 1,00 | 1 |
| **Cajado** | rifle/sniper — médio/raro | 1,55 | 1,45 | 1,25 | 1,70 | 1,30 | 2 |
| **Manopla** | lendária — só em Baú Celestial | 1,25 | **0,80** ⚠ | 1,15 | 1,15 | **1,45** ⚠ | 3 |

*(⚠ = os dois valores que este estudo pede para corrigir; hoje estão em 0,45 e
0,95. Ver §2.6. O resto da tabela é o `Arma.gd` como já está.)*

**A varinha é a régua.** Ela é 1,00 em tudo, então "todo mundo cai do céu com uma"
significa, em código, que sem arma equipada o jogo se comporta exatamente como
antes do sistema existir. Todo balanceamento se mede em "quantas varinhadas".

**O limitador da manopla tem de ser a mana, não o dano** — é isso que a torna
balanceável. Com os multiplicadores corrigidos: DPS bruto de 51 a 66 (o maior do
jogo) mas **DPS sustentado de 17,0 a 20,2 — o MENOR do jogo**, abaixo dos 19,7 a
23,5 da varinha. Ela ganha o duelo de 2s e perde o de 8s. É a formulação mecânica
do "poder alto, flexibilidade menor" do §16.2, e casa com os dois elementos fixos
e o brilho que denuncia o portador. **Com os multiplicadores de hoje isso não
acontece: a manopla é a melhor arma em burst E em sustentação** (§2.6).

O cajado troca precisão exigida por **alcance de 48 a 65m e velocidade 1,25×**: a
30m o projétil de fogo dele chega em 0,94s contra 1,15s da varinha. Ele tem ~7%
mais DPS que a varinha, o que é o prêmio honesto de um drop raro — mas erra caro,
porque a cadência de 0,38s a 0,51s não perdoa. Canalizar supremas com bônus
(`suprema_bonus`, já em `Arma.gd`) é território de kit, fora deste modelo.

### 3.2 Matriz de elementos (multiplicadores sobre o chassi)

| Elemento | dano | cadência | vel. proj. | mana | **em escudo** | **em vida** |
|---|---|---|---|---|---|---|
| **Fogo** | 1,00 | 1,00 | 1,00 | 1,00 | 1,00 | 1,00 |
| **Água** | 0,85 | 1,08 | 0,78 | 0,92 | 1,00 | 1,00 |
| **Raio** | 1,12 | 1,24 | 1,35 | 1,18 | **1,25** | 1,00 |
| **Terra** | 1,24 | 1,36 | 0,70 | 1,24 | 1,00 | **1,15** |
| **Vento** | 0,68 | 0,90 | 1,02 | 0,82 | **0,75** | 1,00 |

*(cadência e mana: >1 = mais lento/mais caro. Fogo é a linha neutra de propósito —
é a referência mental do jogador.)*

**A leitura de design, uma frase por elemento:**

| Elemento | O que ele É |
|---|---|
| **Fogo** | a régua. Dano honesto, cadência honesta, e a única fonte de dano ao longo do tempo do modelo base. |
| **Água** | o **setup**. O menor dano direto do jogo com escudo, mas é quem MOLHA — e molhado é o que faz o raio dobrar de valor. Água sozinha perde; água **antes** do raio ganha. |
| **Raio** | o **abre-escudo**. 1,25× em escudo e 1,00× em vida: derruba a barra azul e depois vira mediano. Projétil mais rápido do jogo (§4.1: quase-hitscan estilizado, **nunca** hitscan). |
| **Terra** | o **finalizador e o antiestrutura**. 1,15× em vida, maior dano por tiro, projétil mais lento — difícil de acertar em quem se move, brutal em quem está preso, e é ele que derruba cobertura. |
| **Vento** | o **controle**. Menor dano e menor efeito em escudo do jogo, compensados com empurrão dobrado, dispersão de área e a maior cadência. Vento não mata: vento tira o inimigo do lugar. |

**Dano em escudo × dano em vida (§5).** Regra base: **o escudo absorve primeiro,
1:1, e o excedente do MESMO tiro transborda para a vida.** Sem transbordo, o
último tiro contra um escudo de 3 pontos desperdiça 10 de dano e o TTK ganha um
degrau aleatório. As três exceções da matriz (raio +25% em escudo, terra +15% em
vida, vento −25% em escudo) são o que dá aos elementos papéis diferentes *dentro
do mesmo duelo*, e são a tradução mecânica de "magia contra magia": o escudo é
mágico, a pedra é física, e o vento empurra em vez de quebrar.

### 3.3 Efeitos secundários — o que cada elemento faz ALÉM de dano

| Elemento | Efeito | Números | Limitador |
|---|---|---|---|
| **Fogo** | **Queimadura** | 4 dps por 3s (12 no total) | **não acumula**, só refresca a duração; água remove; ignora escudo (§3.5) |
| **Água** | **Molhado** | 5s; −10% de velocidade; apaga queimadura | não causa dano extra nenhum; é habilitador puro |
| **Raio** | **Condução** | em alvo MOLHADO: **+50% no impacto** e atordoa 0,4s | só em alvo molhado; **teto de atordoamento do kernel: 0,8s** (`personagens/17-sylva.md`) |
| **Terra** | **Antiestrutura** | **2,0× de dano** em muro, torreta, totem, casulo, bigorna, frasco | zero efeito extra contra magos; é o counter de cobertura, não de gente |
| **Vento** | **Empurrão e dispersão** | knockback **2,0×** (4,4 m/s); dispersa névoa/gás/vapor no raio de impacto | não empurra quem está no ar (senão vira controle infinito); não interrompe conjuração |

### 3.4 As reações do §4.6 e do §16.4, traduzidas em números

Tabela de reação **no alvo**. A do terreno já existe e continua valendo (§14).

| Estado no alvo | Elemento que chega | Reação | Números |
|---|---|---|---|
| MOLHADO | Raio | **Condução** | +50% no impacto, atordoa 0,4s. Arco para outro alvo molhado a <4m: 50% do dano |
| MOLHADO | Água/gelo | **Hipotermia** (§16.4) | lentidão dobrada: −20% por 5s em vez de −10% |
| MOLHADO | Fogo | **Vapor** | o fogo **não aplica queimadura**; remove MOLHADO; nuvem de 2s que ofusca levemente os dois lados |
| QUEIMANDO | Água | **Extinção** | remove queimadura, aplica MOLHADO, zero dano extra — a contra-jogada é de graça de propósito |
| QUEIMANDO | Vento | **Atiçar** (triângulo do fogo, §16.4) | queimadura sobe de 4 para **7 dps** e ganha **+2s**. Dois gumes: o vento do inimigo te ajuda a queimar |
| Em NÉVOA (Vex) | Vento | dispersa | já é lei §14 |
| Em NÉVOA (Vex) | Fogo | incendeia e consome em 2s | já é lei §14 |
| Em ÁREA DE LAVA | Água | **Explosão de vapor** (§16.4) | empurra e escalda **os dois lados**: 15 de dano em raio de 3m, knockback 5,0 m/s |
| Em AREIA | Raio | **Fulgurito** (§16.4) | cobertura de vidro frágil: 15 de vida, bloqueia visão, 1 tiro destrói |

**A regra que impede isso de virar sopa:** todo estado do alvo é **exclusivo por
categoria** — um alvo tem no máximo UM estado térmico (queimando **ou** molhado,
nunca os dois) e UM estado de movimento (lentidão **ou** empurrão). A reação
sempre **substitui**, nunca soma. É a regra 2 do "chemistry engine" do BotW citada
no §16.1 ("elementos mudam o estado de outros elementos"), e é o que mantém o
modelo previsível. Previsível é aprendível.

### 3.5 Dano ao longo do tempo (DoT)

| Fonte | dps | duração | total | acumula? | ignora escudo? |
|---|---|---|---|---|---|
| Queimadura (fogo direto) | 4,0 | 3,0s | 12 | não, refresca | **sim** |
| Queimadura atiçada (vento) | 7,0 | 5,0s | 35 | não, refresca | **sim** |
| Terreno queimando | 6,0 | enquanto pisar | — | n/a | **sim** |
| Terreno eletrificado | 8,0 | enquanto pisar | — | n/a | **sim** |
| Névoa do Vex | 3,0 | enquanto dentro | — | n/a | **sim** |
| Fio da Tessa | 8,0 no toque | instantâneo | 8 | 1 por fio, 0,5s de imunidade por fio | não |

**Duas leis de DoT, ambas necessárias:**

1. **DoT ignora escudo e vai direto na vida.** Justificativa: o escudo é proteção
   *contra magia*, não contra estar em chamas. É o que dá dentes ao pilar §14 —
   sem isso, um mago com escudo N4 atravessa a floresta em chamas sem pensar e o
   terreno reativo volta a ser cenário, que é o problema que a nota de medição de
   19/08 já resolveu uma vez. **É a proposta mais agressiva deste documento;
   entra como KNOB `DOT.ignora_escudo` para poder ser desligada num playtest sem
   tocar em código.**
2. **Teto de DoT somado: 12 dps.** Nenhum alvo perde mais de 12 de vida por
   segundo por fontes de DoT somadas, não importa quantas o atinjam. Sem o teto,
   queimadura + terreno + névoa = 19 dps e o jogador cai de fontes que não
   consegue ler. Com o teto, o pior caso de DoT puro derruba um alvo de 100 em
   8,3s — tempo de sobra para sair da área. **O teto é o que torna o dano
   ambiental legível, e legibilidade é o pedido do Diretor.**

### 3.6 Multiplicadores situacionais — a decisão

**Decisão: o modelo de dano global NÃO tem nenhum multiplicador situacional.** Sem
zona de acerto, sem bônus pelas costas, sem bônus de queima-roupa, sem queda de
dano por distância.

| Candidato | Veredito | Justificativa |
|---|---|---|
| Acerto na cabeça | **NÃO** | (a) 10+ — dano localizado empurra a leitura para anatomia, e o modelo diz "derrubado". (b) A razão de peso: **entrada de toque + projétil com tempo de viagem**. Multiplicador de cabeça premia precisão fina, que é justamente o que o joystick virtual não entrega; o resultado é variância de TTK, não teto de habilidade. O Apex tem 2,0× na cabeça, mas é mouse/controle com armas quase-hitscan. (c) Bônus: zero trabalho de hitbox por parte do corpo em 6 bots + player no orçamento mobile. |
| Pelas costas | **NÃO** | Premia quem já tem a vantagem (a surpresa), dobrando o *snowball*. Exige checagem de orientação por acerto. O gênero de referência não tem. |
| Queima-roupa | **NÃO** | O bônus **já existe implicitamente**: a 2m o tempo de viagem é ~0,08s e o alvo não tem como esquivar — o DPS efetivo já é o máximo possível. Somar multiplicador em cima torna "grudar no inimigo" a jogada estritamente correta e mata a regra §4.1 (prever movimento). |
| Queda de dano por distância | **NÃO** | O `range` do projétil já é o limitador honesto e legível: passou do alcance, o projétil **some**. Queda de dano é o mesmo limitador, só que invisível. |
| **Onde multiplicador situacional PODE viver** | **KIT** | A Umbra já tem "+50% no primeiro golpe saindo do véu" (`personagens/12-umbra.md`); o Basalto tem "+60% de redução de dano" na Suprema; a Sylva transfere 20% do dano do aliado. **Regra: multiplicador situacional é território de habilidade de personagem, com telegrafia própria — nunca do modelo global.** É o que preserva os limitadores do GDD como parte do kit em vez de virarem regra silenciosa do jogo inteiro. |

### 3.7 TTK ALVO — qual deve ser e por quê

| Contexto | TTK de cadência | TTK a 12m |
|---|---|---|
| **Varinha × alvo sem escudo (100)** | **2,1 – 3,0s** | 2,7 – 3,5s |
| **Varinha × alvo com escudo N1 (EHP 150)** | 3,1 – 4,5s | **3,8 – 5,0s** |
| Manopla × sem escudo (o teto do jogo) | 1,4 – 2,1s | 1,8 – 2,6s |
| Cajado × sem escudo, 100% de acerto | 1,8 – 3,8s | 2,2 – 4,2s |

**Por que essa faixa.** O piso absoluto do jogo é **1,4s** (manopla + terra — e a
manopla é o drop mais raro do mapa); o teto é **~4s** com a varinha de vento
contra escudo. Comparação com o gênero: Apex fica em **2 a 4s com escudo**;
Fortnite, mais alto; Call of Duty Mobile, perto de 1s — e é exatamente onde o
combate vira "quem atira primeiro". A régua do Diretor é mobile: precisa ser um
pouco mais rápido que o Apex para caber numa sessão de metrô, e não pode cair
abaixo de ~2s com a arma comum, senão a esquiva de 0,18s com 2,6s de recarga
(`Balance.DODGE`) deixa de ser uma decisão — não sobra tempo para usá-la.

**A conta que amarra tudo:** com a varinha de fogo, derrubar um alvo com escudo N1
leva 3,84s de cadência, e nesse tempo cabem **2 esquivas completas** (cooldown
2,6s) e **~1,5 reposicionamentos**. É esse número — não o TTK sozinho — que diz se
há jogo dentro do duelo.

**Custo de mana de um abate:** varinha de fogo contra escudo N1 = 14 tiros × 7,5 =
105 de mana, com ~54 regenerados durante os 3,84s. Saldo: **~51 de mana, metade da
barra**. Regra de bolso publicável: **um abate através de escudo N1 custa metade da
barra de mana.** É esse número que impede o *spam* sem precisar de mais nenhuma
trava.

### 3.8 TTK resultante do modelo proposto (calculado, com transbordo de escudo)

Os 15 pares arma × elemento, com os multiplicadores de `Arma.gd` (manopla já
corrigida, §2.6) sobre os perfis de elemento retunados (§7.2). Regen de mana
proposto: **16,0/s** (era 14,0). O escudo é calculado com absorção 1:1 **com
transbordo** e com os multiplicadores `esc`/`vida` do §3.2.

**VARINHA** — mult. 1,00 em tudo (a régua)

| Elemento | dano | cad. | vel. | alc. | mana | DPS | DPS sust. | TTK cad. 100 | TTK 12m 100 | TTK 12m N1 |
|---|---|---|---|---|---|---|---|---|---|---|
| Fogo | 11,0 | 0,26 | 26 | 34 | 7,5 | 42,3 | **23,5** | 10t 2,34s | **2,80s** | 14t 3,84s |
| Água | 9,3 | 0,28 | 20 | 32 | 6,9 | 33,2 | **21,6** | 11t 2,80s | **3,40s** | 17t 5,08s |
| Raio | 12,3 | 0,32 | 35 | 38 | 8,8 | 38,4 | **22,4** | 9t 2,56s | **2,90s** | 12t 3,86s |
| Terra | 13,6 | 0,35 | 18 | 28 | 9,3 | 38,9 | **23,4** | 7t 2,10s | **2,77s** | 11t 4,17s |
| Vento | 7,5 | 0,23 | 27 | 30 | 6,1 | 32,6 | **19,7** | 14t 2,99s | **3,43s** | 23t 5,50s |

**CAJADO** — dano 1,55 · cad. 1,45 · vel. 1,25 · alc. 1,70 · mana 1,30

| Elemento | dano | cad. | vel. | alc. | mana | DPS | DPS sust. | TTK cad. 100 | TTK 12m 100 | TTK 12m N1 |
|---|---|---|---|---|---|---|---|---|---|---|
| Fogo | 17,1 | 0,38 | 32 | **58** | 9,8 | 45,2 | **28,0** | 6t 1,89s | **2,25s** | 9t 3,39s |
| Água | 14,4 | 0,41 | 25 | 54 | 9,0 | 35,5 | **25,7** | 7t 2,44s | **2,92s** | 11t 4,54s |
| Raio | 19,1 | 0,46 | 44 | **65** | 11,4 | 41,1 | **26,7** | 6t 2,32s | **2,59s** | 8t 3,52s |
| Terra | 21,1 | 0,51 | 22 | 48 | 12,1 | 41,5 | **27,9** | 5t 2,03s | **2,56s** | 7t 3,58s |
| Vento | 11,6 | 0,33 | 34 | 51 | 7,9 | 34,9 | **23,5** | 9t 2,67s | **3,02s** | 15t 5,02s |

**MANOPLA** — dano 1,25 · cad. **0,80** · vel. 1,15 · alc. 1,15 · mana **1,45**

| Elemento | dano | cad. | vel. | alc. | mana | DPS | DPS sust. | TTK cad. 100 | TTK 12m 100 | TTK 12m N1 |
|---|---|---|---|---|---|---|---|---|---|---|
| Fogo | 13,8 | 0,21 | 30 | 39 | 10,9 | **66,1** | 20,2 | 8t 1,46s | **1,86s** | 11t 2,48s |
| Água | 11,6 | 0,22 | 23 | 37 | 10,0 | 51,9 | 18,6 | 9t 1,79s | **2,31s** | 13t 3,21s |
| Raio | 15,4 | 0,26 | 40 | 44 | 12,8 | 60,1 | 19,3 | 7t 1,54s | **1,83s** | 10t 2,60s |
| Terra | 17,0 | 0,28 | 21 | 32 | 13,5 | 60,7 | 20,2 | 6t **1,40s** | **1,98s** | 9t 2,82s |
| Vento | 9,4 | 0,18 | 31 | 34 | 8,8 | 51,0 | **17,0** | 11t 1,84s | **2,23s** | 18t 3,51s |

**O que essas três tabelas provam:**

- **A hierarquia das armas fica coerente e legível em uma linha:** a varinha
  sustenta (19,7–23,5), o cajado alcança (48–65m com ~7% de DPS a mais) e a
  manopla explode (DPS bruto 51–66) **pagando com o menor DPS sustentado do jogo
  (17,0–20,2)**. É o limitador do §16.2 escrito em número, e é exatamente o que
  os multiplicadores de hoje **não** produzem (§2.6).
- **Todos os 15 pares caem dentro da faixa de §3.7.** Piso do jogo: 1,40s
  (manopla + terra, o drop mais raro com o elemento mais lento de acertar). Teto:
  3,43s (varinha de vento). Nenhum par fica abaixo de 1,4s nem acima de 3,5s
  contra vida base.
- **O DPS sustentado da varinha não muda a régua do jogo**: 19,7 a 23,5 contra os
  18,0 a 21,0 de hoje. **O modelo não inflaciona o jogo** — ele redistribui. Os
  efeitos secundários (§3.3) e os multiplicadores de escudo (§3.2) é que passam a
  somar por cima.
- **Vento contra escudo é o pior caso do modelo** (5,50s com a varinha, 23 tiros).
  É de propósito, e é o que obriga a troca de elemento: o vento tira o inimigo da
  cobertura; outro elemento derruba.

### 3.9 Escudo — os números

Do GDD §5, sem alteração:

| Nível | Cor | Escudo | Dano causado para evoluir (acumulado) |
|---|---|---|---|
| 1 | Branco | 50 | 0 |
| 2 | Azul | 75 | 150 |
| 3 | Roxo | 100 | 400 |
| 4 | Dourado | 125 | 900 |

Acréscimos deste estudo (são regras, não números novos):

- **Absorção 1:1 com transbordo no mesmo acerto** (§3.2).
- **Regeneração: o escudo NÃO regenera sozinho** — só por pergaminho, pela passiva
  da Tessa e pela Suprema do Brok. Sem isso, recuar 10s desfaz toda a luta e o TTK
  de §3.7 deixa de valer.
- **Anti-farm, versão completa (fecha o furo do §2.7):** dano que conta para
  evolução é **apenas dano em mago inimigo**. Não conta: aliado (já é lei §5), bot
  de treino, muro, torreta, totem, casulo, bigorna, frasco, corvo, e **dano
  próprio**. Regra geral herdada: *todo sistema que recompensa "dano causado"
  precisa listar o que NÃO conta, ou vira farm sem risco.*

### 3.10 Vida base por mago

`personagens/20-pip.md` já fixa 55 de vida para o Pip, o que exige um
multiplicador de vida por ficha. Proposta mínima, com teto explícito para não
virar porta de desbalanço:

| Faixa | Multiplicador | Quem | Contrapartida obrigatória |
|---|---|---|---|
| Frágil | 0,55 – 0,75 | Pip (0,55) | mobilidade ou informação acima da média |
| Padrão | 1,00 | 17 dos 20 | — |
| Robusto | 1,00 + redução | Basalto | a redução é **de habilidade** (Suprema Monólito, +60%), **nunca de vida base** |

**Regra: vida base varia para BAIXO; nunca para cima.** Vida acima de 100 empurra
todo o TTK do jogo e obriga a rebalancear os três chassis. Quem precisa ser
resistente ganha isso pela habilidade, com telegrafia — que é o §4.3 do GDD ("se
mata rápido, avisa antes") e o seu simétrico: se aguenta muito, mostra quando.

---

## 4. FEEDBACK DE DANO AO JOGADOR

A lei do GDD §10 é **cor + FORMA + SOM, nunca só cor** (acessibilidade e
daltonismo). Ela foi escrita para os projéteis; aqui ela é aplicada ao dano.

### 4.1 A matriz de feedback

| Evento | COR | FORMA | SOM |
|---|---|---|---|
| Acertei — em **escudo** | branco-azulado `#CFE6FF` | número com **contorno hexagonal** | "tink" agudo curto (vidro) |
| Acertei — em **vida** | **cor do elemento** (§10 / `Projectile.tint`) | número **sólido**, sem contorno | "thud" grave |
| Acertei — **DoT** | cor do elemento a 60% de alfa | número **pequeno em itálico**, acumulado num contador só | **sem som** (§4.3) |
| **Quebrei o escudo** | branco | **estilhaços** + número em 1,6× de tamanho | vidro quebrando (é o único som novo obrigatório) |
| **Derrubei** | dourado `#F0C75E` | Selo de Arkana pulsando | sting de abate (já existe em `Sfx`) |
| **Levei dano** | vinheta na **borda do lado de onde veio** | **arco direcional de 60°** na borda | impacto grave + camada do elemento |
| **Meu escudo quebrou** | branco piscando na barra | a barra de escudo **estilhaça** e some | vidro quebrando, mais alto |
| **Estou queimando / na névoa / eletrificado** | tarja fina na borda, cor do elemento | **ícone de estado** ao lado da barra de vida | loop baixo e contínuo, **um só**, nunca um som por tique |

### 4.2 Números flutuantes — a especificação

- Fonte **Chakra Petch** (GDD §10 manda para números de HUD).
- Tamanho: `base × (0,8 + 0,5 × dano / 25)`, teto **1,6×**. Um acerto de 30 fica
  visivelmente maior que um de 8 — a leitura é pré-consciente.
- Vida útil **0,6s**, subida **1,0m** (valores já usados em
  `Projectile._damage_number`; não mexer).
- **ACUMULAÇÃO — o item mais importante desta seção.** Acertos no mesmo alvo
  dentro de **0,35s** somam **no mesmo label** em vez de criar outro. Com cadência
  de 0,21s (manopla), sem acumulação nascem 5 números por segundo empilhados: é
  ilegível, e é literalmente o que o Diretor está vendo hoje.
- **A cor vem do elemento.** `Projectile.tint(el)` já existe e já devolve a paleta
  do §10; hoje o número é dourado fixo. Trocar o `modulate` por `tint(element)` é
  **uma linha** e resolve metade da queixa.

### 4.3 Anti-spam (é o que conserta o §2.3)

| Regra | Valor |
|---|---|
| Som de acerto: mínimo entre dois | **0,08s**. Abaixo disso, engole. |
| DoT: som de acerto | **nunca** — o estado tem um loop próprio, contínuo |
| DoT: número flutuante | **um a cada 0,5s**, com o total acumulado da janela |
| Vinheta de dano recebido | mínimo **0,12s** entre disparos; não recriar tween se já há um vivo |
| Screen shake | mantém o limiar atual de 15,0 num **evento único**; DoT **nunca** treme |

### 4.4 Indicador direcional

- Arco de **60°** na borda da tela, no ângulo do atacante **relativo ao yaw da
  câmera** (não ao corpo — o jogador lê a tela).
- Vida útil **1,4s**, alfa esvaindo.
- Cor **do elemento que te acertou** — é informação tática de graça: "laranja na
  esquerda" já diz que tem um mago de fogo ali.
- Máximo **3 arcos** simultâneos; o quarto substitui o mais antigo.
- **Dano de terreno não gera arco** (não tem direção) — gera a tarja de estado da
  §4.1.
- **Bloqueado por §2.4:** exige `source` no `Bus.damage_dealt`.

---

## 5. O QUE IMPLEMENTAR — mudanças de código recomendadas

> Nenhuma foi aplicada por este documento. Ordem de dependência em §8.

| # | Onde | Mudança | Destrava |
|---|---|---|---|
| **C1** | `core/Bus.gd` | `damage_dealt(target, amount: float, element, source: Node, on_shield: bool)`. `amount` vira **float** — é o `int(round())` que produz o zero do §2.3. | indicador direcional, hitmarker correto, crédito de escudo, som por tipo, kill feed com autor |
| **C2** | `gameplay/Combat.gd` | `deal(target, amount, element, source := null)`: escudo antes da vida com transbordo; aplica `esc`/`vida`; acumula dano causado no `source` para a evolução (com a lista de exclusão do §3.9); emite o sinal novo. **Continua sendo o ponto único de dano.** | escudo evolutivo inteiro |
| **C3** | `gameplay/Pawn.gd` | campos `shield`, `shield_level`, `dmg_dealt`, `status` (dicionário de estados) e o passo de DoT com teto (§3.5) | escudo, estados, DoT |
| **C4** | `gameplay/Pawn.gd` | acumular o dano de terreno num balde e chamar `Combat.deal` a cada **0,25s** em vez de a cada frame | mata os três defeitos do §2.3 de uma vez |
| **C5** | `gameplay/Projectile.gd` | ler os campos novos por elemento (`esc`/`vida`/`empurrao`/`estrutura`, §7.1) e aplicá-los em cima do que `Arma.perfil()` já devolve; `KNOCKBACK` sai da constante local e passa a `Balance.COMBATE.knockback × perfil.empurrao` | identidade de elemento |
| **C6** | `gameplay/Projectile.gd` | `_damage_number`: cor de `tint(element)`, tamanho por dano, **acumulação em 0,35s** | §4.2 — a metade barata da queixa do Diretor |
| **C7** | `ui/Hud.gd` | arcos direcionais, ícones de estado, barra de escudo (segmentada por nível, cores do §5) | §4.4 |
| **C8** | `audio/Sfx.gd` | throttle de 0,08s; sons separados `hit_shield` / `hit_flesh` / `shield_break`; DoT em loop, nunca por tique | §4.3 |
| **C9** | `juice/MatchJuice.gd` | guarda de 0,12s na vinheta e não recriar tween vivo | §4.3 |
| **C10** | `terrain/TerrainSystem.gd` | `_element_dmg` passa a usar o multiplicador `estrutura` (§3.3) em vez do dano cru do projétil | resolve a contradição do §2.5 (raio racha mais rápido) |
| **C11** | `gameplay/selftest.gd` | guardas novas: TTK dentro da faixa nos 15 pares arma×elemento; DPS sustentado dentro da banda; transbordo de escudo não perde dano; teto de DoT respeitado; nenhum `damage_dealt` com `amount == 0` | anti-vacuidade, roda sem aparelho |
| **C12** ⚠ | `gameplay/Arma.gd` | `ARMAS.manopla.fire_rate` 0,45 → **0,80** e `mana_cost` 0,95 → **1,45** (§2.6). **É a correção mais urgente do documento** — dois números, e é território da raia de loot. | tira a manopla do TTK sub-segundo e faz a mana virar o limitador que o próprio arquivo declara |
| **D1** | `docs/GDD.md` §14 | corrigir "muro ~200hp" para **60** (o código está certo; o GDD, não) | fim da contradição |
| **D2** | `docs/GDD.md` §5 | anexar a lista completa do anti-farm (§3.9) | fecha o furo |

---

## 6. O QUE **NÃO** IMPLEMENTAR (decisões negativas, para não voltarem)

- Zona de acerto / cabeça / costas / queima-roupa (§3.6).
- Queda de dano por distância (§3.6).
- Hitscan em qualquer elemento — o raio é o **mais rápido**, nunca instantâneo
  (GDD §4.1; `Balance.LIGHTNING` já carrega o comentário).
- Vida base **acima** de 100 para qualquer mago (§3.10).
- Acúmulo de queimadura em pilha (§3.3) — refresca, nunca soma.
- Atordoamento encadeado acima de **0,8s** (teto do kernel, `personagens/17-sylva.md`).
- Regeneração automática de escudo (§3.9).
- Chance de propagação de fogo por tique (§14 — já medido: 380/380 células).

---

## 7. TABELA DE KNOBs — pronta para aplicar (`Balance.gd` + 2 números em `Arma.gd`)

Formato do arquivo mantido: dicionários `const` com comentário de KNOB.

### 7.1 Entradas NOVAS

**A camada de arma NÃO entra aqui** — ela já existe em `gameplay/Arma.gd` como
multiplicador de tier, e a fatoração dele é a adotada (§3.1). O que muda em
`Arma.gd` são dois números (§2.6), listados em §7.2.

```gdscript
## CAMPOS NOVOS POR ELEMENTO. Entram DENTRO de cada dicionario existente
## (FIRE/WATER/LIGHTNING/EARTH/WIND), ao lado de dmg/mana_cost/fire_rate — sao
## adimensionais e a arma NAO os multiplica (o tier muda quanto, nunca o quê).
##   esc/vida  : dano em ESCUDO x dano em VIDA (GDD §5). E' o que da' papel
##               diferente a cada elemento DENTRO do mesmo duelo.
##   empurrao  : multiplica COMBATE.knockback (o vento empurra, nao quebra).
##   estrutura : multiplica o dano em muro/torreta/totem/casulo/bigorna.
##               Resolve a contradicao do GDD §14 ("raio racha mais rapido").
##
##   FIRE      += {"esc": 1.00, "vida": 1.00, "empurrao": 1.0, "estrutura": 1.0}
##   WATER     += {"esc": 1.00, "vida": 1.00, "empurrao": 1.0, "estrutura": 1.0}
##   LIGHTNING += {"esc": 1.25, "vida": 1.00, "empurrao": 1.0, "estrutura": 1.6}
##   EARTH     += {"esc": 1.00, "vida": 1.15, "empurrao": 1.2, "estrutura": 2.0}
##   WIND      += {"esc": 0.75, "vida": 1.00, "empurrao": 2.0, "estrutura": 0.6}

## ESCUDO DE MAGIA EVOLUTIVO (GDD §5 — numeros do GDD, sem alteracao).
## NAO regenera sozinho: so' pergaminho, passiva da Tessa e Suprema do Brok.
const ESCUDO := {
	"niveis":     [50.0, 75.0, 100.0, 125.0],
	"evoluir":    [0.0, 150.0, 400.0, 900.0],  # dano causado ACUMULADO
	"transbordo": true,   # excedente do MESMO tiro passa para a vida (lei)
	"regen": 0.0,         # KNOB — >0 desfaz o TTK alvo; playtest so'
}

## EFEITOS SECUNDARIOS POR ELEMENTO. Estado do alvo e' EXCLUSIVO por categoria
## (um termico, um de movimento): a reacao SUBSTITUI, nunca soma.
const STATUS := {
	"burn_dps": 4.0, "burn_dur": 3.0,                  # queimadura do fogo direto
	"burn_fanned_dps": 7.0, "burn_fanned_bonus": 2.0,  # aticada pelo vento (§16.4)
	"wet_dur": 5.0, "wet_slow": 0.90,                  # molhado: habilita a conducao
	"hypothermia_slow": 0.80,                          # molhado + gelo (§16.4)
	"conduct_mult": 1.50, "conduct_stun": 0.40,
	"conduct_arc_m": 4.0, "conduct_arc_mult": 0.50,
	"stun_cap": 0.80,                                  # TETO do kernel — NUNCA subir
}

## DANO AO LONGO DO TEMPO — as duas leis do estudo.
const DOT := {
	"teto_dps": 12.0,       # nenhum alvo perde mais que isso por segundo de DoT
	"ignora_escudo": true,  # KNOB: da' dentes ao pilar §14; desligar so' em playtest
	"tick": 0.25,           # s por aplicacao — MATA o dano de 0 a 60Hz
}

## FEEDBACK DE DANO. Cor+FORMA+SOM e' lei (GDD §10).
const FEEDBACK := {
	"num_merge_s": 0.35,    # acertos no mesmo alvo dentro disso SOMAM num label so'
	"num_life_s": 0.60,
	"num_scale_base": 0.80, "num_scale_gain": 0.50, "num_scale_max": 1.60,
	"hit_sound_min_s": 0.08,
	"dot_num_every_s": 0.50,
	"vignette_min_s": 0.12,
	"arc_dur_s": 1.40, "arc_deg": 60.0, "arc_max": 3,
}

## COMBATE — o que hoje esta' solto em constante de cena.
const COMBATE := {
	"knockback": 2.2,       # m/s base; multiplicado pelo campo `empurrao` do elemento
	"ref_range_m": 12.0,    # alcance de REFERENCIA de toda medicao de TTK
}
```

### 7.2 Entradas ALTERADAS

**Os cinco perfis de elemento em `Balance.gd`** — retune. A estrutura não muda:
mesmos dicionários, mesmas chaves, só valores. `Projectile.spec()`, `Arma.base()`
e o `selftest` continuam funcionando sem uma linha de reescrita.

| Elemento | dmg | fire_rate | mana_cost | projectile_speed | range |
|---|---|---|---|---|---|
| `FIRE` | 13,0 → **11,0** | 0,27 → **0,26** | 9,0 → **7,5** | 24,0 → **26,0** | 40,0 → **34,0** |
| `WATER` | 11,0 → **9,3** | 0,30 → **0,28** | 8,0 → **6,9** | 20,0 → **20,0** | 38,0 → **32,0** |
| `LIGHTNING` | 15,0 → **12,3** | 0,34 → **0,32** | 10,0 → **8,8** | 34,0 → **35,0** | 44,0 → **38,0** |
| `EARTH` | 16,0 → **13,6** | 0,36 → **0,35** | 11,0 → **9,3** | 18,0 → **18,0** | 34,0 → **28,0** |
| `WIND` | 9,0 → **7,5** | 0,24 → **0,23** | 7,0 → **6,1** | 26,0 → **27,0** | 36,0 → **30,0** |

> As três invariantes que o `selftest` já testa continuam valendo: terra é a mais
> lenta (18), raio é o mais rápido (35), vento tem a maior cadência (0,23). Os
> alcances caem porque a varinha é 1,00 e o cajado multiplica por 1,70 — com os
> alcances de hoje o cajado de raio chegaria a 75m, longe demais para a câmera.

**Demais chaves:**

| Chave | Hoje | Proposto | Efeito de AUMENTAR | Efeito de DIMINUIR |
|---|---|---|---|---|
| `PLAYER.mana_regen` | 14,0 | **16,0** | luta mais longa e mais agressiva; o DPS sustentado sobe e o TTK real cai | a luta vira gerenciamento de recurso; o combate esfria e o TTK real sobe |
| `TERRAIN.burn_dps` | 6,0 | **6,0** (manter) | o incêndio vira armadilha mortal e ninguém entra na floresta | o pilar §14 vira cenário |
| `TERRAIN.electrify_dps` | 10,0 | **8,0** | 3s de eletrificado passa de 24 para >30 de dano — a poça mata sozinha | a poça deixa de assustar e o combo água+raio perde a razão de existir |
| `TERRAIN.wall_hp` | 60,0 | **60,0** (manter; corrigir o GDD) | cobertura invencível: o combate trava | o muro vira efeito visual |

**Em `gameplay/Arma.gd`** — os dois números do §2.6. **É a alteração mais urgente
do documento inteiro.**

| Chave | Hoje | Proposto | Por quê |
|---|---|---|---|
| `ARMAS.manopla.fire_rate` | 0,45 | **0,80** | TTK de cadência sobe de 0,65–0,95s (sub-segundo) para 1,40–1,84s |
| `ARMAS.manopla.mana_cost` | 0,95 | **1,45** | DPS sustentado cai de 23,7–27,6 para 17,0–20,2 — **abaixo** da varinha, que é o desenho declarado no próprio comentário do arquivo |

### 7.3 Como ler cada KNOB do modelo novo

| KNOB | Aumentar faz | Diminuir faz |
|---|---|---|
| `FIRE.dmg` (a régua) | **move o jogo inteiro**: todo TTK cai proporcionalmente, nas 3 armas | todo TTK sobe; a mana deixa de ser o limitador e a cadência vira o teto |
| `FIRE.mana_cost` | o DPS sustentado cai e as lutas viram trocas curtas com recuo | o *spam* volta e o §4.2 (economia única) deixa de morder |
| `<ELEM>.dmg` | aquele elemento vira a escolha óbvia — o carrossel volta a não importar | aquele elemento só se justifica pelo efeito secundário (é o desenho da água e do vento) |
| `<ELEM>.esc` | esse elemento vira o abre-escudo do jogo | esse elemento fica inútil contra alvo escudado (é o desenho do vento, de propósito) |
| `Arma.ARMAS.manopla.fire_rate` | a manopla deixa de ser a arma mais rápida e perde a fantasia do estalo | **abaixo de 0,80 o TTK cai para sub-segundo** (§2.6) |
| `Arma.ARMAS.manopla.mana_cost` | a manopla vira só rajada — 1 abate por barra | ela volta a vencer também a luta longa, e nenhum limitador sobra |
| `ESCUDO.regen` | recuar 10s desfaz a luta e o TTK alvo deixa de valer | (0,0 é o valor de projeto) |
| `DOT.teto_dps` | o jogador cai de fontes que não consegue ler — a queixa do Diretor volta | o terreno e a queimadura viram enfeite |
| `DOT.tick` | menos precisão no dano ambiental, mais folga de CPU | volta a 60Hz: número zero, som em zumbido, tween por frame (§2.3) |
| `FEEDBACK.num_merge_s` | os números somem numa soma só e o jogador perde a cadência do acerto | escada ilegível de números (o estado de hoje) |
| `STATUS.stun_cap` | **NÃO SUBIR.** Acima de 0,8s o jogador perde o controle e o jogo fica injusto | atordoamento deixa de ser uma jogada |

---

## 8. PLANO DE IMPLEMENTAÇÃO EM FASES

### Fase 0 — o gargalo (destrava tudo o resto)

**C1 + C4.** Sinal do `Bus` com `source`, `on_shield` e `amount` em float; dano de
terreno acumulado a cada 0,25s.

- Sozinha, esta fase **conserta o §2.3 inteiro** (número zero, zumbido de 60Hz,
  tween por frame) e destrava as cinco coisas do §2.4.
- **Testável sem aparelho:** guarda no `selftest` de que nenhum `damage_dealt` sai
  com `amount == 0`.
- Diff estimado: ~15 linhas em 4 arquivos.

### Fase 1 — feedback barato (o que o Diretor vê primeiro)

**C6 + C8 + C9.** Cor do elemento no número, tamanho por dano, acumulação em
0,35s; throttle de som; guarda da vinheta.

- Nenhuma mudança de balanceamento: o jogo continua com os mesmos números e
  **parece** outro.
- **Testável sem aparelho** — o `selftest` de juice já existe e cobre o padrão.

### Fase 2 — o chassi

**C5 + C12 + as entradas 7.1/7.2.** Os cinco perfis de elemento são retunados,
os campos novos (`esc`/`vida`/`empurrao`/`estrutura`) entram nos mesmos
dicionários, `KNOCKBACK` sai da constante local, e os dois multiplicadores da
manopla são corrigidos em `Arma.gd` (§2.6).

- Depende de: nada. Pode ir em paralelo com a Fase 1.
- **Testável sem aparelho:** as tabelas de §3.8 viram guardas no `selftest` (TTK
  dentro da faixa nos 15 pares arma×elemento — é o teste que hoje não existe: o
  `_test_elements` prova que os cinco *diferem*, nunca que a diferença *importa*).

### Fase 3 — o escudo

**C2 + C3 (parte de escudo) + C7 (barra).** É a fase grande, e é onde §2.2 fecha.

- Depende de: **Fase 0** — o crédito de dano causado precisa do `source`.
- **Testável sem aparelho:** o transbordo não perde dano; a evolução não credita
  dano em aliado, em muro nem em si mesmo; a barra segmenta por nível.

### Fase 4 — estados e reações

**C3 (parte de status) + a tabela do §3.4 + C10.** Queimadura, molhado, condução,
antiestrutura, atiçar.

- Depende de: **Fase 2** (os campos novos por elemento precisam existir) e **Fase 0** (o DoT
  precisa do tique de 0,25s).
- **Testável sem aparelho:** exclusividade de estado por categoria; teto de DoT;
  água apaga queimadura; raio em molhado dá 1,5×; teto de atordoamento em 0,8s.

### Fase 5 — feedback caro

**C7 (arcos direcionais + ícones de estado).**

- Depende de: **Fase 0** (direção) e **Fase 4** (estados a mostrar).
- Precisa de aparelho: posição dos arcos na área segura e legibilidade em tela
  pequena são calibração de `Dp`, não de código.

### Fase 6 — armas de verdade no mapa

Loot de varinha/cajado/manopla, Baú Celestial, runas (§16.3).

- Depende de: **Fase 2**. É a fase que transforma o chassi de §3.1 de tabela em
  jogo, e a única que precisa da raia de loot.

### O que dá para testar sem device (resumo)

| Testável em `selftest` | Precisa de aparelho |
|---|---|
| TTK e DPS sustentado nos 15 pares (§3.8) | legibilidade dos números em tela pequena |
| transbordo de escudo (nenhum dano perdido) | posição dos arcos direcionais na área segura |
| teto de DoT e exclusividade de estado | se 2,8s de TTK *sente* certo com joystick virtual |
| nenhum `damage_dealt` com `amount == 0` | volume relativo dos sons de acerto |
| throttle de som e de vinheta (contagem de chamadas) | custo de CPU dos estados com 6 bots |
| anti-farm da evolução (dano em muro não credita) | alcance do cajado (58–65m) contra o alcance real da câmera |
| espelho `Balance` ↔ `spec()` continua íntegro | — |

---

## 9. Perguntas em aberto para o Diretor

0. **A manopla (§2.6) — é a única pergunta urgente.** Como está codificada em
   `Arma.gd`, ela derruba um alvo de vida cheia em **0,65 a 0,95 segundo** e tem o
   maior DPS sustentado do jogo. São dois números para mudar (`fire_rate` 0,45 →
   0,80 e `mana_cost` 0,95 → 1,45) e é território da raia de loot, não desta.
   Autoriza a correção antes do próximo playtest?
1. **`DOT.ignora_escudo`** — é a proposta mais agressiva do documento (§3.5). Ela
   dá dentes ao pilar §14; a alternativa é o escudo N4 andar na floresta em chamas
   sem pensar. Entra ligada, com KNOB?
2. **Faixa de TTK** — §3.7 propõe 2,1–3,0s de cadência com a varinha contra vida
   base, contra os 1,5–2,5s escritos no §5. O modelo atual **já está** em 1,89s
   (fogo) a 2,70s (água), então o §5 já não descreve o jogo. Ajusta-se a faixa do
   GDD, ou aperta-se o chassi da varinha para caber nela?
3. **Muro de pedra:** 60 (código) ou 200 (GDD §14)? Este estudo recomenda **60** e
   corrigir o GDD (D1).
4. **Vida por mago** (§3.10): o Pip já tem 55 na ficha. Entra o multiplicador de
   ficha, ou o Pip passa a ser frágil por mobilidade em vez de por vida?
