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

## 3. O que está instalado na máquina (medido em 27/08/2026, noite)

O bloqueio de disco que morava aqui **foi resolvido**: ~30 GB liberados de
temporários, Windows antigo e APKs de teste.

| | Estado | Tamanho |
|---|---|---|
| **Unreal Engine 5.8.2** | `C:\Program Files\Epic Games\UE_5.8` | 29,5 GiB |
| **Quixel Bridge (UE 5.8)** | instalado | — |
| **Fab UE Plugin (UE 5.8)** | instalado | — |
| **Lyra Starter Game** | `C:\Users\VINICIUS\Documents\Unreal Projects\LyraStarterGame` | 5,0 GB |
| **Visual Studio** | Build Tools 2022, MSVC 14.44.35207, SDK 10.0.26100 — **sem IDE** | — |

**Atenção ao disco:** sobraram **14 GB livres** depois do Lyra. O DDC (cache de
shaders) do primeiro `open` do Lyra come muitos GB. Se apertar, o que dá para
apagar sem perder nada é o cache de download da Fab (`.../Launcher/VaultCache`,
~5 GB — cópia do que já foi extraído).

## 4. A ordem

| Passo | O quê | Portão |
|---|---|---|
| **0** ✅ | UE5 + Lyra instalados; 4 peças do Meshy com Nanite sobre terreno — **medido abaixo** | falta só o Diretor olhar e aprovar o teto visual |
| **1** | terreno da Ilha Fraturada com o kit esculpido | mapa navegável |
| **2** | movimento, câmera e um disparo — no Lyra, servidor autoritativo | dois clientes na mesma partida |
| **3** | Zona, loot, queda — reimplementados a partir de `design/` | partida completa |
| **4** | elenco, kits, VFX, áudio | jogo bonito de ver em vídeo |
| **5** | EOS, matchmaking, EAC | competitivo honesto |

**Fora desta lista e antes de tudo:** a página da Steam. A receita do primeiro mês
é função da wishlist no dia do lançamento, e a página é o que constrói wishlist.
Ver a análise, §1.2.

---

## 5. Passo 0 — o que foi medido (28/08/2026, 00h20)

Nível `/Game/ARKANA/L_TesteNanite` (Mundo Aberto), com 4 peças importadas
**cruas**, sem passar por Blender e sem decimação:

| Peça | Triângulos Nanite | Fallback | Disco compactado |
|---|---|---|---|
| `19-arco-calcario-nymara` | **3.122.424** | 16.334 | 26,1 MB |
| + `17-rocha-basalto-modular`, `21-pilar-condutor-fulgar`, `29-arvore-folhas-douradas` | mesma ordem de grandeza | | |

**Custo em tela, medido com `stat unit` / `stat rhi`:**

| | |
|---|---|
| Quadro | **19,77 ms (~50 FPS)** |
| Draw (thread de render) | 16,15 ms — **é o gargalo** |
| GPU | 15,51 ms |
| Draw calls | 1.030 |
| Primitivas | 5,3 milhões |
| VRAM | 3,45 de 5,11 GB |

**Como ler isso.** É o **editor**, que carrega painéis, gizmos e seleção — jogo
empacotado corta boa parte do Draw. O gargalo estar no Draw e não na GPU diz que
o Nanite **não** é o problema: 5,3 M de primitivas custaram 15,5 ms de GPU.

**A comparação que importa:** no `mobile-godot/` esta mesma peça precisava ser
decimada de 3 M para **2–8 mil faces** para caber no orçamento do celular. Aqui
ela roda inteira. Toda a esteira `decimar.py` deixa de existir no PC.

**O que ainda NÃO foi feito neste nível:** material do terreno (é o cinza padrão
do Mundo Aberto), iluminação (é a padrão, estourada no horizonte) e colisão das
peças. Nada disso é a arte — é o nível de teste do portão.

