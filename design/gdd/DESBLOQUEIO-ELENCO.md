# DESBLOQUEIO DO ELENCO — economia da moeda por partida

> **Origem:** decisão do Diretor em 26/08/2026 (verbatim): *"no jogo apenas os
> 10 primeiros vão ser incluídos como jogáveis no lançamento, os demais vão ser
> lançados ou cada partida vai dar uma quantidade de pontos ou dinheiro para ir
> comprando os demais, desbloqueando eles, como em todo jogo battle royale."*
> E: *"deveríamos partir para a ideia de produzir todos os personagens"* (os 20
> em 3D).
>
> **Status:** DOCUMENTO DE DESIGN. Nenhuma linha de código nasce daqui até o
> Diretor bater o martelo na seção 6. Tudo marcado PROPOSTA é invenção da
> equipe aguardando veredito; todo fato citado tem o caminho do arquivo.

---

## 1. A regra do lançamento: 10 jogáveis + 10 desbloqueáveis

A semente já existe no código: `godot/menu/Elenco.gd` define os 20 magos com
`id` 1–10 = lançamento e 11–20 = temporadas, e `e_temporada()` já marca os
ids 11–20 com o selo "EM BREVE" na vitrine. A decisão do Diretor transforma
esse selo em **preço**: os 10 bloqueados deixam de ser promessa de temporada e
viram objetivo de jogo — cada partida paga moeda, a moeda compra o mago.

### PROPOSTA: os 10 do lançamento = os ids 1–10 do Elenco.gd

O Diretor disse "os 10 primeiros", e os ids 1–10 já são exatamente o elenco
detalhado do GDD §3 ("Elenco de lançamento — 10 magos detalhados",
`docs/GDD.md`). A cobertura é perfeita por construção: **2 magos por classe,
5 classes** (a taxonomia do GDD §1), os 3 magos com kit implementado estão
todos dentro (`godot/core/Kits.gd`: `01-pyra`, `03-veu`, `10-tessa` com
`implementado: true`), e os 2 únicos modelos 3D prontos ou em veredito também
(Pyra id 1; Brok id 13 é a exceção — ver seção 4).

| Id | Mago | Classe | Justificativa (1 linha) |
|---|---|---|---|
| 1 | Pyra | Vanguarda | Kit IMPLEMENTADO + modelo 3D recriado (aguarda veredito) — o rosto do jogo. |
| 2 | Ceifadora | Vanguarda | 2ª Vanguarda: perseguição/mobilidade, contraste com a Pyra de área. |
| 3 | Véu | Errante | Kit IMPLEMENTADO — a fuga/intangibilidade que ensina o plano espectral. |
| 4 | Corvus | Errante | 2º Errante: rastreio por cheiro + transformação, silhueta única (lobo). |
| 5 | Corvomante | Vidente | Recon de longa distância + o único anti-magia do elenco (GDD §4.7). |
| 6 | Olho-de-Éter | Vidente | 2º Vidente: detecção de conjuração — counterplay do meta de magia. |
| 7 | Vitalis | Guardião | A curandeira: todo BR precisa do suporte de vida no dia 1. |
| 8 | Ilusionista | Guardião | 2º Guardião: proteção por engano — suporte que não é cura. |
| 9 | Vex | Dominador | Controle de área por névoa/veneno — o negador de espaço. |
| 10 | Tessa | Dominador | Kit IMPLEMENTADO — defesa por fios e escudo, fecha as 5 classes em dupla. |

**Ordem de desbloqueio dos 10 restantes (11–20): a ordem de produção 3D da
seção 4** — o mago só entra na vitrine com preço quando tiver modelo e kit
aprovados; até lá permanece "EM BREVE" (o mecanismo atual de `e_temporada()`
não muda, só ganha a segunda fase "com preço").

> ⚖️ **A lista final é DECISÃO DO DIRETOR** (seção 6). A tabela acima é a
> proposta de menor atrito: nenhum dado do `Elenco.gd` muda, nenhum kit
> implementado fica de fora, e a promessa já feita na vitrine é honrada.

---

## 2. A moeda

### 2.1 Nome — PROPOSTA: **Éter**

Curto, pt-BR, já é vocabulário do universo: as mariposas-de-éter do
Olho-de-Éter e o Noctus "Sedento de Éter" (`godot/menu/Elenco.gd`) tratam o
éter como a substância bruta da magia — colecionar éter jogando é coerente
com a ficção. Nomes descartados por conflito interno:

- **Essência** — já é o item de ressurreição do aliado morto (GDD §16.6).
- **Cristais** — já é a moeda PAGA do passe de batalha (GDD §7).
- Alternativas reservas, se o Diretor vetar Éter: **Fragmentos** ou
  **Vestígios** (sem conflito conhecido).

### 2.2 Ganho por partida — estrutura (PROPOSTA, sem número final)

A lição central do dossiê Spellbreak (`docs/referencias/SPELLBREAK.md` §5):
**o problema deles foi retenção, não aquisição** — 10 milhões instalaram e o
jogo não deu motivo para voltar. A moeda por partida é exatamente a resposta:
toda partida, ganhando ou perdendo, move uma barra visível em direção ao
próximo mago. Três parcelas, nesta hierarquia:

| Parcela | Quando paga | Ordem de grandeza relativa (PROPOSTA) |
|---|---|---|
| **Participação** | terminar a partida, qualquer resultado | a BASE — 1× (o piso; proteger o casual é a regra do XP do passe, GDD §7) |
| **Colocação** | por faixa (top 10 / top 5 / top 3 / vitória) | 0 a ~1,5× a base — a vitória no máximo ~2,5× uma partida ruim |
| **Abates** | por abate, com TETO | ~0,1–0,2× a base por abate, teto de ~5 — tempero, nunca o prato |

Regras da estrutura (as mesmas leis que o jogo já usa):

- **Perder também paga.** A participação é a maior parcela isolada — a régua é
  a do XP do passe (GDD §7: "nunca só por vitória — protege o jogador casual").
- **Abate não pode dominar**, ou a moeda ensina o jogo errado (hot-drop e
  morrer). O teto e o peso baixo copiam a lição do escudo evolutivo: todo
  sistema que recompensa desempenho precisa de anti-farm (GDD §5).
- **Dano em bot aliado nunca conta** para nada — a regra geral já escrita no
  GDD §5 vale aqui por herança.

### 2.3 Preço dos magos — a curva (PROPOSTA)

Primeiro desbloqueio rápido (o "gostinho" que fisga), últimos mais caros (o
objetivo de longo prazo). Em unidades de "partidas médias" (uma partida média
= participação + alguma colocação):

| Desbloqueio | Custo-alvo em partidas médias |
|---|---|
| 1º mago | **3–5 partidas** — o jogador novo desbloqueia na primeira sessão longa |
| 2º–4º | ~8–12 partidas cada |
| 5º–7º | ~15–20 partidas cada |
| 8º–10º | ~25–30 partidas cada |
| **Elenco inteiro** | **~150–200 partidas** (~15–25 h de jogo a 4–6 min/partida, GDD §19.2) |

Os números finais (quanto vale a base, o preço em Éter de cada card) são
DECISÃO DO DIRETOR — este documento fixa só a forma da curva e os
tempos-alvo. Quando ele bater o martelo, os valores nascem em um lugar só
(padrão `core/Balance.gd` — dono: coordenador), nunca espalhados.

### 2.4 Regras duras (não são proposta — são as leis já escritas)

1. **Tudo offline/local no lançamento.** O jogo hoje é 100% single-player
   contra bots, sem uma linha de rede (`docs/PROJETO.md` §1–2). Saldo de Éter
   e magos desbloqueados persistem em salvamento local no aparelho — e morrem
   com ele, como todo o resto do progresso (a recomendação "começar sem
   conta" de `docs/infra/04-DADOS-E-MENORES.md` §5.1). Anti-fraude não
   importa agora: não há ranking, não há outro jogador para prejudicar.
2. **Dinheiro real NUNCA compra nem acelera mago.** Juramento de Arkana, GDD
   §19.1.3, corrigido em 18/08 para ser literal: "dinheiro nunca compra
   poder, personagem, cooldown, loot, reroll, boost competitivo nem acesso
   antecipado". A moeda deste documento é 100% gratuita e é a ÚNICA via de
   desbloqueio — para sempre, não só no lançamento.
3. **IAP é decisão futura** e está registrada onde deve:
   `docs/infra/04-DADOS-E-MENORES.md` §5.4 ("nada de compra antes do jogo
   estar de pé"). Nada aqui depende dela.

---

## 3. UX — onde a moeda vive

### 3.1 O momento de recompensa: o fim de partida

É AQUI que a lição do Spellbreak se paga. A tela de fim de partida (vitória ou
derrota) ganha um bloco de recompensa, nesta ordem:

1. **O contador de Éter sobe animado**, parcela por parcela: participação →
   colocação → abates. Três batidas, som próprio (a assinatura em camadas que
   `docs/AUDIO.md` já pratica).
2. **A barra do próximo mago avança na mesma tela**: retrato do mago que o
   jogador está juntando para comprar + "faltam N" — o motivo de voltar fica
   visível no exato momento em que a partida termina. (Se o jogador ainda não
   escolheu um alvo, a barra mostra o mais barato bloqueado.)
3. Botão "JOGAR DE NOVO" logo abaixo — recompensa e reengajamento no mesmo
   quadro.

### 3.2 A vitrine (seleção de magos)

- **Saldo de Éter sempre visível** no canto da vitrine (e só lá + fim de
  partida — a moeda não polui a HUD de combate).
- **O card bloqueado troca "EM BREVE" por preço** quando o mago está
  produzido e aprovado: retrato em silhueta/dessaturado + "🜁 900" (o glifo do
  Éter + valor). O selo "EM BREVE" continua existindo para mago ainda sem
  modelo/kit — são dois estados distintos do mesmo card.
- **Comprar é 1 toque + confirmação** no perfil do mago (a tela de detalhes já
  existente), com a apresentação do mago como celebração da compra.
- Mago bloqueado continua **inspecionável** (história, kit) — vitrine é
  desejo; esconder ficha não vende ninguém.

---

## 4. Plano de produção 3D dos 18 restantes

**Estado real** (`docs/PROJETO.md`, CONTINUAR DAQUI): Pyra recriada no site em
26/08 (aguardando veredito do Diretor), Brok pronto com 13 clipes. **18 magos
sem modelo.** A esteira está provada de ponta a ponta: concept → Meshy
multi-view no webapp → remesh ~15k (grátis) → rig → clipes da biblioteca →
download → `tools/blender/fundir_animacoes.py` → glb no jogo. Custo medido:
**~30 créditos/mago no site. Saldo atual: ~2.994.**

### 4.1 O portão por mago — AUDITAR AS VISTAS ANTES DE GASTAR

Pré-requisito inegociável, é a pendência 1.2 de `docs/PROJETO.md`: **o lote de
concept art (8 vistas/mago, 24/08) tem defeito de ângulo — não há perfil de
90° verdadeiro e a vista "3/4" é a frontal repetida.** Alimentar o multi-view
com essas vistas ensina a espessura lateral errada. A Pyra passou exatamente
porque a recriação usou frente + perfil esquerdo VERDADEIRO + costas, com a
"3/4" excluída de propósito (`docs/PROJETO.md`, leva 4).

Portão por mago, nesta ordem, antes de qualquer crédito:

1. **Auditoria das 4 vistas** do mago em `personagens/*/arte/_originais/`:
   existe perfil de 90° real? A "3/4" é única ou é a frontal repetida? Só
   vistas aprovadas entram no multi-view; vista defeituosa é regerada em 2D
   primeiro (barato) — nunca compensada com crédito de 3D (loteria de 30).
2. **Geração multi-view no SITE, na conta do Diretor** (política webapp-first,
   `docs/MESHY.md`/`docs/PROJETO.md`): Meshy 7 + Ultra + Textura + A-Pose +
   licença PRIVADO — a receita da Pyra.
3. **Revisão NO VIEWER antes de baixar** — regra nascida de correção do
   próprio Diretor em 26/08 (ele pegou 2 defeitos que iam passar na Luva de
   Conjurador). Zoom em mãos, rosto, assinaturas visuais da ficha.
4. Aprovado → remesh ~15k (grátis) → rig na altura da ficha → clipes → fusão
   → selftest de characters. Detalhe 2D que a geração perde 2× vira **decal no
   Blender** (determinístico, grátis), não terceira geração.

### 4.2 Ordem de produção — PROPOSTA

**Os 10 do lançamento primeiro** (são os que o jogador vê no dia 1), kit
implementado na frente (o mago completo — modelo + kit — é o que testa de
verdade):

| Lote | Magos | Critério |
|---|---|---|
| 1 | Véu (3), Tessa (10), Ceifadora (2) | os 2 kits implementados restantes + a 2ª Vanguarda |
| 2 | Vitalis (7), Corvus (4), Vex (9) | suporte de vida do dia 1 + silhuetas distintas (fada, lobo, fole) |
| 3 | Corvomante (5), Olho-de-Éter (6), Ilusionista (8) | fecha o elenco de lançamento |
| 4 | Umbra (12), Gromm (14), Noctus (19) | 1ª leva de desbloqueáveis (perseguição + suporte + vampiro) |
| 5 | Aelion (11), Maris (15), Sylva (17) | 2ª leva |
| 6 | Fizz (16), Basalto (18), Pip (20) | 3ª leva — os 3 de escala extrema (0,95 m / golem 2 t / 0,60 m), deixados por último de propósito: são os que mais desafiam rig e retarget de clipe (a lição da patinação do anão, pendência 1.1) |

- **3 magos por sessão, revisão do Diretor no viewer entre lotes** — a regra
  dele ("revisar ANTES de baixar") vira o ritmo da esteira: nenhum lote novo
  começa com o anterior sem veredito.
- Pyra fica FORA dos lotes: já está gerada, na galeria dele, primeira posição,
  aguardando o veredito que destrava o resto da receita dela.
- Brok (13) já está pronto e é a referência de clipes/fusão para todos.

### 4.3 Custo estimado

| Item | Créditos |
|---|---|
| 18 magos × ~30 (multi-view no site; remesh/rig/animação grátis no webapp) | ~540 |
| Margem para regerações (a taxa real medida: Conjurador precisou de v2; ~30%) | ~160 |
| **Total estimado** | **~700** |
| Saldo atual | ~2.994 |
| **Sobra projetada** | **~2.290** (folga para cenário, adereços e imprevistos) |

Movimentos de IA pagos ("Planar horizontal v2" e "Planar (glide)") já estão na
conta e aplicam em qualquer modelo — custo zero adicional por mago.

---

## 5. O que este documento NÃO decide

- Números finais de ganho e preço (estrutura sim, valores não — seção 6).
- Implementação (persistência, tela, Balance) — nenhuma linha até o martelo.
- Kits dos magos 11–20 — continuam PROPOSTA DA EQUIPE até o Diretor aprovar
  (`godot/menu/Elenco.gd`, cabeçalho; `personagens/00-LEIA.md`).
- Conta/nuvem/IAP — decisões abertas de `docs/infra/04-DADOS-E-MENORES.md` §5,
  do Diretor, intocadas.

---

## 6. DECISÕES DO DIRETOR (bater o martelo)

1. **A lista dos 10 do lançamento** — proposta: os ids 1–10 do `Elenco.gd`
   (tabela da seção 1). Confirma ou troca?
2. **O nome da moeda** — proposta: **Éter** (reservas: Fragmentos, Vestígios).
3. **Os valores finais** — base de ganho por partida e preço em moeda de cada
   desbloqueio (a curva e os tempos-alvo da seção 2.3 são a régua proposta).
4. **Peso relativo: colocação vs abates** — proposta: colocação pesa mais
   (até ~1,5× a base) e abate é tempero com teto. Confirma?
5. **Ordem dos lotes de produção 3D** (seção 4.2) e o veredito da Pyra que
   destrava a esteira.
