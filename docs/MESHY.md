# Meshy API — geração 3D dos personagens

> Integração da [Meshy](https://www.meshy.ai/) ao pipeline do Arkana: a concept art
> de cada mago vira malha 3D texturizada e riggada, sem escultura manual.
> Complementa [PASSOS_GRAFICOS.md](PASSOS_GRAFICOS.md) — este documento é o **como**;
> aquele é o **porquê** e o plano completo.
>
> **Escrito em:** 21/08/2026 (documentação lida em `docs.meshy.ai` na mesma data)
> **Revisado em:** 24/08/2026 — caminho da imagem de entrada, limitação de vista
> única e retenção de 3 dias.

---

## 1. Antes de tudo: a chave NÃO vem pelo chat

A chave de API é uma credencial. Colada numa conversa, ela fica registrada no
histórico — e qualquer um com acesso a ele passa a poder gastar seus créditos.

**O jeito certo, em 3 passos:**

1. Gere a chave em **https://www.meshy.ai/settings/api**
   (a Meshy só mostra a chave **uma vez** — copie na hora)
2. Crie o arquivo `tools/meshy/.env` com uma linha:
   ```
   MESHY_API_KEY=msy_sua_chave_aqui
   ```
3. Pronto. O `.gitignore` já bloqueia `.env` em qualquer profundidade, e o script
   lê a chave de lá sozinho.

Alternativa, se preferir não criar arquivo:
```bash
export MESHY_API_KEY=msy_sua_chave_aqui
```

**Nunca** commite a chave, não a cole em issue, print ou chat. Se vazar, revogue em
`meshy.ai/settings/api` e gere outra.

---

## 2. O que a API oferece (documentação oficial, lida em 21/08/2026)

| Item | Valor |
|---|---|
| Base URL | `https://api.meshy.ai/openapi/v1` |
| Autenticação | header `Authorization: Bearer $MESHY_API_KEY` |
| Formato | JSON, padrão *criar tarefa → consultar → baixar* |
| Formatos de saída | **glb**, obj, fbx, usdz, stl, 3mf |

### Endpoints que usamos

| Endpoint | Uso no Arkana |
|---|---|
| `POST /image-to-3d` | **é o que usamos** — 1 vista frontal → malha texturizada |
| `GET /image-to-3d/{id}` | acompanhar o progresso |
| `POST /rigging` | malha → esqueleto + animações base |
| `GET /rigging/{id}` | acompanhar o rig |
| `POST /multi-image-to-3d` | **não usamos** — até 4 vistas, daria modelo melhor; bloqueado pelo defeito das vistas (§3) |

### Parâmetros que importam para nós

| Parâmetro | Nosso valor | Por quê |
|---|---|---|
| `target_polycount` | **15000** | orçamento mobile do Arkana (12–20k por mago) |
| `topology` | `triangle` | engine consome triângulo; quad é para escultura |
| `enable_pbr` | `true` | gera normal, roughness e metallic — o que dá volume |
| `texture_resolution` | `2k` | 4k não cabe em celular médio |
| `pose_mode` | `t-pose` | **obrigatório na prática**: o rigger precisa dela |
| `target_formats` | `["glb"]` | glTF é nativo do Godot; **FBX dá problema** |
| `should_remesh` | `true` | sem isso a malha vem densa demais |

### Limites duros (documentados)

- **Rigging exige humanoide bípede** com membros claros. Não funciona em malha sem
  textura nem em não-humanoide.
- **Rigging rejeita acima de 300.000 faces** — por isso pedimos remesh a 15k.
- O personagem precisa **olhar para +Z** (padrão glTF).
- Rigging entrega **FBX e GLB**, com animações base de **andar e correr**.

### Retenção: 3 dias — o risco de perder trabalho pago

Fora do plano Enterprise, a Meshy guarda os assets gerados pela API por **3 dias**.
Depois disso as URLs de download morrem. Um `.glb` que custou 20 créditos e não foi
baixado a tempo tem que ser **pago de novo**.

**Estamos cobertos:** o `meshy.py` baixa cada asset assim que o job termina, dentro
da mesma execução — verificado na função `baixar()`, chamada logo após `esperar()`
em `gerar()` e em `riggar()`. Não existe janela em que o arquivo só esteja no
servidor da Meshy.

**A regra que isso impõe:** nunca disparar um job e sair sem esperar o download.
Se um comando for interrompido no meio, rode `status <id>` **dentro de 3 dias** e
baixe manualmente — depois disso, só pagando outra vez.

### Custos em créditos

| Operação | Créditos |
|---|---|
| Image to 3D (malha + textura) | 5–35 conforme modelo e opções (~20 típico) |
| Remesh | 5 |
| Auto-rigging | 5 |
| Animação | 3 |
| Converter / redimensionar | 1 |

**Medido na Pyra (21/08):** ~41 créditos para o ciclo completo — é o custo de
regenerar o ateliê dela do zero. Para os 10 magos de lançamento, projetar ~410
créditos. Some os 30 queimados na tentativa do *character sheet*: a primeira peça
sempre custa a lição.

### Limites de taxa

| Plano | Req/s | Tarefas simultâneas |
|---|---|---|
| Pro | 20 | 10 |
| Premium | 20 | 30 |
| Ultra | 20 | 100 |

Estourar devolve **429**. Os limites valem **por conta**, somando todas as chaves.
Nosso script gera um personagem por vez — não chega perto do teto.

---

## 3. Como usar

O script é `tools/meshy/meshy.py`. Só precisa de Python — **nenhuma dependência
externa** (usa `urllib` da biblioteca padrão).

```bash
python tools/meshy/meshy.py saldo          # testa a chave
python tools/meshy/meshy.py tudo 01-pyra   # concept -> GLB riggado
```

Outros comandos:

```bash
python tools/meshy/meshy.py gerar 01-pyra    # só a malha
python tools/meshy/meshy.py riggar 01-pyra   # só o rig
python tools/meshy/meshy.py status <id>      # consulta uma tarefa
```

### O que ele faz sozinho

1. Lê `personagens/01-pyra/arte/_originais/master-reference-frente.png` e envia
   como data URI (sem precisar hospedar a imagem em lugar nenhum)
2. Pede malha em T-pose, 15k triângulos, PBR, textura 2k
3. Acompanha o progresso a cada 5s, mostrando a porcentagem
4. **Baixa** o `.glb` e as texturas (albedo, normal, roughness, metallic) assim que
   o job termina — é o que nos protege da retenção de 3 dias
5. **Lê a altura na ficha do personagem** e passa ao rigger — o Brok tem 1,40m e
   o Basalto 2,30m; mandar 1,7m para todos apagaria a raça
6. Salva tudo em `godot/characters/modelos/01-pyra/` — o **ateliê**, fora do git.
   O arquivo que o jogo carrega sai depois, com
   `python tools/meshy/montar_glb.py 01-pyra pyra` → `modelos/pyra.glb`

### Uma imagem só — a limitação de hoje

O script usa **`/image-to-3d`, que aceita UMA imagem**: a frontal.

A Meshy tem `/multi-image-to-3d`, que aceita até 4 vistas e **daria um modelo
melhor** — o perfil é o que resolve a espessura lateral do corpo, justamente o que
uma vista frontal não consegue informar.

**Por que não usamos hoje:** as vistas do lote atual têm defeito medido — a
`vista-3-4.png` é a frontal repetida (12 de 12 conferidos) e a `vista-lateral.png`
é um três-quartos de ~60–70°. **Não existe perfil de 90° no lote.** Mandar as
quatro vistas assim não acrescenta a informação que falta; só gasta crédito.

**O que destrava:** um lote novo com perfil real de 90°. Aí trocar o endpoint vale
a pena. Detalhe da auditoria em [ART.md](ART.md).

**Lição paga com 30 créditos em 21/08:** mandar o *character sheet* inteiro (3
vistas + paleta + insets) como uma imagem faz a Meshy extrudar um **painel plano** —
saiu com 1,9 de largura e 0,05 de profundidade, e o rigging morreu com "pose
estimation failed". **Uma figura por imagem, sempre.**

### Erros que ele traduz

| Código | Significado |
|---|---|
| 401 | chave inválida ou revogada |
| 402 | créditos insuficientes |
| 429 | limite de taxa — esperar |

---

## 4. Depois de gerar: o que ainda é trabalho humano

A Meshy **não** entrega personagem pronto para produção. Ela entrega uma **base boa**
que economiza as etapas mais caras (escultura e retopologia inicial). Continua
faltando:

1. **Revisar a topologia** — geração por IA costuma produzir malha irregular;
   articulações (ombro, cotovelo, joelho) pedem correção no Blender para deformar bem
2. **Conferir a fidelidade à concept art** — **confirmado na Pyra: a manopla de
   bronze saiu ausente.** O corvo do Corvomante e o fole do Vex correm o mesmo
   risco. Peça-assinatura costuma exigir modelagem à mão. Os três caminhos
   possíveis estão em [PASSOS_GRAFICOS.md](PASSOS_GRAFICOS.md) §9 — decisão
   pendente do Diretor
3. **Ajustar pesos do skinning** — o rig automático erra em roupa larga e capuz
4. **Animações do jogo** — a Meshy dá andar e correr; conjurar, esquivar, cair e
   reviver saem do Mixamo (grátis) ou do Blender
5. **Otimizar draw calls** — juntar materiais num atlas (hoje ~18 por mago; a meta
   é 3–5)

---

## 5. O plano de execução

### Passo 1 — Prova de conceito com a Pyra · **parcialmente feito**
- [x] Gerar pela Meshy e importar no Godot — `modelos/pyra.glb` está no jogo
- [ ] **Rodar no celular e julgar** — nunca aconteceu; não houve device em
      `adb devices`. É o portão que decide todo o resto

### Passo 2 — Infraestrutura de código · **feito**
- [x] Carregar `.glb` por personagem no lugar da malha procedural do `_lathe`
- [x] **Fallback procedural** para quem ainda não tem modelo
- [x] Git LFS para os binários
- [ ] LOD e atlas automáticos no import

### Passo 3 — Escalar
Os 10 magos de lançamento. **Só depois do Passo 1 fechado** e da direção de arte
decidida ([ART.md](ART.md)) — escalar antes é assinar 20 retrabalhos.

---

## 6. Riscos conhecidos

| Risco | Mitigação |
|---|---|
| Malha não fica fiel à concept art | julgar com 1 personagem antes de gastar créditos nos 20 |
| Topologia ruim deforma feio na animação | retopologia no Blender por cima da base |
| **Adereço-assinatura some** (aconteceu na Pyra) | três caminhos em PASSOS_GRAFICOS.md §9 — pendente do Diretor |
| **Asset gerado expira em 3 dias e o crédito se perde** | o `meshy.py` baixa na mesma execução; nunca deixar job sem esperar |
| Créditos acabam no meio | conferir saldo antes de lote; ~41 medidos por personagem |
| Binários incham o repositório | Git LFS **antes** do primeiro commit de `.glb`; ateliê fica fora do git |
| Vista única limita a qualidade da malha | só destrava com lote novo de vistas com perfil de 90° (§3) |

---

## Fontes

- [Quick Start — Meshy Docs](https://docs.meshy.ai/en/api/quick-start)
- [Image to 3D API](https://docs.meshy.ai/en/api/image-to-3d)
- [Rigging API](https://docs.meshy.ai/en/api/rigging-and-animation)
- [Pricing](https://docs.meshy.ai/en/api/pricing)
- [Rate Limits](https://docs.meshy.ai/en/api/rate-limits)
