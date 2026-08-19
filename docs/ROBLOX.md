# ARKANA no ROBLOX — "Campo de Provas" (produto de validação)

> Plano de execução da versão Roblox, decidida pelo Diretor em 17/08/2026
> (GDD §9 "Decisões travadas" · PROJETO_PRISMA §9). Este documento é o
> **briefing auto-contido** da missão: uma sessão nova deve conseguir executar
> tudo lendo apenas ele + o GDD.

---

## 1. O que é e o que NÃO é

**É:** um produto de VALIDAÇÃO. Existe para responder, com jogadores reais e
custo de infraestrutura zero, as perguntas que bots nunca responderão:

| # | Pergunta de validação | Fase que responde |
|---|---|---|
| V1 | A **Sintonia** (Conjuração Combinada) é divertida entre dois jogadores? | R2 |
| V2 | O TTK (~1,5–2,5s) está certo com humanos mirando? | R1/R3 |
| V3 | **Queimar a floresta / congelar o lago vira jogada real** (meta)? | R1/R3 |
| V4 | Os 5 elementos se equilibram, ou um domina? | R3 |
| V5 | O combate é gostoso o bastante para o jogador voltar? | R3 |

**NÃO é:** o jogo-troféu. O visual premium (régua Spell Arena, PROJETO_PRISMA
§1) pertence à versão **first-person Android**, que só inicia depois desta
validação. Nada de código/asset migra do Roblox — **o GDD é a única ponte.**

---

## 2. Direção de arte (diferente do jogo premium — de propósito)

- **Estética blocky, referência Pixel Gun 3D**: formas em blocos, cores
  chapadas e saturadas, silhuetas legíveis a distância de gameplay.
- Nada contra a corrente da plataforma: é o look nativo do Roblox, barato de
  produzir e amado pelo público de lá.
- **Paleta**: mantém a do GDD §10 — base azul-noite `#0B1026`, acento dourado
  `#F0C75E`, elementais saturados (Fogo `#FF5A2A`, Água `#2AA7FF`, Terra
  `#A8763E`, Vento `#8FE8C9`, Raio `#F5D90A`).
- **Acessibilidade (inegociável, GDD §10)**: cada elemento tem **cor + FORMA**
  distintas — nunca só cor.
- **REGRA DURA: zero modelos da Toolbox.** Modelos de terceiros são a fonte
  clássica de "asset flip" e de scripts maliciosos. Geometria nossa, feita em
  parts (é barato neste estilo). **Sistemas nativos** da plataforma
  (multiplayer/replicação, avatares, física, câmera, animação) — usar à vontade.
- Terceira pessoa (câmera padrão do Roblox): o jogador vê os próprios
  cosméticos — sinergia com o Juramento anti-P2W (GDD §19.1: só cosmético).

---

## 3. Stack e disciplina de trabalho

- **Rojo** — código Luau em arquivos `.luau` versionados em git, sincronizados
  com o Studio. É o que permite equipe de agentes em raias paralelas, review e
  histórico. `rojo build` gera o `.rbxlx` (arquivo do lugar) sem abrir o Studio.
- **Roblox Studio** — já instalado nesta máquina. Necessário para PLAYTEST e
  para publicar (exige login do Diretor — nenhum agente faz login).
- **Verificação automatizável** (o que a equipe consegue provar sozinha):
  `rojo build` conclui sem erro · análise estática Luau sem erros · estrutura
  de instâncias conferida no `.rbxlx` gerado.
  **Playtest é humano** — fica com o Diretor ao final de todas as fases.
- Estrutura de pastas:
  ```
  roblox/
  ├─ default.project.json     mapa Rojo (o que vai para onde no DataModel)
  ├─ src/
  │  ├─ shared/               ReplicatedStorage — balance, tipos, matriz de combos
  │  ├─ server/               ServerScriptService — AUTORIDADE (dano, terreno, estado)
  │  └─ client/               StarterPlayerScripts — input, câmera, HUD, VFX
  └─ build/                   .rbxlx gerado (ignorado no git)
  ```

### Regra de arquitetura (herdada do que funcionou no 2D)
**Design antes de código; números viram dados; o código lê dados.** O
`balance.luau` de `shared/` é o espelho do `src/core/balance.ts` do protótipo —
rebalancear é editar um arquivo, nunca caçar constantes.

### Regra de segurança de servidor (nova, obrigatória no multiplayer)
**O servidor é autoritativo.** Cliente envia INTENÇÃO (mirei ali, conjurei
agora); servidor decide dano, cooldown, mana e estado do terreno. Nada de
`FireServer("tome 40 de dano")`. Todo RemoteEvent valida: cooldown, distância
plausível, mana disponível, estado vivo. Isso é a base do anti-cheat (EQUIPE.md
3.3) e não é opcional nem no protótipo.

---

## 4. Lições de controle que ATRAVESSAM do teste em aparelho (17/08)

O teste do APK 2D reprovou dois pontos — os dois viram REQUISITO nativo aqui:

1. **Mira e disparo em UM gesto.** Dois gestos (mirar, depois atirar) é
   inviável em celular. Padrão obrigatório: arrastar a partir do botão da
   magia mira e dispara ao soltar; toque simples = disparo rápido na direção
   da câmera. (Referência de gênero: Brawl Stars / Free Fire.)
2. **Troca de elemento deve responder ao toque.** Botões grandes (≥48dp),
   feedback visual imediato do elemento ativo, e alvo de toque generoso.
3. Suporte a teclado/mouse em paralelo (PC) — o Roblox entrega os dois.

---

## 5. Fases (executar em ordem; cada uma fecha com commit + CHANGELOG)

### R0 · Fundação
- Rojo instalado, `default.project.json`, esqueleto `shared/server/client`.
- `balance.luau` portado do protótipo (dano, mana, cooldowns, escudo evolutivo,
  velocidades) e `elements.luau` (5 elementos: cor + forma + stats).
- Movimentação + câmera 3ª pessoa (nativas) e **um elemento (Fogo) disparando**
  projétil com tempo de viagem (GDD §4.1: nada de hitscan) — servidor
  autoritativo, cliente só prevê o visual.
- **Aceite:** `rojo build` gera o `.rbxlx`; abrir no Studio e disparar fogo.

### R1 · Os pilares
- 5 elementos completos (projétil, cor, forma, som placeholder, custo de mana).
- **Terreno reativo em blocos** — a mesma máquina de estados do 2D (GDD §14):
  floresta queima e propaga (vira carvão), lago congela e vira rota, raio
  eletrocuta água conectada, terra ergue muro destrutível, lama desacelera.
  Grid lógico de células → parts 3D; propagação no servidor.
- Arena com os biomas (lago, floresta, grama alta, rochas, campo aberto).
- Escudo de Magia Evolutivo (níveis 1→4, GDD §5) + HUD (vida, escudo, mana,
  elemento ativo) + controles móveis da §4 acima.
- **Aceite:** partida solo/multi onde queimar floresta e congelar lago funciona
  e é visível; build limpo.

### R2 · Sintonia (a invenção nº 1)
- Matriz dos 10 combos (GDD §9) — pelo menos os 5 de maior impacto visual
  primeiro (Tornado Flamejante, Explosão de Plasma, Eletrocussão, Chuva de
  Magma, Tempestade Torrencial), demais em seguida.
- Janela de 1,5s, canalização visível/sonora ~1s, cooldown compartilhado longo
  (limitadores do GDD §9), validação 100% no servidor.
- **Ping de Sintonia** (GDD §18.7): propor combo com 1 toque; aliado aceita.
- **Aceite:** dois jogadores (ou 1 + bot aliado) executam um combo e o efeito
  muda o terreno.

### R3 · Loop de Battle Royale
- 16–20 jogadores (GDD §19.2 — 20 no lançamento, não 60), matchmaking simples
  por lugar/servidor.
- Zona arcana fechando, queda/entrada na arena, loot básico (pergaminhos de
  cura/escudo), placar e tela de fim com **Selo do Campeão** (GDD §18.6).
- Bots preenchendo partidas vazias (GDD §19.6 — tecnologia que já dominamos).
- **Aceite:** partida completa do início ao campeão, jogável por humanos.

---

## 6. Definition of Done da missão — FECHADO em 18/08/2026

- [x] R0, R1, R2, R3 completas, commitadas, CHANGELOG atualizado (v0.2.0-roblox).
- [x] `rojo build` gera `.rbxlx` sem erro (233 KB) e a estrutura confere.
- [x] Verificação estática: **19 arquivos Luau, 0 erros** via `luau-compile`
      (toolchain Luau 0.734 instalado em `%LOCALAPPDATA%\Programs\luau`).
      Nota: `luau-analyze` não serve aqui — sem o dump de tipos do Roblox ele
      acusa `Unknown global 'game'` em todo arquivo. O gate é o `luau-compile`.
- [x] Nenhum RemoteEvent confia no cliente: `Main.server` sanitiza toda entrada,
      `Combat` valida vivo/cadência/cooldown/mana com o relógio do servidor, e
      dano só existe via `Combat.applyDamage` (zona e terreno respeitam isso).
- [x] Este documento atualizado · [x] Memória da equipe atualizada.
- [x] Relatório final ao Diretor entregue (ver §10).

### Autotestes embutidos (rodar na Command Bar do Studio)
```lua
require(game.ServerScriptService.Server.Combat).selfTest()
require(game.ServerScriptService.Server.Sintonia).selfTest()
require(game.ServerScriptService.Server.Match).selfTest()
```

---

## 10. Como abrir e testar (Diretor)

1. Abra o **Roblox Studio** e crie um lugar vazio (ou abra `roblox/build/arkana.rbxlx`,
   gerado por `rojo build`).
2. Para desenvolver com sincronização ao vivo: no terminal, dentro de `roblox/`,
   rode `rojo serve`; no Studio, plugin **Rojo → Connect**.
3. Aperte **Play**. Para testar a Sintonia (a pergunta de validação nº 1),
   use **Test → Clients and Servers → 2 jogadores**.

### O que observar (o que esta missão existe para responder)
- **V1 Sintonia:** dois magos conjurando elementos diferentes no mesmo alvo em
  1,5s disparam a canalização e a Magia Combinada. É divertido? Vale o risco?
- **V2 TTK:** duelo parelho deve durar ~1,5–2,5s. Rápido demais? Lento demais?
- **V3 Terreno:** queimar a floresta e congelar o lago mudam a partida de fato?
- **V4 Elementos:** algum domina claramente os outros?
- **V5 Feel:** o gesto único de mira/disparo resolveu o problema do celular?

### Knobs de calibração (editar `roblox/src/shared/Balance.luau`, não o código)
velocidade do mago (`player.speed = 22`) · dano do combo (`sintonia.dmgMult = 2.35`)
· tamanho do incêndio (`terrain.fuelBudget`) · ritmo da zona (`match.zone`).

## 7. O que NENHUM agente faz (fronteira dura)

- **Login/publicação no Roblox** — exige credenciais do Diretor. Os agentes
  entregam o arquivo do lugar pronto; publicar é ato humano.
- Instalar plugins de terceiros no Studio além do **plugin oficial do Rojo**.
- Usar modelos/scripts da Toolbox (§2).

---

## 11. Protocolo do PLAYTEST ALFA (duplas de desconhecidos)

> A §10 ensina a abrir o jogo no Studio e testar **sozinho**. Esta seção é outra
> coisa: é a **sessão com gente de fora**, o único experimento capaz de responder
> V1–V5. A auditoria externa de 18/08 continua valendo ao pé da letra: *"a
> próxima unidade de progresso não é mais uma feature; é uma dupla externa
> apertando COMBO sem você dizer para apertar"*.
>
> Auto-contido de propósito: quem ler só esta seção roda a sessão e lê o
> resultado. Toda afirmação sobre o comportamento do jogo aponta o arquivo onde
> ela é verificável. Onde não há dado, está escrito que **não há** — protocolo
> que promete medição que o build não entrega é pior do que protocolo nenhum.

### 11.1 O que UMA sessão responde — e o que não

| # | Pergunta (§1) | Uma sessão responde? | Por quê |
|---|---|---|---|
| V1 | Sintonia é divertida entre dois jogadores? | **Sim** — é para isto | Funil impresso + observação direta |
| V2 | TTK está certo com humanos mirando? | **Parcial** | Precisa de humano-contra-humano e de amostra (§11.6) |
| V3 | Queimar/congelar vira jogada real? | **Só qualitativo** | O evento de terreno não tem sujeito (pendência impressa) |
| V4 | Algum elemento domina? | **Não** | Os `casts` de uma sessão não sustentam o índice |
| V5 | O jogador volta? | **Não, por construção** | Retorno é comportamento de OUTRO dia (§11.6) |

Quem tratar V4 e V5 como respondidas ao fim da primeira sessão vai rebalancear o
jogo com ruído. O produto desta sessão é **V1 respondida** e um sinal preliminar
de V2/V3.

### 11.2 Amostra: quantas pessoas, em que arranjo, por quanto tempo

**Como as duplas se formam — leia antes de convidar alguém.** Não existe tela de
escolher parceiro (nenhuma foi encontrada no cliente). Na largada, o
`Match.startMatch` monta o roster com **os humanos primeiro** e chama
`Teams.build(roster, Teams.DEFAULT_SIZE)`; o `Teams.build` fatia por posição
(`id = floor((i-1)/2)+1`, em `Teams.luau`). Ou seja:

- 1º e 2º humanos do roster = **dupla 1**; 3º e 4º = **dupla 2**; e assim por diante.
- A ordem do roster é a que `Players:GetPlayers()` devolve. Na prática é a ordem
  de entrada no servidor, mas **isso não é garantido em documento — não verificado**.
- **Número ÍMPAR de humanos deixa o último com um BOT de parceiro** (comentário
  explícito no `startMatch`). Essa dupla **não responde V1**.

Mitigação operacional: entre com as pessoas **em pares e em sequência**, e mande
cada um conferir na tela quem é o parceiro — o HUD tem a faixa `PARCEIRO` com o
nome, alto à direita (`Hud.luau`, `Strings["hud.partner"]`). Saiu errado: sair e
entrar de novo **antes da largada**.

**Arranjo recomendado (o menor que produz resposta): 4 humanos = 2 duplas.**

| Arranjo | Permite concluir | NÃO permite concluir |
|---|---|---|
| **2 humanos** (1 dupla) | V1 em modo sim/não: a dupla achou o COMBO sozinha? usou de novo? | Nada de V2/V4: **toda a oposição é bot**. O preenchimento vai até `maxPlayers - humanos`, com teto `TUNE.maxBots = 11` (`Match.luau`). Dado de bot já enganou este projeto uma vez — o "Fogo 1,42×" da R7 era taxa de acerto do piloto, não balanceamento |
| **4 humanos** (2 duplas) — *recomendado* | V1 com duas duplas independentes (uma dupla que não acha o botão é anedota; duas é sinal). Existe humano-contra-humano, então o `p50`/`p10` de TTK humano começa a existir | V4 (poucos `casts`) e V5 (outro dia). Ainda não é estatística: leia percentil como sinal, não como medida |
| **8 humanos** (4 duplas), em 3 sessões | Aí a `DOMINANCIA AJUSTADA POR USO` começa a ter denominador. É o menor arranjo em que discutir V4 é honesto | V5 continua fora: retorno não se mede na mesma sessão |

**Duração.** Números do repositório, com a variante padrão `zoneVariant = "media"`
(`Balance.luau`):

- Campo de Provas (onboarding) na PRIMEIRA entrada de cada pessoa: até ~3 min
  (`Tutorial.luau`, roteiro 0–180 s). O lobby **segura** enquanto alguém está no
  tutorial (teto `TUNE.tutorialHoldMax = 240`).
- Lobby: `Balance.match.lobbyWait = 25` s.
- Partida: 5 fases × (36 s parada + 30 s fechando) = **330 s** de cronograma;
  teto duro ~490 s (`overtimeFactor 1.35` + `overtimeSlack 45`, em `Match.luau`).
- Tela de fim: `TUNE.endedTime = 10` s, e volta ao lobby.

→ **3 partidas ≈ 3 min de tutorial + 3 × (25 + ~330 + 10) ≈ 21 min.**
Reserve **45 min** por sessão: sobra folga para travamento, reentrada e para as
perguntas do fim. Menos de 3 partidas não serve — a primeira partida de qualquer
pessoa é a partida em que ela está aprendendo a andar.

### 11.3 O papel do Diretor: a regra do silêncio

A V1 é literalmente *"a dupla aperta COMBO **sem você dizer para apertar**"*. A
instrução já existe e **é do produto**: o passo 5 do Campo de Provas mostra
`"Pingue COMBO."` (`Strings["tutorial.combo"]`) com halo no botão
(`TutorialUi.luau`). É o único tutorial verbal do roteiro, e é ele que está sendo
avaliado. Se você repetir a frase, o que a sessão mede deixa de ser o produto e
passa a ser você.

**O que você PODE dizer** — o script inteiro, decorado, sem improviso:

> "É um jogo de magos, em dupla, tipo battle royale. Vai aparecer um treino curto
> no começo. Joga do jeito que você achar. Eu não vou responder nada durante a
> partida, mas guarda o que te irritar — eu pergunto no fim."

**O que você NÃO pode dizer, e o que cada frase custa:**

| Frase proibida | O que ela destrói |
|---|---|
| "aperta COMBO", "vocês dois no mesmo alvo", "faz combo com ele" | **V1 inteira.** O funil não distingue ping espontâneo de ping induzido — não há como filtrar depois |
| "queima a floresta", "congela o lago" | **V3.** A pergunta é sobre INTENÇÃO do jogador, não sobre a mecânica funcionar |
| "troca de elemento", "usa o Q para esquivar" | **V4** (polui o share de uso) e o pedaço de feel da **V5** |
| "esse elemento é melhor" | **V4** |
| "mira na frente, o tiro demora a chegar" | **V2.** Tempo de viagem (GDD §4.1) é justamente o que o jogador tem de aprender sozinho |
| "tá vendo aquele botão ali?" (apontar qualquer coisa na tela) | Anula o teste de legibilidade da UI — o mais barato de estragar sem perceber |

Silêncio também no chat e no voz **durante** a partida. Perguntaram? Responda
sempre a mesma coisa — *"faz o que você achar que dá"* — e **anote a pergunta
literal**: a pergunta em voz alta é o dado mais valioso da sessão (§11.4).

**Única exceção:** travamento real (personagem preso, cliente caído, jogador
parado sem saber que morreu). Socorra, e **marque a partida como contaminada** —
o relatório dela não entra na leitura de V1.

Regra final: **uma variante por sessão.** Não troque
`Balance.experiment.sintoniaVariant` nem `zoneVariant` no meio — a variante é
resolvida no `require` do `Balance` e trocar exige editar arquivo e reiniciar
(NOTA DE HONESTIDADE no cabeçalho do `Playtest.luau`). Trocar no meio contamina
as duas coortes.

### 11.4 O que observar com o olho (o que a telemetria não pega)

A telemetria conta **eventos**. Ela não sabe que a pessoa estava confusa, que
apertou o botão errado três vezes, ou que riu. Uma folha por participante, uma
linha por anotação, com o minuto:

1. **Primeiro minuto.** Ela anda? gira a câmera? atira? Alguém parado 30 s no
   Campo de Provas é defeito de onboarding, não timidez.
2. **Onde trava.** Marque o passo: `aim` · `targets` · `burn` · `freeze` ·
   `combo` (nomes reais dos passos, `Tutorial.luau`). Travar em `burn`/`freeze` é
   sinal de V3; travar em `combo` é sinal de V1.
3. **O que pergunta em voz alta — transcreva literal.** Cada pergunta é uma frase
   que está faltando na tela: "Como eu troco?", "Quem é meu parceiro?", "Isso é
   meu ou dele?", "Acabou?".
4. **Quando ri, quando xinga, quando o corpo se mexe.** A fusão saindo é o
   momento candidato: se o combo explode e a sala fica em silêncio, V1 é NÃO
   mesmo que o funil fique verde.
5. **O segundo COMBO.** O relatório conta pings; ele **não** conta "pediu de novo
   por vontade própria". Anote a hora do primeiro ping espontâneo e se houve um
   segundo.
6. **Quando desiste.** Alt-tab, larga o mouse/celular, fala com quem está do
   lado, pergunta quanto falta.
7. **A primeira frase depois do fim da partida**, antes de qualquer pergunta sua.
8. Só no fim da sessão inteira, perguntas abertas — nunca de sim/não, nunca
   sugerindo a resposta: *"me conta o que aconteceu na última partida"* · *"teve
   alguma hora que você não entendeu o que estava acontecendo?"* · *"o que você
   faria diferente se jogasse de novo?"*. **Não pergunte "você voltaria?"** —
   todo mundo diz que sim, e a V5 tem uma medição melhor (§11.6).

Pontos cegos conhecidos da instrumentação, que só o olho cobre nesta sessão:
tela de celular (não há métrica de aparelho — `device_tier` sai `"unknown"`),
legibilidade do HUD, som, e **descoberta** — o Grimório emite `discovery`
(`Grimoire.luau`), mas **o relatório não imprime nenhuma linha de descoberta**.

### 11.5 Como o relatório chega até você

**O relatório sai sozinho no fim de cada partida.** O `Match.finish()` chama
`Telemetry.matchEnd()` (`Match.luau`), que faz `print(Telemetry.report())`
(`Telemetry.luau`). O destino é a **saída do SERVIDOR** — não a do cliente.

**No lugar publicado, o que você tem é exatamente isso e mais nada:**

- `Playtest.duo()`, `P.report()`, `P.reset()`, `P.trace()` **não existem no
  alfa**. O cabeçalho do `Playtest.luau` é explícito: o módulo só existe quando
  alguém o requer na **Command Bar do Studio, contexto SERVIDOR**. Servidor
  publicado não tem Command Bar.
- Não há persistência: `Telemetry.matchStart` zera os agregados (`newAgg()`), e
  não há DataStore nem envio para lugar nenhum. **Relatório não copiado é
  relatório perdido.** Entre o fim de uma partida e o começo da próxima você tem
  `TUNE.endedTime = 10` s + `lobbyWait = 25` s: copie o texto para um arquivo
  nesse intervalo, com a data e o nome da sessão.
- O caminho nativo do Roblox para ler a saída do servidor num lugar publicado é o
  **Developer Console do cliente (F9), aba de log do SERVIDOR**, visível a quem
  tem permissão de edição no lugar. ⚠ **Isto é comportamento da PLATAFORMA, não
  deste repositório — não é verificável aqui.** Confirme no ensaio (abaixo) antes
  de depender dele.

**Ensaio obrigatório antes da sessão com gente (15 min, sozinho):**

1. `rojo build` e abrir o lugar; `Test → Clients and Servers → 2 jogadores`.
2. Command Bar em contexto **SERVIDOR**:
   `local P = require(game.ServerScriptService.Server.Playtest)` e depois `P.duo(3)`.
3. Deixe a partida terminar e **confirme que o relatório apareceu no Output**. Se
   não aparecer no Studio, não vai aparecer no lugar publicado.
4. Publique — **ato seu (§7: nenhum agente faz login nem publica)** —, entre no
   lugar publicado e abra o F9 para confirmar que a aba de servidor existe e
   mostra o log. Se não mostrar, **a sessão vira presencial no Studio**: você
   perde o "desconhecido pela internet", mas não perde o dado.

**O cabeçalho carimba a condição do experimento** — é por isso que o dado nunca
fica órfão. Confira que bate com o que você pretendia rodar:

```
partida .......... m68a3f-1c4e
duracao .......... 5m31s
variante Sintonia  A  (channel 1.00s / cd 24.0s / x2.35)
fonte do dano .... eventos `damage` da raia A
colunas .......... human_only = evento cujo SUJEITO e humano
                   all_entities = humanos + bots (NAO use para balancear)
```

**Leia SEMPRE a coluna `human_only`.** A `all_entities` mistura bot com gente e
existe só para conferência — o próprio relatório avisa isso no cabeçalho.

Seções impressas, na ordem: `FUNIL DA SINTONIA` · `TEMPO ATE A QUEDA (TTK)` ·
`DANO POR ELEMENTO` · `ABATES POR ELEMENTO` · `DOMINANCIA AJUSTADA POR USO` ·
`ESCUDO EVOLUTIVO` · `ESQUIVA` · `TERRENO` · `RITMO DA PARTIDA` · `PENDENCIAS DE
INSTRUMENTACAO`. Esse é o conjunto MÍNIMO garantido; a raia de instrumentação da
V5 e do dado de aparelho está acrescentando seções (§11.6, V5). Se o seu
relatório trouxer blocos além dos listados aqui, a instrumentação avançou — leia
os novos, esta lista é piso, não teto.

**Leia o rodapé de PENDÊNCIAS primeiro.** Ele lista, por partida, o que *não* foi
medido. Uma linha que aparecer ali invalida a seção correspondente — se aparecer
`` `dodge_start` no Combat.handleDodge ``, o bloco ESQUIVA daquela partida é zero
por falta de emissor, não por falta de esquiva. E se aparecer `!! N falhas
internas engolidas por pcall`, o jogo não sentiu, mas o dado sim.

### 11.6 O que é "bom" — limiares por pergunta

Cada limiar diz de onde vem. **`[código]`** = está escrito no repositório.
**`[doc]`** = está escrito num documento do projeto. **`[julgamento]`** = proposta
minha, sem respaldo no repositório — ponto de partida a discutir, não verdade.

#### V1 — Sintonia (bloco `FUNIL DA SINTONIA`, coluna `human_only`)

| Linha do relatório | Bom | Fonte |
|---|---|---|
| `OPORTUNIDADES COM >=1 PING` | ≥ 50% = a dupla ACHA a mecânica · 20–50% = achou e não confia · < 20% = não está sendo descoberta | `[julgamento]` |
| `INTENCIONALIDADE (pacto / canaliz.)` | ≥ 40% = coordenação real · < 20% = o que existe é coincidência de tiro, e a V1 como perguntada está respondida **NÃO** | `[julgamento]`; a definição "intencional = canalização nascida de ping aceito" é `[código]` (comentário do próprio relatório) |
| `OPORTUNIDADES QUE VIRARAM FUSAO` | conversão fim-a-fim; use para comparar sessões entre si, não contra um alvo absoluto | `[julgamento]` |
| `taxa de interrupcao` | alta = o custo cobrado no início está punindo demais → candidato à variante B (§11.8) | `[julgamento]` |

**A leitura que vale mais que todas e não está no relatório:** a dupla pediu combo
**de novo**, por vontade própria, depois do primeiro? (§11.4, item 5.)

**Zero é leitura válida, mas cuidado com o que ela significa.** Se
`oportunidades (engajamentos)` = 0, o funil **não** reprovou a Sintonia: reprovou
o arranjo — as duas pessoas nunca estiveram perto uma da outra, com elementos
diferentes e alvo em comum. Repita a sessão antes de concluir qualquer coisa.

**Contaminação conhecida do funil neste build.** `Sintonia.propose` emite
`sintonia_ping` **antes** de qualquer verificação de aliado (`Sintonia.luau`), e o
Campo de Provas usa o ping de verdade (o `Tutorial` só observa). Efeito prático:
quem entra **com a partida já em andamento** faz o tutorial dentro da janela de
coleta daquela partida, e os pings dele entram em `pings` — inflando `pings por
oportunidade` e diluindo `conversao ping -> aceite`. Não afeta `oportunidades` nem
`OPORTUNIDADES COM >=1 PING` (quem está no tutorial não tem time, logo não tem
engajamento aberto). **Mitigação: comece a sessão com todo mundo já dentro do
servidor, no lobby, antes da largada.** Quem chegar atrasado, anote — e desconte a
linha de pings daquela partida.

#### V2 — TTK (bloco `TEMPO ATE A QUEDA`)

⚠ **Há duas réguas escritas no projeto e elas não batem** (ver §11.9): a §1 e a
§10 deste documento dizem **1,5–2,5 s**; a varredura de balanceamento da R7 usou
alvo **2,75–3,5 s**. Decida qual vale **antes** de olhar o número, senão o número
decide por você.

| Linha | Bom | Fonte |
|---|---|---|
| `amostras (vistas / guardadas)` | **piso**: menos de 10 amostras humanas = anedota, não percentil. Não calcule nada | `[julgamento]` |
| `p50 (mediana)` | dentro da régua que você escolheu (1,5–2,5 s ou 2,75–3,5 s) | `[doc]` |
| `p10 (a queixa 'morri instantaneo')` | **< 1,0 s = alguém vai dizer "morri sem ver"** — a queixa que mais mata retenção | `[julgamento]`; o relatório já isola o p10 exatamente por isso `[código]` |
| `quedas SEM eliminacao` | alto = o Retorno Contestável está sendo usado (bom sinal de dupla) | leitura qualitativa |

#### V3 — Terreno (bloco `TERRENO`)

| Linha | Bom | Fonte |
|---|---|---|
| `celulas por incendio (media / maior)` | ~44 células por tática de Fogo é o valor de projeto medido | `[código]` — comentário de `Balance.terrain.fuelBudget` |
| `frentes travadas por combustivel` | alto = `fuelBudget` apertado (a própria linha diz isso) | `[código]` |
| `celulas que pegaram fogo ... (% da cobertura)` | varredura direta da grade, independente de emissor: serve para pegar mentira nos eventos | `[código]` |
| `interacoes por AUTOR/partida` | **não use para V3** — ver abaixo | — |

**V3 é leitura QUALITATIVA nesta sessão, e isso não é escolha: é limitação.** Duas
razões, ambas verificáveis:

1. `terrain_state_change` é emitido **com sujeito nulo (mundo)** — a própria linha
   de pendência do relatório diz que "interação de terreno POR JOGADOR não tem
   como ser atribuída". `interacoes por AUTOR` divide por autores que incluem o
   mundo.
2. `Events.Name.TerrainTacticalOutcome` ("mudança de terreno que teve consequência
   tática verificável") está **declarado e consumido, e ninguém o emite** — uma
   varredura em `roblox/src/` só encontra a declaração e o consumo. Ou seja:
   **não existe métrica de "o terreno mudou a partida"**.

Logo, V3 se responde com o olho: o jogador queimou/congelou **e depois usou o
resultado** (atravessou o lago virado ponte, perseguiu por dentro do carvão, usou
o muro como cobertura)? **Uma ocorrência observada vale mais que a contagem de
incêndios** — porque a contagem não distingue intenção de acidente.

#### V4 — Elementos (bloco `DOMINANCIA AJUSTADA POR USO`)

| Linha | Bom | Fonte |
|---|---|---|
| `idx(h)` de cada elemento | **≤ 1,25×** — a régua da auditoria | `[código]` + `[doc]`, impressa no título da própria seção |
| lado baixo | `Wind` em ~0,91× já é conhecido e tem correção preparada (§11.8) | `[doc]` — STATUS R8/R9 |
| piso de amostra | **< 30 `casts` do elemento: ignore o índice.** Sem amostra nenhuma ele sai `-`; com amostra pequena ele sai instável, que é pior | `[julgamento]` |

Leia `idx(h)`, **nunca** `idx(a)`. Foi exatamente misturar bot com gente que
produziu o falso alarme "Fogo 1,42×" da R7, investigado e inocentado na R8.

**Uma sessão não responde V4.** Trate o índice como triagem: ele só serve para
dizer se vale a pena olhar um elemento nas sessões seguintes.

#### V5 — Retorno

⚠ **A §1 e a §10 deste documento definem V5 de formas diferentes** (§11.9). A §1 —
que é a tabela normativa das perguntas de validação — diz: *"O combate é gostoso o
bastante para o jogador voltar?"*. É essa a definição usada aqui.

**Estado da instrumentação no build de referência desta seção: não existe.**
`Events.Name.SessionEnd = "session_end"` estava declarado no schema com **nada o
emitindo**; o relatório não tinha seção de sessão nem de retorno; e `device_tier`
saía `"unknown"` (pendência impressa). **Uma raia paralela está instrumentando
exatamente isso.** Portanto, o primeiro teste é no próprio relatório:

- **Se aparecer um bloco de sessão/retorno** (e o rodapé de pendências deixar de
  acusar `device_tier`), a instrumentação chegou: leia os números de lá e trate
  o protocolo manual abaixo como conferência independente.
- **Se não aparecer**, **V5 não tem número nenhum** e o protocolo manual abaixo
  é a única medição disponível.

Em qualquer dos dois casos, **retorno de verdade não se mede na mesma sessão** —
nenhuma instrumentação muda isso.

**Como medir V5 sem instrumentação — e é a medição certa de qualquer jeito:**

1. **Não pergunte "você voltaria?"** no fim da sessão. Todo mundo diz que sim, na
   frente de quem fez o jogo. A resposta tem valor zero.
2. Mande o link **48 h depois**, sozinho, sem texto, sem lembrete, sem pedido.
3. **Conte quem entra por conta própria.**

| Resultado | Leitura | Fonte |
|---|---|---|
| ≥ 2 de 4 voltando | sinal forte de V5 = sim | `[julgamento]` |
| 1 de 4 | inconclusivo, repita com outra dupla | `[julgamento]` |
| 0 de 4, **com todo mundo tendo dito "adorei"** | o resultado **mais informativo** da sessão inteira: o jogo agrada e não puxa de volta | `[julgamento]` |

**O pedaço de feel que a §10 chama de V5** (o gesto único de mira/disparo resolveu
o celular?) só existe se **pelo menos um participante jogar no celular** — e a
leitura será 100% do olho, porque não há métrica de aparelho até o `device_tier`
chegar.

### 11.7 Onde o dado vai ser fraco (diga isso a si mesmo antes de olhar)

1. **Amostra.** 4 pessoas, 3 partidas. Percentil de TTK com 10–30 amostras tem
   intervalo largo. Todo número desta sessão é **sinal**, não medida.
2. **Terreno sem sujeito e sem desfecho** (§11.6, V3): não há como dizer "o
   jogador X queimou de propósito e ganhou a briga por causa disso".
3. **V5 sem instrumentação** (§11.6, V5).
4. **Aparelho:** `device_tier` = `"unknown"`. Nenhuma conclusão sobre celular
   fraco, FPS ou latência sai deste relatório.
5. **Fase da zona é ESTIMADA por relógio** — o `Match` não expõe `getState`
   (pendência impressa). Cruzamentos do tipo "as fusões acontecem na fase 4"
   herdam esse erro.
6. **Pings do tutorial de quem entra atrasado** entram em `pings` (§11.6, V1).
7. **Bots no meio.** Com 4 humanos e `maxBots = 11`, a maioria dos corpos na arena
   é bot. `human_only` protege as métricas cujo **sujeito** é humano; não protege
   contra "o humano passou a partida caçando bot".
8. **Uma sessão é uma condição.** Variante A **ou** B, um `zoneVariant` só.
   Comparar coortes exige **duas sessões**, com gente diferente.

### 11.8 O que fazer com o resultado — os gatilhos já preparados

Os números abaixo **já estão escolhidos e esperando dado humano**. Nenhum deles é
decisão nova a tomar no calor da sessão.

| # | Knob (arquivo · campo) | Valor hoje | Gatilho | Fonte |
|---|---|---|---|---|
| 1 | `Balance.shield.thresholds[4]` | `900` | p90 do dano **humano**: **< 700 → baixe · > 1400 → suba · entre = mantenha** | STATUS R8/R9 (contingente pós-playtest) |
| 2 | `Balance.elements.Wind.basic.dmg` | `9` → `10` | `idx(h)` de `Wind` confirmado abaixo de 1,0 com humanos. Único fora da régua pelo lado baixo, e isolável | STATUS R8/R9 |
| 3 | `Balance.experiment.sintoniaVariant` | `"A"` | `taxa de interrupcao` alta **e** `INTENCIONALIDADE` baixa na coorte A → a próxima sessão roda **B** (`channel 0.75` / `cd 18` / `x2.0`). A hipótese está escrita no próprio `Balance`: "B aumenta uso intencional sem virar spam" | `Balance.luau` |
| 4 | `Balance.experiment.zoneVariant` | `"media"` (330 s) | partida acabando **antes** de a dupla ter tido a primeira oportunidade de Sintonia (`tempo ate o primeiro combate` alto + `oportunidades` baixo) → `"longa"` (435 s). Curva de vivos com trechos longos sem nada acontecendo → `"blitz"` (275 s) | `Balance.luau` |
| 5 | `Balance.terrain.fuelBudget` | `16` | incêndio grande ou pequeno demais na observação. O comentário é explícito: **"para regular incêndio: mexa AQUI, não no sorteio"** | `Balance.luau` |
| 6 | `Balance.player.speed` | `22` | knob de calibração declarado desde a R0 ("ajustar no playtest") | `Balance.luau` |
| 7 | `Balance.controls.aimAssist.coneDeg` | `18` (era 30) | hipótese de teste declarada; se a queixa for "não acerto nada" com TTK dentro da régua, é aqui | `Balance.luau` |
| 8 | `Balance.player.dodge` | `iframes 0.18` / `cooldown 2.6` (eram 0.28 / 1.6) | linha `% dos eventos de dano anulados` do bloco ESQUIVA. **Só vale se a pendência do `nullified` NÃO aparecer no rodapé** | `Balance.luau` |
| 9 | `Match.luau` · `TUNE.maxBots` | `11` | comentário `ponytail`: "sobe para `M.maxPlayers - 1` **quando o playtest medir**" 20 Humanoids em aparelho fraco | `Match.luau` |

⚠ **O gatilho nº 1 não é diretamente legível no relatório.** O relatório imprime
`dano CAUSADO por nivel`, `pico medio` de escudo e `chegaram ao nivel 4 ... (%)` —
**não** imprime a distribuição de dano por jogador, então **não existe p90 de dano
humano impresso** (nem em `Telemetry.snapshot()`, que já vem agregado). Substituto
utilizável enquanto isso, e ele **não é** o gatilho escrito:

> Se, somando as partidas humanas da sessão, `chegaram ao nivel 4` continuar em
> **0%** — como na varredura de bots, que deu p50 211 · p90 633 · máx 712 com 0%
> alcançando 900 —, o nível 4 é decorativo e a decisão é **baixar o threshold**.

**O que fazer sempre, independentemente do resultado:** copiar os relatórios para
um arquivo com data + variante, anotar as observações da §11.4 ao lado, e **não
mexer em mais de um knob por sessão**. Duas mudanças simultâneas tornam a próxima
sessão ilegível — que é exatamente o problema que a telemetria em duas colunas
existe para não repetir.

### 11.9 Contradições conhecidas (apontadas, não corrigidas aqui)

Ficam registradas para o Diretor decidir; nenhuma foi reescrita por esta seção.

1. **Alvo de TTK.** §1 e §10 dizem **1,5–2,5 s**; a varredura de balanceamento da
   R7 usou **2,75–3,5 s** e reportou "5/5 no alvo". As duas réguas não podem valer
   ao mesmo tempo.
2. **Definição de V5.** §1 (tabela normativa) = *"o combate é gostoso o bastante
   para o jogador voltar?"*. §10 = *"o gesto único de mira/disparo resolveu o
   problema do celular?"*. São perguntas diferentes, com instrumentações
   diferentes.
3. **§6 está congelada em 18/08.** Diz "19 arquivos Luau" e `.rbxlx` de 233 KB; o
   estado atual é 39 arquivos e ~748 KB, com harness de 43 verificações. A §5
   também só descreve R0–R3, enquanto R4–R9 já foram executadas.
4. **Cabeçalho do `Playtest.luau` desatualizado.** Ele afirma que "o Match não
   expõe `startMatch` (é local) nem `forceEnd`", e por isso `duo()` empurra o
   lobby mexendo em `Balance.match`. O `Match` **já expõe** `Match.forceStart(bots)`
   e `Match.forceEnd()`; o `Playtest` continua usando o caminho antigo.
