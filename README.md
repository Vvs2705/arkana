# ARKANA — Magos Battle Royale

> **Caia do castelo voador, domine os 5 elementos e seja o último mago de pé.**

Battle royale de magos em primeira pessoa na visão completa; hoje, um protótipo
jogável top-down em 2D que valida o combate elemental e o **terreno reativo**.

**Estado atual:** `v0.1 — Campo de Provas` · protótipo PC completo e testado ·
Fase 1 do roadmap concluída em 17/08/2026.

---

## O que é este projeto

Arkana pega o esqueleto comprovado do gênero battle royale e troca as armas de fogo
por magia — mas a aposta não é a temática, e sim duas invenções mecânicas que os
concorrentes não têm:

1. **Conjuração Combinada ("Sintonia")** — dois magos conjurando no mesmo alvo dentro de
   uma janela de 1,5s fundem as magias numa Magia Combinada mais forte que a soma das
   partes (10 pares elementais). É o motivo mecânico para jogar em squad.
2. **Terreno Reativo** — o mapa não é cenário, é recurso. Fogo se propaga pela floresta e
   apaga a cobertura; água congela o lago e cria uma ponte; raio eletrocuta toda a água
   conectada; terra ergue muros destrutíveis. Arquitetura inspirada nas 3 regras do
   *chemistry engine* de Breath of the Wild.

O documento que manda no projeto é o **[GDD](docs/GDD.md)** — toda decisão de design entra
lá antes de virar código.

---

## Jogar

**Modo mais simples — clique duplo em `JOGAR.bat`.** Ele encontra o Node, instala as
dependências na primeira vez, sobe o servidor e abre o jogo no navegador sozinho. Para
encerrar, feche a janela preta do terminal.

### Pela linha de comando

Requisitos: Node.js 18+ e npm.

```bash
npm install
```

```bash
npm run dev
```

Abre em `http://localhost:5173`. Outros comandos:

```bash
npm run typecheck
```

```bash
npm run build
```

### Controles

| Ação | Tecla |
|---|---|
| Mover | `W` `A` `S` `D` |
| Mirar | mouse |
| Ataque básico | botão esquerdo (consome mana) |
| Magia tática | botão direito (cooldown) |
| Esquiva com i-frames | `Espaço` |
| Trocar elemento | `Q` |
| Pausa | `Esc` |

Todas as teclas são remapeáveis em Configurações › Controles.

---

## O que já funciona (v0.1)

- Boot completo: splash do estúdio → carregamento com dicas que ensinam a química do
  jogo → tela de título → menu principal
- Configurações completas em 4 abas (vídeo, áudio, controles com remapeamento, jogo com
  PT-BR/EN e modo daltonismo), persistidas entre sessões
- Arena de 60×60 células com lago, floresta, grama alta, rochas, campo aberto e estrada
- Um mago jogável com os 5 elementos (Fogo, Água, Terra, Vento, Raio) — cada um com cor,
  **forma de projétil** e som próprios (acessibilidade: nunca só cor)
- Terreno reativo funcional: propagação de incêndio, lago que congela e vira rota,
  eletrocussão por condução na água, lamaçal, muro de pedra destrutível
- Escudo de Magia Evolutivo (níveis 1→4) que sobe conforme o dano causado
- 5 bots com máquina de estados (vagar, perseguir, atacar, **fugir do fogo**) + dummy de
  treino com números de dano e leitura de DPS
- HUD completo: vida, escudo, mana, carrossel de elementos, cooldowns, minimapa vivo,
  Selo de Sintonia e contador de FPS
- 60 FPS estáveis com incêndio florestal e 5 bots ativos

Arte e áudio são **100% procedurais** (geradas em runtime por código) — zero assets
externos. Ver [docs/CREDITS.md](docs/CREDITS.md).

---

## Stack

**Phaser 3** · **TypeScript** (strict) · **Vite** — escolha deliberada: motor 2D maduro,
roda no navegador, e o protótipo é descartável por definição enquanto o GDD é portátil.
A avaliação de engine e o gatilho para reconsiderar Godot estão em
[docs/PROJETO_PRISMA.md](docs/PROJETO_PRISMA.md).

---

## Documentação

| Documento | Conteúdo |
|---|---|
| [docs/GDD.md](docs/GDD.md) | **Fonte da verdade** do produto: classes, magos, balanceamento, monetização, roadmap |
| [docs/HISTORICO.md](docs/HISTORICO.md) | Como o protótipo foi construído, decisões técnicas e armadilhas conhecidas |
| [docs/PROJETO_PRISMA.md](docs/PROJETO_PRISMA.md) | Estratégia para elevar o gráfico ao nível comercial |
| [docs/EQUIPE.md](docs/EQUIPE.md) | Blueprint de estúdio: setores, cargos e o roster de agentes por fase |
| [docs/ART.md](docs/ART.md) | Paleta, tipografia e regras visuais |
| [docs/AUDIO.md](docs/AUDIO.md) | Direção sonora e síntese procedural |
| [docs/CREDITS.md](docs/CREDITS.md) | Todo asset externo e sua licença |
| [CHANGELOG.md](CHANGELOG.md) | Histórico de versões |

---

## Roadmap

- [x] **Fase 1** — Protótipo PC: boot, menus, arena, 5 elementos, terreno reativo, escudo
      evolutivo, bots *(concluída)*
- [x] **Fase 2** — APK Android via Capacitor + controles de toque *(código completo;
      compilar o APK exige Android Studio/JDK 17 — ver [docs/BUILD_ANDROID.md](docs/BUILD_ANDROID.md))*
- [ ] **Fase 3** — v0.2: Sintonia com bot aliado, tela de queda, Selo do Campeão, Presságios
- [x] **Projeto Prisma — PRISMA-1** — fundação de render: iluminação dinâmica, autotiling,
      pós-processamento (bloom/vinheta/grading), personagens animados e VFX/juice
      *(PRISMA-2 a 4 — arte autoral, animação esqueletal e otimização mobile — pendentes)*

---

## O Juramento de Arkana

Política de produto inegociável, registrada no GDD seção 19.1:

> **Este jogo nunca será pay-to-win.** Todo item que afeta poder existe apenas dentro da
> partida, encontrado no loot — nunca vendido, nunca em passe, nunca em caixa. A loja vende
> exclusivamente cosméticos. Habilidade decide, sempre.

---

*Projeto pessoal em desenvolvimento. Direção de jogo: Vinicius.*
