## DADOS DOS 20 MAGOS — a unica fonte que a tela de selecao le.
##
## CONTRATO (todo mago tem TODOS os campos; nenhum pode faltar — o selftest
## desta raia falha se faltar):
##   id             int     1..20, unico. 01-10 = lancamento, 11-20 = temporadas.
##   slug           String  pasta em personagens/ (ex.: "01-pyra")
##   nome           String  "Pyra"
##   titulo         String  "a Chama de Guerra"  (sem o nome, sem virgula)
##   raca           String  "Humana"
##   classe         String  Vanguarda | Errante | Vidente | Guardiao |
##                          Dominador | ""  (vazio = ainda nao decidida)
##   funcao         String  taxonomia do Diretor (personagens/00-LEIA.md)
##   historia_breve String  2 a 4 frases. VAZIO HOJE — ver "COMO COSTURAR".
##   passiva        String  "Nome — o que faz"
##   tatica         String  idem
##   suprema        String  idem
##   limitadores    String  o preco do poder (o balanceamento do GDD par.3)
##   caminho_arte   String  res:// do retrato; se o arquivo nao existir a tela
##                          desenha a silhueta placeholder sozinha
##
## Campo String vazio NAO e erro: a tela mostra Textos.PERFIL_PENDENTE no
## lugar. E assim que esta ficha convive com o roteiro sendo escrito agora.
##
## COMO COSTURAR (Diretor / raia de roteiro):
##   As historias vivem em personagens/NN-slug.md. Quando um perfil fechar,
##   cole a versao curta (2 a 4 frases) em historia_breve do id correspondente
##   AQUI. Nenhuma linha de codigo muda — a tela ja le o campo.
##   Mesma regra para 11-20: os kits de temporada sao PROPOSTA DA EQUIPE ate o
##   Diretor aprovar (00-LEIA.md), entao passiva/tatica/suprema/limitadores/
##   classe ficam vazios de proposito.
##
## FONTES: personagens/00-LEIA.md (nome, raca, funcao) e GDD par.3 (kits de
## 01-10, condensados). Nada aqui foi inventado.
class_name Elenco

const MAGOS: Array[Dictionary] = [
	{
		"id": 1, "slug": "01-pyra", "nome": "Pyra", "titulo": "a Chama de Guerra",
		"raca": "Humana", "classe": "Vanguarda", "funcao": "Ataque / média distância",
		"historia_breve": "O portão de Vharen estava cedendo e quarenta soldados ainda estavam do lado errado do fogo. Pyra segurou o portão aberto com o braço esquerdo até o último passar — e depois não tinha mais braço esquerdo. O incêndio não foi embora com a noite: mora hoje numa manopla de bronze, respirando devagar quando ela está calma. Ela não chama isso de perda. Chama de conta paga.",
		"passiva": "Coração de Fornalha — fogo no chão não a fere e reacende o braço: +10% de velocidade por 2s ao atravessar chamas.",
		"tatica": "Muralha de Brasas — linha de fogo baixo de 8m por 5s; bloqueia visão rasante e quem atravessa sai aceso por 2s.",
		"suprema": "Braço Livre — destrava a manopla por 6s: leque contínuo de chamas e dash que deixa fogo no chão.",
		"limitadores": "Água apaga a muralha e molha o braço (+1s de recarga); vento a empurra 3m; ao fim da Suprema o braço esfria: 4s sem tática e menos 15% de velocidade.",
		"caminho_arte": "res://menu/art/01.png",
	},
	{
		"id": 2, "slug": "02-ceifadora", "nome": "Ceifadora", "titulo": "a Voz do Vazio",
		"raca": "Humana (tocada pelo Vazio)", "classe": "Vanguarda", "funcao": "Ataque / Perseguição",
		"historia_breve": "Ela morreu afogada numa fenda do Vazio e voltou remendada — a pele emendada em luz violeta, como louça consertada com ouro. O chão embaixo dela é a prova: a sombra ficou do outro lado e nunca mais voltou. Quem cai perto dela ainda fala por alguns segundos, e ela escuta todos. Não porque queira. Porque não sabe desligar. Voltou devendo alguma coisa a alguém, e persegue como quem cobra.",
		"passiva": "Ecos dos Caídos — onde alguém morreu há menos de 60s ela vê o fantasma dos últimos 3s da luta.",
		"tatica": "Mão do Vazio — marca um ponto a 12m: uma mão de sombra agarra o primeiro inimigo da área por 1,2s.",
		"suprema": "Travessia — rasga o Vazio em linha reta (até 60m); ela e aliados que tocarem o rasgo em 3s atravessam juntos.",
		"limitadores": "A mão cai com 1 golpe corpo a corpo; o rasgo fica aberto 3s (o inimigo entra atrás); quem atravessa sai revelado 4s e 1s sem conjurar.",
		"caminho_arte": "res://menu/art/02.png",
	},
	{
		"id": 3, "slug": "03-veu", "nome": "Véu", "titulo": "a Andarilha",
		"raca": "Humana (planos)", "classe": "Errante", "funcao": "Perseguição / fuga",
		"historia_breve": "Tinha doze anos quando a torre da vila desabou e a jogou para dentro do plano espectral. Voltou sete anos depois com o mesmo rosto de menina, para um mundo que tinha seguido sem ela. Trouxe de volta uma mão que atravessa paredes quando a concentração falha, e vozes: outras Véus, de planos vizinhos, avisando o que já viveram. O nome de batismo ficou do outro lado. \"Véu\" foi o que gritaram quando ela apareceu. Serviu.",
		"passiva": "Entrelinha — após 4s sem atacar nem tomar dano fica semi-translúcida a mais de 20m.",
		"tatica": "Atravessar — 1,5s no plano espectral: invulnerável, mais rápida e passa por paredes finas.",
		"suprema": "Maré Espectral — por 5s ela e os aliados num raio de 6m entram juntos no plano espectral.",
		"limitadores": "Atravessar deixa um eco na entrada e ela sai 1s sem conjurar; na Maré ninguém conjura e um sino espectral toca na posição do grupo; qualquer dano quebra a passiva.",
		"caminho_arte": "res://menu/art/03.png",
	},
	{
		"id": 4, "slug": "04-corvus", "nome": "Corvus", "titulo": "o Caçador",
		"raca": "Humano tribal", "classe": "Errante", "funcao": "Ataque / Perseguição",
		"historia_breve": "No inverno da fome, o espírito-lobo o encontrou primeiro. Corvus não venceu a luta: caído e já sem os olhos, ofereceu à fera a última caça que tinha. O lobo aceitou — e ficou. Desde então ele não enxerga, ele fareja: cheiros chegam como cores, trilhas e formas no ar, e nada nesse mundo mente tão pouco quanto um rastro. Antes de vestir a forma da fera, ele pede licença. Todas as vezes.",
		"passiva": "Mundo de Cheiros — vê as trilhas de cheiro dos últimos 60s como fitas de cor.",
		"tatica": "Uivo de Caça — raio de 25m: inimigos EM MOVIMENTO ficam com o contorno aceso por 4s.",
		"suprema": "Forma de Lobisomem — 30s como besta: 40% mais rápido e cura ao abater.",
		"limitadores": "O uivo denuncia a posição dele para todo o raio; ficar parado esconde do uivo; na forma de besta não conjura magia e a silhueta é maior.",
		"caminho_arte": "res://menu/art/04.png",
	},
	{
		"id": 5, "slug": "05-corvomante", "nome": "Corvomante", "titulo": "o Olho Distante",
		"raca": "Humano", "classe": "Vidente", "funcao": "Longa distância / recon",
		"historia_breve": "Houve um inverno em que uma notícia levaria dez dias a cavalo e três a asa, e o Corvomante não tinha dez dias. Ofereceu o olho direito e recebeu uma pedra de obsidiana no lugar — o olho verdadeiro voa por aí, dentro de um corvo espectral. Tudo o que a ave vê, ele vê. Enquanto ela voa, o corpo dele fica parado onde estiver, indefeso e sozinho. Ele sabe de cor a rota de todas as Torres Arcanas. De perto, quase não enxerga nada.",
		"passiva": "Meu Olho Voa — o corvo marca para o esquadrão os inimigos que vê.",
		"tatica": "Voo do Olho — assume o corvo (baús e Torres Arcanas) ou o pousa num aliado como sentinela a 20m.",
		"suprema": "Grasnido do Fim — pulso antimagia: 50 de dano em escudo, derruba 1 nível de escudo e destrói construções.",
		"limitadores": "Enquanto voa o corvo, o corpo dele fica parado e indefeso; o corvo tem 60 de vida e bate asas audivelmente; o Grasnido quebra construções aliadas também.",
		"caminho_arte": "res://menu/art/05.png",
	},
	{
		"id": 6, "slug": "06-olho-de-eter", "nome": "Olho-de-Éter", "titulo": "o Observador",
		"raca": "Humano", "classe": "Vidente", "funcao": "Longa distância / recon",
		"historia_breve": "A febre da infância levou o som dele e abriu um olho no peito. O templo o tratou como amaldiçoado por anos — até a noite em que as mariposas-de-éter chegaram e não foram mais embora. Elas sentem a vibração do ar, do chão e da magia e devolvem tudo em luz: os ouvidos dele têm asas. Olho-de-Éter nunca escutou uma palavra na vida, e ainda assim é sempre o primeiro a saber que você conjurou.",
		"passiva": "Pó de Éter — quem conjurou nos últimos 5s carrega poeira luminosa visível a 40m.",
		"tatica": "Enxame Perscrutador — enxame em linha: interrompe conjuração ou cura e revela por 6s.",
		"suprema": "Crisálida — casulo que eclode em 2s e revela todos num raio grande por 6s.",
		"limitadores": "A passiva só sente quem CONJUROU (segurar a magia é a contra-jogada); o enxame tem 1,4s de atraso e túnel estreito; o casulo é destrutível (60 de vida) e fogo em área queima as mariposas.",
		"caminho_arte": "res://menu/art/06.png",
	},
	{
		"id": 7, "slug": "07-vitalis", "nome": "Vitalis", "titulo": "a Mão que Cura",
		"raca": "Humana", "classe": "Guardião", "funcao": "Suporte / Vida",
		"historia_breve": "No rio Claro, Vitalis segurou a mão da irmã gêmea até o fim — e o fim não veio inteiro. Lúmen não morreu: ficou ENTRE os mundos, e o que restou dela deste lado é uma pequena luz em forma de menina que nunca se afasta. A mecha branca nasceu naquela noite; os dedos da mão esquerda nunca mais esquentaram. Ela cura estranhos de graça e estuda Círculos de Invocação em segredo. Um dia vai devolver um corpo à irmã.",
		"passiva": "Mãos Livres — Lúmen reergue aliados caídos enquanto Vitalis continua lutando.",
		"tatica": "Vai, Lúmen — envia a fada a um aliado a até 30m: cura 8 hp/s por 12s.",
		"suprema": "Jardim da Aurora — círculo de luz por 10s: aliados regeneram e reerguem 50% mais rápido; inimigos dentro não recebem cura nenhuma.",
		"limitadores": "Lúmen reerguendo não gera escudo; qualquer dano dissipa a fada e a cura para; com Lúmen longe, Vitalis fica SEM a passiva; o Jardim é visível através de paredes para todos.",
		"caminho_arte": "res://menu/art/07.png",
	},
	{
		"id": 8, "slug": "08-ilusionista", "nome": "Ilusionista", "titulo": "o Espelho",
		"raca": "Humano", "classe": "Guardião", "funcao": "Suporte / engano",
		"historia_breve": "Três anos preso dentro do espelho do próprio mestre. Quando finalmente saiu, saiu pelo lado errado: o coração bate à direita, os botões da casaca insistem no lado trocado e a mão boa dele trocou de lugar. O pior ele conta rindo, sempre no meio de um truque — um dos reflexos que dançam ao redor dele é o original, e nem ele sabe qual. Fala sem parar desde então. Três anos de silêncio foram suficientes.",
		"passiva": "Truque de Fuga — ao ser derrubado quebra em cacos de luz: 3s invisível e um reflexo caído no lugar.",
		"tatica": "Espelho de Mão — espelho fixo por 2s que devolve até 3 projéteis mágicos com 30% do dano.",
		"suprema": "Baile de Espelhos — 5 reflexos que espelham os movimentos dele invertidos, mais 2,5s invisível.",
		"limitadores": "O espelho não bloqueia corpo a corpo nem área e quebra após 3 devoluções (som de vidro); reflexos não causam dano e movem-se invertidos; a invisibilidade quebra ao conjurar.",
		"caminho_arte": "res://menu/art/08.png",
	},
	{
		"id": 9, "slug": "09-vex", "nome": "Vex", "titulo": "o Alquimista da Peste",
		"raca": "Humano", "classe": "Dominador", "funcao": "Curta distância / área",
		"historia_breve": "A mina estava soterrada e o ar lá dentro matava devagar. Vex transmutou o ar para salvar quem estava embaixo — e funcionou. A Grande Obra explodiu no mesmo instante e levou os dois pulmões dele. Hoje um fole alquímico de latão respira no lugar, num ritmo que nunca acelera: ele não pode ofegar, não pode correr, não pode ter pressa. O que ele exala agora é a própria peste. Ele anotou tudo. Não se arrependeu de nada.",
		"passiva": "Olhos do Miasma — vê inimigos dentro da própria névoa com contorno verde.",
		"tatica": "Frascos de Reagente — até 6 poças inertes; pisar detona a nuvem local (dano baixo e lentidão).",
		"suprema": "A Grande Obra — névoa enorme por 12s que desacelera e SELA consumíveis: ninguém dentro usa poção, cura ou pergaminho.",
		"limitadores": "A névoa nega área, não mata; vento dispersa e fogo consome em 2s; um tiro no frasco detona a nuvem à distância; o selo da Grande Obra vale para o time dele também.",
		"caminho_arte": "res://menu/art/09.png",
	},
	{
		"id": 10, "slug": "10-tessa", "nome": "Tessa", "titulo": "a Tecelã de Raios",
		"raca": "Humana", "classe": "Dominador", "funcao": "Curta distância / defesa",
		"historia_breve": "Aos nove anos, um raio destruiu o moinho da família e atravessou Tessa no caminho. Ela acordou três dias depois com samambaias de luz desenhadas do ombro ao punho — e um coração que perdeu o compasso para sempre. Aos quinze, forjou sozinha o próprio marca-passo rúnico e mandou abrir um recorte no macacão para deixá-lo à mostra: ela QUER que vejam. Relógios param perto dela. Ela acha ótimo.",
		"passiva": "Compasso Rúnico — o marca-passo regenera escudo; quando o escudo quebra, +15% de velocidade por 2s.",
		"tatica": "Fio do Tear — fio de raio entre 2 pontos (até 6 fios): tocar dá dano, lentidão e revela.",
		"suprema": "Tear-Mãe — tear giratório que absorve projéteis inimigos e tece o absorvido em escudo para os aliados.",
		"limitadores": "Fios quebram com 1 golpe na âncora e brilham e zumbem a 5m; o Tear-Mãe não absorve curtíssimo alcance nem corpo a corpo (máx. 1 por vez); água no chão conduz o raio dos fios para o time dela também.",
		"caminho_arte": "res://menu/art/10.png",
	},
	# --- 11-20: TEMPORADAS. Raca e funcao vieram de 00-LEIA.md; kit e classe
	# sao PROPOSTA DA EQUIPE ate o Diretor aprovar, entao ficam vazios aqui.
	{
		"id": 11, "slug": "11-aelion", "nome": "Aelion", "titulo": "o Arco do Crepúsculo",
		"raca": "Alto Elfo", "classe": "", "funcao": "Longa distância",
		"historia_breve": "Guardião das Torres Arcanas por mais tempo do que dura uma linhagem humana, Aelion tinha uma obrigação só: que nenhuma torre caísse na vigília dele. Uma caiu. Havia uma vila embaixo, e havia uma menina que o mundo deu por perdida — e que não estava. Ele não fala disso. Aliás, fala pouco de qualquer coisa: guarda as palavras como guarda as flechas, e erra menos que ambas.", "passiva": "", "tatica": "", "suprema": "", "limitadores": "",
		"caminho_arte": "res://menu/art/11.png",
	},
	{
		"id": 12, "slug": "12-umbra", "nome": "Umbra", "titulo": "a Lâmina da Noite",
		"raca": "Elfa Negra (Drow)", "classe": "", "funcao": "Ataque / Perseguição",
		"historia_breve": "A corte subterrânea a expulsou, e ela agradeceu em voz alta na saída — o que garantiu que o exílio fosse definitivo. Umbra caça por contrato e cobra caro, com desconto zero para nobres. As runas roxas na pele acendem quando a luz some, e luz direta a incomoda mais do que qualquer inimigo do mapa. Já aceitou trabalho por menos que uma dívida. Nunca perdoou nenhuma.", "passiva": "", "tatica": "", "suprema": "", "limitadores": "",
		"caminho_arte": "res://menu/art/12.png",
	},
	{
		"id": 13, "slug": "13-brok", "nome": "Brok", "titulo": "o Ferreiro de Runas",
		"raca": "Anão", "classe": "", "funcao": "Suporte / Proteção",
		"historia_breve": "Brok martela runa há tempo suficiente para falar com elas como quem fala com filho — e para reclamar quando respondem torto. Foi ele quem forjou a manopla de bronze que prende o fogo vivo no braço da Pyra, a única prisão que aceitou fazer na vida. Foi ele quem olhou o marca-passo torto da Tessa e se recusou a endireitar. E é ele quem carrega há anos a placa em branco que o Basalto pede para gravar — e não grava.", "passiva": "", "tatica": "", "suprema": "", "limitadores": "",
		"caminho_arte": "res://menu/art/13.png",
	},
	{
		"id": 14, "slug": "14-gromm", "nome": "Gromm", "titulo": "o Xamã da Tempestade",
		"raca": "Orc", "classe": "", "funcao": "Suporte / Vida",
		"historia_breve": "As estepes ensinam a ouvir o trovão antes de vê-lo, e Gromm aprendeu cedo demais: era o mais novo da tribo na noite em que a tempestade levou os mais velhos e deixou os feridos com ele. Desde então cura primeiro e discute justiça depois. A chuva que ele chama apaga fogo — inclusive o fogo dos amigos, e ele avisa antes, todas as vezes. Fala devagar. Tem tempo.", "passiva": "", "tatica": "", "suprema": "", "limitadores": "",
		"caminho_arte": "res://menu/art/14.png",
	},
	{
		"id": 15, "slug": "15-maris", "nome": "Maris", "titulo": "a Voz das Marés",
		"raca": "Nereida", "classe": "", "funcao": "Longa distância",
		"historia_breve": "Sacerdotisa do lago sob o castelo voador, Maris passou tempo demais abençoando uma água que ninguém mais visitava. Quando o castelo começou a cruzar o céu despejando magos sobre o mapa, ela subiu junto — e descobriu, com certa surpresa, que gosta de brigar. Fala em maré: frases que sobem devagar e quebram de uma vez. Os pés dela nunca tocam o chão de verdade; há sempre dois dedos d'água entre ela e o mundo.", "passiva": "", "tatica": "", "suprema": "", "limitadores": "",
		"caminho_arte": "res://menu/art/15.png",
	},
	{
		"id": 16, "slug": "16-fizz", "nome": "Fizz", "titulo": "o Artífice de Bolso",
		"raca": "Gnomo", "classe": "", "funcao": "Longa distância",
		"historia_breve": "Fizz inventa mais rápido do que documenta, o que é a maneira educada de dizer que ele explode coisas com frequência preocupante. Tem noventa e cinco centímetros, uma mochila-oficina maior que o próprio tronco e molas nos calcanhares porque cair de lugares altos era o último problema que faltava resolver. Pede desculpa explodindo outra coisa. A lente de cima dos óculos está sempre virada, e ele jura que é de propósito.", "passiva": "", "tatica": "", "suprema": "", "limitadores": "",
		"caminho_arte": "res://menu/art/16.png",
	},
	{
		"id": 17, "slug": "17-sylva", "nome": "Sylva", "titulo": "a Filha da Floresta",
		"raca": "Dríade", "classe": "", "funcao": "Suporte / Vida",
		"historia_breve": "Sylva é a memória viva da floresta do mapa: lembra de cada árvore que caiu e do nome de quem a derrubou. A pele dela é casca, o cabelo é estação — floresce quando ela está bem e vira outono quando não está, e todo mundo em campo aprende a ler isso. Ela dá a própria seiva pelos aliados, literalmente: o dano que iria para eles vem para ela. Doce com quase todos. Menos com quem queima.", "passiva": "", "tatica": "", "suprema": "", "limitadores": "",
		"caminho_arte": "res://menu/art/17.png",
	},
	{
		"id": 18, "slug": "18-basalto", "nome": "Basalto", "titulo": "o Desperto",
		"raca": "Golem rúnico", "classe": "", "funcao": "Curta distância",
		"historia_breve": "Ninguém o acordou de propósito. Basalto abriu o olho de âmbar dentro da ruína a noroeste do mapa e não havia mais ninguém ali para explicar por quê. As correntes no ombro dele têm uma placa de bronze para cada mestre que já serviu, e a última está em branco: ele procura um propósito digno de ser gravado. Duas toneladas de pedra rúnica, uma palavra por vez, e um cuidado quase cômico com tudo que é pequeno.", "passiva": "", "tatica": "", "suprema": "", "limitadores": "",
		"caminho_arte": "res://menu/art/18.png",
	},
	{
		"id": 19, "slug": "19-noctus", "nome": "Noctus", "titulo": "o Sedento de Éter",
		"raca": "Vampiro arcano", "classe": "", "funcao": "Ataque / Perseguição",
		"historia_breve": "Noctus foi nobre de uma corte que já não existe, e continua se vestindo como se ela existisse. Não bebe sangue — considera vulgar. Bebe ÉTER: a mana alheia, tomada numa investida curta e num pedido de licença que ninguém tem tempo de recusar. Quanto mais tempo você passa em combate com ele, menos magia lhe sobra e mais sobra a ele. Chama o torneio inteiro de banquete de má educação. E não perde um.", "passiva": "", "tatica": "", "suprema": "", "limitadores": "",
		"caminho_arte": "res://menu/art/19.png",
	},
	{
		"id": 20, "slug": "20-pip", "nome": "Pip", "titulo": "a Centelha Selvagem",
		"raca": "Fada", "classe": "", "funcao": "Ataque / Perseguição",
		"historia_breve": "Sessenta centímetros de fada, quatro asas de libélula com desenho de circuito e velocidade suficiente para chegar quase antes do próprio barulho. Quase: o sino no tornozelo dela é maldição de infância que magia nenhuma tira, e é por ele que todo mundo sabe que ela está por perto. Pip coleciona botões arrancados de casacos alheios — o do Noctus é a joia da coleção e ela o usa como pingente. Parada, ela definha. Então ela não para.", "passiva": "", "tatica": "", "suprema": "", "limitadores": "",
		"caminho_arte": "res://menu/art/20.png",
	},
]

## Os campos que TODO mago precisa ter (o selftest desta raia usa esta lista).
const CAMPOS: Array[String] = [
	"id", "slug", "nome", "titulo", "raca", "classe", "funcao",
	"historia_breve", "passiva", "tatica", "suprema", "limitadores", "caminho_arte",
]


## Mago de temporada (11-20): a vitrine marca com o selo EM BREVE.
static func e_temporada(d: Dictionary) -> bool:
	return int(d.get("id", 0)) >= 11


## "Pyra, a Chama de Guerra" — o rotulo do card e do cabecalho do perfil.
static func nome_cheio(d: Dictionary) -> String:
	var t := str(d.get("titulo", ""))
	if t == "":
		return str(d.get("nome", "?"))
	return "%s, %s" % [d.get("nome", "?"), t]


## Devolve o texto do campo ou o placeholder — nenhuma tela precisa saber que
## o roteiro ainda esta sendo escrito.
static func campo(d: Dictionary, chave: String) -> String:
	var v := str(d.get(chave, ""))
	return v if v.strip_edges() != "" else Textos.PERFIL_PENDENTE
