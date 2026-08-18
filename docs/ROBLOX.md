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

## 6. Definition of Done da missão

- [ ] R0, R1, R2, R3 completas, cada uma commitada com CHANGELOG atualizado.
- [ ] `rojo build` gera `.rbxlx` sem erro e a estrutura confere.
- [ ] Análise estática Luau sem erros; nenhum RemoteEvent confia no cliente.
- [ ] `docs/ROBLOX.md` (este arquivo) atualizado com o que mudou de verdade.
- [ ] Memória da equipe atualizada (`_memoria/arkana.md` + `LICOES.md`).
- [ ] Relatório final ao Diretor: como abrir no Studio, o que testar, o que
      ficou pendente e o que exige a conta dele (publicar).

## 7. O que NENHUM agente faz (fronteira dura)

- **Login/publicação no Roblox** — exige credenciais do Diretor. Os agentes
  entregam o arquivo do lugar pronto; publicar é ato humano.
- Instalar plugins de terceiros no Studio além do **plugin oficial do Rojo**.
- Usar modelos/scripts da Toolbox (§2).
