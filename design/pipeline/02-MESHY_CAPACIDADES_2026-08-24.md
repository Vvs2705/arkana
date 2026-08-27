# MESHY — O QUE TEMOS DISPONÍVEL E COMO USAR NO ARKANA

## Resumo executivo

Em agosto de 2026, a Meshy já cobre praticamente toda a primeira metade do pipeline 3D:

- Text to Image;
- Image to Image;
- geração multiview;
- Text to 3D;
- Image to 3D;
- Multi-Image to 3D;
- Meshy 7;
- Smart Topology / Meshy T2;
- PBR;
- Retexture;
- Remesh;
- Resize / Convert;
- rig automático;
- animações pré-definidas;
- mais de 500 presets de animação na web;
- Text to Motion na aplicação web;
- 3D Agent para criação conversacional e lotes consistentes;
- Scene Compose para blockout/prototipagem de cenas;
- Discover com modelos comunitários;
- API / Playground / MCP em planos compatíveis.

## Meshy 7

É o modelo 3D mais recente documentado em agosto de 2026.

Uso recomendado no ARKANA:
- personagens principais;
- itens hero;
- peças com silhueta importante;
- reconstrução a partir de turnaround.

`ultra_mode` existe para Meshy 7, porém custa mais e deve ser reservado para testes específicos. Para mobile, detalhe geométrico extremo frequentemente é reduzido depois.

## Multi-Image to 3D

É a ferramenta mais importante para nossos personagens.

A API aceita 1–4 imagens do mesmo objeto. Com Meshy 7, a primeira imagem funciona como vista principal/frontal.

Para personagem aprovado:
1. frente;
2. perfil;
3. costas;
4. 3/4.

Usar as quatro imagens da MESMA master reference.

Também é possível usar 1–4 imagens separadas para guiar a textura no Meshy 7.

## Smart Topology — Meshy T2

Ideal para:
- props;
- pedras;
- cristais;
- móveis;
- objetos modulares;
- vegetação simples;
- peças arquitetônicas menores.

Vantagens:
- geometria gerada já mirando contagem menor;
- partes separadas nativamente;
- `target_polycount` controlável;
- custo significativamente menor que Standard.

Limite documentado na API para T2:
- 100 a 15.000 faces;
- padrão 4.000.

Não usar automaticamente para todos os personagens hero sem comparar deformação/anatomia.

## PBR

A API pode gerar:
- base color;
- normal;
- metallic;
- roughness.

Observação:
- Meshy 6 pode incluir emission em algumas configurações;
- Meshy 7 não fornece emission map no mesmo caminho documentado.

No ARKANA:
- emissão mágica crítica deve ser recriada/controlada no Godot/Blender;
- não depender de emissão “assada” pelo gerador.

## Texturas

A API suporta 2K, 4K e 8K em modelos compatíveis.

Política ARKANA:
- gerar 4K quando necessário para fonte de alta qualidade;
- entregar ao jogo normalmente 2K para hero, 1K para maioria dos props;
- 8K apenas para fonte/marketing ou caso técnico comprovado;
- nunca importar 8K diretamente no mobile sem justificativa.

## Retexture

Meshy 7 suporta retextura guiada por múltiplas imagens.

Uso:
- transformar modelo CC0/Discover em linguagem ARKANA;
- corrigir paleta de um asset aprovado;
- unificar pedra, couro, metal e madeira;
- aproximar o 3D do concept master.

Retexture não substitui correção geométrica.

## Remesh

A API permite:
- `triangle`;
- `quad`;
- meta de polígonos;
- múltiplos formatos.

Uso:
- `triangle` para entrega final/runtime;
- `quad` quando a equipe ainda vai editar/retopologizar;
- nunca assumir que remesh automático resolveu loops de deformação em ombro, joelho, cotovelo, mãos ou rosto.

## Rigging

Webapp:
- humanoides;
- quadrúpedes.

API pública:
- a documentação alerta que o rig programático funciona melhor em humanoides bípedes padrão.

Recomendação:
- humanoides padrão: Meshy Rig como primeira tentativa;
- quadrúpedes: testar pela web e validar;
- Pip, criaturas especiais, caudas, asas, face: rig manual/Blender pode ser obrigatório.

## Animação

A Meshy oferece:
- 500+ presets no módulo Animate;
- biblioteca via API com `action_id`;
- custo baixo por animação na API;
- Text to Motion disponível na aplicação web em 2026.

Usar para:
- idle;
- walk;
- run;
- strafe;
- hit;
- jump;
- ataques genéricos.

Não usar como solução final automática para:
- Suprema;
- conjuração icônica;
- animação facial cinematográfica;
- transformação;
- poderes com mãos/props muito específicos.

## 3D Agent

Excelente para:
- criar famílias de props;
- explorar 6–12 variações de um kit;
- manter um estilo por conversa;
- criar conjuntos de cenário;
- discutir e gerar objetos relacionados.

Não é o melhor método para reproduzir exatamente um personagem canônico. Para personagem: master reference + Multi-Image.

## Scene Compose

Uso no ARKANA:
- montar uma maquete rápida de POI;
- testar escala de ruínas;
- provar que um conjunto de módulos combina.

Depois:
- reconstruir o layout real dentro do Godot.
- Scene Compose não substitui editor de level profissional.

## Discover

Use como biblioteca de matéria-prima.

Prioridade:
1. CC0.
2. estilo compatível.
3. asset fácil de editar.
4. pouca dependência de microtextura.
5. não ser derivado de IP alheio.

A galeria de tags da Meshy informa que assets prontos da galeria podem ser CC0 em várias categorias; confirmar a página individual antes de baixar.

## Retenção da API

PONTO CRÍTICO:
- assets gerados pela API, em planos não-Enterprise, são retidos por no máximo **3 dias**.

Portanto todo job aprovado deve ser baixado automaticamente para armazenamento local/versionado assim que concluir.


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
