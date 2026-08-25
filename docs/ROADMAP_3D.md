# ROADMAP 3D — "Arkana no Android, régua Spellbreak"

> **Ordem do Diretor (19/08):** *"não me importo em seguir mais o roadmap — quero
> avançar direto para modelagem dos personagens e ambientes no Android em 3D. O
> próximo APK que eu testar quero ver realidade de jogo, não mapa de teste."*
>
> Este documento SUBSTITUI o plano faseado de `docs/ANDROID.md` como plano ativo.
> Referência visual/mecânica: **Spellbreak** — 3ª pessoa, magos, cel-shading
> vibrante, mobilidade alta. O GDD continua sendo a fonte da verdade das REGRAS
> (dano/mana/cooldown/terreno reativo); o §19.4 já previa esta migração:
> *"decisão consciente de migrar para Godot 4 levando o GDD"*.

## Veículo: Godot 4.4 (decidido, não discutir de novo)

Godot 4 é a engine do produto: gratuita, exporta APK/AAB direto, usa cenas em
texto versionáveis e possui toolchain Android configurada na máquina. O projeto
vive em `godot/`. O antigo protótipo 2D está descontinuado e foi removido da
árvore atual; seu registro permanece apenas no histórico do Git.

## As fases — cada uma termina com APK no telefone do Diretor

### G0 · Motor de pé *(horas, não dias)*
Godot instalado + templates Android + projeto `godot/` exportando APK.
**Pronto quando:** APK abre no telefone com uma cena 3D girando a 60fps.

### G1 · A FATIA REAL — o próximo APK que o Diretor testa
Nada de mapa de teste. Uma ilha jogável com cara de jogo:
- **Ambiente**: ilha estilizada cel-shaded (colinas, floresta, lago, ruínas —
  os 4 POIs do Roblox reinterpretados em 3D), com a paleta do `ART.md`.
  Atenção: **não existe spec de céu, hora do dia nem iluminação** em documento
  nenhum — o que existe é a paleta e a regra "clima se faz com COR, nunca com
  falta de luz". Quem precisar de mais que isso escreve a spec antes.
- **Personagem**: mago 3D low-poly estilizado (silhueta Spellbreak: manto,
  capuz, mãos que conjuram), com animações idle/correr/conjurar.
- **Câmera 3ª pessoa** sobre o ombro + **controles do GDD §19.3**: joystick
  esquerdo + gesto único (o mesmo que acabou de ser validado no 2D e no Roblox).
- **Fogo completo**: projétil com viagem, impacto com VFX, números de dano.
- **Bots de tiro ao alvo com IA mínima** (perseguem e atacam) numa partida
  curta com vitória/derrota — LOOP, não sandbox.
**Pronto quando:** o Diretor joga uma partida de 3 min e a palavra que sai é
"jogo", não "teste".

### G2 · Os 5 elementos + terreno reativo 3D
Água/Terra/Vento/Raio com formas distintas (cor+FORMA, GDD §10). Floresta queima
e propaga (o modelo por ORÇAMENTO do §14 — nunca chance por tique), lago congela
e vira rota, raio conduz na água. Os números vêm do GDD/Balance, direto.
**Pronto quando:** queimar a floresta abre caminho num APK.

### G3 · Personagens e ambientes de verdade (a "modelagem" pedida)
- Mago com rig completo + variações de cosmético (anti-P2W: só visual).
- 3 biomas com identidade (o mapa deixa de ser uma ilha genérica).
- VFX por elemento na régua Spellbreak (trails, impactos, ultimates visuais).
- Produção de assets: procedural/low-poly autoral primeiro (custo zero);
  geração 3D por IA como acelerador **só com OK do Diretor** (consome créditos).
**Pronto quando:** screenshot do jogo passa por screenshot de jogo publicado.

### G4 · O loop BR completo
Zona que fecha, squads de 2, Sintonia (o pilar — regras prontas do GDD §9),
Grimório, 12+ bots preenchendo, espectador/espírito. Tudo já desenhado e
validado no Roblox — aqui é PORTE pela ponte oficial (GDD + docs/PONTE.md, que
classifica o que é PROVADO e pode atravessar).
**Pronto quando:** uma partida BR completa contra bots, do lobby ao Selo.

### G5 · Multiplayer real *(o degrau honesto de estúdio)*
Godot high-level multiplayer + servidor dedicado. É o item mais caro do mapa
inteiro e o único onde "agressivo" não dobra a física: netcode de BR é obra.
Entra DEPOIS de G1–G4 estarem divertidos contra bots — bots divertidos são
exatamente o Modo Treino Offline que o GDD §19.6 quer como produto.
**Pronto quando:** 2 telefones na mesma partida.

### G6 · Loja
Assinatura, ficha, IARC (10+, já decidido), ícone 512px, capturas — as Fases
4–6 do `docs/ANDROID.md` continuam valendo aqui, sem mudança.

## Regras que NÃO mudam com a agressividade
- GDD é a fonte da verdade; números novos entram nele, não em código solto.
- Anti-P2W / zero caixa aleatória / 10+ (decididos, no GDD §9/§19.1).
- Todo APK entregue = compilado + verificado dentro do pacote, como sempre.
- Gate de cada fase: `godot --headless --export-debug` verde + typecheck dos
  scripts + o Diretor com o APK na mão.

## O que o Roblox vira
**MANTIDO por ordem do Diretor** — é o único lugar com multiplayer real hoje e
segue disponível para playtest de Sintonia com gente. Custo: zero (pronto, 51/51).
