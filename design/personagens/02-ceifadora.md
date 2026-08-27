# Ceifadora, a Voz do Vazio

**Classe:** Vanguarda · **Função:** Ataque / Perseguição
**Origem do conceito:** partiu de Ash (Apex) — **descolada em 20/08/2026** (ordem do Diretor). Ecos: mitos de barqueiro (quem cruza o rio deve algo), kintsugi (o quebrado emendado em ouro), os ecos de Hollow Knight.
**Fonte do kit:** GDD §3 (revisado 20/08)

## Aparência física
1,82m, silhueta afiada e ereta. Ela morreu uma vez — afogada numa fenda do Vazio — e voltou remendada: a pele tem rachaduras finas de porcelana no rosto e nos braços, emendadas com LUZ VIOLETA (kintsugi do Vazio; 10+: bonito, nunca mórbido). **Ela não projeta sombra** — a sombra ficou do outro lado, e essa ausência é a assinatura visual dela em qualquer chão iluminado. Olhos cinza-claros, quase brancos; cabelo preto com uma mecha descolorida pela travessia.

## Vestuário
Elegância fúnebre: casaca longa de gola alta preta, meia-máscara que cobre o maxilar, luvas de aparência líquida. A FOICE espectral não é carregada — ela se condensa na mão quando chamada e evapora depois. Detalhe dourado mínimo (um único fio na costura, como a emenda das rachaduras).

## Personalidade & história curta
Fala baixo porque os caídos falam baixo. Ouve os que acabaram de morrer — não como poder, como condição. Voltou do Vazio com uma dívida que não sabe nomear, e persegue como quem cobra.

## História

**Bio de tela**
> Ela morreu afogada numa fenda do Vazio e voltou remendada — a pele emendada em luz
> violeta, como louça consertada com ouro. O chão embaixo dela é a prova: a sombra
> ficou do outro lado e nunca mais voltou. Quem cai perto dela ainda fala por alguns
> segundos, e ela escuta todos. Não porque queira. Porque não sabe desligar. Voltou
> devendo alguma coisa a alguém, e persegue como quem cobra.

**Fundo**
Antes da fenda, ela tinha nome, ofício e sombra. Depois, tem uma travessia e uma
conta em aberto: alguma coisa do outro lado a empurrou de volta para este, e nada
do outro lado é de graça. Ela não sabe o preço, não sabe o prazo e não sabe quem
cobra — sabe apenas que os caídos falam baixo com ela desde então, e que os ecos
que enxerga são sempre passado, nunca aviso.

Vitalis a procurou uma vez, com a irmã de luz no ombro, para perguntar como se
atravessa a morte e volta com corpo. Ceifadora ouviu a pergunta inteira e devolveu
silêncio — não por crueldade, mas porque a resposta honesta era "não fui eu quem
decidiu". O Ilusionista também lhe fez uma pergunta uma única vez ("sou eu?"), e
recebeu o mesmo nada. Véu, que também voltou de um lugar que não devolve, a
reconhece de longe: as duas se cumprimentam com a cabeça e nunca conversam.

**Assinatura:** *"Alguém pagou para eu voltar. Um dia vem cobrar. Até lá, eu cobro."*

## Kit
- **Passiva — Ecos dos Caídos:** onde alguém morreu há menos de 60s, ela vê o eco espectral dos últimos 3s da luta (um replay fantasma local, visível só para ela).
- **Tática — Mão do Vazio:** marca um ponto a até 12m: uma mão de sombra irrompe do chão e AGARRA o primeiro inimigo na área pequena por 1,2s (ele ainda conjura).
- **Suprema — Travessia:** rasga o Vazio até um ponto em linha reta (até 60m): ela e aliados que tocarem o rasgo em 3s atravessam juntos, instantaneamente.

## ⚖️ Limitadores (o preço do poder — parte do kit, não corte)
A mão de sombra é cortável com 1 golpe corpo a corpo e o alvo agarrado ainda conjura (prende posição, não vira execução); o rasgo da Travessia fica ABERTO por 3s — inimigos podem entrar atrás; quem atravessa sai revelado por 4s e passa 1s sem conjurar (vertigem do Vazio). O eco da passiva só mostra o passado — nunca posição atual.

## VFX de assinatura
Rachaduras de luz violeta que acendem quando ela conjura; a foice se condensa de fumaça em 3 frames; o rasgo da Travessia é um corte vertical de tinta preta com borda dourada; os ecos dos caídos são silhuetas de vidro fosco.

## Paleta
Preto-vazio #1A1A22 · violeta #8A5CF0 · fio dourado #F0C75E

## Notas de modelagem 3D
Silhueta vertical afiada; a foice é adereço condensável (aparece/some por shader, sem troca de malha). A AUSÊNCIA de sombra é feature de render barata (desligar o blob shadow dela). Rachaduras via textura emissiva. ~3k vértices.

> **REFORMULADA 20/08 (a arte MANDA):** kintsugi na PELE (rachaduras douradas), a foice, e ela não projeta sombra. Imagem atual foi gerada com a ficha ANTIGA — regeração pendente (prompt canônico em `arte/prompt.txt`).

## Arte do Diretor
> Solte os arquivos em `personagens/02-ceifadora/arte/` (crie a pasta). O que ajuda a
> modelagem 3D: frente/costas/lado, paleta, e qualquer detalhe que não pode se
> perder (as rachaduras, a meia-máscara, a foice).

- [x] concept frente (`arte/concept.png`, 20/08)
- [ ] concept costas/lado
- [ ] paleta final
- [ ] extras (adereços, VFX de assinatura)

## Status no jogo
- [x] arte recebida · [ ] modelo 3D · [ ] rig/animações · [ ] kit implementado
