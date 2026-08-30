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
| **0** ✅ | UE5 + Lyra; peça do Meshy com Nanite sobre terreno | teto visual aprovado |
| **1** ✅ | **Ilha Fraturada jogável**: terreno, material, água, kit assentado, pontos de partida | **o pawn nasce no chão e anda** |
| **2** | movimento, câmera e um disparo — no Lyra, servidor autoritativo | dois clientes na mesma partida |
| **3** | Zona, loot, queda — reimplementados a partir de `design/` | partida completa |
| **4** | elenco, kits, VFX, áudio | jogo bonito de ver em vídeo |
| **5** | EOS, matchmaking, EAC | competitivo honesto |

**Fora desta lista e antes de tudo:** a página da Steam.

---

## 5. A ilha, medida (30/08/2026)

`/Game/ARKANA/L_IlhaFraturada` — 2.400 m de lado, centrada na **origem**.

| | |
|---|---|
| Envelope | −120.000 a +120.000 cm nos dois eixos, **medido por 58.081 traços** |
| Pico | 11.983 cm (120 m) |
| Fundo | −2.480 cm |
| Acima do mar | 52,4% (o heightmap dizia 52,5%) |
| Peças plantadas | **1.276 assentadas no chão medido**, 1 descartada |
| Pontos de partida | 5 (lago e alagado ficam de fora: o centro deles é água) |
| Quadro | 21,82 ms (~46 FPS), GPU 19,08 ms, 412 draw calls |

### A REGRA QUE ESTA FASE DEIXOU, e ela custou horas

> **O arquivo que entra não é o mundo que sai, e medida de ontem não mede o
> mundo de hoje.** Quem responde onde o chão está é o COLISOR, traçado AGORA.

Eu tinha cravado o centro da ilha em (−1.200, −1.200) m, lido dos LIMITES dos
proxies. Aquilo era a paisagem ANTIGA, de antes de eu refazer o nível. Medida
velha guardada em variável nova é o defeito mais traiçoeiro que existe: não
parece chute, parece medição. Com 1.200 m de deslocamento, metade das peças caía
no mar e o POI do "pico" apontava para um morro de 18,8 m.

### O que a ilha AINDA não tem

- **O kit tem 11 peças das 24 do design.** Faltam as famílias de parede/cânion,
  os cristais elementais e a estátua dos colossos. Nenhuma delas existe na
  oficina da Meshy — conferido peça por peça em 30/08.
- **Lago e alagado não têm marco.** Os POIs existem no terreno; falta vesti-los.
- **Sem nomes de área legíveis da queda** — pedido do Diretor, ainda não feito.
- **Sem loot, sem zona, sem queda.** Isso é o passo 3.

---

