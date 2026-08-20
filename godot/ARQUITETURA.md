# ARQUITETURA godot/ — contratos entre raias (R16+)

Dono de `project.godot`, `core/` e deste arquivo: COORDENADOR.

| Pasta | Dona | Entrega |
|---|---|---|
| `world/` | raia MUNDO | `world/Island.tscn` (raiz Node3D; Marker3D de spawn no grupo `"spawn"`), cel-shading, luz, POIs |
| `characters/` | raia PERSONAGEM | `characters/Mage.tscn` (raiz Node3D "visual puro": mesh+Skeleton+AnimationPlayer; método `play_anim(nome:String)` com `idle`/`run`/`cast`; método `set_tint(Color)` p/ bots) |
| `gameplay/` + `ui/` | raia GAMEPLAY | `gameplay/Main.tscn` (CENA PRINCIPAL: instancia Island + player + bots + HUD), controle 3ª pessoa, gesto único §19.3, Fogo, bots, loop 3min |
| `export/` | raia MUNDO | `export_presets.cfg`, script `build_apk.sh`, config headless |

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

## Gates de toda raia
1. `"$LOCALAPPDATA/Programs/godot/Godot_v4.4.1-stable_win64.exe" --headless --path godot --import` sem erro.
2. `--headless --check-only --script` nos .gd tocados (ou boot da cena principal headless por 3s sem erro).
3. Zero binário grande; malha é PROCEDURAL (ArrayMesh/CSG/primitivas) ou .tscn/.tres texto.
