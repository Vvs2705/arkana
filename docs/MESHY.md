# Meshy API — geração 3D dos personagens

> Integração da [Meshy](https://www.meshy.ai/) ao pipeline do Arkana: a concept art
> de cada mago vira malha 3D texturizada e riggada, sem escultura manual.
> Complementa [PASSOS_GRAFICOS.md](PASSOS_GRAFICOS.md) — este documento é o **como**;
> aquele é o **porquê** e o plano completo.
>
> **Data:** 21/08/2026 · Documentação lida em `docs.meshy.ai` na mesma data.

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
3. Pronto. O `.gitignore` já bloqueia `.env` (confirmado: regra na linha 34), e o
   script lê a chave de lá sozinho.

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
| `POST /image-to-3d` | concept art → malha texturizada |
| `GET /image-to-3d/{id}` | acompanhar o progresso |
| `POST /rigging` | malha → esqueleto + animações base |
| `GET /rigging/{id}` | acompanhar o rig |

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

### Custos em créditos

| Operação | Créditos |
|---|---|
| Image to 3D (malha + textura) | 5–35 conforme modelo e opções (~20 típico) |
| Remesh | 5 |
| Auto-rigging | 5 |
| Animação | 3 |
| Converter / redimensionar | 1 |

**Estimativa para a Pyra completa:** ~25–40 créditos. Para os 10 magos de
lançamento: ~250–400 créditos — cabe folgado num plano de 1.000/mês.

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

1. Lê `personagens/01-pyra/arte/concept.png` e envia como data URI
   (sem precisar hospedar a imagem em lugar nenhum)
2. Pede malha em T-pose, 15k triângulos, PBR, textura 2k
3. Acompanha o progresso a cada 5s, mostrando a porcentagem
4. Baixa o `.glb` e as texturas (albedo, normal, roughness, metallic)
5. **Lê a altura na ficha do personagem** e passa ao rigger — o Brok tem 1,40m e
   o Basalto 2,30m; mandar 1,7m para todos apagaria a raça
6. Salva tudo em `godot/characters/modelos/01-pyra/`

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
2. **Conferir a fidelidade à concept art** — a manopla de bronze da Pyra, o corvo do
   Corvomante e o fole do Vex podem sair aproximados ou ausentes; peça-assinatura
   costuma exigir modelagem à mão
3. **Ajustar pesos do skinning** — o rig automático erra em roupa larga e capuz
4. **Animações do jogo** — a Meshy dá andar e correr; conjurar, esquivar, cair e
   reviver saem do Mixamo (grátis) ou do Blender
5. **Otimizar draw calls** — juntar materiais num atlas (hoje ~18 por mago; a meta
   é 3–5)

---

## 5. O plano de execução

### Passo 1 — Prova de conceito com a Pyra
Gerar, importar no Godot, **rodar no celular** e julgar: chegou na qualidade da
concept art? Essa resposta decide todo o resto (é a Fase 1 de PASSOS_GRAFICOS.md).

### Passo 2 — Infraestrutura de código *(equipe técnica, sem você)*
- Carregar `.glb` por personagem no lugar da malha procedural do `_lathe`
- Manter o **fallback procedural** para quem ainda não tem modelo
- Git LFS para os binários (`git lfs track "*.glb"`)
- LOD e atlas automáticos no import

### Passo 3 — Escalar
Os 10 magos de lançamento com o pipeline já provado.

---

## 6. Riscos conhecidos

| Risco | Mitigação |
|---|---|
| Malha não fica fiel à concept art | julgar com 1 personagem antes de gastar créditos nos 20 |
| Topologia ruim deforma feio na animação | retopologia no Blender por cima da base |
| Adereço-assinatura sai errado ou some | modelar a peça à mão e acoplar ao rig |
| Créditos acabam no meio | conferir saldo antes de lote; ~25–40 por personagem |
| Binários incham o repositório | Git LFS **antes** do primeiro commit de `.glb` |

---

## Fontes

- [Quick Start — Meshy Docs](https://docs.meshy.ai/en/api/quick-start)
- [Image to 3D API](https://docs.meshy.ai/en/api/image-to-3d)
- [Rigging API](https://docs.meshy.ai/en/api/rigging-and-animation)
- [Pricing](https://docs.meshy.ai/en/api/pricing)
- [Rate Limits](https://docs.meshy.ai/en/api/rate-limits)
