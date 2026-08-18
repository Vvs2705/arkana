# ARKANA — da ideia ao estado atual

> Dossiê narrativo do projeto: o que é, por que cada decisão foi tomada, o que
> já existe e o que falta. Escrito para quem chega agora (pessoa ou IA) entender
> o projeto inteiro sem ler código.
> Atualizado em 18/08/2026 · Direção de jogo: Vinicius.

---

## 1. A ideia

Arkana nasceu de uma pergunta simples: **e se o Apex Legends fosse de magos?**

Mas a resposta não parou na temática. Trocar arma de fogo por bola de fogo é
skin, não é jogo. A pergunta que passou a guiar todo o design foi outra, e ela
está escrita no GDD:

> **"Qual é a invenção que vão copiar DE NÓS?"**

Duas respostas surgiram, e elas são o projeto inteiro:

### Pilar 1 — Sintonia (Conjuração Combinada)

Dois magos conjurando **elementos diferentes** no mesmo alvo, dentro de uma
janela de 1,5 segundo, fundem as magias numa **Magia Combinada** mais forte que
a soma das partes. São 10 pares elementais (Fogo + Vento = Tornado Flamejante,
Água + Raio = Eletrocussão, Terra + Raio = Cristais Carregados...).

Por que é o pilar: é o **motivo mecânico de jogar em squad** — não "somar dano",
mas fazer junto algo que sozinho é impossível. Gera clipe viral, cria teto
competitivo (times treinam rotações de combo) e é estruturalmente impossível de
copiar rápido, porque exige redesenhar o combate inteiro em volta.

Os limitadores nasceram junto com a ideia, e são o que impede o abuso: a
canalização de ~1s é visível e interrompível, e o **custo é cobrado no início** —
combo interrompido não devolve mana nem cooldown. Combo errado deixa a dupla
vulnerável de verdade.

### Pilar 2 — Terreno Reativo

O mapa não é cenário, é **recurso**. Fogo se propaga pela floresta e destrói a
cobertura permanentemente. Água congela o lago e cria uma ponte. Raio conduz por
toda a água conectada. Terra ergue muros destrutíveis. Água + terra vira lamaçal.

A arquitetura veio das **3 regras formais do *chemistry engine* de Breath of the
Wild**: elementos mudam o estado de materiais; elementos mudam o estado de outros
elementos; materiais nunca mudam materiais. Na prática: o **material** de uma
célula é imutável, o **estado** é o que a magia altera. Essa separação é o que
mantém o sistema simples o bastante para rodar em celular.

### O Juramento de Arkana

Uma terceira decisão, não mecânica mas de produto, foi travada cedo e é
inegociável: **o jogo nunca será pay-to-win**. Todo item que afeta poder existe
apenas dentro da partida, encontrado no loot — nunca vendido. A loja vende
exclusivamente cosmético. É posicionamento de marca num mercado mobile saturado
de P2W, e está publicado no GDD como política.

---

## 2. O método: o documento manda, o código obedece

O projeto nasceu como **documento, não como repositório**. O
[GDD](GDD.md) veio primeiro e cresceu por camadas — classes, elenco de magos,
balanceamento, monetização, identidade visual, pesquisa aplicada.

Disso saiu a regra de processo que sustenta tudo até hoje:

> **Toda decisão de design entra no GDD antes de virar código.**

É o que permite trocar de engine sem perder o jogo — e foi exatamente o que
aconteceu duas vezes (2D → 3D, Phaser → Roblox) sem perder um grama de design.

Um segundo documento, o [EQUIPE.md](EQUIPE.md), mapeou como estúdios reais se
organizaram (Counter-Strike nasceu com 2 pessoas, PUBG com ~35, Apex com ~115) e
traduziu cada cargo — designer de sistemas, engenheiro de simulação, artista 2D,
QA, engenheiro mobile — em um **agente** com papel, conhecimentos, entregas e
fronteiras. É esse roster que o ORQUESTRADOR aciona.

### Como o trabalho é feito

O padrão que se provou em todas as fases:

1. **O coordenador escreve os contratos primeiro** — tipos, interfaces, dados de
   balanceamento, protocolo de rede. Nada de feature antes disso.
2. **Workers em paralelo, com raias de arquivos disjuntas.** Nenhum arquivo tem
   dois autores. Se duas raias precisam do mesmo contrato, ele é decidido antes.
3. **Verificação fechada antes de declarar pronto** — typecheck, build, testes.
4. **Integração e síntese pelo coordenador**, nunca um painel de opiniões soltas.

Resultado medido: na Fase 1, ~3.500 linhas integradas com **3 correções de tipo**.
Na missão Roblox, 6 workers em 2 levas com **zero colisão de arquivos**.

---

## 3. Linha do tempo

### 17/08/2026 — Fase 1: o protótipo 2D (`v0.1`)

Um jogo completo em **Phaser 3 + TypeScript + Vite**, top-down: boot com splash e
menus, configurações persistidas em 4 abas, arena 60×60 com biomas, mago jogável
com os 5 elementos, terreno reativo funcionando, escudo evolutivo, 5 bots com
máquina de estados e HUD completo. Arte e áudio **100% procedurais** — zero asset
externo, zero problema de licença.

Não foi validado "olhando e achando bonito": cada sistema passou por **sondas
determinísticas** no console contra o estado real do jogo — 11 verificações só de
terreno, deslocamento medido em pixels por segundo, condução elétrica conferida
célula a célula.

**A lição que custou tempo:** o fogo carbonizava o mapa inteiro. A regra virou
"fogo consome cobertura (floresta e grama alta), nunca campo aberto" — preservando
a intenção de design do GDD.

### 17/08 — Fase 2: Android (`v0.1.1`)

Pipeline web→APK via **Capacitor**, projeto Android com orientação travada, e a
camada de **controles de toque**: joystick, botões com cooldown desenhado,
carrossel de elementos, dois esquemas de mira, layout editável e escalável.

### 17/08 — Projeto Prisma, leva 1 (`v0.1.2`)

Salto de qualidade gráfica em 4 raias paralelas: iluminação dinâmica com bloom,
autotiling do terreno por bitmask, personagens com rig procedural (idle
respirando, manto com inércia, sombra, squash & stretch) e VFX com juice
(hitstop, onda de choque, screen shake por trauma).

O diagnóstico que orientou a leva continua válido: **o salto visual não vem de
desenhar melhor, vem de pipeline de renderização.**

### 17/08 — O primeiro teste real, e a virada

O APK foi compilado e testado em aparelho físico. O veredito mudou o rumo do
projeto:

- Graficamente, ainda longe do aceitável — **"precisa avançar muito, avançar a
  nível 3D, mesmo que custe tempo e trabalho. Visual é tudo para jogadores."**
- Mecanicamente, dois erros graves de toque: a troca de elemento não respondia, e
  mirar e atirar eram dois gestos separados — inviável em celular.

Isso puxou o gatilho de reavaliação de engine que o próprio
[PROJETO_PRISMA](PROJETO_PRISMA.md) previa. O documento foi reescrito (v2) com a
régua estética explícita ("nível Spell Arena", virada em tabela verificável), o
style guide 3D e as rotas possíveis.

### 17/08 — A decisão de plataforma

Avaliadas quatro rotas, a escolha foi **construir primeiro no Roblox**, com um
enquadramento honesto:

- **O que se ganha:** o item mais caro do roadmap — multiplayer autoritativo,
  salas, hospedagem, distribuição — vem pronto. Custo de infra zero. E o pilar
  Sintonia **só pode ser validado com gente de verdade**: bots não provam que
  jogar em dupla é divertido.
- **O que se aceita:** nada técnico migra de lá (Luau e assets são proprietários),
  o teto visual fica abaixo da régua premium, e a plataforma fica com ~75% da
  receita.
- **O enquadramento correto:** o Roblox **não é rampa** para o jogo próprio. É um
  **produto paralelo de validação**. O jogo-troféu (first-person Android, régua
  Spell Arena) continua sendo o destino — e começa quando a validação fechar.

Referência visual escolhida: **Pixel Gun 3D**. Não por ser bonito, mas por ser
*legível* e nativo da plataforma — blocos são baratos de produzir e o público do
Roblox já ama esse look.

### 18/08 — A missão Roblox (R0 → R4)

Executada em modo autônomo, 10 agentes especialistas em 3 levas paralelas.

| Fase | Entrega |
|---|---|
| **R0** | Projeto Rojo (código Luau versionado em git) e os contratos: balanceamento portado do 2D, os 5 elementos com cor + forma, a matriz dos 10 combos, a grade do terreno e o protocolo de rede |
| **R1** | Arena com biomas, terreno reativo completo, combate autoritativo, controles de um gesto, HUD e VFX |
| **R2** | Sintonia: os 10 combos com dano em área e reação de terreno, janela, canalização, cooldown compartilhado e Ping de Sintonia |
| **R3** | Loop de battle royale: lobby, zona em 5 fases, eliminações, bots de preenchimento e Selo do Campeão |
| **R4** | Correções do teste do Diretor: comandos básicos de BR, presença do personagem, atmosfera e água jogável |

---

## 4. Estado atual (18/08/2026)

**Dois produtos vivos, um único GDD:**

| | Protótipo 2D (Phaser) | Campo de Provas (Roblox) |
|---|---|---|
| Papel | laboratório de mecânica (histórico) | **produto de validação — foco atual** |
| Estado | completo, jogável, APK compilado | completo, jogável, nunca testado por terceiros |
| Código | ~8.200 linhas TypeScript | ~8.600 linhas Luau |

### O que funciona no Campo de Provas

- **Combate 100% autoritativo no servidor.** O cliente envia intenção; dano,
  mana, cooldown, colisão e estado do mapa são decididos no servidor. Projéteis
  têm tempo de viagem — nada de hitscan (regra do GDD).
- **Terreno reativo completo.** A floresta foi calibrada acima do limiar de
  percolação (238 de 282 árvores contíguas) — sem isso o incêndio morre em
  bolsões e o pilar não acontece. Congelar o lago **ergue a lâmina d'água** até o
  nível do chão: a ponte é literal, não pintura.
- **Sintonia, os 10 combos**, com reação de terreno coerente com a fantasia de
  cada fusão e o Ping para combinar sem microfone.
- **Loop de BR** com zona fechando, bots de preenchimento e Selo do Campeão.
- **Comandos básicos completos:** Espaço pula, Shift corre, C agacha, Q esquiva,
  1–5 ou roda trocam elemento; no celular, botões próprios e o gesto único de
  arrastar-mirar-soltar.
- **Presença visual:** câmera próxima (8–16 studs), avatar 1,3× com chapéu, manto
  e cajado em blocos, atmosfera de entardecer arcano com bloom, e efeito próprio
  e grandioso para a Sintonia.
- **Zero asset de terceiros.** Tudo montado por código — política de qualidade e
  de segurança.

### Como isso é verificado

Não há QA humano, então o rigor mora em ferramenta:

- `rojo build` gera o arquivo do lugar; **`luau-compile` checa a sintaxe** dos 22
  arquivos (o `rojo build` empacota mas não compila — descobrir isso mudou a
  qualidade da missão).
- **Autotestes embutidos**: `Combat.selfTest()`, `Sintonia.selfTest()`,
  `Match.selfTest()` e `Terrain.selfTest()` — este último faz busca em largura
  no mapa construído e **reprova o mapa se qualquer célula prender o jogador**.
  Foi ele que encontrou 107 bolsões de floresta sem saída onde o sorteio de
  spawn podia largar um jogador preso a partida inteira.

**O que nenhuma ferramenta prova: se é divertido.** Playtest é humano.

---

## 5. As perguntas que o Campo de Provas existe para responder

| | Pergunta | Como se responde |
|---|---|---|
| V1 | A **Sintonia** é divertida entre dois jogadores? Vale o risco de ficar seco? | 2 jogadores reais |
| V2 | O **TTK** (~1,5–2,5s) está certo com humanos mirando? | duelos parelhos |
| V3 | **Queimar a floresta / congelar o lago** vira jogada de verdade? | observar se acontece sem ser pedido |
| V4 | Algum **elemento** domina os outros? | telemetria de uso e abates |
| V5 | O **gesto único de mira** resolveu o problema do celular? | teste em aparelho |

Todo ajuste dessas respostas é editar **um arquivo de dados**
(`roblox/src/shared/Balance.luau`), nunca caçar constante no código.

---

## 6. O que falta

**Curto prazo (depende do playtest):** calibrar TTK, força do combo, velocidade
do incêndio e ritmo da zona.

**Pendências técnicas conhecidas e registradas:**
- Não há **times/squads** — dois jogadores hostis conseguem disparar um combo
  entre si e queimar o cooldown de ambos.
- **Atordoamento** dos combos elétricos exige um sistema de status no combate.
- Bots limitados a 11 até haver medição de performance em aparelho fraco.
- Jogador eliminado ainda consegue conjurar do poleiro de espectador.
- Fontes da identidade visual (Cinzel, Chakra Petch) exigem upload pelo Diretor.

**Médio prazo:** retratos dos magos e key art (independem de engine), o primeiro
mago do folclore brasileiro (Curupira) como bandeira, publicação no Roblox — que
é **ato do Diretor**, nunca de um agente, porque exige login.

**Longo prazo:** o produto premium first-person no Android, na régua Spell Arena,
que só começa quando a validação fechar.

---

## 7. Armadilhas conhecidas (leia antes de mexer)

Registro do que já custou tempo — vale mais que qualquer documentação de API:

1. **`rojo build` não compila Luau.** Empacota. O portão real é `luau-compile`.
   (`luau-analyze` não serve: sem os tipos do Roblox, acusa `game` como global
   desconhecido em todo arquivo.)
2. **O servidor reescreve `WalkSpeed` todo frame.** Qualquer velocidade mudada só
   no cliente é apagada no frame seguinte — corrida e agachamento precisam ser
   autoritativos.
3. **`JumpPower` sem `UseJumpPower = true` é ignorado** no Roblox atual.
4. **Não sinkar o Espaço.** Foi assim que o pulo sumiu: a esquiva engoliu o input
   nativo, e o lago virou armadilha.
5. **Água bloqueia o andar, mas não pode bloquear o tiro** — senão todo projétil
   explode na margem e a validação V3 morre.
6. **O terreno nunca aplica dano.** Ele informa o perigo; quem aplica é o combate.
   Fronteira única para escudo, i-frames e números na tela funcionarem.
7. Do protótipo 2D: `as const` gera tipos literais; `ArenaScene` sobrescreve
   `this.scene`; teclas usam `KeyboardEvent.code` cru.

---

## 8. Mapa dos documentos

| Documento | Para quê |
|---|---|
| [GDD.md](GDD.md) | **Fonte da verdade do produto.** Classes, magos, balanceamento, Sintonia, terreno, monetização, roadmap |
| [ROBLOX.md](ROBLOX.md) | Plano e perguntas de validação da versão Roblox |
| [PROJETO_PRISMA.md](PROJETO_PRISMA.md) | Estratégia visual: régua de qualidade, style guide 3D, rotas de engine |
| [COMO_JOGAR.md](COMO_JOGAR.md) | Como abrir, controles e o que observar no teste |
| [EQUIPE.md](EQUIPE.md) | Blueprint de estúdio: cargos → agentes |
| [HISTORICO.md](HISTORICO.md) | Como o protótipo 2D foi construído e suas armadilhas |
| [PROMPT_DEEP_RESEARCH.md](PROMPT_DEEP_RESEARCH.md) | Prompt para pesquisa de mercado externa |
| [../CHANGELOG.md](../CHANGELOG.md) | Toda versão, em ordem |

---

*Projeto pessoal em desenvolvimento. Direção de jogo: Vinicius.
Execução: ORQUESTRADOR + equipe de agentes especialistas.*
