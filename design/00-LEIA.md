# design — a fonte da verdade

**Aqui não tem engine e não tem código.** Só decisão.

| Pasta | O que é |
|---|---|
| `gdd/` | GDD, dano, direção, kits, fusões de manopla, desbloqueio do elenco, itens equipáveis, arte, áudio |
| `personagens/` | as fichas dos 20 magos — identidade, kit, paleta, altura |
| `cenario/` | a Ilha Fraturada, o plano do mapa grande, validações |
| `referencias/` | estudo da Zona (PUBG/Apex/Fortnite medidos), Spellbreak, **análise de mercado PC/Steam** |
| `infra/` | multijogador, servidores, contas, custos, dados e menores |
| `pipeline/` | Meshy, Android, Roblox, pipeline de arte |
| `PROJETO.md` | **a memória do projeto.** CONTINUAR DAQUI sempre no topo |

## A regra

**Muda aqui primeiro, implementa depois. Nunca o contrário.**

Quando `pc-unreal/` e `mobile-godot/` discordarem sobre um número, quem está
certo é o `design/`. E quando o design não disser, é porque a decisão não foi
tomada — aí para e pergunta, não inventa.

## Por que isto sobreviveu à troca de engine

Porque decisão não tem engine. A tabela da Zona calibrada contra PUBG, Apex e
Fortnite; as 10 fusões de manopla; as fichas dos 20 magos; os números de
balanceamento — nada disso muda porque o renderizador mudou. **O GDScript se
joga fora; isto aqui não.**
