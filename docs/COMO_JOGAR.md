# COMO JOGAR — Arkana: Campo de Provas (Roblox)

> Guia do Diretor. A versão Roblox é o **produto de validação** (docs/ROBLOX.md):
> ela existe para responder se o jogo é divertido, não para ser bonita.

---

## 1. Abrir o jogo

1. Abra o **Roblox Studio** (já instalado; faça login com sua conta na primeira vez).
2. `Arquivo → Abrir do computador` e escolha:
   `C:\Users\VINICIUS\Videos\MEUS PROJETOS\ARKANA\roblox\build\arkana.rbxlx`
3. Aperte **Play** (botão ▶ no topo, ou tecla **F5**).
4. Para sair do teste: **Shift+F5** (ou o botão Stop ■).

### Testar a Sintonia (precisa de 2 magos)
Aba **Test → Clients and Servers → 2 Players → Start**. Abrem duas janelas; cada
uma é um jogador. É a única forma de testar o pilar de inovação sozinho.

### Se você editar o código (sincronização ao vivo)
No terminal, dentro de `roblox/`: `rojo serve`. No Studio: plugin **Rojo → Connect**.
A partir daí, salvar um arquivo `.luau` atualiza o jogo aberto na hora.

---

## 2. Controles

### PC (teclado e mouse)
| Ação | Comando |
|---|---|
| Mover | `W` `A` `S` `D` |
| **Pular** | **Espaço** |
| **Correr** | **Shift esquerdo** (segurar) |
| **Agachar** | **C** (alterna) |
| Mirar | mouse (a direção do cursor no chão) |
| Ataque básico | segurar **botão esquerdo** (atira sozinho na cadência) |
| Magia tática | **botão direito** (usa cooldown, não mana) |
| **Esquiva (i-frames)** | **Q** |
| Trocar elemento | **1–5** (direto) ou **roda do mouse** (cicla) |
| Girar câmera | mouse (padrão do Roblox) |

### Celular / toque
| Ação | Gesto |
|---|---|
| Mover | thumbstick nativo (lado esquerdo) |
| Girar câmera | arrastar no meio/alto da tela |
| **Pular / Correr / Agachar** | botões próprios (PULAR à direita; CORRER e AGACHAR à esquerda) |
| **Atirar mirando** | **pressione o botão da magia e ARRASTE** na direção do alvo → **solte para disparar** |
| Tiro rápido | **toque curto** no botão (atira para onde a câmera olha) |
| Cancelar o tiro | arraste e **volte o dedo ao centro** antes de soltar |
| Esquiva | botão **ESQ** |
| Trocar elemento | tocar no elemento no carrossel |

> O gesto de arrastar-e-soltar é **um movimento só** — foi a correção do que você
> reprovou no teste anterior. Não existe "mirar primeiro e atirar depois".

---

## 3. As regras que importam

**Duas economias separadas (GDD §4):** o ataque básico gasta **mana** (regenera
devagar); a magia tática gasta **cooldown**. Não dá para spammar as duas.

**Escudo de Magia Evolutivo:** você começa com escudo nível 1. **Causar dano**
faz ele evoluir (1→2→3→4: branco → azul → roxo → dourado), aumentando a
capacidade. O HUD mostra o nível em número e em quadradinhos.

**Terreno reativo — o mapa é recurso, não cenário:**
| Faça isso | Acontece |
|---|---|
| Fogo na floresta | incendeia e **se espalha**; depois vira carvão e a cobertura **some** |
| Fogo na grama alta | queima e **revela** quem estava escondido |
| Água no lago | **congela**: a lâmina sobe e vira ponte para atravessar |
| Raio na água | **eletrocuta** toda a água conectada — não pise |
| Terra no chão | ergue **muro de pedra** destrutível |
| Água em terra | **lamaçal**: quem entra fica lento |

**Sintonia (o pilar):** dois magos conjurando **elementos diferentes** no mesmo
alvo dentro de **1,5s** disparam uma **Magia Combinada** (ex.: Fogo + Vento =
Tornado Flamejante). Há ~1s de canalização visível — o inimigo pode interromper
matando um dos dois. **O custo é cobrado no início**: os dois ficam sem mana e
com cooldown longo, mesmo se o combo for interrompido. Combo errado = dupla
vulnerável. Use o botão **"COMBO?"** para propor a fusão a quem está por perto.

**A partida:** lobby → zona arcana fechando em 5 fases (fora dela você toma dano
crescente) → último de pé leva o **Selo do Campeão**, com abates, dano, combos e
elementos usados. Bots preenchem a partida quando faltam jogadores.

---

## 4. O que eu quero que você me diga depois de jogar

1. **Sintonia** é divertida? Vale o risco de ficar sem mana?
2. **TTK**: um duelo parelho deveria durar 1,5–2,5s. Está rápido ou lento demais?
3. **Terreno**: você teve vontade de queimar a floresta ou congelar o lago *de propósito*?
4. Algum **elemento** está claramente melhor que os outros?
5. O **gesto de mira** resolveu o problema do celular?

### Ajustes rápidos (arquivo de dados, não código)
Tudo em `roblox/src/shared/Balance.luau`:
`player.speed` (velocidade) · `sintonia.dmgMult` (força do combo) ·
`terrain.fuelBudget` (tamanho do incêndio) · `match.zone` (ritmo da zona).

---

## 5. Diagnóstico rápido

Na **Command Bar** do Studio (`Exibir → Command Bar`), com o jogo rodando:
```lua
require(game.ServerScriptService.Server.Combat).selfTest()
require(game.ServerScriptService.Server.Sintonia).selfTest()
require(game.ServerScriptService.Server.Match).selfTest()
```
Erros aparecem na aba **Output** (`Exibir → Output`) — é o primeiro lugar para
olhar se algo não funcionar.
