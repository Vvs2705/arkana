# mobile-godot — CONGELADO

O jogo de celular, em Godot 4.4.1. **12/12 autotestes verdes.**

## A diretriz: congelado, não morto

Em 27/08/2026 o Diretor decidiu migrar o projeto para PC/Steam. Esta pasta
**para de receber feature nova**, mas continua valendo por três motivos:

1. **É gameplay que funciona.** Zona, queda do castelo, loot, kits, terreno
   procedural, 20 magos — tudo rodando, com portão de 12 autotestes.
2. **É orçamento MEDIDO.** Os números daqui são a única referência real de custo
   que o projeto tem: ilha de 600 m em 184.967 tris e 67 draw calls, kit em
   201.203 e 98, bots a ~0,25 ms cada, colisao em 34.848 faces.
3. **É a exportação mobile futura.** Se um dia voltar, volta daqui.

## O que NÃO fazer

- **Não portar este código para o `pc-unreal/`.** Toda a lógica roda no CLIENTE:
  dano, mana, posição, loot, zona. Levar isso para um jogo em rede é levar o
  defeito que inviabiliza competitivo. O que atravessa é o `design/`.
- **Não mexer aqui para "aproveitar".** Se algo tem que mudar no design, muda em
  `design/` e as duas bases leem de lá.

## Como rodar

```bash
bash mobile-godot/godot/tests/run_all.sh      # os 12 autotestes
bash mobile-godot/godot/export/build_apk.sh   # APK de teste
```

O APK datado vai sempre para a MESMA pasta do clone principal
(`mobile-godot/godot/build/testes/`), venha de worktree ou não — ordem do
Diretor de 27/08, e o script resolve isso por `git rev-parse --git-common-dir`.
