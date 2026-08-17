# HISTÓRICO DO PROJETO — como o Arkana foi construído

> Registro técnico e cronológico. Serve para (a) retomar o trabalho em qualquer máquina ou
> sessão futura sem re-descobrir nada e (b) preservar as decisões e as armadilhas que
> custaram tempo. Complementa o GDD (que é o *quê*) explicando o *como* e o *porquê*.

---

## Linha do tempo

### Antes do código — o documento primeiro
O projeto nasceu como GDD, não como repositório. A ideia partiu de "Apex Legends traduzido
para fantasia" e foi crescendo por camadas: as 5 classes, o mapa completo de lendas→magos,
o elenco de lançamento com limitadores de balanceamento, a Conjuração Combinada, o terreno
reativo, a identidade visual, a pesquisa aplicada (Spellbreak, Noita, BotW), as escolas de
magia, a Escola do Folclore Brasileiro e o Juramento anti-P2W.

Uma regra de processo foi estabelecida e mantida: **toda decisão de design entra no GDD
antes de virar código. O documento manda, o código obedece.** É o que permite trocar de
engine sem perder o jogo.

Um segundo documento — o blueprint de estúdio ([EQUIPE.md](EQUIPE.md)) — mapeou como
estúdios reais se organizaram (Counter-Strike com 2 pessoas, PUBG com ~35, Apex com ~115) e
traduziu cada cargo em um agente com papel, conhecimentos, entregas e fronteiras.

### 17/08/2026 — Fase 1: do zero ao protótipo jogável em uma sessão

Executada com o padrão **coordenador + 4 subagents em paralelo**, cada um encarnando um
cargo do blueprint (UX/UI, Engenharia de Simulação, Programação de Gameplay, Bots+HUD).

O que tornou a paralelização possível sem colisão:

1. **Contratos primeiro.** O coordenador escreveu antes de qualquer feature:
   `types.ts` (interfaces `ITerrainGrid` e `ArenaApi`, enums, eventos), `config.ts`,
   `balance.ts`, `strings.ts`, `settings.ts`, `audio.ts`, `textures.ts` e as 4 cenas
   pequenas de boot. Os workers programaram *contra as interfaces*, não uns contra os outros.
2. **Raias de arquivos disjuntas.** Cada worker era dono exclusivo de um conjunto de
   arquivos. Nenhum arquivo teve dois autores.
3. **Typecheck isolado por raia** assim que cada reporte chegava — erro encontrado cedo é
   barato.

Resultado: ~3.500 linhas integradas com **apenas 3 correções de tipo**, typecheck limpo e o
jogo rodando a 60 FPS no primeiro teste real.

---

## Arquitetura

```
src/
├─ core/          contratos e dados — o "sistema nervoso"
│  ├─ types.ts        interfaces ITerrainGrid e ArenaApi, enums, eventos (EVT)
│  ├─ config.ts       dimensões, paleta, chaves de cena/textura, profundidades
│  ├─ balance.ts      TODOS os números de jogo (dano, mana, cooldowns, terreno, bots)
│  ├─ strings.ts      i18n PT-BR/EN — nenhum texto solto no código
│  ├─ settings.ts     configurações persistentes + notificação de mudança
│  ├─ audio.ts        SFX e música sintetizados em WebAudio (zero arquivos)
│  └─ textures.ts     toda a arte gerada proceduralmente em runtime
├─ terrain/       simulação do terreno reativo (autômato celular)
├─ entities/      mago jogável, bots, dummy, pool de projéteis
├─ scenes/        Splash → Loading → Title → Menu → Settings/Credits → Arena ⇄ Pause
└─ ui/            HUD de combate
```

**Princípio que sustenta tudo:** *design antes de código; números viram dados; o código lê
dados.* Rebalancear o jogo é editar `balance.ts` — nunca caçar constantes espalhadas.

---

## Decisões técnicas registradas

| Decisão | Motivo |
|---|---|
| **Phaser 3 + TypeScript + Vite** | Motor 2D maduro, roda no navegador, zero curva de linguagem. O GDD já declara o código do protótipo descartável e o documento portátil. |
| **Arte e áudio 100% procedurais** | Nenhuma dependência de asset externo, nenhum problema de licença, protótipo autocontido. Substituíveis por arte autoral sem tocar na lógica. |
| **Grade lógica de 32px, arena 60×60** | Terreno reativo por células é barato; por pixel (estilo Noita) é referência de design, não de tecnologia mobile. |
| **Configurações em localStorage** | Equivalente web do `settings.json` do GDD. Migra para arquivo real no empacotamento Tauri/Capacitor. |
| **Fogo não se propaga pela grama rasteira** | Descoberto no teste real: o incêndio carbonizava o mapa inteiro. A regra passou a ser "fogo consome cobertura (floresta e grama alta), não o campo aberto" — preserva a intenção de design do GDD seção 14. |
| **Manter Phaser em vez de migrar para Godot** | O que limita o visual hoje é o uso do motor, não o motor. Avaliação completa em [PROJETO_PRISMA.md](PROJETO_PRISMA.md). |

---

## Armadilhas conhecidas (leia antes de mexer)

1. **`as const` em `balance.ts` gera tipos literais.** Um campo iniciado com
   `BAL.player.hp` é inferido como o literal `100`, não `number`. Anote `: number`
   explicitamente em qualquer campo mutável inicializado a partir de `BAL.*`.

2. **`ArenaScene` sobrescreve `this.scene`.** O contrato `ArenaApi` exige uma propriedade
   `scene: Phaser.Scene`, mas o Phaser injeta o `ScenePlugin` exatamente nesse nome. A cena
   preserva o plugin em `this.scenePlugin` e aponta `scene` para si mesma.
   **Regra: `pause`, `launch` e `restart` SEMPRE via `this.scenePlugin`.**
   É o ponto mais frágil do código — funciona e foi testado, mas exige atenção.
   *Lição para contratos futuros: verificar se o nome da propriedade colide com a classe-base
   antes de escrever a interface.*

3. **Teclas usam `KeyboardEvent.code`, não `Phaser.KeyCodes`.** As configurações guardam
   `'KeyW'` / `'Space'`; o `addKey()` do Phaser espera `'W'` / `'SPACE'` e falharia
   silenciosamente. O input lê `ev.code` cru e compara com o que está salvo.

4. **`window.__ARKANA_GAME`** expõe a instância do jogo para inspeção pelo console do
   navegador — é o gancho usado para testar sistemas de forma determinística (estado do
   terreno, escudo, cooldowns) em vez de julgar por screenshot.

---

## Como foi testado

O teste não foi "olhar e achar bonito". Cada sistema foi verificado por **sondas
determinísticas** rodando no console do navegador contra o estado real do jogo:

- **Movimento:** deslocamento medido em pixels por segundo, batendo com o valor do balance.
- **Terreno reativo (11 verificações):** congelamento do lago produzindo célula andável,
  condução elétrica na água conectada, formação de lamaçal, muro com 200 de vida
  bloqueando passagem, ignição de árvore, propagação na floresta e — após a correção —
  ausência de propagação na grama rasteira.
- **Escudo evolutivo:** níveis e capacidades conferidos contra a tabela do GDD seção 5.
- **Esquiva:** cooldown disparado e deslocamento do dash medidos.
- **Bots:** distância percorrida ao fugir do fogo, comprovando a reação.
- **Performance:** 60 FPS estáveis com incêndio ativo, 5 bots e minimapa; zero erros de
  console durante toda a sessão.

Uma lição de método ficou registrada: durante o QA, o navegador de teste é compartilhado
com o jogador humano. Antes de injetar input sintético, verificar se vida/mana/posição
mudam sozinhos — se mudam, há alguém jogando, e o certo é observar em vez de dirigir.

---

## Ambiente de desenvolvimento

- **Node.js:** o projeto usa Node 18+. Na máquina de origem o Node não estava no PATH; o
  contorno foi usar o binário que acompanha o driver do Playwright, com o npm baixado para
  um diretório temporário. Em máquina nova, basta instalar o Node normalmente.
- **Servidor de desenvolvimento:** `npm run dev` (Vite, porta 5173).
- **Verificação:** `npm run typecheck` deve terminar com zero erros antes de qualquer commit.

---

## Próximos passos

1. **Projeto Prisma** — salto de qualidade gráfica: iluminação dinâmica, pós-processamento,
   autotiling do terreno, personagens animados. Estratégia completa e ordem de execução em
   [PROJETO_PRISMA.md](PROJETO_PRISMA.md). *Planejado, ainda não iniciado.*
2. **Fase 2 (GDD 19.5)** — APK Android via Capacitor + controles de toque.
3. **Fase 3 (GDD 19.5)** — v0.2: Sintonia jogável com bot aliado, tela de queda, Selo do
   Campeão e Presságios.

O que **não** muda: o GDD continua sendo a fonte da verdade, e qualquer ambiguidade vira
pergunta ao Diretor de Jogo — nunca invenção silenciosa.
