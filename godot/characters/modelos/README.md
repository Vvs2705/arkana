# Modelos de personagens

Pasta reservada para personagens game-ready importados no Godot.

Contrato recomendado para a prova da Pyra:

- `pyra.glb`: malha low-poly, esqueleto e animacoes compartilhaveis.
- `pyra_albedo.png`: cor base.
- `pyra_normal.png`: normal map.
- `pyra_orm.png`: occlusion, roughness e metallic empacotados em RGB.

Alvos tecnicos iniciais:

- formato: glTF 2.0 binario (`.glb`);
- triangulos: 12k a 20k por mago;
- materiais: 1 principal, 3 a 5 draw calls no maximo;
- ossos: 60 a 90;
- textura: 1024 ou 2048 para mobile;
- import: VRAM Compressed, LOD automatico e compressao ETC2/ASTC no export Android.

Contrato de animacoes:

- o jogo chama sempre `idle`, `run` e `cast`;
- o arquivo externo pode vir com aliases comuns como `Idle`, `Armature|Running`,
  `Spell Cast`, `Attack`, `magic_cast`, `shoot` ou `fireball`;
- `cast` precisa durar mais que `Mage.CAST_FIRE_TIME`, hoje 0.22s;
- se faltar animacao obrigatoria, o jogo volta para o mago procedural.

Inspecao tecnica:

- `Mage.get_model_source()` informa se o visual veio do procedural ou de modelo externo;
- `Mage.get_model_report()` retorna vertices, superficies, materiais, ossos e aliases
  de animacao resolvidos para o modelo carregado.

Nao colocar assets grandes fora do Git LFS.
