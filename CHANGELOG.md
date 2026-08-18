# CHANGELOG — Arkana

Cada versão do protótipo documentada (regra do GDD, seção 15).

## v0.2.0-roblox — 2026-08-18 — "Campo de Provas" no Roblox (R0–R3)

Primeira versão da vertente **Roblox** (produto de validação — GDD §9 "Decisões
travadas", plano em `docs/ROBLOX.md`). Terceira pessoa, estética blocky
(referência Pixel Gun 3D), servidor autoritativo. Código em `roblox/`,
sincronizado por **Rojo**; `rojo build` gera o arquivo do lugar.

**R0 · Fundação** — projeto Rojo, `Shared/Balance` (porte fiel dos números do
protótipo 2D + conversão px→studs), `Shared/Elements` (5 elementos com cor +
FORMA e a matriz dos 10 combos), `Shared/Grid` (materiais imutáveis, estados
mutáveis) e `Shared/Net` (remotes com sanitização — o cliente manda intenção,
nunca dano).

**R1 · Os pilares**
- Arena 60×60 determinística: lago de 205 células contíguas, floresta com
  densidade **acima do limiar de percolação** (238 de 282 árvores conectadas —
  abaixo disso o incêndio morre em bolsões e o pilar do GDD §14 não acontece).
- Terreno reativo completo: fogo propaga e derruba a copa, lago **congela
  erguendo a lâmina até o chão** (ponte literal), raio eletrocuta toda a água
  conectada, terra ergue muro, água+terra vira lamaçal.
- Combate 100% autoritativo: projéteis com tempo de viagem (zero hitscan),
  mana/cooldown/dano decididos no servidor, Escudo Evolutivo 1→4, i-frames.
- **Controles com mira e disparo em UM gesto** (correção da reprovação do teste
  em aparelho real): arrasta do botão da magia e solta; toque curto atira
  rápido; voltar ao centro cancela. Carrossel de elementos com alvos de 58dp.
- HUD com nível de escudo em número **e** pips (acessibilidade além da cor),
  Selo de Arkana, VFX blocky, números de dano e i18n PT-BR/EN.

**R2 · Sintonia (o pilar de inovação, GDD §9)** — os 10 combos com dano em área
e reação de terreno coerente; janela de 1,5s, canalização interrompível de 1s e
**cooldown compartilhado cobrado no início** (combo interrompido não devolve o
custo — é o que impede spam); Ping de Sintonia (GDD §18.7) para combinar sem
microfone; mana dos dois conjuradores drenada.

**R3 · Loop de battle royale** — `Lobby → Playing → Ended`, zona arcana em 5
fases com dano crescente, eliminações com crédito de abate, **bots de
preenchimento** montados em blocos por código (FSM do protótipo: vagar,
perseguir, atacar, fugir do fogo) e **Selo do Campeão** com abates, dano,
combos e elementos usados.

### Verificação

`rojo build` limpo e **19 arquivos Luau sem erro de sintaxe** (`luau-compile`).
Autotestes embutidos: `Combat.selfTest()`, `Sintonia.selfTest()`,
`Match.selfTest()`. **Playtest é humano** — nada foi jogado ainda.

### Limitações conhecidas

- Sem times: dois jogadores hostis podem disparar um combo entre si (queima o
  cooldown de ambos). Resolve-se quando houver squads.
- Atordoamento dos combos elétricos pendente (exige `Combat.applyStatus`).
- Bots limitados a 11 até haver medição de performance em aparelho fraco.
- Jogador eliminado ainda consegue conjurar do poleiro de espectador.
- Fontes Cinzel/Chakra Petch do GDD §10 exigem upload pelo Diretor.

## v0.1.2 — 2026-08-17 — PRISMA-1: fundação de render

Primeira leva do Projeto Prisma (docs/PROJETO_PRISMA.md §3), executada em
4 raias paralelas sobre o contrato `RenderModule` (`src/render/contract.ts`),
fiado na ArenaScene. Só a camada de apresentação mudou — simulação,
balanceamento e contratos intactos.

1. **Luz & pós-processamento** (`src/render/lighting.ts`) — entardecer arcano
   (Light2D + luz ambiente), luzes dinâmicas com pool por distância (projéteis
   na cor do elemento, fogo/eletricidade pulsantes, cajado dourado com
   flicker), Bloom + vinheta + color grading na câmera. No-op gracioso em
   Canvas; reage à mudança de qualidade ao vivo.
2. **Pele do terreno** (`src/render/terrainTextures.ts` + `terrainSkin.ts` +
   TerrainGrid visual) — autotiling por bitmask com bordas onduladas
   (água>areia>terra>grama), 4 variantes por tile a 64px (RT 2×, mundo lógico
   intacto), decals determinísticos, água com ondulação e espuma na borda
   (gelo congela a animação — estado legível), praia em terra vizinha de água.
3. **Personagens vivos** (`src/render/characterRig.ts` + `characterTextures.ts`
   + entidades) — sprites 64px, idle respirando, bob de passo, inclinação na
   direção do movimento, manto em 2 segmentos com inércia, sombra elíptica,
   squash & stretch na esquiva; identidade de elemento dos bots preservada.
4. **VFX & juice** (`src/render/vfx.ts` + `vfxTextures.ts` + Projectile
   visual) — trilhas por elemento (cor+forma), impacto com onda de choque +
   flash + hitstop (40–60ms com cooldown) + recuo de câmera, fogo do terreno
   em 3 camadas (chama + brasas + fumaça), pólen ambiental, screen shake por
   trauma decaindo.

Tudo com gate pela config de Qualidade (Baixa/Média/Alta) — antecipando o
PRISMA-4 (mobile). Validado em runtime: WebGL, 10 luzes ativas, 3
post-pipelines, zero texturas ausentes, zero erros de console.

### Limitações conhecidas

- RT do terreno usa 3840×3840px — GPUs com `MAX_TEXTURE_SIZE < 4096`
  precisarão de fallback 1× (PRISMA-4).
- Teto de 10 luzes simultâneas (default do Phaser) — subir exige config em
  `main.ts` (PRISMA-4).
- Feel do hitstop/shake e densidade do fogo merecem ajuste fino com controle
  humano (sondas validam presença e orçamento, não gosto).

## v0.1.1 — 2026-08-17 — Fase 2: pipeline Android + controles de toque

Entrega o escopo da Fase 2 (GDD seções 19.3–19.5), executada em 2 raias paralelas:

1. **Pipeline web→APK (Capacitor 8.5)** — `capacitor.config.ts`
   (`br.com.vstack.arkana`), projeto nativo em `android/` com orientação
   landscape travada e tela cheia, script `npm run build:android` e guia
   completo de compilação em `docs/BUILD_ANDROID.md`.
2. **Controles de toque (GDD 19.3)** — `src/ui/TouchControls.ts`: joystick
   virtual (zona esquerda), mira por arrasto (zona direita), botões de
   Ataque (segurar = fogo contínuo), Tática e Esquiva com cooldown desenhado
   no próprio botão, carrossel dos 5 elementos (cor + forma), botão de pausa
   e alvos ≥ 48dp.
3. **Dois esquemas de mira** — Simples (assistida por cone) e Avançado
   (manual), escolhíveis em Configurações › Controles.
4. **Layout editável e escalável** — slider de escala (0.7–1.5) e modo
   "Editar layout" (`src/scenes/TouchLayoutScene.ts`) com arrastar-e-soltar,
   persistência e "Restaurar padrão".
5. **Ativação Auto/Ligado/Desligado** — auto-detecção por `maxTouchPoints`;
   "Ligado" permite testar toque no desktop. Com toque desligado, o desktop
   permanece 100% intacto.
6. **HUD mobile** — painéis realocados para zonas seguras dos polegares;
   `index.html` endurecido para WebView (viewport fixo, sem long-press/zoom).

### Limitações conhecidas

- **APK não compilado nesta máquina** — falta JDK/Android SDK; o passo a
  passo está em `docs/BUILD_ANDROID.md` (o build é 1 comando após instalar).
- Mudanças de modo/escala/layout de toque aplicam ao (re)entrar na arena.
- Ergonomia real dos polegares deve ser conferida no aparelho físico.

## v0.1.0 — 2026-08-17 — Protótipo "Campo de Provas" (Fase 1)

Primeira versão jogável. Entrega o Definition of Done da seção 15 do GDD:

1. **Boot completo** — splash do estúdio → carregamento (key art + logo +
   barra dourada + dicas rotativas) → título → menu.
2. **Menu principal** navegável com música gerativa e fundo animado
   (partículas elementais + Selo de Arkana).
3. **Configurações funcionais e persistidas** — todas as abas da seção 12
   (Vídeo, Áudio, Controles com remapeamento, Jogo com PT-BR/EN), com
   "Restaurar padrão" por aba.
4. **Arena top-down 60×60 tiles** com biomas: lago, floresta, grama alta,
   rochas e campo aberto.
5. **1 mago jogável (Evocador)** — WASD + mira no mouse, ataque básico (M1),
   magia tática (M2) e esquiva com i-frames curtos (ESPAÇO).
6. **5 elementos no Q** — Fogo, Água, Terra, Vento e Raio, cada um com
   projétil, cor, ícone e som próprios (regra de acessibilidade cor+forma).
7. **Terreno reativo** — árvore queima e propaga; lago congela (ponte) e
   eletrocuta com Raio; água + terra = lamaçal; Terra ergue muro de pedra
   destrutível (tabela da seção 14).
8. **Escudo de Magia Evolutivo** — dano causado acumula e sobe o nível
   (1→4, branco→azul→roxo→dourado) com banner e flash no HUD.
9. **5 bots de treino + 1 dummy** — bots com FSM (vagam, perseguem, atacam
   com erro de mira e FOGEM do fogo — reagem ao terreno como o player);
   dummy imóvel com números de dano e janelinha de DPS que reseta sozinha.
10. **HUD completo** — vida, escudo (com progresso de evolução), mana,
    carrossel do elemento ativo + cooldowns de tática/esquiva, minimapa com
    terreno e entidades, Selo de Sintonia decorativo e números de dano.
11. **Pausa (ESC) + contador de FPS** (ligável nas configurações de vídeo).

### Limitações conhecidas

- **Settings em `localStorage`** — equivalente web do `settings.json`; o
  arquivo real chega no empacotamento Tauri da Fase 2.
- **VSync e resolução** efetivamente controlados pelo navegador; as opções
  existem e persistem, mas o navegador tem a palavra final.
- **Sintonia decorativa** — o Selo no topo do HUD é placeholder do medidor
  real de Conjuração Combinada (v0.2, com 1 bot aliado).
- **Suprema reservada** — a tecla R está mapeada nas configurações, mas a
  magia suprema é escopo do v0.2.

### Fora do escopo (anotado para o v0.2)

Sintonia jogável, queda do castelo, multiplayer, cosméticos e passe — ver GDD
seção 15 ("Fora do escopo do v0.1").
