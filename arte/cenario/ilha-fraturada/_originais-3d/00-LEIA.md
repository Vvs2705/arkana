# Peças originais do Meshy — 3 milhões de faces cada

**NÃO decimadas. É de propósito.**

Estas são as peças como saíram do site da Meshy: ~3.000.000 de faces e textura
2K. O `mobile-godot/` usa versões reduzidas a 800–4.000 faces e textura 512,
porque celular não aguenta o original — mas **reduzida é derivada, e derivada se
refaz**.

O `pc-unreal/` usa ESTAS, quase cruas: com Nanite o Unreal lida com a contagem
original, e toda a esteira de decimação que o mobile exigiu desaparece.

Salvas aqui em 27/08/2026 porque estavam numa pasta temporária do sistema e
teriam sido apagadas na limpeza de disco. Existem também no workspace da Meshy do
Diretor, mas baixar de novo são 10 downloads um a um pelo navegador.

**Fora do git** (ver `.gitignore`): 1,1 GB de binário que se rebaixa. O que o git
guarda é a versão de jogo, não a matéria-prima pesada.

---

## Estado em 04/09/2026 — o que foi gerado e o que falta BAIXAR

**Sete pecas novas foram geradas no SITE da Meshy** (nunca pela API), a partir dos
concepts ja' aprovados em `arte/cenario/`. Todas com **Ultra 2K, Textura ligada e
licenca PRIVADO**. Custo: **215 creditos** (de 1.849 para 1.634).

| peca | concept de origem | familia do kit |
|---|---|---|
| arco partido | `06-ruinas/arco-partido-frente.png` | passagem |
| coluna-braseiro | `06-ruinas/coluna-braseiro-frente.png` | ruina |
| estatua-vigia | `06-ruinas/estatua-vigia-frente.png` | landmark |
| torre arcana | `07-torre-arcana/frente.png` | POI alto |
| obelisco | `08-altar-sintonia/obelisco-frente.png` | ruina |
| plataforma | `08-altar-sintonia/plataforma-frente.png` | ruina |
| braseiro esfera | `08-altar-sintonia/braseiro-esfera.png` | elemental |

**AS SETE ESTAO NA OFICINA DA MESHY E NAO SE PERDEM.** Falta so' BAIXAR.

Baixado ate' agora: **uma**, e na versao SEM TEXTURA
(`27-braseiro-elemental-SEM-TEXTURA.glb`, 54 MB) — a passada de textura sai como
um ativo separado no site, e e' ela que deve substituir este arquivo.

**Por que parou:** a janela do Chrome encolheu para 365x196 no meio do processo e
depois a extensao desconectou. Nada a ver com o Meshy nem com o projeto.

**COMO RETOMAR:** abrir o Chrome logado, ir ao workspace, e para cada uma das sete
pecas texturizadas: clicar na miniatura, botao de download, formato `glb`, Origem
`Fundo`, Baixar. Depois `importar_kit.py`, `semear_kit.py`, `plantar_kit.py`,
`aterrar_kit.py` — nessa ordem.
