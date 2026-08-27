# BLENDER — CHECKLIST DE LIMPEZA

## Geometria
- aplicar transforms;
- escala métrica;
- normals;
- remover faces internas;
- remover objetos perdidos;
- checar non-manifold;
- corrigir clipping;
- separar partes úteis;
- triangulação final controlada.

## Personagem
- loops em juntas;
- mãos;
- dedos;
- rosto;
- ombros;
- quadril;
- roupa;
- cabelo.

## UV
- sem sobreposição acidental;
- padding;
- texel density consistente;
- atlas quando útil.

## Materiais
- reduzir slots;
- nomear;
- separar emissive lógico;
- retirar iluminação assada.

## LOD
Criar LODs antes de integração final.

## Colisão
Props:
- colisão simples.
Personagem:
- capsules/boxes no jogo, não mesh collider visual.

## Export
Preferência Godot:
- GLB/glTF;
- FBX somente quando necessário no pipeline de animação.
