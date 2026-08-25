# NOMENCLATURA E PASTAS

## Nomes

Personagem:
`ARK_CH_01_PYRA_BODY_LOD0.glb`
`ARK_CH_01_PYRA_GAUNTLET.glb`

Ambiente:
`ARK_ENV_FOREST_TREE_A_LOD0.glb`
`ARK_ENV_RUIN_ARCH_A.glb`

Prop:
`ARK_PROP_ARCANE_CRYSTAL_A.glb`

VFX:
`ARK_VFX_FIRE_IMPACT_A.tres`

## Sufixos
`_SRC` fonte
`_RAW` gerado
`_CLEAN` Blender
`_LOD0`
`_LOD1`
`_LOD2`
`_RT` runtime

## Estrutura sugerida

ART_SOURCE/
- Characters/
- Environment/
- Props/
- VFX/
- References/
- Licenses/

GODOT_RUNTIME/
- characters/
- environment/
- props/
- vfx/

Não armazenar intermediário pesado dentro do caminho exportado pelo Godot.
