# PROJETO — estado atual do Arkana

> **Este é o documento de memória do projeto.** Quem chegar agora deve conseguir
> ler só este arquivo e entender: onde o projeto está, o que já foi feito, o que
> falta e o que está bloqueado. Atualize-o ao fechar cada fase.
>
> **Atualizado em:** 25/09/2026 (nova visão da ilha: Documento Mestre 4,8 km, trios, Unity primeiro; MCP for Unity instalado). Antes: 23/09/2026 (esteira do emulador: AVD + variante de build + `emulador.ps1` com partida inteira PASS; auditoria técnica; Blender sem MCP; push de `main` feito; bancada corrigida)
>
> **REGRA PERMANENTE (ordem do Diretor, 20/08):** ao finalizar QUALQUER fase,
> atualize este documento com **o que foi feito, como e por que**, e reescreva
> a secao CONTINUAR DAQUI com tudo que ainda falta. Esta secao nunca pode
> apontar para uma fase antiga. Este e' o UNICO documento de memoria do
> projeto — nao crie outro.

---

## DIRETRIZ PERMANENTE DO DIRETOR (25/08/2026)

*"O tamanho do APK não importa hoje. Para testar e validar isso é irrelevante,
meu celular suporta. O importante é validar com o que é real, mais importante
do que deixar o arquivo comprimido e sem qualidade. Com esse excesso de
segurança e travas de lógica sem sentido... eu quero correr os riscos, fazer
acontecer com qualidade e funcionalidade, claro com lógica, sem atropelar as
coisas, mas sem esse medo excessivo."*

**Como isso muda o trabalho, em concreto:**

- **PARE de usar peso de arquivo como argumento** para recusar asset melhor,
  textura maior ou modelo mais detalhado. Ele tem 20+ GB e o aparelho aguenta.
- **Qualidade e funcionalidade reais valem mais que otimização precoce.**
  Compressão, atlas e formato de loja entram quando as projeções fecharem.
- **O que CONTINUA valendo**, e não é a mesma coisa: **quadros por segundo no
  aparelho**. Pacote grande é irrelevante; jogo travando é o jogo não
  funcionando. Se um asset derrubar o FPS, o problema é o FPS — diga isso com
  MEDIÇÃO no aparelho, nunca com estimativa de disco.
**O APARELHO DE TESTE, agora conhecido (25/08):** **Poco F4** — Snapdragon 870,
GPU Adreno 650, tela 120 Hz. Nao e' aparelho de entrada: e' alto desempenho de
2022 e roda jogo pesado. Ate' hoje o projeto mirava "aparelho intermediario"
generico, o que era chute. **A regua de desenvolvimento pode ser bem mais
generosa do que a que estava escrita** nos orcamentos de TECH_ART.
Duas coisas seguem verdadeiras mesmo assim: o aparelho do Diretor NAO e' o
aparelho do jogador final (isso volta a importar em G6, na loja), e **FPS se
mede, nao se estima** — e nunca foi medido neste projeto, porque nenhum
aparelho apareceu em `adb devices`.

- **O que também continua valendo:** teste provado em vermelho, portão verde
  antes de entregar, e nada de atropelar etapa. Ele pediu risco com lógica, não
  ausência de método. *"sem atropelar as coisas"* são as palavras dele.

---

## CONTINUAR DAQUI

### >>> 26/09, fim da madrugada (8): ESTADO ATUAL — o que continuar amanhã

**Fechado nesta leva (tudo em main, 525 testes EditMode verdes):** ilha flutuante com base de rocha e mar de nuvens;
acampamento com peças CC0 do Kenney; kit por script levas 5–6 (torre de observação andável, santuário, pátio, terraço,
poço, doca, heliponto, torrinha); reviver completo — canal de 8 s junto ao corpo, volta com 30 % de vida, sem escudo e
só a varinha; corpo do ALIADO eliminado não afunda mais (VisualDoAbate), bússola verde aponta para ele, aviso
"REVIVENDO <nome>" com prioridade máxima (`AvisoLogica.P_REVIVER = -1`) e anel verde — conferido na tela de jogo
(`ScreenCapture.CaptureScreenshot` em Play captura a interface; a câmera do MCP não).
**Amanhã, nesta ordem:** (1) o Diretor assina o Tripo e cola a chave em `arte/tools/.env.tripo`; (2) rodar
`python arte/tools/tripo.py lote arte/cenario/documento-mestre/lote-tripo-validacao.txt` (7 peças) e aprovar o estilo
com foto no Unity; (3) lote completo (43 peças) — árvore gigante primeiro (R02 ainda é blockout); (4) APK pela CLI
(batchmode, sem o diálogo do Input Handling) e teste no Poco F4. **Pendente de decisão do Diretor:** ProjectSettings,
cena Main e URP_Base (sombra 50→60) alterados pelo build — fora do commit.

### >>> 26/09, madrugada (7): ilha no céu de verdade (base de rocha + mar de nuvens) e acampamento com peças CC0

Sem Tripo esta noite (assinatura amanhã) e sem APK — decisão do Diretor. **Base de rocha** (`IlhaMestre.Base.cs`): cone
irregular pendurado sob a costa real (256 direções, 16 anéis, afina até 700 m), rocha do Terrain nos dois lados, sem
colisor. Armadilha: a 1ª ordem de triângulos deixou as normais para dentro — a rocha ficava preta do lado do sol.
**Céu** (`ArkanaCeu.shader`, `_Abismo`): abaixo do horizonte, um mar de nuvens em faixas toon (as cores dos cúmulos),
escurecendo para o lilás no nadir; ligado pela IlhaMestre quando `IlhaFlutuante`. Luz ambiente de baixo 0,28 → 0,62
(a base recebe o reflexo das nuvens). **Acampamento R06:** peças do Kenney Survival Kit convertidas por
`arte/tools/blender/cc0_para_unity.py` (escala real, textura embutida → `Resources/cc0-*.glb`, 14 peças): 2 barracas
grandes, 3 fogueiras (1 com espeto) com colchonetes, bancada, baú, lenha, tábuas, ferramentas, baldes, placa, 3 barracas
extras; barracas do kit Meshy em 1,8× (tinham 2 m). A `structure-canvas` do Kenney renderiza só a armação (a lona some) —
não usar. Fotos conferidas no editor. **HUD do reviver:** anel de canalizar em verde + "REVIVENDO <nome>" para quem revive,
"VOLTANDO · N%" para o eliminado (`Hud.Revivendo`, alimentado em `Main.AtualizarHud`); testado em partida (canal a 79 %,
voltou com a varinha). A foto de câmera NÃO mostra a interface (canvas overlay): conferir UI pela Game view. **Blocos
escuros das entradas U** removidos (tampavam as bocas reais); só voltam se faltar o JSON do subterrâneo.
**Kit leva 5 (script, grátis):** 094 torre de observação (fuste oco 11×11, janelas em arco, ESCADA INTERNA em espiral
quadrada de lances de 3 m com patamares até a plataforma a 48 m, sacada a 24 m, campanário e agulha a ~58 m), 096 santuário
(embasamento 30×30 com escadaria, arcada andável com altar, 2º corpo com pináculos, campanário, ~49 m), 201 lajeado do
pátio e 202 terraço do templo (pedra texturizada no lugar dos blocos bege). Helpers novos: `piramide()`, `aduelas()`.
R03 e R09 sem blockout; 094/096 saíram do lote Tripo (43 peças). Lote de validação: 7 peças (uma por família) em
`lote-tripo-validacao.txt`. A textura pedra-templo a 2 m/UV parece listrada de perto — vetável: trocar a escala.
**Kit leva 6 (script):** 203 poço da praça (bocal de pedra, telhadinho, balde), 204 doca do lago (tabuado sobre estacas;
o pivô é o pé das estacas, tabuado 3,4 m acima), 205 heliponto (concreto 28 m, faixa amarela, H) e 206 torrinha
arruinada da vila — todos no lugar dos blocos. Helpers: `mat_cor()` (pintura sem textura), `disco()`. Os itens que o
catálogo dava como "faltando no kit" (heliponto, comando, alojamentos, doca, poço) estão feitos; resta o veículo civil
(Tripo: 077 caminhão está na validação).

### >>> 25/09, noite (6): TRIPO assinado (25.000 créditos/mês) — prazo da ARTE: 26/10/2026 — ferramentas instaladas

O Diretor assinou o plano grande do Tripo (≈1.660 modelos/mês, lote, DCC Bridge, uso comercial). **Instalado:** SDK
`tripo3d` (pip); plugin `tripo-3d-for-blender` v0.7.7 ativado no Blender 5.2 (painel N → Tripo: cola a chave lá);
**Tripo Bridge 1.0.14** como pacote embutido em `mobile-unity/Packages/Tripo3d_Unity_Bridge` (menu Tools → Tripo
Bridge; só no Chrome/Edge, só no modo editor; ainda NÃO verificado no Unity — abrir e conferir o menu); script
`arte/tools/tripo.py` (saldo / imagem / texto / lote / baixar) que grava `arte/cenario/<slug>/<slug>.glb` — a chave
vai em `arte/tools/.env.tripo` (`TRIPO_API_KEY=tsk_...`, ignorada pelo git; eu nunca leio). Lote pronto:
`arte/cenario/documento-mestre/lote-tripo.txt` com as 50 peças orgânicas do catálogo (12.000 faces, v3.1, PBR, 8 em
paralelo). **Regra:** validar a 1ª peça de cada família com foto antes de rodar o lote inteiro (créditos). O MCP oficial
do Tripo está parado (alpha 2025): não usar. Máquina: RTX 3050 6 GB / 8 GB RAM — nada de gerar 3D local. **CC0 instalado:** `arte/tools/cc0.py` baixou Kenney *Nature Kit* (330 modelos: árvores, pedras, folhagem) e *Survival
Kit* (80: barracas, caixas, fogueira) em `arte/cenario/cc0/` (fora do git, reproduzível). Quaternius (Ultimate Nature,
150 modelos) só sai por pasta do Google Drive (sem link direto): se quiser, baixar a mão em `arte/cenario/cc0/quaternius/`.
**Mixamo** não se instala: é site (login Adobe, o Diretor tem CC): sobe o FBX do mago (T-pose) → auto-rig → baixa as
animações (strafe esquerda/direita, pulo, corrida) em FBX "without skin" → Unity importa como Humanoid e retarget.
**Prazo do Diretor: criação de arte fechada até 26/10.**

### >>> 25/09, noite (5): ILHA FLUTUANTE (sem mar) + a regra do REVIVER — decisões do Diretor

**Diretor, 25/09:** "em volta da ilha não precisamos ter o mar" — nenhum jogo de referência tem; a borda é QUEDA: quem
passa do mapa morre e precisa ser revivido. **Feito:** `IlhaMestre.IlhaFlutuante = true`: sem o plano do mar, o Terrain
abaixo de 0,5 m vira buraco (sem colisor), `RelevoMestre.VazioY = −25`: o `Pawn` que passa disso morre direto
(`Combat.MorrerNoVazio`: sem escudo, sem cair derrubado — não há chão lá embaixo). O relevo NÃO mudou (a "praia" ainda
desce até 0 e some); o visual de ilha no céu (nuvens embaixo, borda rochosa) fica para uma leva de arte. Blender/doc
seguem com mar até refazer (a réplica v002 está com mar). **Medido em partida (25/09):** jogador pousado, teleportado para
(−2.700, −30, 0): viva=False, hp=0, não derrubado, PlayerFora=True (a câmera passa ao parceiro). Construir() da ilha com os
buracos do vazio: ~20 s no editor (o MCP devolve nulo, mas termina).
**REVIVER (Diretor, a implementar — como Apex/Warzone, "incentiva a continuar jogando e acreditar que o time possa dar
um jeito"):** existe uma forma de trazer o morto de volta (comprar/reviver); quem volta vem com o BÁSICO: sem escudo, sem
manoplas duplas, só a manopla base do elemento de MAIOR VÍNCULO. Hoje existe o `Derrubado` (caído + reerguer por aliado);
**FEITO (25/09, medido em partida):** `Partida.Reviver` + canal `TickReviver`: um aliado DE PÉ a ≤ 2,4 m do corpo do
eliminado por 8 s (`REVIVER_S`, vetável; decai se afastar) traz o morto de volta com 30 % de vida, escudo 0 e SÓ a luva
base (`Arma.VARINHA`) do elemento do mago (`IdentidadeMago.Elemento` — hoje é o "elemento de maior vínculo"; não existe
vínculo medido por uso). Quem cai no vazio tem o corpo devolvido ao último chão seguro (`Pawn.UltimoChaoSeguro`) para o time
chegar. Bots vão socorrer o eliminado (`Dupla.AliadoCaido` inclui mortos). Teste: jogador morto no vazio, parceiro ao lado →
8 s → viva, hp 30, escudo 0, varinha/Fogo, câmera de volta. **Falta:** HUD do progresso (`Partida.ProgressoReviver`),
"comprar" o reviver (Warzone) — sem economia no jogo ainda — e o nome: no código, "manopla" é a luva LENDÁRIA dupla;
a "manopla base" do Diretor é a Luva Comum (varinha). Vetável.

### >>> 25/09, fim de tarde (4): a ilha de 4,8 km é JOGÁVEL no editor — subterrâneo no Unity, zona §12.1, loot, minimapa

**Medido na 2ª partida automática (editor):** cronômetro 1.800 s, 93 itens de loot (100 varinhas tentadas + 1 cajado por
região), zona 2.200 → 1.800 → 1.000 → 450 → 120 → 0 m com centros em terra (última em (361, −34), leste do lago), minimapa
pintado (`MapaLogica.PintarMestre`: água pela regra do jogo, chão pelas camadas, sombra de relevo). Duração da partida:
`Balance.Match.DurationGrandeS` quando o raio de terra > `Zona.RAIO_GRANDE` (1.000 m); os testes da ilha pequena não mudam.
**Subterrâneo no Unity:** `IlhaMestre.Subterraneo.cs` constrói as MESMAS cascas do Blender a partir do JSON (túneis, salões,
poços com escada), com MeshCollider e material dos dois lados; `SetHoles` abre 649 células nas bocas; uma luz por salão
(cores do §8.3; túneis escuros de propósito — 140 luzes não cabem no celular, cristais emissivos depois). O `Pawn` não puxa
para a superfície quem está numa caverna (`Ilha.Subterraneo`). **Base militar:** perímetro 300 × 250 m (a cerca de 380 × 300
passava da falésia; o platô de 150 m vai até x 1.400 / y −1.475).
**Armadilhas do Play pelo MCP:** sem foco o editor congela o Play (`Application.runInBackground = true` por execute_code);
NUNCA ff em main nem focar o Unity durante um teste em Play (o auto-refresh recompila e o domain reload apaga a partida).
**TRIO (25/09, medido):** `Balance.Match.TamanhoDoTime = 3`, `TimesInimigos = 5` (`DuplasInimigas` virou alias) →
`Montagem.Times(times, tamanho)`: líder + seguidores em lados alternados, e os 2 parceiros do jogador POR ÚLTIMO
(`Main.Bots[0]` segue inimigo). `Main.Parceiro` = o 1º parceiro (ping, marca azul, cartão); o 2º só joga. Partida ao vivo:
18 corpos, 6 times, 2 parceiros pousaram a ~70 m e em 20 s estavam a 5 e 12 m do jogador; 59 fps; 525 testes EditMode
passando. Textos: "TRIOS {0}", "ÚLTIMO TRIO DE PÉ", modo "TRIO". **Limites conhecidos (vetáveis):** o ping fala só com o 1º parceiro (a marca azul e o ponto no minimapa
já cobrem os DOIS — `Partida.ParceiroVivo(k)`, medido em partida 25/09); cada bot foca o alvo de UM aliado; para 40–60 corpos medir a percepção dos bots (O(n²)) no
aparelho antes de subir `TimesInimigos`.
**Kit por script, leva 2 (25/09):** 095 hangar militar (abóbada de zinco, frente aberta) e 080 torre de luz — a base
usa 2 hangares e 4 torres de luz; cristais emissivos nos túneis/salões (1 malha por região, sem luz real).
Leva 3: 098 torre industrial 72 m (treliça que afunila, tanque no topo, chaminé) e 071 guindaste (mastro 40 m, lança
34 m, estais, gancho) — `trelica()` é o helper reutilizável. A réplica no Blender (v002) NÃO tem as peças novas: refazer
pelo caminho ExportarIlhaMestre → ilha_para_blender.py quando precisar.
Leva 4 (templo): 046 escadaria (20 degraus, muretas), 026 arco (9 aduelas), 014 pilar quebrado, 013 muro arruinado,
043 parapeito — R03 usa escadaria oeste, 4 arcos nas entradas, 12 muros na borda, 36 parapeitos no terraço, pilares.
**Travessia inferior (R05):** rampas de pedra dos dois encontros (126 m) até Z 80, varanda na parede oeste até a boca U10,
ponte secundária de madeira só no vão (x 103–212 em y −1.220) com parapeitos — `Rampa()` é o helper (laje inclinada).
**S16 (anel exterior):** `arte/tools/rota_anel.py` acha o caminho de menor custo no relevo (Dijkstra em grade de 19 m,
custo 1 + 40·tan(declive), teto 22°, sem água) entre 8 pontos a ~80 % do raio; 13,6 km, 55 pontos colados em ROTAS.
Relevo regenerado (metas OK) e subterrâneo revalidado (rocha ≥ 12 m mantida). Achado: o anel corta o pátio da base (platô
plano, sem dano) e entra pelo norte do lago em vez de contornar — vetável; para forçar fora, subir RAIO em rota_anel.py.
**APK do dia:** `mobile-unity/Builds/testes/arkana-2026-09-25_1632.apk` (465 MB, 25 min, 0 erros) — ilha nova, trio,
subterrâneo, zona §12.1, minimapa, kit levas 2–4; NÃO tem o S16 (entrou em main depois do build). Buildado pelo editor
aberto: o diálogo "Active Input Handling: Both" parou o build 20 min até clicar Ignore (memória: unity-abre-o-checkout-central).
O build reescreve `Scenes/Main.unity` e `Settings/URP_Base.asset` (sombra 50→60) — ficaram fora do commit, com os ProjectSettings.
**Falta:** jogar o APK no Poco F4 (o Diretor), árvore gigante (orgânica: Tripo depois), réplica Blender v003 com as peças novas.

### >>> 25/09, tarde (3): a PARTIDA já cai do castelo na ilha nova + o subterrâneo (R10–R12) existe, medido, no Blender v002

**Jogo (validado no editor, 25/09):** `Main.UsarIlhaMestre = true` → `GarantirIlha` cria a `IlhaMestre` antes da `Ilha`;
`Ilha.Montar` vê a IlhaMestre e só cria o adaptador `RelevoMestre : IRelevo` (`Ilha.Chao`; `Relevo` fica nulo — minimapa,
vitrine e terreno reativo já tratam nulo). Castelo: `RotaDoCastelo(seed, IRelevo)` voa a 760 m por 75 s (vetável) quando o lado
> 1 km; névoa 4.000/14.000 e far 15.000 (o `NevoaDaCamera` empurra o far de toda câmera). **Medido:** partida automática pelo
MCP (`scratchpad/teste_queda.py`): castelo entra de fora, salto a 45 % em (−573, 596, −62), pouso em y = 139,7 com chão 139,6,
13 bots, 50 fps no editor, 0 erros (só o aviso "Nature/Soft Occlusion" das árvores). **Falta:** andar no Poco F4 (o APK ainda
não foi gerado com isto), zona/loot escalados (12 itens em 4,8 km é deserto), minimapa da ilha nova, nascimentos de trio.

**Subterrâneo:** `arte/tools/subterraneo.py` (traçado de todos os nós U01–U15 do doc §7, 28 túneis = 11,0 km, 15 salões/câmaras,
2 poços com escada em caracol) → `Resources/ilha-mestre-subterraneo.json` + cascas glTF + `buracos.png`. **Regras medidas
(exit 1 se falhar):** rocha ≥ 12 m sobre o teto fora dos 120 m de boca, declive ≤ 30 % (envoltória inferior 1D), nenhum túnel sob
o lago, todas as ligações do §7.2 presentes. Túneis U14–U10 e U12–U14 mergulham a ~19 m para passar SOB o rio (leito 38 m). Está no
`ARKANA_Ilha_Mestre_v002.blend` (coleções ARKANA_R10/R11/R12 dentro de REGIOES; bocas = atributo "buraco" + Geometry Nodes no
terreno; materiais com backface culling — de fora vê-se o interior). **Não está no Unity ainda** (o JSON está; falta o construtor
gerar as cascas e `SetHoles` nas 651 células). **Tripo3D:** só para qualidade das peças orgânicas — NÃO é necessário para validar.

### >>> 25/09, noite (2): a MESMA ilha também está no Blender — `arte/cenario/documento-mestre/ARKANA_Ilha_Mestre_v001.blend`

Montada ao vivo pelo MCP do Blender (o Diretor acompanhou): relevo (malha 1025² com DISPLACE do mapa de alturas), chão com a
mistura das 5 camadas do Unity, mar/lago/rio, sol do SO, 611 peças e 5.764 árvores (LOD1) como instâncias de coleção, 6.418
objetos, ~110 MB (fora do git, reproduzível). Caminho: `ExportarIlhaMestre` (Unity sem janela) → `arte/tools/ilha_para_blender.py`
(planta glTF por região) → montagem descrita no fim do mesmo script. **Conferido:** a vila vista de cima bate com a foto do Unity
(o glTFast nega o X; o importador do Blender deixa o mundo girado 180° em Z). **Uso:** modelar cavernas/túneis (R10–R12) encaixados
no relevo real; relevo esculpido no Blender tem de VOLTAR ao `ilha-mestre-altura.bytes` por script (fonte única). **Achado:** a
cerca leste da base passa da borda da falésia (perímetro 380 × 300 m maior que o platô) — corrigir no `IlhaMestre`.

### 25/09, noite: ilha do Documento Mestre com KIT real (casas, ponte de arcos, galpões, base) e estradas

**Kit por script** (`arte/tools/blender/kit_documento_mestre.py`, headless: `blender -b --factory-startup --python ... -- <NNN> <saida.glb> [previa.png]`):
21 peças no Unity (`Resources/mestre-NNN-*.glb`). Pequenas (027 caixa, 028 barril, 053 barraca, 058 mesa, 055 banco, 025 cerca, 073 barreira,
074 caixa militar, 062 cano, 064 tanque, 086 trilhos): procedural ASSADO em 1024 (cor × oclusão + normal). Arquitetura (015/016/103/109 casas,
093 ponte de arcos 300 m, 097 galpão, 078 alojamento, 079 comando, 076 torre de vigia): UV em METROS (1 u = 2 m) + 7 fotos CC0 que se repetem
(`arte/cenario/texturas/arq-*`, via `python arte/tools/texturas_terreno.py arq`), paredes com VÃOS reais (porta 1,6×2,5, janelas), escada de 16
degraus na casa de 2 andares. Toda peça sai com base no chão e centro em X/Y (assert no script). **Estradas:** rotas S01–S15 alisadas no relevo e
pintadas de terra; árvores/pedras fora delas. **Passeio:** Play na cena → `ExploradorMestre` (cápsula 1,80 m, 6/9 m/s, salto 1,2 m, F voa).
**Armadilhas novas:** comparar nós do Blender com `is` apaga o BSDF (wrapper muda a cada acesso); cilindro criado girado mantém o giro no objeto
(aplicar antes do join); a câmera de prévia corta em 100 m; a amostra de altura GRUDADA na borda do mapa fazia os cabos pintarem uma cruz "rasa"
no mar inteiro (fora do mapa = mar fundo). **Ainda blockout:** templo (terraços/torre), torre industrial, guindastes, hangares, torre de observação,
árvore gigante, entradas U, travessia inferior Z80 sem rampas. **Pago (espera o Diretor):** peças orgânicas (Meshy/Higgsfield: conta gratuita,
API bloqueada). **Falta ao jogo:** integrar a ilha nova ao `Main` (castelo/queda, zona, bots, loot, início por time de 3), subterrâneo, anel S16.

### 25/09, tarde: a BASE da ilha do Documento Mestre existe no Unity (cena IlhaMestre)

**Abrir:** `mobile-unity/Assets/_Arkana/Scenes/IlhaMestre.unity` (o componente `IlhaMestre` reconstrói tudo em ~10–15 s; menu de
contexto "Reconstruir"). **Relevo:** `python arte/tools/ilha_mestre.py [pasta_previa]` gera `Resources/ilha-mestre-altura.bytes`
(2049², uint16, −40..660 m) + `ilha-mestre.json` e MEDE (doc §18), saindo com erro fora da meta. Medido: extensão 4781×4383 m,
terra 14,25 km², acessível 10,83 km², maior componente 95,8%, borda do lago 105,8 > 105, cume 602 m, rio só desce, ponte L–O
com encontros a 126 m, rio a 47,8 m sob ela. **Unity:** 16 Terrain nativos (4×4 de 513), 5 camadas CC0 do Poly Haven
RECOLORIDAS na paleta das concepts (`arte/tools/texturas_terreno.py`), mar/lago/rio no `ArkanaAgua`, céu `ArkanaCeu`, sol da
tarde do SO, ~9 mil árvores do kit (35/36/37/51) como instâncias do Terrain, 260 pedras em grupos, blockout das 12 regiões
(templo, vila 30 casas, ponte L–O + travessia Z80, acampamento com a caixa 027, indústria, base, torre 58 m, árvore gigante,
entradas U). **Armadilhas:** o namespace `Arkana.Terrain` esconde `UnityEngine.Terrain` (qualificar); textura CC0 pelo NOME
engana (leafy_grass é bege, rocky_terrain_02 verde) — medir a cor; lago recortado no platô virava buraco com parede de 20 m
(margem em rampa 1,0→1,55); câmeras DontSave não aparecem para o `manage_camera` (usar a Main Camera). **Falta (base → jogo):**
integrar ao jogo (castelo/queda, zona, bots, loot usam a `Ilha` de 600 m), peças do kit substituindo blockout, subterrâneo,
rotas S01–S16 físicas, pontos de início por time, linhas claras em cruz no mar (shader de água na origem).

### 25/09: nova visão da ilha (Documento Mestre, 4,8 km) + Unity ao vivo pelo MCP

**Decisões do Diretor (25/09) — substituem as anteriores sobre a ilha:**
- A ilha segue o **Documento Mestre** (`C:\Users\VINICIUS\Downloads\ARKANA_Documento_Mestre_Blender.md`, v1.0): **4.800 × 4.400 m**,
  12 regiões, subterrâneo; **Zona Industrial e Base Militar entram no mundo**. Substitui a ilha de 600 m e o plano de 2,4 km (27/08).
- Referência de formato: **PUBG Mobile**. **Times de 3**, média de **40–60 personagens** (13–20 times) → início por TIME, 300 m entre times.
- Tamanho **não** se decide medindo FPS (o mapa inteiro só é visto de cima; em jogo a câmera 3ª pessoa desenha só a tela).
  Desempenho vira engenharia: carregar a ilha por partes e simplificar o que está longe (o Unity hoje NÃO tem isso: malha única).
- **O máximo possível direto no Unity** (Terrain, água, vegetação, luz, montagem, medições). **Blender só** para o que o Unity não faz:
  cavernas/túneis (Terrain não tem teto), prédios, ponte, árvore gigante, kit modular. A regra §21.13 do doc ("não integrar com engine") caiu.
- As concept arts virão do Diretor para o projeto; olhar antes de modelar cada região.

**Verificação do doc (24–25/09, 3 raias + script):** a conta fecha (13/13, rio desce, nenhum túnel sob o lago, 134 posições cabem a 300 m),
mas há erros de geometria a corrigir na construção: **ponte N–S corre PARALELA ao rio** (5,7°; 231 de 300 m do tabuleiro sobre o rio) →
girar para L–O, encontros (50,−1050) e (350,−1050); travessia inferior não cruza o rio → 120–220 m ao SUL da ponte; túnel U14–U10 passa no
vazio do cânion (piso 65, rio 44) → mergulhar ~40 m; U12–U14 rente ao leito (0,9 m de rocha) → piso ~20; acampamento sem rota para a floresta.
**Em aberto (perguntar):** como a Sintonia (desenhada para DUPLA) funciona em trio; se o doc entra em `design/cenario/` como fonte da verdade.

**MCP for Unity instalado e provado (25/09):** pacote `com.coplaydev.unity-mcp` v10.2.0 (MIT) no `manifest.json`; servidor HTTP local
`127.0.0.1:8080/mcp` liga sozinho ao abrir o Unity (EditorPrefs AutoStartOnLoad); telemetria desligada (env `UNITY_MCP_DISABLE_TELEMETRY`
+ EditorPrefs). 48 ferramentas; provado: foto da Scene View devolvida à IA e `execute_code` (C#) rodando no editor. O próprio pacote
registra o Claude Code em escopo LOCAL do repositório (remove o de usuário) — as ferramentas só aparecem numa sessão NOVA do Claude Code.
Armadilhas: (1) o Unity só abre com o **Unity Hub rodando** (licença; sem ele sai com 198); (2) a foto é gravada em `Assets/Screenshots`
antes de voltar (ignorado no git); (3) editor aberto trava o projeto para o batchmode da esteira — testar `manage_build` de dentro do editor.
Plugin oficial da Unity para o Claude Code (29 skills): cartão oferecido ao Diretor, instalação é um clique dele.

### >>> COMECE POR AQUI — 23/09: a esteira CODAR → BUILD → EMULADOR → TESTAR → LOG existe e passou numa partida inteira

**Auditoria técnica** (arquivo único `ARKANA_AUDITORIA_TECNICA_ANDROID.md` na raiz, 31 seções + 4 anexos): o retrato da máquina, do
projeto e do git. Achado P0 resolvido no mesmo dia: `main` estava **93 commits e 729 MB de LFS à frente do GitHub** desde 09/09 —
push feito (`origin/main` = `0a94c2f` antes desta leva). Atenção: em repo com LFS, `git push --dry-run` NÃO é seco — o hook sobe os objetos.

**Emulador (funciona, sem Android Studio):** `sdkmanager` do SDK avulso instalou `emulator` 37.1.11 + `system-images;android-35;google_apis;x86_64`;
AVD `arkana_api35` (2400×1080 paisagem, 2 GB, GPU do host) boota em 50 s com **WHPX** — a auditoria da manhã tinha deduzido "sem
aceleração" do `InstallState`, mas `emulator -accel-check` diz "WHPX usable"; `mobile-unity/habilitar_whpx.bat` ficou de contingência
(admin + reinício). **Regra: Unity e emulador nunca juntos** (8 GB). O emulador NÃO mede FPS (GPU do PC); serve para instalar, abrir,
jogar sem dedo, achar crash e ler log quando não há cabo. A régua continua o Poco F4.

**Variante de build** `Arkana.EditorTools.Build.AndroidEmulador` (`Build.cs`): ARM64+x86_64, GLES3 fixo, Development, saída
`Builds/arkana-emulador.apk` (274 MB, 8,5 min); o `finally` devolve o `ProjectSettings.asset` byte a byte à produção (que segue ARM64-only,
release). **Esteira** `mobile-unity/emulador.ps1 [-Build] [-Auto partida] [-Espera s] [-Bancada]`: adb (o do SDK avulso, 37.0.1, unificado
com `kill-server`) → `install -r` → `am start` com os extras de sempre → espera → screencap + logcat → PASS/FAIL em `Logs/emulador/<data>/`.

**Rodadas de 23/09 no AVD:** 1ª FAIL (Android-ism: o aviso "Viewing full screen / Got it" tira o foco e o Unity pausa a 0 quadros — a
esteira passou a marcar `immersive_mode_confirmations confirmed`); 2ª PASS 150 s; 3ª **PASS a partida inteira** (570 s, fim da partida
"DERROTA #2 de 7 duplas", Selo, MENU → título → JOGAR → 2ª partida); toques por `adb shell input` provaram andar, TÁTICA (parede de fogo,
recarga) e SALTO; o tiro por toque ficou inconclusivo no quadro. **Bug que só o emulador mostrou:** a `BancadaDeCortes` restaurava o SSAO
como LIGADO, mas desde `9749474` ele está desligado no asset e o build nem leva os recursos — 50 mil linhas "Couldn't find the required
resources for the ScreenSpaceAmbientOcclusion" numa partida, e a linha `base` da bancada media com esse ruído. Corrigido: guarda e
restaura o estado real (`_ssaoLigado`). **Conferir a próxima bancada no Poco F4** — as medições feitas com bancada depois de 21/09 tarde
carregavam esse erro por quadro.

**Blender:** 5.2.1 instalado; **não existe MCP nem ponte** além de `blender.exe -b --python` (5,4 s de partida, funciona). Os 20 magos já
vêm rigados (28 ossos, pesos, 13–15 takes) — rig manual não economiza crédito nos magos atuais. `otimizar.py`/`desneon.py` compatíveis com
o 5.2 por API; `fundir_animacoes.py` tem risco no 5.x (slotted actions) e carrega a proibição de 27/08. Decisão: sem MCP; Blender headless
só para o determinístico (retoque de textura, escala/eixo, retarget entre magos, clipe pontual); Meshy só para corpo novo ou gesto fora da biblioteca.

**24/09 — a esteira no aparelho e o MCP do Blender.** Reinício consolidou o WHPX (recurso agora consta habilitado); AVD passou de novo (PASS 90 s). `emulador.ps1` ganhou acordar tela + `wm dismiss-keyguard` (com PIN o Diretor destrava). **Bancada no Poco F4 com a correção do SSAO (APK de produção `arkana-2026-09-24_1111`, 0 erros): base 39,8 FPS (era 34,6 em 21/09 com o ruído do erro por quadro), sem_ssao 39,8, sombra 2 cascatas 39,7, sem_sombra 47,6, escala 0,70 48,6.** Achado: o corte `sem_hdr` travou 30 s (pior quadro 30.108 ms — desligar HDR em runtime recria os alvos de render); os cortes seguintes ficaram contaminados porque o Diretor assumiu o controle — repetir sem dedo. **Blender + Claude Code:** instalado `uv` (winget), add-on `mcp-for-blender` 2.0.4 (Ahuja; ex-`blender-mcp`) em `%APPDATA%\Blender Foundation\Blender\5.2\scripts\addons\blender_mcp.py`, ativado nas preferências; servidor MCP registrado no Claude Code (escopo usuário, `BLENDER_MCP_SAFE_MODE=1`, status Connected); ponta a ponta provado: 32 tools, `execute_blender_code` rodou no Blender 5.2.1. Para usar: abrir o Blender e clicar "Start MCP Server" (painel N → MCP for Blender, porta 9876, só localhost); ou `blender.exe --python <script que chama bpy.ops.blendermcp.start_server()>`. O headless `blender -b --python` continua sendo o caminho reproduzível para script.

**24/09, tarde — decisão do Diretor sobre os personagens:** "é muito nítido que está péssimo a qualidade"; escolheu o item 3 (melhorar o modelo) e autorizou **criar tudo no Blender sem crédito da Meshy**. Revisão dos 20 magos (Blender headless, câmera do jogo): 19 estão no padrão (28 ossos, altura = ficha, ~31 k tris, 12 clipes); **o Vex é o único quebrado, e é o RIG** — a Meshy pôs a junta `Head` no bico da máscara (18,5 cm à frente da coluna), o `neck` virou diagonal de 20 cm (10,6 % da altura; normal 4 %) e o crânio ficou preso a `head_end`/ombros. Frente A: re-rig da cabeça do Vex no Blender (cópia, FBX v2 com os 13 takes). Frente B: prova de conceito de mago criado do zero no Blender, no mesmo esqueleto da Meshy, **modular** (corpo base sem roupa + casaco/capuz/luvas/botas/cinto como malhas separadas no mesmo Armature, atlas compartilhado, faces cobertas escondidas — regra nova do Diretor: peças separadas dão qualidade e viram cosméticos da G6). Se o render Meshy × Blender convencer, esse vira o caminho para os 20. **Resultado da Frente B (24/09):** o PIPELINE fecha (script de 18 s gera mago modular de 6 peças/7,9 k tris no esqueleto da Pyra, 15 takes tocando, atlas 2048², FBX igual ao padrão, reimport limpo; o `Mago.cs` já itera N malhas, só exige atlas único) — mas a ARTE não: sem rosto, roupa de primitivas, textura chapada = manequim. Sem escultura manual (corpo base + cabeça feitos UMA vez), o teto é esse. Decisão: Meshy segue como fonte de malha/textura; Blender conserta (rig, escala, retarget) e fabrica peças/cosméticos. **Frente A (Vex):** re-rig da cabeça feito e provado no Blender (neck 20 cm/68° → 7,9 cm/3°), mas o FBX exportado importa com todos os ossos em posição (0,0,0) no Unity (teste de altura mede 0,80 m) — causa real: **o Unity monta a pose padrão do prefab pelo PRIMEIRO AnimStack do FBX** (e o teste mede essa pose por `BakeMesh` em EditMode); o exportador do Blender gravou os takes em ordem alfabética e o primeiro virou o take "Planar" (corpo deitado). O v4 exporta os takes na ordem do original (Running primeiro) — portão 620/0. Regra para todo FBX editado no Blender: takes na mesma ordem do original, strips com `REPLACE`/`NOTHING` e frame fora das strips no export (senão a pose atual vai para os nós dos ossos). Scripts: `rerig_vex.py`, `fbx_nos.py`, `fbx_takes.py` (scratchpad de 24/09). Original guardado em `arte/personagens/09-vex/09-vex.meshy-original.fbx`.

**Achados e ideias do Diretor (24/09, jogando o APK no Poco F4 — VALIDAR com evidência antes de implementar; diagnóstico em andamento):** (1) andar para os lados: "não existe a animação, o botão sequer direciona para as laterais"; (2) "as miras não estão muito boas"; (3) melhorar animação e som das magias; (4) "o personagem está horrível, o pescoço parece imenso de comprido" (medir no Blender: modelo, rig, código ou câmera). **Diagnóstico feito no mesmo dia (2 raias, só leitura):**

| Achado | Veredito | Onde está (código) | Correção mínima (vetável) |
|---|---|---|---|
| Andar de lado | **procede em parte**: o vetor lateral existe (`Locomocao.cs:75-81,236-238`) — sem mira o mago VIRA e corre de frente (`Pawn.cs:371-386`); mirando, toca `Running` girado 90° com tronco torcido (`Pawn.cs:322-338`, `PoseMago.cs:396-402`, `Mago.cs:616-630`). **Nenhum dos 20 FBX tem take de strafe** (takes reais: Combat_Stance, Idle_02, Walking, Running, mage_soell_cast, Fall1, Swim_*, Prone_Reach_Help, Cautious_Crouch_Walk_Forward, Walk_Backward, Regular_Jump). GDD não decide. | `PoseMago.cs`, `Pawn.cs`, `Mago.cs` | take de strafe da biblioteca da Meshy (grátis, receita de 16/09; conferir se existe) + ~40 linhas e teste em `CharactersCorpoTests.cs:79-111`. Decisão do Diretor: corpo sempre de frente para a mira (strafe) ou virar para o rumo? |
| Mira | **procede** (3 causas de código): sensibilidade em **px** em vez de dp (`CameraTerceiraPessoa.cs:18,35,47-52` vs `Dp.cs:6` → ~71°/cm no Poco, 2,5× o previsto); o tiro arrastado **não converge no retículo** (`Player.cs:84`, sem raycast, cai ~2 m curto; o toque converge, `Player.cs:129-143`); retículo fixo que não acende sobre alvo (`Hud.cs:435-448`, `MarcasDeAlvo.cs:70`); segurar >220 ms sem arrastar cancela (`GestoDeDisparo.cs:76-77`); sem assistência (GDD §19.3). | `Player.cs`, `CameraTerceiraPessoa.cs`, `Hud.cs` | ~6 linhas sem asset: px→dp (`Player.cs:77`), arrasto pelo ponto do retículo (`Player.cs:84`), retículo acende (`Hud.cs:~1339`); assist leve opcional (~4 linhas). Medir com o Diretor "cm para meia volta" antes de fixar o slider. |
| Animação e som das magias | **procede**: tática e suprema **não movem o corpo** (só o tiro básico e o pegar pedem gesto: `Pawn.cs:444,531`; `KitRunner.UsarTatica/UsarSuprema` não); som genérico por elemento/kit ignorando o slug (`Sfx.cs:59-63,502-506,536-548`; `Bus.cs:80` nem carrega slug); VFX procedural sem textura (`VisualDosKits.cs:84-100`, projétil primitiva+trail `Main.cs:691-775`). `AUDIO.md:13-15` autoriza áudio externo com crédito. | `KitRunner`, `Sfx.cs`, `VisualDosKits.cs` | `Dono.Gesto("tatica")` nos dois verbos + take "Mage Spell Cast" (grátis); `Sfx.Clip` com `Resources.Load("sfx-<nome>")` e síntese de reserva (2 linhas) + `.ogg` CC0 em `Resources/sfx-*.ogg` + `CREDITS.md`; PNG de partícula antes de shader novo. |
| Pescoço "imenso" | **não procede no modelo/rig**: Neck 7,3 cm = 4,1 % da altura (Pyra, Tessa, Brok iguais), escala 1,0, não estica na animação; `Mago.LateUpdate` não toca `neck`/`Head`. **É a pose + a câmera**: o Idle é o clipe `Combat_Stance` (`PoseMago.cs:289`, escolha de 12/09 porque `Idle_02` ergue o braço) que dobra pescoço 46–48° e cabeça 49–52° para a frente, visto por uma câmera a 2,07 m e 17° para baixo (`CameraTerceiraPessoa.cs:23-25`). Render com a câmera exata do jogo: nuca ~10 % da figura no Combat vs ~3 % no Walking. Fator secundário do modelo: pescoço ~25 % mais fino que o real + coque de 8 cm. | `PoseMago.cs:289`, `Mago.cs:616`, `CameraTerceiraPessoa.cs:23-25` | **b1**: no Idle, girar `neck` e `Head` ~20° para trás no `LateUpdate` (padrão da torção, knob em `Balance.Anim`, ~12 linhas, vale para os 20) → foto no Poco; se ainda incomodar, `PITCH_PADRAO` 0,30→0,15 e/ou `ALTURA_PIVO` 1,85→1,45 (muda o game feel de 11/09). Reeditar a malha: não vale o risco. |

Prova visual do pescoço (render A/B com a câmera do jogo) e os relatórios completos ficaram no scratchpad da sessão de 24/09; o que importa está nas linhas acima. **Ideias para o GDD (decidir em `design/` antes):** cavernas e mudanças de ambiente na ilha (neve, lava, lama), dobrar o tamanho da ilha (custo de FPS: medir), castelo passando mais baixo, nomes das regiões visíveis do alto (definir os nomes), "física é importante ter" (o que ele quer dizer além do PhysX atual — perguntar). Ele também disse que validar jogando é importante e vai se envolver: separar rodada "sem dedo" (mede) da rodada dele (julga).

**Fila:** disparo por toque no AVD (prova com dois quadros ou log); portão vermelho para a bancada (hoje é MonoBehaviour sem teste);
repetir a bancada no Poco F4 com a correção; `SwappyDisplayManager couldn't find libgame.so` (1 linha E/, inofensiva, investigar);
keystore de release (G6); o resto da fila de 21/09 continua abaixo.

### >>> COMECE POR AQUI — 21/09, tarde: o chão do Poco F4 de 16 para 34 FPS (medido); gelo vira chão

**FPS resolvido pela medição (commit `9749474`), portão 620 testes, 0 falhas.** Três rodadas da `BancadaDeCortes` no aparelho:
o jogo inteiro dava 15,7–17 FPS no chão; os custos por quadro eram **sombra macia ALTA (~10 ms)**, **SSAO (~12 ms)** e a
resolução (a imagem é limitada por pixel: 70% da escala tira ~22 ms). Grama, água, nuvens, HDR e pós custam 0–2,6 ms cada
(ficam). **Entrou:** SSAO desligado (`URP_Base_Renderer`), macia BAIXA no asset e no sol (a dura mede igual e devolve os
degraus da foto 24), e a opção **Qualidade** das Configurações passou a mudar de verdade — antes os 3 níveis usavam o mesmo
asset e ela não fazia nada: **Alta** = o asset (100%, 4 cascatas de 4096, ~25 FPS), **Média (padrão, vetável)** = 85% +
2 cascatas de 2048 (**34,6 FPS, pior quadro 33 ms**, aparelho a 47 °C), **Baixa** = 70% (~40 FPS). Castelo/queda: 42–52.
No editor o perfil NÃO se aplica (o asset é o arquivo do projeto; mudar em runtime gravaria no disco): as fotos saem na Alta.
**A bancada ficou:** `--ei arkana_bancada 1 --es arkana_cortes "base,escala_070,..."` mede qualquer lista sem APK novo.

**Gelo (commit `b9831a0`):** cada célula congelada ganha colisor com o topo na lâmina (padrão do muro); quem nadava ali sobe;
derreteu, cai e nada; o tiro para no gelo. Teste PlayMode prova no Unity. **Falta:** a laje quase não se distingue da água na
foto 59 (parece andar sobre a água) — reforçar o visual do gelo (cor/opacidade/rachaduras no `VisualDoTerreno`).

**Teste com toque no aparelho (MIUI com "Depuração USB (segurança)" aceita `adb shell input`):** menu, Grimório (1/12 real:
"Muro de Pedra" acendeu numa partida), JOGAR, salto — o parceiro salta junto. Achados: a marca do parceiro cobre a mira quando
ele está bem à frente (no castelo); saltar no começo da rota cai no mar longe da ilha (decidir se o salto espera terra).

### 21/09, manhã: o Poco F4 mediu 17–20 FPS no chão; onda 18 fechada

**O aparelho voltou ao `adb` (21/09) e MEDIU** (APK `arkana-2026-09-19_1725`, partida automática em dupla, `arkana_fps 300`):
castelo/queda **36–40 FPS**; **no chão, 17–20 FPS a partida inteira** (pior quadro 66–165 ms); bateria a 44 °C. Em 12/09 eram 60 cravados — as ondas 5–17 (sombra 4096×4 cascatas + SSAO, `_Chao` em toda a ilha, 615 árvores, grama, mata, praia, céu) nunca tinham sido medidas. **O gargalo é a GPU:** com o jogo a 17 FPS, `UnityMain` usa 17% de um núcleo e o render 10% (`top -H`). O Diretor tinha mandado ignorar FPS ("o Poco aguenta"); **a medição diz que não aguenta** — é a regra dele de 25/08: FPS se mede, e jogo travando é jogo não funcionando. **Próximo passo, com o celular no USB:** `BancadaDeCortes` (commit `2d8510f`, já no APK `arkana-2026-09-21_1418`): `adb shell am start -n br.com.vstack.arkana/com.unity3d.player.UnityPlayerGameActivity --es arkana_auto partida --ei arkana_fps 300 --ei arkana_bancada 1` e `adb logcat -s Unity | grep "ARKANA BANCADA"` — 15 cortes de 10 s depois do pouso (sem SSAO, sombra 2 cascatas/2048, sem sombra, escala 0,85/0,70, sem HDR, sem pós, sem grama, sem vegetação, chão simples, sem água, sem nuvens, combinado, base de novo). Com a tabela, corta-se o que custa mais e rende menos. MIUI: com "Depuração USB (configurações de segurança)" ligada, `adb shell input tap/swipe` FUNCIONA (21/09) — dá para jogar pelo computador.

**Achado do aparelho (corrigido na 18D):** a marca azul do parceiro atrás da câmera grudava em cima do botão SUPREMA.

**Onda 18 (portão 613 testes, 0 falhas; fotos 55–58):**

| Raia | O que entrou |
|---|---|
| 18A | os 10 combos redesenhados: zona em grade que segue o chão desenhado, shader `ArkanaSintoniaZona` (material em `Resources/`, teste que o carrega), funil, lava, plasma violeta, nuvens de partícula, cristais da Meshy, nuvem com raio. Os "arcos brancos" eram a onda de choque plana |
| 18B | **Selo do Campeão** (§18.6): cartão de fim com carimbo, retrato, colocação por dupla, abates, dano, Sintonias, tempo vivo, elementos; canvas próprio (ordem 60), HUD de combate some. Compartilhar de verdade pede FileProvider no manifesto (não feito) |
| 18C | **Grimório de Descobertas** (§18.3, A18): 12 páginas sem vantagem (6 de terreno, 6 de dupla/desfecho), bit por página em `grimorio.v1`, aviso em partida, livro no menu (ELENCO \| GRIMÓRIO). "Ponte de Gelo" virou "Lago Congelado" e "Fumaçar o Esconderijo" virou "Vento no Fogo" (vetável) |
| 18D | **Ping de Sintonia** (§18.7): tocar no anel pronto = COMBO? no inimigo da mira; o parceiro aceita em 0,5 s, prende o foco 12 s e escolhe o elemento que funde, ou recusa "SEM COMBO" |
| coordenador | dica falsa "fogo na grama alta revela escondidos" (não existe) trocada pela da Sintonia; dica do anel tocável |

**Em andamento:** 18E — **o gelo não sustentava ninguém** (o `TerrenoReativo` diz "congelada vira ROTA", mas o corpo nadava por baixo): colisor por célula congelada no `VisualDoTerreno`, como o muro.

**Fila:** a tabela da bancada e os cortes de GPU (prioridade 1 quando o celular voltar); Presságios (escrever a spec no GDD antes); Espírito Errante + altares; números locais dos combos para a `Balance`; cegueira do vapor/areia para bots; autor no `Bus.TerrainChanged` (kit e combo não acendem página de terreno); botão compartilhar do cartão; bots usando kit (Diretor).

### 19/09: fase G4 — a dupla e a Sintonia (o pilar do jogo) entraram no celular

**Avaliação das fases (19/09, pedida pelo Diretor):** G0–G2 fechadas; G3 praticamente fechada (20 magos reais, kits, praia, mata, céu, menu). **G4 é a fase aberta**: tinha zona, 12 bots e derrubado, mas **faltava a dupla e a Sintonia** — a "invenção que vão copiar de nós" (GDD §9) não existia no celular. G5 (multijogador) exige servidor e contas: decisão do Diretor, e só depois da G4 divertida contra bots. G6 (loja) é ato do Diretor. O Diretor mandou **ignorar a medição de FPS** ("o Poco F4 vai aguentar").

**Onda 17 (4 raias paralelas contra um contrato escrito antes — `scratchpad/onda17-contrato.md`; portão 562 testes, 0 falhas; 105 mutantes mortos):**

| Raia | O que entrou | Por quê |
|---|---|---|
| 17A Times | `Combat.MesmoTime` é a ÚNICA pergunta "é aliado?" (anti-farm, derrubado, 12 kits, Grupo D); `EhPlayer` = só "é o humano". Vitória por dupla (PONTE A6), `Partida.PlayerFora`/`ParceiroVivo`/`TimesVivos` | havia ~15 cópias de `a.EhPlayer && b.EhPlayer`; dupla com bot exigia um lugar só |
| 17B Sintonia | `Core/Sintonia.cs`: aliados, elementos diferentes, mesmo ponto (7 m) em 1,5 s → 1 s de canalização → combo ×2,35; recarga 24 s cobrada no INÍCIO, devolvida a quem fica de pé se o outro cai (PONTE A5); gancho único no `Projetil.Impacto` | GDD §9; números = variante A do Roblox, **NÃO validados** (A19) — o Diretor julga no aparelho |
| 17C Efeitos | `SintoniaEfeitos.cs` (os 10 combos no mundo com receita de terreno do Roblox e counters: Água desfaz tornado/magma/areia, Vento desfaz vapor...) + `VisualDaSintonia.cs` | dano só em inimigo (fonte = A); o que vira chão pega todo mundo (A4) |
| 17D Dupla | Menu MODO **DUPLA** (padrão, VETÁVEL) \| SOLO; parceiro bot que acompanha a 4–8 m, foca o seu alvo, prefere outro elemento e te levanta; 6 duplas inimigas saltam juntas; câmera de espectador; HUD: DUPLAS n, faixa SINTONIA, anel de recarga, marca azul do parceiro, minimapa | GDD §9: o solo é pareado com um parceiro bot que entra na canalização real |
| coordenador | tiro direto ATRAVESSA o aliado (A4); **time inteiro no chão sai no próximo tique** (antes esperava 30 s); pontes apagadas; dica da Config vira "como fazer o combo"; testes de contagem de bots seguem o modo; foto 29 com aliado de pé; **tronco dentro da rocha**: a Ilha monta as ruínas ANTES da vegetação e a mata refaz o sorteio do kit com as mesmas Pegadas (o diag ainda acusa 1 tronco a 9,0 m do centro de uma rocha de 11,5 m × escala — o limiar do diag usa o raio sem escala; conferir visualmente) | |

**Fotos:** 53-sintonia-* (canalização e os 10 combos), 54-dupla-* (menu, parceiro, faixa, quebrada, espectador). **Veredito do coordenador sobre a 53-combos:** a canalização e a faixa estão boas; os combos de ZONA estão amadores (discos chapados e opacos que cortam a ladeira, plasma = esfera branca, nuvem/tornado fracos, arcos brancos sobrando) → **onda 18A refaz o visual**.

**Onda 18 (em andamento, contrato `scratchpad/onda18-contrato.md`; `Textos` virou `partial` para cada raia ter o seu `Textos.<Raia>.cs`):** 18A acabamento visual dos 10 combos (zona que segue o relevo, sem opaco); 18B **Selo do Campeão** (cartão de fim de partida, §18.6); 18C **Grimório de Descobertas** (12 páginas sem vantagem, §18.3 + PONTE A18); 18D **Ping de Sintonia** (tocar no anel = "COMBO?", parceiro aceita e vai com o elemento complementar, §18.7).

**Fila depois da 18:** Presságios (§18.4) — o GDD só tem 3 exemplos e mexem em números de combate: **escrever a spec no GDD antes** (regra do `design/00-LEIA.md`); Espírito Errante + altares de retorno (§18.6, PONTE A8/A9); números locais dos combos (`SintoniaEfeitos`) para a `Balance.Sintonia`; cegueira do vapor/areia para os bots (hoje só visual); bots usando kit (decisão do Diretor); capim da duna mais leve (descartado enquanto FPS não importa).

### 16/09: o corpo do mago (a queixa do Diretor) e o logo ligado

**O Diretor jogou o APK de 13/09 e reclamou (16/09):** "salto do personagem, a corrida para trás, ao pegar a luva parece que ela é imensa, chega a cobrir a câmera toda"; perguntou se valia passar os personagens pelo Blender. **Diagnóstico com foto e número antes de mexer** (`Foto_Diag_LuvaPuloTras`, foto 46): os três eram de CÓDIGO, não de modelo — o Blender para personagem foi descartado (já tinha falhado em 27/08).

| Onda | Causa raiz medida | O que entrou | Foto |
|---|---|---|---|
| 15A luva | `Mago.MaoDireita` no externo é o osso `RightHand` com **lossyScale 100** (a Armature do FBX da Meshy); as primitivas de 0,1 m viravam cubos de 10–17 m | `LuvaVisual`: soquete filho do osso que cancela a escala, orientado pelo bind (dedos +Y, polegar = frente); a luva é o MESMO `.glb` da Meshy que aparece no chão (varinha e cajado são luvas esquerdas: espelhados), 1,3× o antebraço, teto 0,35 m; gema acesa na cor do elemento; sem `.glb`, primitivas no soquete | 47-luva-* |
| 15B pulo | não existia clipe de pulo: subia em Idle e piscava `Fall1` 0,2 s | **Salto Regular** da biblioteca do site (take `Regular_Jump`) aplicado nos 20 magos (grátis) e baixado de novo (`importar_mago.ps1`); `Clipe.Pular` começa na decolagem (0,50 s do take), o ar estica ao voo previsto (2·Vy/g), `Pousar` ao tocar o chão (0,5 s; correndo mistura direto); fases medidas em runtime por `SampleAnimation` (cache por clipe); o deslocamento do quadril sai na importação (`OnPostprocessAnimation`); sem o take, segura o quadro 12 do Running | 48-pulo-* |
| 15B recuo | mirando, o corpo olha a mira e toca a corrida de frente (moonwalk, dif=180°) | **Andar para trás** (`Walk_Backward`) nos 20; acima de 105° entre mira e movimento vira `AndarTras` com **recuo a 0,7×** (`Balance.Move.BackpedalMult` — o Diretor pode vetar); até 105° as pernas giram para o rumo e o tronco torce até 75° para a mira (`LateUpdate`, Spine02/01/Spine); sem o take, a corrida ao contrário | 48-tras-*, 48-lado-* |
| 15B tiro | o `cast` congelava as pernas correndo | cast correndo = camada só do tronco (`AddMixingTransform` em Spine02); parado, corpo inteiro; `CastFired`/`CastFireT` iguais | 48-atirando-* |
| logo | o `Logo : MaskableGraphic` nascia **sem `CanvasRenderer`** (o Graphic do UGUI 2 não exige; `Image` sim) — malha montada, nada desenhado | `[RequireComponent(typeof(CanvasRenderer))]`; religado no título (300 dp), menu (200 dp) e carregamento (140 dp) | 45-logo-*, 42-carregando |

**Receita para clipe novo em todos os magos (site da Meshy, grátis):** Animar → filtro "animados" (ícone de boneco) → abrir o rig de 12/09 pelo nome inglês da Meshy (Emberarm Vanguard = Pyra, Voidreaper = Ceifadora, Azure Wraith = Véu, Ravenborn = Corvus, Occultist = Corvomante, Amethyst Butterfly = Olho-de-Éter, Starlight Alchemist = Vitalis, Violet Aristocrat = Ilusionista, Toxic Alchemist = Vex, Sparkthread = Tessa, Starborn Elven = Aelion, Violet Shadowblade = Umbra, Ironforge = Brok, Stormhide = Gromm, Azure Tide = Maris, Gizmo = Fizz, Verdant Blossom = Sylva, Emberstone Colossus = Basalto, Crimson Nocturne = Noctus, Azure Sparkwing = Pip) → Biblioteca → buscar → Adicionar (≈10 s) → Baixar: fbx, **MeshyRig** (não Mixamo!), Todos Adicionados, Arquivo único, 30 fps → `importar_mago.ps1 <zip> NN-slug`. Conferir o slug pela md5 da `texture_0.png` contra `NN-slug-cor.png` (`scratchpad\elenco-zips\conferir.ps1`). Há uma "Pyra (modelo de jogo)" de 26/08 no filtro que NÃO é a do jogo.

**Portão:** 493 testes, 0 falhas. **APK** `arkana-2026-09-16_1023.apk` (221 MB) na pasta de testes — o Diretor sobe ele mesmo no Drive ("JOGOS EM DESENVOLVIMENTO").

**16/09, tarde — entrou em `main` (portão 509 testes, 0 falhas):**

| O quê | Causa / decisão | Foto |
|---|---|---|
| **Botões do menu** (onda 16A) | a "oliva" era a Borda: FILHO esticado do botão, e o uGUI pinta filho por cima do pai. Agora `PlacaBotao` (assada em código, 9-slice): placa escura chanfrada, moldura dourada em degradê, gemas de losango, rótulo `OuroClaro` com espaçamento (`Letreiro`); **JOGAR** em ouro cheio com aura que respira; afunda no toque (`BotaoMenu`). Vale para todo `Estilo.Botao` (Config, etc.) | 50-menu-*, 50-config |
| **Configurações cortadas** | a `Lista` nascia com o `sizeDelta` padrão (100×100) e, esticada, ficava 100 px mais larga que a máscara | 50-config |
| **Tiro por toque** | o `Player` largava a mira no quadro seguinte: agora segura `MIRA_APOS_TIRO_S` (0,55 s) e o corpo vira para o retículo | 52-tiro-toque |
| **Reflexo da Ilusionista** | o clone não recebia o `run` ao sair do recuo e seguia andando para trás | 39-kit-08 |
| **A mata** (onda 14A, `Mata.cs`) | 16 cristais arcanos, 32 troncos com musgo, 112 cachos de cogumelo luminoso, 248 samambaias, no molde da `Praia` (instanciado por bloco, LOD, corte). Foge das pegadas do kit (`PegadasDoKit` refaz o plantio puro do `KitCenario`). **Tronco:** o de 1,5 K lia como musgo em blocos de perto → o remesh inteiro de 3,1 K é o LOD0 e o de 1,5 K o LOD1 (25 m). **Brilho:** cristal e cogumelo usam o molde-asset `ArkanaMeshyBrilhoInstancing.mat` (Lit + instancing + `_EMISSION`) — keyword só em runtime some no APK com "Strip Unused" (teste `MoldeDoBrilho_AssetComEmissaoEInstancing`) | 51-mata-* |

**Lições:** RectTransform novo nasce com `sizeDelta` 100×100 — todo nó esticado precisa zerar; `Graphic` próprio precisa de `[RequireComponent(typeof(CanvasRenderer))]`; keyword de shader ligada só em runtime precisa de um material-asset que a peça, senão o build a descarta; **nunca editar `.cs` com o Unity rodando** (aconteceu uma vez nesta tarde: o portão foi repetido).

**Na fila:** FPS no Poco F4 (sem aparelho no `adb` em 16/09); `48-capim-duna-lod1.glb` ainda com 1,28 K tris (remesh de 400 no site); a mata tem 1 tronco encostando na rocha de basalto do kit (`KIT x MATA` no diag da foto 51); bots usando kit (decisão do Diretor).

### 13/09 (pausa às ~03h30): costa, céu, praia; o logo novo espera conserto

**Próximo passo, na ordem (retomada):**
1. **Consertar o logo** (`Scripts/Menu/Logo.cs`, onda 14B, commit `c0d6ea9`). A marca foi desenhada em código (glifos próprios estilo Cinzel/Trajan, ouro com bisel, contorno grosso, K em raio com halo azul, reflexo que corre). A prévia em Python ficou profissional, e a textura gerada pelo C# no mono bate com ela em 1/255. **No Unity, porém, a marca sai INVISÍVEL** no título, no menu e no carregamento: o espaço do layout existe, não há exceção no log e a foto passa. Já descartado: a textura destruída pelo `UnloadUnusedAssets` (o `hideFlags` já está no arquivo e continuou invisível). Por isso o `Menu.Wordmark` voltou para a fonte antiga. Para religar, use a versão do agente: `Menu.Wordmark(pai, larguraDp, brilho)` devolvendo um `Logo`, título 300 dp, menu 200 dp, carregamento 140 dp parada, e a foto `Foto_Logo_TituloEMenu` → `45-logo-*`. Próxima suspeita: um teste PlayMode que imprima `rect`, `canvasRenderer.GetMaterial()`, `mainTexture` e o número de vértices da malha gerada, e compare com uma `RawImage` usando a mesma textura.
2. **Onda 14A — a mata** (cristal arcano, tronco com musgo, cogumelos luminosos, samambaia). Os GLBs já otimizados estão em `arte/cenario/ilha-fraturada/_originais-3d/50–53-*.glb`:
   - cristal: 1,2 K tris, 1,5 m;
   - tronco: 1,5 K;
   - cogumelos: 1,2 K + LOD1 de 300;
   - samambaia: 1,4 K. O LOD1 dela não desceu (1,2 K), porque as folhas são ilhas soltas.

   O musgo do tronco é verde-limão: tinja no material ou passe no `arte/tools/blender/desneon.py`. Plante pelo `Vegetacao.cs`, como a `Praia.cs` faz: instanciado, com LOD e manchas. O cristal pede emissão no material (brilho arcano).
3. **Medir FPS no Poco F4** com o APK mais novo (pasta de testes de sempre). Ainda não houve aparelho no `adb`. Custos novos que entraram nesta madrugada: o céu (~350–650 instruções na banda de nuvens), a costa (+20 slots no mar), a praia (~40–60 K tris na tela) e as 360 pedrinhas do cume. Os KNOBs de corte continuam os de baixo, mais `_Cobertura` do céu e `SeixosDoCumeRef`.
4. Pendências anotadas:
   - o `48-capim-duna-lod1.glb` tem 1,28 K tris (não desceu): reexportar mais leve;
   - a rocha da costa tem o topo de areia laranja forte ao pôr do sol (tinta em `Praia.cs`);
   - os botões do menu ainda são placas chapadas cor de oliva (próximo alvo visual, junto com o logo).

**Entrou em `main` depois das 03h (portão 461 testes, 0 falhas):**

| Onda | O que se vê | Arquivos | Foto |
|---|---|---|---|
| 13A | Costa do mar: faixa rasa turquesa, espuma que respira na linha d'água (~6,7 s por onda) e linha rala ao largo. Textura 192² assada do relevo, só no material do mar; lago e alagado iguais | ArkanaAgua.shader, Ilha | 42-costa-* |
| 13B | Céu: banda de cúmulos no horizonte (topo em couve-flor, base reta, 3 tons toon, borda de luz dourada do lado do sol) e cirros no alto. **O castelo da Meshy passou pelo `KitCenario.Domado`**: tinha `metallicFactor` 1 e espelhava o zênite, então saiu azul inteiro com o céu limpo | ArkanaCeu.shader, Castelo, KitCenario | 43-ceu-*, 34-* |
| 13C | Praia pela Meshy: 2 barcos naufragados, 20 troncos à deriva, 32 rochas com estrela-do-mar (9 com o pé no mar), 165 touceiras de capim de duna. No cume, as 1.000 lascas chapadas viram 360 pedrinhas da Meshy | Praia (novo), Vegetacao, Grama, 45–49-*.glb | 44-* |

**Entrou em `main` na madrugada de 13/09 (portão 441 testes, 0 falhas):**

| Onda | O que se vê | Arquivos | Foto |
|---|---|---|---|
| 10A | Colunas e blocos das Ruínas viram modelos da Meshy (coluna canelada, bloco rachado, coluna tombada): 59 peças em 2 malhas, 64,6 K tris | Ruinas, 38/39-*.glb | 40-ruinas-* |
| 11 | Muro de terra (sobe do chão em 0,3 s, tomba ao quebrar), placas do Monólito do Basalto, torreta e Megabobina do Fizz (sucata tomba e afunda), Runa-Escudo do Brok — sai a caixa cinza. Carregador `PecaDaMeshy` no `VisualDoTerreno`; sem o GLB, volta a primitiva | VisualDoTerreno, VisualDosKits.GrupoC/D, 40–44-*.glb | 41-pecas-* |
| 12A | Verbos de motor: `Teleportar` com pouso seguro (nunca no mar, em copa ou dentro do morro), `Impulso` na distância exata, invisível/penumbra/sem passos de verdade contra o bot (só vê a 2,5 m), fator de pulo, vida base na `IdentidadeMago` (Pip 55). Ceifadora, Umbra (8 m por esquiva), Noctus, Pip, Fizz e Ilusionista já usam | Pawn, Locomocao, Bot, IConjurador, IdentidadeMago, 7 kits | 39-kit-* |
| 12B | Tela de carregamento: o JOGAR cobre tudo no mesmo quadro (vitrine do mago, nome, título, elemento, dica, barra real); a partida monta em fatias de 12 ms. Editor: 7 quadros, 254 ms, pior quadro 151 ms (medido em `Logs/carregamento-*.txt`) | Main, TelaDeCarregamento (novo), Textos, Menu | 42-carregando |

**Lições da madrugada:**
- **Líquen/musgo verde-limão:** a Meshy às vezes devolve essa cor; o `arte/tools/blender/desneon.py` apaga a matiz 40–110° saturada da textura antes do jogo.
- **Remesh de 3 K:** o capim de duna de lâminas finas sobreviveu (lâmina presa na base não estilhaça; o que estilhaça é folha solta).
- **Metal da Meshy:** todo modelo novo precisa passar pelo `KitCenario.Domado`. O castelo era o único que não passava, e o céu novo o denunciou.
- **Textura criada em código:** para sobreviver ao `UnloadUnusedAssets`, precisa de `HideFlags.DontUnloadUnusedAsset` (ou de um objeto da cena que a referencie, como o Sprite do Selo). Não resolveu o logo, mas vale sempre.

**Créditos Meshy:** ~340.

**OS 20 MAGOS TÊM KIT (12/09, noite).** Os 17 que eram "declarados e inertes" ganharam passiva, tática e suprema com limitador e VFX, seguindo as fichas de `design/personagens/`. O trabalho foi feito por 4 agentes em paralelo (grupos A–D) e **só com arquivos novos**. Para isso, o coordenador tornou `Kits`, `KitRunner` e `VisualDosKits` classes `partial` com ganchos `GrupoA..D` (métodos parciais; grupo sem arquivo some na compilação). As leis dos kits (sem mana, telegrafia de 1 a 4 s, tática de 5 a 10 s, ficha = registro) valem para todos. São 50 testes novos, com ~140 mutantes mortos.

| Grupo | Magos | Foto |
|---|---|---|
| A | Ceifadora (Mão do Vazio, Travessia), Corvus (Uivo, Lobisomem), Corvomante (corvo, Grasnido), Olho-de-Éter (Enxame, Crisálida) | 39-kit-* |
| B | Vitalis (Lúmen, Jardim da Aurora), Ilusionista (espelho, Baile de Espelhos), Vex (frascos, Grande Obra), Aelion (flecha carregada, chuva) | 39-kit-* |
| C | Umbra (Véu Umbrio, Dança das Sombras), Brok (Runa-Escudo, Forja Viva), Gromm (Totem das Chuvas, Espírito do Trovão), Maris (Onda Prisão, Maré Cheia) | 39-kit-* |
| D | Fizz (torreta, Megabobina), Sylva (broto, Coração), Basalto (onda de pedra, Monólito), Noctus (mordida, névoa), Pip (zigue-zague, supercélula) | 39-kit-* |

**O que ficou simplificado por falta de motor** (cada caso tem `ponytail:` no código e a mudança certa no relatório do grupo):
- `IConjurador.Teleportar`/`Impulso`: o dash e a travessia deslizam ou movem o transform.
- Invisibilidade de verdade: o bot ainda vê.
- Esquadrão: passivas de aliado dormem em solo.
- `Textos.HudEstados`: os estados novos não viram chip.
- Estruturas de kit na `Partida.Acerto`: torreta, bobina e bigorna são protegidas pelo próprio kit.

**Ondas 9–11:**
- **9A:** contorno quente e frio nos magos (toon próprio).
- **9B:** calçamento das ruínas.
- **9C:** tela de seleção com cartão, abas e habilidades descritas.
- **10A:** colunas e blocos das ruínas pela Meshy.
- **11:** muro de terra, torreta/bobina, placas do Basalto e escudo do Brok pela Meshy, no lugar das caixas.

**Portão:** 420 testes, 0 falhas. **Créditos Meshy:** ~620.

**Próximo passo, na ordem:**
1. **Medir FPS no Poco F4** com o APK mais novo (pasta de testes de sempre). O aparelho não estava no `adb devices` em 12/09 à tarde. As ondas 5 a 8 são as mais caras até aqui:
   - sombra 4096 com 4 cascatas e SSAO;
   - chão `_Chao` em toda a ilha (~880–985 slots no pior pixel);
   - 615 árvores da Meshy instanciadas (LOD0 de 3 K até 40 m) e 1.040 moitas;
   - água +45%;
   - VFX do voo.

   Se cair, os KNOBs pela ordem: `m_RenderScale` 0,85; SSAO `m_Active` 0; `_Pintado`/`_Fissura` 0; sombra 2048; `DistanciaLod1` 30.
2. ~~Ondas 10A e 11, HudEstados dos kits e verbos de motor~~ — feitos (ver o bloco de 13/09 acima). Falta: bots usando kit (decisão do Diretor); `Interromper()` do canal do baú e `RegredirNivel()` da Vitalidade (a 12A deixou anotado); fatiar o boot da ilha (`Ilha.MontarEmFatias`, os 6 s do aparelho) e aquecer shaders com `ShaderVariantCollection` na tela de carregamento.

**Ondas 6 a 8 (12/09, fim de tarde), mesmo método:**

| Onda | O que se vê | Arquivos | Foto |
|---|---|---|---|
| 6A | Minimapa (ilha pintada 1x fora da thread principal, tempestade, próximo círculo, baú, seta com cone), bússola no topo, mapa grande ao tocar | Minimapa (novo), Hud, Textos | 30 |
| 6B | Árvores e moita geradas no SITE da Meshy (texto → textura → remesh grátis): copa larga e pinheiro; 615 no lugar de sempre, `RenderMeshInstanced` por bloco de 60 m, LOD1 a 40 m; moita de 1,2 K até 45 m e de 428 tris até 95 m | Vegetacao, 35/36/37-*.glb, ArkanaArvoreInstancing.mat | 31 |
| 7A | A água "listrada" do alto era o ALAGADO: bruma sem LOD (seno×seno) + o mar desenhado por cima das poças (ordem de transparente). Mar mascarado nos discos, detalhe por fwidth, reflexo e caminho de sol | ArkanaAgua/ArkanaNevoa.shader, Ilha | 32 |
| 7B | Campina, mata e praia no modo `_Chao`: manchas frias/quentes, terra batida e trilhas, trevo e flor, chão escuro na mata, areia molhada | ArkanaToon, Relevo, Ilha, Grama | 33 |
| 7C | O voo: círculo de runas e rochas em órbita no castelo, esteira dourada, vento na queda, fitas no planeio, anel no pouso | VisualDoVoo (novo), Main | 34 |
| 8A | Barra de vida/escudo e nome sobre o inimigo acertado (e sob a mira, com linha de visada) | MarcasDeAlvo (novo), Hud | 35 |

**Lições:**
- **Meshy para vegetação:** nuvem de folhas soltas estilhaça no remesh; peça cachos SÓLIDOS no prompt.
- **Remesh personalizado:** o do site aceita valor abaixo de 3K (só avisa); a moita de 428 tris saiu de lá.
- **PowerShell:** o 5.1 DESCARTA argumento vazio ("") para exe nativo, e os parâmetros do `otimizar.py` escorregam. O 80 virou giro em X e o pinheiro saiu deitado. Passe sempre um valor.
- **Blender 4.1+:** o `shade_smooth_by_angle` é modificador, então `export_apply=True` é necessário.
- **Árvores:** o facetado de perto era geometria (decimação para 1,5 K), não normal. O LOD0 passou a ser o remesh de 3K do site.

**Portão:** 347 testes, 0 falhas. A bateria completa de fotos (38 testes, 71 fotos) passou sem regressão. **Créditos Meshy:** 879.

**O que foi feito (12/09, tarde — ordem do Diretor: "acelere, mais atividades ao
mesmo tempo").** Cada onda tem 2 a 4 agentes, cada um dono dos seus arquivos. Eles
escrevem em cópias no rascunho e compilam fora do Unity (Roslyn contra as DLLs do
Library). O coordenador confere o md5 da base, aplica, roda UM portão e UMA
rodada de fotos filtrada (`foto.ps1 "Foto_X|Foto_Y"`) e gera o APK enquanto a
próxima onda escreve.

| Onda | O que se vê | Arquivos | Foto |
|---|---|---|---|
| 3 | Braço esquerdo da Pyra em brasa, eco do Véu em silhueta, orbes de loot com halo, telas de VITÓRIA/DERROTA e PAUSA | VisualDosKits, VisualDaPartida, Hud | 17–23 |
| 4 | O PESO do acerto: estouro por elemento, piscada do corpo (MaterialPropertyBlock HDR), bolha na cor do escudo, número de dano que pula. **O chão agora para o tiro** (antes varava morro; o terreno reativo só via tiro em corpo) | VisualDoImpacto (novo), Partida, Hud | 24, 25 |
| 5A | Chão do pico: manchas de pedra gasta, terra quente e líquen, estratos na encosta, fissura e seixo pintado de perto. Liga só no material do terreno (`_Chao`) | ArkanaToon.shader, Relevo, Ilha | 26 |
| 5B | Os 232 pedregulhos viram a rocha da Meshy (`18-pedregulho.glb`, 1 K), assentados e tintos por bioma; seixos no cume | Vegetacao, Ruinas, Grama | 27 |
| 5C | **A sombra macia estava DESLIGADA no asset URP** (a luz pedia e o asset ignorava). Agora: 4096 px, 4 cascatas, macia alta, SSAO | Sol, URP_Base(_Renderer).asset | 28 |
| 5D | Derrubado: anel vermelho e losango. Abate: coluna de alma e corpo que deita e afunda. Faixa ELIMINADO: NOME. Vinhetas macias: as 4 tarjas chapadas de dano davam moldura de borda dura | VisualDoAbate (novo), Pawn, Hud, HudAviso | 29 |

**Lições da tarde:**
- Número de dano ancora no **viewport**, não em pixel. A foto de 2400x1080 sobre a tela de 640x480 do teste jogava o número no joystick, e no aparelho a área segura o deslocava.
- O `otimizar.py` agora **solda por distância antes de decimar**. O glb do remesh chega em ~900 ilhas soltas, e era isso que estilhaçava a peça, não a razão.
- Foto de combate roda no **treino**. A versão na partida normal dependia de onde o castelo estava, e uma rodada pousou os dois no convés, a 262 m.

**Portão (onda 5):** 334 testes, 0 falhas.

#### (12/09, manhã) os 20 magos REAIS, o kit sem buracos e o Altar estão no jogo (foto); falta jogar no aparelho

**Próximo passo, na ordem:** instalar o APK novo no Poco F4 e medir FPS com o
elenco real e as 15 peças do kit (`--es arkana_auto partida`); se cair, o
primeiro corte é o LOD dos magos distantes. Depois: baú/luvas pela receita do
kit, luz e pós (passo D, que custa FPS: só com o aparelho medindo). Se o
procedural ainda destoar no celular, a troca boa é um remesh de 1K da rocha do
kit (grátis no site) para os pedregulhos. A seção "LEVA 4", depois
do passo A, tem a receita inteira do site.

**09/09/2026.** Ordem do Diretor: *"quero desistir da ideia de fazer para
Steam... tudo que estava sendo feito no Godot eu quero que seja adaptado para
ser feito com o Unity, porque se ele faz os dois projetos ao mesmo tempo não faz
sentido manter vários projetos... não pretendo mudar mais agora com o
conhecimento do Unity e como ele funciona. O que tiver que ser refeito ou
criamos do zero faz parte, são processos."* E em seguida: *"deixe para montar
um novo APK mais para frente quando fizermos mais coisas... avançar tanto
quanto avançamos no Godot."*

Uma pessoa, dois jogos (este e o Limiar), **uma engine**. Esta é a última troca
de engine do projeto.

#### O QUE FOI FEITO (09/09 → 11/09): `mobile-unity/` existe e passa no portão

| | |
|---|---|
| **Projeto** | `mobile-unity/` — Unity 6000.3.23f1, URP, Input System, uGUI, Test Framework; Android IL2CPP/ARM64, minSdk 26, `br.com.vstack.arkana` |
| **Portão** | `powershell -File mobile-unity\portao.ps1` → **420 testes, 0 falhas** (12/09, noite) (285 EditMode sobre classes puras, com a altura de cada um dos 20 magos medida na malha deformada + 30 PlayMode: os que montam a arena inteira e rodam 3 s sem um log sequer, e as fotos) |
| **Fotos** | `powershell -File mobile-unity\foto.ps1 [filtro]` → as fotos do jogo rodando em `mobile-unity/Logs/fotos/` + `diag.txt` (o que a câmera, o corpo e os kits tocam). Sem filtro roda todas (~15 min, as 20 folhas de clipes pesam); com filtro, só o que a leva mexeu, ex. `.\foto.ps1 "Foto_Kit\|Foto_Menu"`. **Toda leva visual termina olhando as fotos** |
| **Sistemas reescritos** | Core (Balance com todos os números, Kits dos 20, Combat num ponto só, Velocidade como produto único, Vitalidade, Textos, Bus) · Mundo (Relevo procedural de 600 m com 7 POIs em fração do raio e 14 nascimentos, Ilha com malha e colisor, Vegetação por célula, Castelo com N passageiros por seed, Sol) · Partida (Zona que nasce inerte e liga no pouso, Queda, luvas/loot/Baú Celestial, Derrubado/esvaecer/reerguer, Projétil, Efeitos, Água, Locomoção com dodge/pulo/flutuar, Pawn/Player/Bot com percepção de 4 canais, câmera no ombro, loop de partida e TREINO) · Kits (KitRunner com carga da suprema e telegrafia grampeada; Pyra, Véu, Tessa) · Terreno reativo (fogo por orçamento com 1 rolagem por aresta, carvão, gelo, elétrico por água conectada, muro, lama, vento) · Personagem (mago procedural com 10 clipes por código, identidade dos 20, luva visual) · UI (gesto único em dp, joystick, HUD completa observando o Bus, avisos, menu, config persistida, seleção dos 20, selo) · 48 timbres sintetizados |
| **APK** | **GERADO E JOGADO no Poco F4 em 12/09.** `build_apk.ps1` → 176 MB, 5 min 20 s a primeira vez, 1 min 33 s incremental; cópia datada em `mobile-unity/Builds/testes/` (fora do git) |
| **FPS no aparelho (a dívida de 25/08, PAGA)** | **60 FPS sustentados, quadro de 16,6 ms, pior quadro 33 ms, 36,9 °C**, sem erro nem exceção no logcat, numa partida inteira: castelo, queda, pouso, 12 bots em FFA, tempestade. A tela do Poco estava em 60 Hz: o vsync segura aí; a folga real só aparece com a tela em 120 Hz |
| **Visual** | seis shaders próprios (toon do chão com textura de detalhe, grama instanciada, água com ondas e espuma, céu de entardecer, bruma, toon do mago), grama/flores/juncos por célula, as 8 peças do kit plantadas, ruínas, mar até o horizonte, castelo e luvas `.glb` do Godot via glTFast, 20 retratos, loot/baú/tempestade/kits/terreno desenhados, sol do Godot, espaço de cor Linear |

**Como foi feito.** Sete raias em paralelo (Core, Mundo, Gameplay, UI; depois
Personagem, Pawn, Kits+Terreno; depois Cena e a costura), cada uma dona de uma
pasta, codificando contra um contrato escrito antes
(`mobile-unity/ARQUITETURA.md`) e lendo o `.gd` correspondente como referência
de COMO — nunca traduzindo. Como só cabe **um Unity por vez** no projeto, as
raias validaram com o Roslyn do próprio Unity fora do editor (`csc.dll` via
`NetCoreRuntime\dotnet.exe`) e rodaram os testes num mini-runner sobre o Mono; o
portão oficial foi sempre do coordenador. Toda lógica em classe pura com
`Tick(dt)`; `MonoBehaviour` só como casca — é isso que deixa 229 testes rodarem
headless em três minutos.

**Por quê assim.** O Godot deixou 21 mil linhas e 12 autotestes que diziam O QUE
se cobra; o Unity herdou os invariantes, não o código. Cada teste novo se provou
reintroduzindo o defeito (o próprio reporte das raias registra: "sem a guarda
de estado, o teste fica vermelho").

#### LEVA 3 (11/09): o que se VÊ — e a regra nova de olhar a foto

**A regra que esta leva deixou:** teste verde não diz se o mago saiu rosa, se a
ilha ficou preta ou se a HUD saiu da tela. `powershell -File mobile-unity\foto.ps1`
sobe o jogo com GPU e grava PNGs em `mobile-unity/Logs/fotos/` (menu, treino,
mago de perto, ilha do alto, chão rasante, castelo, queda, pouso, zona do alto,
jogador na zona), na proporção e na densidade de tela do Poco F4. **Toda leva
visual termina olhando as fotos.** Foi assim que o Godot fazia (`_shot.gd`), e
foi assim que esta leva achou onze defeitos que nenhum dos 257 testes pegava
(cada um virou teste, provado em vermelho, e o `diag.txt` nasceu porque dois
palpites sobre um quadro estranho erraram antes de o dado chegar):

| Defeito visto na foto | Causa | Conserto |
|---|---|---|
| Fogo, gelo e muro nunca aconteciam na partida | o terreno reativo só existia nos testes: nenhuma cena o criava | `Main.Montar` cria o `TerrenoReativoBehaviour` |
| Loot, baú e tempestade invisíveis | só existiam como dado; ninguém desenhava | `VisualDaPartida` (luvas `.glb`, coluna de luz por raridade, baú caindo com rastro, parede da tempestade, anel do próximo círculo) |
| Kits e terreno invisíveis | idem | `VisualDosKits` e `VisualDoTerreno` (e o muro virou cobertura: segura tiro de verdade) |
| Duas faixas escuras de borda dura cortando o mundo | a "grade de leitura" da HUD era retângulo chapado (o Godot fazia igual) | degradê suave |
| "VOLTE PARA A ZONA" logo após o pouso | a Zona contava "fora" durante os 70 s de abertura, quando a tempestade não existe (o Godot fazia igual) | só conta com a tempestade ativa; teste provado em vermelho |
| Mar acabava num vazio cinza | plano finito | mar de 6 km que segue a câmera; céu sem chão cinza |
| Ilha lavada de névoa vista do alto | névoa linear fixa — a lição G5 do Godot tinha se perdido | névoa acompanha a altura de cada câmera que desenha |
| Câmera colada na torre do castelo; na queda olhando o horizonte | braço de 26 m num castelo de 35,8 m; 7° de inclinação | 70 m e 43° no castelo; 9 m e 32° na queda (KNOB, calibrar no aparelho) |
| Peças do kit estilhaçadas, chão aparecendo por dentro | a malha decimada da Meshy tem triângulo com a volta trocada e o glTFast só desenha um lado | material URP Lit com os dois lados |
| Mago pousava DENTRO de uma rocha | a Queda só conhecia o terreno ("pouso sempre no terreno, sem telhado") | `ChaoComObstaculos`: a Queda vê o topo do que estiver no caminho; peças do kit com casco convexo (a malha crua deixava o raio atravessar o topo) |
| Câmera dentro da rocha com o mago encostado nela | o ombro da câmera (0,78 m ao lado) entrava na parede e o cast que começa dentro não a reporta como parede | colisão em três camadas + **câmera em canto sobe e olha de cima** (a saída clássica de toda 3ª pessoa) |

**O que entrou além disso:** seis shaders próprios em `Resources/` (toon do
terreno com textura de detalhe, grama instanciada, água com ondas e espuma de
margem, céu de entardecer com nuvens, bruma, toon do mago); grama, flores,
juncos e seixos por célula; as 8 peças do kit de cenário plantadas pelo mapa;
ruínas e rochedos no mar; o castelo e as luvas `.glb` do Godot (glTFast 6.20);
os 20 retratos do menu; o sol quente do Godot; projeto em espaço de cor Linear
(o Godot é Linear; em Gamma o toon saía saturado).

#### PASSO A FEITO (12/09, madrugada): o APK no Poco F4, sem dedo

**O que foi feito.** Três APKs em 25 minutos; o terceiro joga uma partida
inteira sozinho no aparelho e mede o FPS. O jogo passou: 60 FPS cravados,
HUD na escala certa (dp de verdade, 395 ppi), salto com altímetro, pouso na
campina, bots se matando (12 → 6 em dois minutos), tempestade contando.

**Como.** O MIUI do Poco F4 recusa três coisas do `adb`: toque injetado
(`INJECT_EVENTS`), `install -g` (permissões em lote) e instalação sem
"Instalar via USB" ligado. Então o jogo ganhou dois extras de intent e um
medidor:

```
adb shell am start -n br.com.vstack.arkana/com.unity3d.player.UnityPlayerGameActivity     --es arkana_auto partida --ei arkana_fps 300     # ou "treino"; sem extras = jogo normal
adb logcat -s Unity | grep "ARKANA FPS"              # media, pior segundo, pior quadro, a cada 5 s
adb exec-out screencap -p > foto.png                 # a tela do aparelho
```

O jogador automático salta a 45% da rota do castelo (na 1ª rodada ele não
saltava e o castelo o empurrou no FIM da rota, no mar). `targetFrameRate = -1`
**não é "sem teto" no Android: é 30 FPS** (o padrão do Unity) — pedir 300 é o
jeito de medir.

**O que o aparelho mostrou que a foto de PC não mostrava.** O boot tem dois
engasgos de 6 s (montar a ilha e a arena: grama, kit, colisores); os bots
duelam de verdade; a água de nado funciona (o mago caiu no mar e nadou); as
rochas do kit continuam "rachadas" de perto (asset).

**A avaliação do Diretor, na hora:** *"parte gráfica está muito amadora ainda,
muito longe de algo real; espero que em breve seja possível ver algo
visualmente melhor."* **Isso vira a prioridade.** O que está na tela é
placeholder: mago de primitivas, rochas decimadas com buracos, luz de primeira
passada, nenhum VFX de assinatura. O caminho para "real" está na ORDEM abaixo.

#### LEVA 4 (12/09, madrugada, sem o Diretor): o ELENCO REAL e o kit SEM BURACOS

**O que foi feito.** Os **20 magos** refeitos no SITE da Meshy (nunca pela API),
um por um, e dentro do jogo: `mobile-unity/Assets/_Arkana/Resources/magos/NN-slug.fbx`
+ `-cor.png` + `-normal.png`. E as **8 peças do kit** trocadas por versões com
topologia fechada (o "vidro estilhaçado" das rochas era a decimação).

**A receita do elenco (a que funcionou, para refazer um mago):**

1. **Modelo** → Meshy 7 Flagship, **Multi-View** com três vistas das
   referências: `master-reference-frente.png` (principal), `vista-costas.png`
   (Trás) e `vista-lateral.png` no slot do lado para onde o personagem OLHA
   (Esquerda = olha para a esquerda da imagem: 01, 03-08, 11-17, 19; Direita:
   02, 09, 10, 18, 20). Ultra 2K, Textura, **Pose T**, Melhoria de imagem,
   Privado → **35 créditos**. A Pyra ficou com a de 1 imagem (ordem do Diretor:
   "se funcionar, não refaz").
2. **Ilusionista:** as três vistas trazem o clone de cristal junto; recortado
   por polígono (fundo pintado com a cor da própria linha) →
   `_originais/multiview-*-sem-clone.png`. Saiu limpo.
3. **Animar → Rig** → pede remesh (300 K+ faces): **Corrigido 30K, Triângulo**
   (0 crédito). Rig **Humanoide**, **altura da ficha** (`IdentidadeMago`), e
   **conferir os marcadores**: a Meshy põe a **virilha no cinto** em quase
   todos — descer até onde as pernas se separam; tornozelo no tornozelo.
4. **Clipes** (biblioteca, grátis; o rig já traz Walking/Running):
   **Combate Ocioso** (idle), Mage Spell Cast, Outono 1 (queda), Coletar
   objeto, Alcance Prone Ajuda (derrubado), Nadar para frente, Nadar Parado,
   Andar Agachado com Cautela, e **Planar Arkana** (Texto para Movimento, 10
   créditos, uma vez só: vale para todos, mesmo id). **NUNCA** o "Planar
   horizontal v2" (movimento de IA antigo da conta): mergulha de cabeça para
   baixo — o Diretor viu e disse *"não é aceitável"*. **Nem** o "Parado 1"
   como idle: ergue o braço no meio do laço.
5. **Baixar**: fbx, **Todos Adicionados**, **Arquivo único**, 30 FPS → zip.
   `powershell -File mobile-unity\importar_mago.ps1 <zip> NN-slug` extrai e lista
   as takes.
6. Custo total da noite: **650 créditos** (1.569 → 919), com o remesh do kit
   (grátis). A geração do Gromm falhou no servidor uma vez (crédito devolvido);
   o "tentar de novo" gerou só a malha e a textura saiu pelo botão Textura (10).

**O que o Unity precisou (e os defeitos que só a foto mostrou):**

| Defeito | Causa | Conserto |
|---|---|---|
| Pyra entrou com **1 cm** (um ponto no chão) e o portão verde | o `SkinnedMeshRenderer.bounds` do FBX da Meshy vem ~100× maior (localBounds no espaço do Hips, que herda a escala 100 da Armature) e o Mago escalava por ele | altura pela malha de repouso levada pelo transform do renderer; teste `CharactersMagoExternoTests` mede a malha DEFORMADA (BakeMesh) de todo slug com FBX |
| FBX sem textura | a Meshy não grava o caminho das PNGs | `ImportacaoArkana` liga `-cor`/`-normal` no material NA IMPORTAÇÃO (`_NORMALMAP` ligado em runtime some no APK) |
| Elenco inteiro de **braço erguido** na foto | `Play(Idle)` sai cedo porque o clipe inicial já é Idle: o modelo ficava na pose congelada do FBX até correr a 1ª vez | o Mago começa o idle ao vestir o FBX; teste exige `Animation.isPlaying` |
| Planar de cabeça para baixo | clipe ruim da conta | trocado pelo Planar Arkana; alias por id no `PoseMago` |
| Troca de clipe "pulando" no externo | `Animation.Play` seco; disparo único em `Once` morria e a volta saía do nada | `CrossFade` com os tempos do procedural; disparo único em `ClampForever` e fim lido pelo tempo |
| Portão parado para sempre depois do EditMode | `Start-Process -Wait` espera a ÁRVORE de processos, e o Unity deixa o `VBCSCompiler` (Roslyn) vivo ~10 min | `WaitForExit()` só no Unity (portão, foto e build) |
| PlayMode vermelho com o kit novo ("partial hull") | o casco convexo tem teto de 255 faces no PhysX; a peça de 10 K estoura | `MeshCollider` com a malha REAL (não convexa): some o aviso e a ponte-raiz ganha o vão embaixo |
| Nuvens cortadas em **linhas retas** no céu (foto `12-elenco`) | o hash `frac(sin(x)*43758)` com x na casa dos milhares: o mesmo canto da grade de ruído, calculado por duas células vizinhas, dava valores diferentes | hash sem seno (Hoskins) no céu, no chão toon e na água — os três tinham o mesmo hash |

Fotos novas: `11-clipes-<slug>.png` (os 10 clipes do mago lado a lado, **toda
leva que mexer em clipe olha essa foto**) e `12-elenco.png` (os 20 juntos).

**O kit sem buracos.** A causa do "estilhaçado" era a decimação COLLAPSE do
`arte/tools/blender/otimizar.py` sobre o original de 3 M de faces. Agora a
topologia vem do **Remesh do site** (grátis): 10K triângulos nas 7 peças
grandes, 3K no piso de runa; e o `otimizar.py` ganhou `tris_alvo = 0` (não
decima) e `tex_max_px` (textura 1K JPEG, só a cor base — o `KitCenario` só lê
baseColor). Cada peça: ~0,3–0,9 MB. Mapa (nome na Meshy → peça): Runic Stone
Sanctuary → 17, Emberstone Outcrop → 18, Verdant Rune Arch → 19, Mossroot
Arch → 20, Stormspire Nexus → 21, Arcane Celestial Altar → 28, Emberbloom
Tree → 30, Ancient Keystone Slab → 32.

**As sete da oficina entraram (12/09, manhã).** As peças geradas em 04/09
passaram pela mesma receita (Remesh 10K no site, 0 crédito → glb →
`otimizar.py` com textura 1K). O glb do remesh fica em
`arte/cenario/ilha-fraturada/_originais-3d/NN-*-remesh10k.glb` (fora do git).
Onde cada uma nasce vem das fichas de `arte/prompts/06`, `07` e `08`:

| Peça (nome na Meshy) | Onde | Altura |
|---|---|---|
| 22 arco partido (Runes of the Broken Arch) | anel das Ruínas, de frente para o centro; colide (as pernas batem, o vão passa) | 5 m |
| 23 coluna-braseiro (Emerald Flame Reliquary) | anel das Ruínas | 2,05 m |
| 24 estátua-vigia (The Veiled Warden) | anel das Ruínas, vigiando o centro | 4,1 m |
| 26 torre arcana (Astral Spire) | POIs, 4 no mapa | 22 m |
| 27 braseiro (Elemental Crucible) · 33 obelisco (Crimson Obelisk) · 34 plataforma (Stonefoot Shrine) | **o Altar de Sintonia, montado como a ficha**: braseiro no centro, 5 obeliscos no pentágono de 9 m (**dois tombados**, gema no chão), as duas plataformas frente a frente. Um só, no vale | 1,35 · 2,85 · 1,2 m |

O kit agora recebe as **pegadas das Ruínas** (colunas e blocos da muralha) e não
planta em cima delas.

**O procedural perto da Meshy (mesma manhã).** Ao lado das peças texturizadas, o
que é código lia como maquete. Três ajustes sem triângulo a mais:
- A copa ganhou cacho de 4 massas e `MalhaProc.Blob` passou a pintar em
  degradê por vértice (escuro embaixo). Árvore, moita, pedregulho, seixo e
  rochedo ganham volume.
- O pedregulho passou a usar `CorPedregulho` (família do basalto); o lilás
  lia como gelo.
- As colunas das Ruínas passaram a usar `CorRuina`, de pedra gasta; o azulado
  lia como cano de PVC.

Fotos `13-altar`, `14-ruinas` e `15-torre` (novas no `FotoTests`).

**Passo D começado (12/09, tarde).** O Diretor liberou o custo de FPS: *"o FPS
vai rodar legal, disso tenho certeza"*.
- **Pós-processamento.** Um volume global criado em código (`Ilha.MontarPos`)
  aplica tonemapping neutro, bloom leve (limiar 1,1), contraste e saturação
  +10 e vinheta 0,2. A câmera do jogador liga o `renderPostProcessing`, que no
  URP vem desligado. O `GlobalSettings` não remove variantes de pós, então o
  perfil feito em código vale no APK.
- **Personagem legível.** No `ImportacaoArkana`, a emissão virou a própria cor
  × 0,16, uma luz de rebote que tira o mago do contraluz sem brilhar. A versão
  do importador subiu para 2 para os FBX reimportarem.
- **Magia acesa.** A partícula aditiva dos kits ganhou cor base 1,8 (HDR), e o
  miolo passa do limiar do bloom.
- **Baú e luvas.** Estavam decimados (a foto `16-bau-luvas` mostrava o baú
  estilhaçado) e foram trocados pelo remesh do site: baú em 10K, luvas em 3K, 0
  crédito. A emissão das gemas e das brasas ficou, graças ao 6º argumento do
  `otimizar.py`. O baú caiu de 9,8 MB para 0,8 MB e cada luva de ~8 MB para
  0,4 MB.
- **Mapa das luvas.** Varinha = Emberhand Gauntlet · cajado = Celestial
  Sovereign Gauntlet · manopla = Gemini Gauntlet. Os glb do remesh ficam em
  `arte/cenario/*/origem/` (fora do git).

**O kit em ação, pela primeira vez em foto (`17-kit-tatica`, `18-kit-suprema`).**
Nenhuma foto tinha mostrado um kit disparando, e o diagnóstico achou dois
defeitos de jogo que os testes não pegavam:

| Defeito | Causa | Conserto |
|---|---|---|
| A muralha da Pyra nascia **atrás** dela, fora da tela | os botões TÁTICA/SUPREMA não contavam como mira: o kit saía para a frente do CORPO; parado e com a câmera girada, ia para trás | `Player.Tatica/Suprema` miram como o disparo (`YawAlvo` = yaw da câmera). Teste `Treino_TaticaSaiNaMiraDaCamera` |
| No treino a suprema levava 50 s, não 5 | `Partida.SupremaCargaS` existia e tinha teste, mas o `KitRunner` lia `Dados.SupremaCarga` direto | `KitRunner.SupremaCargaS` pergunta à partida. Teste `Suprema_NoTreino_EncheEm5s` |
| A muralha era uma barra laranja lisa | teto de 48 partículas para 8 m de parede, chama miúda | chama grande e densa (teto 260) + faíscas que espirram |
| Tiro = bola chapada | primitiva `Unlit` de cor pura | cor HDR (acende no bloom) + rastro aditivo por elemento |

**Três frentes em paralelo (12/09, tarde; ordem do Diretor: "acelere, mais atividades
ao mesmo tempo").** Três agentes escreveram código em arquivos separados e
compilaram fora do Unity (Roslyn do próprio 6000.3 contra as DLLs). Houve **uma
rodada só** de portão e fotos para as três:
- **Menu com fundo 3D (`VitrineDoMenu`).** A ilha e o sol nascem no boot. O
  mago escolhido fica no pico, virado para a câmera, que orbita devagar; o fundo
  do menu virou translúcido. A foto `01-menu` usa a câmera real do menu.
- **VFX da Véu e da Tessa.** O eco ganhou névoa e partículas espectrais. O fio
  ficou mais grosso, com faíscas e âncoras acesas. O tear ganhou anel no chão e
  faíscas girando na cúpula. O revelado virou feixe de luz, e a poça da Pyra
  ganhou chamas. Material novo `MaterialVfx.DeLinha()` (faixa macia na largura)
  para fio, feixe e rastro de tiro. Fotos `19/20-kit-<slug>`.
- **HUD.**
  - Barras com moldura, degradê, rastro de dano e número.
  - Botões com anel na cor da ação, cooldown radial e pulso da suprema pronta.
  - Joystick escuro com anel.
  - As barras já nascem com o valor real, e a suprema não aparece mais "pronta"
    no nascimento.

Rochedos do mar: a rocha vulcânica do kit num remesh de 3K, grátis (22 × 3K),
no lugar do bloco facetado. **Decisão que volta ao Diretor:** o validador propôs "Altar
só depois do playtest" porque o Altar encosta na Sintonia. Ele entrou só como
CENÁRIO, sem sistema nenhum; se for para esperar, é tirar uma linha do
`KitCenario.Montar`.

#### O que o Unity AINDA NÃO TEM (registro honesto, 12/09)

Tudo acima **passa no teste, foi visto em foto e rodou no aparelho**. Faltam:

1. **Medir no aparelho** o elenco real (20 × ~30 K triângulos) e o kit novo
   (10 K por peça): FPS e temperatura. Nada disso foi medido ainda.
2. **VFX de assinatura, pós-processamento e luz**: o personagem de frente para
   a câmera fica escuro contra o sol (ambiente baixo); sem bloom/tonemapping.
3. **Baú e luvas** ainda são os `.glb` do Godot. O castelo (30 K da API, foto
   `06-castelo`) lê bem e fica. Se o baú/luvas estilhaçarem de perto: mesma
   receita do kit — "Gemforged Treasure Chest" é o baú na oficina.
4. **Modelo externo** entra como `Animation` legado (com crossfade);
   retargeting/Humanoid ficou desnecessário para os clipes da Meshy.
5. "Intangível" não muda a colisão; bots não usam kit (como no Godot).
6. O gesto de disparo é o do GDD §19.3 (pressionar-arrastar-soltar, cancelar
   voltando ao centro). O Godot R17 tinha "tocar dispara + segurar auto-fogo".
   **Qual dos dois fica é do Diretor.**
7. **Sombra da grama** e o **toon** ainda são de primeira versão: ajustar cor
   ambiente, faixas e alcance da sombra olhando as fotos no aparelho.

#### O que está NA MÁQUINA (11/09)

| | |
|---|---|
| **Unity 6000.3.23f1 + Hub 3.21** | `C:\Program Files\Unity\Hub\Editor\6000.3.23f1`, módulos Android e Windows |
| **Licença** | Personal, resolvida online: **o Hub precisa estar aberto** ou o batchmode morre com 198. Os scripts abrem o Hub sozinhos — pelo AppID, **nunca por `unityhub://`** (sem caminho, o Hub entende "instalar editor" e mostra "versão do Editor arquivado") |
| **Aparelho de teste** | Poco F4 (Snapdragon 870, Adreno 650, 120 Hz) |
| **Apagado em 09/09** | Unreal 5.8, Lyra, caches do Epic, Godot 4.4.1 (~48 GB liberados) |

#### O que FICOU e o que SAIU do repositório (09/09)

| Fica | Por quê |
|---|---|
| `design/` inteiro | decisão não tem engine |
| `arte/` inteiro, incluindo os originais de 3 M de faces fora do git | matéria-prima; a versão de celular se deriva no Blender |
| `mobile-godot/` | **referência de leitura**: cada sistema ali foi jogado e medido. Sai quando o Unity for visto no aparelho fazendo o mesmo |
| `roblox/` | intocado, como sempre |

| Saiu | Por quê |
|---|---|
| `pc-unreal/`, `design/referencias/PC-STEAM-ANALISE.md`, exportador de heightmap, CI do Godot | produto abandonado; o que valia atravessou para as lições abaixo |

#### A ORDEM daqui para a frente — o VISUAL primeiro (ordem do Diretor, 12/09)

| Passo | O quê | Portão |
|---|---|---|
| **A ✅** | APK no Poco F4, partida inteira, FPS medido | 60 FPS, 16,6 ms, sem erro |
| **B ✅ (foto)** | **Elenco real**: os 20 magos do SITE da Meshy no jogo, 11 clipes cada (leva 4). **Falta o aparelho** | o mago real anda, corre, conjura e cai no aparelho; foto lado a lado com a ficha |
| **C ½** | **Kit sem buracos**: as 8 peças com remesh do site (leva 4) ✅, a ponte-raiz com vão (colisor da malha real) ✅, as 7 peças da oficina (arco, coluna-braseiro, estátua-vigia, torre arcana e o Altar de Sintonia) ✅ (foto). Baú e luvas pela receita ✅ (foto 16). Pedregulho da Meshy ✅ (onda 5B); árvore da Meshy na onda 6 | a rocha de perto lê como rocha; FPS mantido |
| **D ¾** | **VFX de assinatura e pós**: pós ✅, partícula HDR ✅, preenchimento do mago ✅, braço de chama da Pyra ✅, muralha de brasas ✅, fio da Tessa ✅, eco da Véu ✅, impacto/derrubado/abate ✅, sombra macia + SSAO ✅. Falta afinar luz e ambiente pelas fotos NO APARELHO | jogo bonito de ver em vídeo — o Diretor aprova |
| **E** | Boot sem engasgo (montar ilha/arena em fatias por frame); tela em 120 Hz para medir a folga | sem quadro acima de 100 ms |
| **F** | Rede: continua não existindo e continua sendo o item mais caro. Netcode for GameObjects + servidor dedicado sem amarrar a fornecedor | dois celulares na mesma partida |

**Antes da rede, o jogo solo contra bots vale por si** — a decisão de gênero
que sobreviveu ao desvio (ver lições). Modelo de receita fica com o Diretor.

#### O que NÃO muda

- **O elenco continua a ser refeito no SITE da Meshy, um por um.** A causa e a
  receita estão na seção da leva 7, abaixo. Destino: FBX para o Unity.
- `design/` e `arte/` alimentam as implementações; as implementações **nunca
  cruzam código**. GDScript não se traduz: se relê a decisão e se escreve em C#.
- **FPS se mede, não se estima.** Medido: 60 no Poco F4 (12/09). Toda leva visual mede de novo.
- **Teste provado em vermelho, portão verde antes de entregar.** *"Sem
  atropelar as coisas."*

#### Decisões que esperam o Diretor

1. **Gesto de disparo**: GDD §19.3 (o que está no Unity) ou R17 do Godot
   (tocar dispara + segurar auto-fogo). É uma classe só (`GestoLogica`).
2. **Bots com kit?** Hoje não (como no Godot). Ligar é uma linha no `KitRunner`.
3. **Baú Celestial**: dano ao canalizador NÃO cancela (só sair do raio, cair ou
   morrer) — herdado do `.gd`, nunca carimbado. Hora fixa (45 s) vs sorteada.
4. **A tabela de fases da Zona é proposta, não spec** (70 s de abertura, 10 s
   de formação, 5 fases 60/50/40/30/15 s, frações .62/.42/.26/.13/0), calibrada
   contra PUBG/Apex/Fortnite, nunca sancionada por escrito.
5. Do papel de 26/08: **lista dos 10 magos do lançamento**, **nome e valores da
   moeda**, **quais pares de fusão estreiam**; e o **círculo final no platô e no
   pico** (o teto de altura caiu; final em terreno alto é padrão do gênero).
6. Package ID `br.com.vstack.arkana` está em uso; mudar é uma linha em
   `Editor/Build.cs` e no `ProjectSettings`.

---

## LIÇÕES DO DESVIO STEAM / UNREAL (27/08 → 09/09/2026)

Registrado para não repetir. **O que erramos, como consertamos, o que atravessou.**

### O que erramos

- **Decidimos pela feature mais distante.** Os argumentos de 27/08 (rede
  competitiva para 40+, anti-cheat nativo, Nanite) eram tecnicamente
  verdadeiros, mas rede não existia em nenhuma base e continua não existindo.
  Trocamos engine e plataforma por causa de um item que está a meses de
  distância, enquanto o item de hoje (FPS no aparelho) seguia sem medição.
- **Mudamos antes de fechar o jogo.** O Godot tinha 12/12 autotestes verdes e
  partida completa; o Unreal chegou a ter uma ilha bonita e nenhuma partida.
  Treze dias e ~45 GB de instalação para um mapa sem jogo.
- **Uma pessoa, duas engines.** O custo real não era a curva do Unreal: era
  manter dois jogos em ferramentas diferentes. Foi isso que decidiu a volta.

### Como consertamos

- Uma engine para os dois jogos (Unity), decidida em 09/09 com a frase *"não
  pretendo mudar mais"*. Tudo do Unreal apagado da máquina e do repositório.
- A regra "bases nunca cruzam código" foi mantida: o Unity não herda GDScript
  nem Blueprint. Herda decisão, arte e número medido.
- O portão do passo 0 é a medição que faltava desde 25/08. Nenhuma decisão de
  escopo (mapa maior, mais bots, mais peças) antes de FPS no aparelho.

### O que atravessou do Unreal e vale no Unity

1. **"O arquivo que entra não é o mundo que sai, e medida de ontem não mede o
   mundo de hoje."** Quem responde onde o chão está é o colisor, traçado
   AGORA. Um centro de ilha lido de uma paisagem antiga deslocou 1.200 m e
   afogou metade das peças no mar. Medida velha guardada em variável nova não
   parece chute, parece medição — é o defeito mais traiçoeiro que existe.
2. **Cobertura por composição.** As peças de parede/cânion não existem na
   oficina da Meshy. Uma fila de rochas de basalto encostadas lê como crista
   de pedra, com a peça de maior reuso do kit e custo de draw call zero. Quando
   a peça de parede existir, troca-se a malha e a regra continua.
3. **O mapa grande.** 2.400 m de lado, 1.396 peças assentadas no chão medido,
   6 nomes de área legíveis da queda, 5 pontos de partida (lago e alagado fora:
   o centro deles é água). O plano está em `design/cenario/MAPA-GRANDE-PLANO.md`
   e vale para o Unity — com o teto do celular medido antes.
4. **As sete peças novas** geradas no site em 04/09 (215 créditos) continuam na
   oficina da Meshy: `arte/cenario/ilha-fraturada/_originais-3d/00-LEIA.md`.
5. **Gênero, da análise de mercado:** battle royale de 60 com matchmaking global
   é a aposta mais arriscada possível para um projeto sem base. Lobby de 16 a
   20, **bots por padrão sempre** (a partida nunca espera) e **modo solo/PvE
   valendo por si** — o jogo pode lançar sem rede, e a rede entra depois,
   financiada. O Spellbreak provou que combate excelente não segura um BR sem
   base; retenção, não aquisição, foi o que o matou.
6. **Dois fornecedores de servidor dedicado fecharam em 2026** (Hathora e o
   Multiplay da Unity). Quando a rede vier: build headless em contêiner, sem
   amarrar a fornecedor.

---

### A leva 7 FOI DESFEITA: o elenco se faz no SITE (27/08, continua valendo)

**27/08, noite.** Esta secao substitui a que existia aqui e dizia "o elenco
inteiro no jogo (20 de 20)". Aquilo foi **apagado por ordem do Diretor**, e o que
segue e' o registro do porque — para ninguem tentar de novo.

#### 1. O QUE FOI APAGADO, E POR QUE

Os 18 magos foram gerados e riggados pela **API** da Meshy. No aparelho do
Diretor eles **animam como robo**: *"parecem robos, congelados na mesma pose
mesmo quando correm"*. Ordem dele: **apagar tudo e refazer no SITE, um por um,
mesmo perdendo o que foi feito.**

**A CAUSA MEDIDA, e sao duas:**

1. **O rig da API nao tem a etapa de MARCACAO DE ARTICULACOES.** No site, depois
   de escolher Humanoide e a altura, ha' uma tela em que se posiciona queixo,
   ombros, cotovelos, pulsos, virilha, joelhos e tornozelos sobre o corpo. E' essa
   tela que faz a animacao encaixar naquele corpo. A API pula isso: manda a malha
   e a altura, e o esqueleto sai chutado.
2. **A fusao de clipes produziu CLIPE DUPLICADO.** Conferido lendo as chaves de
   rotacao de `brok.glb`:
   - `mage_soell_cast` era **copia byte a byte de `cair`**
   - `derrubado` era copia de `Idle_02`
   - `nadar_parado` era copia de `Walking`

   **Dez clipes distintos vendidos como treze. Ao lancar magia, o mago tocava a
   animacao de CAIR.** Isso estava no `brok.glb` desde a fusao de 26/08 — ou seja,
   o defeito e' anterior aos 18 e foi COPIADO para todos eles.

**Por que o teste nao pegou:** ele cobrava que o clipe EXISTE e que o modelo e'
vestido. Clipe duplicado passa nas duas — existe, e o modelo veste. Cobrir
"o clipe X e' diferente do clipe Y" nunca ocorreu a ninguem porque ninguem
imaginou que a fusao pudesse renomear a mesma acao duas vezes.

#### 2. O QUE SAIU DO PROJETO (higienizacao, ordem do Diretor)

| O que | Por que |
|---|---|
| os 20 `.glb` de personagem + texturas | 18 da API animam como robo; os 2 do site tem clipe duplicado |
| o atelie da API (`godot/characters/modelos/NN-slug/`) | materia-prima do caminho errado |
| `tools/meshy/vestir.sh` | era a esteira do caminho errado |
| `docs/prompts/personagens/` (20 arquivos) | nasceram de uma auditoria mal lida; a arte SEMPRE existiu em `personagens/NN-slug/arte/_originais/` |
| o modo `manter` de `fundir_animacoes.py` | e' o que produziu o clipe duplicado |

**Barreira posta no codigo:** `tools/meshy/meshy.py` agora **recusa** `gerar` e
`riggar` de personagem (`PERSONAGEM_PELA_API = False`) com a explicacao na
mensagem de erro. `prop` (cenario) continua liberado — la' nao ha esqueleto.
`fundir_animacoes.py` ganhou um aviso no topo: **props e cenario apenas**.

**Estado do jogo agora:** 0 de 20 magos com `.glb`; todos rodam o **mago
procedural**, que e' o fallback e funciona. Portao **12/12 verde**.

**Dois testes foram corrigidos** para nao ficarem vermelhos por semanas (portao
vermelho cronico ensina a ignorar o portao):
- "todo mago tem .glb" virou **placar impresso**, nao portao — faltar modelo e' o
  estado esperado enquanto o elenco e' refeito.
- "player inicia como Pyra GLB" cravava o caminho do arquivo; virou "o player
  nasce com mago vestido (procedural ou externo)".

**O que CONTINUA sendo portao, e e' o que tem dentes:** modelo que EXISTE tem que
ser VESTIDO — um `.glb` no disco que o Mage rejeita em silencio e' o modo de
falhar mais caro do pipeline.

#### 3. O CAMINHO CERTO, PASSO A PASSO (site, um mago por vez)

1. **Imagem** -> subir `personagens/NN-slug/arte/_originais/master-reference-frente.png`
   -> Meshy 7 Flagship, **Modo Ultra**, **Textura**, **Pose T-Pose** -> Gerar (35 cr)
2. **Animar** -> selecionar o modelo -> **Rig** -> Humanoide -> **altura da ficha**
   -> **conferir a marcacao das articulacoes** -> Confirmar
3. aplicar os clipes da **biblioteca** (andar, correr, parado, cair, planar,
   nadar, pegar, derrubado e o **gesto de disparo**)
4. exportar **rigged + todos + arquivo unico**, em **FBX**, e importar no
   `mobile-unity/` como rig Humanoid (o Mecanim retargeta)
5. **conferir no motor** que os clipes sao DISTINTOS (nao repetir o erro da fusao)

⚠️ **O Ilusionista** precisa do `frente-recorte.png` (a mestra dele tem duas
figuras de proposito — ele e o reflexo). O `meshy.py` ja' da' precedencia a esse
arquivo, e ele foi mantido.

#### 4. CREDITOS

Gastos e perdidos no caminho errado: **~670**. Mais **30** de uma geracao que eu
disparei por engano ao testar a barreira depois da ordem de parar — erro meu,
registrado. Saldo: **~2.264**.

---

## HISTÓRICO DO GODOT (19/08 → 04/09/2026) — referência para a reescrita

O que segue é o registro das levas em Godot: o que foi feito, como e por quê,
com os defeitos medidos e as regras que nasceram de cada um. **Não é lista de
tarefas** — é o mapa do que o Unity precisa alcançar e das armadilhas já pagas.
Os caminhos `godot/...` apontam para `mobile-godot/godot/`.

### (leva 6, 27/08 — histórico do Godot) mapa 4x maior, tempestade de battle royale, nada mais fixo

**27/08, tarde (leva 6).** APK do teste:
`godot/build/testes/arkana-2026-08-27_1245.apk` (141 MB) — **tem a ilha nova**.
Portao 12/12 em cada passo, com vermelho provado.
Branch `claude/whatsapp-video-review-799b24`.

#### 1. A ILHA QUADRUPLICOU DE AREA — e ficou 27% mais BARATA na tela (`99bb7d8`)

300 -> **600 m de lado**, raio de terra 132 -> **264 m**. Um knob so':
`Island.ESCALA` (2.0). A escala e' **HORIZONTAL**: o relevo nao e' novo, e' a
MESMA ilha amostrada com metade da frequencia — colinas 2x mais largas, e a
malha de 132x132 quads continua acima do Nyquist (quad 2,27 -> 4,55 m; detalhe
mais fino 12 -> 24 m). Colisao: **exatamente as mesmas 34.848 faces**.

MEDIDO headless em 27/08 (nao estimado):

| escala | lado | area | build | colisao | tris NA TELA | draws NA TELA |
|---|---|---|---|---|---|---|
| 1.0 | 300 m | 1x | 610 ms | 34.848 | 137.432 | 67 |
| **2.0** | **600 m** | **4x** | **1403 ms** | **34.848** | **184.967** | **67** |
| 2.5 | 750 m | 6,2x | 2028 ms | 34.848 | 194.216 | 67 |
| 3.0 | 900 m | 9x | 2876 ms | 34.848 | 237.086 | 67 |

O APK anterior desenhava **253.562** tris por quadro. A ilha nova, com 4x a
area, desenha **184.967**. **2.5 e 3.0 estao medidos e a um caractere de
distancia** — o que segura nao e' o quadro, e' carga e memoria.

**O defeito que apareceu no caminho e valia por si so':** o culling por celula
da grama **nao funcionava na horizontal**. As celulas tinham a transformada do
mundo assada nas instancias e o NO' ficava em (0,0,0) — `visibility_range` mede
a distancia da camera ate' a ORIGEM DO NO'. O corte era tudo-ou-nada e o unico
eixo em que funcionava era o vertical. Invisivel numa ilha de 132 m (a camera
nunca fica a mais de 80 m do centro), fatal em qualquer mapa maior. Corrigido:
cada celula no seu lugar, instancias em coordenada local. Seixo, moita, flor e
junco entraram na mesma grade.

**A licao paga pela 4a vez nesta fase:** metro cravado envelhece calado.
Viraram fracao — as varinhas do loot, a espiral de busca do loot (fracao do
raio do POI: no mapa novo a beira seca do brejo foi para 96 m e **dois loots
deixaram de nascer, calados**), o anel do Bau Celestial e os 14 nascimentos do
`Island.tscn`. `Island.pois()` agora entrega **centro E raio**.

#### 2. A TEMPESTADE VIROU BATTLE ROYALE DE VERDADE (`1643168`)

Ordem do Diretor, e cada numero conferido contra PUBG, Apex e Fortnite antes de
entrar — o estudo esta' em **`docs/referencias/ZONA-BATTLE-ROYALE.md`** (as
tres tabelas reais, as seis leis que os tres compartilham, e o que ainda falta).

- **NA QUEDA NAO HA' LIMITE.** A zona nascia LIGADA no `_ready`: com o mago
  ainda no castelo, a fase 1 ja' contava. Agora nasce **inerte** (parede
  invisivel, nenhum cronometro, dps zero) e **liga no sinal de pouso**. Mesma
  lei que a suprema ja' seguia.
- **1:10 de mapa aberto**, a tempestade **se forma** em torno da ilha e para na
  costa, e so' entao comecam as janelas **1:00, 50, 40, 30**. A janela ENCOLHE
  de fase em fase — e' o que faz a partida acelerar (Fortnite faz igual).
- **OS CIRCULOS DEIXARAM DE SER OS MESMOS.** `SEED_ZONA` era fixo em 2707:
  toda partida da historia do jogo teve os mesmos cinco circulos nos mesmos
  lugares. **A rota do castelo tinha o mesmo defeito** (`SEED_ROTA := 3103`).
  Os dois sorteiam por partida agora. O determinismo mudou de escopo: era entre
  partidas, virou DENTRO da partida (um seed gera o plano inteiro de uma vez, e
  fica guardado em `Zona.seed_da_partida` / `Castelo.seed_da_rota` para a rede
  transmitir).
- **A ULTIMA FASE FECHA EM ZERO.** Antes parava em 4 m: existia refugio
  permanente e a partida podia acabar por cronometro com dois vivos.
- Os raios viraram **fracao do raio do mapa** e a regua morreu (`RAIO_INICIAL`,
  `ILHA_REF` e `escala_do_mapa()` sairam; entrou `raio_do_mapa()`, que le'
  `Island.LAND_R`). **0,62 e' 62% do mapa em 300 m e em 2.400 m.**
- `Balance.MATCH.duration_s`: 180 -> **480 s** (a tabela soma 396 s depois do
  pouso; com 180 a partida morria no meio da fase 2).

#### 3. A POPULACAO DOBROU, com a conta na mesa (`Balance.MATCH.bots` 6 -> 12)

Quadruplicar o mapa sem mexer na populacao divide a densidade de encontro por 4.
MEDIDO headless — CPU de fisica por passo, teto de 60 fps = 16,67 ms:
**6 bots 1,684 ms · 12 bots 2,592 ms · 20 bots 4,207 ms · 30 bots 7,630 ms**
(marginal ~0,25 ms/bot). Dobrar custa 0,9 ms = 5% do orcamento.
**12 tambem e' o teto estrutural de hoje:** o `Island.tscn` publica 14
nascimentos e o Main pede `1 + bots` pontos DISTINTOS; passar de 13 faz dois
magos nascerem um dentro do outro. Ha' teste vermelho provado para isso.

#### 4. O CAJADO VOLTOU PARA O SEU POI (`a96d8e6`) — tres defeitos vivos no APK

Achados ao planejar o mapa grande, e medidos com a ilha real:
- `Loot.POIS` era **copia congelada** dos POIs da ilha de 180 m: o cajado das
  ruinas nascia a **46 m** do centro de um plato de 20 m de raio, ou seja FORA
  dele. O contrato "1 cajado por POI" (GDD 14) estava quebrado.
- Um corte de raio cravado em **70 m** num mapa de raio 132 deixava **47% da
  ilha sem loot nenhum**.
- A janela de altura `1,4 a 8,5` estava cravada em TRES arquivos (Loot, Zona,
  BauCelestial), cada um comentando "mesmo corte do outro" — e as tres erravam
  identico: o teto proibia o **plato das ruinas (9,0 m exatos)** e o **topo do
  pico (28 m)**. Os dois unicos POIs com altura de verdade eram os dois onde
  nada podia nascer: nem loot, nem bau, nem circulo final.
Correcao: **`Island.pode_pousar(x, z)`** — chao seco acima da praia, DERIVADO
de `agua_y()`. Sem teto de altura, de proposito.
**Por que o teste nao pegou:** testava contra uma ilha FALSA E PLANA
(`height()` = 3,0 sempre). O teste novo instancia `world/Island.gd`.

---

### O QUE FALTA — em ordem de quem esta' bloqueando

**BLOQUEIO 1 — medir FPS no celular.** Nao e' codigo, e' um cabo USB
(`adb devices` vazio). E' o portao que este projeto deve desde 25/08 e o que
libera: `Island.ESCALA` 2.5/3.0 (ja' medidos), mais bots, e as ondas 3 e 4 do
`docs/cenario/MAPA-GRANDE-PLANO.md` (chunking ate' 2,4 km).

**BLOQUEIO 2 — creditos de imagem.** Os 18 prompts de personagem estao prontos
(`docs/prompts/personagens/`, 20 arquivos) e a fila e' Veu -> Tessa ->
Ceifadora -> ... A geracao 2D esta' **travada: a plataforma de imagem esta' sem
creditos** (saldo 0,07; ~2 creditos por vista, 3 vistas por mago = ~108 para os
18). Os ~2.964 creditos da Meshy sao de OUTRA conta e servem para 3D, nao para
as vistas. Caminhos: recarregar a plataforma de imagem, gerar as vistas no
Firefly (o Diretor tem acesso) ou ir direto de Texto-para-3D na Meshy (mais
barato em passos, pior em qualidade e contra o pipeline aprovado).

**PENDENCIA — o gesto de disparo da Pyra.** Continua o unico item aberto dela
(detalhe na leva 5, abaixo). As 6 variantes gratuitas de "Mage Spell Cast" sao
todas conjuracao de aura com bracos para cima — o diagnostico do Diretor esta'
certo. Melhor candidato pela biomecanica: **"Soco para Frente com Ambas as
Maos"**, aplicado mas nao julgado (a camera do viewer travou num angulo que nao
deixa avaliar a pose, e nao se aprova animacao que nao se viu). Video de
referencia preflightado em **32,5 creditos**, nao gasto. Prompt para o Firefly
entregue ao Diretor.

**DECISOES QUE ESPERAM O DIRETOR**
1. **Veto ou nao** do circulo final no plato e no pico (o teto de 8,5 m que
   proibia caiu; achei melhor liberar — final em terreno alto e' padrao do
   genero, mas e' decisao dele).
2. **Direcao de arte dos 18** (`docs/prompts/personagens/00-FILA.md`): a escolha
   por evidencia foi escultura 3D "stylized premium" — o grupo do Brok
   APROVADO; a Pyra REPROVADA estava no grupo de pintura. Um martelo so' libera
   as 18 geracoes.
3. **Chunking e 20 pawns** (`docs/cenario/MAPA-GRANDE-PLANO.md` §8), depois do
   BLOQUEIO 1.
4. Do papel de 26/08, ainda em aberto: **lista dos 10 magos do lancamento**,
   **nome e valores da moeda**, e **quais pares de fusao estreiam**.

---

### (leva 5, 27/08 — histórico do Godot: as 5 ordens do Diretor viraram codigo e papel)

**27/08, madrugada (leva 5).** APK do teste:
`godot/build/testes/arkana-2026-08-27_0836.apk` (141 MB). Portao 12/12 em cada
passo, com vermelho provado. Branch `claude/whatsapp-video-review-799b24`.

1. **A TELA DE BR** (`ec8b0cb`) — tres ordens numa raia:
   - o rotulo da arma equipada **SOME** depois de 2,5 s (era painel pendurado a
     partida inteira: *"pode apagar este texto da manopla para ser visto de
     cima"*). Confirmacao virou EVENTO; quem quer saber o que tem na mao olha o
     botao de ataque, que diz o elemento.
   - **passar por cima de uma luva/manopla anuncia NOME + ELEMENTO** ("Luva
     Comum · AGUA", "Manopla · AGUA + RAIO"). `Textos.arma_rotulo` e' o dono
     unico do formato — o botao de PEGAR e a confirmacao nunca divergem.
   - **de maos nuas NAO existe elemento na tela**: carrossel dos 5 escondido e
     botao de ataque CINZA e sem rotulo. Equipar e' o que acende. (O teste
     antigo cobrava a lei antiga — "carrossel visivel de maos nuas" — e foi
     reescrito para a nova.)
2. **A SUPREMA SO' CARREGA NO CHAO** (`52a2aa4`): a guarda mora no ponto unico
   (`_carregar_suprema`), entao fecha os dois canais (tempo e dano) de uma vez.
   O gancho `Queda.no_ar()` ja' existia escrito para isto.
3. **AS MANOPLAS REDESENHADAS NO PAPEL** (`992e0c3`) —
   `docs/design/MANOPLAS-FUSAO.md`: a manopla passa a conjurar UM ataque
   FUNDIDO por par (o **Tufao de Brasas** = fogo+vento, o exemplo do Diretor),
   nao dois tiros alternados. Os 10 pares herdam a identidade dos combos de
   Sintonia que o Roblox ja' tem, em escala de ARMA (~1/4 do raio): a manopla
   **vende** a Sintonia, nunca a substitui. Leitura pela lei do Spellbreak
   (FORMA de um + PALETA do outro); efeitos SO' com as reacoes de DANO.md.
4. **O ELENCO E A MOEDA** (`992e0c3`) — `docs/design/DESBLOQUEIO-ELENCO.md`:
   10 jogaveis no lancamento, 10 desbloqueaveis por moeda ganha em partida
   (a resposta direta a licao de retencao do Spellbreak), tudo offline/local —
   o jogo nao tem uma linha de rede. Inclui o plano de producao 3D dos 18 que
   faltam, com o portao de auditar as 4 vistas ANTES de gastar credito.
   **Os dois docs esperam o martelo do Diretor** (lista dos 10, nome da moeda,
   valores, e quais pares de fusao estreiam).

**PYRA — aprovada, riggada e com 11 clipes; falta SO' o gesto de disparo.**
Estado no site (conta dele): geracao nativa multi-view aprovada -> **remesh 10.360
faces / 4.992 vertices** (gratis) -> **rig Humanoide com altura 1,78** ->
**11 clipes aplicados**: Planar (glide), Planar horizontal v2, Agachar-se
Pegar, Andando, Caindo, Correndo, Gemido (derrubado), Mage Spell Cast, Nadar
Parado, Nadar para frente, Parado 1. **NAO foi baixada ainda** — falta o cast.

**O CAST E' O UNICO ITEM ABERTO (ordem do Diretor, 27/08):** *"o movimento de
spell magic e' mais para magias que vem do chao e nosso objetivo e' disparo de
magia... quero que ele fique com a mao estendida ou simulando jogando algo como
na vida real, de filmes, animes"*. Duas geracoes por Texto para Motion (20
creditos) NAO acertaram: a 1a fez arremesso por cima da cabeca + agachada, a 2a
levantou o braco ao lado da cabeca (aceno). As duas foram descartadas.
Caminhos que sobraram, em ordem de custo:
1. **as 5 variantes gratuitas** da biblioteca (Mage Spell Cast 1..5) e os
   vizinhos "Atacar" / "Correr e Atirar" / "Tiro Lateral" — aplicar e olhar
   custa ZERO credito;
2. **Video para Motion**: o Diretor grava 3s fazendo o gesto (ou um clipe de
   referencia) e o Meshy converte — e' o caminho mais certeiro para um gesto
   especifico;
3. outra tentativa de texto com fraseado diferente (10 creditos, incerto).
**Saldo: 2.964.**

**Ao baixar (depois do cast resolvido):** rigged + todos + arquivo unico. Ai vem
uma armadilha JA' conhecida: os clipes de IA (os planars e o cast novo) entram
no .glb com **nome UUID**, nao com o nome que se digita — o `Mage.gd` resolve
por ALIAS, entao e' obrigatorio um passe no Blender renomeando UUID -> alias
(cair/planar/pegar/derrubado/nadar/cast) antes de virar `pyra.glb`. Sem isso o
`cast` nao resolve, e sem `cast` o Mage REJEITA o modelo externo e cai no mago
procedural — a Pyra sumiria de novo. O `tools/blender/fundir_animacoes.py` ja'
faz esse mapeamento por substring; para ela e' SUBSTITUICAO (esqueleto novo),
nao fusao.

**Armadilha nova do navegador, anotada:** a janela do Chrome mudou de escala e
os cliques por coordenada passaram a desviar — no Meshy, clicar por `ref` do
elemento (find) e' o unico caminho confiavel. Foi assim que o painel do
remesh finalmente abriu.

---

### (leva 4, 26/08 — histórico do Godot: a leva Spellbreak aplicada; 4 raias, fila visual atacada, Pyra em rig no site

**26/08, madrugada (leva 4 — 4 raias em paralelo, todas com vermelho provado
e portão 12/12).** APK do teste: `godot/build/testes/arkana-2026-08-26_2249.apk`
(141 MB — modelos + tudo desta noite).

1. **NADAR tem animação própria** (`b7bf4fb`): a locomoção consulta a água na
   fonte (`locomotion_anim`) — bracada = `nadar`, boiar = `nadar_parado`;
   contrato completo no Mage com fallback `run` (quem não tem o clipe fica
   como era; o Brok já tem). E o **treino agora diz "TREINO"** no canto, não
   "3:00/BOTS 0" (pendência do 1º vídeo).
2. **O loot ACENDE** (`926dcbc`, receita Spellbreak): corpo do item emissivo
   na cor do elemento, POR INSTÂNCIA — a luva na mão nasce limpa (contra-prova
   no selftest). Achado: o glb Meshy vem com `emission_enabled=true` e emissão
   PRETA — "já é emissivo" era falso-positivo. Knob: `Loot.EMISSAO_CORPO 0.35`
   (se ainda ler escuro no aparelho, sobe o knob, não mexe em mais nada).
3. **Muralha de Brasas virou FOGO** (`8c8c096`): cunha emissiva com gradiente
   vermelho→amarelo, ≤48 partículas, flicker em PASSOS de 0,08 s (a lição dos
   12–15 fps), OmniLight removida. Gameplay intocado.
4. **Muro de Terra virou PEDRA** (`4404168`): ArrayMesh único compartilhado
   (noise determinístico seed 907, topo irregular, facetado como as rochas da
   ilha) + albedo NoiseTexture2D — zero binário. Colisão EXATA.

**PYRA RECRIADA NO SITE — aguardando o VEREDITO do Diretor.** O upload do
glb antigo foi riggado e chegou a receber clipes, mas o Diretor REPROVOU no
olho ("longe de ficar boa") e mandou recriar como as peças-herói. Feito:
geração nativa Multi-View na conta dele (30 créditos, saldo 2.994) com
frente + perfil esquerdo + costas das vistas aprovadas (a "3/4" NÃO entrou:
é a frontal repetida e ensinaria o perfil errado), Meshy 7 + Ultra + Textura
+ Pose A-Pose + licença PRIVADO. Resultado: 1,97 M faces, revisado no viewer
com zoom — braço-manopla de bronze em chamas no lado ESQUERDO certo, rosto
com as marcas de lava, capelete queimado nas costas, braçadeira no braço
direito. **Está na área de trabalho dele, primeira posição da galeria.**
Quando ELE aprovar: remesh ~15k (grátis no site) → rig (altura 1,78!) →
os mesmos clipes do Brok (biblioteca: Mage Spell Cast, Caindo, Nadar ×2,
Agachar-se Pegar, Gemido, Parado 1 + os 2 Movimentos de IA já pagos) →
download rigged/todos/arquivo único → SUBSTITUIÇÃO inteira do `pyra.glb`
(esqueleto novo ≠ repo) → characters/selftest (1,78 m) + clipes no motor.
Os DOIS uploads antigos dela (T-pose e riggado) seguem na galeria — remover
só se o Diretor mandar.

**Perguntas abertas ao Diretor:** (a) os rótulos de bússola ("Manopla · ÁGUA
+ RAIO") lendo do outro lado do mapa são wayfinding proposital ou poluição?
(b) manopla emissiva sai na cor do 1º elemento do par — quer a cor do par?

---

### (26/08, noite — leva 3: Brok animado, castelo voando, 3 defeitos, histórico)

**26/08, noite (leva 2 — atualiza o bloco abaixo).** Tudo na branch
`claude/whatsapp-video-review-799b24`, portão 12/12 em cada passo:

1. **O Brok tem os 13 clipes** (`888567f`). Da biblioteca Meshy (grátis):
   `cair` (falling_down), `nadar`, `nadar_parado`, `pegar` (agachar-pegar),
   `derrubado` (gemido no chão), `ande_agachado`. Por **Texto para Motion**
   (20 créditos, autorizados): `planar` (barriga, pose de paraquedismo — saiu
   ÓTIMO no retarget) e `mergulho` (de cabeça — guardado para o mergulhar da
   água). Fusão via `tools/blender/fundir_animacoes.py` (novo, headless,
   reutilizável): **o export "Todos Adicionados" da Meshy OMITIU o
   `mage_soell_cast`** — por isso NUNCA substituir o glb do repo pelo download;
   sempre fundir. Esqueletos conferidos idênticos (24 ossos). Aliases exatos:
   o `Mage.gd` adota sozinho, com laço automático em cair/planar/derrubado.
2. **A abertura agora é o castelo VOANDO** (`a04ec09`, ordem dele por texto):
   no trajeto, o pivô da câmera larga o ombro (o mago viaja pendurado SOB a
   rocha — era ESSA a causa do breu) e vai ao coração do castelo com o braço
   a 90 m e sem colisão; o arrastar de olhar orbita a ilha voadora. No salto,
   volta ao ombro. Provado no selftest da queda.
3. Os 3 defeitos do vídeo (kill feed batizado, BOTS 0 na vitória, pausa na
   tela) — ver leva 1 abaixo. **Saldo Meshy: 3.004.**

**Próximo (em ordem):**
- **APK novo para o Diretor testar** (build disparado no fecho desta leva —
  conferir `godot/build/`): abertura do castelo, queda/planar do Brok, pausa,
  kill feed com nome, BOTS 0.
- **Pyra: mesma receita do Brok** — ela NÃO está na galeria do site (página 1;
  provavelmente veio da API). Caminho: Carregar o `pyra.glb` do repo no
  webapp → Animar → mesmos clipes da biblioteca + os 2 Movimentos de IA já
  PAGOS na conta ("Planar horizontal v2" e "Planar (glide)" aplicam em
  qualquer modelo) → download → `fundir_animacoes.py`. ~15 min de navegador.
- **Código novo que os clipes destravam**: contrato `nadar`/`nadar_parado` no
  Pawn/Mage (a água hoje não troca animação), `mergulho` (botão novo, GDD),
  `ande_agachado` no agachar, e o gesto do baú sustentado com o `pegar` real.
- O quadro geral da leva 1 (respostas do teste, dossiê, fila visual) está no
  bloco logo abaixo — continua valendo.

---

### (26/08, noite — leva 1: 2º vídeo avaliado, dossiê Spellbreak, histórico)

**26/08, noite.** O Diretor testou o APK 16:50 (o primeiro com os modelos Meshy
dentro) e mandou vídeo de **partida completa** (130 s, VITÓRIA). Branch desta
leva: `claude/whatsapp-video-review-799b24` (commits `4fa9755` + `522ba4d`).

**As 3 respostas que a fase anterior pediu, tiradas do vídeo:**
- **Castelo na abertura: metade.** Os ~1,5 s iniciais são a rocha DELE em sombra
  ocupando a tela inteira (quase preto); olhando de baixo funciona como
  silhueta dramática e o brilho dourado central lê bem. Conserto sugerido:
  câmera nascer enquadrando o castelo contra o CÉU + luz de preenchimento/
  emissiva (clima com COR, nunca falta de luz).
- **Luvas no chão: de perto leem MUITO bem** (a manopla é a melhor peça do
  lote); **de longe viram vulto preto** — quem lê à distância é o feixe e o
  rótulo. E o rótulo lê longe DEMAIS (sem corte por distância, gruda na borda).
- **A queda "estranha" — dívida paga:** ela cai em pose de idle/corrida a queda
  inteira (os .glb não têm clipe de cair/planar; os aliases de `Mage.gd`
  esperam), câmera 90° para baixo (sem horizonte = sem velocidade), personagem
  em contra-luz. Correção por custo: clipes Meshy → inclinar câmera → vento/FOV.

**3 defeitos do vídeo consertados com prova em vermelho (12/12 no portão):**
1. Kill feed vazava `@CharacterBody3D@1718` → bots batizados com nome de mago
   no spawn (`Main.gd`, `Kits.de(slug).nome`).
2. VITÓRIA com "BOTS 1" congelado → `_end_match` leva o placar final à HUD.
3. **Botão de PAUSA nascia 58 dp ABAIXO da tela** (o `_canto` ancora tudo no
   rodapé e o `_layout` contava o topo do topo) → re-ancorado; o teste agora
   cobra o RETÂNGULO na tela (o `visible=true` era assert decorativo).

**Dossiê Spellbreak** em `docs/referencias/SPELLBREAK.md` (pedido do Diretor):
mecânicas, combos (referência da Sintonia: combo = FORMA de um elemento +
PALETA do outro), VFX toon com ramp + partículas 12–15 fps (resposta para
muros chapados), **loot emissivo/auto-iluminado (resolve o vulto preto)**, a
queda deles freia com a própria levitação, 4 lições do fracasso (retenção,
MMR de novato, exclusividade, dono do IP). Community Version oficial no
itch.io é referência jogável legal; extrair asset é proibido.

**Meshy (webapp, conta dele) — clipes de animação NO MEIO:**
- **"Caindo" foi ADICIONADO ao Brok de jogo** (o riggado de 15.424 faces).
- Já localizados na biblioteca (grátis): "Nadar Parado", "Nadar para frente",
  "Agachar-se, Pegar", "Pular para Pegar e Cair", "Cair", "Tiro e Cair p/
  Frente". **PLANAR NÃO EXISTE na biblioteca** → o Diretor autorizou gerar por
  "Texto para Motion" pagando créditos.
- **INTERROMPIDO no meio:** a janela do Chrome encolheu (Diretor na máquina).
  Retomar: janela restaurada → meshy.ai/workspace → Animar → Brok já fica
  selecionado. Saldo intacto: **2.984**. Depois de baixar: fundir clipes nos
  .glb (os aliases adotam por nome) e repetir para a Pyra (conferir se a Pyra
  está na conta do site — pode ser só da API, aí é Carregar o FBX riggado).
- Atenção ao baixar: apareceu um selo "+50" junto do botão de exportar no
  viewer — conferir O QUE custa 50 antes de clicar (a regra era rig/animação
  grátis no webapp).

**O vídeo também re-confirmou a fila visual (§10):** muro de terra caixote,
muralha de brasas chapada, volumes amarelos do baú, Pyra escura em contra-luz
(candidata a rim/fill light), horizonte lendo como prédios, e os ~3 s iniciais
quase pretos da queda.

---

### (26/08, noite — os modelos do Meshy entram no jogo, histórico)

**26/08, noite.** A esteira do Blender rodou e os 5 heróis entraram no APK:

- **`tools/blender/otimizar.py`** (headless): decima preservando UV (o
  normal/albedo do Meshy continua valendo — sem re-bake), escala para METROS
  reais e põe o pé em y=0. Resultados: castelo 30k tris/52 m · luvas 3,5–5k
  em tamanho real (0,32–0,46 m) · baú 8k **nas medidas do código** (1,15 m —
  colisão e canalização intocadas).
- **`core/Pbr.gd`** — o grampo do metal virou dono único (a lição do mago
  preto): `Pbr.domar()` em todo modelo Meshy instanciado.
- **Integração com fallback defensivo em tudo**: `Castelo._montar_visual`
  veste `world/modelos/castelo.glb` (primitivas viram `_montar_fallback`);
  `BauCelestial._montar` veste `bau.glb`; `ArmaSlot.modelo()` veste
  `gameplay/modelos/luva-<id>.glb` — sem arquivo, o procedural de sempre.
- **Teste da lei antiga atualizado**: o selftest da queda cobrava "zero
  binário" no castelo — a DIREÇÃO §10 mudou a lei; agora cobra o modelo
  vestido E o fallback vivo (chamado direto). Vermelhos: caminho quebrado (3),
  Pbr removido (1).
- **APK: 140 MB** (era 53) — os 5 GLB com texturas 2K. Ordem do Diretor de
  25/08 vale: tamanho não é critério agora; compressão vem na fase de loja.

**Pendências REGISTRADAS da fase:** recolor por elemento das luvas-modelo
(hoje usam as cores master; a runa procedural é quem colore por elemento no
fallback) · decal do bordado na palma da Conjurador (Blender manual) ·
castelo saiu 52 m de ALTURA (o Meshy esticou a rocha; ficha era 34 m — ajustar
escala/eixo na passada fina) · **FPS no Poco F4 continua sem medição — é o
portão da repaginação do chão (DIRECAO §10.1)**.

---

### (26/08, fim de tarde — 5 heróis em 3D na conta, histórico)

**26/08, fim de tarde.** A fila inteira da DIREÇÃO §10 virou modelo: Castelo,
Luva Comum, Luva de Conjurador, Manopla e Baú — todos pelo SITE, visíveis na
galeria dele, Multi-View das vistas ortogonais aprovadas. Placar e ressalvas em
`cenario/00-MODELOS-3D.md`. **210 créditos no dia · saldo 2.984.**

**A regra que nasceu no meio, por correção DELE:** revisar o modelo NO VIEWER
antes de baixar. Ele pegou dois defeitos na Conjurador v1 (gema extra no
anelar + palma sem os símbolos) que eu ia deixar passar. A v2 corrigiu a
geometria (4 cristais exatos, contados em zoom); a palma lisa persistiu em 2/2
gerações → **o bordado vira decal no Blender** (determinístico, grátis), não
terceira loteria de 30 créditos.

**Pergunta dele respondida:** as 5 cores da Luva Comum NÃO são 5 modelos — é
UM master (o vermelho) + recolor de material no motor, como as luvas
procedurais já fazem. 5 gerações custariam 5× e dariam 5 geometrias.

**Blender INSTALADO (winget).** A esteira da próxima fase, por peça: decimar
(~2 M → alvo de jogo) + bake de normal + decal da palma (conjurador) → GLB de
jogo em `godot/world/modelos/` (git/LFS) → pendurar nas cenas (castelo em
`Castelo.gd`; luvas em `ArmaSlot.modelo()` no lugar do procedural; baú em
`BauCelestial`) → colisão simples à mão → **FPS no aparelho** (DIRECAO §10.1).

**Aba do Meshy deixada ABERTA no Chrome dele** — os processos estão todos lá.

---

### (26/08, fim de tarde — o castelo 3D nasce, histórico)

**26/08, fim de tarde — o primeiro modelo de cenário nasceu.** E uma ordem do
Diretor mudou o processo no meio: *"quero ver todos os processos lá no site,
a arte e tudo disponível na minha área de trabalho da plataforma"*.

- **Verificado no navegador: tarefa de API NÃO aparece no workspace Meshy.**
  A política nova está em DIRECAO.md §10.2: **peça-herói gera no WEBAPP, na
  conta dele** (processo visível); API só para validação/scripts.
- O castelo saiu pelas DUAS vias no mesmo dia (60 créditos ao todo, saldo
  3.134): o do **site é o oficial** ("Aetherstone Citadel", 1,9 M tris,
  texturizado, visível na galeria dele); o da API (30.917 tris) é backup e
  candidato a base low-poly. Ambos em `cenario/01-castelo-voador/origem/`
  (fora do git — a nuvem do oficial é a própria conta Meshy dele).
- **Próximo passo do castelo:** Blender (decimar OU retopo com bake do
  hi-poly) → `godot/world/modelos/castelo.glb` → pendurar em `Castelo.gd` →
  FPS no aparelho. Detalhes em `cenario/01-castelo-voador/00-MODELO.md`.
- **A repaginação do CHÃO tem plano e portões** (pergunta dele, respondida):
  DIRECAO.md §10.1 — o chão é a 3ª onda, depois dos heróis e do FPS medido,
  em três sub-ondas (árvores/pedras → splat do terreno → água), cada uma com
  FPS antes/depois. Herói é um objeto; o chão é o mundo.
- Fila do Meshy no site: **Luvas** (3 modelos) → **Baú**.

---

### (26/08, tarde — concept arts validadas, histórico)

**O ciclo de arte fechou o primeiro loop completo** (26/08, tarde): prompts em
`docs/prompts/` → o Diretor gerou 55 imagens no ChatGPT → validação contra os
critérios de cada ficha → **54 aprovadas, 1 para regerar** → transportadas
para `cenario/<peça>/arte/` (o espelho de `personagens/`), 112 MB no LFS.

- **Veredito e lupa:** `cenario/00-VALIDACAO.md` — inclui a checagem legal
  anti-FMA da palma da Conjurador (passou) e o critério mais fino da Manopla
  (polegar+médio em ouro, os dedos do estalo — exato).
- **A reprovada:** a key art do Altar (48) sem os dois obeliscos tombados — o
  texto de ajuste está no 00-VALIDACAO, pronto para colar no gerador.
- **Custo LFS atualizado:** ~470 MB totais → ~2 clones limpos/mês na cota.
- **Próximo passo da arte:** Castelo → Meshy multi-imagem (frente-selo +
  lateral-porta-salto + costas + tres-quartos). Depois: Luvas → Baú.

---

### (26/08, tarde — o vídeo do Diretor avaliado, histórico)

O Diretor testou o APK 12:20 e mandou vídeo (55 s, treino). **Quatro defeitos
reais saíram dele, três já corrigidos com teste vermelho:**

1. **"BRAÇO FRIO" preso na tela** (do segundo 2 ao 55). Causa: a Pyra ligava o
   chip com `avisar_estado` direto — fora do relógio de estados, ninguém
   desligava. Agora `ligar_estado(esfria_dur)`: expira e avisa a HUD.
2. **A personagem anda como SILHUETA PRETA.** Causa MEDIDA: o PBR da Meshy vem
   com `metallic = 1.00`, e metal é quase todo reflexo — sem reflection probe o
   mobile devolve breu. `Mage._domar_pbr` grampeia metallic ≤ 0.2 / roughness
   ≥ 0.45 na importação.
3. **O carrossel MENTIA**: mostrava VENTO e o tiro saía FOGO (o elemento agora
   é da luva). Luva de elemento travado esconde o carrossel; mãos nuas devolvem.
4. **"Caixote bege gigante"** no 0:11 — não é bug: é o MURO do elemento TERRA.
   Feio de doer; entra na fila visual (Meshy §10).

**O que mais entrou nesta leva:**
- **O elemento MORA NA LUVA** (decisão dele): loot cicla os 5 pelo mapa,
  pegar/trocar viaja com o elemento, o disparo ignora o carrossel.
- **Bastões viraram LUVAS** na tela (punho/dorso/dedos procedurais, cor do
  elemento, silhueta por tier). Modelo definitivo: Meshy (peça nº 2).
- **PAUSA em partida** (pedido do vídeo + GDD §12): ícone no canto superior
  direito → RETOMAR / CONFIGURAÇÕES (o MESMO menu/Config.gd) / ABANDONAR.
- **O braço desce**: pedir `cast` de novo REINICIA o clipe (antes o disparo
  contínuo subia o braço no 1º tiro e congelava — a queixa literal dele).

**Pendências NOVAS do vídeo/feedback (por ordem do que ele falou):**
- A QUEDA "ainda está estranha" — sem detalhe suficiente; pedir o que
  exatamente (pose? velocidade? câmera?) ou vídeo da queda.
- Gesto do baú: agachar e FICAR (canalizar sustentado), não agachar-levantar
  em loop. Pede clipe próprio (Meshy anim library / Blender).
- Biblioteca de animação Meshy: baixar cair/planar/pegar/derrubado/nadar para
  Pyra e Brok (grátis no webapp); o que não existir, animar no Blender.
- Layout dos botões AJUSTÁVEL pelo jogador (edição de posição na tela).
- Timer "3:00" e "BOTS 0" aparecem no treino — esconder/trocar por "TREINO".
- Sons com profundidade: magias, água, corpo (o Diretor pediu; hoje é síntese).
- Silhueta do cenário de fundo lê como PRÉDIOS no horizonte (q04/q06) —
  quebra fantasia; revisar as rochas de fundo.

**Concept art:** o Diretor gera as imagens por IA ANTES de qualquer crédito
Meshy. **Os prompts completos moram em `docs/prompts/`** (pasta única, ordem
dele): 8 peças, cada uma com ficha física em metros, prompt mestre, 4 vistas
separadas (a lição do painel plano de 21/08), negative e critérios de
aprovação. Regra: imagem aprovada → multi-image no Meshy.

---

### (26/08, tarde — as 4 fases da DIREÇÃO, histórico)

Ordem do Diretor: *"faça todas as fases necessárias"*. Quatro fases entraram,
cada uma com teste provado em vermelho:

**1. A Lei das Luvas.** Todo slot nascia com varinha — "desarmado" não existia
no jogo. Agora TODOS caem de mãos nuas; sem luva não há ataque básico (tática/
suprema são natas); o bot desarmado CAÇA a luva mais próxima (grupo
`loot_arma`) e o auto-upgrade equipa na chegada; o player NÃO tem auto-pegar
(decisão nº 5 — o botão vira TROCAR com luva na mão); loot pego de mãos nuas é
CONSUMIDO (o swap deixaria "arma vazia" fantasma no chão). Nomes de exibição:
Luva Comum / Luva de Conjurador / Manopla — ids internos intactos.

**2. O lobby de TREINO** (decisão nº 19). Botão no menu; mesma cena da partida
com pedido via static consumida no `_ready` do Main (sobrevive à troca de cena;
o consumo impede herança — a prova em vermelho derrubou 5 testes em cascata sem
ele). Sem zona, sem relógio, sem queda; as 3 luvas expostas no spawn; 2 bonecos
que regeneram; suprema em 5 s. Tutorial não-obrigatório: fase futura.

**3. O castelo cruza o céu VAZIO** (decisão nº 14). O corpo fica invisível de
`_ready` até `saltar()` — surge do nada, sancionado. Vale para player e bots
pelo mesmo caminho.

**4. A água deixou de ser cenário.** Lâmina no peito (1,2 m) → nadar: 55% da
velocidade no PRODUTO ÚNICO, corpo FLUTUA (gravidade não puxa ao leito). Sair
= roupa encharcada (80% por 2,5 s). Quem responde onde há água é a ilha
(`agua_y`), pelas MESMAS cotas que desenham a lâmina (agora constantes).
**Pendente da água:** MERGULHAR para esconder (pede botão novo), atirar só na
superfície, animação de nado.

**Ainda sem código, da DIREÇÃO:** braço ergue/abaixa + recarga da luva
(animação); bots usando tática/suprema; sons de passos/respiração (os canais de
percepção já existem — falta o áudio); tutorial básico.

**Armadilha de teste registrada:** `queue_free()` num Main de teste deixa a
ilha REAL viva (adiada) no grupo "ilha" até o fim do frame — o teste seguinte
pergunta a lâmina para a ilha errada. Em teste, `free()` imediato.

---

### (26/08, meio-dia — a DIREÇÃO nasce, histórico)

O Diretor respondeu a entrevista de 20 perguntas sobre luvas, habilidades,
castelo, água e bots. **Tudo está em [DIRECAO.md](DIRECAO.md)** — até ele colar
as emendas no GDD, aquele arquivo é a palavra dele por escrito e nenhuma
implementação pode contradizê-lo. O que já virou código, com teste vermelho:

**a) FFA + percepção dos bots.** `Main.gd` cravava `b.target = player` no
nascimento — "mesmo sem me mexer eles já me notam" não era IA agressiva, era
ausência de percepção. Agora o bot nasce cego e NOTA por quatro canais com
contra-jogada: VISTO (< 12 m), OUVIDO (< 18 m só em movimento — ficar parado
esconde), DISPARO (< 30 m, `Bus.disparo` emitido por `Projectile.launch`, o
ponto único de todo tiro) e REVIDE (tomar dano ensina quem bateu). Todos contra
todos: bot caça bot.

**b) A suprema virou CARGA 0→100%.** Cooldown morreu. Começa VAZIA na queda
("senão fica muito roubado"), enche com o tempo (`suprema_carga` s por mago) e
acelera com dano CAUSADO (`Kits.CARGA_POR_DANO`, modelo Apex por ordem dele).
O botão da HUD mostra a porcentagem (`AcaoButton.mostra_carga`). Dano RECEBIDO
não enche — apanhar não é bateria.

**c) Táticas na faixa 5–10 s** (agressão no teto): Pyra 14→9, Véu 12→6, Tessa 7
(mantido — decisão anterior deliberada). Tabela dos 20 em DIRECAO.md §4.

**Um teste passava VACUAMENTE e foi consertado:** o "braço frio" da Pyra exigia
`tatica_cd >= 4 s` após a suprema — passava porque o cooldown antigo de 14 s
ainda estava correndo (sobrava 6,4 s) e mascarava o esfriamento. Com a tática em
9 s a sobra é 1,4 s e o `maxf` do esfria é quem levanta para 4 — o check agora
prova o que sempre disse provar (a tolerância virou 1 tique, não 0,01).

**O que a DIREÇÃO manda e AINDA NÃO tem código** (ordem sugerida):
1. Luvas como forma física (varinha/cajado → luva; PEGAR vira TROCAR com arma
   na mão; braço ergue ao disparar/abaixa no cansaço; recarga da luva).
2. Lobby de treino (decisão nº 19 — destrava o teste do próprio Diretor).
3. Castelo sem bonecos + surgir ao acionar (decisão nº 14).
4. Água: nadar do peito, tiro na superfície, mergulho esconde, saída lenta.
5. Bots nas leis do jogador: caem sem luva, acham loot, usam tática/suprema.
6. Sons de passos/respiração (a percepção já tem os canais; falta o áudio).

---

### (26/08, noite — animação derrubado, histórico)

**ACHADO QUE MUDA PRIORIDADE — leia antes de planejar arte 3D.**

Lendo o JSON dos `.glb` direto: **a Pyra tem TRÊS animações** (idle, run, cast) e
**o Brok tem CINCO** (Idle_02, Run_02, Running, Walking, mage_soell_cast).
**Nenhum dos dois tem `cair`, `planar` nem `pegar`.** Nos modelos 3D reais essas
animações caem no fallback: `pegar` vira `cast`, `cair` vira `idle`.

Consequência: **os dois personagens "prontos" em 3D são os mais pobres em
animação do elenco.** O gesto de agachar e a queda que o Diretor elogiou em
26/08 são do **mago procedural**, que 18 dos 20 personagens rodam. Cada modelo
novo da Meshy, do jeito que o pipeline está hoje, *piora* a expressividade em
troca de melhorar a malha.

**O que fazer com isso:** baixar os clipes que faltam da biblioteca de animação
da Meshy (grátis no webapp, segundo `docs/MESHY.md`) e reexportar. Os aliases de
`cair`/`planar`/`pegar`/`derrubado` já estão escritos em `Mage.gd` — no dia em
que o clipe chegar com qualquer nome conhecido, ele é adotado sozinho.

**O estado DERRUBADO agora tem corpo.** Antes o caído usava a animação de
locomoção: rasteja devagar, cai no lado "idle" da histerese, e ficava **de pé e
parado** no meio da partida. O aliado só descobria quem dava para reerguer pela
HUD. Agora: corpo no chão, apoiado num braço, e a cabeça sobe uma vez por volta
— o beat que diz "ainda dá tempo".
- **Onde a decisão mora:** `Pawn.locomotion_anim()`. Player e Bot reescrevem a
  animação todo frame; pedido feito de fora seria apagado no frame seguinte.
  `ArmaSlot.gesto` resolve isso para gesto **transitório** (volta por relógio);
  derrubado é **sustentado** e não tem quando voltar — estado sustentado se
  resolve na fonte da decisão, não driblando ela.

**Dois defeitos nas FERRAMENTAS, achados por usá-las:**
1. `_shot_mago.gd` fotografava só idle/run/cast. As animações escritas à mão
   (`cair`, `planar`, `pegar`) **nunca tinham sido olhadas renderizadas**.
2. A altura do personagem era a **maior malha isolada** — respondia 1,02 m para
   um mago de 1,83 m, e o enquadramento **cortava a cabeça**. Descoberto tentando
   julgar a pose nova: não dava para dizer se o mago estava caído ou de pé porque
   a régua estava errada. São dois casos e não há fórmula única: **malha skinada
   é desenhada pelo esqueleto** e o `global_transform` dela não participa do
   desenho (multiplicar por ele deu 0,018 m para a Pyra, que renderiza em 1,78).
   Regressão conferida: Pyra 1,78 e Brok 1,40, exatamente as fichas.

**A pose de `derrubado` foi reescrita DUAS vezes depois de fotografada** — a
primeira afundava no terreno, a segunda lia como em pé. Calibrar no escuro não
funciona; foi a régua consertada que permitiu acertar.

---

### (26/08/2026, dia — histórico)

**Duas decisões suas foram tomadas e executadas hoje:**
1. **PR #2 mesclado na `main`** (`f03f97e`). O CI rodou de verdade e passou —
   `12 selftests headless`, SUCCESS. Era a única coisa que nem você nem eu
   tínhamos observado até então.
2. **As 160 referências de arte foram para o Git LFS.** Elas existiam em UM lugar
   só, sem cópia, desde que os .zip da entrega foram apagados na higienização.
   Agora têm histórico e backup.
   **O custo, dito de frente:** um clone limpo passou a puxar ~350 MB de LFS. A
   cota gratuita do GitHub é 1 GB de armazenamento e 1 GB de banda por mês —
   cabe, mas são ~3 clones limpos por mês. Se estourar: pacote de dados pago ou
   storage externo. Não há terceira saída.

**O que foi feito depois disso, na branch `feat/artes-em-lfs`:**

**a) O Arkana passou a abrir com a cara dele.** Até hoje a PRIMEIRA coisa que o
jogador via era o **logo do Godot** — `project.godot` não tinha nenhuma chave de
`boot_splash`. E o ícone na gaveta era o retrato de corpo inteiro da Pyra num
quadrado de 96 px. Agora os dois são o **Selo**, o mesmo pentágono da tela de
título. *(Correção de registro: a nota antiga dizia que o ícone era o padrão do
Godot. Não era. Conferido abrindo o APK.)*
- **Como:** o Selo saiu de dentro de `Menu.gd` para `menu/Selo.gd`. Era classe
  aninhada, e quem gera os PNG roda como `--script`, antes dos autoloads — um
  preload de `Menu.gd` ali morre em "Identifier not found: Bus". `menu/_marca.gd`
  renderiza os 4 PNG a partir desse mesmo Selo, para o desenho continuar tendo
  um dono só. É ferramenta de ateliê, fora do APK.
- **Por quê assim:** a alternativa era redesenhar o selo num editor de imagem —
  e no dia em que você mudasse o dourado existiriam duas verdades divergindo em
  silêncio.
- A frente do ícone adaptativo sai em 64% do quadro: o Android mascara os
  432×432 e só garante o miolo.

**b) A sombra voltou a existir na fase que abre a partida.** Vista do castelo a
200 m, a ilha inteira não projetava UMA sombra. `directional_shadow_max_distance`
valia 60 m — calibrado quando a ilha tinha 180 m e ninguém caía do céu.
- **Errei duas hipóteses antes de achar**, e ambas foram descartadas por medida:
  o toon shader (cena de isolamento mostrou que ele recebe sombra igual ao
  material padrão) e o balanço de energia (subir o sol de 1,55 para 4,5 clareou
  tudo e não trouxe sombra nenhuma).
- **O conserto não é aumentar o número:** o atlas tem 2048 px e cobre o alcance
  inteiro. A 60 m o texel é ~6 cm; a 400 m vira ~20 cm e borra a sombra PERTO,
  onde o jogador passa 95% da partida. `world/Sol.gd` faz o alcance **seguir a
  altura** — abre no ar, fecha ao pousar. Um split só; nada reabre PSSM.
- A ferramenta de calibragem também mentia: mantinha o sol em repouso nas
  tomadas aéreas. Corrigida.

**c) `docs/infra/` — a pasta técnica que você pediu.** Servidores, contas, custos
e a regra dos 10+. Ela começa por um fato que muda todas as respostas: **o Arkana
não tem uma linha de rede.** Nenhum `@rpc`, nenhum `multiplayer`, nenhum
`HTTPRequest`. Contratar servidor hoje é alugar garagem para um carro que ainda
não foi construído.
- **O item que ATRASA o lançamento se ficar para o fim:** a conta de loja tem
  verificação de identidade e período de teste fechado que se contam em
  **semanas**. O relógio dela corre em paralelo ao desenvolvimento.
- **Quatro decisões suas ficaram em aberto lá** (`04-DADOS-E-MENORES.md` §5):
  conta de jogador, apelido visível, amizade/grupo, compra dentro do app.

**Por que a Sintonia NÃO foi implementada, mesmo sendo o maior buraco:** o GDD §9
trava — *"mudanças em combate, `Balance` e formato de partida dependentes de
V1–V5 esperam o playtest humano do Roblox"*. Dar um parceiro ao jogador
transforma 1×6 em 2×5: é mudança de formato. A mesma linha libera o que foi
feito: *"personagens, ambientes, pipeline e apresentação do Godot podem
avançar"*. A Sintonia já está inteira no Roblox (1.314 linhas) e o GDD §274 já
decidiu o caso solo — falta o playtest, não o código.

---

### (25/08) — auditoria externa executada, mantida como histórico

Um plano de auditoria em 12 tarefas foi executado na branch
`chore/auditoria-organizacao-arkana`. **Nada foi mesclado na `main`.**

**O que ele consertou de verdade:**
- O auditor de modelos media VERTICE num orcamento definido em TRIANGULO, e
  reprovava os DOIS modelos reais do jogo estando ambos dentro do alvo.
- `Bus.damage_dealt` parecia ter consumidor, mas o gancho vivia num ramo `else`
  que nunca rodava. Removido, junto com o mecanismo de troco que so' existia
  para arredondar o sinal.
- `weapon_equipped` e `bau_canalizando` nao diziam de QUEM eram: a HUD
  adivinhava pelo id da arma, e um bot com a mesma arma mudava o icone do
  jogador. Os dois passaram a carregar o `pawn`.
- Os 6 bots passaram a CAIR do castelo, sob a mesma lei do jogador.
- Os 12 autotestes viraram um comando so' (`godot/tests/run_all.sh`) com CI no
  GitHub. Antes eram 12 linhas copiadas a mao — e comando copiado a mao e' como
  se esquece um teste.

**O que ficou BLOQUEADO, por falta de entrada externa:**
- As 160 referencias de arte: falta o documento mestre da direcao aprovada e o
  manifesto/checksums da entrega. Os 160 PNGs estao integros e com manifesto
  gerado, mas **existem em UM lugar so', sem copia**.

**O que NAO foi executado:** teste no Poco F4 — nenhum aparelho apareceu em
`adb devices`. FPS segue sem medicao neste projeto.

### (25/08, manha — o APK anterior, mantido como historico)

O APK esta' em `godot/build/testes/`. **O que mudou e o que olhar:**

1. **A partida agora comeca NO AR.** O castelo cruza o mapa, um toque salta,
   o corpo cai (~9,5 s de ar, 183 m de alcance horizontal), plana e pousa.
   **Nao ha' magia durante a queda** — ordem dele, e esta' testada.
2. **A ilha quase triplicou** (180 -> 300 m) e ficou mais legivel DO ALTO, que
   e' a vista nova que a queda criou. 7 POIs agora (era 4).
3. **A patinacao dos pes**: as DUAS causas achadas foram corrigidas — o modelo
   entrava virado 180 graus e a animacao importada entrava sem laco. **Falta
   medir no aparelho** se o casamento entre cadencia da animacao e velocidade
   real ainda satura (ver item 12 das dividas). Nao declare resolvido antes.
4. **Pegar item e abrir bau viraram GESTO**, com a faisca saindo no quadro em
   que a mao chega ao chao.

**Pendencias conhecidas, ditas sem maquiagem:** os bots ainda nascem no chao
(nao caem); nao ha' colisao no ar (nao da' para pousar em cima de arvore); quem
mergulhar reto no mar pousa na agua, porque natacao nao existe; e o FPS nunca
foi medido em aparelho nenhum.

---

### (25/08 — RESOLVIDO, mantido como historico)

**O defeito da corrida esta' DIAGNOSTICADO e NAO corrigido.** Deixei parado de
proposito; e' a primeira coisa da manha.

**Sintoma, nas palavras do Diretor apos testar o APK no aparelho:** *"da' apenas
os tres primeiros passos e depois patina, mas agora pelo menos esta' na direcao
correta, ja' fez diferenca"*.

**A direcao ja' foi corrigida** (commit `a6efe3f`): o modelo entrava virado 180
graus porque glTF olha para +Z e o Godot anda para -Z. Isso esta' resolvido e
tem teste de regressao.

**O que sobrou — a patinacao — tem causa confirmada:**
`Mage.gd` so' define `loop_mode` nas animacoes PROCEDURAIS (ver `_new_anim`,
que recebe `looped` e aplica `LOOP_LINEAR`). Para o modelo EXTERNO (.glb) nao
existe nenhum tratamento de laco, e o importador glTF do Godot traz as
animacoes com `LOOP_NONE` por padrao. Resultado: o clipe "Running" toca UMA vez
— sao uns tres passos —, congela no ultimo quadro, e o corpo continua
deslizando. Isso e' exatamente a patinacao descrita.

**O conserto (nao aplicado):** em `_build_imported_model()`, depois de resolver
os aliases, marcar `LOOP_LINEAR` nas animacoes de `idle` e `run`. **`cast` NAO
pode entrar no laco** — e' disparo unico e o `cast_fired` depende do fim dela.
Deixar teste de regressao que fique vermelho sem o laco, como manda a regra.

**Cuidado ao medir depois:** havia um SEGUNDO suspeito para a patinacao — o
teto de `Balance.ANIM` saturando num anao de 1,40 m que usa clipe feito para
~1,70 m (`Pawn._sync_anim_speed`). Com o laco consertado, medir de novo ANTES
de mexer nesse numero: pode ja' nao ser necessario.

---

### O que ja' foi feito
O jogo e' **jogavel de ponta a ponta no Android**: menu, selecao de mago,
partida contra 6 bots, zona que fecha, habilidades, escudo, estado derrubado,
armas arcanas, bau, terreno reativo com 6 reacoes, HUD completa e audio
sintetizado. **A Pyra tem modelo 3D real** (Meshy, versionado em LFS). Os 20
magos tem ficha aprofundada e **conjunto completo de concept art** (8 vistas
cada, entregue em 24/08). O Roblox segue como campo de provas multiplayer.

### Como foi feito
Raias paralelas com **donos exclusivos de pastas**: o coordenador escreve os
contratos (`core/Balance.gd`, `core/Bus.gd`) ANTES de despachar e faz as
costuras na integracao. **Todo teste novo se prova reintroduzindo o defeito** —
se nao fica vermelho, e' decorativo. Fiacao defensiva: cada cena boota sem as
outras. Gate de entrega: selftests + boot + APK com o conteudo LISTADO dentro
do pacote (nunca confiar em timestamp).

### Por que foi feito assim
O pivo de 19/08 ("quero ver realidade de jogo, nao mapa de teste") levou o
projeto para Godot 3D. **O GDD e' a unica ponte**: o Roblox valida regras com
gente, e nenhum numero de combate muda sem dado humano. As regras duras
(fogo por orcamento, dano num ponto so', dp para o dedo, cancelar como estado
de primeira classe) nao sao estilo — sao cicatrizes de defeitos medidos.

### Ainda falta

**So' o Diretor pode:**
1. **Decidir a direcao de arte dos personagens** (ver secao 4, item 4).
2. Testar o APK no aparelho e dar o veredito.
3. Aprovar ou cortar os kits 11–20 para subirem ao GDD.
4. Playtest do Roblox com gente de verdade (V1–V5). Login e publicacao sao ato dele.
5. Contratar servidor e contas de loja quando chegar a hora (G5/G6) e responder o IARC.

**A equipe faz.** As pendencias estao separadas em TRES GRUPOS, porque
misturar correcao com decisao de produto foi o que fez o proximo agente
implementar por inferencia:

### GRUPO 1 — correcoes tecnicas reproduziveis (pode fazer)
1.1. **Medir a patinacao no aparelho.** As duas causas achadas foram corrigidas
   (modelo virado e animacao sem laco). Falta medir se o casamento entre
   cadencia e velocidade satura o teto de `Balance.ANIM` num anao de 1,40 m
   usando clipe feito para ~1,70 m.
1.2. **Corrigir os angulos das vistas** da concept art: nao ha' perfil de 90
   graus no lote, e o "3/4" e' a frontal repetida. Trava o multi-imagem da Meshy.
1.3. **Texturas do Brok**: duas de 2048. Consolidar SO' se material e comparacao
   visual provarem que uma e' redundante.

### GRUPO 2 — funcionalidades ja' especificadas (pode fazer, com teste)
2.1. ~~Bots caem do castelo~~ **[FEITO 25/08]** — os 6 caem sob a mesma lei do
   jogador, com 8 checagens no selftest da queda.
2.2. **Animacao `derrubado`**: hoje o caido usa a de locomocao mais lenta. Entra
   como OPCIONAL, com o mesmo mecanismo de `cair`/`planar`/`pegar`, para nao
   derrubar modelo que nao tenha o clipe.
2.3. **Os 17 kits que faltam** — o maior buraco de jogabilidade.
2.4. **A Sintonia** — um dos dois pilares, sem uma linha em codigo.
2.5. **Modelo 3D dos magos restantes.** Caminho provado: 30 creditos cada.

### GRUPO 3 — DECISOES DO DIRETOR (nao implementar por inferencia)
Cada uma esta' na secao 4 com as opcoes e o impacto. Enquanto nao houver
resposta, o comportamento atual permanece e NAO deve ser "melhorado" sozinho:
dano cancelar o bau, resultado do timeout, tabela da zona, colisao aerea,
mergulho no mar, manopla da Pyra, armazenamento das artes, remocao da branch
antiga, protecao da main e contas de publicacao.

### O que sobrou fora dos grupos
 — e' o maior buraco de jogabilidade (secao 3).
- ~~A Sintonia~~ (ver 2.4) — um dos dois pilares de identidade, sem uma linha em codigo.
- ~~Angulos das vistas~~ (ver 1.2) da concept art: nao existe perfil de 90
   graus no lote, e o "3/4" e' a frontal repetida. Isso trava o multi-imagem
   da Meshy e independe da decisao de estilo.
- ~~Modelo 3D dos restantes~~ (ver 2.5) **O caminho esta provado de ponta a ponta**
   (25/08): concept -> Meshy multi-imagem -> remesh 15k -> rig -> animacoes ->
   .glb -> jogo. Custo medido: **30 creditos** por personagem (remesh, rig e
   animacao saem de graca no webapp; na API o remesh custa 5).
- **[FEITO 25/08]** ~~teto de vertices do model_audit~~ — agora mede TRIANGULO: ele reprova em 20.000 e
   **os dois modelos reais reprovam** (Pyra 22.561, Brok 29.212) mesmo estando
   dentro do orcamento de 15k triangulos. Costura de UV multiplica vertice — o
   teto foi escrito na metrica errada.
- ~~Texturas do Brok~~ (ver 1.3): o Brok trouxe duas de 2048 (13,6 MB no
   pacote). TECH_ART pede 2K para hero, entao passa, mas duas e' desperdicio.
5. Vozes (560 falas escritas, nenhuma gravada) e arte de UI (68 prompts prontos).
   **Bloqueante para publicar:** o APK ainda usa o icone padrao do Godot.

---


---

## O que o Godot já provou (resumo do que o Unity tem que alcançar)

O jogo era **jogável de ponta a ponta no Android**: menu, seleção de mago,
queda do castelo, partida contra 12 bots, zona que fecha, luvas e loot, Baú
Celestial, habilidades, escudo, derrubado e reerguer, terreno reativo com 6
reações, água que se nada, HUD completa, pausa, treino, áudio sintetizado.
Modelos 3D reais do castelo, das 3 luvas e do baú (decimados, em LFS). Os 20
magos rodavam o mago procedural — o elenco 3D é refeito no site.

A tabela de sistemas e onde cada um está: `mobile-godot/00-LEIA.md`. Os
contratos e as armadilhas de teste: `mobile-godot/godot/ARQUITETURA.md`.

**As regras duras não são estilo, são cicatrizes de defeitos medidos:**
dano passa por UM lugar e NaN se barra com `not (x > 0)`; velocidade é produto
único (base × terreno × status); número que o dedo sente vive em dp, do mundo
em metros; cancelar é estado de primeira classe; cor + FORMA sempre, clima com
COR e nunca com falta de luz; fogo propaga por ORÇAMENTO, nunca chance por
tique; metro cravado envelhece calado — raio, altura e distância viram fração
do mapa; a Zona nasce inerte e liga no pouso; todo teste novo se prova
reintroduzindo o defeito.

---

## Frente paralela: Roblox

`roblox/` é o **Campo de Provas**: o ambiente multiplayer para testar com pessoas
o que bots não validam — Sintonia, TTK, leitura do terreno, equilíbrio dos
elementos e vontade de jogar de novo. Tem servidor autoritativo, duplas, terreno
reativo, os dez combos de Sintonia, loop de BR, bots, acessibilidade e telemetria.
51/51 nos testes. **O bloqueio não é técnico:** falta o playtest humano, e login
e publicação são ato do Diretor. Intocado desde 19/08.

---

## O que foi encerrado

| Quando | O quê | Por quê |
|---|---|---|
| 19/08/2026 | Protótipo 2D (Phaser/TypeScript/Capacitor) | validou toque e mecânica; não era produto |
| 27/08/2026 | Godot 4.4.1 como produto | congelado quando o projeto foi para PC; hoje é referência da reescrita |
| 09/09/2026 | PC / Steam / Unreal 5 | desvio desfeito pelo Diretor — ver LIÇÕES DO DESVIO |

Tudo continua recuperável pelo histórico do git.

---

## Linha do tempo

| Data | Marco |
|---|---|
| 17/08 | Do zero ao protótipo jogável (2D), com 4 raias em paralelo |
| 19/08 | 2D encerrado; elenco cresce para 20 magos; pivô para Godot 3D |
| 20/08 | Consolidação em Godot 3D + Roblox; concept art dos 20 |
| 21/08 | R20: zona, habilidades, escudo, derrubado, armas arcanas, baú, HUD; Pyra em 3D |
| 24/08 | Novo lote de concept art dos 20 magos (8 vistas cada) |
| 25/08 | A QUEDA (castelo, salto, planeio, pouso); ilha 180 → 300 m; Brok em 3D; higienização |
| 26/08 | A DIREÇÃO (20 respostas do Diretor viram código); castelo, luvas e baú em 3D; vídeos avaliados; Spellbreak aplicado |
| 27/08 | Ilha 600 m, tempestade de BR de verdade, 12 bots; leva 7 desfeita (elenco no site); **virada para PC/Steam/Unreal** |
| 28/08 → 04/09 | Ilha Fraturada no Unreal: 2.400 m, 1.396 peças, 46 FPS; sete peças novas no site da Meshy |
| **09/09** | **Desvio desfeito. O produto é celular em Unity 6.** Unreal, Lyra e Godot apagados; `pc-unreal/` removido |
