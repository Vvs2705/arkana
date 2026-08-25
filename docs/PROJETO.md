# PROJETO — estado atual do Arkana

> **Este é o documento de memória do projeto.** Quem chegar agora deve conseguir
> ler só este arquivo e entender: onde o projeto está, o que já foi feito, o que
> falta e o que está bloqueado. Atualize-o ao fechar cada fase.
>
> **Atualizado em:** 25/08/2026 (higienizacao pos-R20.1)
>
> **REGRA PERMANENTE (ordem do Diretor, 20/08):** ao finalizar QUALQUER fase,
> atualize este documento com **o que foi feito, como e por que**, e reescreva
> a secao CONTINUAR DAQUI com tudo que ainda falta. Esta secao nunca pode
> apontar para uma fase antiga. Este e' o UNICO documento de memoria do
> projeto — nao crie outro.

---

## CONTINUAR DAQUI

### O que ja' foi feito
O jogo e' **jogavel de ponta a ponta no Android**: menu, selecao de mago,
partida contra 6 bots, zona que fecha, habilidades, escudo, estado derrubado,
armas arcanas, bau, terreno reativo com 6 reacoes, HUD completa e audio
sintetizado. **A Pyra tem modelo 3D real** (Meshy, versionado em LFS). Os 20
magos tem ficha aprofundada e **conjunto completo de concept art** (8 vistas
cada, entregue em 24/08). O Roblox segue como campo de provas multiplayer.

### Como foi feito
Raias paralelas com **donos exclusivos de pastas**: o coordenador escreve os
contratos (`core/Balance.gd`, `core/Bus.gd`) ANTES de despachar e faz as
costuras na integracao. **Todo teste novo se prova reintroduzindo o defeito** —
se nao fica vermelho, e' decorativo. Fiacao defensiva: cada cena boota sem as
outras. Gate de entrega: selftests + boot + APK com o conteudo LISTADO dentro
do pacote (nunca confiar em timestamp).

### Por que foi feito assim
O pivo de 19/08 ("quero ver realidade de jogo, nao mapa de teste") levou o
projeto para Godot 3D. **O GDD e' a unica ponte**: o Roblox valida regras com
gente, e nenhum numero de combate muda sem dado humano. As regras duras
(fogo por orcamento, dano num ponto so', dp para o dedo, cancelar como estado
de primeira classe) nao sao estilo — sao cicatrizes de defeitos medidos.

### Ainda falta

**So' o Diretor pode:**
1. **Decidir a direcao de arte dos personagens** (ver secao 4, item 4).
2. Testar o APK no aparelho e dar o veredito.
3. Aprovar ou cortar os kits 11–20 para subirem ao GDD.
4. Playtest do Roblox com gente de verdade (V1–V5). Login e publicacao sao ato dele.
5. Contratar servidor e contas de loja quando chegar a hora (G5/G6) e responder o IARC.

**A equipe faz, em ordem de valor:**
1. **Os 17 kits que faltam** — e' o maior buraco de jogabilidade (secao 3).
2. **A Sintonia** — um dos dois pilares de identidade, sem uma linha em codigo.
3. **Corrigir os angulos das vistas** da concept art: nao existe perfil de 90
   graus no lote, e o "3/4" e' a frontal repetida. Isso trava o multi-imagem
   da Meshy e independe da decisao de estilo.
4. Modelo 3D dos outros 19 magos, quando a direcao estiver decidida.
5. Vozes (560 falas escritas, nenhuma gerada) e arte de UI (68 prompts prontos).
   **Bloqueante para publicar:** o APK ainda usa o icone padrao do Godot.

---

## 1. O produto

Arkana é um **battle royale de magos** em terceira pessoa, para Android. Dois
pilares de identidade:

1. **Sintonia** — dois jogadores combinam elementos numa magia conjunta.
2. **Terreno reativo** — fogo, água, gelo, raio, terra e vento mudam rotas,
   cobertura e risco durante a partida.

O [GDD](GDD.md) é a **fonte da verdade de produto**: toda decisão de design entra
nele antes de virar código. Os números de jogo vivem em `godot/core/Balance.gd`
(combate, terreno, movimento) e `godot/core/Kits.gd` (habilidades).

---

## 2. Onde o projeto está HOJE

O jogo **é jogável de ponta a ponta** num celular Android: menu → seleção de
personagem → partida com bots → fim de partida. O APK de debug sai em
`godot/build/arkana3d.apk`.

### Funcionando e testado

| Sistema | Estado |
|---|---|
| Ilha 3D, terceira pessoa, partida contra 6 bots | ✅ |
| Controles de toque (joystick, mira por gesto, esquiva) | ✅ |
| 5 elementos + terreno reativo | ✅ |
| **Zona que fecha** (5 fases, 90m → 4m em 165s) | ✅ |
| **Habilidades**: passiva/tática/suprema | ✅ sistema + 3 kits (Pyra, Véu, Tessa) |
| **Escudo de Magia Evolutivo** (GDD §5) | ✅ |
| **Estado derrubado + reerguer** | ✅ |
| **Armas arcanas** (varinha, cajado, manopla) + loot no mapa | ✅ |
| **Baú Celestial** (única fonte da manopla) | ✅ |
| Efeitos elementais no alvo + 6 reações | ✅ |
| HUD completa (cooldowns, escudo, zona, baú, derrubado, dano) | ✅ |
| Menu, Configurações (GDD §12), seleção dos 20 magos | ✅ |
| Áudio sintetizado (48 timbres, zero arquivo de áudio) | ✅ |
| **Pyra em 3D** (15.492 tris, PBR, riggada, 3 animações) | ✅ |

### Verificação

Cada pasta tem seu `selftest.gd`. Rodar todos antes de qualquer entrega:

```bash
bash godot/export/build_apk.sh
```

Self-tests individuais:
```bash
"$LOCALAPPDATA/Programs/godot/Godot_v4.4.1-stable_win64.exe" --headless --path godot --script res://gameplay/selftest.gd
```
Existem: `gameplay`, `gameplay/selftest_kits`, `gameplay/selftest_zona`,
`gameplay/selftest_derrubado`, `ui`, `menu`, `characters`, `world`, `terrain`,
`audio`, `juice`.

---

## 3. O que FALTA (em ordem de importância)

### Bloqueiam a experiência
1. **17 dos 20 magos não têm kit.** O sistema está pronto e cadastrar um mago são
   3 passos (`core/Kits.gd` → `gameplay/habilidades/<nome>.gd` → registrar em
   `KitRunner.IMPL`). Hoje quem não tem kit joga sem tática nem suprema.
2. **A Sintonia não existe** (GDD §9) — é um dos dois pilares de identidade do
   jogo e não há uma linha dela em código.
3. **Só a Pyra tem modelo 3D.** Os outros 19 usam o mago procedural genérico.
   Pipeline pronto em `tools/meshy/` (ver [MESHY.md](MESHY.md)).
   ⚠️ **As vistas da concept art tem defeito de angulo** (nao ha perfil de 90
   graus; o "3/4" e' a frontal repetida), entao o fluxo multi-imagem da Meshy
   perde a espessura lateral do corpo. Corrigir antes de escalar o elenco —
   ver `personagens/00-LEIA.md`.

### Qualidade e conteúdo
4. **Vozes**: 560 falas escritas em `audio/vozes/`, **nenhum áudio gerado**. O
   fluxo está no `00-LEIA.txt` de lá (ElevenLabs).
5. **Arte de UI**: 68 prompts prontos em `docs/prompts-arte/`, nada gerado.
   ⚠️ **Bloqueante para publicar**: o APK ainda usa o **ícone padrão do Godot** —
   faltam o ícone do app e a splash (peças L6/L7 e M2 dos prompts).
6. **Runas de aprimoramento** (GDD §16.3): estrutura declarada, não implementada.
7. **Espírito Errante** (§18.6), **Presságios** (§18.4), **Grimório** (§18.3):
   ideias aprovadas, nada em código.

### Dívidas técnicas conhecidas
8. `Bus.damage_dealt` está **sem consumidor de produção** (áudio e HUD migraram
   para `damage_applied`). Pode ser removido junto com `Combat._emitir_legado`.
9. `weapon_equipped` e `bau_canalizando` **não dizem de quem são** — a HUD filtra
   por gambiarra comparando com o slot do player.
10. `Balance.PLAYER.jump` é KNOB **órfão**: nenhum código usa. Ou entra um botão
    de pulo, ou o número sai.
11. Falta animação `"derrubado"`; hoje o caído usa a de locomoção mais lenta.

---

## 4. Decisões que precisam do Diretor

1. **A tabela de fases da zona é PROPOSTA, não spec.** O GDD *pressupõe* a zona
   em três lugares (roadmap, kit do Vidente, resgate de espírito) mas nunca a
   especificou. Os números em `gameplay/Zona.gd` foram projetados pela equipe.
2. **Fim de partida**: com a zona, o timer de 180s virou rede de segurança e o
   fim de verdade é "último em pé". O que acontece ao esgotar o tempo (empate?
   vitória por sobrevivência?) continua não decidido.
3. **Fidelidade dos modelos 3D**: a geração por IA acertou o corpo da Pyra mas
   **perdeu a manopla de bronze**, que é a assinatura dela. Ver
   [PASSOS_GRAFICOS.md](PASSOS_GRAFICOS.md) §9 para os três caminhos possíveis.
4. **A direção de arte dos personagens.** O Diretor pediu sair do "muito
   realismo" para um "3D mais detalhado de alto padrão". A auditoria de 25/08
   mediu que o elenco tem **três linguagens** (7 escultura 3D, 7 pintura
   semi-realista, 6 anime) e que **o eixo não é realismo**: o rosto do Brok,
   que ele aprovou, é mais realista que o da Pyra, que ele rejeitou. O eixo
   real é superfície pintada × esculpida, sombra de contato e proporção
   exagerada. **Perigo:** pedir "menos realismo" a um gerador empurra para
   anime, que já é o grupo que mais quebra o elenco. Evidência completa em
   `personagens/00-LEIA.md` e `docs/ART.md`. **Nada foi decidido.**

---

## 5. Como o repositório está organizado

```
godot/          o jogo (produto principal, Godot 4.4.1 → Android)
  core/         contratos e números: Balance, Kits, Bus, Textos
  gameplay/     partida, combate, zona, loot, baú, derrubado, habilidades/
  characters/   o mago (procedural + modelo externo .glb)
  world/        a ilha procedural e os shaders
  ui/           HUD, botões de toque, área segura
  menu/         menu, configurações, seleção de personagem
  audio/        SFX sintetizados
  export/       build_apk.sh e o setup de máquina
roblox/         "Campo de Provas" — frente ativa de validação humana
personagens/    as 20 fichas + o ateliê de arte de cada um
docs/           GDD (fonte da verdade) e os estudos
  pipeline-arte/  o pacote de pipeline Meshy (concept -> 3D), material de apoio
audio/vozes/    as 560 falas para gerar no ElevenLabs
tools/meshy/    pipeline concept art → personagem 3D riggado
infra/          planejamento de servidores, custos e distribuição
```

### O que NÃO é versionado (e por quê)
- `personagens/*/arte/_originais/` — o **ateliê de arte**: 8 vistas por mago,
  307 MB. É matéria-prima da modelagem, não produto. O produto versionado é o
  retrato de 512px em `godot/menu/art/NN.png`, que entra no APK.
- `godot/build/` — APKs, artefato gerado.
- `godot/characters/modelos/<slug>/` — a **oficina 3D** (malha crua, FBX,
  texturas soltas, animações separadas). São ~102 MB por personagem e
  **regeneráveis** com dois comandos do `tools/meshy/`. Só o `<nome>.glb` que o
  jogo carrega é versionado.
- `tools/meshy/.env` — a chave da API Meshy. **Nunca commitar.**

---

## 6. Frente paralela: Roblox

`roblox/` é o **Campo de Provas**: o ambiente multiplayer para testar com pessoas
o que bots não validam — Sintonia, TTK, leitura do terreno, equilíbrio dos
elementos e vontade de jogar de novo. Tem servidor autoritativo, duplas, terreno
reativo, os dez combos de Sintonia, loop de BR, bots, acessibilidade e telemetria.

**O bloqueio não é técnico:** falta executar o playtest humano descrito em
[ROBLOX.md](ROBLOX.md).

---

## 7. O que foi encerrado

O protótipo 2D em Phaser/TypeScript/Capacitor foi descontinuado em 19/08/2026.
Validou as primeiras mecânicas e o toque, mas não é frente de produto e não
recebe manutenção. Continua recuperável pelo histórico do Git.

---

## 8. Linha do tempo

| Data | Marco |
|---|---|
| 17/08 | Do zero ao protótipo jogável (2D), com 4 raias em paralelo |
| 19/08 | 2D encerrado; elenco cresce para 20 magos |
| 20/08 | Consolidação em Godot 3D + Roblox; concept art dos 20 |
| **21/08** | **R20/R20.1** — elenco descolado do Apex com marcas mágicas próprias; Pyra em 3D pela Meshy; zona, habilidades, escudo, derrubado, armas arcanas, baú e HUD completa; área segura corrigida; estudo de dano |

| 24/08 | Novo lote de concept art dos 20 magos (8 vistas cada) |
| **25/08** | **Higienização**: -398 MB em duplicatas e material superseded; ateliê de arte fora do git; pipeline Meshy achatado em `docs/pipeline-arte/`; `meshy.py` consertado; auditoria visual dos 20 e da validade do pacote de pipeline |

Detalhe de cada mudança: `git log` e o [CHANGELOG](../CHANGELOG.md).
