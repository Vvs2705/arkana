# ARKANA - Magos Battle Royale

> Caia do castelo voador, domine os cinco elementos e seja o ultimo mago de pe.

Arkana e um battle royale de magia em terceira pessoa para Android. O projeto
mantem duas frentes ativas que compartilham o mesmo GDD:

| Frente | Papel | Estado em 26/08/2026 |
|---|---|---|
| **Godot 4.4** | produto principal 3D para Android | jogavel de ponta a ponta; a partida comeca no ar; 2 dos 20 magos com modelo 3D real |
| **Roblox / Rojo** | Campo de Provas multiplayer | congelado e tecnicamente pronto; aguarda playtest humano |

O antigo prototipo 2D em Phaser/Capacitor foi encerrado depois de cumprir seu
papel de validar combate, toque e terreno reativo. O codigo e o pipeline dessa
frente nao fazem mais parte da arvore atual; permanecem acessiveis apenas pelo
historico do Git.

## Estado do jogo 3D

O projeto Godot ja entrega:

**A partida**
- comeca **no ar**: um castelo voador cruza o mapa, o jogador salta quando quer,
  cai, plana e pousa — sem nenhuma magia durante a queda;
- ilha procedural de 300 m com 7 pontos de interesse legiveis do alto;
- **zona da tempestade** que fecha em 5 fases e escala com o tamanho do mapa;
- seis bots, vitoria e derrota, com o fim de verdade sendo o ultimo em pe.

**O combate**
- Fogo, Agua, Terra, Vento e Raio com cor **e forma** proprias;
- **habilidades** por mago (passiva, tatica e suprema) — 3 dos 20 kits escritos;
- **escudo de magia evolutivo**, **estado derrubado com reerguer**, esquiva com
  i-frames, mana e vida;
- floresta incendiavel, agua congelavel e eletrificavel, muros de Terra e vento
  espalhando fogo por orcamento.

**O loot**
- **armas arcanas** espalhadas pelo mapa e o **Bau Celestial**, unica fonte da
  manopla, com canalizacao interrompivel;
- pegar e abrir sao um **gesto** do personagem, nao uma troca de estado invisivel.

**Apresentacao**
- camera sobre o ombro, controles de toque com gesto unico de mira;
- audio 100% sintetizado em codigo, sem um arquivo de som no repositorio;
- menu, configuracoes e selecao dos 20 magos com os retratos entregues.

**Personagens 3D**
- **Pyra** e **Brok** com modelo real gerado pela Meshy a partir da concept art,
  riggados e animados. Os outros 18 usam o mago procedural.

O proximo marco e **fechar o elenco**: os 17 kits que faltam e a **Sintonia**, o
pilar de combinar elementos entre dois jogadores, que ainda nao tem uma linha em
codigo. A arte 3D de cenario vem depois — decisao registrada em
[docs/PROJETO.md](docs/PROJETO.md).

## Estrutura

```text
arkana/
|- godot/       jogo principal 3D e export Android
|  |- gameplay/   partida, combate, zona, loot, bau, queda, habilidades
|  |- characters/ o mago (procedural + modelos .glb reais)
|  |- world/      ilha procedural, castelo e shaders
|  `- build/      APK gerado; `testes/` guarda as copias datadas
|- roblox/      Campo de Provas multiplayer (congelado)
|- personagens/ as 20 fichas e o atelie de arte de cada mago
|- tools/meshy/ pipeline concept art -> personagem 3D riggado
|- audio/vozes/ falas dos 20 magos escritas; nenhuma gravada ainda
|- docs/        GDD, estado, roadmap e guias ativos
|- infra/       infraestrutura futura por fase
`- CHANGELOG.md marcos atuais do projeto
```

## Executar o Godot

Requisitos: Godot 4.4.1. Abra `godot/project.godot` no editor ou execute:

```powershell
godot --path godot
```

O projeto inicia em `godot/menu/Menu.tscn`. O setup e o comando de exportacao
Android estao em [godot/export/SETUP.md](godot/export/SETUP.md).

## Executar o Roblox

Requisitos: Roblox Studio e Rojo. O mapa do projeto esta em
`roblox/default.project.json`; o guia de playtest e os criterios de validacao
estao em [docs/ROBLOX.md](docs/ROBLOX.md) e
[docs/COMO_JOGAR.md](docs/COMO_JOGAR.md).

## Documentacao principal

| Documento | Funcao |
|---|---|
| [docs/PROJETO.md](docs/PROJETO.md) | estado atual e decisoes em vigor |
| [docs/GDD.md](docs/GDD.md) | fonte da verdade de produto e gameplay |
| [docs/ROADMAP_3D.md](docs/ROADMAP_3D.md) | sequencia ativa G0-G6 |
| [docs/ROBLOX.md](docs/ROBLOX.md) | arquitetura e protocolo do Campo de Provas |
| [docs/PONTE.md](docs/PONTE.md) | regras validadas que atravessam para o Godot |
| [docs/ANDROID.md](docs/ANDROID.md) | estado e pipeline Android atual |
| [docs/MESHY.md](docs/MESHY.md) | concept art -> modelo 3D riggado, com custos medidos |
| [docs/ART.md](docs/ART.md) | direcao visual e o que ainda nao foi decidido |
| [godot/ARQUITETURA.md](godot/ARQUITETURA.md) | contratos internos do jogo 3D |
| [CHANGELOG.md](CHANGELOG.md) | marcos mantidos do projeto ativo |

## Como verificar

Cada pasta tem seu proprio autoteste headless. O portao antes de qualquer
entrega e' rodar **todos** e nenhum falhar — e existe um comando so' para isso:

```bash
bash godot/tests/run_all.sh
```

Ele imprime `ARKANA: 12/12 selftests executados com sucesso.` no fim. Qualquer
teste que falhe **encerra o script na hora**, sem imprimir essa linha. Use
`GODOT_BIN=/caminho/para/godot` se o binario nao estiver no PATH.

Os doze continuam disponiveis individualmente, que e' como se diagnostica uma
falha depois que o runner apontou onde ela esta':

```bash
godot --headless --path godot --script res://gameplay/selftest.gd
```

Sao eles: `gameplay` (mais `selftest_kits`, `selftest_zona`,
`selftest_derrubado`), `gameplay/queda`, `ui`, `menu`, `characters`, `world`,
`terrain`, `audio` e `juice`.

**Regra que vale para todos:** todo teste novo se prova REINTRODUZINDO o defeito
que ele existe para pegar. Teste que nunca ficou vermelho e' decoracao.

O APK sai com `bash godot/export/build_apk.sh`, que tambem deixa uma copia
datada em `godot/build/testes/`.

## O que NAO esta pronto

Escrito aqui de proposito, para o repositorio nao parecer mais adiantado do que e':

- **17 dos 20 magos nao tem kit** — entram em partida sem tatica nem suprema.
- **A Sintonia nao existe em codigo**, e e' um dos dois pilares de identidade.
- **Os bots nao caem do castelo**: nascem no chao.
- **Nao ha' colisao no ar** durante a queda, e nem natacao.
- **FPS nunca foi medido em aparelho** — nenhum celular apareceu em `adb devices`.
- **As vozes e a arte de UI** estao escritas e com prompts prontos, nada gerado. O
  APK ainda usa o icone padrao do Godot, o que bloqueia publicar.

## Regra de produto

Arkana nunca sera pay-to-win. Poder e obtido dentro da partida; monetizacao e
exclusivamente cosmetica. Habilidade decide.

Projeto privado em desenvolvimento. Direcao de jogo: Vinicius Souza.
