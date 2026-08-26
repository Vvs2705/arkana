# CASTELO VOADOR — o modelo 3D existe (26/08/2026)

## As duas gerações do dia

| Arquivo (em `origem/`, fora do git) | Via | Tris | Créditos | Papel |
|---|---|---:|---:|---|
| `castelo-site-hipoly.glb` | **webapp, na conta do Diretor** ("Aetherstone Citadel", Meshy 7, Multi-View 3 vistas) | 1.904.098 | 30 | **OFICIAL** — processo visível na plataforma, textura pronta |
| `castelo-api-30k.glb` | API multi-image (4 vistas) | 30.917 | 30 | backup offline + candidato a base low-poly |

Texturas da via API também em `origem/` (albedo/normal/roughness/metallic 2K).
**A cópia de nuvem do oficial é a conta Meshy do Diretor** — foi exigência
dele (DIRECAO.md §10.2) e é a redundância do arquivo local.

## Vistas usadas (multi-view do site)

`arte/frente-selo.png` → principal · `arte/lateral-porta-salto.png` → esquerda
· `arte/costas.png` → trás · direita vazia de propósito (o três-quartos não é
ortogonal e confundiria a fotogrametria).

## Próximos passos (nesta ordem)

1. **Blender:** decimar o hi-poly do site para o alvo de jogo (~25–35k tris)
   OU retopologia usando o `castelo-api-30k` como base e o hi-poly para bake
   de normal — decidir olhando os dois lado a lado.
2. Exportar `castelo.glb` de jogo para `godot/world/modelos/` (este SIM entra
   no git/LFS).
3. Pendurar em `world/Castelo.gd` no lugar das primitivas; colisão continua a
   SIMPLES feita à mão (regra do projeto: nunca trimesh de modelo de IA).
4. FPS no aparelho antes/depois (DIRECAO §10.1, onda 2).
