# ARQUITETURA godot/ — contratos entre raias (R16+)

Dono de `project.godot`, `core/` e deste arquivo: COORDENADOR.

| Pasta | Dona | Entrega |
|---|---|---|
| `world/` | raia MUNDO | `world/Island.tscn` (raiz Node3D; Marker3D de spawn no grupo `"spawn"`), cel-shading, luz, POIs |
| `characters/` | raia PERSONAGEM | `characters/Mage.tscn` (raiz Node3D "visual puro": mesh+Skeleton+AnimationPlayer; método `play_anim(nome:String)` com `idle`/`run`/`cast`; método `set_tint(Color)` p/ bots) |
| `gameplay/` + `ui/` | raia GAMEPLAY | `gameplay/Main.tscn` (CENA PRINCIPAL: instancia Island + player + bots + HUD), controle 3ª pessoa, gesto único §19.3, Fogo, bots, loop 3min |
| `export/` | raia MUNDO | `export_presets.cfg`, script `build_apk.sh`, config headless |

## O PORTAO

Antes de qualquer entrega, um comando so':

```bash
bash godot/tests/run_all.sh
```

Ele roda os 12 autotestes e **encerra no primeiro que falhar**. Sucesso e' a
linha `ARKANA: 12/12 selftests executados com sucesso.` — nao a mensagem de
commit de ninguem.

**Quem criar selftest novo o acrescenta ao vetor `SELFTESTS` do runner.** Teste
fora da lista nao roda no portao nem no CI: ele nao falha, ele deixa de existir.

**Armadilha medida:** `quit(1)` dentro de `_initialize()` faz o Godot sair com
codigo ZERO — um teste escrito assim passaria calado. Falhe pelo caminho normal
(contar falhas e sair de dentro do `_process`).

## Regras que atravessaram (docs/PONTE.md — só o PROVADO)
- Dano passa por UM lugar (`gameplay/Combat.gd`); NaN se barra com `not (x > 0)`.
- Velocidade é produto único (base × terreno × status) — ninguém escreve direto.
- Número que o DEDO sente vive em **dp** (`Balance.TOUCH`); do MUNDO, em metros.
- Cancelar é estado de 1ª classe: o botão MOSTRA se soltar dispara (anel/X).
- Cor + FORMA sempre (GDD §10). Clima com COR, nunca falta de luz.
- Fogo propaga por ORÇAMENTO, nunca chance por tique (GDD §14 + nota de medição).

## Fiação defensiva (obrigatória)
`Main.tscn` carrega Island/Mage por `load()` com fallback (placeholder simples)
— uma raia atrasada NÃO quebra as outras. Integração final é do coordenador.

## Personagens game-ready
`characters/Mage.tscn` continua sendo a fachada estável da raia PERSONAGEM. Quando
existir `.glb` em `characters/modelos/`, `Mage.gd` pode carregar o modelo externo
e cair automaticamente no procedural se faltar arquivo, `AnimationPlayer` ou as
animações `idle`/`run`/`cast`. Binários 3D e texturas dessa pasta devem usar Git
LFS; a regra de "zero binário grande" segue valendo para o restante do projeto.

## Gates de toda raia
1. `"$LOCALAPPDATA/Programs/godot/Godot_v4.4.1-stable_win64.exe" --headless --path godot --import` sem erro.
2. `--headless --check-only --script` nos .gd tocados (ou boot da cena principal headless por 3s sem erro).
3. Zero binário grande fora de `characters/modelos/`; assets 3D dessa pasta exigem Git LFS.
