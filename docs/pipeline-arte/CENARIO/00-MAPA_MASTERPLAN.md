# MAPA — MASTERPLAN VISUAL E DE PRODUÇÃO

## Objetivo

O mapa não é decoração. Ele é um sistema de combate.

Pilares mecânicos já existentes:
- floresta pode queimar;
- grama alta pode desaparecer/revelar;
- água pode congelar;
- raio conduz pela água;
- terra cria cobertura;
- água + terra gera lama;
- vento desloca/dissipa estados.

A direção visual deve tornar essas possibilidades previsíveis.

## Estrutura macro sugerida

Criar 3 macrobiomas + 4 POIs principais.

### Bioma A — Floresta Arcana
Funções:
- combustível;
- cobertura;
- linhas de visão;
- emboscada;
- caminho destrutível.

### Bioma B — Bacia / Lago Elemental
Funções:
- rota aberta;
- gelo cria ponte;
- raio cria risco;
- margem vira lama.

### Bioma C — Ruínas / Colinas Arcanas
Funções:
- verticalidade;
- cobertura de pedra;
- corredores;
- torres;
- pontos de leitura do mapa.

## 4 POIs
POI-01 — Ruínas Centrais / Conclave  
POI-02 — Torre Arcana / ponto alto  
POI-03 — Lago / Bacia ritual  
POI-04 — Ruína de borda / Forja ou santuário

Nomes finais dependem do lore. A função de gameplay é obrigatória.

## Fluxo de criação do mapa

1. greybox no Godot;
2. teste de distância;
3. teste de combate;
4. colocar proxies;
5. definir kits;
6. produzir master asset por kit;
7. povoar POI;
8. vegetação;
9. decals;
10. iluminação;
11. terreno reativo;
12. LOD/HLOD;
13. teste mobile.

## Regra
Nunca construir uma área inteira no Meshy como uma única malha.

O mapa deve ser modular para:
- culling;
- LOD;
- colisão;
- destruição;
- variação;
- terreno reativo;
- manutenção.
