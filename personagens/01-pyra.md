# Pyra, a Chama de Guerra

**Classe:** Vanguarda · **Função:** Ataque / média distância
**Origem do conceito:** partiu de Bangalore (Apex) — **descolada em 20/08/2026** (ordem do Diretor). Ecos: Fullmetal Alchemist (o preço pago no próprio corpo), Escanor/Endeavor (orgulho que queima).
**Fonte do kit:** GDD §3 (revisado 20/08)

## Aparência física
1,78m, postura de sargento; pele morena com cicatrizes de queimadura subindo do ombro esquerdo pelo pescoço até a mandíbula — ela não as esconde, prende o cabelo do lado queimado. **Não tem o braço esquerdo**: no lugar, um braço de CHAMA VIVA contido numa manopla de bronze articulada (perdeu o braço de carne segurando aberto um portão em colapso para a legião dela escapar do incêndio de Vharen). O fogo do braço respira devagar quando ela está calma e ruge quando luta.

## Vestuário
Armadura leve chamuscada sobre túnica de guerra azul-noite; a manopla de bronze do braço esquerdo é a peça central (placas com respiros de fornalha, brasas visíveis nas juntas); bainha de manto curto queimada na barra; botas de marcha. Nenhum adorno — tudo nela já serviu em campo.

## Personalidade & história curta
Fala como quem dá ordem, protege como quem já perdeu gente. Não considera o braço uma perda: "eu troquei um braço por quarenta soldados — foi barato." O que ela não perdoa é fogo desperdiçado.

## Kit
- **Passiva — Coração de Fornalha:** fogo no chão (inclusive o dela) não a machuca e reacende o braço: +10% de velocidade por 2s ao atravessar chamas.
- **Tática — Muralha de Brasas:** risca uma linha de fogo baixo de 8m que dura 5s: bloqueia a visão rasante e quem atravessa leva dano moderado e sai "aceso" (rastro visível por 2s).
- **Suprema — Braço Livre:** destrava a manopla por 6s: o braço vira chama total — os disparos saem contínuos em leque curto (lança-chamas) e o dash dela deixa fogo no chão.

## ⚖️ Limitadores (o preço do poder — parte do kit, não corte)
Água/gelo apagam a muralha e "molham" o braço (+1s de recarga na tática enquanto molhada); vento EMPURRA a muralha 3m (a parede anda — §14); a Suprema é telegrafada com rugido de fornalha + brilho crescente, e ao terminar o braço precisa esfriar: 4s sem tática e −15% de velocidade. Toda a força dela é curta-média — kitá-la de longe é a contra-jogada.

## VFX de assinatura
O braço é fogo cel-shaded com "batimento" (pulsa no ritmo do coração dela); a muralha sobe como brasas varridas por vento; na Suprema a manopla abre em pétalas de bronze e flutua presa por correntes.

## Paleta
Fogo #FF5A2A · azul-noite #2B2E63 · bronze #A8763E

## Notas de modelagem 3D
Silhueta militar assimétrica — a manopla esquerda é 2x o volume do braço direito (leitura instantânea). O braço de chama é shader + partículas, não malha densa. Cicatrizes no pescoço via textura. ~3,5k vértices.

## Arte do Diretor
> Solte os arquivos em `personagens/01-pyra/arte/` (crie a pasta). O que ajuda a
> modelagem 3D: frente/costas/lado, paleta, e qualquer detalhe que não pode se
> perder (a manopla, as cicatrizes, o fogo do braço).

- [ ] concept frente
- [ ] concept costas/lado
- [ ] paleta final
- [ ] extras (adereços, VFX de assinatura)

## Status no jogo
- [ ] arte recebida · [ ] modelo 3D · [ ] rig/animações · [ ] kit implementado
