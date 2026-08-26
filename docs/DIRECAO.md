# DIREÇÃO — as decisões do Diretor de 26/08/2026, por escrito

> **O que este arquivo é:** o registro fiel do que o Diretor decidiu em 26/08,
> em entrevista de 20 perguntas, sobre luvas, habilidades, castelo, água e bots.
> O GDD continua sendo a fonte da verdade de produto — a seção final lista as
> emendas prontas para colar lá (colar é ato do Diretor, GDD §19.5). Até a
> colagem, **este arquivo é a palavra dele por escrito** e nenhuma implementação
> pode contradizê-lo.
>
> **O que este arquivo NÃO é:** um roadmap. Ordem de implementação mora em
> docs/PROJETO.md.

---

## 1. A Lei das Luvas

**A arma arcana deixa de ser varinha/cajado/manopla-de-bastão e vira LUVA — algo
que se veste na mão.** Os três tiers do GDD §16.2 continuam existindo com os
mesmos papéis; muda a forma física:

| Tier | Nome novo | Papel (inalterado) | Elementos |
|---|---|---|---|
| comum | **Luva Comum** | SMG/pistola: conjuração rápida, curto alcance | 1 |
| raro | **Luva de Conjurador** | rifle: alcance e dano maiores, conjuração lenta | 1 |
| lendário | **Manopla** | só no Baú Celestial; estalar de dedos | **2, par fixo** |

**O que a luva destrava — e o que ela NÃO destrava (decisão nº 1):**

- A luva destrava **apenas o ataque básico** do elemento dela. Cada luva tem
  **cor e forma** do próprio elemento (GDD §10: cor + forma, nunca só cor) —
  quem vê a mão do inimigo sabe o que vem.
- **Passiva, tática e suprema são NATAS** — funcionam sem luva nenhuma. É a
  separação que o GDD §16.2 já fazia: *personagem = habilidades · arma = ataque*.
- **Todos caem do castelo SEM luva.** Achar a primeira luva é a corrida de
  abertura da partida.
- **Ao cair, as táticas já estão CARREGADAS** — usáveis de imediato. Depois de
  usadas, recarregam no tempo de cada mago (seção 4).
- **Uma luva por vez, um elemento por vez.** Duas mãos com elementos diferentes
  NÃO existe fora do baú — "senão ficaria muito roubado" (palavras dele). Dois
  elementos é privilégio da Manopla, e a Manopla continua nascendo só no baú.

**Equipar (decisão nº 5):** sobre a luva no chão aparece o botão **PEGAR** (já
existe na HUD, só com loot ao alcance). Toque = equipa. Se já há luva na mão, o
botão vira **TROCAR** — nada de troca automática por pisar em cima: trocar a
luva boa sem querer é roubo, não conveniência. A animação de equipar precisa
deixar **claro que ele está segurando/vestindo em uma das mãos**.

**Mira e disparo (decisão nº 11):** mão aberta, braço estendido — a mira do
universo de magia. **Enquanto dispara, o braço segue erguido; ao parar ou
esgotar a mana, o braço ABAIXA como sinal de cansaço.** Toda luva tem RECARGA
(o equivalente ao "reload"): o desenho exato do gesto fica em aberto — proposta
da equipe: a luva *apaga* quando esvazia e o mago fecha o punho para
*reacendê-la* (~1,2 s), com som próprio. A Manopla não estende o braço: estala
os dedos e conjura na direção da mira.

### 1.1 A Pyra — validação do que o Diretor descreveu

**Correto, com um refinamento de registro.** A ficha dela (GDD §3.1) diz: perdeu
o braço **esquerdo**; uma manopla de bronze com chama viva o substitui. Logo:

- A luva de loot vai **na mão direita — a humana**. Só ela.
- O braço esquerdo (manopla de bronze) é **NATO e permanente** — é a passiva e a
  suprema dela, não é loot. Ela nunca equipa duas luvas: quando a suprema
  destrava a manopla, não há luva de loot no caminho para destruir ou esconder.
- Isso **não** é desequilíbrio: o braço de bronze não dá ataque básico — dá as
  habilidades que todo mago já tem de graça. O ataque básico dela continua
  refém da luva de loot, como o de todo mundo.
- **Consequência de design que o Diretor pediu na animação da suprema:** ao
  ativar o Braço Livre (lança-chamas), **a mão direita SEGURA o antebraço
  esquerdo como apoio, para não descontrolar**. Se a mão direita está ocupada
  apoiando, ela não conjura ataque básico durante a suprema — o custo fica
  legível no corpo.

### 1.2 Baús especiais com manoplas únicas — ideia VALIDADA

O Diretor perguntou se vale visionar baús com itens que **nunca aparecem no
loot natural** (modelo Apex: care package com armas exclusivas). **Sim, e o
projeto já está a meio caminho:** a Manopla já é exclusiva do Baú Celestial.
A extensão natural:

- Cada baú carrega **UMA manopla de par fixo sorteado** (já é assim) — e, no
  futuro, manoplas **nomeadas** com habilidade única embutida (ex.: uma manopla
  que conjura o combo da Sintonia sozinha, versão fraca), que **nunca** dropam
  no chão.
- Regra de ouro mantida: quanto mais raro o poder, **mais barulhento o
  anúncio** — o baú já cai do céu anunciado para o mapa inteiro.
- **Nada disso entra em código agora.** Fica registrado como direção para
  quando os baús ganharem variedade.

---

## 2. O Castelo e a queda (decisão nº 14)

- O castelo voador cruza o céu **SEM personagens visíveis** — como a nave do
  Apex e o ônibus do Fortnite: um objeto, não uma multidão.
- **O mesmo botão que conjura magia aciona o salto.** Ao acionar, o personagem
  **surge** saindo do castelo (janela/porta) — pode surgir "do nada", isso não
  é problema (palavras dele). A animação a criar é a do castelo (a abertura por
  onde se cai), não a de vinte bonecos pendurados.
- **Bots idem:** surgem no ar em pontos determinados do trajeto, nunca
  aparecem flutuando parados no nada.

---

## 3. Água, lava e a física que começa agora (decisões nº 15–16)

- **Água na altura do peito → modo NADAR.** Velocidade natural, não rápida.
  Outros jogadores veem quem nada. **É possível atirar nadando** (na
  superfície). **Mergulhar esconde** — a contra-jogada de quem foge.
- **Sair da água é lento** por alguns segundos — física de roupa molhada — e
  depois a velocidade normal volta sozinha.
- **Afogamento NÃO existe** (com nado, não é necessário).
- **Gelo continua REAÇÃO, não elemento** (decisão nº 3): água congelada por
  reação vira piso — anda-se por cima. A matriz da Sintonia não muda.
- **Lava e perigos naturais CAUSAM dano** — o que machucaria na vida real,
  machuca no jogo.
- **Queda de altura NÃO causa dano** — regra explícita: "toda queda causar dano
  seria prejudicial para o jogo". (Coerente com Fizz, cuja passiva de
  imunidade a queda vira só o *dobro* do pulo — registrado na seção 6.)

---

## 4. A economia das habilidades (decisões nº 1, 7–10)

### Tática — cooldown em segundos, POR MAGO

Faixa oficial: **5 a 10 s**, com a régua que o Diretor deu: **habilidade que
agride ou causa dano fica no TETO; utilidade e mobilidade ficam no PISO.**
Carregada ao cair do castelo; recarrega após o uso.

### Suprema — CARGA de 0% a 100%, nunca cooldown

- Ao cair do castelo: **0%**. A HUD mostra a **porcentagem no botão** — o
  contador que o Diretor pediu ("senão fica muito roubado").
- **Enche com o tempo** (base ~40 s, por mago) **e acelera com dano causado** —
  modelo Apex, adotado por ordem dele ("faça exatamente igual para facilitar a
  lógica"). Quem luta carrega antes de quem se esconde.
- Só ativa a **100%**; ao ativar, volta a **0%**.
- A **telegrafia do GDD §4.3 continua intocada**: 1–4 s de som + visual antes
  do efeito. Carga não substitui aviso.

### A tabela dos 20 — tempos oficiais de partida (v1, calibrar no playtest)

| # | Mago | Tática | CD | Suprema | Carga base |
|---|---|---|---|---|---|
| 01 | Pyra | Muralha de Brasas | 9 s | Braço Livre | 50 s |
| 02 | Ceifadora | Mão do Vazio | 8 s | Travessia | 40 s |
| 03 | Véu | Atravessar | 6 s | Maré Espectral | 40 s |
| 04 | Corvus | Uivo de Caça | 7 s | Forma de Lobisomem | 50 s |
| 05 | Corvomante | Voo do Olho | 5 s | Grasnido do Fim | 45 s |
| 06 | Olho-de-Éter | Enxame Perscrutador | 8 s | Crisálida | 35 s |
| 07 | Vitalis | Vai, Lúmen | 7 s | Jardim da Aurora | 40 s |
| 08 | Ilusionista | Espelho de Mão | 8 s | Baile de Espelhos | 40 s |
| 09 | Vex | Frascos de Reagente | 9 s | A Grande Obra | 50 s |
| 10 | Tessa | Fio do Tear | 7 s | Tear-Mãe | 45 s |
| 11 | Aelion | Flecha de Éter | 10 s | Chuva do Crepúsculo | 50 s |
| 12 | Umbra | Véu Umbrio | 8 s | Dança das Sombras | 45 s |
| 13 | Brok | Runa-Escudo | 7 s | Forja Viva | 40 s |
| 14 | Gromm | Totem das Chuvas | 7 s | Espírito do Trovão | 45 s |
| 15 | Maris | Onda Prisão | 8 s | Maré Cheia | 45 s |
| 16 | Fizz | Torreta Faísca | 9 s | MEGABOBINA | 50 s |
| 17 | Sylva | Broto Guardião | 7 s | Coração da Mata | 45 s |
| 18 | Basalto | Punho Sísmico | 9 s | Monólito | 40 s |
| 19 | Noctus | Mordida do Vazio | 6 s | Forma de Névoa | 40 s |
| 20 | Pip | Zip-Zag | 7 s | Supercélula | 45 s |

A régua aplicada: dano puro no teto (Aelion 10, Pyra/Vex/Fizz/Basalto 9);
Tessa fica em 7 s por decisão anterior deliberada — ela tece VÁRIOS fios;
mobilidade e utilidade no piso (Corvomante 5, Véu/Noctus 6). Supremas de dano
carregam devagar (50 s); informação carrega rápido (Crisálida 35 s). **Números
v1 — o playtest humano (Roblox V1–V5) continua sendo quem os confirma.**

---

## 5. Telegrafia e área — como toda magia avisa (decisão nº 12)

- Magia de ÁREA marca **um círculo no chão, visível PARA TODOS**, com **pelo
  menos 1 s** entre aparecer e machucar. Ver e reagir é a contra-jogada justa
  (a referência do Diretor: as bombas do Fuse marcam onde pegam; a mira da
  Rampart tem laser e som).
- **A magia VIAJA da mão ao círculo** — nada nasce direto no alvo (GDD §4.1:
  tudo é projétil). O tornado do exemplo dele: sai do conjurador em direção ao
  círculo e **quem cruzar o caminho é pego no trajeto**.
- **Cada mago tem assinatura de som + luz própria** ao conjurar — o "som do
  estalo" da Manopla é o modelo: dá para saber QUEM conjurou sem ver. As
  assinaturas por mago estão na tabela da seção 6 (fase ANTECIPA).

---

## 6. Os 20 kits — funcionalidade e as TRÊS FASES da animação

O Diretor pediu: para cada tática e suprema, **como ANTECIPA, como INICIA, como
ENCERRA**. A gramática é fixa (é ela que torna o jogo legível):

> **ANTECIPA** = o corpo + som que avisam (o inimigo pode reagir) ·
> **INICIA** = o efeito no mundo · **ENCERRA** = o preço visível no corpo.

Os kits 1–10 são os do GDD §3 (aprovados); os 11–20 vêm das fichas
`personagens/*.md` — **já rascunhados, aguardando a aprovação do Diretor para
subir ao GDD** (pendência antiga, agora com tempos).

**01 · Pyra — Braço Livre (suprema, 50 s)**
ANTECIPA: rugido da fornalha; o bronze do braço esquerdo brilha até branco (1,4 s).
INICIA: a manopla destrava — lança-chamas em leque; **a mão direita agarra o
antebraço esquerdo como apoio para não descontrolar** (ordem dele); sem ataque
básico durante.
ENCERRA: o braço ESFRIA fumegando, pende morto 4 s; ela sacode a mão direita
dormente. Tática (Muralha, 9 s): risca o chão com o pé — linha de brasas; apagar
com água "molha" o braço (+1 s de recarga).

**02 · Ceifadora — Travessia (40 s)**
ANTECIPA: o mundo perde cor ao redor dela; um coro grave sobe (2 s).
INICIA: rasga o ar em linha reta — uma ferida de luz negra atravessável 3 s.
ENCERRA: o rasgo cicatriza com estalo de vidro; ela sai do outro lado SEM
sombra por 4 s (revelada). Tática (Mão do Vazio, 8 s): aponta com dois dedos,
a mão de sombra irrompe do chão.

**03 · Véu — Maré Espectral (40 s)**
ANTECIPA: um SINO espectral toca no mundo real (1,6 s) — o counter sonoro.
INICIA: ela e aliados a 6 m mergulham no plano espectral, translúcidos e velozes.
ENCERRA: todos "emergem" com uma golfada de ar; 1 s sem conjurar. Tática
(Atravessar, 6 s): some num borrão vertical, eco visível fica na entrada.

**04 · Corvus — Forma de Lobisomem (50 s)**
ANTECIPA: uivo ouvido num raio enorme; a coluna dele verga (2 s).
INICIA: 30 s de besta — corre 40% mais rápido, garras, cura ao abater, **sem
magias**.
ENCERRA: a transformação desmancha com ele de joelhos, ofegante, 2 s vulnerável.
Tática (Uivo, 7 s): denuncia a posição dele — o preço já é o anúncio.

**05 · Corvomante — Grasnido do Fim (45 s)**
ANTECIPA: o corvo sobe em espiral grasnando — sombra circular cresce no chão (2,5 s).
INICIA: pulso antimagia na área: 50 no escudo, derruba 1 nível, quebra construções.
ENCERRA: o corvo despenca exausto no ombro dele; a pedra de obsidiana da órbita
apaga por 5 s. Tática (Voo do Olho, 5 s): o corpo fica parado e INDEFESO — o
preço é o próprio corpo.

**06 · Olho-de-Éter — Crisálida (35 s)**
ANTECIPA: casulo pulsante plantado, batimento luminoso acelerando (2 s, destrutível).
INICIA: eclode — mariposas pousam nos inimigos da área: revelados 6 s.
ENCERRA: as mariposas viram pó de luz; ele fica 1 s "cego" (a tradução
vibração→luz satura). Tática (Enxame, 8 s): túnel estreito com 1,4 s de atraso.

**07 · Vitalis — Jardim da Aurora (40 s)**
ANTECIPA: Lúmen dispara ao centro e roda desenhando o círculo em luz (1,5 s).
INICIA: jardim floresce — cura 8 hp/s, reerguer 50% mais rápido, visível
através de paredes.
ENCERRA: as flores murcham em pétalas que sobem; Lúmen volta arrastada, apagada
(passiva off por 3 s). Tática (Vai Lúmen, 7 s): ela beija a fada e sopra.

**08 · Ilusionista — Baile de Espelhos (40 s)**
ANTECIPA: som de vidro trincando em acorde; ele abre os braços como maestro (1,8 s).
INICIA: 5 reflexos invertidos + 2,5 s invisível.
ENCERRA: os reflexos quebram em sequência, cada um com uma nota — a última é a
posição real dele por 1 s. Tática (Espelho, 8 s): saca o espelho do colete com
floreio.

**09 · Vex — A Grande Obra (50 s)**
ANTECIPA: o fole do peito INFLA com chiado crescente; runas de transmutação
giram nos pés (3 s).
INICIA: névoa enorme 12 s — lentidão + SELA consumíveis (dos dois lados).
ENCERRA: o fole esvazia num suspiro comprido; ele curva, tossindo — 2 s sem
correr. Tática (Frascos, 9 s): arremesso em arco com tilintar de vidro.

**10 · Tessa — Tear-Mãe (45 s)**
ANTECIPA: o marca-passo rúnico ACELERA audível; fios de luz sobem das mãos (2 s).
INICIA: tear giratório que absorve projéteis e tece escudo para aliados.
ENCERRA: o tear se desfia; as cicatrizes de Lichtenberg dela brilham e apagam.
Tática (Fio, 8 s): lança as duas âncoras como agulhas de tricô.

**11 · Aelion — Chuva do Crepúsculo (50 s)** *(ficha; aguarda aprovação)*
ANTECIPA: dispara UMA flecha ao céu — todo o servidor vê o risco dourado (3 s).
INICIA: feixe de flechas espectrais cai numa linha longa e estreita, marcada no
chão.
ENCERRA: o arco esfria vertendo pó de estrela; 2 s sem carregar a tática.
Tática (Flecha, 10 s): carregar deixa 40% mais lento e brilhando.

**12 · Umbra — Dança das Sombras (45 s)**
ANTECIPA: a luz ao redor dela É SUGADA para dentro (escurece 2 m, 1,5 s).
INICIA: 6 s — cada esquiva teleporta 8 m e deixa sombra que explode fraca.
ENCERRA: as sombras restantes implodem nela; um segundo cega de LUZ — o oposto
dela. Tática (Véu Umbrio, 8 s): penumbra; o 1º golpe saindo dá +50%.

**13 · Brok — Forja Viva (40 s)**
ANTECIPA: ergue a bigorna acima da cabeça — clang de metal a cada passo (2 s).
INICIA: bigorna-totem 12 s: regenera ESCUDO (nunca vida) + runa-escudo pessoal.
ENCERRA: a bigorna racha e afunda no chão; as runas do martelo apagam por 3 s.
Tática (Runa-Escudo, 7 s): martelada no chão, muralha curva sobe.

**14 · Gromm — Espírito do Trovão (45 s)**
ANTECIPA: ajoelha e toca o chão; tambores + o chão treme numa linha à frente (2,5 s).
INICIA: bisão espectral atravessa o campo — empurra, derruba muros, rastro
acelera aliados.
ENCERRA: o bisão dissolve em chuva fina; Gromm levanta com o peso dos anos —
2 s lento. Tática (Totem, 7 s): planta com as duas mãos, chuva local apaga fogo.

**15 · Maris — Maré Cheia (45 s)**
ANTECIPA: puxa as mãos para trás como quem recolhe a maré — a água da área
RECUA primeiro (2 s, o aviso é o mundo secando).
INICIA: parede de maré em câmera lenta inunda a área: 30 cm d'água por 10 s,
conduz raio (dois gumes).
ENCERRA: a água drena em espiral por onde ela pisa; roupa encharcada — 2 s
lenta (a própria física da seção 3). Tática (Onda Prisão, 8 s): esfera lenta,
coluna d'água suspende 1,2 s.

**16 · Fizz — MEGABOBINA (50 s)**
ANTECIPA: arma a bobina peça a peça, rindo; zumbido elétrico crescente (2 s,
destrutível).
INICIA: UM raio devastador no marcado mais próximo; desliga construções no
caminho.
ENCERRA: a bobina derrete em sucata fumegante; o cabelo dele fica em pé —
molas dos calcanhares travadas 2 s (sem pulo alto). Tática (Torreta, 9 s):
saca da mochila e finca no chão.

**17 · Sylva — Coração da Mata (45 s)**
ANTECIPA: finca o cajado-galho; raízes douradas correm pelo chão desenhando a
área (2 s).
INICIA: 8 s — aliados regeneram vida E escudo; inimigo que cruza é agarrado 0,8 s.
ENCERRA: as raízes viram madeira seca e esfarelam; a pele dela perde o verde
por 3 s (a seiva foi gasta). Tática (Broto, 7 s): planta com carinho; em grama
vira moita de cobertura.

**18 · Basalto — Monólito (40 s)**
ANTECIPA: cruza os braços e AFUNDA meio metro no chão com estrondo de pedra (1,8 s).
INICIA: 6 s torre inamovível — +60% de redução, provoca, reflete estilhaços.
ENCERRA: as placas se soltam numa avalanche em miniatura; 2 s sem correr
(pernas "endurecidas"). Tática (Punho, 9 s): soco no chão, cone de pedra.

**19 · Noctus — Forma de Névoa (40 s)**
ANTECIPA: floreio de capa que engole o próprio corpo; a luz ao redor avermelha (1,5 s).
INICIA: 4 s névoa carmesim — intangível a projéteis, deixa lentidão por onde
passa, não conjura.
ENCERRA: recondensa de joelhos, faminto — a passiva não gera mana por 3 s.
Tática (Mordida, 6 s): investida de 6 m; errar = 1,5 s vulnerável.

**20 · Pip — Supercélula (45 s)**
ANTECIPA: sobe 1 m além do normal e gira; as quatro asas viram hélice — o
zumbido dobra de volume (1,5 s).
INICIA: mini-nuvem a segue 8 s disparando raios fracos automáticos no mais
próximo.
ENCERRA: a nuvem chove em cima DELA — encharcada, voa baixo e lento 2 s (e
conduz raio: janela de punição). Tática (Zip-Zag, 7 s): três dashes em
zigue-zague com faíscas.

---

## 7. Bots de verdade (decisões nº 17–18)

- **FFA imediato: todos contra todos.** O defeito atual tem endereço —
  `Main.gd` crava o jogador como alvo de todos os 6 no nascimento. Não é IA
  agressiva: é ausência de percepção. **Muda agora.**
- **Percepção por PRESENÇA e por SOM**, nunca por onisciência: raio de
  percepção; disparo e passos DENUNCIAM (pegadas, respiração — como todo jogo
  de ação, palavras dele); quem toma dano aprende de onde veio.
- **As leis do jogador valem para os bots**: caem sem luva, precisam achar
  luva para ter ataque básico, táticas e supremas nas mesmas regras. (Entra em
  etapas — a primeira é FFA + percepção; loot e habilidades de bot são as
  seguintes.)

## 8. Treino e tutorial (decisão nº 19)

- **Lobby de treino:** qualquer mago, todas as luvas expostas, bonecos de
  treino, suprema carregando rápido — para testar personagem, equipamento e
  junções. É também a ferramenta de teste do próprio Diretor.
- **Tutorial básico NÃO obrigatório** para jogadores novos.
- Prioridade: o treino vem antes do tutorial (destrava o teste de hoje).

---

## 9. Emendas prontas para o GDD (colar é ato do Diretor)

1. **§16.2** — trocar a forma física dos tiers: varinha→Luva Comum,
   cajado→Luva de Conjurador (papéis e números inalterados). Nota: luvas caem
   no chão como loot; equipar exige o botão PEGAR/TROCAR.
2. **§4.2** — suprema passa de cooldown para **CARGA 0→100%** (base ~40–50 s
   por mago + aceleração por dano, modelo Apex). Táticas: faixa oficial 5–10 s,
   agressão no teto. Todos caem com tática carregada e suprema a 0%.
3. **§3** — anexar as três fases de animação (ANTECIPA/INICIA/ENCERRA) de cada
   kit, conforme seção 6 deste arquivo.
4. **§8/§14** — física de água: nadar do peito, tiro na superfície, mergulho
   esconde, saída lenta; sem afogamento; sem dano de queda; lava e perigos
   naturais causam dano.
5. **Kits 11–20** — subir ao GDD com os tempos da seção 4 (pendência antiga de
   aprovação, agora com números).
6. **§11** — castelo sem personagens visíveis; o botão de magia aciona o salto;
   o personagem surge ao acionar.


---

## 10. A ilha rumo ao lançamento — e o que sai do Meshy (pergunta de 26/08)

O Diretor perguntou: para lançar, além de melhorar a ilha graficamente e
colocar mais elementos, **o que vamos criar e o que usa o Meshy?**

### Tamanho: densidade antes de área

A ilha tem 300 m para **7 magos** — a proporção está saudável (o mini-BR do
GDD §8 é 16–20 jogadores; quando o jogo chegar lá, a ilha cresce junto).
Crescer área AGORA espalharia os mesmos 7 num mapa vazio e adiaria os
encontros. **O caminho do lançamento é densidade**: mais elementos por POI,
não mais metros. E cada leva de elementos entra com **FPS medido no aparelho**
— que continua sem medição alguma.

### O que NUNCA sai do Meshy (continua procedural)

O **terreno em si**: altura, colisão, água, praia, grama. Três sistemas
dependem de `Island.height()` ser determinístico e barato — o terreno reativo,
a zona e o pouso da queda. Malha de IA aqui quebraria os três. O Meshy é para
o que fica **em cima** do terreno.

### O que vem do Discover (0 créditos + refino no Blender)

A regra já escrita em `docs/pipeline-arte/CENARIO/04`: genérico se acha, não
se gera — árvores, pedras, penhascos, arcos, colunas, caixas, barris, tochas,
pontes, entulho. **Toda peça externa passa pela regra anti-asset-flip**
(retextura, shader Arkana, mudança de proporção ou kitbashing — no mínimo uma).

### O que se GERA no Meshy (créditos — a identidade não se acha pronta)

Em ordem de aparição na tela do jogador:

| # | Peça | Por quê | Créditos est. |
|---|---|---|---|
| 1 | **O Castelo Voador** | é a 1ª coisa da partida, e hoje é primitivas | ~20–35 |
| 2 | **As 3 Luvas** (comum/conjurador/manopla) | viraram O loot central da Lei | ~15–60 |
| 3 | **O Baú Celestial** | o evento mais anunciado do mapa | ~20 |
| 4 | **Estátuas do Selo + arcos rúnicos** (Ruínas) | o POI vira lore, não pedra genérica | ~40 |
| 5 | **Torres Arcanas** | têm função de gameplay (GDD) — autoral por regra | ~40 |
| 6 | **Altar de Sintonia** | o pilar do jogo merece um landmark | ~20 |

Estimativa da leva inteira: **~150–220 créditos** (props estáticos não pagam
rig nem animação — só malha+textura, e remesh quando preciso). Referência: um
personagem completo custa ~41.

### Ordem de produção (uma peça por vez, medida no aparelho)

1. Castelo (herói da abertura — o efeito "jogo de verdade" mais barato)
2. Luvas (o jogador olha para elas a partida inteira: chão, mão e HUD)
3. Baú → Ruínas → Torres → Altar
4. Depois da leva autoral: passada Discover nos genéricos (árvores/pedras)

**O que muda no código:** quase nada — `Island.gd` já instancia malha externa
por caminho (`_add_mesh`), e o modelo do castelo pendura em `world/Castelo.gd`.
A regra dura continua: colisão SIMPLES feita à mão por peça (primitivas),
nunca trimesh do modelo de IA.
