# ARKANA — Documento Mestre de Construção da Ilha no Blender

**Versão:** 1.0 — especificação consolidada de produção  
**Data:** 24 de setembro de 2026  
**Destinatário:** Claude Code conectado ao Blender por MCP  
**Entrega pretendida:** uma ilha 3D completa, contínua, editável e visualizável no Blender, antes de qualquer integração com uma engine.  
**Modalidade prevista:** battle royale entre magos, com 40–60 jogadores simultâneos.  
**Idioma de comunicação com o usuário:** português do Brasil.

> Este documento é a única especificação textual necessária para construir a primeira versão completa de ARKANA. Ele consolida e substitui operacionalmente a Bíblia de Continuidade e o Sistema de Concept Art anteriores. Não é necessário solicitar esses dois documentos. As imagens, quando fornecidas, são referências visuais complementares; nenhuma informação numérica obrigatória depende delas.

---

## 1. Instrução principal ao executor

Construa no Blender um único território contínuo, grande e explorável, contendo todas as 12 regiões descritas aqui. Desenvolva primeiro a ilha inteira em blockout, verifique medidas e conexões, e depois avance até uma versão visual completa com arquitetura, vegetação, materiais, iluminação e câmeras.

O objetivo não é produzir somente um render de uma ilha, uma maquete pequena ou uma região isolada. O resultado precisa existir como geometria tridimensional inspecionável, incluindo partes posteriores, interiores especificados, rotas inferiores e subterrâneo. Todas as regiões devem coexistir no mesmo sistema de coordenadas.

Trabalhe em etapas curtas e verificáveis, salvando e mostrando a evolução no Blender. Continue autonomamente entre etapas que passaram na verificação; não solicite confirmação a cada árvore, coleção ou material. Se houver limitação técnica real, preserve o trabalho, explique a limitação concreta e prossiga com o que for possível sem alterar a intenção do projeto.

Não declare a ilha pronta quando houver apenas blocos, uma fachada voltada à câmera ou imagens aplicadas em planos. Não declare equilíbrio multiplayer, desempenho em jogo, colisão da engine ou duração de partida validados pelo simples fato de o cenário abrir no Blender.

### 1.1 Prioridade das decisões

1. Instruções posteriores e explícitas do usuário.
2. Medidas, coordenadas, conexões, hipóteses de gameplay e critérios deste documento.
3. Direção de arte e fichas regionais deste documento.
4. Concept arts fornecidas: aparência, silhuetas, materiais e atmosfera.
5. Decisões locais do executor, registradas no projeto.

As imagens têm perspectiva, compressão de distâncias e possíveis inconsistências entre vistas. Não extrair delas dimensões exatas, número obrigatório de degraus ou posição métrica de estruturas ocultas. Em caso de conflito, manter a geografia deste documento e adaptar a composição visual.

### 1.2 O que está definido e o que ainda é hipótese

**Requisitos de construção desta versão:** ilha de grande escala; 12 regiões; mundo contínuo; eixos e coordenadas definidos; circulação por solo sem exigir magia; múltiplos acessos; elaboração completa no Blender; salvamento e apresentação progressivos.

**Valores iniciais de projeto, ainda não comprovados em gameplay:** área acessível, velocidades, alcances, espaçamento inicial, quantidade de pontos de interesse secundários, duração de partida e fechamento da arena. Use os valores abaixo para construir e medir. Registre os resultados reais. Só altere o objetivo de escala ou as relações geográficas por decisão explícita do usuário; corrija detalhes locais de terreno para atender à especificação.

**Não definidos pelo usuário:** engine, plataforma final, hardware de referência, sistema de combate, modo em equipes, entrada por queda livre, voo, destruição de cenário, loot funcional, rede e regras definitivas da zona. Não inventar integrações prontas para esses sistemas.

---

## 2. Escala da ilha e espaço para a partida

### 2.1 Dimensões canônicas

| Parâmetro | Valor de projeto |
|---|---:|
| Extensão máxima oeste–leste | 4.800 m |
| Extensão máxima sul–norte | 4.400 m |
| Limites de construção da ilha | X = −2.400 a +2.400; Y = −2.200 a +2.200 m |
| Envelope retangular, não área jogável | 21,12 km² |
| Elipse de referência antes de recortes costeiros | aproximadamente 16,59 km² |
| Meta de terra emersa projetada em XY | 13–15 km² |
| Meta de superfície acessível projetada em XY | 10–12 km² |
| Água do Lago Central | aproximadamente 0,50 km² |
| Oceano | visual; não conta como área útil |
| Altitude do oceano | Z = 0 m |
| Nível estável do Lago Central | Z = 105 m |
| Cumes naturais | até Z = 620 m |
| Altura máxima total, incluindo construções/copas | até Z = 720 m |
| Capacidade usada no teste geométrico | 60 posições iniciais; também verificar 40 |

Use uma costa irregular, inspirada numa elipse com semieixos de 2.400 e 2.200 m, com enseadas, praias, falésias e algumas saliências. Preserve o alcance aproximado dos quatro extremos. A elipse é um envelope inicial, não uma parede invisível e não uma ordem para fazer uma ilha oval perfeita.

**Não confundir área do retângulo, terra emersa e espaço acessível.** A meta de 10–12 km² exclui mar, água profunda, paredes íngremes, volumes sólidos, coberturas inacessíveis e regiões sem ligação a caminhos. Não somar a área real de faces inclinadas para inflar o resultado. Reportar separadamente a área adicional de interiores, telhados acessíveis e subterrâneo; nunca contá-la duas vezes na métrica de superfície.

### 2.2 Por que esta escala foi escolhida

É uma proposta deliberadamente espaçosa para 40–60 magos, com distância entre núcleos, rotas de aproximação e áreas secundárias. Com 10–12 km² acessíveis, a divisão simples por 60 equivale a aproximadamente 167–200 mil m² por jogador; isso é apenas uma relação de área, não um território reservado nem garantia de separação real.

O mapa deve oferecer preparação, exploração e possibilidade de evitar um confronto imediato. Não preencher toda essa área com campo vazio. Distribuir terrenos intermediários, abrigo, passagens e pequenos locais exploráveis para sustentar o deslocamento.

### 2.3 Medição obrigatória da área

Após o blockout, amostrar a superfície em grade XY de 10 m e refinar bordas e passagens com amostras de 2 m. Classificar terra, água, declive, obstáculo, pé-direito e acesso. A área aproximada é a soma das células elegíveis; reportar a resolução e a incerteza de borda, sem apresentar o valor como levantamento exato.

A maior componente conectada da superfície acessível deve conter pelo menos 95% dessa área. As demais porções precisam ser ilhotas acessíveis por conexão prevista ou falhas a corrigir. O chão de caverna não pode ser confundido com superfície só porque um raio vertical o encontrou através de um buraco.

Se a meta de área não for alcançada, ampliar vales e planaltos, suavizar encostas apropriadas e reduzir obstáculos excessivos. Não aumentar ficticiamente a medida incluindo mar ou rocha inacessível. Não reduzir a ilha para melhorar um enquadramento ou desempenho de viewport.

---

## 3. Hipóteses de movimentação e combate entre magos

Estes valores são instrumentos de dimensionamento desta versão. Não implementar um sistema de combate funcional no Blender.

| Referência de projeto | Valor inicial |
|---|---:|
| Altura do personagem | 1,80 m |
| Envelope de passagem do corpo | cápsula de referência com raio de 0,45 m e altura total de 1,80 m |
| Altura dos olhos para inspeção | 1,65 m acima do piso |
| Deslocamento contínuo de referência | 6 m/s |
| Corrida de referência | 9 m/s |
| Salto vertical de referência | 1,20 m; não obrigatório em rotas principais |
| Avanço mágico hipotético | 12 m a cada 10 s; opcional |
| Alcance comum de projéteis usado no layout | 30–80 m |
| Alcance longo usado no layout | 120–180 m |
| Raio de área de efeito usado no layout | 4–8 m |
| Voo livre e teleporte ilimitado | não presumidos |

Uma rota de 600 m exige cerca de 100 s a 6 m/s ou 67 s a 9 m/s, sem contar curvas, declives, combate e coleta. Uma travessia idealizada de 4.800 m levaria cerca de 8,9 minutos a 9 m/s; a travessia real por caminhos será diferente. Não usar distância em linha reta como se fosse distância caminhável.

O avanço mágico de referência pode elevar a velocidade média em um percurso compatível; testar também o limite simplificado de 10,2 m/s, sem assumir que atravessa paredes ou precipícios. Caso o jogo ganhe voo prolongado, teleporte amplo ou ataques de alcance muito maior, reavaliar separações, coberturas e duração de rotação antes da integração.

### 3.1 Dimensões de circulação

| Elemento | Dimensão mínima ou faixa desejada |
|---|---|
| Via regional principal | 12–20 m de largura útil |
| Trilha secundária | 5–8 m |
| Passagem estreita opcional | mínimo de 2,5 m; não sustenta sozinha a conexão regional |
| Porta principal de construção explorável | mínimo de 2,4 m de largura × 3,0 m de altura |
| Porta secundária | mínimo de 1,4 m × 2,4 m |
| Escada principal | largura mínima de 3 m; espelho até 0,18 m; piso a partir de 0,30 m |
| Rampa principal | inclinação desejada até 12° |
| Encosta de caminhada comum | até 25°; acima disso criar rota própria ou tratar como não transitável |
| Pé-direito comum de interior | mínimo de 3 m |
| Túnel regional principal | mínimo de 8 m de largura × 6 m de altura |
| Galeria alternativa | mínimo de 4 m × 3,5 m |
| Arena de confronto média | aproximadamente 50–100 m de diâmetro útil |
| Pátio principal amplo | aproximadamente 100–180 m |
| Cobertura baixa | 0,8–1,2 m de altura |
| Cobertura completa | 2,2–4 m de altura; largura útil a partir de 3 m |

As faixas são referências por função. Casas, portas e degraus mantêm escala humana mesmo que o território seja grande. Não ampliar uniformemente uma maquete com casas prontas para alcançar quilômetros.

### 3.2 Regras espaciais de combate

- Misturar espaços abertos, interiores, rotas laterais e níveis verticais em cada região de superfície.
- Em áreas destinadas a confronto, disponibilizar cobertura sólida a cada 15–35 m de deslocamento, com distribuição irregular. Vegetação visualmente densa não substitui pedra, terra ou construção como proteção sólida.
- Nas transições tranquilas, admitir intervalos maiores, de 40–80 m, desde que haja relevo, abrigo lateral e leitura do próximo destino.
- Linhas diretas de tiro comuns devem ser interrompidas aproximadamente a cada 60–120 m. Vistas de 180–300 m são excepcionais e precisam de contrajogo; visibilidade de landmark a quilômetros não implica linha de tiro desobstruída.
- Evitar que uma única torre observe todas as saídas de uma região ou todos os pontos iniciais.
- Oferecer saídas em pelo menos duas direções nos pátios principais. Uma entrada única pode existir em uma sala opcional, nunca como único caminho de uma região inteira.
- Distribuir coberturas em grupos espaçados; não formar um único abrigo que torne uma magia de área capaz de atingir todos os jogadores próximos.
- Poços, precipícios e água profunda devem ser legíveis. Não exigir saltos cegos nem escalada não definida.

---

## 4. Sistema de coordenadas e configuração espacial

Usar sistema métrico, escala de unidades 1,0 e convenção de projeto **1 unidade Blender = 1 metro**. Verificar a dimensão real de um objeto de 2 m; mudar apenas a unidade exibida não corrige uma malha modelada fora de escala.

| Eixo | Sentido |
|---|---|
| +X | leste |
| −X | oeste |
| +Y | norte |
| −Y | sul |
| +Z | cima |
| Origem (0, 0, 0) | referência horizontal próxima ao centro da ilha, na altitude do mar |

O centro do lago está em (0, 150, 105), não no Z zero. Nenhuma região deve redefinir silenciosamente a origem global. Peças modulares podem ter origem local, mas sua instalação deve preservar a transformação global.

### 4.1 Implantação regional canônica

As dimensões abaixo são envelopes orientativos de trabalho em XY, não terrenos retangulares separados. Eles podem se sobrepor nas transições e não devem ser somados como área total. Os centros são âncoras de implantação; os detalhes internos podem ser deslocados para resolver acessibilidade.

| ID | Região | Centro XY em metros | Envelope aproximado X × Y | Faixa de piso ou terreno em Z |
|---|---|---|---|---|
| R01 | Lago Central e margens | (0, 150) | 1.300 × 1.100 m | água 105; margens 108–145 |
| R02 | Floresta Gigante | (−150, 1.150) | 1.600 × 1.200 m | 180–340 |
| R03 | Templo Antigo | (750, −250) | 650 × 650 m | 120–185 |
| R04 | Vila Abandonada | (−1.200, 650) | 850 × 850 m | 145–250 |
| R05 | Ponte Principal e Desfiladeiro | (200, −1.050) | 700 × 750 m | tabuleiro 125; rio inferior 35–70 |
| R06 | Acampamento | (−900, −350) | 450 × 450 m | 115–145 |
| R07 | Zona Industrial | (1.550, 100) | 850 × 850 m | 150–225 |
| R08 | Base Militar Abandonada | (1.250, −1.350) | 800 × 700 m | 130–215 |
| R09 | Torre de Observação | (−550, 1.850) | 400 × 400 m | platô 430; observação 478; topo 488 |
| R10 | Caverna Profunda | (1.000, 1.100) | 600 × 650 m | entrada 220; pisos internos 125–210 |
| R11 | Rede de Túneis | nós da seção 7 | corredores, não um retângulo único | pisos 40–160 conforme trecho |
| R12 | Caverna Subterrânea | (−450, −1.050) | 1.000 × 800 m | pisos 5–35; teto principal até 65 |

Templo a leste/sudeste do lago. Vila a oeste/noroeste. Acampamento a sudoeste entre vila e lago. Indústria a leste. Base a sudeste. Torre ao norte, ligeiramente a oeste da árvore principal. Caverna Profunda a nordeste, ligada à floresta. Esta disposição resolve a divergência do antigo esquema que colocava o templo no lado oeste.

As extremidades de envelopes retangulares podem cair sobre costa ou regiões vizinhas: recortar a ocupação pela terra real, sem estender prédios sobre o mar para completar um retângulo.

### 4.2 Elementos fixos de terreno

- Lago: contorno irregular baseado em 900 × 700 m, centro (0, 150), água em 105. Profundidade predominante de 8–25 m; margens rasas localizadas de 0–1 m.
- Árvore principal: centro aproximado (−100, 1.300), base em Z 280; altura de 180–220 m; tronco principal de 35–50 m; raízes estruturais estendendo-se 60–120 m. Acesso oco principal com cerca de 12 m de largura e 18 m de altura.
- Cristas montanhosas: sobretudo no norte e nas bordas leste/oeste, com passagens entre vales. Não cercar toda a costa com uma parede intransponível.
- Garganta principal: eixo geral X próximo de 200, descendo do sul do lago para Y negativo. Desviar localmente o leito sem mover a ponte para outra região.
- Planícies e colinas periféricas: completar o território entre os envelopes; nenhuma faixa extensa pode ser deixada como plano vazio porque não recebeu um nome de região.
- Litoral: praias e enseadas em trechos oeste, sudoeste e leste; falésias e rochedos mais fortes ao norte. Oferecer retornos ao interior em cada trecho acessível.

### 4.3 Tolerâncias

Âncoras regionais podem ter ajustes locais de até 50 m em XY para resolver apoio e curvas de terreno, mantendo os envelopes e vizinhanças. Altitudes de entrada explicitamente tabeladas e nível do lago não mudam sem atualizar todos os trechos conectados e registrar a alteração. Não usar a tolerância repetidamente para deslocar uma região inteira centenas de metros.

---

## 5. Rede de circulação na superfície

Todas as conexões desta seção são físicas e precisam existir como piso transitável. Desenhar uma linha de guia não conclui a conexão. Os comprimentos são faixas de referência para medir após a implantação, não ordens para esticar artificialmente o caminho.

| Rota | Ligações e sentido geral | Comprimento de referência | Alternativa exigida |
|---|---|---:|---|
| S01 | lago norte ↔ floresta | 700–1.200 m | trilha alta paralela ou rota entre raízes |
| S02 | lago oeste ↔ vila | 1.000–1.600 m | acampamento + via ocidental |
| S03 | lago sudoeste ↔ acampamento | 650–1.100 m | trilha de ruínas pela margem oeste |
| S04 | lago leste ↔ templo | 350–700 m | caminho por terra acima da margem |
| S05 | lago leste ↔ indústria | 1.100–1.700 m | templo + estrada de oficinas |
| S06 | lago sul ↔ ponte | 900–1.500 m | aproximação ocidental ao caminho inferior |
| S07 | vila ↔ floresta | 1.000–1.700 m | trilha por ruínas na encosta |
| S08 | vila ↔ acampamento | 800–1.400 m | caminho exterior entre casas dispersas |
| S09 | floresta ↔ torre | 800–1.400 m | segunda subida por lombo da montanha |
| S10 | floresta ↔ caverna profunda | 900–1.500 m | galeria de raízes e trilha rochosa |
| S11 | templo ↔ indústria | 750–1.300 m | trilha florestal periférica |
| S12 | templo ↔ ponte | 800–1.300 m | varanda rochosa ligada à travessia baixa |
| S13 | indústria ↔ base | 1.400–2.000 m | via externa leste |
| S14 | ponte sul ↔ base | 950–1.500 m | via lateral sudeste |
| S15 | acampamento ↔ ponte oeste | 1.000–1.700 m | túneis com acessos distintos |
| S16 | anel exterior oeste–norte–leste–sul | trechos conectados, medidos após relevo | desvios locais sem depender da ponte |

O anel exterior acompanha vales e platôs entre 1,4 e 2,0 km do centro, com desvios onde a costa exigir. Não fazer uma estrada circular perfeita, uma pista de corrida ou uma única faixa de cobertura contínua. Ele distribui deslocamento por áreas secundárias e permite evitar o lago.

### 5.1 Ponte e travessias do desfiladeiro

- Ponte principal orientada aproximadamente norte–sul; centro (200, −1.050, 125).
- Encontro norte em (200, −900, 125); encontro sul em (200, −1.200, 125).
- Comprimento inicial 300 m; largura útil 16 m; parapeitos de 1,2 m; apoios em rocha sólida. Comprimento pode ajustar-se à borda real, preservando ambos os encontros e acessos.
- Não fechar a ponte com objetos decorativos. Barricadas devem deixar pelo menos duas faixas úteis, somando 8 m ou mais.
- Criar uma travessia inferior por varandas na rocha e ponte secundária, aproximadamente 120–220 m a oeste da principal, piso próximo de Z 80. Essa travessia atravessa o mesmo desfiladeiro e reconecta os dois lados; não é apenas uma escada para um beco.
- As descidas até Z 80 devem usar rampas, escadarias e curvas com espaço suficiente. O rio fica abaixo desse caminho.
- Criar terceira opção via túneis, com acessos fora da visão direta dos encontros da ponte.
- Caminhos inferiores são arquitetura real. Não representar a alternativa apenas como traço pintado na parede.

### 5.2 Transições sem fronteira artificial

Reservar transições de 150–300 m quando houver espaço: floresta densa → árvores espaçadas → ruínas isoladas → primeiras casas → vila. Para indústria: ruínas → tubo e oficina isolados → depósitos → galpões. Para base: trilha → caixas e barreiras → pequeno posto → perímetro principal.

As zonas de transição podem sobrepor envelopes. Compartilham terreno e vegetação; não colocar bordas retangulares, escarpas artificiais ou paredes invisíveis entre coleções.

---

## 6. Água, geologia e coerência vertical

### 6.1 Sistema hídrico principal

| Marco hídrico | Posição aproximada XY | Cota da água |
|---|---|---:|
| Cabeceiras norte | (−250, 1.800) e (850, 1.600) | 380–430 m |
| Riachos da floresta | próximos de (−150, 1.100) | 230–280 m |
| Quedas de entrada no lago | bordas norte/nordeste | descem até 105 m |
| Lago Central | (0, 150) | 105 m |
| Vertedouro natural sul | (100, −220) | 105 m |
| Rio antes da garganta | (160, −650) | aproximadamente 95 m |
| Rio sob a ponte | (200, −1.050) | aproximadamente 50 m |
| Rio depois da garganta | (220, −1.450) | aproximadamente 30 m |
| Estuário sul | (260, −2.050) | 0–3 m |

Esses pontos descrevem um fluxo descendente. Construir leito contínuo entre eles. Cachoeiras resolvem diferenças de altitude localizadas; água parada permanece horizontal. Não fazer o rio subir, atravessar paredes ou terminar no meio de um chão seco.

Parte da água pode infiltrar por fissuras fora do lago e alimentar canais subterrâneos. O sistema profundo desemboca por uma nascente baixa/saída costeira ao sul, aproximadamente (−450, −1.950), antes de alcançar o mar. Não abrir um furo abaixo do lago que devesse drenar todo o reservatório e deixá-lo inexplicavelmente cheio.

Água de visualização pode ser malha simples com material; simulação de fluidos não é requisito. Cachoeiras devem ter origem, volume visual, zona de impacto e escoamento. Evitar fumaça/espuma volumosa que esconda o relevo.

### 6.2 Geologia e subterrâneo

Utilizar rochas cinza-escuras fraturadas e estratificadas, solo terroso, musgo e raízes. Cristais azuis/violetas aparecem como inclusões localizadas; não transformar todas as montanhas em cristal emissivo.

Cavernas precisam de piso, teto, paredes, espessura aparente e aberturas reais. Uma superfície de altura só resolve o terreno externo; as cavidades exigem malhas próprias ou recortes locais coerentes. Não usar um único terreno sólido preenchendo os interiores.

Manter pelo menos 12 m de rocha entre o teto das galerias comuns e a superfície, salvo entradas e fissuras previstas. Sob o lago, preferir evitar o volume de água; quando inevitável, manter pelo menos 25 m de rocha abaixo do leito e documentar o trecho. A caverna profunda fica sob os maciços do nordeste, e a caverna subterrânea sob o platô sudoeste, não aberta ao ar como no corte ilustrativo do mapa original.

Para inspecionar o subterrâneo, usar coleções ocultáveis ou vista técnica seccionada. A apresentação final exterior deve mostrar a superfície íntegra e somente entradas justificadas.

---

## 7. Entradas e rede subterrânea

### 7.1 Portais e nós fixos

As cotas são do piso no limiar ou centro do nó. Um portal alto não é conectado a uma galeria baixa por uma aresta invisível: modelar toda a descida transitável.

| ID | Função | Coordenadas XYZ, em metros |
|---|---|---|
| U01 | entrada exterior da Caverna Profunda | (850, 950, 220) |
| U02 | grande galeria profunda | (1.000, 1.100, 160) |
| U03 | saída da galeria para túneis leste | (900, 700, 125) |
| U04 | núcleo subterrâneo leste | (1.150, 250, 95) |
| U05 | entrada da cripta do templo | (700, −300, 125) |
| U06 | acesso do porão da vila | (−1.050, 450, 160) |
| U07 | núcleo subterrâneo oeste | (−900, 100, 110) |
| U08 | saída oculta próxima à margem sudoeste do lago | (−500, −250, 115) |
| U09 | galeria sudoeste de distribuição | (−700, −500, 70) |
| U10 | acesso do caminho inferior da ponte | (0, −1.050, 80) |
| U11 | entrada da caverna subterrânea pelo norte | (−450, −700, 35) |
| U12 | salão central subterrâneo | (−450, −1.050, 20) |
| U13 | acesso na base militar | (1.050, −1.250, 145) |
| U14 | galeria sul/leste | (550, −1.350, 40) |
| U15 | saída costeira sudoeste | (−450, −1.950, 10) |

### 7.2 Conexões obrigatórias

Construir U01–U02–U03–U04; U04–U05; U06–U07–U09; U07–U08; U09–U11–U12; U12–U14–U13; U14–U10; U12–U15. Construir também U04–U14 pela borda leste, contornando por baixo o templo, e U09–U10 pela aproximação oeste da ponte.

Esses trechos formam um ciclo U09–U10–U14–U12–U11–U09. Criar bifurcações locais nas demais galerias e pelo menos dois percursos distintos dentro dos salões principais. As passagens não precisam ser retas: devem seguir a rocha, preservar espessura e evitar o volume do lago.

Para toda aresta, calcular comprimento real, diferença de cota, declive e folga. Não concluir conectividade apenas porque dois nomes estão em uma lista. As rotas longas podem ter escadas, patamares e rampas; poços verticais precisam de alternativa por solo. Elevadores podem ser elementos visuais, mas não podem ser o único meio de percorrer uma ligação enquanto não houver gameplay funcional.

O mundo inferior também permite fuga e rotação. Não concentrar todas as entradas em um único ponto, não ocultar todas com vegetação e não transformar as galerias em um labirinto sem referências.

---

## 8. Direção de arte global

Fantasia sombria de sobrevivência, aventura e conflito, com acabamento estilizado de videogame de alta qualidade. Natureza exuberante reconquista ruínas e instalações abandonadas. A sensação de perigo vem da arquitetura, escala, silêncio e vestígios de guerra, sem exigir que a imagem fique escura a ponto de esconder o cenário.

### 8.1 Materiais e linguagem visual

| Família | Direção |
|---|---|
| Rocha natural | cinza grafite, estratos/fraturas legíveis, musgo nas áreas úmidas |
| Arquitetura antiga | pedra cinza-bege, blocos grandes, pilares quadrados, colunas e arcos; desgaste consistente |
| Construções da vila | bases de pedra, vigas de madeira envelhecida, telhas quebradas, sinais de incêndio |
| Indústria | metal escuro, ferrugem laranja/marrom, concreto/pedra, tubulações e cabos |
| Base militar | concreto gasto, aço oxidado, tons cinza e verde dessaturado; abandono |
| Vegetação | folhagem verde de árvores de copa larga, raízes estruturais e sub-bosque compatível |
| Magia | azul/ciano/violeta em cristais; âmbar discreto em cavidades e vestígios de energia |
| Água | turquesa nas margens rasas, azul mais profundo no centro, sem emissão generalizada |

O templo estabelece a linguagem da civilização antiga. Reutilizar seus blocos, colunas, altares e motivos geométricos nas ruínas da vila, floresta, túneis e subterrâneo. Usar motivos abstratos sem textos legíveis; não inventar outra civilização arquitetônica por região.

Industrial e militar são camadas históricas posteriores dentro do mesmo mundo. Elementos tecnológicos podem estar presentes conforme as fichas, mas não adicionar cidade futurista, naves, arranha-céus, infraestrutura urbana moderna extensa ou armamentos funcionais como foco.

### 8.2 Iluminação

Exterior: tarde com luz solar quente vinda do quadrante oeste/sudoeste; sombras com preenchimento frio suficiente. Manter a direção solar global em todas as câmeras, mesmo que isso mude o lado iluminado na composição. Não girar o sol para favorecer cada região.

Interior: luz natural por entradas e rachaduras, pontos âmbar em locais coerentes, emissivos mágicos discretos. Caverna Profunda combina azul/violeta e calor pontual; Caverna Subterrânea pode ser mais escura e monumental, mas rotas, pisos e volumes permanecem legíveis.

Neblina só como acabamento leve e distante. Manter uma vista de verificação sem neblina, sem profundidade de campo e sem efeitos que ocultem defeitos. Não usar luz emissiva em massa para substituir materiais.

### 8.3 Referências visuais disponíveis

Há uma concept art geral da ilha e quatro concepts regionais: Lago Central, Floresta Gigante, Templo Antigo e Vila Abandonada. Se recebidas, abri-las antes de modelar detalhes. Não presumir acesso a anexos de outra conversa ou computador.

O mapa geral contém legendas, ícones, título e miniaturas; esses elementos não pertencem à geometria. O corte visível da caverna inferior é uma convenção ilustrativa. As distâncias aparentes são comprimidas. Preservar as silhuetas, a árvore oca monumental, a torre de pedra, o templo em terraços, a vila em encostas e o contraste indústria/natureza.

Se nenhuma imagem estiver disponível, construir pela descrição completa deste documento e registrar que a fidelidade visual às imagens ainda não foi conferida. Não interromper a produção de todas as regiões por falta de concepts individuais.

---

## 9. Fichas completas das 12 regiões

Os números de construções abaixo são mínimos/faixas de planejamento para evitar regiões vazias. Não multiplicar objetos sem função somente para atingir contagem. Cada região deve manter acessos, alternativas, escala humana e conexão com o relevo global.

### R01 — Lago Central

**Papel:** orientação geográfica e distribuição de rotas. Não transformar o centro inteiro em água que todos precisam atravessar nadando.

**Construir:** lago irregular de 900 × 700 m; 3–5 ilhotas de 25–80 m; um pequeno santuário arruinado na ilha principal; 4–6 acessos de margem distribuídos; 2–4 docas simples; ruínas, pedras, áreas rasas e pequenas construções; quedas d'água do norte; saída fluvial ao sul.

**Circulação:** percurso costeiro contínuo por terra, com mudanças de nível e desvios; conexões com floresta, vila, acampamento, templo, indústria e ponte. Pelo menos duas ilhotas ligadas por pontes ou passagens rasas claramente verificáveis. Outras podem ser apenas decorativas e devem ser classificadas assim. Nenhuma rotação regional obrigatória depende de natação.

**Espaço:** alternar enseadas abertas, encostas, rochas e árvores. Manter bolsões de 60–120 m para confronto e aproximações protegidas; não criar uma arena plana exposta a todas as margens.

**Continuidade visual:** árvore gigante ao norte; torre ao norte/noroeste; vila no oeste; templo no leste/sudeste; indústria além dele a leste. A visibilidade exata depende do relevo; não remover montanhas só para todos os marcos caberem em uma câmera.

### R02 — Floresta Gigante

**Papel:** exploração vertical, cobertura natural e ligação norte.

**Construir:** árvore principal nas dimensões da seção 4; 6–10 árvores gigantes secundárias de 50–100 m de altura; árvores comuns de 12–35 m distribuídas no restante da região. Cavidade principal com piso, paredes e plataformas; pelo menos duas árvores secundárias ocas exploráveis. Raízes arqueadas, pequenas cavernas, troncos caídos, riachos, ruínas e restos de acampamento.

**Detalhes:** folhas, arbustos, samambaias, flores, cogumelos, cipós e pedras cobertas de musgo. Emissão discreta em poucas raízes. Não cobrir todo o solo com objetos que impedem caminhada.

**Circulação:** quatro conexões externas — lago, vila, torre, caverna profunda. Pelo menos três clareiras de 60–120 m conectadas por mais de uma trilha; caminhos sob raízes e uma rede limitada de plataformas elevadas com apoios e escadas. Cavidade da árvore principal com segundo acesso lateral.

**Escala:** plataformas não devem ocupar toda a copa como cidade suspensa. Tronco e raízes são o landmark; o piso precisa ter área útil suficiente para deslocamento e confronto.

### R03 — Templo Antigo

**Papel:** principal núcleo arqueológico/místico; define os módulos antigos do mundo.

**Construir:** complexo principal de aproximadamente 220 × 180 m dentro da região; torre/santuário quadrado central com cerca de 40–50 m acima do terraço; alas com colunas; grande escadaria; 2–3 pátios; altar; estátuas quebradas; fontes, jardins e corredores. Reservar um pátio de aproximadamente 100 × 80 m, articulado com coberturas sem virar campo totalmente aberto.

**Interiores:** sala central, galerias laterais e cripta ligada a U05. Partes do teto podem estar destruídas; outras permanecem cobertas. Modelar paredes posteriores, espessura de lajes e todas as escadas que ligam os níveis.

**Materiais:** pedra cinza-bege, blocos e pilares reconhecíveis, musgo e raízes através de fissuras; símbolos geométricos sem texto. Cristais e altar com magia discreta.

**Circulação:** margem do lago → entrada oeste; acesso sul pela direção da ponte; saída leste para indústria; rota florestal periférica; acesso subterrâneo. A escadaria central não é a única passagem entre terraços.

### R04 — Vila Abandonada

**Papel:** confrontos urbanos, investigação visual e transição floresta/lago.

**Construir:** 24–36 casas de 10–18 m por 8–14 m, de um a três pavimentos; 4–6 oficinas; praça com poço; mercado abandonado; pequena torre local parcialmente destruída; carroças, barris, ferramentas, móveis, cercas e restos de comércio. Organizar em três faixas de terraço conectadas, sem empilhar construções impossíveis sobre falésias.

**Interiores:** pelo menos 12 casas exploráveis, incluindo 4 com dois níveis ligados por escada. Nas restantes, indicar claramente se são ruínas abertas ou volumes fechados; nenhuma porta com aparência de passagem deve terminar em parede oculta.

**Detalhes:** telhas ausentes, vigas queimadas, madeira apodrecida, raízes, objetos abandonados e sinais de evacuação. Bandeiras rasgadas sem insígnias textuais. Não parecer uma vila habitada em festa.

**Circulação:** ruas principais de 8–12 m e becos secundários de 3–5 m; praças de 35–70 m; rotas atrás das casas; escadarias para margem. Saídas para floresta, acampamento, lago e U06. A pequena torre arruinada da vila não deve competir com a Torre de Observação.

### R05 — Ponte Principal e Desfiladeiro

**Papel:** gargalo estratégico com alternativas reais.

**Construir:** ponte e cotas da seção 5.1; quedas d'água, rio inferior, paredes de rocha, floresta nas bordas, ruínas defensivas, parapeitos, plataformas e entradas de caverna. Linguagem de alvenaria antiga com reparos metálicos/madeira quando coerentes.

**Circulação:** tabuleiro principal, travessia inferior, ligação subterrânea U10 e trilhas laterais. Abrigos antes dos encontros para observar a passagem sem exposição obrigatória. Pontos elevados devem poder ser flanqueados.

**Verificação especial:** ocultar a ponte principal e confirmar uma rota contínua entre as duas margens por outra travessia. Restaurar visibilidade depois; não apagar a ponte para executar o teste.

### R06 — Acampamento

**Papel:** clareira intermediária entre vila, floresta e lago; não um bioma separado.

**Construir:** núcleo de aproximadamente 120 × 100 m com 8–12 barracas, 2 abrigos maiores, mesas, caixas, suprimentos, ferramentas, cordas, madeira e cercas improvisadas. Três áreas de fogueira, com no máximo uma ou duas visualmente acesas. Equipamento de sobrevivência e vestígios de combate compatíveis com magos.

**Circulação:** pelo menos quatro saídas distribuídas: vila, lago, floresta e sudoeste/ponte. Ruínas e árvores grandes nas bordas, clareira central com cobertura moderada. Não criar fortaleza que concentre todos os itens e jogadores iniciais.

**Continuidade:** as barracas e caixas tornam-se módulos recorrentes em pequenos postos e pontos secundários.

### R07 — Zona Industrial Abandonada

**Papel:** combate vertical, interiores amplos e passagem leste.

**Construir:** 6–10 galpões de aproximadamente 30–60 × 20–35 m; 4 oficinas; 3–5 tanques; 2 guindastes; depósitos; geradores; tubulações; cabos; plataformas; veículos abandonados e torre industrial principal de 65–85 m. Pelo menos 4 galpões com interior completo e dois acessos.

**Verticalidade:** solo, mezaninos, telhados e passarelas; cada plataforma relevante tem acesso identificável. Tubulações podem servir como linguagem visual e cobertura, sem atravessar portas, pisos ou vias essenciais.

**Materiais:** ferrugem, metal oxidado, água acumulada e vegetação. Poucas máquinas com vestígios de energia mágica, sem sugerir fábrica em pleno funcionamento. Placas podem estar quebradas, sem textos legíveis.

**Conexões:** lago, templo, base militar e via exterior leste. Criar oficinas e componentes dispersos nas transições para evitar mudança brusca de tema.

### R08 — Base Militar Abandonada

**Papel:** conjunto de edifícios exploráveis e pátios do sul/sudeste.

**Construir:** centro de comando, 3 alojamentos, 2 hangares, enfermaria/depósito, oficina e posto de entrada; 3–4 torres de vigilância de 12–18 m; um heliponto de cerca de 28 m de diâmetro; cercas danificadas, barreiras, caixas, torres de luz e 6–10 veículos destruídos ou abandonados.

**Interiores:** comando, dois alojamentos, um hangar e depósito completos. Vestígios de conflito em paredes, crateras e carcaças, sem deformar todos os caminhos a ponto de impedir movimentação.

**Circulação:** acesso por ponte, indústria, estrada externa e U13. Pelo menos três brechas atravessáveis no perímetro. Distribuir coberturas nos pátios e rotas atrás dos edifícios. A entrada principal não deve controlar toda a instalação.

**Integração visual:** concreto e metal envelhecidos sobre terreno/ruínas da mesma ilha. O heliponto preserva o elemento das referências; não exige aeronave funcional.

### R09 — Torre de Observação

**Papel:** landmark elevado e posição estratégica, com contrajogo.

**Construir:** base de pedra de aproximadamente 16 × 16 m, platô em 430 m, estrutura de 58 m até o topo, plataforma principal a 478 m. Pedra, madeira, escadas, sala de observação, plataformas, instrumentos abandonados, bandeiras antigas e restos de fogueira.

**Circulação:** duas subidas exteriores diferentes até o platô; escada interna e acesso secundário à primeira plataforma. Patamares e abrigos interrompem exposição durante subida. A estrutura pode ter partes destruídas, mas o acesso ao nível de observação precisa estar completo.

**Vista:** lago, floresta, partes da vila e templo podem aparecer entre relevo e copas. Não garantir visão de todos os spawns. Topo e forma devem ser reconhecíveis a longa distância; a torre industrial tem silhueta distinta.

### R10 — Caverna Profunda

**Papel:** entrada monumental do nordeste para o mundo inferior.

**Construir:** entrada de aproximadamente 35 × 25 m atravessada por raízes; 3 salões de 60–120 m; rochas em níveis, quedas d'água, pequenos lagos, pontes naturais, cristais, ruínas e galerias secundárias. U01, U02 e U03 ancoram seu percurso.

**Circulação:** duas opções entre o primeiro e o último salão; plataformas elevadas conectadas por rampas/escadas e rotas baixas. Entrada visível da trilha da floresta. Não esconder toda a caverna em preto nem iluminar só um altar.

**Geometria:** raízes vêm da camada acima; teto e paredes possuem continuidade e espessura. Água tem origem por infiltração/canal alto e escoa para galerias inferiores.

### R11 — Rede de Túneis

**Papel:** ligações subterrâneas, flanqueamento e retorno ao exterior.

**Construir:** todos os trechos da seção 7; mistura de rocha natural, corredores antigos e galerias de mineração. Incluir 6–8 câmaras intermediárias, bifurcações, poços protegidos, pontes, trilhos parciais, carrinhos, ferramentas, suportes de madeira, lanternas, cabos e equipamentos quebrados.

**Circulação:** rotas principais generosas; trechos estreitos opcionais; patamares a cada mudança significativa de cota. Variar largura e altura para evitar quilômetros de corredor idêntico. Trilhos não substituem piso caminhável e não precisam percorrer toda a rede.

**Orientação:** cada núcleo tem silhueta e combinação de materiais/luz reconhecíveis. Luz azul/âmbar conduz visualmente a saídas, sem texto obrigatório. Evitar becos longos que só existem para aumentar extensão.

### R12 — Caverna Subterrânea

**Papel:** grande região profunda, distinta da entrada rochosa do nordeste.

**Construir:** salão principal de aproximadamente 320 × 220 m dentro do envelope de 1.000 × 800 m, mais 3 câmaras conectadas de 80–160 m; tetos até Z 65, respeitando o platô acima. Rios, lago subterrâneo raso nas bordas, cristais, raízes, vegetação fantástica localizada, ruínas enterradas, pontes de pedra e plataformas naturais.

**Alturas:** piso principal próximo de Z 20; bancos altos em Z 30–35; áreas baixas até Z 5, com água local abaixo dos percursos. Não criar rio subindo para sair no oceano. Árvores subterrâneas são menores, de 8–20 m, compatíveis com o teto.

**Circulação:** conexão norte U11, leste U14 e saída sul U15; pelo menos duas rotas entre cada par de setores principais. Não depender de uma única ponte sobre o lago interior.

**Atmosfera:** mundo escondido de escala monumental, azul profundo e violeta com pontos quentes. Ruínas compartilham a linguagem do templo. A vegetação fantástica é uma escolha artística; não precisa de simulação biológica.

---

## 10. Ocupação das áreas entre regiões

Uma ilha grande precisa de conteúdo distribuído. Além dos 12 núcleos, criar **24–36 pontos de interesse secundários** e **60–90 pontos de abrigo/exploração pequenos**, com distribuição irregular e densidade ajustada pela medição de rotas. Esses elementos não são novas regiões independentes.

| Tipo secundário | Quantidade inicial | Conteúdo |
|---|---:|---|
| Ruínas de passagem | 8–12 | altar, parede, colunas ou antiga guarita |
| Cabanas/oficinas dispersas | 6–8 | pequeno interior, caixa, ferramenta, cobertura |
| Bosques/afloramentos exploráveis | 6–10 | clareira, raiz, pedra, trilha alternativa |
| Postos abandonados | 4–6 | abrigo simples, sinal de antiga ocupação |

As faixas somam 24–36. Os 60–90 abrigos menores incluem rochas, troncos, pequenas ruínas e clareiras; podem usar kits repetidos, mas não a mesma composição visível repetida em grade.

Posicionar locais secundários em intervalos aproximados de 250–450 m ao longo de trajetos extensos, variando segundo relevo e visibilidade. Evitar mais de 300 m contínuos de chão plano sem decisão espacial, referência ou abrigo lateral. Esses valores não obrigam cada direção a ter um ponto de interesse na mesma distância.

Reservar marcadores de recursos/loot visual, sem implementar inventário. Distribuí-los entre regiões e periferia. Na proposta inicial, pelo menos metade dos pontos candidatos deve estar fora dos quatro grandes núcleos centrais. Não concentrar todos os recursos úteis no templo ou na árvore gigante.

---

## 11. Distribuição inicial de 40–60 jogadores

### 11.1 Cenário de referência desta versão

Usar como ensaio espacial **início solo distribuído por materialização no solo**, com 60 posições distintas. É uma hipótese para validar espaço; o sistema definitivo de entrada ainda não foi escolhido. Não modelar um único lobby de spawn dentro da ilha como se fosse a distribuição de partida.

Se o jogo adotar escolha livre de pouso, será possível vários jogadores escolherem o mesmo lugar; o tamanho do mapa não impede isso. Nesse caso, reaproveitar a auditoria de terreno, mas revisar as regras de distribuição. Este documento não promete impedir encontros rápidos por decisão voluntária dos jogadores.

### 11.2 Regras geométricas

- Gerar pelo menos 84 candidatos de início válidos na superfície, em setores variados.
- Selecionar um conjunto de 60 e um conjunto de 40 usando a mesma malha de candidatos; não é necessário que o conjunto de 40 seja subconjunto do de 60.
- Distância horizontal mínima entre posições selecionadas: **300 m**.
- Distância por caminho entre vizinhos: meta de pelo menos **400 m**, medida na rede caminhável. A distância vertical não pode mascarar proximidade horizontal.
- Não iniciar no subterrâneo, em água profunda, na ponte, em telhado inacessível, perto de precipício ou dentro de volume sólido.
- Inclinação local do piso de spawn até 10°; área livre de pelo menos 6 × 6 m; cápsula de referência com folga.
- Manter pelo menos 15 m de água profunda, queda abrupta ou obstáculo perigoso no plano de deslocamento imediato.
- Cada início deve alcançar uma rota regional e pelo menos duas direções locais de saída, sem magia obrigatória.
- Disponibilizar um abrigo sólido a 15–40 m e um ponto de exploração/recurso candidato a 40–120 m.
- Não permitir linha de visão direta entre posições iniciais a até 500 m de distância. Testar a partir dos olhos e de pequenos deslocamentos locais; ocultação só por folhas transparentes não satisfaz o teste.
- Procurar distribuir pelo menos 50% dos inícios selecionados fora dos envelopes de templo, vila, indústria e base, evitando concentração nos grandes núcleos.

Os 300 m não garantem tempo mínimo de primeiro contato: dois jogadores podem correr um em direção ao outro. Registrar o tempo aproximado de encontro por rota como `distância / (velocidade A + velocidade B)`, além do tempo individual de aproximação. Por exemplo, 400 m a 9 + 9 m/s equivalem a aproximadamente 22 s se ambos se buscarem, sem obstáculos ou outras ações. A proposta reduz proximidade inicial; não promete três minutos sem combate.

### 11.3 Procedimento de seleção e verificação

1. Amostrar terreno elegível fora das exclusões; projetar candidatos no piso e verificar cápsula/folgas.
2. Separar a ilha em setores de amostragem para evitar concentração; não desenhar fronteiras desses setores no cenário.
3. Aplicar seleção por maior distância ao conjunto já escolhido, com semente reprodutível 24092026 e correções por conectividade/visibilidade.
4. Repetir seleção com ordens iniciais determinísticas alternativas se uma tentativa não produzir 60 posições.
5. Medir matriz de distâncias XY, distâncias por caminho e linhas de visão dos pares relevantes.
6. Corrigir terreno, obstáculos e candidatos problemáticos; recalcular depois.
7. Criar marcadores `SPAWN_060_001` a `SPAWN_060_060` e `SPAWN_040_001` a `SPAWN_040_040` em coleções de diagnóstico ocultas no render.
8. Registrar mínimo, mediana e percentis das distâncias ao vizinho mais próximo, candidatos rejeitados e motivo. Mostrar visão superior técnica com os marcadores.

Não reduzir o mínimo de 300 m silenciosamente para atingir 60. Se não houver solução após correções razoáveis, registrar falha e apresentar o conjunto obtido com os conflitos; a especificação de início permanece não atendida até correção. Um teste que retorna 60 números não basta se esses pontos não existem em piso acessível.

---

## 12. Ritmo de partida e fechamento da arena — estudo visual

A intenção inicial é permitir partidas de aproximadamente **20–30 minutos**, mas esse intervalo é uma meta de design, não resultado comprovado. Dano, velocidade, recursos, habilidades e escolhas dos jogadores podem alterar radicalmente a duração.

Construir somente guias de zona no Blender, ocultos na apresentação artística. A zona deve ser interpretada como **coluna vertical sobre XY**, atingindo superfície e subterrâneo: descer para uma caverna não permite escapar do fechamento. Não criar esfera que trate altitude como vantagem indevida.

### 12.1 Ensaio reprodutível de zona

Para um primeiro estudo, usar centro fixo em (−600, −350), sobre terra a oeste/sudoeste do lago. Verificar a área final nesse local; criar terreno de confronto apropriado sem remover o acampamento. O centro fixo serve para auditoria, não é decisão de todas as partidas futuras.

| Intervalo | Estado da zona | Raio ao final |
|---|---|---:|
| 0–3 min | exploração inicial; sem contração | 4.000 m |
| 3–7 min | primeira contração gradual | 2.600 m |
| 7–8 min | pausa de reposicionamento | 2.600 m |
| 8–12 min | segunda contração | 1.800 m |
| 12–13 min | pausa | 1.800 m |
| 13–17 min | terceira contração | 1.000 m |
| 17–18 min | pausa | 1.000 m |
| 18–22 min | quarta contração | 450 m |
| 22–23 min | pausa | 450 m |
| 23–26 min | fase final de estudo | 120 m |

O círculo inicial inclui oceano ao redor; isso não adiciona área jogável. O raio de 4.000 m cobre inclusive os cantos do envelope retangular em relação ao centro de estudo; o canto mais distante fica a aproximadamente 3.937 m. Não reduzir a ilha para caber no círculo final.

Verificar o deslocamento real do ponto acessível mais desfavorável até a próxima região segura. Usar 6 m/s, caminhos reais e 30% de margem de tempo como ensaio inicial. Comparar o tempo calculado com o disponível; não ajustar números só porque o círculo visual parece bom.

Validar entradas subterrâneas, pontos sem saída e regiões separadas por água. Se uma zona cortar todas as saídas de uma caverna antes de permitir a fuga, corrigir o trajeto ou registrar ajuste proposto no cronograma. Não tratar o tempo total da tabela como duração garantida da partida.

Depois da auditoria fixa, preparar 3 posições alternativas de centro final em solo seco e acessível. Se centros mudarem entre fases, cada círculo menor deve ficar contido no anterior, considerando distância entre centros mais raio novo. Não aprovar centro final somente porque existe uma superfície de água plana no local.

---

## 13. Preparação do Blender e uso do MCP

### 13.1 Inspeção inicial obrigatória

Antes de modificar a cena:

1. Confirmar a conexão efetiva com o Blender e listar as operações que o MCP realmente disponibiliza.
2. Identificar versão instalada do Blender, arquivo aberto, objetos existentes, unidades, renderizadores disponíveis e pasta de trabalho com permissão de gravação.
3. Detectar se existe trabalho do usuário. Preservá-lo e salvar uma cópia de segurança antes de alterações amplas; não executar limpeza global da cena nem apagar coleções desconhecidas.
4. Verificar se é possível executar Python no Blender, inspecionar objetos, salvar o arquivo e obter uma captura/visualização. Não inventar nomes de ferramentas ou comandos MCP.
5. Definir a pasta de saída ao lado do projeto atual, quando houver; caso contrário, usar uma pasta de trabalho existente e gravável e informar seu caminho. Não gravar em uma pasta inventada ou inacessível.

Se houver Python disponível, usá-lo para geração determinística, instâncias, organização e verificações geométricas. Se não houver, usar as operações reais disponíveis em lotes menores. Quando uma capacidade indispensável não existir, explicar exatamente o que ficou bloqueado; não simular sucesso com texto.

A API e os nomes de opções dependem da versão instalada. Inspecionar antes de usar; não impor uma versão de Blender, renderizador, nó ou complemento inexistente. Evitar automação por interface quando uma operação de dados está disponível, mas respeitar as capacidades reais do ambiente.

### 13.2 Escala, câmeras e precisão

- Configurar unidade métrica e verificar um cubo/medidor de 2 m e a cápsula humana de referência.
- Configurar plano distante de viewport e câmeras de mapa para cobrir a ilha e cenário de fundo, inicialmente cerca de 15.000 m; ajustar plano próximo conforme a câmera. Não usar o mesmo clipping extremo em todas as câmeras de interior.
- Câmeras de exploração começam com plano próximo de aproximadamente 0,1 m, revisto se necessário; mapa geral pode usar plano próximo maior para estabilidade.
- Corrigir navegação e clipping quando a ilha parecer desaparecer. Não encolher a geometria para caber na visualização padrão.
- Usar coordenadas próximas da origem definida; não transladar a ilha para distâncias desnecessariamente grandes.
- As funções de instanciamento e unidades são recursos do Blender; elas não constituem automaticamente streaming, colisão ou otimização dentro de uma engine futura.

### 13.3 Atualização e acompanhamento visível

Após cada lote significativo, atualizar a cena, deixar a região trabalhada enquadrada e apresentar uma captura se o MCP oferecer esse recurso. Fazer uma atualização curta a cada etapa e quando uma operação demorada terminar. Lotes com duração alvo de 20–60 s ajudam a evitar bloqueio prolongado; ajustar à máquina real.

Se o MCP executar comandos sem disponibilizar acompanhamento em tempo real, informar isso e entregar capturas/renders entre etapas. Não prometer transmissão contínua nem afirmar que o usuário está vendo um progresso que não foi exibido.

Para inspeção, disponibilizar modos de visualização geral, superfície, subterrâneo e diagnóstico. As opções podem ser coleções/câmeras organizadas; não é necessário instalar uma interface customizada.

---

## 14. Organização da cena e nomes

Usar um `.blend` mestre com todas as regiões posicionadas. Dividir coleções para facilitar ocultação, isolamento e carregamento visual. Se o hardware exigir arquivos de apoio, manter o mestre como entrada única e garantir que todas as referências locais acompanhem a entrega e reabram corretamente.

| Coleção | Conteúdo |
|---|---|
| `ARKANA_00_CONTROLE` | origem, escala, medidores, limites e estado; ocultos no render |
| `ARKANA_01_TERRENO` | terreno por setores, falésias, costa e leitos |
| `ARKANA_02_AGUA` | mar, lago, rios, cachoeiras e água subterrânea |
| `ARKANA_03_ROTAS` | pisos e estruturas de circulação entre regiões |
| `ARKANA_R01` a `ARKANA_R12` | construções e conteúdos regionais, IDs desta especificação |
| `ARKANA_20_TRANSICOES` | áreas periféricas e pontos secundários |
| `ARKANA_21_VEGETACAO` | instâncias organizadas por setores |
| `ARKANA_22_KIT_MODULAR` | modelos-fonte, sem duplicatas indesejadas na apresentação |
| `ARKANA_30_LUZ` | sol e luzes locais |
| `ARKANA_31_CAMERAS` | câmeras gerais, regionais e de exploração |
| `ARKANA_90_DIAGNOSTICO` | spawns, caminhos de teste, medidas e guias de zona |
| `ARKANA_91_COLISAO_PROPOSTA` | volumes simplificados para referência futura, sem alegar engine funcional |

Evitar posse ambígua: água pertence à coleção de água; terreno à coleção de terreno; um objeto compartilhado por várias coleções continua sendo um objeto, não vários exemplares sobrepostos. Guias de rotas de diagnóstico ficam separados dos pisos construídos.

**Nomes:** `R03_Templo_Coluna_A_001`, `R04_Casa_B_012`, `R11_Tunel_U07_U09_Piso_001`, `TER_05_07`, `CAM_R04_Geral`, `SPAWN_060_001`. Usar identificadores ASCII nos objetos e arquivos para facilitar automação. Nomes descritivos no relatório podem usar acentos.

**Propriedades recomendadas:** `arkana_id`, `region_id`, `stage`, `asset_role`, `source_module`, `generation_seed`, `walkable`, `collision_role`, `spec_version`. Não depender somente do nome para reconhecer um objeto gerado.

### 14.1 Construção reprodutível e retomada

- Semente global: `24092026`; sementes por setor derivadas deterministicamente de ID e semente global, sem usar hash instável entre processos.
- Scripts devem criar ou atualizar objetos próprios pelo identificador; uma nova execução não duplica árvores, materiais ou pontes já existentes.
- Não modificar trabalho do usuário fora das coleções/propriedades do projeto sem necessidade e autorização correspondente.
- Registrar etapas completas, pendências e resultados em blocos de texto dentro do `.blend`, por exemplo `ARKANA_ESTADO`, `ARKANA_DECISOES`, `ARKANA_VALIDACAO` e `ARKANA_CONFIG`.
- Salvar os scripts utilizados dentro do `.blend` e, quando possível, também ao lado do projeto. Isso é saída de produção, não outro documento que o usuário precisa fornecer.
- Antes de retomar após interrupção, inspecionar o arquivo e o estado real. Não confiar apenas na última mensagem de chat.
- Se um lote falhar, corrigir/reexecutar só o setor ou etapa afetada. Preservar o último arquivo válido.

---

## 15. Estratégia de modelagem e desempenho

### 15.1 Terreno em setores

Usar grade organizacional de 400 × 400 m, cobrindo 12 colunas por 11 linhas do envelope. Os 132 setores teóricos são recortados pela costa. Essa divisão é interna; não deve aparecer como rachadura ou mudança de material.

No blockout, malha de terreno com espaçamento inicial de cerca de 10 m. Refinar localmente para 2–5 m nas margens, cortes, caminhos e acessos que exigirem precisão. Pedras, entradas, escadas e pontes usam geometria própria; não aumentar a resolução de toda a ilha para detalhar um degrau.

Setores vizinhos compartilham as mesmas amostras de altura nas bordas. Manter máscaras de caminhos, água, construções, spawns e transições para que o espalhamento de vegetação não invada áreas funcionais.

### 15.2 Kit modular mínimo

| Família | Variações-base a produzir |
|---|---|
| Rocha | 8–12 pedras, 4 segmentos de falésia, 3 pisos rochosos |
| Vegetação | 4–6 árvores comuns, 3 gigantes secundárias, árvore principal própria, 4 arbustos, 4 grupos de chão, 3 troncos/raízes |
| Arquitetura antiga | 3 paredes, 3 pilares/colunas, 2 arcos, 3 pisos, 2 escadas, 2 parapeitos, 2 altares |
| Vila | 4–6 casas-base com variações de dano, portas, janelas, vigas e telhados separados |
| Acampamento | 3 barracas, caixa, barril, mesa, corda e fogueira |
| Industrial | 3 módulos de galpão, tubos retos/curvos/junções, tanque, passarela, apoio e escada |
| Militar | módulos de parede, alojamento, posto, cerca, barreira e veículos simplificados |
| Subterrâneo | piso, teto, paredes curvas, suportes, cristais, ponte e ruína |

Instanciar peças repetidas e compartilhar materiais. Variar escala dentro de limites físicos, rotação, dano e composição; não produzir deformações que mudem portas de tamanho ou escadas de inclinação. Realizar instâncias apenas quando necessário à edição/inspeção do trecho, evitando converter vegetação de toda a ilha sem motivo.

### 15.3 Níveis de detalhe

Preparar três representações onde houver repetição pesada: próxima, média e distante. A escolha pode ser manual por coleção ou automatizada se o ambiente suportar; não afirmar que LOD de engine existe por ter três versões no Blender.

- Próxima: volumes e materiais que sustentam vista a 1,65 m, interiores previstos e silhuetas de objetos.
- Média: geometrias simplificadas que preservam forma e leitura de terreno.
- Distante: silhuetas e massas de vegetação, sem interiores invisíveis nem folhas individuais modeladas em excesso.

Manter o mundo completo posicionado; reduzir densidade visual de cópias distantes e resolução de detalhes, sem remover regiões ou diminuir sua escala.

### 15.4 Limites iniciais de trabalho

Sem conhecer o hardware, não fixar um orçamento como garantia. Começar com alvo de até 2 milhões de triângulos avaliados visíveis no blockout e até 5 milhões na visualização geral de trabalho. Usar instâncias, níveis de detalhe e isolamento de coleção antes de aumentar esses valores.

Registrar quantidade de objetos, malhas únicas, triângulos avaliados visíveis, instâncias e memória, quando essas métricas puderem ser obtidas. Triângulos únicos de uma malha e triângulos avaliados com todas as instâncias são medidas diferentes. Não reportar a primeira como se fosse a segunda.

Texturas: preferir materiais reutilizáveis e mapas de 1K/2K; 4K somente para elementos principais quando fizer diferença verificável. Não gerar textura exclusiva de alta resolução para cada pedra. Materiais procedurais do Blender podem exigir conversão futura; documentar sem iniciar exportação prematura.

Testar se o usuário consegue orbitar a visão geral, entrar em regiões e alternar subterrâneo/superfície. Se houver lentidão, reduzir representação de viewport, subdivisões, sombras locais e detalhamento distante. Registrar medições reais; não prometer FPS da engine nem suporte multiplayer com base nisso.

---

## 16. Sequência de execução do começo ao fim

Não detalhar uma região inteira antes de existir o blockout da ilha completa. Todas as fases produzem arquivos salvos e evidência visual. Os pontos de verificação são técnicos, não pedidos automáticos de confirmação.

| Fase | Trabalho | Evidência e condição para avançar |
|---|---|---|
| F00 — inspeção | conexão, versão, cena, backup, pasta e capacidades | estado real descrito e primeiro arquivo salvo |
| F01 — implantação | unidades, costa, setores, terreno macro, cotas, lago e rio | vista superior com dimensões; nenhuma ilha em miniatura |
| F02 — blockout completo | volumes das 12 regiões, rotas, pontes, cavernas e entradas | mapa inteiro existe; subterrâneo inspecionável |
| F03 — auditoria espacial | área acessível, declives, cápsula, conexões e 40/60 inícios | resultados medidos; corrigir falhas estruturais antes de detalhar |
| F04 — kit e linguagem | módulos naturais/arquitetônicos; amostra de materiais | kit consistente instalado em pequenas áreas de todas as famílias |
| F05 — geografia detalhada | R01, R02, R03 e R04 | lago, floresta, templo e vila com acessos e interiores previstos |
| F06 — conexões detalhadas | R05, R06, R07 e R08 | ponte, acampamento, indústria e base completos |
| F07 — vertical e subterrâneo | R09, R10, R11 e R12 | torre, cavernas e todos os túneis completos |
| F08 — integração | periferia, litoral, pontos secundários, vegetação e água | nenhuma região sem transição nem grandes vazios acidentais |
| F09 — apresentação | materiais finais, luz, câmeras e passeio | vistas gerais, 12 vistas regionais e passeio visível |
| F10 — auditoria final | repetir verificações afetadas pelo detalhe e reabrir projeto | entrega completa, limitações e medições registradas |

Em F03, a árvore e os edifícios podem ser proxies dimensionados; a topologia das rotas já precisa ser real. Em F10, proxies dos elementos obrigatórios devem ter sido substituídos. Pedras, vegetação e entulho adicionados depois não podem bloquear rotas que passaram anteriormente.

### 16.1 Procedimento por lote

1. Ler o estado salvo e identificar a próxima unidade de trabalho.
2. Gerar ou atualizar somente o conjunto previsto.
3. Conferir medidas, apoio, coleções e ligação com vizinhos.
4. Atualizar a visualização e salvar o checkpoint se o lote estiver consistente.
5. Capturar uma vista pertinente e informar o que foi concluído, o que falhou e o próximo passo.
6. Prosseguir até F10; se o usuário interromper, deixar arquivo e estado suficientes para retomada.

Um timeout não prova que nada foi criado. Inspecionar a cena antes de repetir uma operação, evitando duplicações.

---

## 17. Câmeras, passeio e apresentação no Blender

### 17.1 Câmeras obrigatórias

| Nome | Finalidade |
|---|---|
| `CAM_Mapa_Topo` | vista ortográfica superior; norte para cima; ilha inteira |
| `CAM_Mapa_Sudoeste` | vista aérea oblíqua geral com leitura de relevo |
| `CAM_Mapa_Nordeste` | lado oposto para conferir volume e partes posteriores |
| `CAM_R01_Geral` a `CAM_R12_Geral` | uma vista ampla e legível por região |
| `CAM_Ponte_Alternativas` | ponte, travessia inferior e acessos |
| `CAM_Subsolo_Geral` | visão técnica do subterrâneo com superfície ocultada |
| `CAM_Exploracao` | percurso à altura dos olhos, com referência de 1,65 m |

Na vista superior, rótulos podem aparecer somente na coleção de diagnóstico. Na apresentação artística, ocultar nomes, marcadores, spawns, círculos de zona e volumes de teste. O pedido original de concepts limpas continua válido para os renders de arte.

### 17.2 Passeio de verificação

Preparar um trajeto de superfície: acampamento → vila → floresta → torre → retorno à floresta → margem do lago → templo → indústria → base → ponte → acampamento.

Preparar trajeto subterrâneo: floresta → U01 → U02 → U03 → U04 → U14 → U12 → U11 → U09 → U07 → U06/vila. Mostrar também o ramal até U15.

As câmeras devem percorrer corredores reais e acompanhar pisos; não atravessar montanhas para fingir conectividade. Um voo aéreo cinematográfico pode existir como apresentação adicional, claramente distinto do passeio por solo.

Criar sequência de câmera pré-visualizável de 60–120 s com trechos representativos; ela não precisa cobrir quilômetros à velocidade real. Cortes entre regiões são permitidos, identificados no planejamento. Para provar tempo de deslocamento, usar medidas de rota, não a duração da montagem.

### 17.3 Saídas visuais

- Renders de trabalho em aproximadamente 1.280 × 720 para conferência rápida.
- Renders finais gerais e regionais em pelo menos 1.920 × 1.080, quando a máquina permitir, com geometria legível.
- Imagens técnicas de superfície, subterrâneo, caminhos e distribuição inicial.
- Uma sequência de passeio reproduzível na cena. Render de vídeo completo é opcional conforme recursos; não substitui o `.blend` editável.

Se render final estiver tecnicamente bloqueado, preservar câmeras e entregar previews existentes identificados como tal. Não chamar captura de viewport de render final de alta qualidade.

---

## 18. Verificação geométrica e critérios de conclusão

### 18.1 Como medir sem fingir teste de engine

Usar geometria avaliada, transformações globais, consultas de superfície e cápsula/volumes de referência conforme as ferramentas disponíveis. Projeção vertical simples não detecta todas as colisões laterais; inspecionar folgas e obstáculos ao longo das rotas.

Construir um grafo de caminhada a partir do chão real amostrado e das ligações verticais construídas. Arestas devem passar em inclinação, altura livre e ausência de obstrução. Testes em uma curva desenhada manualmente não demonstram que a geometria permite a passagem.

Calcular caminhos mínimos nesse grafo para verificar distâncias e conectividade. Registrar resolução de amostragem e limitações. Não chamar esse grafo de navmesh da engine, nem as marcações de colisão de sistema físico já validado em jogo.

### 18.2 Lista de aceitação

| ID | Critério | Evidência exigida |
|---|---|---|
| V01 | extensão de aproximadamente 4.800 × 4.400 m | bounds globais do terreno emerso; tolerância de 1% |
| V02 | terra emersa de 13–15 km² e superfície acessível de 10–12 km² | relatório de área com método e exclusões; falha registrada se fora da meta |
| V03 | todas as 12 regiões presentes e nas relações definidas | tabela de âncoras reais, bounds e capturas |
| V04 | nível do lago e rios coerentes | cotas da água e verificação de continuidade descendente |
| V05 | maior componente de superfície com pelo menos 95% da área acessível | resultado do grafo sobre terreno real |
| V06 | todas as conexões S e U construídas | lista de rotas, comprimento, inclinação e resultado de percurso |
| V07 | ponte com alternativas funcionais | percurso após desativar apenas a travessia principal no grafo |
| V08 | nenhum setor depende de magia para acesso principal | ensaio com cápsula e rotas por solo |
| V09 | 60 e 40 posições iniciais atendendo às regras | distâncias, visibilidade, folgas e distribuição registradas |
| V10 | pisos, portas e pé-direito coerentes | medidas por família e inspeção de pontos críticos |
| V11 | subterrâneo não invade terreno sólido/lago indevidamente | inspeção em seção e medições de espessura |
| V12 | interiores obrigatórios presentes | inventário por região e vistas internas |
| V13 | sem junções visivelmente abertas | inspeção das bordas dos setores e encontros de estruturas |
| V14 | sem bloqueios após vegetação/entulho | repetição das rotas afetadas pelo detalhamento |
| V15 | todas as regiões recebem acabamento | vista regional e lista de proxies restantes |
| V16 | zona de estudo permite rota de fuga | tempos de caminho e saídas, incluindo subsolo |
| V17 | referências e recursos de arquivo resolvidos | salvar, reabrir e conferir materiais/coleções |
| V18 | acompanhamento e arte final inspecionáveis | câmeras, previews, renders disponíveis e passeio preparado |

### 18.3 Estados permitidos

- **Blockout completo:** todas as regiões e ligações existem em volumes simples; não é arte final.
- **Versão visual completa para revisão no Blender:** regiões detalhadas, interiores obrigatórios, materiais, luz e apresentação; critérios verificados e limitações identificadas.
- **Pendente de validação de gameplay:** sempre aplicável até testar movimentação, combate, zona e rede na engine futura.

Uma falha em V01, V03, V04, V06, V07 ou ausência de regiões/interiores obrigatórios impede chamar o mapa de completo. V02 e V09 também são requisitos desta proposta espacial: se não passarem, apresentar a versão como revisão pendente e continuar corrigindo quando possível. Desempenho de engine e duração de partida não fazem parte de uma alegação de aprovação no Blender.

Não esconder defeitos com enquadramento, folhagem, escuridão ou neblina. Não substituir geometria faltante por promessa de que será feita na engine.

---

## 19. Salvamento e entrega ao usuário

Criar nomes versionados para checkpoints, por exemplo:

- `ARKANA_F01_Implantacao_v001.blend`
- `ARKANA_F02_Blockout_Completo_v001.blend`
- `ARKANA_F03_Auditoria_v001.blend`
- `ARKANA_F08_Ilha_Integrada_v001.blend`
- `ARKANA_Ilha_Completa_Blender_v001.blend`

Os exemplos são arquivos de saída a produzir no computador do usuário, não arquivos que já existem. Nunca afirmar que um deles foi salvo sem resultado confirmado da operação.

O último arquivo mestre deve abrir com a ilha enquadrada em vista geral, coleções nomeadas, luz coerente e diagnóstico oculto. Subterrâneo deve estar acessível por organização clara, sem apagar o terreno de cobertura.

Empacotar recursos que puderem ser incorporados ao `.blend`; para dependências não empacotáveis, usar caminhos relativos e entregar a pasta correspondente. Não depender de texturas temporárias ou arquivos em outra máquina. Salvar, reabrir e conferir o arquivo final antes de concluir.

Dentro do mestre, manter:

| Bloco de texto | Conteúdo |
|---|---|
| `ARKANA_DOCUMENTO_MESTRE` | cópia integral desta especificação |
| `ARKANA_CONFIG` | valores canônicos e ajustes registrados |
| `ARKANA_ESTADO` | fases concluídas, próxima etapa, arquivos e pendências |
| `ARKANA_DECISOES` | escolhas locais, motivo e impacto |
| `ARKANA_VALIDACAO` | métricas, testes, falhas e limitações |
| `ARKANA_COMO_VISUALIZAR` | câmeras, coleções, passeio e versão instalada |

Na mensagem de conclusão, informar caminho real do `.blend`, o que está completo, como alternar superfície/subterrâneo, onde estão os renders e quais pontos ainda dependem de engine/playtest. O usuário deve conseguir abrir e inspecionar a ilha sem reler vários documentos.

---

## 20. Configuração central para implementar

O JSON abaixo resume parâmetros globais, não substitui as tabelas de regiões e ligações. A unidade de todos os comprimentos é metro, exceto campos explicitamente nomeados. Copiar para `ARKANA_CONFIG` e acrescentar medidas reais em campos distintos, preservando os alvos.

```json
{
  "project": "ARKANA",
  "spec_version": "1.0",
  "seed": 24092026,
  "players_min": 40,
  "players_max": 60,
  "units": "meters",
  "blender_unit_scale": 1.0,
  "north_axis": "+Y",
  "east_axis": "+X",
  "up_axis": "+Z",
  "bounds_xy": {"min": [-2400, -2200], "max": [2400, 2200]},
  "coast_reference_semiaxes": [2400, 2200],
  "dry_land_area_target_km2": [13, 15],
  "surface_walkable_area_target_km2": [10, 12],
  "largest_walkable_component_min_fraction": 0.95,
  "sea_z": 0,
  "lake_center": [0, 150, 105],
  "lake_size_xy": [900, 700],
  "terrain_max_z": 620,
  "world_visual_max_z": 720,
  "terrain_sector_size": 400,
  "terrain_sector_counts_xy": [12, 11],
  "terrain_base_spacing": 10,
  "surface_audit_spacing": 10,
  "surface_audit_refined_spacing": 2,
  "character_height": 1.8,
  "character_radius": 0.45,
  "eye_height": 1.65,
  "walk_speed_mps": 6,
  "run_speed_mps": 9,
  "optional_dash_distance": 12,
  "optional_dash_cooldown_s": 10,
  "free_flight_assumed": false,
  "primary_route_width": [12, 20],
  "secondary_route_width": [5, 8],
  "common_walkable_slope_max_deg": 25,
  "spawn_slope_max_deg": 10,
  "spawn_candidate_count_min": 84,
  "spawn_pair_min_xy": 300,
  "spawn_neighbor_path_target_min": 400,
  "spawn_los_check_radius": 500,
  "bridge_center": [200, -1050, 125],
  "bridge_size_length_width": [300, 16],
  "zone_study_center_xy": [-600, -350],
  "zone_initial_radius": 4000,
  "zone_final_radius": 120,
  "secondary_poi_target": [24, 36],
  "small_shelter_target": [60, 90],
  "engine_export_requested": false,
  "gameplay_validated": false
}
```

---

## 21. Regras contra inconsistência e trabalho incompleto

1. Não trocar posições cardeais das regiões para imitar o enquadramento de uma imagem.
2. Não construir cada região em uma origem independente e tentar encaixar tudo apenas no final.
3. Não compactar os 12 núcleos ao redor do lago como atrações de um parque em miniatura.
4. Não aumentar casas, portas, escadas e personagens para compensar escala territorial incorreta.
5. Não deixar litoral e transições sem modelagem porque as concept arts se concentram no centro.
6. Não usar água, paredões e telhados inacessíveis como evidência de espaço útil para 60 jogadores.
7. Não remover alternativas de rota para economizar modelagem.
8. Não criar passagens que terminam em rocha sólida, plataformas sem acesso ou interiores sem piso.
9. Não abrir o teto de toda a ilha para mostrar o subterrâneo na arte final.
10. Não transformar todas as superfícies em emissivos, todas as áreas em ruínas iguais ou toda a floresta em uma única árvore.
11. Não instalar ferramentas, comprar assets ou depender de serviços externos sem necessidade e autorização aplicável. O primeiro caminho é gerar os módulos no próprio Blender.
12. Não inventar operação MCP, resposta de ferramenta, captura, render, teste ou arquivo salvo.
13. Não iniciar exportação ou integração com engine nesta tarefa. Preparar organização reutilizável e documentar requisitos futuros.
14. Não encerrar após F02 por considerar o blockout suficiente. O pedido inclui a versão visual completa para revisão no Blender.
15. Não garantir ausência de erros, partidas longas ou desempenho multiplayer. Reduzir ambiguidade com parâmetros e verificar com evidências.

---

## 22. Referências técnicas e origem das decisões

As dimensões, distribuição inicial, tempos, cotas, quantidades e regras espaciais deste arquivo são **decisões de projeto propostas para ARKANA**, criadas para tornar a produção executável. Não foram medidas das concept arts e não são recomendações oficiais de Blender ou parâmetros comprovados de battle royale.

O conteúdo artístico consolida os dois documentos originais fornecidos pelo usuário, preservando as 12 regiões, as três camadas, a linguagem visual compartilhada e as conexões do mundo. A especificação resolve a posição conflitante do templo a favor do leste/sudeste e amplia explicitamente o território para o objetivo de 40–60 magos.

Referências oficiais consultadas para princípios técnicos; consultar a documentação correspondente à versão instalada ao implementar:

- [Blender Manual — Scene Properties](https://docs.blender.org/manual/en/5.0/scene_layout/scene/properties.html): sistema métrico e escala de unidades.
- [Blender Manual — Instances](https://docs.blender.org/manual/en/latest/modeling/geometry_nodes/instances.html): instâncias reutilizam dados geométricos, em vez de duplicar toda a geometria-base.
- [Blender Manual — 3D Viewport Sidebar](https://docs.blender.org/manual/en/latest/editors/3dview/sidebar.html): limites próximo e distante de visibilidade da viewport.

Essas referências não atestam o layout, o dimensionamento ou o equilíbrio do jogo. A construção espacial é avaliada pelas medições deste documento e o equilíbrio será avaliado posteriormente em gameplay.

---

## 23. Comando de início para o Claude Code

> Leia integralmente este documento e use-o como a única especificação textual de ARKANA. Inspecione o Blender conectado por MCP, preserve o trabalho existente e construa a ilha completa de 4.800 × 4.400 metros nas coordenadas definidas. Comece com todas as 12 regiões em blockout, verifique área acessível, cotas, ligações e distribuição inicial para 40 e 60 jogadores, e então avance até o mapa inteiro detalhado, com interiores obrigatórios, subterrâneo, materiais, vegetação, iluminação e câmeras. Mostre resultados entre etapas, salve checkpoints e registre as medições no próprio `.blend`. Continue autonomamente quando as verificações passarem, sem pedir aprovação a cada etapa. Não reduza a ilha, não encerre no blockout e não integre com uma engine ainda. Entregue o arquivo mestre completo e editável para que possamos visualizar a ilha no Blender; informe qualquer limitação real ou requisito que ainda não foi atendido.

**Resultado esperado:** um único mundo de ARKANA, grande, contínuo e visualmente completo, com medidas auditáveis e condições claras para a futura validação de um battle royale entre magos.
