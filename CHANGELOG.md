# CHANGELOG — Arkana

Cada versão do protótipo documentada (regra do GDD, seção 15).

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
