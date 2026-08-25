# KITS MODULARES — REGRA DE PRODUÇÃO

## O princípio

Criar peças reutilizáveis, não edifícios únicos.

## Grade
Definir uma unidade modular comum:
- 1m para props;
- módulos arquitetônicos em múltiplos simples;
- pivôs consistentes.

## Ruínas
Gerar conceitos/meshes separados:
- wall_A;
- wall_B_broken;
- corner_A;
- arch_A;
- column_A;
- stairs_A;
- floor_A;
- rubble_A.

## Variação barata
Variação vem de:
- rotação;
- escala controlada;
- material;
- decal;
- musgo;
- quebra;
- vegetação;
- cristais;
- emissive.

## Meshy 3D Agent
Boa utilização:
“Crie uma família coesa de 12 módulos de ruína arcana com as mesmas proporções, materiais e linguagem de formas.”

Não pedir:
“Crie uma cidade inteira em um modelo”.

## Smart Topology
Padrão para a maior parte dos módulos.
Meta inicial:
- 2k–8k faces por módulo;
- 10k–15k só para peça maior/hero.

## Colisão
A colisão do Godot deve ser simplificada.
Nunca usar a malha visual completa automaticamente em todo prop.

## Pivot
- parede: canto inferior;
- coluna: centro da base;
- porta: base;
- árvore: centro da raiz;
- rocha: base;
- prop: ponto lógico de colocação.
