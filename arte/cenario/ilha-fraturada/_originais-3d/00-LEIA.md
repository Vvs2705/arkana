# Peças originais do Meshy — 3 milhões de faces cada

**NÃO decimadas. É de propósito.**

Estas são as peças como saíram do site da Meshy: ~3.000.000 de faces e textura
2K. Celular não aguenta o original: o jogo usa versões reduzidas a 800 a 30 mil
faces, feitas no Blender (`arte/tools/blender/otimizar.py`). **Reduzida é
derivada, e derivada se refaz.** O original é o que se guarda.

Salvas aqui em 27/08/2026 porque estavam numa pasta temporária do sistema e
teriam sido apagadas na limpeza de disco. Existem também no workspace da Meshy do
Diretor, mas baixar de novo são 10 downloads um a um pelo navegador.

**Fora do git** (ver `.gitignore`): 1,1 GB de binário que se rebaixa. O que o git
guarda é a versão de jogo, não a matéria-prima pesada.

---

## Estado em 09/09/2026 — o que foi gerado e o que falta BAIXAR

**Sete peças novas foram geradas no SITE da Meshy** em 04/09 (nunca pela API), a
partir dos concepts já aprovados em `arte/cenario/`. Todas com **Ultra 2K,
Textura ligada e licença PRIVADO**. Custo: **215 créditos** (de 1.849 para 1.634).

| peça | concept de origem | família do kit |
|---|---|---|
| arco partido | `06-ruinas/arco-partido-frente.png` | passagem |
| coluna-braseiro | `06-ruinas/coluna-braseiro-frente.png` | ruína |
| estátua-vigia | `06-ruinas/estatua-vigia-frente.png` | landmark |
| torre arcana | `07-torre-arcana/frente.png` | POI alto |
| obelisco | `08-altar-sintonia/obelisco-frente.png` | ruína |
| plataforma | `08-altar-sintonia/plataforma-frente.png` | ruína |
| braseiro esfera | `08-altar-sintonia/braseiro-esfera.png` | elemental |

**AS SETE ESTÃO NA OFICINA DA MESHY E NÃO SE PERDEM.** Falta só BAIXAR.

Baixado até agora: **uma**, e na versão SEM TEXTURA
(`27-braseiro-elemental-SEM-TEXTURA.glb`, 54 MB) — a passada de textura sai como
um ativo separado no site, e é ela que deve substituir este arquivo.

**COMO RETOMAR:** abrir o Chrome logado, ir ao workspace, e para cada uma das sete
peças texturizadas: clicar na miniatura, botão de download, formato `glb`, Origem
`Fundo`, Baixar. Depois decimar no Blender para o alvo de celular e importar no
`mobile-unity/`.

## O que o kit ainda não tem

O design pede 24 peças; existem 11 no Meshy mais as 7 acima. Faltam a família
**parede/cânion** (parede reta, canto de 90°, coluna isolada, topo de crista),
os **cristais elementais** e a **estátua dos colossos**. Nenhuma existe na
oficina: precisam ser geradas no site. Enquanto não existem, cobertura se
**compõe** com o que há: uma fila de rochas de basalto encostadas lê como crista
de pedra. Foi assim que a ilha do Unreal ganhou muralhas sem peça nova, e a
regra vale igual no Unity.
