# RIGGING E ANIMAÇÃO

## Rig automático Meshy

Use como primeira passagem para humanoides limpos.

Pré-condições:
- T-pose/A-pose;
- braços/pernas separados;
- textura presente quando o endpoint exigir;
- topologia razoável;
- sem capa soldada entre braços e torso.

## Onde revisar pesos
- ombro;
- axila;
- cotovelo;
- punho;
- quadril;
- joelho;
- tornozelo;
- saia/manto;
- cabelo longo;
- manopla pesada.

## Personagens especiais
### Basalto
Provavelmente pede rig customizado ou pelo menos pesos específicos.

### Pip
Asas e escala pequena podem exigir ossos extras.

### Sylva
Galhos/cabelos vegetais podem precisar chains.

### Corvomante
Corvo é rig separado.

### Vitalis
Lúmen deve ser entidade/rig separado.

## Biblioteca Meshy
A web documenta mais de 500 presets.
A API aplica animações por `action_id`.

## Política de produção
Meshy fornece fonte.
No projeto final:
- locomotion compartilhada;
- retarget por família;
- blend no AnimationTree;
- ajustes de timing no Blender/Godot;
- root motion somente quando realmente necessário.

## Text to Motion
Recurso presente no webapp em 2026.
Uso:
- experimentar casting;
- gestos;
- ataques;
- emotes.

Não assumir disponibilidade na API pública. Se a API interna da equipe expuser o recurso, registrar endpoint/versão e custo antes de automação.


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
