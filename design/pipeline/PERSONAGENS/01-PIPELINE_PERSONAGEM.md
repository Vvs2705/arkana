# PIPELINE DE PERSONAGEM — CONCEPT ATÉ GODOT

## Etapa 1 — ficha
Antes de gerar:
- nome;
- raça;
- altura;
- função;
- afinidade;
- peça assinatura;
- marcas;
- paleta;
- silhueta;
- personalidade;
- itens obrigatórios;
- itens proibidos.

## Etapa 2 — master concept
Criar frente ou 3/4 limpa.
Fundo neutro.
Corpo inteiro.
Sem perspectiva extrema.
Sem VFX cobrindo anatomia.

## Etapa 3 — referências
Gerar:
- frente;
- perfil;
- costas;
- 3/4;
- close rosto;
- peça assinatura isolada;
- marcas/tatuagens isoladas;
- paleta.

Todas derivadas da mesma master.

## Etapa 4 — Multi-Image to 3D
Para hero:
- Meshy 7;
- 4 vistas;
- pose `a-pose` ou `t-pose`;
- textura habilitada;
- PBR habilitado;
- 2K/4K como fonte;
- GLB + FBX quando necessário.

Gerar 1 candidato primeiro.
Só repetir se houver defeito estrutural.

## Etapa 5 — avaliação do bruto
Reprovar imediatamente se:
- rosto mudou;
- roupa virou massa;
- mão deformou severamente;
- peça assinatura sumiu;
- braço/pernas estão fundidos;
- cabelo não tem volume;
- silhueta mudou;
- assimetria crítica desapareceu.

## Etapa 6 — Blender
Obrigatório:
- escala;
- orientação;
- separar peças quando útil;
- revisar mãos/rosto;
- corrigir interseções;
- loops de articulação;
- limpeza;
- UV;
- materiais;
- pivô;
- LODs;
- normals/tangents.

## Etapa 7 — Rig
- humanoide padrão: tentar Meshy;
- revisar pesos;
- corrigir ombros, quadril, joelhos, roupa;
- personagens especiais: Blender.

## Etapa 8 — animação
Aplicar kit base compartilhado.
Criar apenas movimentos assinatura novos.

## Etapa 9 — Godot
- importar GLB;
- material Arkana;
- shader;
- emissive;
- AnimationTree;
- hitboxes;
- VFX;
- teste de câmera;
- teste Android.

## Etapa 10 — aprovação
Só é “final” quando:
- funciona parado;
- funciona correndo;
- funciona atacando;
- funciona de costas;
- funciona pequeno na tela;
- mantém ênfase da master concept.
