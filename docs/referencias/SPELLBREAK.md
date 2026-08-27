# DOSSIÊ DE REFERÊNCIA — SPELLBREAK (Proletariat Inc., 2020–2023)

> Pesquisa de mercado/UX para o ARKANA (battle royale de magos, 3ª pessoa, Android, Godot 4.4.1).
> Regra do documento: todo fato traz FONTE (URL). O que não foi possível confirmar está marcado como **NÃO CONFIRMADO**.
> Data da pesquisa: 26/08/2026.

Ficha rápida:
- Lançamento: 03/09/2020 (free-to-play, Epic exclusivo no PC até dez/2020). Plataformas: Windows, PS4, Xbox One, Switch, com crossplay.
- Servidores oficiais encerrados em 10/01/2023, após a Proletariat ser adquirida pela Activision Blizzard (jun/2022) e realocada para World of Warcraft.
- Em dez/2022 a própria Proletariat lançou uma "Community Version" gratuita para servidores hospedados pelos jogadores (ver Seção 8).
- Fontes: https://en.wikipedia.org/wiki/Spellbreak · https://www.pcgamer.com/wizard-royale-spellbreak-is-shutting-down-next-year/

---

## 1. MECÂNICAS-NÚCLEO

### 1.1 Classes e gauntlets (o análogo direto das Luvas do Arkana)
6 classes, cada uma "casada" com um gauntlet elemental inicial. O jogador equipa DOIS gauntlets (um por mão): o da classe + um segundo que pega no loot — é daí que nascem os combos pessoais.

| Classe | Gauntlet | Identidade (fonte: wiki/guias) |
|---|---|---|
| Pyromancer | Fire | Controle de área balanceado; fireball + flamewall |
| Frostborn | Frost | Dano alto a longa distância (sniper de gelo); trilho de gelo para deslizar |
| Conduit | Lightning | Cadência rápida, eficiência de runa/mana |
| Toxicologist | Toxic | Nuvens e poças de veneno, dano ao longo do tempo |
| Stoneshaper | Stone | Dano bruto de projétil (shockwave) + boulderfall |
| Tempest | Wind | Mobilidade aérea alta (tornado, wind shear) |

- Progressão in-match: o jogador começa no nível 1 com a 1ª habilidade da classe; a cada nova zona segura sobe de nível e desbloqueia a próxima (4 no total). Ou seja: a "build" cresce com o ritmo da partida, não no lobby.
- Fontes: https://spellbreak.fandom.com/wiki/Classes · https://techraptor.net/gaming/guides/what-are-spellbreak-classes · https://www.pcgamesn.com/spellbreak/best-gauntlet-tier-list

### 1.2 Spell vs. Sorcery (o modelo de disparo)
- Cada gauntlet tem 2 ataques: **Spell** (ataque básico: sem cooldown, consome MANA) e **Sorcery** (habilidade forte: tem COOLDOWN, não consome mana).
- Mana é recurso duplo: munição dos spells E combustível da levitação — administrar mana é a skill central do jogo.
- Números confirmados por fontes secundárias (tabela completa na wiki, ver Seção 6):
  - Fireball base: 25 de dano em acerto direto (fonte: https://prodigygamers.com/2020/09/06/spellbreak-advance-stats-guide-on-gauntlet-skills-combo-damage/).
  - Stone spell (comum): 30 de dano; sorcery de pedra com cooldown de ~10 s (fonte: https://gamerant.com/spellbreak-gauntlets-ranked/).
  - Flame Puddle do Pyromancer persiste ~6 s aplicando Ignite/DoT (fonte: https://www.inverse.com/gaming/spellbreak-best-builds-gauntlets-class-combo-talents).
  - Demais valores (por raridade, por patch): consultar as páginas por patch da wiki — https://spellbreak.fandom.com/wiki/Spells_%26_Sorceries (e variantes "Patch .28/.38/2.1/3.1"). **Não listar números daqui sem conferir a página do patch** — mudaram muito entre capítulos.

### 1.3 Combinações de feitiços (análogo da SINTONIA do Arkana)
O sistema de combos é AMBIENTAL: os projéteis/efeitos interagem no mundo, não num menu. Qualquer jogador (ou dois aliados) pode compor. Lista consolidada (fonte principal: https://www.pcgamesn.com/spellbreak/combos · https://www.thegamer.com/spellbreak-best-combos-ranked/ · https://nerdschalk.com/spellbreak-combos-full-list-and-the-best-combo/):

Combos ofensivos:
- **Tornado + Fireball = Tornado de Fogo** (o tornado incendeia e passa a dar dano de queimadura).
- **Tornado + Lightning = Tornado Elétrico** (raio em acerto direto eletrifica o tornado).
- **Tornado + Toxic = Redemoinho Tóxico**; se incendiado depois, explode verticalmente.
- **Toxic Cloud/Puddle + Fogo = Dragonfire** (chama verde, explosão + 2 ticks de dano alto).
- **Boulderfall + Fireball = Meteoros** (pedras incendiadas que explodem e deixam poças de fogo).
- **Poça de água/gelo + Lightning = poça eletrificada** (área de choque).
- **Gelo derretido por fogo → vapor; vapor congelado (Flash Freeze) → névoa que congela inimigos**.

Contra-interações (counterplay — tão importante quanto o combo):
- Flash Freeze (gelo) APAGA fogo. Vento EXTINGUE chamas e DISPERSA nuvens tóxicas e névoas. Tóxico CONTAMINA água/gelo (vira poça tóxica). Pedra BLOQUEIA projéteis (boulder bloqueia raio; shockwave bloqueia fireball). Tornado deflete Ice Lance.
- Lição de design: cada elemento tem pelo menos 1 resposta a outro elemento — o sistema é pedra-papel-tesoura ambiental, e a leitura visual do combo resultante mescla a FORMA de um elemento com a PALETA do outro (ver Seção 2).

### 1.4 Mobilidade: levitação + runas
- **Levitação**: todo jogador levita segurando o pulo, drenando mana; usada para ganhar altura, planar entre morros e mirar do alto. Levitar impõe ~1,5 s de atraso na recarga de mana enquanto no ar (fonte: https://spellbreak.fandom.com/wiki/Gameplay — via resumo de busca; conferir na wiki). O combate raramente fica no chão ("players barely stay on the ground for more than a couple of seconds" — https://gamespace.com/featured/spellbreak-quick-hit-review/).
- **Runas**: item equipável que dá uma 3ª habilidade ativa em cooldown, quase sempre de mobilidade ou informação: voo, dash, invisibilidade, ver inimigos através de paredes etc. (fontes: https://en.wikipedia.org/wiki/Spellbreak · https://wcrobinson.org/2020/09/09/spellbreak-review/). Lista completa de runas: https://spellbreak.fandom.com/wiki/Equipment.
- Resultado: 3 eixos de mobilidade empilhados (corrida/pulo + levitação por mana + runa em cooldown) — teto de habilidade altíssimo (isso vira problema na Seção 5).

### 1.5 Queda/inserção no início da partida (referência para a queda do castelo do Arkana)
- Após fechar o lobby, o jogador tem **20 segundos para escolher um PORTAL no céu** por onde cair nas Hollow Lands (fonte: https://spellbreak.fandom.com/wiki/Gameplay — via resumo de busca).
- A queda é um mergulho livre a partir do portal; **não existe "acelerar a queda" segurando para frente** — o jogo pede que você posicione o personagem sobre o ponto desejado e use o tempo para LER o terreno e ver onde os outros vão pousar (fonte: https://www.thegamer.com/spellbreak-beginner-guide/).
- O freio final é a própria levitação (mana), não um paraquedas — a transição queda→pouso usa a MESMA mecânica que o jogador usará a partida inteira. Não há dano de queda relevante reportado nos guias.
- **NÃO CONFIRMADO em texto**: ângulo de câmera e pose de queda exatos (verificar visualmente nos vídeos da Seção 6 — os primeiros 60 s de qualquer gameplay mostram).

---

## 2. LINGUAGEM VISUAL

### 2.1 Direção de arte (fonte: entrevista com Damon Iannuzzelli, AD/cofundador — https://www.cgmagonline.com/interviews/spellbreak-art-direction-interview/)
- Alvo declarado: "anime kind of feel" — personagens chapados/cel-shaded sobre ambientes mais pictóricos. Influências citadas: **Doctor Strange** (traços mágicos elaborados), **Avatar: The Last Airbender / Korra** (magia elemental física, "grounded").
- Regra de ouro de legibilidade: **paleta "ancorada no natural"** — nenhuma cor arbitrária; cada elemento tem uma **shape language consistente** (linguagem de formas própria).
  - Fogo: arestas flamejantes + fumaça volumosa; Tóxico: verde brilhante e OPACO (nunca confundível com outro efeito); Gelo/Raio/Vento: cada um com forma própria.
  - **Quando dois elementos se combinam, o VFX resultante usa a FORMA de um + a PALETA do outro** — o jogador reconhece o combo à distância. (Regra diretamente reutilizável na Sintonia do Arkana.)

### 2.2 Técnica de render/VFX (fonte: https://80.lv/articles/spellbreak-developing-a-magic-battle-royale-rpg)
- Toon shading em quase toda superfície: shader Unreal com **ramp texture** controlando como a luz "enrola" nas normais, com blend contínuo entre cel-shade autoral, PBR padrão e luz indireta.
- **Personagens**: quase 100% cel-shaded, SEM luz indireta, texturas muito chapadas — reforça leitura 2D e silhueta (nota Arkana: silhueta escura em contra-luz se resolve com esse tipo de shading + rim/outline, não com luz de cena).
- **VFX estilo desenhado à mão**: partículas em mesh com o toon shader, animação com **frame rate reduzido (12–15 fps)** para look de anime, e **erosão de alfa** para dissipação estilizada (nada de fade linear).
- **Legibilidade acima da estética**: quando o VFX atrapalha o gameplay, aplicam **erosão de detalhe por distância e/ou pixel dithering** — o efeito perto é rico, longe vira mancha de cor limpa e identificável.
- Concept art oficial de VFX do próprio Iannuzzelli: https://www.artstation.com/artwork/lgNoo

### 2.3 Muros/paredes elementais (referência para Muro de Terra e Muralha de Brasas do Arkana)
- **Flamewall (sorcery do Fire)**: parede de chamas contínua com topo irregular animado (lambidas de fogo em 12–15 fps), base com fumaça; lê à distância como uma linha laranja VIVA, nunca um retângulo estático. Dá dano/ignite ao atravessar.
- **Muro de pedra (Stoneshaper)**: placas de rocha que EMERGEM do chão com animação de erupção (poeira + fragmentos no surgimento), silhueta facetada/irregular — nunca um paralelepípedo liso; funciona como bloqueio físico de projéteis.
- **NÃO CONFIRMADO em texto** (descrições acima derivadas do material visual oficial — conferir de olho): galeria de VFX concepts https://www.artstation.com/artwork/lgNoo · key visuals/UI https://www.wildbluestudios.art/projects/spellbreak1 · vídeos da Seção 6. Números (vida do muro, duração) na wiki: https://spellbreak.fandom.com/wiki/Spells_%26_Sorceries.
- Receita extraível: muro elemental legível = (1) animação de SURGIMENTO (erupção/acender), (2) topo/borda irregular animada, (3) partícula secundária (poeira/fumaça/brasas), (4) cor saturada da paleta do elemento com interior mais escuro para contraste.

### 2.4 Loot no chão e raridade
- 5 raridades de equipamento: **Common, Uncommon, Rare, Epic, Legendary**. Common/Uncommon spawnam no mundo; raridades altas só em Baús e Shrines; **Legendary é exclusiva dos Mana Vaults** (baú-cofre especial) (fonte: https://spellbreak.fandom.com/wiki/Equipment — via resumo de busca).
- Escada de baús: Small Chest (comum, chance de Rare/Epic) → baús maiores → **Mana Vault** (garante Legendary/Epic) (fonte: https://www.gameshedge.com/spellbreak-items-guide/).
- Itens no chão aparecem como ícones/orbes flutuantes com brilho na cor da raridade — **cores exatas por tier: NÃO CONFIRMADO em texto**; verificar screenshots em https://www.gameuidatabase.com/gameData.php?id=1368 (catálogo de todas as telas do jogo). A convenção que o jogo aparenta seguir é a padrão do gênero (cinza/branco → verde → azul → roxo → dourado).
- Lição para o Arkana (loot que "vira vulto preto"): o Spellbreak resolve com item AUTO-ILUMINADO (emissivo, ignora luz da cena) + billboard/ícone + brilho de raridade — o loot nunca depende da iluminação ambiente para ler.

---

## 3. UI/HUD

- Fontes primárias visuais (o material textual público é escasso): **Game UI Database — todas as telas do Spellbreak**: https://www.gameuidatabase.com/gameData.php?id=1368 · portfólio da designer de HUD Kat Dolan: https://katdolan.design/spellbreak-hud-design · key visuals/UI da Wild Blue Studios: https://www.wildbluestudios.art/projects/spellbreak1
- O que está confirmado em texto:
  - HUD projetado em colaboração estreita com Gameplay Design/Engineering para que "cada animação comunique a informação certa" (fonte: https://katdolan.design/spellbreak-hud-design).
  - Spells não têm cooldown (custam mana) e Sorceries têm cooldown sem mana — o HUD portanto mostra **barra de mana** (recurso contínuo) + **ícones de habilidade com sweep de cooldown** (sorcery + runa), padrão que os guias descrevem como "menu de ações na parte de baixo da tela" (fonte: https://wcrobinson.org/2020/09/09/spellbreak-review/).
  - Sobre inimigos: barras de vida com **ícones acima mostrando quais spells o inimigo tem disponíveis** (informação de counterplay no próprio target) (fonte: idem).
- Layout espacial exato (minimapa, kill feed, posição de vida/armadura), PC vs console: **NÃO CONFIRMADO em texto** — verificar nos screenshots do Game UI Database acima (catalogados por tela) antes de copiar qualquer decisão.
- Observação relevante para o pedido de "layout ajustável" do Arkana: Spellbreak **não tinha versão mobile** — não há referência de HUD touch dele. A referência correta para botões touch reposicionáveis é o padrão Fortnite Mobile/CoD Mobile (edição de HUD por arrastar). O que o Spellbreak contribui é a HIERARQUIA da informação (mana sempre visível, cooldowns nos ícones, spells do inimigo no target), não o layout físico.

---

## 4. ÁUDIO

- **Resultado da pesquisa: não existe material público substancial sobre a direção de som do Spellbreak** (nenhum talk, artigo técnico ou entrevista de sound design encontrado; a entrevista da Unreal Engine e o artigo da 80.lv cobrem apenas arte/tecnologia visual). Middleware, equipe de áudio e abordagem por elemento: **NÃO CONFIRMADO**.
- O que dá para usar mesmo assim: a Community Version oficial (Seção 8) permite ouvir o jogo inteiro legalmente, de graça — a referência de áudio do Spellbreak é o próprio jogo rodando. Para o pedido de "sons com profundidade" do Arkana, o padrão audível no jogo (verificável nos vídeos da Seção 6): cada elemento com assinatura em camadas — transiente de conjuração + corpo (loop do projétil) + impacto com cauda — e sorceries com som muito mais "cheio" que spells, reforçando a hierarquia spell/sorcery.
- Marcação honesta: o parágrafo acima é observação de gameplay, não fonte de dev. Tratar como hipótese de referência, não como fato documentado.

---

## 5. LIÇÕES DO FRACASSO — o que o ARKANA deve EVITAR

Cronologia dura: pico inicial forte (2M jogadores na 1ª semana, ~10M acumulados), declínio contínuo, aquisição pela Blizzard (jun/2022) para trabalhar em WoW, servidores desligados em 10/01/2023. Palavras oficiais da Proletariat: o jogo "não conseguiu alcançar um lugar sustentável" para continuar recebendo investimento.
Fontes: https://www.pcgamer.com/wizard-royale-spellbreak-is-shutting-down-next-year/ · https://gamerant.com/spellbreak-shutting-down/ · https://en.wikipedia.org/wiki/Spellbreak · https://aftermath.site/spellbreak-battle-royale-community-servers-blizzard-proletariat/

Causas apontadas (por fonte):
1. **Retenção, não aquisição, foi o problema.** O jogo atraía gente (10M instalaram) mas não segurava. Receita estimada em ~US$ 43M no ciclo de vida, decaindo após o lançamento (fonte: https://playercounter.com/spellbreak/ — estimativa de terceiro, tratar com cautela). Pico Steam de apenas ~5,5 mil simultâneos (idem).
2. **Teto de habilidade brutal sem proteção ao novato.** Levitação + runa + mira em projéteis + gestão de mana = curva de aprendizado íngreme ("steep learning curve" — https://geekculture.co/geek-review-spellbreak/). A comunidade aponta a ausência de MMR/matchmaking protegido para novos jogadores: veteranos devoraram os novatos e a base "se consumiu" (fonte: discussões da comunidade Steam — https://steamcommunity.com/app/1399780/discussions/ — opinião de jogadores, não dado oficial).
3. **Exclusividade Epic no lançamento** limitou a base de PC no momento de maior hype; o lançamento na Steam (dez/2020) veio tarde e não deu tração (fontes: https://en.wikipedia.org/wiki/Spellbreak · discussões Steam acima).
4. **Concorrência frontal com Fortnite/Apex/Warzone** sem o mesmo ritmo de conteúdo — capítulos lentos, e o estúdio (equipe pequena) trocou o modo Clash por Dominion (Chapter 2) dividindo a base (fontes: https://gamerant.com/spellbreak-shutting-down/ · https://en.wikipedia.org/wiki/Spellbreak).
5. **Queixas de balanceamento persistentes** (aim assist de controle dominante em crossplay, metas de runa/gauntlet) minando o competitivo (fonte: discussões de comunidade — opinião, não confirmado oficialmente).
6. **O fim não foi do jogo, foi do dono.** A aquisição pela Blizzard realocou o estúdio inteiro; o desligamento foi decisão de portfólio (fonte: https://aftermath.site/spellbreak-battle-royale-community-servers-blizzard-proletariat/).
   Análises em vídeo dos pontos acima: "Death of a Game: Spellbreak" e "What Went Wrong?" (links na Seção 6).

Tradução em regras para o Arkana:
- **Onboarding primeiro**: a Sintonia e o terreno reativo são tão "combo-dependentes" quanto o Spellbreak — precisam de tutorial jogável + bots/matchmaking piedoso nas primeiras partidas, ou a retenção D1→D7 morre igual.
- **Complexidade legível**: o Spellbreak provou que combo elemental VENDE o jogo (era o gancho de todo review) mas MATA se o novato não entender o que o matou. Killcam/feed explicando "você morreu para Tornado de Fogo (Fulano + Beltrano)" é retenção, não luxo.
- **Não competir em volume de conteúdo** com gigantes: o Arkana (dev solo/equipe mínima) deve competir em identidade (Sintonia cooperativa não existe em nenhum BR), não em cadência de skins.
- **Mobile é uma vantagem, não um plano B**: o Spellbreak nunca teve versão mobile — o nicho "Spellbreak de bolso" ficou vago desde 2023.

---

## 6. LINKS CURADOS

Vídeos (YouTube):
1. Death of a Game: Spellbreak (nerdSlayer, análise do fracasso, 2022) — https://www.youtube.com/watch?v=1MLIR95-SBQ
2. Spellbreak: What Went Wrong? An Analysis — https://www.youtube.com/watch?v=rDkVfh0Dnfs
3. Spellbreak Developer Interview | GDC 2019 | Unreal Engine (visão dos devs sobre design/tec) — https://www.youtube.com/watch?v=ePTExLKGGM8
4. The ULTIMATE Combo Guide for Spellbreak (todas as interações elementais em vídeo — referência direta para a Sintonia) — https://www.youtube.com/watch?v=fIACl-2Pr7M
5. Spellbreak Spell Combinations Guide — https://www.youtube.com/watch?v=MTIdikb0it0
6. Spellbreak Beginner's Guide in 4 Minutes (mostra queda inicial, HUD e loop em 4 min — bom para estudar a inserção no mapa) — https://www.youtube.com/watch?v=0DiqP3SLVew
7. 7 Spellbreak Tips & Tricks EVERYONE Must Know (MARCUSakaAPOSTLE, gameplay de alto nível/movimento) — https://www.youtube.com/watch?v=rZH5ibXuAhM
8. The COMPLETE CLASS GUIDE for Spellbreak Chapter Two — https://www.youtube.com/watch?v=vQ8XIYkwPUU

Wikis e dados de balanceamento:
- Wiki oficial (Fandom) — hub: https://spellbreak.fandom.com/
- Spells & Sorceries (dano/cooldown por patch): https://spellbreak.fandom.com/wiki/Spells_%26_Sorceries (variantes por patch: .28, .38, 2.1, 3.1)
- Classes (kits e desbloqueio por nível): https://spellbreak.fandom.com/wiki/Classes
- Equipment (raridades, runas, baús): https://spellbreak.fandom.com/wiki/Equipment
- Gameplay (portais, levitação, zona): https://spellbreak.fandom.com/wiki/Gameplay
- Combos por texto: https://www.pcgamesn.com/spellbreak/combos · https://nerdschalk.com/spellbreak-combos-full-list-and-the-best-combo/

Arte, VFX e UI:
- 80 Level — pipeline de cel-shading e VFX (artigo técnico essencial): https://80.lv/articles/spellbreak-developing-a-magic-battle-royale-rpg
- CGMagazine — entrevista de direção de arte (legibilidade e shape language): https://www.cgmagonline.com/interviews/spellbreak-art-direction-interview/
- ArtStation de Damon Iannuzzelli — concepts de VFX oficiais: https://www.artstation.com/artwork/lgNoo
- Wild Blue Studios — key visuals, concepts e assets de UI: https://www.wildbluestudios.art/projects/spellbreak1
- Game UI Database — screenshots catalogados de TODAS as telas/HUD: https://www.gameuidatabase.com/gameData.php?id=1368
- Kat Dolan — portfólio do HUD: https://katdolan.design/spellbreak-hud-design
- Unreal Engine — entrevista de desenvolvimento: https://www.unrealengine.com/developer-interviews/spellbreak-is-a-unique-battle-royale-game-that-combines-magic-roguelike-and-rpg-elements

Comunidade pós-shutdown:
- Community Version oficial (itch.io): https://sbcommunity.itch.io/spellbreak-community-version
- Launcher/comunidade Elemental Fracture: https://elefrac.com (Discord 5.000+ membros)
- Cobertura: https://aftermath.site/spellbreak-battle-royale-community-servers-blizzard-proletariat/ · https://www.techdirt.com/2023/02/22/spellbreak-developer-gets-it-exactly-right-in-shutting-down-its-game/

---

## 7. TABELA "ADAPTAR → ARKANA"

| O que o Spellbreak faz | Pendência do Arkana que resolve | Esforço |
|---|---|---|
| Queda por portal: 20 s de escolha, mergulho livre SEM acelerar segurando frente, freio final com a MESMA mecânica de levitação usada em jogo, câmera atrás do personagem em pose de mergulho | Queda do castelo "estranha": adotar pose de mergulho + câmera atrás/inclinada (não 90° para baixo) + vento/streaks de velocidade + transição queda→planar usando o próprio sistema de planar | Médio |
| Muro elemental = animação de SURGIMENTO + borda irregular animada + partícula secundária + interior escuro/borda saturada | Muro de Terra caixote bege → placas facetadas que ERGUEM do chão com poeira; Muralha de Brasas chapada → topo de chamas animado (10–15 fps) + brasas + fumaça | Médio |
| VFX cel-shaded: mesh particles com toon shader, animação 12–15 fps, erosão de alfa, dithering/erosão de detalhe por DISTÂNCIA | Todas as pendências de leitura de VFX à distância — regra "perto rico, longe mancha limpa de cor única" | Alto (shader base) / Baixo (aplicar por efeito depois) |
| Loot emissivo auto-iluminado (ignora luz da cena) + ícone billboard + brilho na cor da raridade; 5 tiers; melhor raridade só em baús especiais | Loot que vira "vulto preto" à distância e em contra-luz | Baixo |
| Personagens 100% cel-shaded com texturas chapadas, sem luz indireta (leitura 2D da silhueta independe da luz da cena) | Silhueta do personagem escura em contra-luz | Médio |
| Combos ambientais com matriz completa de interação (cada par de elementos tem resultado OU counter) e regra visual "forma de um + paleta do outro" | Base de design da SINTONIA: tabela 5×5 do Arkana (Fogo/Água/Raio/Terra/Vento) com resultado nomeado + leitura visual própria por combo | Alto (design) / a matriz da Seção 1.3 corta metade do trabalho |
| Spell (mana, sem cooldown) vs Sorcery (cooldown, sem mana) — dois recursos que não competem | Clareza do HUD: barra de mana para disparo, sweep de cooldown para TÁTICA/SUPREMA — dois lugares diferentes, sem ambiguidade | Baixo |
| Ícones sobre a barra de vida do inimigo mostrando as habilidades DISPONÍVEIS dele | Counterplay legível na Sintonia (saber o que a dupla inimiga pode combinar) — candidato a backlog, não MVP | Médio |
| Zona segura = level-up (habilidades desbloqueiam com o ritmo da partida) | Ritmo de progressão in-match (alternativa a tudo liberado desde o pouso) — avaliar para a carga da SUPREMA | Baixo (regra) |
| Lição negativa: sem MMR/onboarding → base se consome | Planejamento de bots/matchmaking piedoso + killcam explicando combos desde o Alpha | Alto (mas existencial) |

---

## 8. LIMITE LEGAL (curto e obrigatório)

- **Livre**: inspirar-se em mecânicas, regras, sensação de jogo e princípios de estética (cel-shading, shape language, hierarquia de HUD). Mecânica de jogo não é protegida por copyright.
- **PROIBIDO**: copiar assets (modelos, texturas, VFX, sons, música), nomes próprios (Spellbreak, Hollow Lands, Pyromancer etc. — marca/trade dress), arte ou EXTRAIR arquivos do jogo/da Community Version para uso no Arkana. A propriedade intelectual pertence hoje à Activision Blizzard (que adquiriu a Proletariat em 2022).
- **O que a Proletariat liberou oficialmente**: em dez/2022 publicou gratuitamente a "Spellbreak Community Version" (https://sbcommunity.itch.io/spellbreak-community-version), um standalone que permite aos jogadores HOSPEDAR os próprios servidores e jogar — nas palavras deles, "para memorializar o Spellbreak" (fontes: https://www.gamedeveloper.com/business/proletariat-s-spellbreak-lives-on-as-a-free-player-run-game · https://aftermath.site/spellbreak-battle-royale-community-servers-blizzard-proletariat/). Isso é licença para JOGAR/hospedar, não cessão de assets nem código aberto — nenhuma fonte indica liberação de ferramentas, arte ou fontes para reuso em outros jogos: para fins do Arkana, tratar como **referência de estudo jogável e nada além**.
- Uso correto no projeto: jogar a Community Version e assistir aos vídeos como REFERÊNCIA (timing, leitura, som), recriando tudo do zero com identidade própria (nomes, silhuetas, paleta e assets originais do Arkana).

---

*Dossiê produzido por pesquisa web em 26/08/2026. Itens marcados NÃO CONFIRMADO exigem verificação visual (vídeos/screenshots) antes de virarem spec.*
