# PROJETO - estado atual do Arkana

Atualizado em 20/08/2026. Este documento descreve apenas o que continua em
desenvolvimento.

## Produto

Arkana e um battle royale de magos em terceira pessoa, orientado a Android. A
identidade mecanica do jogo se apoia em dois pilares:

1. **Sintonia:** dois jogadores combinam elementos numa magia conjunta.
2. **Terreno reativo:** fogo, agua, gelo, eletricidade, terra e vento mudam
   rotas, cobertura e risco durante a partida.

O [GDD](GDD.md) e a fonte da verdade de produto. Numeros compartilhados vivem
nos arquivos `Balance` de cada frente e devem permanecer coerentes com o GDD.

## Frentes ativas

### Godot 4.4 - produto principal

O jogo 3D vive em `godot/` e exporta diretamente para Android. Os marcos G0,
G1 e G2 estao completos:

- projeto Godot funcionando e pipeline Android versionado; o ultimo APK existe,
  mas a maquina precisa restaurar templates, SDK e JDK para reproduzi-lo;
- ilha 3D jogavel, camera em terceira pessoa e partida contra bots;
- controles de toque com gesto unico para mirar e disparar;
- cinco elementos e terreno reativo 3D;
- audio, HUD e feedback de combate;
- menu e vitrine do elenco.

O APK de debug local e gerado em `godot/build/arkana3d.apk` e nao e versionado.
O proximo marco e G3, detalhado no [ROADMAP 3D](ROADMAP_3D.md).

### Roblox - Campo de Provas

O projeto em `roblox/` continua ativo. Ele e o ambiente multiplayer para testar
com pessoas o que bots nao conseguem validar: Sintonia, TTK, leitura do terreno,
equilibrio dos elementos e vontade de jogar novamente.

O Campo de Provas possui servidor autoritativo, duplas, terreno reativo, os dez
combos de Sintonia, loop de battle royale, bots, acessibilidade, telemetria e
ferramentas de relatorio. O bloqueio atual nao e tecnico: falta executar o
playtest humano descrito em [ROBLOX.md](ROBLOX.md).

## Elenco e arte

Existem 20 fichas em `personagens/`. Dezenove possuem `arte/concept.png`; Maris
(15) ainda precisa de arte. As mesmas 19 imagens aparecem na vitrine do Godot em
`godot/menu/art/`. Os prompts de regeneracao permanecem junto das fichas
correspondentes, sem pastas de download ou ZIPs duplicados no projeto.

## O que foi encerrado

O prototipo 2D em Phaser, TypeScript e Capacitor foi descontinuado em 19/08/2026.
Ele cumpriu o papel de validar as primeiras mecanicas e o toque, mas nao e uma
frente de produto, nao recebe manutencao e nao faz parte da arvore atual. Seu
codigo e suas decisoes antigas continuam recuperaveis pelo historico do Git.

## Proximo trabalho

1. Produzir o personagem 3D com rig e uma identidade visual aprovada.
2. Transformar a ilha procedural em tres biomas com qualidade de jogo.
3. Elevar VFX e materiais sem quebrar o orcamento mobile.
4. Gerar, verificar e testar o APK G3 em aparelho real.
5. Executar em paralelo o playtest humano do Roblox e registrar os resultados
   no GDD antes de alterar balanceamento compartilhado.

## Criterio de verdade

- Produto e regras: `docs/GDD.md`.
- Execucao atual: `docs/ROADMAP_3D.md`.
- Codigo principal: `godot/`.
- Validacao multiplayer: `roblox/`.
- Regras que podem atravessar: `docs/PONTE.md`.
- Estado de arte: `personagens/00-LEIA.md`.
