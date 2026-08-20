# ARKANA - Magos Battle Royale

> Caia do castelo voador, domine os cinco elementos e seja o ultimo mago de pe.

Arkana e um battle royale de magia em terceira pessoa para Android. O projeto
mantem duas frentes ativas que compartilham o mesmo GDD:

| Frente | Papel | Estado em 20/08/2026 |
|---|---|---|
| **Godot 4.4** | produto principal 3D para Android | G0-G2 concluidos; APK jogavel; G3 e o proximo marco |
| **Roblox / Rojo** | Campo de Provas multiplayer | jogavel e automatizado; aguarda playtest humano |

O antigo prototipo 2D em Phaser/Capacitor foi encerrado depois de cumprir seu
papel de validar combate, toque e terreno reativo. O codigo e o pipeline dessa
frente nao fazem mais parte da arvore atual; permanecem acessiveis apenas pelo
historico do Git.

## Estado do jogo 3D

O projeto Godot ja entrega:

- partida curta contra seis bots, com vitoria e derrota;
- camera sobre o ombro e controles para teclado/mouse e toque;
- disparo continuo, esquiva com i-frames, mana, vida e HUD;
- Fogo, Agua, Terra, Vento e Raio com cor e forma proprias;
- floresta incendiavel, agua congelavel/eletrificavel, muros de Terra e vento
  espalhando fogo por orcamento;
- audio procedural, feedback de dano, contagem, kill feed e tela final;
- menu com elenco de 20 magos e 19 retratos entregues.

O proximo marco e **G3: personagens e ambientes de verdade**. O objetivo e
substituir o personagem e a ilha procedurais por assets com qualidade visual de
jogo publicado, sem alterar os contratos de gameplay ja validados.

## Estrutura

```text
arkana/
|- godot/       jogo principal 3D e export Android
|- roblox/      Campo de Provas multiplayer
|- personagens/ fichas, prompts e artes canonicas do elenco
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
| [godot/ARQUITETURA.md](godot/ARQUITETURA.md) | contratos internos do jogo 3D |
| [CHANGELOG.md](CHANGELOG.md) | marcos mantidos do projeto ativo |

## Regra de produto

Arkana nunca sera pay-to-win. Poder e obtido dentro da partida; monetizacao e
exclusivamente cosmetica. Habilidade decide.

Projeto privado em desenvolvimento. Direcao de jogo: Vinicius Souza.
