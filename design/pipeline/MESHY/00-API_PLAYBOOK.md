# MESHY API — PLAYBOOK PARA A EQUIPE

## Segurança
Nunca:
- commit de API key;
- chave em chat;
- chave em screenshot;
- chave dentro do ZIP do projeto.

Usar variável:
`MESHY_API_KEY`

## Saldo
Endpoint:
`GET /openapi/v1/balance`

Verificar antes e depois de lotes.

## Personagem recomendado — Multi-Image

Fluxo:
1. POST multi-image-to-3d;
2. aguardar via SSE/webhook;
3. GET resultado;
4. baixar GLB/FBX imediatamente;
5. salvar metadata;
6. opcional retexture;
7. remesh;
8. rig;
9. animações.

## Configuração de referência

```json
{
  "ai_model": "meshy-7",
  "image_urls": ["FRONT", "SIDE", "BACK", "THREE_QUARTER"],
  "should_texture": true,
  "enable_pbr": true,
  "texture_resolution": "4k",
  "pose_mode": "a-pose",
  "image_enhancement": false,
  "target_formats": ["glb", "fbx"],
  "multi_view_thumbnails": true
}
```

Ajustar conforme endpoint atual. `image_enhancement=false` é útil quando queremos preservar exatamente a direção estilizada da referência; comparar A/B antes de padronizar.

## Prop Smart Topology

```json
{
  "model_type": "smart-topology",
  "ai_model": "meshy-t2",
  "image_url": "REFERENCE",
  "should_texture": true,
  "enable_pbr": true,
  "target_polycount": 4000,
  "target_formats": ["glb"]
}
```

## Retexture
Use Meshy 7 e multiview quando houver referências consistentes.
Manter UV original quando já estiver boa.

## Remesh
Personagem editável:
- quad quando for para Blender;
- triangle para entrega runtime.

## Webhooks
Preferir webhook/SSE a polling agressivo.

## Retenção
Downloads assinados/outputs da API são temporários.
Regra interna:
**job SUCCEEDED → baixar em minutos, não dias.**

## Metadata obrigatória por job
Salvar JSON/MD com:
- task id;
- data;
- endpoint;
- modelo;
- parâmetros;
- custo;
- origem das referências;
- URLs baixadas;
- hash do arquivo;
- aprovação/reprovação;
- motivo.

## Tarefas criadas por API
A documentação alerta que tarefas da API podem não aparecer em “My Assets”.
Portanto o time não deve depender da interface web para recuperar histórico.


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
