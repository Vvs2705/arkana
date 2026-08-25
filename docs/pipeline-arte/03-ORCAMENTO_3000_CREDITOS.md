# ORÇAMENTO DOS 3.000 CRÉDITOS

## Princípio

Não gastar os 3.000 créditos em “tentativas”. Cada lote deve ter:
- brief;
- referência;
- aprovação visual;
- configuração;
- limite de tentativas.

## Custos de API usados para planejamento conservador

Valores atuais documentados para API:
- Standard Image/Multi-Image to 3D com textura: ~30 créditos;
- Standard sem textura: ~20;
- 8K: ~35;
- Meshy 7 Ultra: +5;
- Smart Topology com textura: ~15;
- Smart Topology sem textura: ~5;
- Retexture 2K/4K: 10;
- Retexture 8K: 15;
- Remesh: 5;
- Rigging API: 5;
- Animation API: 3;
- Convert: 1;
- Resize: 1;
- imagens: aproximadamente 3–12 conforme modelo/operação.

**Atenção:** o webapp atualmente mostra algumas operações de rig como 0 créditos, enquanto a tabela da API registra 5. Para automação via API, orçar pelo valor mais alto até a equipe confirmar o endpoint em produção.

## Plano de alocação sugerido

| Área | Créditos | Objetivo |
|---|---:|---|
| Prova de estilo | 300 | Pyra + Basalto + Pip, referências e testes |
| Personagens 20 — base 3D | 600 | 20 × 30 Standard textured |
| Rig dos 20 | 100 | 20 × 5 API conservador |
| Animações base | 600 | 10 clipes × 20 × 3 |
| Ambiente Smart Topology | 900 | ~60 assets × 15 |
| Retexture/remesh/correções | 300 | correções seletivas |
| Reserva | 200 | falhas, variantes, peças assinatura |
| **TOTAL** | **3.000** | |

## Importante: não gastar as 10 animações em todos os 20 imediatamente

A tabela acima mostra capacidade máxima, não ordem recomendada.

### Ordem real
1. 3 personagens-piloto.
2. 1 kit de cenário.
3. importar no Godot.
4. validar no aparelho.
5. somente depois escalar.

## Economia recomendada

### Personagem
- use Meshy 7 Standard com textura para hero;
- não usar Ultra na primeira geração;
- não gerar 8K;
- não fazer 4 variantes 3D se o concept ainda não foi aprovado.

### Cenário
- Smart Topology T2 por padrão;
- Standard apenas em landmark/hero asset;
- Discover CC0 antes de gerar algo que já existe.

### Animação
- compartilhar locomotion por família;
- não gastar clipe duplicado para 10 humanoides quando um rig compatível permitir retarget;
- Meshy para prototipar/obter fonte, Blender/Godot para reaproveitar.

## Meta de eficiência

O objetivo não é “usar todos os créditos”.
O objetivo é produzir o maior número de **assets aprovados dentro do jogo** por crédito.

Métrica:
`créditos gastos / assets que chegaram ao APK/AAB final`.


## Fontes oficiais Meshy consultadas em 24/08/2026

- https://docs.meshy.ai/en/api
- https://docs.meshy.ai/en/api/pricing
- https://docs.meshy.ai/en/api/image-to-3d
- https://docs.meshy.ai/en/api/multi-image-to-3d
- https://docs.meshy.ai/en/api/remesh
- https://docs.meshy.ai/en/api/retexture
- https://docs.meshy.ai/en/api/rigging
- https://docs.meshy.ai/en/api/animation
- https://docs.meshy.ai/en/api/animation-library
- https://docs.meshy.ai/en/api/text-to-image
- https://docs.meshy.ai/en/api/balance
- https://docs.meshy.ai/en/api/asset-retention
- https://docs.meshy.ai/en/api/webhooks
- https://docs.meshy.ai/en/api/playground
- https://docs.meshy.ai/en/api/changelog
- https://docs.meshy.ai/en/webapp/3d-agent
- https://docs.meshy.ai/en/webapp/guides/animate
- https://docs.meshy.ai/en/webapp/guides/3d-model/rigging
- https://docs.meshy.ai/en/webapp/guides/scene-video/scene-compose
- https://docs.meshy.ai/en/webapp/guides/use-cases/game-assets
- https://docs.meshy.ai/en/webapp/changelog
- https://www.meshy.ai/pt-BR/pricing
- https://help.meshy.ai/en/articles/10000507-how-many-credits-does-each-generation-task-cost
- https://help.meshy.ai/en/articles/9991981-how-do-meshy-credits-work
- https://help.meshy.ai/en/articles/15696428-what-is-included-on-the-free-plan

> A Meshy altera modelos, custos e disponibilidade de recursos com frequência. Antes de qualquer lote grande, consultar novamente `Pricing`, `Changelog` e o custo mostrado no próprio botão/endpoint.
