# design — a fonte da verdade

**Aqui não tem engine e não tem código.** Só decisão.

| Pasta | O que é |
|---|---|
| `gdd/` | GDD, dano, direção, kits, fusões de manopla, desbloqueio do elenco, itens equipáveis, arte, áudio |
| `personagens/` | as fichas dos 20 magos — identidade, kit, paleta, altura |
| `cenario/` | a Ilha Fraturada, o plano do mapa grande, validações |
| `referencias/` | estudo da Zona (PUBG/Apex/Fortnite medidos), Spellbreak |
| `infra/` | multijogador, servidores, contas, custos, dados e menores |
| `pipeline/` | Meshy, Android, Roblox, pipeline de arte |
| `PROJETO.md` | **a memória do projeto.** CONTINUAR DAQUI sempre no topo |

## A regra

**Muda aqui primeiro, implementa depois. Nunca o contrário.**

Quando `mobile-unity/` e `mobile-godot/` discordarem sobre um número, quem está
certo é o `design/`. E quando o design não disser, é porque a decisão não foi
tomada — aí para e pergunta, não inventa.

## Por que isto sobreviveu a duas trocas de engine

Porque decisão não tem engine. A tabela da Zona calibrada contra PUBG, Apex e
Fortnite; as 10 fusões de manopla; as fichas dos 20 magos; os números de
balanceamento — nada disso mudou quando o projeto foi para Unreal, e nada muda
agora que é Unity. **O código se joga fora; isto aqui não.**

Alguns documentos aqui citam `godot/` ou nomes de script `.gd` ao explicar como
uma regra foi implementada. É referência histórica: o caminho vale como
"veja como o Godot resolveu", nunca como instrução para o Unity.
