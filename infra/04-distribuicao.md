# Distribuição — conta de loja, teste fechado, site mínimo

> Fase que ativa: **G6**. O mapa completo de conformidade já existe e continua
> valendo: `docs/ANDROID.md` §3 (diretrizes), §4 (o que falta), §5 (a ordem).
> Este arquivo é o resumo **operacional** — o que se paga, o que se espera, o
> que se hospeda. Todo ato de conta/termo/envio é 🧑 **do Diretor**.

## 1. Contas de loja

| Loja | Custo hoje | Alcançável? |
|---|---|---|
| **Google Play** | **US$ 25, taxa única** — verificar preço vigente | **Sim** — única loja alcançável com o equipamento atual (`docs/ANDROID.md` §3.4) |
| App Store | US$ 99/ano — verificar preço vigente | **Não** — exige macOS + Xcode; entra na conversa quando existir um Mac. **Decisão de compra do Diretor**, não backlog técnico |

Detalhes da conta Play que custam tempo, não dinheiro: verificação de
identidade do titular e, para conta pessoal, o requisito de teste fechado
abaixo. Abrir a conta é a **Fase 4** do `docs/ANDROID.md` §5 — cedo de
propósito, porque o relógio de política só corre depois dela.

## 2. Teste fechado — a dependência mais longa da frente

Contas pessoais novas da Play precisam rodar um **teste fechado com um número
mínimo de testadores por um período mínimo contínuo** antes de poder liberar
produção. Na última verificação a regra era da ordem de **uma dúzia ou mais de
testadores por ~14 dias contínuos** — **verificar na política vigente: os
números E o próprio requisito já mudaram mais de uma vez** (o
`docs/ANDROID.md` §3.4 e §5-Fase 7 mantêm o mesmo aviso).

Operacionalmente, para o Arkana:

- Testadores são pessoas reais com conta Google, aceitas numa lista/grupo, que
  instalam **pela loja** e mantêm o app instalado no período. Recrutar essa
  dúzia (amigos, comunidade do projeto) é trabalho de gente, não de código —
  começar a lista antes de precisar dela.
- Cada subida de build ao teste = `version/code` novo (`03-pipeline-build.md`
  §4).
- O teste fechado é também onde o G5 encontra seus primeiros jogadores de
  multiplayer fora do círculo imediato — as fases se ajudam.

## 3. Classificação etária e formulários

Já decididos ou triviais pela postura do projeto — a infra só executa:

- **IARC/ClassInd**: questionário gratuito dentro do Play Console, respondido
  pelo Diretor 🧑. A resposta-base já está travada no GDD §9: **10+**, combate
  de fantasia sem sangue/gore, **zero caixa aleatória** (a pergunta de
  "compras aleatórias" morre no Juramento §19.1).
- **Segurança de Dados**: com o backend de `02-backend-servicos.md`, a
  declaração continua mínima (sem coleta de dado pessoal persistente; IP
  transitório para conexão a partir do G5). **Verificar formulário vigente**
  na data do envio.
- **Juramento na ficha** desde o dia 1 (GDD §19.1 nº 5) — é texto de loja, não
  infra, mas a ficha não fecha sem ele.

## 4. O site mínimo — política de privacidade obrigatória

A Play **exige URL pública de política de privacidade** na ficha. É a única
peça de "site" que o lançamento precisa. Onde hospedar de graça:

| Opção | Custo | Nota |
|---|---|---|
| **GitHub Pages** ← recomendada | **R$ 0** | O repo já vive no GitHub. Um repo público mínimo (ou pasta `docs/` de um repo dedicado) com 2 páginas estáticas. URL estável `*.github.io` serve para a loja |
| Cloudflare Pages / Netlify / Vercel | R$ 0 (faixas gratuitas — verificar vigentes) | Equivalentes; só valem se o Diretor preferir |
| Domínio próprio `.com.br` (opcional) | ~R$ 40/ano no Registro.br — verificar preço vigente | **Não é requisito de loja.** Vale pela marca quando houver público; decisão futura do Diretor |

Conteúdo mínimo do site (2 páginas estáticas, sem framework, sem analytics —
analytics no site quebraria a postura pela porta dos fundos):

1. **Política de privacidade** — curta e verdadeira: nenhuma coleta de dado
   pessoal; a partir do G5, o parágrafo do IP transitório
   (`02-backend-servicos.md` §6); contato do responsável. LGPD pede
   linguagem clara — a honestidade do projeto torna isso fácil.
2. **Página do jogo** — nome, descrição curta (GDD §9), o **Juramento**
   (§19.1), e-mail de suporte.

E-mail de suporte público: a ficha exige um. Usar um endereço dedicado (ex.:
no domínio vstack já existente) em vez do pessoal — decisão do Diretor 🧑.

## 5. Sequência resumida (espelho da §5 do ANDROID.md, versão Godot)

1. 🧑 Conta Play (US$ 25 — verificar) + rascunho do app com `appId` definitivo.
2. 🧑 Keystore de release + backup fora da máquina (`03-pipeline-build.md` §3).
3. Site mínimo no GitHub Pages com política + Juramento.
4. AAB assinado sobe ao **teste fechado**; recrutar testadores; cumprir o
   período — **verificar exigência vigente**.
5. 🧑 IARC + Segurança de Dados + ficha completa (capturas do aparelho real).
6. Produção.
