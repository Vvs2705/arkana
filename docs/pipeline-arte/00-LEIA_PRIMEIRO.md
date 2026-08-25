# ARKANA — PACOTE DE PRODUÇÃO DE ARTE, 3D, MAPA E ANIMAÇÃO

**Data-base:** 24/08/2026  
**Destino:** equipe de arte / 3D / animação / integração Godot  
**Objetivo:** transformar a nova direção visual de ARKANA em uma fábrica de assets consistente e escalável usando Meshy + Blender + Godot 4.4.

## Ordem obrigatória de leitura

1. `01-DECISOES_E_FONTE_DA_VERDADE.md`
2. `02-MESHY_CAPACIDADES_2026-08-24.md`
3. `03-ORCAMENTO_3000_CREDITOS.md`
4. `PERSONAGENS/00-DIRECAO_VISUAL_PERSONAGENS.md`
5. `PERSONAGENS/01-PIPELINE_PERSONAGEM.md`
6. `CENARIO/00-MAPA_MASTERPLAN.md`
7. `CENARIO/01-BIOMAS_E_POIS.md`
8. `TECH_ART/00-ORCAMENTOS_MOBILE.md`
9. `MESHY/00-API_PLAYBOOK.md`
10. `TECH_ART/04-QA_GATE_DE_ASSET.md`

## Regra principal

A Meshy é **acelerador de produção**, não diretora de arte.

Nenhum asset entra no jogo apenas porque “ficou bonito no Meshy”. Para entrar no ARKANA precisa:

- obedecer à direção **stylized premium**;
- funcionar em terceira pessoa e tela mobile;
- ter silhueta clara;
- respeitar orçamento de geometria, textura e materiais;
- passar por limpeza/validação;
- possuir origem e licença documentadas;
- ser importado e testado no Godot;
- manter identidade visual do universo.

## Pipeline-mãe

`Brief → Concept → Referências multiview → Meshy → seleção → Remesh/Retexture → Blender → Rig/Animação → Godot → Shader/VFX → teste Android/iOS → aprovação`

## O que este ZIP não autoriza

- Não sobrescrever personagens canônicos sem guardar o original.
- Não usar personagem/asset claramente derivado de IP de terceiros.
- Não colocar modelos comunitários sem verificar a licença individual.
- Não exportar pastas de trabalho/intermediários para dentro do APK/AAB.
- Não gastar créditos em lote antes da aprovação do primeiro exemplar da família.
- Não usar 8K apenas porque está disponível.

## Objetivo de qualidade

O alvo é um jogo mobile comercial com leitura de personagens comparável à lógica de produção de títulos estilizados de grande alcance: proporções controladamente exageradas, materiais limpos, silhueta forte, animação expressiva e VFX com alto valor percebido.

Referências como Mobile Legends e Fortnite são referências de **princípios de legibilidade, estilização e produção**, não modelos a copiar.


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
