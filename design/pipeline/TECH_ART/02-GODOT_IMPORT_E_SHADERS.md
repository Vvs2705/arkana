# GODOT — IMPORTAÇÃO E SHADERS

## Regra de pasta
Assets de trabalho NÃO devem ficar dentro do `res://` exportável sem filtro.

Separar:
- source/raw fora do runtime;
- optimized dentro do projeto.

## Import
- escala;
- orientação;
- skeleton;
- animations;
- materiais;
- LOD;
- compressão;
- filtros de textura.

## Shader Arkana
Base:
- toon/stylized;
- ramp de iluminação;
- rim controlado;
- specular simplificado;
- emissive mágico;
- outline somente se leitura justificar;
- fog integrada.

## Material global
Criar famílias:
- skin;
- cloth;
- leather;
- metal;
- stone;
- wood;
- foliage;
- crystal;
- magic.

## Não
Cada asset possuir shader exclusivo.

## Emissive
Magia precisa de máscara controlável:
- intensidade;
- cor;
- pulso;
- estado de habilidade;
- cooldown.

## Performance
Medir:
- draw calls;
- overdraw;
- transparência;
- partículas;
- luzes;
- sombras;
- tamanho de textura.
