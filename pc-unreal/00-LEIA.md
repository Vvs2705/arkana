# pc-unreal — o produto

Aqui mora o jogo que vai para a Steam. **Ainda vazio**: a decisão de migrar é de
27/08/2026 e o primeiro passo está bloqueado por disco (§3).

---

## 1. As diretrizes desta pasta

### 1.1 Autoridade de servidor desde a primeira linha

**Não existe versão 1 client-authoritative que depois "vira" autoritativa.**
Retrofitar autoridade é reescrever o jogo — foi o motivo principal de não portar
o código do `mobile-godot/`, onde dano, mana, posição, loot e zona são todos
decididos pelo cliente.

A regra:

> O cliente manda **INTENÇÃO** (andei para lá, apertei atirar).
> O servidor decide **RESULTADO** (acertou, tomou dano, pegou o item).
> O cliente **só desenha**.

### 1.2 Nada de código vindo do `mobile-godot/`

GDScript não porta para Unreal, e não deve. O que atravessa é a **decisão**, que
está em `design/`. Os números de balanceamento, a tabela da Zona, as regras de
loot e os kits são reimplementados **lendo o design**, não traduzindo código.

### 1.3 A arte vem de `arte/`, não é copiada para cá

As peças `.glb` e as concepts vivem em `arte/`. Esta pasta importa delas. No dia
em que uma peça for regerada, ela muda em um lugar só.

### 1.4 Sem dado pessoal

Login pela Steam/EOS, e o jogo guarda **um ID opaco da plataforma** e estatística
de partida. Nada mais. É a decisão de arquitetura mais barata do projeto: sem
dado pessoal, a superfície de LGPD encolhe para quase nada.
Detalhe em `design/referencias/PC-STEAM-ANALISE.md` §6.

---

## 2. Por que Unreal, e não Godot

Resumo; o desenvolvimento está na análise de mercado.

| | |
|---|---|
| **A frase que decidiu** | as ferramentas de multiplayer do Godot servem a sessões de 2–8 jogadores, e a recomendação pública é "pensar duas vezes" para **netcode competitivo com predição, sessões acima de 40, ou servidor dedicado** — as três coisas que a ARKANA precisa |
| **Anti-cheat** | EAC é produto da própria Epic, nativo no Unreal. Em Godot **não há caso público documentado** de EAC funcionando |
| **Nanite** | as peças do Meshy têm **3 milhões de faces** e rodariam quase cruas. Toda a esteira de decimação que o mobile exigiu desaparece |
| **Lyra** | sample oficial da Epic: um shooter multiplayer **completo**, com EOS e servidor dedicado, já funcionando. É o esqueleto que levaria meses |
| **Custo** | 0% de royalty até US$ 1 M de receita vitalícia por título; 5% acima |

**O risco assumido:** Unreal é pesado, a compilação é longa e a curva é real.
Aceito conscientemente pelo Diretor: *"mesmo que tenhamos uma build demorada e
teremos que aprender, se isso for o melhor para o futuro vamos seguir"*.

---

## 3. ⚠️ BLOQUEIO ATUAL: disco

**Medido em 27/08/2026:** a máquina tem **um único disco (C:)** com **28,3 GB
livres** de 477 GB.

| Precisa | Tamanho |
|---|---|
| Unreal Engine 5 (instalação básica) | ~18 GB |
| Lyra Starter Game | ~30 GB |
| **Mínimo** | **~48 GB** |
| Derived Data Cache + projeto compilado | cresce muito além disso |

**Não cabe.** Antes de instalar qualquer coisa é preciso liberar espaço ou somar
um disco. Não é problema de código e não tem contorno técnico.

O que dá para liberar sem perder nada:
- `mobile-godot/godot/build/testes/` — 16 APKs de teste, ~1,6 GB. O próprio
  `00-LEIA.md` de lá diz que são descartáveis
- caches de importação do Godot (`.godot/`), regeneráveis com um comando

Isso soma ~2 GB. **Falta muito mais**, e a decisão é do Diretor.

---

## 4. A ordem, quando o disco permitir

| Passo | O quê | Portão |
|---|---|---|
| **0** | instalar UE5 + Lyra; pôr uma peça do Meshy em 3 M de faces com Nanite sobre um terreno | o Diretor olha e aprova o teto visual |
| **1** | terreno da Ilha Fraturada com o kit esculpido | mapa navegável |
| **2** | movimento, câmera e um disparo — no Lyra, servidor autoritativo | dois clientes na mesma partida |
| **3** | Zona, loot, queda — reimplementados a partir de `design/` | partida completa |
| **4** | elenco, kits, VFX, áudio | jogo bonito de ver em vídeo |
| **5** | EOS, matchmaking, EAC | competitivo honesto |

**Fora desta lista e antes de tudo:** a página da Steam. A receita do primeiro mês
é função da wishlist no dia do lançamento, e a página é o que constrói wishlist.
Ver a análise, §1.2.
