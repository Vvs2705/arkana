# EQUIPE ARKANA — Blueprint de Estúdio + Roster de Agentes para o ORQUESTRADOR

> Documento-fonte para o ORQUESTRADOR criar, do zero, uma equipe de agentes compatível com o projeto Arkana (GDD.md é a fonte da verdade do produto; este arquivo é a fonte da verdade da equipe).

---

## PARTE 1 — Como os estúdios reais se organizaram (referências e lições)

| Jogo | Origem da equipe | Lição para nós |
|---|---|---|
| **Counter-Strike** | Nasceu como mod feito por **2 estudantes** (Minh Le e Jess Cliffe) sobre a engine do Half-Life; a Valve contratou os dois depois | Um jogo lendário pode nascer minúsculo se apoiado em tecnologia pronta — nosso equivalente: Phaser/Godot + o GDD |
| **PUBG** | Brendan Greene criou o gênero como modder (Arma/DayZ); na Bluehole/Krafton o time inicial era de **~35 pessoas**, crescendo para ~70 até o lançamento | O criador não era o melhor programador — era o dono da VISÃO. Equipe pequena + visão clara vence equipe grande sem direção |
| **Apex Legends** | Respawn desenvolveu em segredo com **~115 pessoas** (número citado publicamente), reaproveitando engine e assets de Titanfall | 115 é o custo de um AAA polido no lançamento — E de reuso maciço de tecnologia própria |
| **Fortnite BR** | O modo battle royale foi construído em **~2 meses** por uma equipe interna da Epic que já tinha o jogo-base (Save the World) pronto | Velocidade vem de fundação pronta. Nosso "Save the World" é o protótipo v0.1 |
| **Spellbreak** | Proletariat, estúdio independente de médio porte (dezenas de devs) | Dá para fazer um BR de magos bonito com equipe média — o risco deles foi retenção/negócio, não capacidade técnica |

**Os 3 tamanhos possíveis do "Estúdio Arkana":**
- **A. Estúdio completo (visão AAA-lite): 45–60 pessoas** — o que um publisher montaria para competir de verdade no mobile global.
- **B. Estúdio enxuto (indie profissional): 12–15 pessoas** — o tamanho real que lança um BR mobile funcional (é aproximadamente a faixa de vários BRs mobile menores no mercado).
- **C. Núcleo mínimo viável: 5–7 pessoas** — lança o "Campo de Provas" + arena multiplayer pequena. É o alvo realista se a ideia for vendida/pitchada com protótipo na mão.
- **D. Você + agentes (hoje):** o ORQUESTRADOR simula os papéis da Parte 2 dentro dos limites da Parte 4.

---

## PARTE 2 — Setores, cargos, conhecimentos e experiências (o coração do documento)

> Cada cargo abaixo está escrito para virar um agente: papel, conhecimentos obrigatórios, experiência esperada no mundo real, entregas e fronteiras.

### SETOR 1 — Direção e Produção
**1.1 Diretor(a) de Jogo** *(no nosso caso: VOCÊ — este papel não vira agente)*
- Dono da visão. Decide o que entra e o que corta. Aprova cada fase.
- Experiência real esperada: profundo conhecimento do gênero como jogador + capacidade de decisão. (Brendan Greene é a prova de que isso vale mais que diploma.)

**1.2 Produtor(a) Técnico**
- Conhecimentos: metodologia ágil (sprints, backlog, definition of done), gestão de escopo e risco, leitura de GDD, priorização custo×impacto.
- Experiência real: 3+ anos entregando software em equipe; em estúdios, é quem já "shipou" ao menos 1 jogo.
- Entregas: plano de sprint por fase, CHANGELOG, lista de bloqueios, relatório de fim de fase.
- NÃO faz: decisões de design (propõe, o Diretor decide).

### SETOR 2 — Game Design
**2.1 Designer de Sistemas/Balanceamento**
- Conhecimentos: matemática de jogo (TTK, DPS, curvas de XP, economia de mana), planilhas de balanceamento, teoria de contra-jogada (counterplay), análise de metagame; conhecer profundamente Apex, Spellbreak, Noita e BotW (nossas referências).
- Experiência real: 3–5 anos; já balanceou um jogo PvP com dados de partidas reais.
- Entregas: tabelas de dano/vida/cooldown em formato de dados (JSON/CSV), simulações de duelo, patch notes de balanceamento.

**2.2 Designer de Combate/Gamefeel**
- Conhecimentos: "game feel" (recuo, hitstop, screen shake, tempos de animação em ms), telegrafia de habilidades, legibilidade do caos, input buffering, coyote time e afins.
- Experiência real: já ajustou combate de action game; sabe explicar POR QUE um tiro é "gostoso".
- Entregas: spec de cada magia com tempos exatos (windup/active/recovery), tabela de efeitos de feedback.

**2.3 Level Designer**
- Conhecimentos: fluxo de mapa BR (POIs, rotas de rotação, linhas de visão, cobertura), teoria de encontros, densidade de loot, integração com o terreno reativo (onde ficam florestas queimáveis/lagos congeláveis É decisão de level design).
- Experiência real: já desenhou mapas multiplayer testados com jogadores.
- Entregas: mapa da arena em grid anotado (biomas, POIs, spawns de loot, Círculos de Invocação, Mesas de Transmutação).

**2.4 Designer de UX/UI**
- Conhecimentos: HUD mobile (zonas de polegar, alvos ≥48dp), hierarquia visual, onboarding, acessibilidade (daltonismo), fluxos de menu; Figma.
- Experiência real: 2+ anos em produto mobile (jogo ou app de alto uso).
- Entregas: wireframes de todas as telas, spec do HUD de combate, fluxo de onboarding.

### SETOR 3 — Engenharia
**3.1 Programador(a) de Gameplay** *(o mais importante do protótipo)*
- Conhecimentos: TypeScript avançado, Phaser 3 (cenas, física arcade, tweens, pooling de objetos), padrões ECS/componentes, máquinas de estado (personagem, magias), input handling teclado/mouse/toque.
- Experiência real: 3+ anos; já implementou combate em tempo real; entende frame budget (16ms).
- Entregas: os sistemas do DoD da seção 15 do GDD.

**3.2 Engenheiro(a) de Simulação (terreno reativo)**
- Conhecimentos: autômatos celulares (modelos de propagação de fogo com estados: intacto/queimando/queimado), tilemaps com estado por célula, otimização (dirty rectangles, atualização por chunks), as 3 regras do chemistry engine de BotW como arquitetura.
- Experiência real: física/simulação em jogos ou computação científica; já otimizou loops quentes.
- Entregas: módulo de terreno reativo isolado e testável (a seção 14 do GDD em código).

**3.3 Engenheiro(a) de Netcode/Multiplayer** *(entra na fase multiplayer — o cargo mais raro e caro do mercado)*
- Conhecimentos: servidor autoritativo, client-side prediction + reconciliation, interpolação de entidades, compensação de lag, tick rate, delta compression, interest management (20 jogadores), WebSocket/UDP, salas e matchmaking; validação server-side como base de anti-cheat.
- Experiência real: 5+ anos; JÁ SHIPOU um jogo multiplayer em tempo real (sem isso, é teoria).
- Entregas: servidor de partida, protocolo de rede documentado, ferramenta de simulação de latência.

**3.4 Engenheiro(a) Mobile/Build**
- Conhecimentos: Capacitor (web→APK), ciclo de build Android (Gradle, assinatura de APK/AAB), perfil de performance em WebView (GPU, memória, bateria), tratamento de múltiplas resoluções/notch, requisitos da Play Store.
- Experiência real: já publicou app/jogo na Play Store.
- Entregas: pipeline de build automatizado, APK de teste a cada fase, relatório de FPS por aparelho.

**3.5 Engenheiro(a) de Backend/LiveOps** *(fase de lançamento)*
- Conhecimentos: contas/autenticação, inventário server-side (a loja NUNCA confia no cliente), telemetria de partidas, feature flags, escalabilidade (o seu stack FastAPI+PostgreSQL serve perfeitamente aqui).
- Experiência real: 3+ anos em backend de produto com usuários reais — **este é o setor onde a SUA experiência profissional já cobre o cargo.**

**3.6 QA / Engenheiro(a) de Testes**
- Conhecimentos: planos de teste, testes automatizados (unitários p/ balanceamento e simulação; smoke tests do build), reprodução de bugs, teste de dispositivo real (matriz de aparelhos).
- Experiência real: 2+ anos testando software interativo.
- Entregas: checklist de regressão por fase, relatório de bugs priorizado.

### SETOR 4 — Arte
**4.1 Diretor(a) de Arte**
- Conhecimentos: linguagem visual consistente (shape language, silhuetas legíveis em tela pequena), paleta e iluminação, style guides; capaz de rejeitar arte bonita que prejudica legibilidade de gameplay.
- Experiência real: 5+ anos; já definiu a identidade visual de um produto inteiro.
- Entregas: style guide do Arkana (evolução da seção 10 do GDD), aprovação de todo asset.

**4.2 Especialista 2D** *(o cargo que você pediu detalhado — prioridade do protótipo)*
- Conhecimentos obrigatórios: desenho de sprites e spritesheets, atlas de textura (empacotamento, POT sizes), animação 2D quadro-a-quadro E esqueletal (**Spine** ou **Live2D** — nossa escolha para as apresentações de lobby), pixel art vs vetor e quando usar cada um, exportação multi-densidade (1x/2x/3x para telas diferentes), teoria de silhueta e legibilidade (um mago precisa ser identificável em 40 pixels de altura), UI art (ícones dos 5 elementos, runas, botões), VFX 2D com partículas e sprite animation.
- Ferramentas: Aseprite/Photoshop/Krita, Spine, TexturePacker.
- Experiência real: 3+ anos com jogo 2D lançado; portfólio com animação de personagem.
- Entregas: spritesheets dos magos, tileset da arena (com variações de estado: queimando/queimado/congelado/lama), ícones de UI, animações de lobby.

**4.3 Especialista 3D** *(fase futura — visão primeira pessoa)*
- Conhecimentos obrigatórios: modelagem **low-poly otimizada para mobile** (orçamento de triângulos por personagem: ~5–15k no mobile, sabendo justificar cada mil), topologia limpa para deformação, UV unwrapping eficiente, texturização **PBR** (Substance Painter) com atlas compartilhados, **LODs** (níveis de detalhe), rigging e skinning de humanoides, limites de draw calls e batching, shaders mobile (o que NÃO usar: subsurface caro, transparências empilhadas), pipeline de export para engine (FBX/glTF → Godot/Unity/Unreal).
- Ferramentas: Blender (padrão indie) ou Maya, Substance Painter.
- Experiência real: 3–5 anos; portfólio com personagem game-ready animável rodando em engine, idealmente mobile.
- Entregas: modelos de mago com rig, props do castelo, cenário modular.

**4.4 Tech Artist / VFX**
- Conhecimentos: sistemas de partículas, shaders (GLSL/shader graph) dentro de orçamento mobile, otimização de arte (por que o jogo caiu para 40fps), ponte arte↔engenharia.
- Experiência real: 3+ anos; híbrido raro e valioso.
- Entregas: VFX das magias e combos de Sintonia dentro do teto de partículas da seção 19.2.

### SETOR 5 — Áudio
**5.1 Sound Designer:** síntese e edição (Reaper/Audacity), foley de magias, mixagem para alto-falante de celular (frequências médias!), áudio posicional, implementação na engine. Experiência: 2+ anos com jogo lançado. Entregas: SFX dos 5 elementos, dos 10 combos, UI sounds, o "estalar" da manopla.
**5.2 Compositor(a):** trilha em camadas/stems (a seção 18.5 exige composição vertical), loops perfeitos, música adaptativa. Entregas: tema do menu, stems elementais, tema da queda.

### SETOR 6 — Negócios, LiveOps e Comunidade *(fase de lançamento)*
**6.1 Analista de Dados:** funis de retenção (D1/D7/D30), telemetria de balanceamento (winrate por mago), A/B tests. **6.2 Community Manager:** Discord, redes, creators BR, tradução da voz do jogo. **6.3 Designer de Economia:** precificação cosmética, passe de batalha (seção 7), SEM tocar em poder (Juramento, seção 19.1). **6.4 Jurídico (consultoria externa):** PI, INPI, LGPD, termos de uso, classificação indicativa.

---

## PARTE 3 — Tradução para o ORQUESTRADOR (roster de agentes por fase)

### Template de agente (.md) — usar para TODOS
```
# AGENTE: [nome do cargo]
## Papel
[1 parágrafo — o que este agente É]
## Conhecimentos que deve aplicar
[lista da Parte 2 correspondente]
## Responsabilidades nesta fase
[tarefas concretas da fase atual]
## Entregas (Definition of Done)
[artefatos verificáveis]
## Interage com
[quais agentes consome/alimenta]
## Fronteiras (o que NÃO faz)
[limites explícitos — ex.: "não altera balanceamento sem aprovação do Designer de Sistemas"]
## Fonte da verdade
GDD.md — em ambiguidade, PERGUNTA ao Diretor (o usuário), nunca inventa.
```

### Fase 1 — Protótipo PC (DoD seção 15) → **7 agentes**
1. **Produtor Técnico** (coordena, mantém CHANGELOG, reporta ao Diretor)
2. **Designer de Sistemas** (tabelas de dano/mana/escudo em JSON antes do código)
3. **Designer de Combate** (spec de tempos de cada magia)
4. **Programador de Gameplay Phaser/TS** (personagem, magias, HUD, menus, boot)
5. **Engenheiro de Simulação** (módulo do terreno reativo)
6. **Artista 2D** (placeholders coerentes: paleta da seção 10 + assets CC0 de Kenney adaptados; documenta tudo em CREDITS.md)
7. **QA** (testa cada entrega contra o DoD; roda checklist de regressão)

### Fase 2 — APK Android → **+2 agentes**
8. **Engenheiro Mobile/Build** (Capacitor, APK assinado, relatório de FPS)
9. **Designer de UX Mobile** (controles de toque da seção 19.3, layout editável)

### Fase 3 — v0.2 (Sintonia com bot, queda, Selo, Presságios) → mesmos agentes, + 
10. **Sound Designer** (primeiros SFX reais substituindo placeholders)

### Regras de operação da equipe de agentes
1. GDD.md é lei; este arquivo define quem faz o quê.
2. Design antes de código: números e specs viram arquivos de dados; código lê dados.
3. Toda entrega passa pelo QA antes de ir ao Diretor.
4. Ambiguidade = pergunta ao Diretor. Invenção silenciosa é bug de processo.
5. CHANGELOG.md e docs/ atualizados a cada entrega (exigência do Diretor: documentar tudo).

---

## PARTE 4 — Honestidade: o que agentes NÃO substituem (por enquanto)

1. **Netcode multiplayer de produção** — agentes escrevem a base, mas multiplayer em tempo real com jogadores reais exige testes em rede real, infraestrutura e iteração que pedem um humano experiente (o cargo 3.3). O plano das fases contorna isso: validamos TUDO offline primeiro.
2. **Arte final autoral** — agentes geram estrutura, placeholders e integração; a identidade visual que vende o jogo virá de artista humano (ou de você dirigindo geração de arte com curadoria dura + style guide).
3. **Áudio original de qualidade** — idem: placeholders licenciados primeiro, compositor depois.
4. **Playtesting de "feel"** — nenhum agente sente se o combate é gostoso. Esse sensor é VOCÊ (e depois, jogadores reais).
5. **Publicação e negócios** — conta de desenvolvedor Google Play, classificação indicativa, contratos, INPI: processos humanos/jurídicos.

O que os agentes fazem MUITO bem neste projeto: todo o código do protótipo, a simulação do terreno, os dados de balanceamento, os menus/configurações, o pipeline de build, a documentação e a disciplina de processo. É exatamente o recorte das Fases 1–3.
