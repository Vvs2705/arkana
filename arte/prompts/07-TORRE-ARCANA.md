# 07 — TORRE ARCANA (landmark com função de gameplay)

## 0. O que é

Estrutura vertical de POI citada no GDD (o corvo do Corvomante "escaneia baús
e Torres Arcanas"). Função dupla: **marco de navegação** visível de longe
(inclusive da queda, a 200 m) e **ponto de disputa** — quem sobe vê mais.
Geração autoral por regra do pipeline (`CENARIO/04`: torres com função de
gameplay nunca vêm do Discover).

## 1. Ficha física

- **Altura total: 22 m** · base octogonal de **7 m** de diâmetro afinando para
  **4,5 m** no topo. Três andares marcados por cornijas salientes de pedra.
- **Materiais:** alvenaria de pedra cinza-azulada (`#8E97AD`) em fiadas
  irregulares — pedras de 30–80 cm, juntas fundas — com **cintas de bronze**
  horizontais nas cornijas (10 cm, rebitadas, pátina verde nos vãos).
- **O TOPO é a identidade:** a torre termina numa **coroa de quatro dentes de
  pedra** inclinados para dentro, e suspenso ENTRE eles — sem tocar nada —
  flutua um **cristal octaédrico de 1,6 m** girando devagar (1 volta / 8 s),
  âmbar (`#F0C75E`), com o núcleo aceso e arestas lascadas. Anéis finos de
  runas de luz orbitam o cristal em dois eixos.
- **Escada externa em espiral** de pedra, 80 cm de largura, agarrada ao corpo
  da torre, com corrimão de corda ancorado em argolas de bronze — degraus
  gastos no centro (400 anos de pés). A escada dá 1,75 volta até a
  **plataforma de observação** no segundo andar: sacada octogonal com mureta
  de 1,1 m (cobertura!).
- **Porta:** arco ogival de 2,4 m no térreo, SEM porta física — cortina de
  luz âmbar fraca que os jogadores atravessam.
- **Janelas:** seteiras verticais de 25 cm (uma por face por andar),
  acesas em âmbar.
- **Vida e dano:** uma das oito faces tem **rachadura estrutural** do térreo à
  primeira cornija, com pedras deslocadas; trepadeira verde-escura sobe 6 m
  pela face norte; três pedras da mureta da sacada caíram e jazem no pé da
  torre; ninhos de andorinha nos vãos da cornija superior; bandeirola
  azul-noite com o Selo bordado, rasgada, no mastro curto da sacada.
- **Iluminação própria:** o cristal do topo lança um **facho vertical fino**
  de luz âmbar para o céu — o farol que se vê da queda.

## 2. PROMPT MESTRE — apresentação (9:16 VERTICAL — a peça é alta)

> Stylized high-end 3D game art, hand-painted textures, soft painterly
> surfaces, strong contact shadows, slightly exaggerated proportions, fantasy
> game landmark concept, deep night-blue and gold palette (#0B1026, #F0C75E),
> no photorealism.
>
> An arcane watchtower 22 meters tall at amber dusk, octagonal, 7 meters wide
> at the base tapering to 4.5 at the top, three floors marked by protruding
> stone cornices belted with riveted bronze bands showing green patina.
> Blue-grey stone masonry (#8E97AD) in irregular courses, stones 30 to 80 cm,
> deep joints. The crown: four inward-leaning stone prongs, and floating
> untouched between them a slowly spinning 1.6-meter amber octahedral crystal
> (#F0C75E), lit core, chipped edges, two thin rings of glowing runes
> orbiting it on different axes, casting a thin vertical beacon of amber
> light into the sky. An external spiral stone staircase 80 cm wide hugs the
> tower, rope handrail on bronze rings, steps worn concave at the center,
> reaching an octagonal observation balcony with a 1.1-meter parapet on the
> second floor — three parapet stones fallen at the tower's foot. Ground
> floor ogival doorway 2.4 meters, no physical door, a faint amber light
> curtain instead. Vertical arrow-slit windows glowing amber, one per face
> per floor. One face carries a structural crack from ground to first
> cornice with shifted stones; dark-green ivy climbs 6 meters up the north
> face; swallow nests under the top cornice; a torn night-blue pennant
> embroidered with a golden pentagon seal on a short mast. Single landmark,
> no people, silhouette readable from very far.

## 3. Vistas para o Meshy (separadas, fundo neutro, 9:16)

Prefixe com a âncora + *"single tower centered, plain dark grey background,
soft even lighting, entire tower in frame, no ground scenery, no people"*.

- **FRENTE:** *front view: doorway with light curtain, staircase wrapping
  visible at the sides, crystal crown on top.*
- **LADO:** *side view: balcony profile, staircase spiral, crack face.*
- **COSTAS:** *back view: ivy face, arrow slits, bronze belts.*
- **TRÊS-QUARTOS (de baixo):** *three-quarter view from slightly below —
  the drop-in view players get: crown crystal dominant, beacon beam.*

## 4. Negative prompt

> photorealistic, photo, people, wizard standing, modern lighthouse, brick,
> concrete, text, watermark, real alphabet, perfect clean stone, symmetrical
> undamaged, sci-fi antenna, power lines, blurry, low quality

## 5. Critérios de aprovação

1. Vista de BAIXO (queda): a coroa + cristal + facho leem em 1 segundo?
2. A escada externa dá cobertura real (largura, corrimão, sacada com mureta)?
3. O cristal NÃO toca os dentes de pedra (flutua)?
4. O dano existe (rachadura, pedras caídas, trepadeira) sem parecer abandono
   total — a torre está VIVA (janelas acesas, cortina de luz)?
5. Silhueta única contra as outras peças do mapa (não confunde com a torre do
   castelo)?
