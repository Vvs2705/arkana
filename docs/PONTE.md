# PONTE — o que o Roblox aprendeu e o GDD ainda não sabe

> **Ordem do Diretor (19/08/2026):** *"vamos continuar o projeto Roblox e com isso
> vamos salvando o que funciona para passar para a versão Android."*
>
> O mecanismo dessa frase já existe e está travado no **GDD §9**: *"Nada técnico
> migra do Roblox — o GDD é a única fonte que serve os dois produtos."* Logo, o que
> funciona no Roblox só atravessa para o Android **depois de promovido ao GDD**. O
> que não estiver no GDD não pode ser copiado — foi essa regra que segurou o gesto
> único por duas fases, até o Diretor promovê-lo ao §19.3 hoje.
>
> **O problema que este documento resolve:** ninguém sabia *quanto* o Roblox já
> tinha aprendido que o GDD não descreve. A ponte existia e estava vazia. Aqui está
> o inventário, nas duas direções.

**O que este documento é:** uma auditoria de diferenças entre `docs/GDD.md` (fonte
da verdade) e o que `roblox/src/**` de fato faz, com a evidência de cada afirmação
apontada no repositório.

**O que este documento NÃO é:** uma emenda ao GDD. Cada item da direção A traz uma
**frase pronta**; colar é ato do Diretor (GDD §19.5). Nada aqui foi aplicado.

**Condições da auditoria (leia antes de confiar nas linhas):** feita em 19/08/2026
sobre a árvore de trabalho da fase R13, com o repositório na altura do commit
`3ba7f77` (R12) e **sete arquivos abertos por outras raias no mesmo instante**
(`docs/GDD.md`, `roblox/tools/sweep.luau`, `server/Match.luau`,
`server/Sintonia.luau`, `server/Bots.luau`, `server/Tutorial.luau`,
`client/Device.luau`). Os números de linha **vão sair do lugar**; por isso toda
referência traz também o **nome do símbolo** (função, constante, campo), que é o
que sobrevive. A leitura do GDD já inclui as emendas do Diretor de 19/08 (gesto
único no §19.3, público 10+, ordem da frente Android, esclarecimento do TTK no §5).

---

## 0. Como ler — a classificação de maturidade

O valor deste documento não está na lista; está em **não confundir "existe" com
"funciona"**. Três níveis, aplicados à AFIRMAÇÃO que se quer promover, não ao
código:

| Nível | Significa | O que serve de prova |
|---|---|---|
| **PROVADO** | tem teste automatizado, medição, ou nasceu de defeito reproduzido | portão de 48 verificações (`roblox/tools/run.luau` + `scenarios.luau`), `selfTest()` de módulo, varredura de balanceamento (`roblox/tools/sweep.luau`), ou correção documentada com o defeito medido |
| **PLAUSÍVEL** | está implementado, o mecanismo é verificável, e **ninguém de fora jogou** | leitura de código + raciocínio de design. É a maioria |
| **NÃO VALIDADO** | depende das perguntas V1–V5 (`docs/ROBLOX.md` §1), que **não têm nenhuma resposta com jogador real** | nada. Por construção |

> ⚠ **O risco central desta ponte é atravessar coisa NÃO VALIDADA.** O jogo nunca
> foi tocado por terceiros — `docs/ANDROID.md` §6 e `docs/PROJETO.md` §4 registram
> isso, e o protocolo de playtest (`docs/ROBLOX.md` §11) existe justamente porque a
> sessão ainda não aconteceu. Um número que sobreviveu a 90 partidas de bot não é
> um número aprovado: a V4 já enganou este projeto uma vez exatamente assim (a
> "dominância do Fogo" de 1,42× era taxa de acerto do piloto automático; no eixo do
> `Balance`, 1,06×).

**Um mesmo item pode ter dois níveis, e isso não é indecisão.** "O teto de
atordoamento é clampado no kernel" é PROVADO (há teste que reprova sem ele). "0,8 s
é o teto certo" é NÃO VALIDADO. Onde os dois convivem, está escrito qual é qual — e
a **frase pronta promove só a parte provada**.

---

## 1. DIREÇÃO A — o que o Roblox FAZ e o GDD NÃO descreve

**21 achados** — 10 PROVADO · 8 PLAUSÍVEL · 3 NÃO VALIDADO. Ordenados por quanto
dói perder: primeiro o que, sem estar escrito, faz um pilar do GDD **não acontecer**
no próximo produto.

---

### A1 · O incêndio tem ORÇAMENTO, não chance por tique — PROVADO

**O que é.** O GDD §14 descreve a propagação do fogo como *"propaga para células
vizinhas (chance por tick)"*. Esse modelo foi implementado, medido e **reprovado
por medição**: 30% por vizinho × 16 tiques em 8 s = `1-(1-0.3)^16` ≈ **99,67%**
acumulado. Não é propagação, é carbonização garantida — o mapa inteiro queima em
100% das ignições. O modelo em produção hoje é outro: **uma rolagem por aresta**
(cada vizinho combustível recebe UMA chance, no instante em que a célula acende) +
um **orçamento de combustível** por incêndio, que é o botão real do tamanho da
queimada.

**Onde está.** `roblox/src/shared/Balance.luau:94–135` (`Balance.terrain`,
`spreadChancePerEdge = 0.5`, `fuelBudget = 16`, e `propagateChance` marcado
**OBSOLETO** na linha 116) · `roblox/src/server/Terrain.luau` (`ignite`, `newFire`,
`applyFire`).

**Evidência.** Medição registrada no `Balance.luau:98–114` e no CHANGELOG v0.2.0
(R5): modelo antigo acendia 380/380 células em 100% das rodadas; o modelo novo dá
**~44 células por tática de Fogo**. `Terrain.selfTest()` reprova carbonização — há
asserção comparando as células acesas contra `sementes + fuelBudget`
(`Terrain.luau:~1410–1420`). O portão de 48 verificações roda isso a cada fase.

**Por que dói perder.** Sem esta linha, quem implementar o GDD §14 no Android
implementa o modelo que já foi medido e reprovado — e o pilar "queimar a floresta é
uma jogada" vira "o mapa inteiro queima toda vez", que não é jogada nenhuma.

**Frase pronta — `docs/GDD.md` §14, substituindo a célula "Fogo em árvore/grama":**

```
| Fogo em árvore/grama | incendeia; propaga com UMA rolagem por aresta (cada
vizinho combustível recebe uma única chance quando a célula acende) e para ao
esgotar o ORÇAMENTO DE COMBUSTÍVEL do incêndio; após ~8s vira carvão — a cobertura
DESAPARECE | Água/Torrencial apaga; Vento espalha (arma de dois gumes) e também
consome o orçamento |
```

E, logo abaixo da tabela do §14:

```
**Regra de propagação (medida, não estimada):** "chance por tique, por vizinho" é
carbonização disfarçada — 30% × 16 tiques ≈ 99,7% acumulado, e a floresta conectada
queima inteira em 100% das ignições. O tamanho do incêndio é um ORÇAMENTO (teto de
células por ignição), não um sorteio: incêndio previsível é aprendível, e é o que
permite ao jogador PLANEJAR a queimada — que é o pilar. Para regular o incêndio,
mexa no orçamento, nunca na chance.
```

---

### A2 · Floresta abaixo do limiar de percolação não queima — o pilar §14 exige level design medido — PROVADO

**O que é.** O terreno reativo não depende só do código do fogo: depende da
**densidade da mancha combustível**. Abaixo do limiar de percolação de sítio em
vizinhança-4 (~0,593), o incêndio morre em bolsões e o pilar do GDD §14 simplesmente
não acontece — por mais correto que o código esteja. E a recíproca também é regra:
mata maciça não é cobertura, é parede, e clareira sem entrada é célula morta.

**Onde está.** `roblox/src/server/Arena.luau:38–44` (números de level design
medidos: ~330 árvores em duas manchas, densidade do núcleo 0,90) ·
`Arena.luau:125–137` (a trilha-eixo que corta a mancha em duas e devolve entrada à
clareira) · `Arena.luau:157–159` (o junco da baixada, **de propósito** em 0,42 —
abaixo do limiar, para que o incêndio infinito não pertença àquele POI) ·
`roblox/src/server/Terrain.luau` (`Terrain.selfTest`, BFS a partir do campo aberto
central, `Terrain.luau:~1340–1360`).

**Evidência.** Duas medições registradas: na primeira versão só 40 de 192 árvores
ficavam conectadas e o pilar não acontecia (LICOES 18/08); o `Terrain.selfTest`
achou e selou **107 bolsões de floresta sem saída** (STATUS R4); e o bug da clareira
inalcançável (floresta virando paredão de 380 células) foi corrigido pela trilha-eixo
(STATUS R7). A BFS roda no portão.

**Por que dói perder.** É a única regra deste projeto que diz que **o mapa é parte
do sistema de combate, e precisa ser medido antes de ser desenhado**. Vale em
qualquer engine e em qualquer dimensão — é aritmética de grafo, não de Roblox.

**Frase pronta — `docs/GDD.md` §14, novo parágrafo depois de "Por que isso importa":**

```
**Requisito de level design (não é detalhe de implementação):** a mancha combustível
precisa ficar ACIMA do limiar de percolação de sítio em vizinhança-4 (~0,593) para o
fogo atravessá-la; abaixo disso ele morre em bolsões e o pilar não acontece. E toda
área do mapa precisa ser alcançável a pé — mata maciça é parede, não cobertura, e
clareira sem entrada é célula morta. Ambas as coisas se MEDEM no mapa (varredura de
conectividade) antes de o mapa ser aprovado, nunca se estimam no olho.
```

---

### A3 · O teto de atordoamento mora no kernel, não no chamador — PROVADO

**O que é.** O GDD §9 promete "atordoa" em dois combos (Eletrocussão e Cristais
Carregados) e não diz por quanto tempo, nem quem garante o limite. O Roblox tem uma
**tabela única de status** e um **produto único de velocidade** (postura × terreno ×
status), com o teto de atordoamento **clampado dentro do kernel**: nenhum chamador
consegue passar disso, nem por engano nem por bug futuro. E existe um **piso de
lentidão** — lentidão perto de zero é atordoamento disfarçado, e escaparia do teto
por outra porta.

**Onde está.** `roblox/src/server/Combat.luau:280–307` (`STUN_MAX = 0.8`,
`REVEAL_MAX = 2.0`, `SLOW_MAX = 3.0`, `SLOW_FLOOR = 0.35`, `STATUS_MAX`) ·
`Combat.applyStatus` / `Combat.statusSpeedMult`.

**Evidência.** `Combat.selfTest()` (`Combat.luau:~1408–1429`) reprova: pedir 5 s de
atordoamento tem de devolver o teto; duração negativa não vira status; lentidão não
pode chegar a zero. O defeito de origem é medido e está anotado: qualquer efeito
escrito direto em `Humanoid.WalkSpeed` durava um frame — os combos **prometiam
atordoar e não atordoavam** (STATUS R6).

**Nível duplo.** *"O teto tem de morar no kernel"* é PROVADO. *"0,8 s é o teto
certo"* é **NÃO VALIDADO** (a justificativa é a auditoria externa, contra um TTK de
~3 s, sem jogador humano). E há um teto conhecido anotado no próprio código: o clamp
é por aplicação, então dois combos de duplas diferentes em sequência encadeariam
~1,6 s.

**Frase pronta — `docs/GDD.md` §4, novo item 8 nas regras globais:**

```
8. **Controle de multidão tem TETO, e o teto mora no sistema.** Todo efeito de
   atordoamento/lentidão/revelação passa por uma tabela única de status com limite
   máximo aplicado DENTRO do sistema, nunca no lado de quem chama: teto que depende
   do chamador não é teto. Lentidão tem PISO de velocidade — lentidão perto de zero
   é atordoamento entrando por outra porta. Referência de calibração inicial:
   atordoamento ≤ 0,8s, revelação ≤ 2,0s, lentidão ≤ 3,0s com piso de 0,35× —
   números a confirmar no playtest, a REGRA não.
```

---

### A4 · Fogo amigo desligado no projétil direto; o terreno continua pegando todo mundo — PLAUSÍVEL

**O que é.** O GDD nunca decidiu fogo amigo. O que ele tem são indícios contrários
(§9, Corvomante: *"a onda também atinge aliados"*; §14: o fogo como arma de dois
gumes). O Roblox tomou a decisão e ela é **assimétrica de propósito**: projétil
direto **não** acerta aliado; **terreno acerta todo mundo**. A razão é medida no
produto, não no gosto — a hipótese que o alpha testa é "dois desconhecidos
cooperam", e com fogo amigo ligado um estranho arruína a sessão do outro em dois
segundos, deixando a V1 ilegível (não dá para separar "a Sintonia não é divertida"
de "meu parceiro me matou").

**Onde está.** `roblox/src/server/Combat.luau:1080–1099` (o filtro em `findHit`, com
a justificativa inteira escrita ali) · `roblox/src/server/Teams.luau:56`
(`Teams.areAllies` é a única fonte de "quem é aliado").

**Evidência.** Decisão registrada com razão (CHANGELOG R8/R9, STATUS R8). O
mecanismo é verificável por leitura e `Teams.selfTest`/`Sintonia.selfTest` cobrem a
noção de aliado. **Nenhum jogador jogou com ela.**

**Por que dói perder.** É a regra que separa "cooperação entre desconhecidos" de
"griefing grátis" — e o Android herda exatamente o mesmo problema no Degrau 3, que é
multiplayer com pareamento automático.

**Frase pronta — `docs/GDD.md` §4, novo item 9:**

```
9. **Fogo amigo é assimétrico:** magia MIRADA (projétil direto) não atinge o
   parceiro; AMBIENTE (fogo no chão, gás, eletrocussão da água, lamaçal) atinge
   todo mundo. Mirar em alguém é ato; o ambiente é consequência — e é o ambiente
   que o §14 define como arma de dois gumes. Sem essa assimetria, um desconhecido
   pareado arruína a partida do outro em segundos, e a Sintonia fica impossível de
   avaliar.
```

---

### A5 · Cooldown compartilhado: cobra no início, devolve a quem continua de pé — PROVADO (mecanismo) / PLAUSÍVEL (design)

**O que é.** O GDD §9 lista como limitador: *"Consome a magia dos DOIS conjuradores
+ cooldown compartilhado longo"*. Ele não diz **quando** o custo é cobrado, e essa
lacuna é explorável. No Roblox o custo é cobrado **no início da canalização** (é o
que impede spam), e a canalização só é cancelável **morrendo ou saindo** — o que
significava que bastava o parceiro morrer de propósito para tirar 24 s do pilar do
jogo do outro. A correção: a interrupção **devolve o cooldown a quem continua vivo**;
quem caiu ou saiu segue pagando. O limitador do §9 fica inteiro, porque **o combo que
DISPARA continua cobrando os dois**.

**Onde está.** `roblox/src/server/Sintonia.luau:455–465` (cobrança no início) ·
`Sintonia.luau:518–546` (`failChannel`, o reembolso e a justificativa completa) ·
`roblox/src/shared/Balance.luau:161–179` (`sharedCooldown`, `channel`, `window`,
`dmgMult`).

**Evidência.** `Sintonia.selfTest()` (`Sintonia.luau:~1195–1222`) cobra as duas
metades em asserções separadas: *"quem continuou DE PÉ não paga a canalização que o
parceiro não terminou"* e *"combo DISPARADO cobra os DOIS"*. Nasceu de griefing
identificado ao preparar o alpha público (R9).

**Frase pronta — `docs/GDD.md` §9, nos "Limitadores":**

```
- O custo (mana dos dois + cooldown compartilhado) é cobrado NO INÍCIO da
  canalização, e é aí que mora o anti-spam. Interrupção por morte/saída **devolve o
  custo a quem continua de pé** — quem caiu segue pagando. Sem essa devolução, um
  parceiro hostil (ou que só deslogou) tira o pilar do jogo do outro por um cooldown
  inteiro com um ato unilateral. O combo que efetivamente DISPARA continua cobrando
  os dois: é ele que impede o spam, não a punição de quem foi abandonado.
```

---

### A6 · A partida acaba quando resta UMA DUPLA, não um jogador — PLAUSÍVEL

**O que é.** O GDD descreve o jogo em squad e nunca define a condição de vitória.
Com a contagem ingênua (um sobrevivente), a última dupla viva **ficava esperando a
zona matar um dos dois para vencer** — o vencedor tinha de perder o parceiro para
ganhar, o oposto de um jogo sobre cooperar.

**Onde está.** `roblox/src/server/Match.luau:919–931` (`matchOver`, com a
justificativa) · o fallback para contagem de jogadores quando não há times montados
(solo/lobby) está na mesma função.

**Evidência.** Corrigido como costura de coordenação na R6; a FASE 3 do portão
(`roblox/tools/run.luau`) roda uma partida completa e cobra que **a partida acaba**.
Não há jogador humano na medição.

**Frase pronta — `docs/GDD.md` §19.2, ou como nota nova em §9:**

```
- **Condição de vitória em modo de esquadrão: a partida acaba quando resta UM
  ESQUADRÃO, não um jogador.** Contar cabeças em vez de times faz o último time vivo
  precisar perder um parceiro para vencer — exatamente o contrário do que o jogo pede.
  O Selo do Campeão é do ESQUADRÃO vencedor.
```

---

### A7 · Muro de pedra não sobe em célula ocupada — PROVADO

**O que é.** O GDD §14 define o muro de Terra como *"cobertura destrutível
(~200hp)"*. Sem uma regra de ocupação, ele também é um botão de deletar alguém: dava
para enterrar o parceiro dentro de 10 studs de pedra por 20 s com uma tática. A
regra é **uniforme** (vale para aliado e inimigo), porque o documento define o muro
como cobertura — não como prisão.

**Onde está.** `roblox/src/server/Terrain.luau:1015–1040` (`applyEarth` /
`raiseWall`, com consulta espacial preguiçosa — só sai quando um muro ia mesmo
subir).

**Evidência.** `Terrain.selfTest()` cobra **as duas metades**: a célula de quem está
de pé fica livre **e** as outras viram muro (a proteção não pode custar poder). A
consulta preguiçosa foi medida: o portão segue em ~1,5 s e a arena nos mesmos 2676
parts.

**Frase pronta — `docs/GDD.md` §14, na linha do muro de pedra:**

```
O muro NÃO nasce na célula de quem está de pé (regra uniforme, aliado ou inimigo):
o muro é cobertura destrutível, não um botão de emparedar alguém. As outras células
do disco sobem normalmente — a proteção não pode custar o poder da magia.
```

---

### A8 · Retorno contestável: 1× por dupla, 4 altares FIXOS, sair zera, proibido na fase final — PLAUSÍVEL

**O que é.** O GDD §16.6 descreve o Círculo de Invocação (essência → ponto fixo →
ritual de 7 s → o aliado volta só com a varinha comum) e o §18.6 descreve o Espírito
Errante. O Roblox implementou os dois **fundidos num sistema só** e acrescentou
quatro limitadores que o GDD não tem: **uma volta por DUPLA** por partida, **sair do
raio ZERA a canalização** (não pausa), **quatro altares em anel fixo** e **nada de
retorno na fase final da zona**.

**Onde está.** `roblox/src/shared/Balance.luau:205–224` (`Balance.match.revive`) ·
`roblox/src/server/Match.luau:96–116` (leitura com padrão),
`Match.luau:677–679` (`reviveAllowed`), `Match.luau:719–732` (`makeSpirit`).

**Evidência.** `Match.selfTest()` cobra `reviveAllowed`. Os números são calibração
declarada, sem jogador. **Divergência não registrada:** o GDD diz 7 s, o código usa
**6,0 s** (ver B1.4).

**Frase pronta — `docs/GDD.md` §16.6, no fim do parágrafo:**

```
Limitadores do ritual (o que o transforma em disputa e não em menu de espera): UMA
volta por esquadrão por partida · pontos FIXOS e conhecidos (o inimigo sabe onde
campear) · sair do raio ZERA a canalização, não pausa · e nenhum retorno depois que
a zona entra na fase final — perto do fim, morte é morte.
```

---

### A9 · Quem caiu está fora da CONTAGEM, não da partida — e há três estados de "não jogando" — PLAUSÍVEL

**O que é.** Num servidor público, **entrar no meio da partida é a experiência da
maioria** — e ela estava muda: o jogador nascia espectador a 120 studs do chão sem
uma palavra na tela. Hoje há três estados distintos, com telas diferentes:
**jogando** · **espírito** (fora da contagem, não da partida — ainda volta pelo
altar, e nunca cai no painel de poleiro) · **espectador**. E quem chegou agora lê
"você entra na próxima" em vez de "você está fora". Sem relógio de próxima partida,
**de propósito**: o teto de duração é teto, não previsão, e erraria por minutos — a
tela mostra fase da zona e times restantes, que são exatos.

**Onde está.** `roblox/src/server/Match.luau:280–290` (`spectateKind`, e o motivo de
`eliminated` ter deixado de ser booleano para virar MOTIVO: `"late"` / `"out"`) ·
`roblox/src/client/MatchUi.luau:118` (o cliente lê o porquê).

**Evidência.** `Match.selfTest()` cobra `spectateKind("late", false) == "late"` —
*"quem entrou no meio da prova assiste ESTA e joga a próxima"*. Nenhum jogador
humano validou a tela.

**Frase pronta — `docs/GDD.md` §11 (fluxo) ou §19.2:**

```
- **Quem não está jogando tem três estados, e a tela diz qual:** *jogando* ·
  *caído mas recuperável* (fora da CONTAGEM, não da partida — ainda volta pelo
  ritual) · *espectador*. Quem entra com a partida em andamento lê "você entra na
  próxima", nunca "você está fora". **Não se mostra relógio de próxima partida**
  quando o número disponível é um TETO e não uma previsão: mostre o que é exato
  (fase da zona, times restantes) e cale sobre o que erraria por minutos.
```

---

### A10 · Mudo liga as legendas — nenhuma informação existe só no som — PROVADO

**O que é.** O GDD §10 tem a regra de daltonismo (cor + ícone + forma, nunca só
cor). Não tem a irmã dela: **mudo não pode custar informação**. Avisos que o jogo dá
só por som ("Sintonia canalizando", "fora da zona") sumiam para quem zerava o
volume. Hoje zerar o volume **liga** a faixa de legendas — ligar, não travar:
desligar depois continua sendo escolha do jogador, e aí é escolha informada.

**Onde está.** `roblox/src/client/Accessibility.luau:578–585` (o gancho, com a
justificativa) · `Accessibility.luau:110–132` (a loja única de preferências, incluindo
`captions`) · `A11y.caption` em `Accessibility.luau:463`.

**Evidência.** `A11y.selfTest()` (`Accessibility.luau:~701–714`) reprova:
*"mudo deixou o jogador sem legendas nem som"*. Roda no portão.

**Frase pronta — `docs/GDD.md` §10, junto da regra de daltonismo:**

```
- Regra irmã (som): **nenhum aviso pode existir só no áudio.** Todo alerta com
  consequência mecânica tem uma faixa de legenda equivalente, e zerar o volume LIGA
  a faixa automaticamente (ligar, não travar — desligar depois é escolha informada
  do jogador). Silêncio é uma condição de uso comum em celular, não uma exceção.
```

---

### A11 · A coluna da mira é território proibido — e o corte de HUD se MEDE antes — PROVADO (medição) / PLAUSÍVEL (escolha)

**O que é.** A queixa do Diretor foi "informação demais na tela". A raia **mediu
antes de cortar**: **65 004 px² permanentes = 8,9% de um celular em paisagem** e,
pior que a área, **72 px de mobília girando na coluna da mira** (em terceira pessoa
o jogador aponta no centro). O que saiu, saiu com argumento: o Selo de Arkana tinha
duas informações e as duas eram **100% duplicadas** — "combo disponível" já era dito
por uma pílula com **palavra + losango** (canal mais acessível que gema colorida), e
"meu elemento" pelo slot do carrossel, permanente em cor + forma, onde o dedo já
está. Nome do parceiro e fase da zona viraram sob demanda (o nome volta **no instante
em que o parceiro cai**, que é a única hora em que decide algo). Resultado medido:
**55 140 px², 15% menos**, coluna da mira zerada. E a densidade virou **preferência**
(`hudDensity`: essencial padrão × completo), porque a queixa foi de excesso — então
o excesso é que vira opt-in.

**Onde está.** `roblox/src/client/Accessibility.luau:119–125` (`hudDensity`, string e
não booleano de propósito) · `roblox/src/client/Hud.luau:23`, `259–263`, `891` ·
`roblox/src/client/MatchUi.luau:139–142`, `504`.

**Evidência.** A medição está no CHANGELOG v0.5.0 e no STATUS R11. `Hud.selfTest()`
cobra o padrão de fábrica (`hudDensity` = essencial) e que nada ancorado no topo
invade a metade de baixo (`Hud.luau:~1141`, `~1166`). **Pendência conhecida e não
resolvida:** o painel de espectador sobrepõe o Selo em 44 px hoje em produção.

**Frase pronta — `docs/GDD.md` §19.2 (guardrails mobile):**

```
- **Orçamento de HUD, medido em px²:** todo elemento permanente da tela tem custo, e
  o custo se MEDE (área permanente / área da tela em paisagem) antes de se discutir
  o corte. Duas regras duras: (a) a coluna central da mira não recebe mobília
  permanente — nada que gire, pulse ou mude ali; (b) informação que não muda durante
  um tiroteio (nome do parceiro, fase da partida) é sob demanda, e aparece no
  instante em que passa a decidir algo. Densidade de HUD é preferência do jogador,
  com o modo ESSENCIAL como padrão de fábrica.
```

---

### A12 · O onboarding é um CONVITE, e o lobby nunca espera por ele — PROVADO

**O que é.** O tutorial era pior que obrigatório-com-botão-de-pular: era empurrado
em todo corpo novo **e o lobby segurava a partida por até 240 s** enquanto alguém
treinava — quem nem escolheu o tutorial esperava por quem escolheu. Virou convite com
**duas saídas do mesmo peso** (TREINAR / JOGAR AGORA); a sessão só abre com aceite; e
recusar **não é porta de mão única** — o botão TREINAR volta no lobby, no mesmo slot
do PULAR, sem mobília nova. O teto de espera **deixou de existir** (o conserto não era
mexer no número, era não ter espera) e uma asserção trava a volta dele.

**Onde está.** `roblox/src/server/Tutorial.luau:30–49` (a decisão escrita),
`Tutorial.luau:243–261` (`offerable` / `reopenable`), `Tutorial.luau:947`
(`Tutorial.offer`).

**Evidência.** O cenário S1 (`roblox/tools/scenarios.luau`) cobra quatro coisas:
ninguém entra sem aceitar · o aceite chega pelo payload REAL do cliente · o lobby não
espera · quem treinava sai limpo. **Provado com dois defeitos reintroduzidos** (R11).

**Frase pronta — `docs/GDD.md` §11, no fluxo de menus, ou §19.6:**

```
- **Onboarding é convite, nunca pedágio.** Duas saídas do mesmo peso visual, e a
  recusa não é porta de mão única: a porta de entrada volta no mesmo lugar, sem
  ocupar espaço novo. **Nenhuma partida espera pelo treino de ninguém** — fazer o
  lobby segurar transforma o tutorial de um jogador em atraso de todos os outros,
  que é o custo mais caro que um treino opcional consegue cobrar.
```

---

### A13 · A grama alta esconde com número: alcance de detecção cai de 55 para 16 studs — PLAUSÍVEL

**O que é.** O GDD §14 diz que *"fogo em grama alta queima e REVELA quem estava
escondido"* — mas nunca diz **como** a grama alta esconde. No Roblox isso é um
número: quem está numa célula de grama alta em estado normal só é detectado a 16
studs, contra 55 em campo aberto. É o que dá sentido mecânico à contra-jogada do
fogo, e é o que faz "incendiar a grama" ser jogada e não vandalismo.

**Onde está.** `roblox/src/shared/Balance.luau:181–195` (`Balance.bots.sightRadius =
55`, `sightConcealed = 16`) · `roblox/src/server/Bots.luau:242–243` (a leitura da
célula) · `roblox/src/server/Terrain.luau:1204`, `1625–1636` (a grama queima e a
ocultação some).

**Evidência.** Mecanismo verificável por leitura; `Terrain.selfTest` cobre que a
grama alta acende de verdade. **É a regra de detecção dos BOTS** — não existe fog of
war entre humanos, então isso ainda não é um sistema de furtividade completo.

**Frase pronta — `docs/GDD.md` §14, na linha da grama alta:**

```
| Grama alta (estado normal) | REDUZ o alcance em que o ocupante é detectado (ordem
de grandeza: cerca de um terço do alcance em campo aberto) | Fogo queima a moita e a
ocultação some junto com ela — é isso que faz "incendiar a grama" ser jogada |
```

---

### A14 · O ritmo da partida tem cronograma, e o teto de duração DERIVA dele — PROVADO

**O que é.** O GDD não descreve nenhuma regra de zona: só diz "20 jogadores" e
"partidas de 4–6 min" (§19.2). O Roblox tem um cronograma completo — **5 fases**,
cada uma com tempo parado + tempo fechando, com **dano por segundo crescente por
fase** — e três variantes de ritmo (blitz 275 s · media 330 s · longa 435 s)
selecionáveis num campo só. E tem a regra que nasceu de um travamento medido: a fase
final **não fecha até zero, ela para e segura**, o que deixava a máquina de estados
em `Playing` **para sempre** com jogadores parados dentro. O teto de duração existe e
é **DERIVADO do cronograma** (fator + folga), não um número grande escolhido a dedo —
por isso acompanha a variante de ritmo em vez de brigar com ela.

**Onde está.** `roblox/src/shared/Balance.luau:226–237` (`Balance.match.zone`,
`phases = 5`, `dpsPerPhase = { 2, 4, 7, 12, 20 }`), `Balance.luau:145–159`
(`zoneVariant` e a tabela de variantes) · `roblox/src/server/Match.luau:71–82`
(`overtimeFactor`, `overtimeSlack`, com a medição do travamento) ·
`Match.luau:404–419` (`matchTimeLimit`, `mustEndNow`).

**Evidência.** O travamento é medido: **900 s simulados, 4 jogadores parados no
centro, ainda `Playing`** e nenhuma partida nova naquele servidor (R8). O cenário S5
separa "tem um número grande" de "o teto ACOMPANHA a variante de ritmo" — medido:
blitz 416 s/275 s · media 491 s/330 s · longa 632 s/435 s. `Match.selfTest()` cobra
que o teto não corta partida ainda dentro do cronograma.

**Frase pronta — `docs/GDD.md` §19.2:**

```
- **Ritmo de partida é dado, não código:** número de fases, tempo parado, tempo
  fechando e dano por fase moram num arquivo de balanceamento, e o ritmo inteiro é
  uma variante trocável num campo só (curta / média / longa) — comparar ritmos é uma
  linha, não uma refatoração.
- **Toda partida tem teto DURO de duração, e o teto DERIVA do cronograma** (fator ×
  cronograma + folga), nunca é um número grande escolhido à mão. A fase final da
  zona para e segura; sem teto, jogadores parados deixam o servidor preso em partida
  para sempre — medido. Teto que não acompanha a variante de ritmo corta partida
  legítima ou não corta nenhuma.
```

---

### A15 · Cada um dos 10 combos tem raio e duração — PLAUSÍVEL

**O que é.** A matriz de combos do GDD §9 tem nome e efeito em texto; não tem número.
O Roblox tem os 10 pares implementados com **raio de área e duração próprios** por
combo (de 10 studs / 0,6 s na Explosão de Plasma a 24 studs / 6 s na Tempestade
Torrencial), mais a chave comutativa que garante que a ordem dos conjuradores não
importa, e a regra de que **elemento igual não funde**.

**Onde está.** `roblox/src/shared/Elements.luau:95–210` (`Elements.Combos`,
`Elements.getCombo`).

**Evidência.** `Sintonia.selfTest()` cobra a coerência dos limitadores do §9
(`dmgMult > 1`, `sharedCooldown > window`) e que os combos cumprem a tabela de
efeitos (R6). **Nenhum número foi jogado por gente** — a proporção entre "burst
pontual" e "negação de área" é hipótese de design.

**Frase pronta — `docs/GDD.md` §9, nota abaixo da matriz de combos:**

```
Cada combo carrega, além do nome e do efeito, um RAIO e uma DURAÇÃO próprios — é o
par (raio, duração) que separa um burst pontual de uma negação de área, e é por ele
que se rebalanceia um combo sem mexer no dano. O par é comutativo: a ordem em que os
dois magos conjuram não muda o resultado. Elementos iguais não fundem.
```

---

### A16 · Preferências de conforto atravessam a saída, e o cofre guarda só o TIPO — PROVADO

**O que é.** As 11 opções de conforto viviam em atributos de sessão: quem baixava o
volume levava o susto de novo a cada entrada — o atrito que faz a pessoa não voltar,
que é literalmente a pergunta V5. Hoje elas persistem, com duas regras de arquitetura
que valem para qualquer engine: **o cofre guarda só o TIPO de cada chave, nunca o
valor padrão** (assim os padrões continuam num lugar só e não há dois números para
divergir), e **zero escritas durante a partida** (uma por sessão, na saída, mais o
fechamento do servidor).

**Onde está.** `roblox/src/server/Prefs.luau:1–50` (a decisão inteira escrita no
cabeçalho, incluindo o teto de chaves que rejeita — não trunca — payload grande) ·
`roblox/src/client/Accessibility.luau:110–132` (a loja única).

**Evidência.** `Prefs.selfTest()` e `A11y.selfTest()` rodam no portão; o segundo
inclui uma sentinela que pega o módulo de áudio voltando a ser segundo dono da
preferência de volume.

**Frase pronta — `docs/GDD.md` §12, no item "Persistência":**

```
- **Persistência de preferências:** o cofre guarda o valor e o TIPO de cada chave,
  nunca uma cópia dos padrões — padrão duplicado é padrão que diverge. Lista de
  chaves FECHADA (chave livre vinda do cliente é armazenamento grátis pago pelo
  estúdio), payload fora do tamanho é REJEITADO inteiro e nunca truncado, e a escrita
  acontece uma vez por sessão, na saída — nunca a cada mexida de slider.
```

---

### A17 · Dado de bot acha assimetria grosseira; não decreta balanceamento — PROVADO

**O que é.** É a lição mais cara já paga por este projeto e ela **não está em
documento de design nenhum**. A varredura reportou "dominância do Fogo em 1,42×,
estoura a régua de 1,25×". A investigação (90 partidas, 3 bases de semente
independentes) mostrou que aquilo era **taxa de acerto do piloto automático**, não
design: no eixo do `Balance`, Fogo dá 1,06×. Um nerf teria sido aplicado a um jogo
que não tinha o problema. Daí saíram três regras: o relatório separa
`human_only` de `all_entities` e **só a primeira serve para balancear**; toda
telemetria carimba a fonte **real** do dano (o elemento do PROJÉTIL, não o que o
autor tem na mão agora); e duas varreduras do mesmo build com bases de semente
diferentes são amostras independentes — é assim que se separa sinal de ruído antes de
recomendar qualquer ajuste.

**Onde está.** `roblox/tools/sweep.luau:11–13` (a honestidade declarada no cabeçalho:
*"esta varredura acha ASSIMETRIA GROSSEIRA e REGRESSÃO entre builds; ela NÃO decreta
balanceamento"*), `sweep.luau:18–21` (deslocamento de base de sementes) ·
`roblox/src/server/Combat.luau:855–865` (o carimbo do elemento da FONTE, com a
medição: 1,34× pelo evento contra 1,64× pela verdade) · `docs/ROBLOX.md` §11.6 (a
régua e o piso de amostra).

**Evidência.** 90 partidas, 3 bases de semente (STATUS R8). O defeito de atribuição
foi corrigido e está coberto por asserção (`Terrain.selfTest`: *"o elemento do perigo
é o do DISPARO, não o que o autor tem na mão agora"*).

**Ressalva honesta.** A própria régua de dominância (`≤ 1,25×` de dano ajustado por
uso) **é contestada dentro do projeto**: ela não é adimensional numa economia de mana
— divide por conjurações e pune quem custa barato. A leitura mais honesta seria
dano/mana e DPS sustentado (LICOES 18/08). Promover a régua sem essa ressalva seria
promover meia lição.

**Frase pronta — `docs/GDD.md` §19.5 (ordem de execução / regras de processo):**

```
- **Regra de medição (vale para os dois produtos):** dado gerado por bot serve para
  achar assimetria grosseira e REGRESSÃO entre versões; ele **não decreta
  balanceamento**. Todo relatório separa "sujeito humano" de "humanos + bots", e só a
  primeira coluna justifica mexer num número. Todo evento carimba a fonte REAL do
  efeito (o elemento do projétil que acertou, não o que o autor tem na mão no
  momento). E antes de recomendar qualquer ajuste, rode a mesma versão com DUAS
  sementes independentes: uma diferença que não sobrevive à troca de semente é ruído.
```

---

### A18 · Grimório: 12 páginas, e nenhuma delas dá vantagem — PLAUSÍVEL

**O que é.** O GDD §18.3 descreve o Grimório de Descobertas como conceito e o §18
prioriza para v0.3. O Roblox tem a versão Lite com **12 páginas de ordem canônica**,
autoridade 100% no servidor (nenhum remote aceita "eu descobri X"), e uma propriedade
que o GDD não declara: **nenhuma página dá dano, mana, escudo ou qualquer vantagem** —
o Juramento anti-P2W (§19.1) por construção, não por promessa. Descobrir é o prêmio.

**Onde está.** `roblox/src/server/Grimoire.luau:1–28` (a decisão) ·
`Grimoire.luau:48–66` (`Grimoire.ORDER`, as 12 páginas: seis de terreno, seis de
dupla e desfecho).

**Evidência.** `Grimoire.selfTest()` no portão. Toda descoberta emite evento próprio,
o que permite medir se a página acendeu **sozinha** ou logo depois de um tutorial
mandar — é a melhor proxy honesta de "descobriu a química sozinho" (V3).

**Frase pronta — `docs/GDD.md` §18.3, no fim:**

```
Regra dura do Grimório: **nenhuma página concede vantagem** (dano, recurso,
resistência, cosmético com atributo). O prêmio é a descoberta. É o Juramento §19.1
cumprido por construção — um sistema de progressão que não TEM como dar poder não
precisa ser vigiado para não dar. E cada descoberta emite telemetria, para se saber
se o jogador achou sozinho ou foi instruído.
```

---

### A19–A21 · O que está implementado e é **NÃO VALIDADO** por definição

Estes três não têm frase pronta e **não devem atravessar como números**. Eles
atravessam, se atravessarem, como *regra sobre o número*, nunca como o número.

| # | O que é | Onde | Por que NÃO VALIDADO |
|---|---|---|---|
| **A19** | Números da Sintonia: janela 1,5 s · canalização 1,0 s · cooldown compartilhado 24 s · ×2,35 · raio de alvo 14 studs | `shared/Balance.luau:161–179` | Existe um **experimento A/B em aberto** (`Balance.experiment.sintoniaVariant`) exatamente porque ninguém sabe qual configuração funciona. A variante B (0,75 / 18 / ×2,0) é a hipótese "aumenta uso intencional sem virar spam". **Escolher uma antes da sessão é decidir sem dado** |
| **A20** | Números de combate: dano/mana/cadência dos 5 elementos, escudo (50/75/100/125 e limiares 0/150/400/900) | `shared/Balance.luau:59–91` | Toda a medição de TTK é de piloto automático. O GDD §5 foi esclarecido em 19/08 (as duas faixas são a mesma régua vista de alvos diferentes) e os 5 elementos estão dentro das duas leituras — **medido com bot**. E o nível 4 nunca foi alcançado na varredura (p50 211 · p90 633 · máx 712, com 0% chegando a 900): ver B2.7 |
| **A21** | Hipóteses de calibração declaradas: cone de assistência 18° (era 30°) · esquiva 0,18 s de i-frame com 2,6 s de cooldown (era 0,28 / 1,6) · velocidade do mago 22 | `shared/Balance.luau:47–55`, `240–256` | Estão escritas no próprio arquivo como **HIPÓTESE DE TESTE**, não como conclusão. O gatilho de cada uma está preparado em `docs/ROBLOX.md` §11.8 esperando dado humano |

---

## 2. DIREÇÃO B — o que o GDD descreve e o Roblox NÃO faz

**19 achados: 9 decisões silenciosas · 10 dívidas declaradas.**

A separação importa. **Dívida** é trabalho não feito, e todo mundo sabe. **Decisão
silenciosa** é o documento e a prática divergindo sem ninguém ter decidido nada — e é
pior, porque o documento serve as DUAS frentes: quem implementar o Android lendo o
GDD vai implementar o que o Roblox já descartou.

---

### 2.1 · DECISÕES SILENCIOSAS (o GDD diz uma coisa, o código faz outra, ninguém registrou)

| # | O GDD diz | O Roblox faz | Onde | Gravidade |
|---|---|---|---|---|
| **B1.1** | §14: fogo *"propaga para células vizinhas (**chance por tick**)"* | Uma rolagem por aresta + orçamento de combustível. `propagateChance` está no arquivo marcado **OBSOLETO — não usar** | `shared/Balance.luau:98–117` | **ALTA.** O texto do GDD descreve o modelo que foi medido e reprovado. Quem implementar o §14 no Android hoje reimplementa a carbonização. Ver A1 |
| **B1.2** | §9 (Corvomante: *"a onda também atinge aliados"*) e §14 (fogo como arma de dois gumes) sugerem fogo amigo ligado | Projétil direto **não** atinge aliado; terreno atinge todos | `server/Combat.luau:1080–1099` | **ALTA.** É uma decisão de design de squad tomada em código, com razão de produto forte e zero registro. Ver A4 |
| **B1.3** | §9: *"Consome a magia dos DOIS conjuradores + cooldown compartilhado longo"* | Interrupção **devolve** o custo a quem continua vivo | `server/Sintonia.luau:518–546` | **ALTA.** O limitador do §9, lido ao pé da letra, é explorável por um parceiro hostil. A correção é boa; o documento continua descrevendo a versão vulnerável. Ver A5 |
| **B1.4** | §16.6: ritual de **7 s** | **6,0 s** (`Balance.match.revive.channel`) | `shared/Balance.luau:208–211` | MÉDIA. Divergência pequena, anotada no código (*"o GDD diz 7 s; a auditoria pediu ~6"*) e nunca levada ao documento. É exatamente o tipo de diferença que se propaga como "número certo" para a outra frente |
| **B1.5** | §18.6: *"ao morrer em squad, vira um espírito **por 60s**"* | O espírito **não tem prazo**: dura até ser resgatado, até a dupla gastar sua única volta, ou até a zona entrar na fase final | `server/Match.luau:677–679`, `719–732` | MÉDIA-ALTA. A regra de expiração foi **substituída** por uma regra de consumo + fase. É um desenho melhor (cria disputa em vez de relógio), e ninguém registrou a troca |
| **B1.6** | §18.6: *"o **vencedor** carimba o Selo de Arkana"*, no singular | Com vitória por dupla, o desfecho normal deixa **dois** vivos, e o Selo sai do **melhor sobrevivente** (abates, depois dano) | `server/Match.luau:975–990` | MÉDIA. O §18.6 nunca previu vitória de esquadrão. O código escolheu um dos dois vencedores — decisão de produto tomada por necessidade de implementação. Ver A6 |
| **B1.7** | §5: *"Dano em escudos de aliados **sendo revividos** não conta para evolução (anti-farm)"* — recorte estreito | Dano em **qualquer** aliado nunca evolui escudo | `server/Combat.luau:950–957` | MÉDIA. O código é mais duro que o documento, e por razão medida: sem isso uma dupla subia os dois escudos ao nível 4 atirando um no outro num canto. A regra do GDD, como escrita, **deixa o exploit aberto** |
| **B1.8** | §9: o jogador solo ganha a **Runa de Eco** (item raro, auto-combina 1× por partida) | Runa de Eco não existe. O solo é resolvido pareando com **parceiro BOT**, que entra na canalização real | `server/Sintonia.luau` (identificador sintético negativo) · `server/Teams.luau:33–46` | MÉDIA. Mesmo problema, solução diferente, nenhuma das duas registrada como escolhida. Consequência operacional já documentada: número **ímpar** de humanos entrega o último a um bot, e **essa dupla não responde V1** (`docs/ROBLOX.md` §11.2) |
| **B1.9** | §19.3: *"**Dois esquemas:** Simples (conjuração assistida leve) e Avançado (mira 100% manual) — ambos gratuitos, **competitivo pareia por esquema**"* | Os dois esquemas existem no código, **sem nenhuma tela para trocar** (*"Sem tela de opções ainda"*), e a assistência é aplicada **100% no cliente**, antes do envio ao servidor — **o servidor nunca sabe qual esquema o jogador usou** | `client/Controls.luau:80–82`, `161–168`, `209`, `652–654`; nenhum arquivo de `server/` menciona assistência de mira | **A MAIS PERIGOSA.** Três consequências: o jogador não consegue escolher · "pareia por esquema" é **inverificável por construção** · e a telemetria de V2 (TTK com humanos mirando) **não sabe separar mira assistida de mira manual**, o que contamina justamente a leitura que a sessão de playtest existe para produzir |

> **B1.9 merece uma linha à parte.** As outras oito são divergências de design. Esta
> é uma divergência que **envenena a medição** — e a medição é o produto inteiro
> desta fase do projeto. Se metade da amostra jogar com assistência e a outra metade
> sem, e o relatório não souber quem é quem, o `p50` de TTK humano mistura duas
> populações. Correção mínima e barata: carimbar o esquema no evento de disparo, do
> mesmo jeito que o gesto (`drag`/`tap`/`key`) já é carimbado
> (`server/Combat.luau:722–723`) — é o precedente pronto, telemetria pura, sem remote
> novo.

**Anexo de B1 — divergência entre documentos, não entre documento e código:**
`docs/DECISOES_PENDENTES.md` está **vencido**. Ele lista cinco decisões com as linhas
de "Decisão" em branco; pela leitura do `docs/GDD.md` de hoje (19/08), **quatro já
foram tomadas e coladas** — gesto único promovido ao §19.3, emenda da frente Android
no §9, ordem Degrau 3 antes do Degrau 4, público-alvo 10+ — e a quinta (régua de TTK)
foi resolvida por esclarecimento no §5 (as duas faixas são a mesma régua vista de
alvos diferentes). Documento de decisões pendentes que lista decisões já tomadas faz a
próxima sessão trabalhar num problema resolvido.

---

### 2.2 · DÍVIDAS (o GDD descreve, o Roblox não implementou — e isso é sabido)

Nenhuma destas é decisão silenciosa: o `docs/ROBLOX.md` §5 define o escopo das fases
R0–R3 e nenhuma delas aparece lá. Ficam listadas para o Diretor saber **de quanto do
GDD o produto de validação não diz nada** — e, portanto, sobre quanto do GDD o
playtest **não vai trazer nenhuma informação**.

| # | Do GDD | Estado no Roblox | Consequência para a ponte |
|---|---|---|---|
| **B2.1** | §1–§3: 5 classes, 10 magos com passiva/tática/suprema, vantagem de classe | **Zero.** Um mago genérico, sem classe e sem identidade | O playtest não diz **nada** sobre equilíbrio de personagem. A V4 mede elementos, não magos |
| **B2.2** | §3 e §4.3: *"toda Suprema é telegrafada"*; §4.5: kamikaze balanceada | **Nenhuma suprema existe.** O código anota: *"hoje a Sintonia É a 'ultimate' do jogo"* (`client/Hud.luau:565`) | A regra "se mata rápido, avisa antes" **nunca foi exercitada** por uma suprema de verdade. A única coisa telegrafada hoje é a canalização da Sintonia |
| **B2.3** | §16.2 armas arcanas (varinha/cajado/manopla) · §16.3 runas · §16.5 mesas de transmutação | **Zero.** Nenhum loot de nenhum tipo | Toda a camada de progressão intra-partida do GDD é não testada. `docs/ROBLOX.md` R3 previa "loot básico (pergaminhos de cura/escudo)" e ele não existe |
| **B2.4** | §18.2 castelo-lobby vivo · §15 queda do castelo | **Zero.** Os jogadores nascem num anel dentro da arena | A "queda" — o momento de assinatura do gênero — nunca foi prototipada em lugar nenhum |
| **B2.5** | §18.1 folclore brasileiro · §18.4 Presságios · §18.5 trilha elemental reativa | **Zero** | O próprio §18 as prioriza para depois; sem novidade. Vale registrar que **as três "baratas" do §18 (Presságios, Selo, Grimório) só uma e meia existem** |
| **B2.6** | §16.4 química real: vento sufoca chama pequena mas ALIMENTA incêndio grande · explosão de vapor · fulgurito · hipotermia | **Nenhuma das quatro.** O vento no Roblox **sempre espalha** (e paga combustível), sem a decisão de risco dos dois sentidos | A camada "conhecimento é poder" do §16.4 é inteiramente não testada — e é ela que o §18.3 (Grimório) existe para ensinar |
| **B2.7** | §5: escudo nível 4 (dourado, 125, limiar 900) com perk | **Implementado — e inalcançável.** O perk existe (`Balance.shield.lv4TacticCooldownMult = 0.8`, aplicado em `server/Combat.luau:527` e `765`); o limiar é que nunca foi atingido: varredura deu p50 211 · p90 633 · máx **712**, com **0%** chegando a 900 | **Dívida com número.** O nível 4 é decorativo no build atual. O gatilho pós-playtest já está escrito (`docs/ROBLOX.md` §11.8, nº 1) — mas a medição que ele pede (p90 de dano HUMANO) só existe com gente. Medido com bot: o nível 4 não acontece |
| **B2.8** | §11 boot/splash/título/menus · §12 spec completa de configurações | Substituídos pelos sistemas da plataforma; existe um subconjunto de opções (11 preferências de conforto, idioma PT/EN) | Escolha coerente com o §9 (*"sistemas da plataforma, incentivados"*), mas não registrada. A **experiência "jogo oficial"** do §11 continua provada só no protótipo 2D |
| **B2.9** | §10: tipografia Cinzel / Chakra Petch | Fontes da plataforma. Upload das fontes é ato do Diretor | Limitação declarada desde a v0.2.0. A **paleta** e a regra cor+forma, essas sim, estão implementadas (`shared/Elements.luau:31–95`) |
| **B2.10** | §19.3: *"layout dos botões **editável e escalável** pelo jogador"* | Existe **escala** (`uiSize`: 0,9 / 1 / 1,15 / 1,3) e espelho canhoto/destro; **não existe layout editável** | O protótipo 2D **tem** o editor de layout (v0.1.1); o Roblox não. Dívida real, e vale notar que a metade implementada (escala + canhoto) cobre a maior parte da queixa ergonômica |

---

## 3. Armadilhas — as que atravessam × as que são só do Roblox

Armadilha é conhecimento negativo: custou defeito para ser aprendida e não aparece em
nenhuma feature. Ela também atravessa a ponte — a diferença é que a maioria pertence
ao **§19.5** (processo), não ao design.

### 3.1 · Atravessam (valem em qualquer engine, em qualquer linguagem)

| # | Armadilha | Origem verificável |
|---|---|---|
| **T1** | **Clima visual se faz com COR (grading), nunca com falta de luz.** Empilhar quatro fontes de escuridão para fazer "entardecer" produz um jogo que o Diretor reprova por não enxergar. O conserto foi cor + sol mais alto + neblina começando FORA da arena | LICOES 18/08 · STATUS R7 |
| **T2** | **`NaN` passa por `amount <= 0`.** `NaN` falha TODA comparação, então `NaN <= 0` é falso e o dano passa direto; depois disso a vida vira `NaN` e o alvo **nunca mais morre** — a barra some da HUD e a partida não termina. A guarda certa é `not (amount > 0)`. E `NaN` entra por caminhos comuns: `tonumber` de um campo lido de outro sistema | `server/Combat.luau:866–871`, teste em `Combat.luau:~1502–1507` |
| **T3** | **Teto que depende do chamador não é teto.** O limite de atordoamento tem de ser clampado dentro do sistema que aplica status; qualquer chamador futuro, por engano ou por bug, passa por cima de um limite mantido do lado de fora | `server/Combat.luau:288–298` (A3) |
| **T4** | **Chance por tique é carbonização disfarçada.** `1-(1-p)^n` cresce rápido; 30% por vizinho em 16 tiques dá 99,67%. Sempre calcule a probabilidade ACUMULADA antes de chamar um número de "chance de propagação" | `shared/Balance.luau:98–103` (A1) |
| **T5** | **Defeito que só aparece num aparelho tem causa que só existe naquele aparelho — procure a variável que MUDA com o aparelho.** O carrossel de elementos do 2D falhava 100% no celular e 0% no desktop: o piso de 48 dp vira raio de 48 px numa tela de 640 e de 24 px numa de 1280; com espaçamento fixo de 54 px, o ícone entregava o toque ao vizinho **só** no celular | CHANGELOG v0.6.0 · LICOES 19/08 |
| **T6** | **Evento que acontece não prova que acontece CARIMBADO — e carimbo errado é falha CALADA.** O dado sai, só sai mentindo. Ao proteger um dado por carimbo, teste os DOIS lados: quem carimba e quem lê. A primeira versão do teste de exclusão do funil passava do lado do coletor e não pegava o lado do emissor | LICOES 19/08 · STATUS R11.1 |
| **T7** | **Pendência registrada por uma fase anterior é HIPÓTESE, não fato.** O `device_tier` saía "unknown" e a pendência dizia "falta o cliente reportar" por **duas fases**; o cliente reportava desde sempre e o que faltava era handler no servidor. O dado era enviado e jogado fora | STATUS R10 · LICOES 18/08 |
| **T8** | **Um relatório que escolhe a régua mais frouxa mente com números verdadeiros.** "TTK 5/5 no alvo" saía desde a R7 contra a banda da auditoria; contra a régua do GDD, nenhum dos cinco passava. A correção certa não é escolher a régua (é decisão do dono do documento): é o relatório **mostrar as duas e denunciar a discordância** | STATUS R10 · `docs/ROBLOX.md` §11.9 |
| **T9** | **Pendência de instrumentação tem de saber a diferença entre "não medimos" e "medimos e deu zero".** A pendência do desfecho de terreno acusava "ninguém emite" e continuaria acusando depois de emitido — zero virou resposta legítima ("ninguém usou o terreno de propósito") e a pendência foi reescrita para acusar o que de fato impede a leitura | CHANGELOG v0.6.0 · LICOES 19/08 |
| **T10** | **Cenário de regressão que perde a regra TROCA DE PERGUNTA, não é apagado.** Quando o Diretor derrubou a regra "o lobby segura pelo tutorial", o teste que a cobrava não foi deletado: o defeito que ele existe para pegar (a sessão de onboarding ficar ÓRFÃ) continuava possível | STATUS R11 · LICOES 19/08 |
| **T11** | **Teste que nunca ficou vermelho não é cobertura — é decoração.** Todo cenário novo se valida **reintroduzindo o defeito**: 10 mutações, 10 vermelhos, e sempre só o cenário-alvo | STATUS R9 · `roblox/tools/scenarios.luau` |
| **T12** | **Meça o level design antes de codar em cima dele.** Densidade de mancha combustível, conectividade a pé, bolsões sem saída — tudo isso é aritmética de grafo que decide se um pilar de design acontece ou não | LICOES 18/08 (A2) |
| **T13** | **Número mágico grande não é teto: teto DERIVA do cronograma.** Senão ele corta partida legítima numa variante e não corta nenhuma na outra | `server/Match.luau:71–82` (A14) |
| **T14** | **Anti-farm cooperativo:** todo sistema que recompensa "dano causado" precisa excluir dano em aliado, ou uma dupla farma no canto sem risco | `server/Combat.luau:950–957` (B1.7) |
| **T15** | **`BUILD SUCCESSFUL` não é `publicável`.** O empacotamento Android termina com sucesso e produz um artefato **não assinado**, que a loja recusa. Prova se faz **dentro do artefato**, não no código-fonte — foi extraindo o pacote que se descobriu que era preciso APAGAR os ícones por densidade, senão o ícone antigo continuava vencendo | CHANGELOG v0.5.0 e v0.6.0 |

### 3.2 · NÃO atravessam (são do Roblox e morrem aqui)

| Armadilha | Por que é local |
|---|---|
| **`rojo build` não compila Luau** — o build gera o arquivo do lugar sem verificar uma linha de código; sem instalar `luau-compile` como portão, uma missão inteira vai para o Studio sem verificação | Ferramenta da plataforma |
| **`luau-analyze` é inútil sem o dump de tipos do Roblox** — acusa `Unknown global 'game'` em todo arquivo. O portão é o `luau-compile` | Ferramenta da plataforma |
| **`humanoid.JumpPower` é ignorado sem `UseJumpPower = true`**, e `BindAction(..., Space, Sink)` engole o pulo nativo | API do Roblox |
| **`Highlight` REPLICA** — o inimigo revelado acendia até para os aliados dele. O servidor publica só o ESTADO; quem desenha é o cliente, o único que sabe de que lado está | Instância do Roblox |
| **Atributo de status guarda DURAÇÃO, nunca timestamp** — `os.clock()` do servidor não existe no relógio do cliente | Modelo de replicação do Roblox |
| **Modelos da Toolbox são proibidos** (§9 do GDD, §2 do ROBLOX.md) | Ecossistema do Roblox |
| **DataStore só existe no Studio com "Enable Studio Access to API Services" ligado** | Serviço do Roblox |
| **Servidor publicado não tem Command Bar** — as ferramentas de playtest (`Playtest.duo()`, `P.report()`) só existem no Studio | Ambiente do Roblox |

> A fronteira entre as duas listas é uma pergunta só: *"esta frase menciona um nome
> próprio da plataforma?"* Se sim, fica. Se a frase sobrevive trocando "Humanoid" por
> "personagem" e "RemoteEvent" por "mensagem do cliente", atravessa.

---

## 4. O que está NÃO VALIDADO e alguém vai querer atravessar mesmo assim

Esta seção é o freio. Tudo aqui **existe, parece funcionar e não tem nenhuma resposta
com jogador real** — e a tentação é exatamente proporcional a quanto o item parece
pronto.

| # | O item | Por que a tentação existe | O que falta, exatamente |
|---|---|---|---|
| **1** | **Os números da Sintonia** (janela / canalização / cooldown / multiplicador) | Estão implementados, testados e o pilar "funciona" no harness | **V1 não tem uma resposta.** E o próprio projeto montou um A/B (`sintoniaVariant`) porque não sabe qual configuração é melhor. Copiar a variante A para o Android é escolher o braço do experimento antes de rodar o experimento |
| **2** | **Os números de combate** (dano, mana, cadência, escudo, TTK) | O GDD §5 foi esclarecido hoje e a medição diz que os 5 elementos estão dentro das duas leituras | A medição é de **piloto automático**. O relatório humano de TTK exige, pelo próprio protocolo, ≥ 10 amostras humanas para calcular um percentil (`docs/ROBLOX.md` §11.6) — e há **zero** |
| **3** | **O nível 4 do escudo** (limiar 900) | Está no GDD desde sempre e implementado com perk | Na varredura, **0%** o alcança (p90 633, máx 712). Levar 900 para o Android é levar um nível decorativo. O gatilho para mexer exige o **p90 do dano humano**, que não existe |
| **4** | **A régua de dominância ≤ 1,25×** | Está impressa no título da própria seção do relatório e parece uma decisão fechada | Ela **não é adimensional numa economia de mana** — divide por conjurações e pune quem custa barato (LICOES 18/08). O elemento Vento em 0,91× já tem correção preparada (`Wind.basic.dmg` 9→10) esperando confirmação **humana** |
| **5** | **O ajuste da esquiva** (0,18 s de i-frame / 2,6 s de cooldown) e **o cone de assistência de 18°** | Já foram alterados uma vez, com justificativa escrita, e o jogo "parece bom" | Os dois estão marcados no `Balance.luau` como **HIPÓTESE DE TESTE**. O gatilho da esquiva (`% dos eventos de dano anulados`) **só vale se a pendência do campo `nullified` não aparecer no rodapé do relatório** (`docs/ROBLOX.md` §11.8, nº 8) |
| **6** | **O ritmo da partida** (variante "media", 330 s) | Foi escolhido por julgamento de auditoria e roda bem | É a variante do meio de três, escolhida sem jogador. O gatilho para trocar depende de duas linhas do relatório que ainda não têm amostra humana |
| **7** | **A V3 (terreno) inteira** | A métrica de desfecho tático **foi implementada na R12** e o relatório agora imprime abates / travessias / coberturas | Métrica existir não é resposta. E há teto conhecido: Sintonia, bots e tutorial mexem terreno **sem autor**, logo não geram desfecho (STATUS R12, item 6). A V3 continua sendo, por protocolo, **leitura qualitativa** nesta primeira sessão |
| **8** | **A V5 inteira** | A instrumentação de sessão foi construída na R10 (tempo, provas jogadas, ponto de desistência) | **Retorno não se mede na mesma sessão** — nenhuma instrumentação muda isso. A medição certa é mandar o link 48 h depois, sem texto, e contar quem entra sozinho (`docs/ROBLOX.md` §11.6) |
| **9** | **O gesto único** — atravessou hoje, e é o precedente | Já está no GDD §19.3: promovido, legal, pronto para o Android | O que atravessou foi a **reprovação medida em aparelho real** (17/08) e o desenho que a corrigiu. O que **não** atravessou e não deve: `tapMaxDuration = 0,18 s` e `aimDeadzonePx = 14` — são calibração do Roblox, não regra do jogo |

**A leitura de conjunto:** dos 21 achados da direção A, **10 são PROVADOS e nenhum
deles é um número de jogo** — são regras de sistema, de arquitetura, de medição e de
acessibilidade. Todo número de jogo deste projeto está, hoje, em PLAUSÍVEL ou NÃO
VALIDADO. **Essa é a forma da ponte:** as regras atravessam agora; os números esperam
a sessão.

---

## 5. Como usar este documento

1. **Ler a direção A** e decidir, item a item, o que vira emenda ao GDD. Cada um traz
   a frase e a seção. Colar é ato seu (§19.5).
2. **Ler a §2.1 (decisões silenciosas)** — são nove, e três delas (B1.1, B1.2, B1.3)
   fazem o GDD descrever comportamento que o Roblox já descartou por medição. Enquanto
   não forem resolvidas, o documento manda a frente Android reimplementar o que esta
   frente já reprovou.
3. **Tratar a B1.9 como urgente e barata:** o esquema de mira não é escolhível e o
   servidor não sabe qual foi usado. Isso contamina a V2 **na sessão de playtest que
   ainda vai acontecer**, e o conserto é carimbar o esquema no evento de disparo — o
   mesmo padrão já usado para o gesto.
4. **Não atravessar nada da §4** sem a sessão. A ordem do Diretor é salvar o que
   funciona; a §4 é a lista do que **ainda não se sabe se funciona**.

O caminho legal continua sendo o do `docs/ANDROID.md` §6, e este documento é só o
inventário que alimenta a primeira seta:

```
resultado do playtest → decisão do Diretor → ESCRITO NO GDD → implementado no Android
```

Nunca `Balance.luau` → código Android. A ponte é o documento.
