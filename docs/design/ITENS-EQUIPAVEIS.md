# ITENS EQUIPÁVEIS — o que vale vestir (design, só papel)

> **Ordem do Diretor (27/08, verbatim):** *"no jogo seria interessante como no
> Spellbreak mais itens aplicáveis, como melhoria de mira, um óculos para
> equipar, uma capa que ajudaria planar ou até um voo curto, entre outras
> coisas. Valide a utilidade, jogabilidade, e aplicação no jogo, o que
> mudaria."*
>
> **O que este documento é:** a validação, item por item, com veredito honesto —
> inclusive "NÃO VALE" onde não vale. Nenhuma linha de código muda por este
> documento e nenhum número de balanceamento entra em `godot/core/Balance.gd`:
> o GDD trava números de combate até o playtest (GDD §19.5, mesma regra de
> `docs/design/MANOPLAS-FUSAO.md`). Fatos citam o arquivo de origem; invenção
> nova está marcada **PROPOSTA**.
>
> **O achado que muda a conversa:** dois dos três pedidos do Diretor **já estão
> desenhados no projeto** — a "melhoria de mira" é a **Lente de Foco** do GDD
> §16.3, e o slot dela **já existe em código** (`godot/gameplay/Arma.gd`:
> `"runas": 1` na Luva Comum, `2` no Conjurador, `3` na Manopla, comentado como
> *"GDD §16.3 — estrutura só"*). Não é item novo: é um sistema pronto e vazio
> esperando efeito. O terceiro pedido — a capa — tem um problema de cronologia
> que a seção 4 resolve.

---

## 1. O PRINCÍPIO E O LIMITE

### O princípio — qual espaço sobra

A Luva manda no **ataque básico** e só nele: ela destrava o disparo e o elemento
(`docs/DIRECAO.md` §1 — *"a luva destrava apenas o ataque básico"*), e o tier
multiplica o perfil do elemento sem inventar número próprio
(`godot/gameplay/Arma.gd`). O Kit manda no **que o mago faz**: passiva, tática
em cooldown de 5–10 s e suprema em carga de 0→100% são **natas**, funcionam sem
luva nenhuma (`docs/DIRECAO.md` §1 e §4; `godot/gameplay/KitRunner.gd`). Entre
os dois, o espaço que sobra para um item é estreito e tem nome: **afinar o
ataque que a luva já dá, e mudar como o mago ATRAVESSA o mapa e o LÊ — nunca
dar uma ação nova.** Item que dá habilidade ativa nova é a Runa do Spellbreak
(`docs/referencias/SPELLBREAK.md` §1.4: *"item equipável que dá uma 3ª
habilidade ativa em cooldown"*) e invade o território do Kit — 20 magos foram
desenhados para que a tática seja a identidade deles; um item de chão que dá voo,
dash ou invisibilidade apaga de uma vez o Corvomante, a Umbra, o Véu e o
Ilusionista. E item que só dá número invade o território do `Balance`, onde o
número já existe e já está calibrado na régua de TTK. **A regra do documento: um
item do Arkana é PASSIVO, não ganha botão, e o que ele muda é uma DECISÃO do
jogador (que caminho fazer, quando abrir o braço), não um multiplicador.**

### O limite — quantos eixos de mobilidade

O Arkana tem hoje **dois** eixos de mobilidade em partida: corrida com
aceleração (`Balance.MOVE`, com curva de joystick e banking) e esquiva/dash
(`Balance.DODGE`, 5 m em 0,18 s com arranque duplo). O nado é modulação do
primeiro, não eixo: entra no **produto único** de velocidade como todos os outros
fatores (`godot/gameplay/Pawn.gd:66` — `speed × terrain_mult × status_mult ×
speed_factor × agua_mult`). E **não existe pulo**: nenhum `jump` em
`godot/gameplay/Player.gd`, `Pawn.gd` ou `Balance.gd`. A verticalidade do jogo
está inteira confinada numa fase fechada, a queda de abertura
(`godot/gameplay/queda/Queda.gd`), onde o `_physics_process` do Player está
**desligado de propósito** para que nenhuma magia saia no ar.

O Spellbreak tinha **três**, empilhados: corrida/pulo + levitação por mana +
runa em cooldown (`docs/referencias/SPELLBREAK.md` §1.4), e o resultado
documentado é *"players barely stay on the ground for more than a couple of
seconds"*. A §5 do mesmo dossiê aponta a conta: retenção, não aquisição, foi o
problema, e a causa nº 2 é *"teto de habilidade brutal sem proteção ao novato"* —
levitação + runa + mira em projéteis + gestão de mana. **O Arkana aceita DOIS
eixos, e nenhum deles aéreo.** Três razões, na ordem de peso:

1. **O touch cobra por eixo.** A mira e o disparo do Arkana são **um gesto só** —
   arrastar a partir do botão da magia e soltar (GDD §563; dois gestos separados
   foram REPROVADOS em aparelho real). Um eixo aéreo é um botão novo na mesma
   mão que já mira, atira, esquiva, usa tática e suprema (`godot/ui/Hud.gd`,
   inf-dir). No celular isso não é "mais profundidade", é o dedo escolhendo
   entre atirar e se mover.
2. **O eixo aéreo desliga o pilar do jogo.** O terreno reativo (GDD §14) só
   existe no chão: poça eletrificada, fogo que propaga por orçamento, lama,
   gelo que vira rota. Quem voa não pisa em nada — e a magia de área, que marca
   círculo no chão com 1 s de aviso (`docs/DIRECAO.md` §5), perde o alvo.
3. **Cada eixo dobra o que o novato precisa aprender antes de acertar o
   primeiro tiro.** Tudo é projétil com tempo de viagem (GDD §4.1): prever um
   alvo que corre já é o teto de habilidade do jogo. Prever um alvo que corre,
   dasha e voa é o Spellbreak.

**Consequência dura:** o único item de mobilidade que este documento admite
discutir é um que **não crie eixo** — que reaproveite o planeio que já existe e
que cobre o preço com o mesmo bloqueio da queda (quem plana não conjura). É a
seção 4.

---

## 2. OS SLOTS (PROPOSTA)

**Proposta: UM slot de item, mais os slots de runa que a luva já tem.** Nada de
cabeça, nada de pés.

| Slot | Onde já vive | O que porta | Custo de tela |
|---|---|---|---|
| **Mão** | existe: `godot/gameplay/ArmaSlot.gd` (nó filho do pawn) | a Luva — comum / conjurador / manopla | zero novo (o rótulo de arma já está na HUD, `ARMA_LBL_S = 2.5`) |
| **Runas da luva** | existe como estrutura: `Arma.gd` `"runas": 1/2/3` | Lente de Foco, Reservatório (GDD §16.3) | **zero** — a runa viaja DENTRO da luva; não é linha de HUD, é propriedade do item que o jogador já carrega |
| **Corpo (o Manto)** | **não existe** — é a única adição deste documento | um item passivo por partida | **1 badge permanente** (teto de 4, `godot/ui/HudAviso.gd:MAX_BADGES`) |

### Por que UM, e o que ele custa

Um slot de corpo é a menor adição que ainda permite dizer "sim" ao Diretor. O
custo não é hipotético, é medível em três lugares:

- **Badge.** A HUD tem teto de 4 badges e a lei escrita é *"o que perde não
  encolhe: SOME"* (`godot/ui/Hud.gd`, cabeçalho). Um manto equipado é um badge
  que nunca sai — sobram 3 para estados de kit, queimadura, molhado, escudo.
- **O botão PEGAR deixa de ser inequívoco.** Hoje o prompt do inf-meio tem um
  destino só (`godot/gameplay/Loot.gd`, `pegar()` é *"a porta ÚNICA do pegar"*),
  e a decisão nº 5 do Diretor foi explícita: *"nada de troca automática por
  pisar em cima — trocar a luva boa sem querer é roubo"* (`docs/DIRECAO.md`
  §1). Com duas categorias no chão, o mesmo botão passa a ter dois destinos e a
  chance de erro no meio da briga sobe. **Mitigação PROPOSTA:** manto e luva
  nunca nascem no mesmo raio de 2,2 m (`Loot.RAIO_PEGAR`), e o prompt escreve a
  categoria.
- **Uma decisão a mais no meio do tiroteio.** É o custo que não aparece em
  nenhum arquivo: parar 1 s para avaliar um item é 1 s de alvo parado.

**Dois slots (corpo + cabeça) foram considerados e REPROVADOS:** 2 badges
permanentes de 4, duas categorias novas competindo pelo mesmo prompt, e — o
argumento que decide — nenhum dos itens de cabeça propostos sobreviveu ao
veredito da seção 3.

---

## 3. TABELA DOS ITENS, COM VEREDITO

Nomes arcanos são **PROPOSTA**, exceto Lente de Foco e Reservatório, que são do
GDD §16.3. Nenhuma linha propõe número — dano, alcance, duração e raio ficam
para depois do playtest.

| Nome arcano | O que FAZ | Como encaixa no que já existe | O que muda na jogabilidade | Contra-jogada | Custo de tela | Esforço | VEREDITO |
|---|---|---|---|---|---|---|---|
| **Lente de Foco** (runa — o "melhoria de mira" do Diretor) | o projétil da luva viaja mais rápido e mais longe | encaixe **pronto**: `Arma.gd` `"runas"` já reserva 1/2/3 slots; o efeito é multiplicador sobre `projectile_speed`/`range` do elemento (`Balance.FIRE` etc.), exatamente o padrão do tier — a runa nunca vira 2ª fonte de dano | acertar quem se MOVE: menos previsão de liderança, e o alcance passa a cobrir o vão entre dois POIs. É a competência central do jogo (GDD §4.1) ficando mais barata | quem tem lente ainda erra alvo que dasha; e o `range` continua sendo corte seco (passou, o projétil SOME — `docs/DANO.md` §3.6), então a lente estica o corte, não o apaga | **zero** — viaja na luva | **baixo** | **ENTRA NO MVP** — é o pedido do Diretor com o encaixe mais barato do projeto: um sistema já estruturado e vazio |
| **Reservatório** (runa, GDD §16.3) | mais conjurações antes da recarga da luva | multiplica `mana_cost` do elemento para baixo; a mana é o teto real de DPS do jogo (`Balance.PLAYER.mana_regen`, comentário do KNOB) e o limitador desenhado da manopla (`docs/DANO.md` §3.1) | segurar o dedo mais tempo numa briga longa em vez de recuar para regenerar; muda o RITMO da luta, não o dano por tiro | é o inverso da manopla: quem tem reservatório atira mais, não mais forte — perde igual para quem acerta melhor. E mexer nele **encosta no limitador da manopla**: se o desconto for grande, a arma lendária deixa de ter preço | **zero** | **baixo** | **ENTRA NO MVP** — mesmo encaixe da Lente, e é o par natural dela (as duas juntas dão à runa uma escolha, não um caminho único). **Ressalva:** o número é sensível — o playtest decide, e o teto é "nunca desfazer o limitador da manopla" |
| **Monóculo do Arauto** (o "óculos", reenquadrado) | a até ~um punhado de metros, mostra sobre o inimigo o ELEMENTO da luva dele | reusa o que já existe: cada luva tem cor **+ forma** por elemento (`Loot.gd`, cabeçalho: *"COR + FORMA é lei"*), e o dossiê registra o padrão do Spellbreak de mostrar as habilidades disponíveis do alvo (`SPELLBREAK.md` §3), classificado lá como *"candidato a backlog, não MVP"* | saber se quem vem na sua direção tem Água (vai te molhar antes do raio) ou Terra (vai derrubar sua cobertura) — informação de CONTRA-JOGADA, não de poder | não revela posição: o inimigo precisa estar visível de qualquer forma. Não vale nada contra quem já está atirando em você — vale antes de decidir engajar | **1 elemento sobre a barra do inimigo** — território da HUD, precisa caber sem virar sopa | **médio** (é trabalho de HUD, não de item) | **ENTRA DEPOIS** — bom item, resolve uma lacuna real de leitura, mas o custo é HUD e a HUD tem lei de prioridade |
| **Óculos que vê através de parede / revela inimigos** (a versão literal do Spellbreak) | mostra inimigo atrás de cobertura | seria a Runa do Spellbreak copiada (`SPELLBREAK.md` §1.4) | — | — | alta | alto | **NÃO VALE** — apaga de uma vez a tática de quatro magos (Corvomante: Voo do Olho; Olho-de-Éter: Enxame Perscrutador; Umbra: Véu Umbrio; Ilusionista) e transforma cobertura em decoração. Informação é território de KIT, com corpo indefeso pagando o preço (`docs/DIRECAO.md` §6: o Voo do Olho deixa o corpo PARADO) |
| **Manto Planador** (a "capa") | ao sair de uma borda, o manto abre e o mago plana em vez de cair | reusa `Queda.gd` (`VEL_PLANEIO`, `VEL_PLANEIO_HORIZ`) e o mesmo bloqueio dela: **quem plana não conjura** | atravessar um vale em vez de descer o morro; rota nova no mapa sem botão novo | alvo alto, lento (planeio é mais devagar que corrida + dash) e sem tiro, contra um jogo onde todo projétil tem tempo de viagem: planar em cima de alguém é se oferecer | **1 badge** | **médio/alto** | **ENTRA DEPOIS, sozinho e medido** — ver seção 4. É o primeiro eixo vertical do jogo: entra numa leva só dele, com FPS no aparelho antes e depois (`docs/DIRECAO.md` §10.1) |
| **Voo livre / voo curto reativável à vontade** | sobe e voa | — | — | — | +1 botão | alto | **NÃO VALE** — é literalmente o 3º eixo que produziu *"players barely stay on the ground"* (`SPELLBREAK.md` §1.4) e a causa nº 2 do fracasso (§5). Ver seção 4 (c) |
| **Botas / Coturno de Vento** (+velocidade) | anda mais rápido | escreveria no **produto único** (`Pawn.gd:66`), onde já brigam terreno, status e água | — | — | 1 badge | baixo | **NÃO VALE** — é o pior tipo de item: só número, e o número mais perigoso do jogo. Velocidade permanente desloca TTK, alcance efetivo de toda magia de área e a janela de 1 s de aviso do §5 de uma vez. Se um dia um item mexer em velocidade, é **temporário e por efeito**, pela porta única que já existe (`KitRunner.afetar_velocidade`), nunca passivo de partida inteira |
| **Talismã do Silêncio** (apaga a assinatura sonora do conjuro) | ninguém ouve você conjurar | — | — | — | 1 badge | médio | **NÃO VALE** — cada mago tem assinatura de som + luz própria justamente para que *"dá para saber QUEM conjurou sem ver"* (`docs/DIRECAO.md` §5). Um item que apaga a contra-jogada dos OUTROS é pior que um item que só dá número: o número dá vantagem, esse remove a informação com que o adversário se defende |
| **Manto do Vulto** (camuflagem/invisibilidade) | fica difícil de ver | — | — | — | 1 badge | médio | **NÃO VALE** — é a runa de invisibilidade do Spellbreak (§1.4) e já é o kit da Umbra e do Ilusionista. Item de chão que entrega a identidade de um mago é o roubo que o Diretor vetou na Lei das Luvas (*"senão ficaria muito roubado"*) |

### A lacuna que apareceu na análise (não é item equipável, e é mais urgente que todos)

O loop de loot do Arkana tem **uma categoria só**: luva. `Loot.espalhar()` nasce
12 varinhas + 4 cajados e nada mais (`godot/gameplay/Loot.gd`, `QTD_VARINHA` /
`QTD_CAJADO`), e o escudo **não regenera sozinho** por lei — só pergaminho,
passiva da Tessa e suprema do Brok (`Balance.ESCUDO.regen = 0.0`). Ou seja: o
GDD já legislou um consumível (pergaminho, GDD §16.5) que o mapa não oferece.
**PROPOSTA:** antes de qualquer item equipável novo, o loop ganha o **consumível
de escudo/cura** — mesma porta de `Loot`, zero slot, zero badge, e é ele que dá
ao jogador algo para fazer com o loot depois de já ter a luva boa. É a adição de
maior retorno por esforço deste documento inteiro, e não estava no pedido.

---

## 4. O CASO DA CAPA DE VOO, EM DETALHE

É o pedido mais forte do Diretor e o mais perigoso — o dossiê é explícito sobre
o que a mobilidade extra fez com a retenção do Spellbreak (§5). As três versões:

### (a) Capa que só melhora o PLANAR que já existe na queda

**Veredito: VAZIA por cronologia — não é uma questão de equilíbrio.** A queda é
a **abertura** da partida: todos caem do castelo **sem luva e sem nada**
(`docs/DIRECAO.md` §1: *"todos caem do castelo SEM luva; achar a primeira é a
corrida de abertura"*), e durante a queda o `_physics_process` do Player está
desligado justamente para que nenhum poder saia no ar (`Queda.gd`, cabeçalho).
**No único momento em que essa capa faria efeito, o jogador não pode tê-la** —
não existe loot antes do pouso. As duas únicas formas de dar a ela um efeito
seriam:

- entregá-la **antes** da partida (lobby/loja) — e aí um item de progressão passa
  a decidir quem pousa primeiro na luva boa: pay-to-win na abertura, veto óbvio;
- guardá-la **entre** partidas — o projeto não tem inventário persistente de
  partida, e `docs/design/DESBLOQUEIO-ELENCO.md` §2.4 já fixou que a moeda
  compra personagem, não poder.

**Efeito no equilíbrio:** nenhum, porque o efeito nunca acontece. **Efeito no
touch:** nenhum. Descartada.

### (b) Capa que dá planeio REATIVÁVEL no chão (voo curto)

**Veredito: é a única versão viável — e mesmo ela precisa ser desenhada com um
gatilho que não crie botão.** O detalhe técnico que decide o desenho: **o jogo
não tem pulo.** Nenhum `jump` em `Player.gd`, `Pawn.gd` ou `Balance.gd`; o pulo
existe só como promessa de kit (a passiva do Fizz vira *"o dobro do pulo"*,
`docs/DIRECAO.md` §3 e §6). Sem pulo, "reativar o planeio no chão" a partir do
nível do solo é andar com uma capa aberta. Então o gatilho honesto é a
**geometria da ilha**: morros e penhascos já existem, e `Island.height(x,z)` é a
verdade do chão que a própria queda consulta.

**PROPOSTA de desenho mínimo:** *o Manto abre sozinho quando o mago SAI de uma
borda com altura suficiente — sem botão novo — e fecha ao tocar o chão.
Enquanto planado, valem as leis da queda: sem magia no ar.*

- **Equilíbrio, quem voa vê e atira de cima?** Não atira. É a regra que a queda
  já implementa e que o manto herda literalmente — e é ela que impede o problema
  clássico. Quem plana ganha **rota e visão**, não ângulo de tiro.
- **Quem está no chão responde como?** Melhor do que contra um corredor. O
  planeio desce a 12 m/s com 16 m/s horizontais (`Queda.gd`) numa trajetória
  **balística e previsível**, sem dash, sem cobertura e sem poder revidar,
  contra um jogo em que **todo** tiro é projétil com tempo de viagem: prever um
  planador é o alvo mais fácil que o Arkana oferece. O erro do Spellbreak não
  foi permitir altura — foi permitir **atirar** de lá com mana ilimitada de
  reposicionamento.
- **Controle touch:** **zero botão novo** nesta versão. Se o Diretor quiser
  reativação livre (abrir o manto quando quiser), o custo é +1 botão no inf-dir,
  na mesma mão que mira e atira — e aí a recomendação muda para "não".
- **Esforço honesto: médio/alto.** O planeio de hoje vive dentro de uma máquina
  de 4 fases que escreve posição direto, **sem colisão no ar** por decisão
  declarada (`Queda.gd`: *"ponytail: sem colisão no ar — não dá pra pousar em
  cima de árvore ou telhado"*). Planeio em partida precisa conviver com o
  `CharacterBody3D` e com a colisão do mundo: não é reusar a constante, é
  reusar o **conceito**. É por isso que ele entra sozinho, medido.

### (c) Voo livre

**Veredito: NÃO. Sem contraproposta.** Reproduz o eixo que o dossiê aponta como
causa (§1.4 + §5); exige colisão aérea que a arquitetura da queda evita de
propósito; exige câmera nova; desliga o terreno reativo (GDD §14), que é o pilar
que diferencia o Arkana; e custa botão na mão que já mira e atira. É a decisão
negativa mais fácil deste documento.

### Recomendação para o MVP

**A capa NÃO entra no MVP.** Entra a versão (b), depois, em leva própria, com
FPS medido no aparelho antes e depois — e só depois de o loop de loot ter mais
de uma categoria (o consumível da seção 3). Motivo de fila, não de mérito: o
manto é o **primeiro eixo vertical em partida** da história do projeto, e a lição
do Spellbreak é que eixo de mobilidade se adiciona um por vez, medindo retenção,
nunca em pacote.

---

## 5. RARIDADE E ONDE APARECE

O loop de hoje: varinhas em anéis largos por toda a ilha (*"nunca ficar sem
arma"*), 1 cajado por POI (*"ir ao ponto de interesse tem que pagar"*), manopla
**só** no Baú Celestial, tudo com seed fixo — `SEED_LOOT = 1301`
(`godot/gameplay/Loot.gd`). E o Baú tem um contrato de design escrito:
telegrafado 45 s antes, pousa em campo aberto, abrir canaliza 3 s de costas para
o mapa — *"ele não é um presente, é um ÍMÃ"* (`godot/gameplay/BauCelestial.gd`).

**PROPOSTA de encaixe, com uma regra dura no meio:**

| Item | Onde nasce | Raridade | Por quê |
|---|---|---|---|
| **Lente de Foco / Reservatório** (runas) | chão, nos POIs, junto do cajado | comum/incomum | a runa é a recompensa de **ir ao POI** — o mesmo contrato que já paga o cajado; e como viaja dentro da luva, não polui o chão com categoria visível nova |
| **Consumível de escudo/cura** | chão, espalhado como a varinha | comum | ninguém pode ficar sem a única forma de recuperar escudo (`Balance.ESCUDO.regen = 0.0`) |
| **Manto Planador** (se aprovado) | **1 por partida**, em POI de altura (Ruínas / Torres Arcanas) | raro | o item que dá altura nasce onde já há altura — o jogador aprende para que serve no lugar onde acha |
| **Manopla** | Baú Celestial, e só | lendária | **não muda nada** |

**A regra dura: o Baú Celestial continua sendo manopla e NADA MAIS.** Se o baú
passar a sortear entre manopla e manto, o anúncio para o mapa inteiro deixa de
significar *"a lendária está ali"* e vira loteria — o ímã perde a força, e com
ela a razão de existir do POI mais bem desenhado do jogo. A extensão já validada
para o baú é outra: manoplas **nomeadas** com habilidade única (`docs/DIRECAO.md`
§1.2), que ficam **dentro** da mesma categoria. Item equipável e manopla nunca
disputam o mesmo espaço.

---

## 6. O QUE MUDARIA NO JOGO, EM CONCRETO

Só os aprovados. É a resposta direta ao *"o que mudaria"*:

- **Lente de Foco** — *Hoje* o jogador erra o inimigo que corre em diagonal a
  25 m e recua para encurtar a distância. *Com a runa*, ele acerta de onde está
  e o duelo passa a ser decidido por posição, não por aproximação forçada.
- **Reservatório** — *Hoje* toda briga longa tem uma pausa obrigatória: a mana
  acaba e o jogador quebra contato para regenerar. *Com a runa*, ele escolhe
  entre pressionar até o fim ou guardar — a decisão que hoje o recurso toma por
  ele.
- **Consumível de escudo/cura** — *Hoje* o escudo quebrado fica quebrado até o
  fim da partida (a não ser que haja uma Tessa ou um Brok no jogo). *Com o
  consumível*, sobreviver a uma briga vira uma **jogada** (recuar, se curar,
  voltar) em vez de uma sentença.
- **Monóculo do Arauto** (depois) — *Hoje* o jogador vê um inimigo se
  aproximando e não sabe o que vem. *Com o monóculo*, ele lê "Água" e sabe que
  vai ser molhado antes do raio — e decide engajar ou girar a cobertura.
- **Manto Planador** (depois) — *Hoje* o mapa tem uma topologia só: quem está no
  morro desce pelo morro. *Com o manto*, existe uma segunda malha de rotas — a
  travessia direta entre alturas — pagando com a impossibilidade de atirar
  durante a travessia.

---

## 7. DECISÕES DO DIRETOR

1. **Número de slots:** a proposta é **1 slot de corpo** (o Manto) + as runas
   que a luva já tem. Confirmar, ou vetar o slot de corpo e ficar só com as
   runas (que custam **zero** tela).
2. **A capa entra no MVP?** A recomendação é **não** — versão (b), depois,
   sozinha e medida no aparelho. Se ele quiser no MVP, a pergunta seguinte é a
   do gatilho: **borda (zero botão)** ou **reativável (+1 botão)** — e a
   recomendação, no celular, é borda.
3. **O item pode ser perdido ao ser derrubado?** Recomendação: **a runa NÃO
   (viaja na luva, e a luva já tem regra de troca), o Manto SIM** — cai no chão
   como loot, pelo mesmo botão PEGAR/TROCAR (`docs/DIRECAO.md` §1). Motivo:
   item de mobilidade que não se perde é vantagem acumulada; item que se perde é
   recompensa para quem derruba.
4. **O óculos vira Monóculo do Arauto (elemento do inimigo) ou está vetado?** A
   versão "vê através da parede" está reprovada neste documento — confirmar o
   veto e decidir se o Monóculo entra na fila.
5. **O consumível de escudo/cura tem prioridade sobre os itens equipáveis?**
   Recomendação: **sim** — é a lacuna do loop, já está no GDD §16.5 e custa zero
   slot.
6. **Os nomes arcanos** (Monóculo do Arauto, Manto Planador) — PROPOSTA, prontos
   para aprovar, vetar ou rebatizar.
7. **Todos os números** — multiplicadores da Lente e do Reservatório, altura
   mínima de borda, alcance do Monóculo: depois do playtest, no `Balance`. Este
   documento não propõe nenhum, por regra (GDD §19.5).
