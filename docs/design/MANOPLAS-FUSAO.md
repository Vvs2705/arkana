# MANOPLAS — os ataques de FUSÃO (design, só papel)

> **Ordem do Diretor (26/08, verbatim):** *"As manoplas que vão aparecer no baú
> precisam ser repensadas... hoje ela apenas dispara um ataque de água e um
> ataque de raio juntos e não é esse o objetivo do jogo. precisa elaborar melhor
> a ideia dos ataques usados por essas luvas, como por exemplo a de fogo com
> vento que forma um furacão ou tufão de fogo ou uma chama mais potente, algo
> similar. pense melhor e redesenhe isso como vai ser desenvolvido no papel do
> jogo."*
>
> **O que este documento é:** o redesenho, no papel, do ataque da Manopla — um
> ataque **FUNDIDO** único por par de elementos, com identidade própria. Nenhuma
> linha de código muda por este documento; nenhum número entra em `Balance`.
> Fatos citam o arquivo de origem; o que é invenção nova está marcado
> **PROPOSTA**. Números de dano/cadência ficam para depois do playtest (GDD
> §19.5: números viram dados; o GDD trava números de combate).
>
> **O problema que ele corrige:** hoje a manopla alterna os 2 elementos tiro a
> tiro (`godot/gameplay/ArmaSlot.gd:100-110` — "alterna entre os 2 fixos, tiro a
> tiro"). Na cadência de ~0,2s isso lê como "dois ataques juntos": dois projéteis
> paralelos, nenhuma fusão. É exatamente o que o Diretor mandou repensar.

---

## 1. O PRINCÍPIO

**A Manopla é a ÚNICA arma do jogo que dispara FUSÃO.** Cada estalar de dedos
conjura **um único projétil/efeito com identidade própria do par** — não dois
tiros, não uma alternância: o Tufão de Brasas não é "fogo e depois vento", é uma
terceira coisa que só existe porque os dois moram na mesma mão. E isso é, de
propósito, **o gostinho solo do pilar do jogo**: a Sintonia (GDD §9) é o ritual
de DOIS magos — canalização visível, cooldown compartilhado de 24s, efeito em
área espetacular. A manopla entrega a MESMA identidade de cada par **na escala
de ARMA**: raio pequeno, sem canalização, sem cooldown — pagando em **mana**
(1,45× por tiro, `godot/gameplay/Arma.gd`), que já é o limitador desenhado da
manopla (`docs/DANO.md` §3.1: "ganha o duelo de 2s e perde o de 8s"). Quem acha
uma manopla no Baú Celestial experimenta sozinho, em miniatura, o que a dupla
faz em grande — e aprende a matriz de combos jogando. A manopla vende a
Sintonia; nunca a substitui.

**A regra de escala (PROPOSTA):** a fusão da manopla usa a MESMA identidade do
combo de Sintonia do par (`roblox/src/shared/Elements.luau` — os 10 nomes e
efeitos), com **~1/4 do raio, fração do efeito e nome próprio** ("irmão menor").
Mesma família visual, outra magnitude: quem viu um Dardo Galvânico reconhece a
Eletrocussão — e entende na hora por que vale coordenar com o parceiro.

---

## 2. A TABELA DOS 10 PARES

Regra visual (lei herdada do Spellbreak, `docs/referencias/SPELLBREAK.md` §2.1):
**combo = FORMA de um elemento + PALETA do outro** — o jogador lê o par à
distância sem legenda. Formas e paletas por elemento:
`roblox/src/shared/Elements.luau` (Fogo=cunha `#FF5A2A` · Água=esfera `#2AA7FF`
· Terra=bloco `#A8763E` · Vento=anel `#8FE8C9` · Raio=zigue-zague `#F5D90A`).
Efeitos no alvo usam **apenas** as reações já legisladas em `docs/DANO.md`
§3.3–3.5 (queimadura, molhado, condução, atiçar, vapor, extinção, tetos do
kernel) — nenhuma fusão contradiz uma reação existente.

Todos os nomes e comportamentos abaixo são **PROPOSTA**.

| Par | Nome arcano | O que o projétil É | Leitura visual (FORMA + PALETA) | Efeito no alvo (leis de DANO.md §3) | Counter / contra-jogada | Leitura no celular |
|---|---|---|---|---|---|---|
| Fogo+Vento | **Tufão de Brasas** | espiral de fogo que avança girando e PUXA de leve quem passa a ~2m do eixo | FORMA do vento (anel/hélice) + PALETA do fogo (laranja `#FF8A3D`) | dano + **queimadura** (4dps/3s); a tração é leve — desloca a mira, nunca prende | água extingue a queimadura de graça (§3.4); o projétil é o mais lento da lista — esquiva lateral resolve | **baixa** — espiral laranja não se confunde com nada |
| Fogo+Terra | **Lasca de Magma** | pedra incandescente em ARCO que, no impacto no chão, deixa **1 poça de lava pequena** (~1 célula) por poucos segundos | FORMA da terra (bloco facetado) + PALETA do fogo (`#FF4500`) | dano de impacto; a poça machuca por **terreno** (via TerrainSystem, nunca pela arma) | água na poça = **explosão de vapor que escalda OS DOIS lados** (§3.4 — counter com custo); não pisar na poça | **média** — o arco pede antecipação, a poça é óbvia |
| Fogo+Raio | **Faísca de Plasma** | o tiro reto mais rápido e de maior impacto da manopla; ESTOURA num ponto, sem área | FORMA do raio (zigue-zague) + PALETA do fogo esbranquiçada (`#FFD98A`) | maior dano por tiro do arsenal + queimadura curta; **nunca atordoa** (plasma é burst, não controle) | o mais caro em mana — 3–4 erros e a manopla "apaga" (DIRECAO.md §1: recarga); trajeto reto = strafe counter | **baixa** — é um tiro, o mais legível dos 10 |
| Fogo+Água | **Jato Escaldante** | globo d'água fervente; no impacto solta um **sopro de vapor** (~2s) que ofusca de perto os dois lados | FORMA da água (esfera) + PALETA do fogo (esfera laranja fervendo) | dano + vapor; seguindo §3.4, **não aplica nem queimadura nem molhado** — vapor é fogo apagado, não fogo somado (mesma lei da Cortina de Vapor, `roblox/src/server/Sintonia.luau`) | vento dissipa o vapor (§14); a névoa cega o atirador também — usar de longe é desperdício | **média** — névoa em tela de 6" é a mais cara das leituras; por isso o sopro é curto e pequeno |
| Água+Raio | **Dardo Galvânico** | gota crepitante que MOLHA e conduz **no mesmo golpe**: o próprio dardo detona a condução que ele preparou | FORMA da água (esfera) + PALETA do raio (âmbar `#F5D90A`) | aplica MOLHADO e cobra na hora: **+50% no impacto + atordoamento 0,4s** (§3.4, condução) — respeitando o teto de 0,8s do kernel: acertos seguidos **não** encadeiam atordoamento | Terra isola (§14); sair da água; o atordoamento não reencadeia — depois do primeiro, o alvo reage | **baixa** — gota amarela crepitando; o par que o baú de hoje já sorteia |
| Água+Terra | **Visgo de Lama** | glóbulo pesado e lento de lama que GRUDA: lentidão severa no alvo + **1 célula de lama** no ponto do impacto | FORMA da água (esfera) + PALETA da terra (marrom `#6B5433`) | **lentidão** (categoria movimento — substitui empurrão, nunca soma, §3.4); dano baixo: o visgo prepara, não mata | fogo seca a lama (§14); o alvo lento ainda conjura — trocar tiro com quem te grudou é a resposta | **baixa** — bola marrom, mancha marrom |
| Água+Vento | **Onda Torrencial** | leque curto de água empurrada por vento: EMPURRA, MOLHA e **apaga fogo** na linha (inclusive Muralha de Brasas e terreno queimando) | FORMA do vento (crescente/anel) + PALETA da água (`#7FD4FF`) | **molhado** + empurrão forte (2× o knockback, identidade do vento §3.3); o menor dano dos 10 — é a fusão-counter e a fusão-setup (molhado é o prato do raio aliado) | quem toma só perde posição, não vida; muro de Terra bloqueia o leque; alcance curto | **baixa** — onda azul que empurra: o corpo sente |
| Terra+Vento | **Rajada de Areia** | cone curto de areia que GRUDA no alvo: areia acesa = **revelado** (contorno ~1,8s, clamp de 2,0s do kernel) + ofusca de perto quem está no cone | FORMA do vento (anel/hélice) + PALETA da terra (ocre `#C9A96A`) | dano leve + revelação — a versão de bolso da Tempestade de Areia (`Sintonia.luau`: "a areia GRUDA no marcado") | alcance mais curto dos 10; **PROPOSTA nova de reação:** ficar MOLHADO lava a areia e limpa a marca (a confirmar — não está em DANO.md) | **média** — o contorno revelado já existe na linguagem do jogo (Corvus, Olho-de-Éter) |
| Terra+Raio | **Cristal Voltaico** | lasca de cristal: no ALVO, é dano direto; no CHÃO, finca **1 mina-cristal** (máx. 2 armadas) que estoura ao ser pisada — atordoa 0,4s | FORMA da terra (bloco facetado) + PALETA do raio (dourado `#D9C46A`) | acerto direto NUNCA atordoa (na cadência da manopla seria stun-lock — o teto de 0,8s do kernel é lei); só a mina atordoa, e mina é telegrafada | a mina brilha e zumbe (lei do GDD §10: cor+forma+som); **1 tiro destrói**; contornar | **média** — dois modos num ataque; a mina paga o custo com counterplay claro |
| Vento+Raio | **Centelha Caçadora** | farpa elétrica carregada pelo vento que **corrige a rota** de leve rumo ao alvo mais próximo da mira (homing fraco, só ângulo pequeno) | FORMA do raio (zigue-zague) + PALETA do vento (verde-menta `#CFE8F5`) | dano leve + empurrão leve; a irmã menor da Nuvem Tempestuosa ("persegue o alvo") — aqui quem persegue é o projétil, por 1 correção | quebrar linha de visão mata o homing; cobertura anula; o homing NUNCA vira aimbot — é conforto de mira, não mira automática | **baixa** — e é a fusão mais amiga do polegar: candidata a manopla "de entrada" |

**Terreno como palco (pilar §14):** toda fusão que faz sentido no chão CONVERSA
com ele — Tufão acende grama no rastro (dentro do orçamento de combustível,
nunca chance por tique), Lasca deixa lava, Visgo deixa lama, Onda apaga
incêndio e congela? não — apenas apaga e molha, Dardo eletrifica poça local,
Cristal finca no chão. A fusão nunca aplica o efeito de terreno "na mão": ela
**pede** ao TerrainSystem, como a Sintonia já faz (`Sintonia.luau`, fronteira de
responsabilidade).

---

## 3. AS 3 REGRAS DURAS

Toda fusão, presente e futura, respeita as três — são as leis que impedem a
manopla de quebrar o jogo:

1. **Dano num ponto só; mundo num ponto só.** A fusão nunca aplica dano por
   conta própria: todo dano entra por `Combat.deal` (Godot) / `applyDamage`
   (Roblox), e toda mudança de mundo entra pelo TerrainSystem — a mesma
   fronteira que já segura a Sintonia (`roblox/src/server/Sintonia.luau`:
   "quem aplica dano é o Combat e quem muda o mundo é o Terrain"). Sem isso, o
   escudo, o anti-farm e a telemetria ficam cegos para a arma mais forte do jogo.
2. **Os tetos do kernel são invioláveis.** Atordoamento clampado em **0,8s** e
   nunca encadeado (`docs/DANO.md` §3.4, `Balance.STATUS.stun_cap`); revelação
   clampada em 2,0s; DoT somado no teto de **12 dps**; estado do alvo exclusivo
   por categoria — a reação **substitui, nunca soma** (§3.4). Consequência
   prática: na cadência da manopla, nenhuma fusão pode atordoar em acerto
   direto (viraria stun-lock) — controle forte só em efeito telegrafado (mina,
   poça).
3. **Nenhuma fusão nega o counterplay dos elementos base — nem os limitadores
   da manopla.** Se água apaga fogo, toda fusão de fogo continua apagável; se
   Terra isola raio, toda fusão de raio continua isolável; vento continua
   dispersando névoa/vapor. E a fusão muda a FORMA do tiro, **nunca o chassi**:
   os multiplicadores da manopla (`Arma.gd`: dano 1,25 · cadência 0,80 · mana
   1,45), o brilho que denuncia o portador e o par fixo sem troca (GDD §16.2)
   ficam exatamente como estão. O limitador da manopla é a mana — a fusão não
   ganha desconto.

---

## 4. PRIORIDADE DE IMPLEMENTAÇÃO

O baú de hoje sorteia o par entre 3 (`godot/gameplay/Arma.gd`,
`PARES_MANOPLA`: fire+wind, water+lightning, earth+fire — sorteio com seed em
`godot/gameplay/BauCelestial.gd:78`). **PROPOSTA: os 3 pares de estreia são
exatamente esses**, trocando earth+fire de "muro e brasa" para a fusão da
tabela:

| Ordem | Par / Ataque | Por que estreia | Esforço | O que reusa |
|---|---|---|---|---|
| 1º | Fogo+Vento — **Tufão de Brasas** | é o exemplo do Diretor, a vitrine da mecânica, e o par nº 1 do GDD | **médio** — espiral + tração leve são comportamento novo de projétil | partículas e tint do `Projectile` (fogo); o VFX da **Muralha de Brasas nova** (topo de chama animado) vira o rastro do tufão; fogo-no-chão do TerrainSystem |
| 2º | Água+Raio — **Dardo Galvânico** | ensina a reação mais importante do jogo (condução §3.4) sozinho, sem parceiro; é setup+payoff num golpe | **baixo** — é um projétil normal com 2 status em sequência; quase só tint + som | `Projectile` inteiro; os status MOLHADO/condução que `docs/DANO.md` §5 (C2/C3/C5) já especifica; eletrificação de poça do TerrainSystem |
| 3º | Fogo+Terra — **Lasca de Magma** | põe o terreno reativo no centro do palco: a manopla que PINTA o chão | **baixo/médio** — arco balístico + pedir 1 célula de lava/queimando ao TerrainSystem | célula queimando/lava do TerrainSystem; partículas de fogo e terra do `Projectile` |

Os outros 7 entram por ondas, do barato ao caro: Faísca de Plasma e Visgo de
Lama (**baixo** — projétil + 1 status), Onda Torrencial e Centelha Caçadora
(**médio** — leque/homing são forma nova), Jato Escaldante e Rajada de Areia
(**médio** — névoa e revelação pedem VFX de área), Cristal Voltaico (**alto** —
mina persistente com vida, som e estouro é um mini-sistema).

**Dependência honesta:** Dardo Galvânico, Visgo de Lama, Onda Torrencial e
Rajada de Areia dependem do sistema de status no alvo que `docs/DANO.md` §5
propõe (C2/C3/C5) e que **ainda não existe em código Godot**. Tufão, Lasca e
Faísca não dependem — dá para estrear a mecânica antes do kernel de status.

---

## 5. DECISÕES QUE SÃO DO DIRETOR

1. **Todos os números** — dano, cadência, custo de mana, raios, durações das
   fusões: depois do playtest, no `Balance` (o GDD trava números de combate;
   este documento não propõe nenhum).
2. **Quais pares estreiam** no baú — a proposta é manter os 3 de
   `PARES_MANOPLA` (§4); confirmar ou trocar.
3. **A fusão substitui o tiro básico da manopla ou é o "forte" dela?**
   PROPOSTA deste documento: **substitui** — todo estalo é fusão (é o que
   diferencia a manopla de verdade, e a mana de 1,45× já cobra o preço). A
   alternativa: básico alterna elementos como hoje + fusão em cooldown curto.
4. **Os 10 nomes arcanos** da tabela (§2) — todos PROPOSTA, prontos para
   aprovar, vetar ou rebatizar.
5. **A reação nova "molhado lava a areia"** (Rajada de Areia, §2) — é a única
   regra deste documento que não existe em `docs/DANO.md`; se aprovada, entra
   lá antes de virar código (regra de processo do GDD §15).
