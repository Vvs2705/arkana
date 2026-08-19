# DECISÕES PENDENTES — as que só o Diretor pode tomar

> **Para que este documento existe:** a equipe acumulou cinco pontos que ela
> **não pode** decidir sozinha — três porque contrariam decisões travadas do GDD
> (que é a fonte da verdade e só o Diretor emenda), e dois porque são escolhas de
> produto, não de engenharia.
>
> Cada item abaixo já vem com **o texto pronto para colar**. A ideia é que
> decidir custe uma palavra, não uma redação.
>
> Nada aqui foi aplicado. Nenhum arquivo do GDD foi alterado pela equipe —
> "qualquer ambiguidade vira pergunta para você, nunca invenção silenciosa"
> (GDD §19.5).
> Criado na R12 (19/08/2026).

---

## 1. Qual é a régua do TTK? *(bloqueia leitura de TODO relatório de combate)*

**O conflito, verificado:**

| Fonte | Faixa | Status |
|---|---|---|
| `docs/GDD.md` linha 168 | **1,5–2,5 s** | fonte da verdade |
| `roblox/tools/sweep.luau` ("alvo da auditoria") | **2,75–3,5 s** | recomendação externa |

O TTK teórico de duelo espelho, direto do `Balance`, é: Fogo 2,95 s · Água 3,05 s ·
Terra 3,17 s · Vento 3,42 s · Raio 2,82 s.

Contra a auditoria: **5 de 5 dentro**. Contra o GDD: **0 de 5 dentro**.
Esse "5/5 no alvo" vinha sendo reportado desde a R7 — contra a régua mais frouxa.

**Hoje** os relatórios mostram as duas e avisam quando discordam. Isso é honesto,
mas não é decisão: enquanto houver duas réguas, nenhum ajuste de `Balance` tem
critério de aceite.

**Se você escolher o GDD (1,5–2,5 s):** o combate inteiro fica ~20% mais letal —
é mexer em dano ou HP dos cinco elementos, não em um.
**Se você escolher a auditoria (2,75–3,5 s):** nada muda no jogo, e o GDD precisa
ser corrigido. Texto pronto para a linha 168 do `docs/GDD.md`:

```
- Vida base: 100. TTK alvo: ~2,75–3,5s em duelo parelho (revisto após a auditoria
  externa de 18/08: a faixa original de 1,5–2,5s foi calibrada antes de existir
  escudo evolutivo, que soma 50 de EHP já no nível 1).
```

> ⚠️ **Verifique a justificativa acima antes de colar.** Ela é a leitura da
> equipe sobre *por que* as faixas divergem, não um fato confirmado por você.

**Decisão:** ______________________

---

## 2. Promover o gesto único ao GDD §19.3 *(bloqueia a Fase 3 do Android)*

**O problema:** o gesto único de mirar-e-atirar (arrastar a partir do botão da
magia e soltar) é **a correção mais importante que este projeto já descobriu para
mobile** — nasceu da reprovação do seu teste de APK em 17/08 e está implementado
no Roblox.

Mas o **GDD §19.3 ainda descreve dois gestos**, e o §9 trava que *"nada técnico
migra do Roblox — o GDD é a única fonte que serve os dois produtos"*. Ou seja:
**implementar o gesto único no Android hoje seria atravessar a ponte errada.**
A equipe parou por isso, de propósito.

**Custo de não decidir:** a Fase 3 do roadmap Android fica bloqueada, e o
protótipo 2D continua com o defeito que você mesmo reprovou.

Texto pronto — substitui o **primeiro marcador** de `docs/GDD.md` §19.3:

```
- Joystick virtual esquerdo (movimento) · lado direito: **mira e disparo em UM
  gesto** — arrastar a partir do botão da magia mira, soltar dispara; toque curto
  dispara na direção da câmera; voltar ao centro cancela. Botões de Tática,
  Esquiva e Suprema seguem o mesmo padrão · troca de elemento em carrossel acima
  dos botões. (Revisto em 19/08 após o teste de APK de 17/08: dois gestos
  separados foram reprovados em aparelho real — PROJETO_PRISMA §0.)
```

**Decisão:** ______________________

---

## 3. Qual build Android vem primeiro? *(bloqueia o planejamento da frente mobile)*

Três documentos do projeto apontam para lugares diferentes, e nenhum está errado:

- **GDD §9** — o destino é *"first-person premium no Android (qualidade régua
  Spell Arena)"*.
- **GDD §8** — a escada de escopo para dev solo põe o **Degrau 3 (mini-BR
  top-down, 16–20 jogadores)** *antes* do **Degrau 4 (3D/primeira pessoa)**, e
  diz que cada degrau é *"um jogo lançável que financia e valida o próximo"*.
- **PROJETO_PRISMA** — recomenda a Rota A e está marcado *"pendente de 1 palavra"*.

**O que muda na prática:** tudo que a próxima fase Android construir. São
produtos diferentes, com custo de produção diferente.

**Decisão:** ______________________

---

## 4. Antecipar o Android contraria o GDD §9 — confirmar ou emendar

O §9 trava: o premium *"só inicia quando o checklist de validação do Roblox
fechar"*. **Nenhuma das 5 perguntas foi respondida com jogador real** — o jogo
nunca foi tocado por terceiros.

Você já decidiu antecipar, e a equipe seguiu. O que falta é **registrar a emenda
no GDD**, senão o documento e a prática ficam em desacordo — e é o documento que
serve as duas frentes.

**O risco concreto, não teórico:** se a V1 ("a Sintonia é divertida entre dois
jogadores?") vier negativa, tudo que a frente Android construir em cima dela é
retrabalho. A V4 já enganou o projeto uma vez com dado de bot (a "dominância do
Fogo" de 1,42× era taxa de acerto do piloto automático; no eixo do `Balance`,
1,06×).

**Meio-termo que a equipe recomenda:** antecipar só o que **não depende** de
V1–V5 — encanamento de build, identidade visual, conta de loja, ficha, conformidade
(Fases 1, 2, 4, 5, 6 do `docs/ANDROID.md`) — e segurar o **design de combate** até
o playtest. É o que já está sendo feito na prática.

**Decisão:** ______________________

---

## 5. Qual é o público-alvo etário? *(bloqueia IARC, ficha de loja e ECA Digital)*

**Buraco encontrado na R11:** o público-alvo etário **nunca foi declarado em
documento nenhum do projeto** — nem no GDD, nem no PROJETO, nem no PRISMA.

Sem essa definição não há como:
- responder o **questionário IARC/ClassInd** (obrigatório para publicar);
- preencher o formulário de **Segurança de Dados** da Play;
- decidir a política de **dados de menores** exigida pelo ECA Digital;
- saber se o app entra nos **programas de família** das lojas (que mudam as
  regras de anúncio, dados e compra).

**O que o jogo já tem a favor de faixa baixa:** GDD §19.1 — zero caixa aleatória,
só cosmético, sem P2W. E o protótipo 2D **não faz nenhuma chamada de rede** (só
`localStorage`), então a resposta natural de coleta de dados é "nenhuma".

**Decisão (faixa e justificativa):** ______________________

---

## Como usar este documento

1. Preencha as cinco linhas de **Decisão**.
2. Para as que têm texto pronto (1 e 2), cole no `docs/GDD.md` — **é ato seu**, o
   GDD é a sua fonte da verdade e a equipe não o edita.
3. Avise a equipe. A partir daí:
   - a decisão **1** destrava o critério de aceite de qualquer ajuste de `Balance`;
   - a **2** destrava a Fase 3 do `docs/ANDROID.md`;
   - a **3** destrava o planejamento da frente mobile;
   - a **4** alinha documento e prática;
   - a **5** destrava as Fases 6 e 7 (ficha e teste fechado).
