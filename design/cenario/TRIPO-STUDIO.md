# Tripo Studio — análise da conta e como usar (30/09/2026)

Conta **vsouz009**, plano **Max** (mensal, US$ 90): **25.200 créditos**. Vale para o ARKANA e para o **Chronicles of Existence (COE)**.
Decisão do Diretor: **sem API** — criar no site e mandar pelas pontes (DCC Bridge).

## O que o plano Max dá (página de preços, conferida hoje)
- 25.000 créditos/mês (≈ 1.660 modelos), **100 tarefas ao mesmo tempo**, fila dedicada, lote de 30.
- Modelos privados e **uso comercial**; histórico e armazenamento permanentes; exportação ilimitada.
- **Textura 8K ilimitada**, Multi-view → 3D, malha Ultra, **Smart Low Poly**, rig e animação, segmentação por
  partes (transformar, retopologizar e retexturizar parte a parte), Magic Brush, "uma vista → várias vistas".
- Gerador de IMAGEM embutido (Nano Banana, Nano Banana Pro/2, GPT Image 2, Midjourney): **20 grátis por dia**,
  depois 80 % de desconto — serve para fazer as concept arts que faltam e vistas extras.
- 3 "Pro Refine" grátis; acesso antecipado a recursos Beta.

## Quanto custa cada coisa (tabela oficial)
| Operação | Créditos | Extras |
|---|---|---|
| Geração HD | 15 | Ultra +15 · por partes +30 · textura +10 · textura HD +10 · Quad +5 |
| Smart Mesh (malha limpa) | 35 | Smart Mesh P2.0 +65 |
| Segmentação | 5 | completar partes +5/parte |
| Retopologia | 5 | Quad +5 · Smart Lowpoly +30 |
| Textura | 10 | referência de estilo +5 · PBR +5 · HD +10 |
| Rig automático | 20 | — |

Na tela de criação: **H3.1 (qualidade máxima) = 65** com 8K ligado; **H2.5 (clássico) = 50**. Modelos: H3.1 / H3.0 / H2.5.

## Ferramentas do Espaço 3D (barra da esquerda)
Imagem · Modelo (HD ou Malha Smart; entrada por imagem, multivista ou texto) · Dividir · Preencher Partes · Remesh ·
Smart UV · Textura · Editar · Upscale · PBR · Rig · Animar. Upload de modelo próprio (OBJ/FBX/STL/GLB até 150 MB)
para retexturizar ou rigar o que já temos.

## Pontes (DCC Bridge)
- Blender Bridge 1.0.34 (porta local 60600) e Unity Bridge 1.0.14 (porta 60610) instalados.
- **O site liga UM programa por vez** (ligar o Unity desligou o Blender). Fica ligado no **Blender**.
- A ponte do Unity só aceita FBX/OBJ/ZIP e grava em `Assets/TripoModels/`.
- A aba do site precisa ficar aberta: fechar a aba derruba a ponte.

## TESTE A × B (30/09) — VALIDADO com a árvore gigante (102)
Uma geração (H3.1, 300 mil polígonos, textura 2K, Ultra + PBR = **45 créditos**), levada pelos dois caminhos:

| | A: site → Unity direto | B: site → Blender (`otimizar.py`) → Unity |
|---|---|---|
| Triângulos | 293.502 | 45.000 |
| Arquivo | 13,5 MB | 1,8 MB |
| Memória de textura no Unity | 117 MB (3 × 2048, sem compressão) | 13 MB (2 × 1024) |
| Visual na distância de jogo | igual | igual |
| Trabalho | nenhum, mas inviável no celular | 12 s de script automático (tamanho, pé no chão, textura, relevo) |

**Veredito: B (site → Blender → jogo).** Uma árvore crua passaria sozinha do orçamento de um quadro inteiro.
O "direto" só competiria gerando já leve no site (contagem de polígonos baixa ou Remesh pago) — e ainda assim o jogo
precisa de escala, pé no chão e textura 1024, que o script do Blender faz de graça. Vetável.
**Transporte:** a ponte (DCC Bridge) liga, mas o site move o painel dela para uma **janela flutuante (Picture-in-Picture)**
que só abre com clique humano; comandada por mim ela não envia nada. Então: **Exportar → GLB 2K (download)** →
`otimizar.py` → `Resources/`. Com o Diretor clicando, a ponte ao Blender também serve de transporte.
Resultado no jogo: `Resources/tripo-102-arvore-gigante.glb` no centro da Floresta Gigante (205 m) e nas 5 gigantes
secundárias; master cru em `arte/cenario/documento-mestre/102-vegetacao-arvore-gigante-tripo.glb`.

## Lote de validação (30/09) — as 6 famílias no jogo
| Peça | Como | Tris no jogo | Onde |
|---|---|---|---|
| 038 rochedo | HD → Blender | 6 mil | 1 em 3 pedras de encosta (76 no mapa) |
| 047 estátua sem cabeça | HD → Blender | 10 mil | templo (no piso real, `Piso()` por raio) e ilhota do lago |
| 066 gerador | HD → Blender | 8 mil | 4 ao pé dos tanques (R07) |
| 077 caminhão | HD → Blender | 12 mil | 3 em frente ao comando (R08) |
| 081 cristal | HD → Blender | 5 mil | 3 por salão das cavernas (45), brilhando com a própria textura |
| 009 árvore comum | **Malha Smart** (P2.0, triângulos, 3 mil pedidos → 2.079) → Blender só textura 512 | 2 mil | espécie principal da floresta (milhares) |

**Regra nova (validada):** peça que se repete aos MILHARES (árvore, moita, capim) vai pela **Malha Smart** — a HD
reduzida a 3 mil no Blender vira placas lisas; a Smart sai leve e com folha de verdade. Peça única ou de dezenas vai
pela **HD → Blender**. A Malha Smart P2.0 teve 1 teste grátis; o preço normal é 100 créditos (4 variações) + 30 da textura.
**Erro que custou 225 créditos:** na página de resultado, trocar a imagem pelo campo de arquivo NÃO troca a imagem
usada — 5 gerações saíram como a árvore anterior. Agora: página NOVA para cada peça e foto do painel antes de gerar.
Gasto do dia: 45 (árvore gigante) + 270 (lote, 225 perdidos) + 225 (refeitas) + 30 (textura Smart) = 570 → saldo 24.630.
Download: botão Exportar (clique humano). O Chrome passou a bloquear downloads do site depois de 2 seguidos por
script — o Diretor liberou na barra do navegador.

## Fluxo ARKANA (validado em 30/09 — vetável)
1. Site: imagem `-limpo.png` do catálogo → **Modelo HD H3.1**, textura 8K DESLIGADA (celular usa 1024; economiza).
   Peça grande ou de chão (árvore, rocha, estátua) → testar também **Malha Smart** (malha limpa e leve).
2. Enviar ao Blender pela ponte → `otimizar.py` (escala real, textura 1024, sem metal) → `Resources/` → IlhaMestre.
3. Validar a 1ª peça de cada família com foto no celular antes de gerar a família inteira.
4. Orçamento: ~50 peças orgânicas × 65–100 ≈ 3.000–5.000 créditos. Sobra para refazer, rigar bichos e o Chronicles of Existence.

## Chronicles of Existence (COE)
Action RPG mobile em Unity 6 (mesma versão do ARKANA), pasta `MEUS PROJETOS/chronicles-of-existence`, **sem nenhuma
arte própria ainda** — o Tripo é o caminho natural para personagens (rig + animação do site), a vila de Auren e props.
O COE já tem o próprio pipeline (`docs/arte/PIPELINE.md`: Tripo → Blender → Unity, orçamento de tris por categoria,
ASTC) e regra de proveniência (`docs/arte/PROVENIENCIA.md`: só concept PRÓPRIO entra no gerador). A comparação
"Unity direto × Blender" feita no ARKANA vale para ele.

## Creator Hub — Mission TRIPOssible (fundo de projetos)
- Aberto o ano todo, análise contínua, **resposta em até 10 dias úteis**, inscrição **grátis**, sem exclusividade.
- Pode dar: até **US$ 1 milhão**, **geração ilimitada grátis**, suporte técnico dedicado (modelos antes do
  lançamento, plugins sob medida), divulgação (até 10 M de alcance), vitrines e parcerias com marcas.
- Aceita de **ideia a projeto em produção**; **obra já lançada não entra**. Direitos acertados em contrato por projeto.
- Três caminhos: *Made with Tripo* (jogos e mundos imersivos), *Powered by Tripo* (negócio de time pequeno),
  *Impact with Tripo* (educação, cultura).
- Regiões citadas: América do Norte, Europa, Reino Unido, Japão/Coreia, Austrália e regiões de língua chinesa —
  **o Brasil não está na lista**, mas o formulário tem "Other".
- O formulário pede: nome/time, e-mail, região, tipo (3D Artist/Dev…), **portfólio/site (obrigatório)**, redes,
  Discord; depois nome do projeto, o que quer criar, caminho, como o Tripo ajuda, que apoio quer, formato,
  metas/orçamento/prazo.
- **Veredito:** o ARKANA se encaixa bem em *Made with Tripo — Games* (battle royale mobile com a ilha montada com
  Tripo). Inscrever DEPOIS das primeiras peças do Tripo no jogo, com foto e vídeo do celular. O **COE também encaixa
  bem** (RPG sem arte ainda: toda a arte sairia do Tripo) — duas propostas separadas, uma por jogo.

## Outros ganhos
- **Indicação:** 30 créditos por compartilhamento qualificado (com limite diário); amigo que se cadastra = 300 para
  cada um; assinatura do amigo = até US$ 120.
- Nível de criador (EXP e recompensas) e desafios semanais na galeria (ex.: "Zombie Outbreak Challenge").
