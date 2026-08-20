# Ilusionista, o Espelho

**Classe:** Guardião · **Função:** Suporte / engano
**Origem do conceito:** partiu de Mirage (Apex) — **descolado em 20/08/2026** (ordem do Diretor). Ecos: contos de espelho (Alice, o Espelho de Ojesed), mágicos de palco da era de ouro.
**Fonte do kit:** GDD §3 (revisado 20/08)

## Aparência física
1,76m, sorriso fácil de showman — e olhos ESPELHADOS: de perto, não têm íris, refletem quem olha. Aprendiz de um mago de espelhos, ficou PRESO dentro de um espelho do mestre por três anos; saiu pelo lado errado: o corpo voltou invertido (o coração bate à direita, o cabelo cai para o lado oposto do que caía, e ele, que era destro, hoje é canhoto). Pior — ou melhor, ele nunca decide: **um dos reflexos do Baile é o original dele, e nem ele sabe qual.** Faz piada disso. A piada nunca chega inteira aos olhos.

## Vestuário
Casaca de gala roxa bordada de fio dourado com os botões do lado ERRADO (alfaiate nenhum conserta — a roupa insiste em ser reflexo); cartola opcional de pose; luvas brancas; abotoaduras de caco de espelho do espelho original (ele guarda todos os cacos).

## Personalidade & história curta
Fala pelos cotovelos, apelida todo mundo no primeiro minuto, odeia silêncio (três anos de silêncio bastaram). Generoso em combate como só quem já ficou sozinho sabe ser: os truques dele existem para os OUTROS escaparem.

## Kit
- **Passiva — Truque de Fuga:** ao ser derrubado, quebra em cacos de luz: fica invisível por 3s e deixa um reflexo caído no lugar.
- **Tática — Espelho de Mão:** conjura um espelho de corpo inteiro fixo por 2s: DEVOLVE até 3 projéteis mágicos como reflexos com 30% do dano, na direção de quem atirou.
- **Suprema — Baile de Espelhos:** 5 reflexos surgem ao redor e ESPELHAM os movimentos dele em tempo real (invertidos, como num salão de baile); ele fica invisível por 2,5s. Reflexo quebrado solta um flash que ofusca por 0,5s quem o quebrou de perto.

## ⚖️ Limitadores (o preço do poder — parte do kit, não corte)
O espelho da tática não bloqueia corpo a corpo nem magias de área, e quebra sozinho após 3 devoluções (som de vidro alto — todos sabem); os reflexos do Baile não causam dano e movem-se INVERTIDOS — observador atento nota o passo trocado; a invisibilidade quebra ao conjurar e tem shimmer visível de perto; o flash de 0,5s só pega a curtíssima distância (quem quebra de longe não paga nada).

## VFX de assinatura
Tudo nele estilhaça e se recompõe em cacos de luz (nunca sangue, nunca dor — vidro e palco); os reflexos têm um brilho de moldura dourada por 1 frame quando surgem; o Espelho de Mão toca um acorde de taça de cristal ao devolver.

## Paleta
Roxo de palco #6B3FA0 · dourado #F0C75E · prata-espelho #D8D8E0

## Notas de modelagem 3D
Mesmo modelo para os reflexos com shader de brilho (barato, já planejado); animação dos reflexos = a dele com mirror no eixo X (de graça no rig). Olhos com material reflexivo simples. ~3k vértices.

## Arte do Diretor
> Solte os arquivos em `personagens/08-ilusionista/arte/` (crie a pasta). O que ajuda a
> modelagem 3D: frente/costas/lado, paleta, e qualquer detalhe que não pode se
> perder (os botões invertidos, os olhos espelhados, as abotoaduras).

- [ ] concept frente
- [ ] concept costas/lado
- [ ] paleta final
- [ ] extras (adereços, VFX de assinatura)

## Status no jogo
- [ ] arte recebida · [ ] modelo 3D · [ ] rig/animações · [ ] kit implementado
