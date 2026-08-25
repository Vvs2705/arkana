# CHANGELOG - Arkana

Este arquivo registra os marcos das frentes que continuam ativas. O historico
detalhado do prototipo 2D encerrado permanece disponivel nos commits anteriores.

## Nao publicado - 2026-08-26 - a partida comeca no ar

- **A QUEDA**: castelo voador com rota deterministica, salto, queda livre,
  planeio e pouso. 9,5 s de ar e 183 m de alcance horizontal. Nenhuma magia
  funciona durante a queda.
- **Ilha de 180 para 300 m** (2,9x de area) com 7 pontos de interesse, agora
  distinguiveis do alto — a vista que a queda criou. Draw calls do pior quadro
  CAIRAM de 53 para 30.
- **Pegar item e abrir bau viraram gesto**, com o efeito saindo no quadro em que
  a mao alcanca o chao. Cancelamento com cor e forma.
- **Fim da patinacao dos pes**: animacao importada entrava sem laco e tocava uma
  vez so'.
- Zona da tempestade e terreno reativo passaram a ESCALAR com o tamanho do mapa,
  em vez de guardar o raio antigo cravado.

## Nao publicado - 2026-08-25 - Brok em 3D e higienizacao

- **Brok**, segundo mago com modelo 3D real (Meshy): 15.424 tris, 1 malha, 1
  material, 24 ossos, 5 animacoes. Custo medido: 30 creditos.
- Corrigida a **meia-volta do modelo importado**: glTF olha para +Z e o Godot
  anda para -Z, entao o personagem corria de costas. A Pyra carregava o defeito
  desde 21/08.
- Escala verificada: Pyra 1,78 m e Brok 1,40 m, exatamente as fichas.
- Ferramenta `characters/_shot_mago.gd` para renderizar personagem e julga-lo
  pelo quadro, nao so' por numero.
- Higienizacao: -398 MB em duplicatas; atelie de arte e builds de teste passaram
  a viver dentro do projeto, fora do git; pacote de pipeline Meshy achatado em
  `docs/pipeline-arte/`.
- Portao novo no build: reprova se o atelie 3D vazar para dentro do APK — foi o
  que levou o pacote de 54 para 106 MB sem nada acusar.

## Nao publicado - 2026-08-21 - R20: o battle royale ganha regras

- **Zona que fecha** em 5 fases, **habilidades** (passiva/tatica/suprema) com 3
  kits, **escudo evolutivo**, **estado derrubado com reerguer**, **armas
  arcanas** com loot no mapa e **Bau Celestial**.
- **Pyra em 3D** pela Meshy, primeira personagem com modelo real.
- Elenco descolado do Apex: cada mago ganhou marca magica propria.

## Nao publicado - 2026-08-20

- Arvore consolidada em torno de Godot e Roblox.
- Removidos Phaser, TypeScript, Vite, Capacitor, Android legado e builds locais.
- Removidos documentos historicos que descreviam planos substituidos.
- README, GDD e guias tecnicos alinhados ao estado atual.
- Mantido o conjunto canonico de 19 artes; removidos ZIPs e copias de importacao.

## v1.3.0-3d - 2026-08-20 - G2 concluido

- Terreno reativo 3D completo: fogo por orcamento, agua, gelo, eletricidade,
  muros de Terra e propagacao pelo Vento.
- Dano ambiental centralizado em `Combat`.
- Cinco elementos com projeteis de forma distinta.
- Audio procedural e camada de feedback de partida.
- Selftests de gameplay, terreno, menu, audio, juice e personagem aprovados.

## v1.2.0-3d - 2026-08-20 - apresentacao e gameplay

- Ilha recebeu vegetacao, agua, luz, nevoa e detalhes ambientais.
- Carrossel dos cinco elementos, esquiva, knockback e bots ampliados.
- Tela de titulo, menu e selecao de personagem passaram a abrir o APK.
- Infraestrutura futura foi documentada por fase.

## v1.1.0-3d - 2026-08-19 - toque e elenco

- Disparo continuo validado no aparelho.
- Elenco organizado em 20 fichas.
- Vitrine de personagens preparada para retratos reais.

## v1.0.0-3d - 2026-08-19 - pivo para Godot

- Godot 4.4 adotado como engine do produto principal.
- G0 e G1 entregaram APK, ilha 3D, mago procedural, camera em terceira pessoa,
  controles de toque, Fogo, bots e loop de partida.

## Campo de Provas Roblox - 2026-08-17 a 2026-08-19

- Projeto Rojo com servidor autoritativo e contratos compartilhados.
- Terreno reativo, combate, duplas e dez combos de Sintonia.
- Loop de battle royale, zona, bots, retorno, onboarding e acessibilidade.
- Telemetria e relatorio preparados para as cinco perguntas de validacao.
- Estado atual: tecnicamente pronto; aguarda playtest humano.
