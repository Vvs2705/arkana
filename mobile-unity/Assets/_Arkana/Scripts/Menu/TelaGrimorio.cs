using System;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.UI;

namespace Arkana.Menu
{
    /// <summary>
    /// O ICONE de cada pagina, desenhado em codigo com as formas do jogo (Formas.DoElemento e as primitivas): 2-3 camadas
    /// por pagina. ACESA = cada camada na sua cor (elemento pelo FiltroDaltonismo). APAGADA = a SILHUETA: as mesmas formas
    /// numa cor so', fosca — a dica esta' no desenho (uma gota e um losango...), a resposta (cor, nome, frase) so' depois.
    /// O livro do menu e o aviso em partida desenham pelo MESMO metodo.
    /// </summary>
    public static class IconeDaPagina
    {
        struct Camada
        {
            public Sprite S; public Color C; public float Lado; public Vector2 Desvio;
            public Camada(Sprite s, Color c, float lado, float dx = 0f, float dy = 0f) { S = s; C = c; Lado = lado; Desvio = new Vector2(dx, dy); }
        }

        /// <summary>Cor da silhueta (KNOB por foto): le' como forma no miolo escuro, nunca como cor.</summary>
        public static Color Silhueta => Formas.ComAlfa(Estilo.TextoFosco, 0.3f);
        static readonly Color Gelo = new Color(0.80f, 0.94f, 1f), Corte = new Color(1f, 0.42f, 0.36f), Vida = new Color(0.45f, 0.95f, 0.62f),
            Tempestade = new Color(0.55f, 0.35f, 1f), Sangue = new Color(1f, 0.29f, 0.23f);

        static Camada[] Camadas(string id)
        {
            Sprite agua = Formas.DoElemento(Elemento.Agua), fogo = Formas.DoElemento(Elemento.Fogo), raio = Formas.DoElemento(Elemento.Raio),
                terra = Formas.DoElemento(Elemento.Terra), vento = Formas.DoElemento(Elemento.Vento);
            Color cAgua = Estilo.CorElemento(Elemento.Agua), cFogo = Estilo.CorElemento(Elemento.Fogo), cRaio = Estilo.CorElemento(Elemento.Raio),
                cTerra = Estilo.CorElemento(Elemento.Terra), cVento = Estilo.CorElemento(Elemento.Vento);
            switch (id)
            {
                case Grimorio.LagoCongelado: return new[] { new Camada(agua, cAgua, 0.8f, -0.08f, 0.02f), new Camada(Formas.Losango(), Gelo, 0.44f, 0.24f, -0.22f) };
                case Grimorio.Conducao: return new[] { new Camada(agua, cAgua, 0.82f, -0.1f, 0f), new Camada(raio, cRaio, 0.66f, 0.2f, -0.04f) };
                case Grimorio.FogoApagado: return new[] { new Camada(fogo, cFogo, 0.86f, -0.04f, -0.06f), new Camada(agua, cAgua, 0.5f, 0.22f, 0.2f) };
                case Grimorio.MuroDePedra: return new[] { new Camada(Formas.Quadrado(), cTerra, 0.36f, -0.2f, -0.2f), new Camada(Formas.Quadrado(), cTerra, 0.36f, 0.2f, -0.2f), new Camada(Formas.Quadrado(), cTerra, 0.36f, 0f, 0.2f) };
                case Grimorio.Lamacal: return new[] { new Camada(terra, cTerra, 0.8f, -0.06f, -0.06f), new Camada(agua, cAgua, 0.48f, 0.22f, 0.22f) };
                case Grimorio.VentoNoFogo: return new[] { new Camada(vento, cVento, 0.84f, -0.08f, 0.02f), new Camada(fogo, cFogo, 0.46f, 0.24f, -0.22f) };
                case Grimorio.Sintonia: return new[] { new Camada(Formas.Anel(), Estilo.Ouro, 0.94f), new Camada(fogo, cFogo, 0.5f, -0.17f, 0f), new Camada(raio, cRaio, 0.52f, 0.17f, 0f) };
                case Grimorio.PactoQuebrado: return new[] { new Camada(Formas.Anel(), Estilo.Ouro, 0.9f), new Camada(Formas.Xis(), Corte, 0.86f) };
                case Grimorio.Retorno: return new[] { new Camada(Formas.Anel(), Vida, 0.9f), new Camada(Formas.Seta(), Vida, 0.56f, 0f, 0.02f) };
                case Grimorio.MapaArma: return new[] { new Camada(Formas.Triangulo(), cTerra, 0.92f, 0f, -0.06f), new Camada(Formas.Losango(), Estilo.Ouro, 0.3f, 0f, 0.3f) };
                case Grimorio.NoVermelho: return new[] { new Camada(Formas.Anel(), Tempestade, 0.94f), new Camada(Formas.Disco(), Sangue, 0.5f) };
                case Grimorio.EscudoCoroado: return new[] { new Camada(Formas.Losango(), Formas.Hex(Balance.Escudo.Cores[EscudoNivel3]), 0.78f, 0f, -0.08f), new Camada(Formas.Triangulo(), Estilo.Ouro, 0.38f, 0f, 0.32f) };
            }
            return new[] { new Camada(Formas.Losango(), Estilo.Ouro, 0.6f) };
        }

        /// <summary>Indice da cor do escudo no nivel da pagina (Balance.Escudo.Cores: branco/azul/ROXO/dourado). So' LEITURA.</summary>
        const int EscudoNivel3 = Grimorio.EscudoNivel - 1;

        /// <summary>Desenha o icone num quadrado de `ladoPx` no centro de `pai`. Devolve o no' (quem troca de pagina o destroi).</summary>
        public static RectTransform Desenhar(Transform pai, string id, bool acesa, float ladoPx)
        {
            var raiz = Formas.No(pai, "Icone");
            raiz.anchorMin = raiz.anchorMax = raiz.pivot = new Vector2(0.5f, 0.5f);
            raiz.anchoredPosition = Vector2.zero;
            raiz.sizeDelta = new Vector2(ladoPx, ladoPx);
            foreach (Camada c in Camadas(id))
            {
                var img = Formas.Imagem(raiz, "Camada", c.S, acesa ? c.C : Silhueta);
                img.rectTransform.sizeDelta = new Vector2(ladoPx * c.Lado, ladoPx * c.Lado);
                img.rectTransform.anchoredPosition = c.Desvio * ladoPx;
            }
            return raiz;
        }
    }

    /// <summary>
    /// A TELA DO GRIMORIO no menu (GDD §18.3): um LIVRO aberto — a folha da esquerda com as 6 de TERRENO, a da direita com as
    /// 6 de DUPLA E DESFECHO, e o contador "7/12" no alto. Acesa = placa com fio de ouro, o selo com o icone em cor, o nome em
    /// ouro e a frase. Apagada = placa fosca, a SILHUETA do icone e "???" — o nome e a frase de verdade nunca aparecem antes
    /// (a graca e' cacar). So' OLHAR: nada aqui muda estado de jogo nem o save (le' Grimorio.MascaraSalva()).
    /// Refaz tudo a cada abertura (12 placas: mais barato e menos bug que sincronizar).
    /// </summary>
    public sealed class TelaGrimorio : MonoBehaviour
    {
        public const string T_TITULO = Textos.GrimorioTitulo, T_VOLTAR = Textos.Voltar, T_TRANCADA = Textos.GrimorioTrancada;
        public event Action VoltarPedido;

        /// <summary>KNOB por foto: o selo do icone, o vao entre placas, a lombada, a folga da folha e o cabecalho dela.</summary>
        const float SeloDp = 54f, VaoDp = 6f, LombadaDp = 18f, FolgaDp = 8f, CabecalhoDp = 22f, RodapeDp = 18f;

        RectTransform _raiz;

        public static TelaGrimorio Criar(Transform pai)
        {
            var go = new GameObject("TelaGrimorio", typeof(RectTransform), typeof(TelaGrimorio));
            go.transform.SetParent(pai, false);
            var t = go.GetComponent<TelaGrimorio>();
            t._raiz = (RectTransform)go.transform;
            AreaSegura.Esticar(t._raiz);
            t.Montar();
            return t;
        }

        /// <summary>A REGRA DA CACA (pura — o teste cobra): apagada NUNCA devolve o nome nem a frase de verdade.</summary>
        public static string[] Rotulo(string id, bool acesa)
        {
            string[] t;
            if (acesa && Textos.GrimorioPagina.TryGetValue(id, out t)) return t;
            return new[] { T_TRANCADA, "" };
        }

        public static string Contagem(int mascara) => string.Format(Textos.GrimorioContagem, Grimorio.Contar(mascara), Grimorio.Paginas.Length);

        /// <summary>Le' o save de novo e redesenha (o Menu chama a cada abertura: a partida pode ter acendido pagina).</summary>
        public void Montar()
        {
            foreach (Transform t in _raiz) { t.gameObject.SetActive(false); Destroy(t.gameObject); }   // apagado ja': o Find nao acha o velho
            int mascara = Grimorio.MascaraSalva();
            Estilo.Fundo(_raiz);   // opaco: e' um livro, a vitrine atras so' atrapalharia a leitura
            var conteudo = Formas.No(_raiz, "Conteudo");
            AreaSegura.EsticarDentro(conteudo, AreaSegura.Atual(), Dp.Px(Estilo.RespiroDp));
            float alt = Estilo.AlturaAlvo() + 8f;

            var titulo = Formas.Texto(conteudo, "Titulo", T_TITULO, 24f, Estilo.Ouro);
            titulo.fontStyle = FontStyle.Bold;
            Arkana.Menu.Menu.Contorno(titulo);
            titulo.rectTransform.anchorMin = new Vector2(0, 1); titulo.rectTransform.anchorMax = Vector2.one; titulo.rectTransform.pivot = new Vector2(0.5f, 1);
            titulo.rectTransform.anchoredPosition = Vector2.zero; titulo.rectTransform.sizeDelta = new Vector2(0, alt);
            var voltar = Estilo.Botao(conteudo, "BtnVoltarGrimorio", T_VOLTAR, 150f, Estilo.AlvoDp, 16f);
            var vrt = (RectTransform)voltar.transform;
            vrt.anchorMin = vrt.anchorMax = vrt.pivot = new Vector2(0, 1); vrt.anchoredPosition = Vector2.zero;
            voltar.onClick.AddListener(() => VoltarPedido?.Invoke());
            Contador(conteudo, mascara, alt);

            var rodape = Formas.Texto(conteudo, "Rodape", Textos.GrimorioRodape, 11f, Estilo.TextoFosco);
            rodape.rectTransform.anchorMin = Vector2.zero; rodape.rectTransform.anchorMax = new Vector2(1, 0); rodape.rectTransform.pivot = new Vector2(0.5f, 0);
            rodape.rectTransform.anchoredPosition = Vector2.zero; rodape.rectTransform.sizeDelta = new Vector2(0, Dp.Px(RodapeDp));

            var livro = Formas.No(conteudo, "Livro");
            livro.anchorMin = Vector2.zero; livro.anchorMax = Vector2.one;
            livro.offsetMin = new Vector2(0, Dp.Px(RodapeDp) + Dp.Px(4f)); livro.offsetMax = new Vector2(0, -alt - Dp.Px(6f));
            Folha(livro, 0, Textos.GrimorioLadoTerreno, mascara);
            Folha(livro, 1, Textos.GrimorioLadoDupla, mascara);
            // a LOMBADA: fio de ouro no vinco com tres losangos — o que faz as duas folhas lerem como UM livro
            var lombada = Formas.Imagem(livro, "Lombada", null, Formas.ComAlfa(Estilo.Ouro, 0.45f));
            lombada.rectTransform.anchorMin = new Vector2(0.5f, 0); lombada.rectTransform.anchorMax = new Vector2(0.5f, 1);
            lombada.rectTransform.sizeDelta = new Vector2(Mathf.Max(Dp.Px(1.5f), 1f), -Dp.Px(16f));
            for (int i = 0; i < 3; i++)
            {
                var l = Formas.Imagem(livro, "LombadaGema" + i, Formas.Losango(), Estilo.Ouro);
                l.rectTransform.anchorMin = l.rectTransform.anchorMax = new Vector2(0.5f, 0.2f + 0.3f * i);
                l.rectTransform.sizeDelta = new Vector2(Dp.Px(9f), Dp.Px(9f));
            }
        }

        /// <summary>No alto a' direita: "7/12" grande em ouro, PAGINAS pequeno, e a trilha que enche com o livro.</summary>
        void Contador(RectTransform conteudo, int mascara, float alt)
        {
            var chip = Hud.Placa(conteudo, "Contador", Dp.Px(10f));
            chip.color = Formas.ComAlfa(Estilo.Ouro, 0.9f);
            var rt = chip.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one;
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(Dp.Px(150f), Estilo.AlturaAlvo());
            var num = Formas.Texto(rt, "Numero", Contagem(mascara), 22f, Estilo.Ouro, TextAnchor.MiddleLeft);
            num.fontStyle = FontStyle.Bold;
            num.rectTransform.anchorMin = Vector2.zero; num.rectTransform.anchorMax = new Vector2(0.55f, 1);
            num.rectTransform.offsetMin = new Vector2(Dp.Px(14f), Dp.Px(4f)); num.rectTransform.offsetMax = Vector2.zero;
            var rot = Formas.Texto(rt, "Rotulo", Textos.GrimorioPaginas, 11f, Estilo.OuroFosco, TextAnchor.MiddleRight);
            rot.rectTransform.anchorMin = new Vector2(0.5f, 0); rot.rectTransform.anchorMax = Vector2.one;
            rot.rectTransform.offsetMin = new Vector2(0, Dp.Px(4f)); rot.rectTransform.offsetMax = new Vector2(-Dp.Px(14f), 0);
            var trilho = Formas.Imagem(rt, "Trilho", null, Formas.ComAlfa(Estilo.OuroFosco, 0.4f));
            var cheio = Formas.Imagem(rt, "Cheio", null, Estilo.Ouro);
            cheio.type = Image.Type.Filled; cheio.fillMethod = Image.FillMethod.Horizontal;
            cheio.fillAmount = Grimorio.Contar(mascara) / (float)Grimorio.Paginas.Length;
            foreach (var barra in new[] { trilho, cheio })
            {
                barra.rectTransform.anchorMin = Vector2.zero; barra.rectTransform.anchorMax = new Vector2(1, 0);
                barra.rectTransform.offsetMin = new Vector2(Dp.Px(14f), Dp.Px(8f)); barra.rectTransform.offsetMax = new Vector2(-Dp.Px(14f), Dp.Px(11f));
            }
        }

        /// <summary>Uma folha do livro: placa escura com fio de ouro fosco, o cabecalho entre dois losangos e as 6 paginas em
        /// 2 colunas x 3 linhas (ancoras em fracao: cabe em qualquer tela; o texto encolhe pelo bestFit, nunca vaza).</summary>
        void Folha(RectTransform livro, int lado, string cabecalho, int mascara)
        {
            var folha = Hud.Placa(livro, lado == 0 ? "FolhaTerreno" : "FolhaDupla", Dp.Px(12f));
            var rt = folha.rectTransform;
            rt.anchorMin = new Vector2(lado * 0.5f, 0); rt.anchorMax = new Vector2(lado * 0.5f + 0.5f, 1);
            rt.offsetMin = new Vector2(lado == 0 ? 0f : Dp.Px(LombadaDp) * 0.5f, 0); rt.offsetMax = new Vector2(lado == 0 ? -Dp.Px(LombadaDp) * 0.5f : 0f, 0);
            var cab = Formas.Texto(rt, "Cabecalho", cabecalho, 12f, Estilo.Ouro);
            cab.fontStyle = FontStyle.Bold;
            cab.GetComponent<Shadow>().enabled = false;   // o Letreiro vem PRIMEIRO (a regra do Estilo.Botao)
            cab.gameObject.AddComponent<Letreiro>();
            cab.rectTransform.anchorMin = new Vector2(0, 1); cab.rectTransform.anchorMax = Vector2.one; cab.rectTransform.pivot = new Vector2(0.5f, 1);
            cab.rectTransform.anchoredPosition = new Vector2(0, -Dp.Px(4f)); cab.rectTransform.sizeDelta = new Vector2(0, Dp.Px(CabecalhoDp));
            float meia = cab.preferredWidth * 0.5f * 1.12f + Dp.Px(14f);   // o Letreiro abre 12% a palavra
            for (int i = 0; i < 2; i++)
            {
                var l = Formas.Imagem(cab.rectTransform, "Losango" + i, Formas.Losango(), Estilo.OuroFosco);
                l.rectTransform.anchorMin = l.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                l.rectTransform.anchoredPosition = new Vector2(i == 0 ? -meia : meia, 0);
                l.rectTransform.sizeDelta = new Vector2(Dp.Px(7f), Dp.Px(7f));
            }
            var grade = Formas.No(rt, "Grade");
            grade.anchorMin = Vector2.zero; grade.anchorMax = Vector2.one;
            grade.offsetMin = new Vector2(Dp.Px(FolgaDp), Dp.Px(FolgaDp)); grade.offsetMax = new Vector2(-Dp.Px(FolgaDp), -Dp.Px(CabecalhoDp) - Dp.Px(6f));
            for (int k = 0; k < Grimorio.DeTerreno; k++)
            {
                string id = Grimorio.Paginas[lado * Grimorio.DeTerreno + k];
                int col = k % 2, lin = k / 2;
                var cel = Formas.No(grade, "Celula" + k);
                cel.anchorMin = new Vector2(col * 0.5f, 1f - (lin + 1) / 3f); cel.anchorMax = new Vector2((col + 1) * 0.5f, 1f - lin / 3f);
                float v = Dp.Px(VaoDp) * 0.5f;
                cel.offsetMin = new Vector2(v, v); cel.offsetMax = new Vector2(-v, -v);
                Pagina(cel, id, Grimorio.AcesaEm(mascara, id));
            }
        }

        void Pagina(RectTransform cel, string id, bool acesa)
        {
            var placa = Hud.Placa(cel, "Pagina_" + id, Dp.Px(10f));
            placa.color = acesa ? Formas.ComAlfa(Estilo.Ouro, 0.95f) : Formas.ComAlfa(Estilo.OuroFosco, 0.35f);
            AreaSegura.Esticar(placa.rectTransform);
            if (!acesa) placa.transform.Find("Miolo").GetComponent<Image>().color = Formas.ComAlfa(Estilo.NoiteFunda, 0.7f);
            float selo = Dp.Px(SeloDp), folga = Dp.Px(10f);
            var ancora = Formas.No(placa.transform, "Selo");
            ancora.anchorMin = ancora.anchorMax = ancora.pivot = new Vector2(0f, 0.5f);
            ancora.anchoredPosition = new Vector2(folga, 0); ancora.sizeDelta = new Vector2(selo, selo);
            if (acesa)
            {
                var brilho = Formas.Imagem(ancora, "Brilho", Formas.Halo(), Formas.ComAlfa(Estilo.Ouro, 0.55f));   // a pagina ILUMINADA
                brilho.rectTransform.sizeDelta = new Vector2(selo * Formas.HaloEscala, selo * Formas.HaloEscala);
            }
            var disco = Formas.Imagem(ancora, "Disco", Formas.Disco(), Estilo.NoiteFunda);
            AreaSegura.Esticar(disco.rectTransform);
            var aro = Formas.Imagem(ancora, "Aro", Formas.Anel(0.86f), acesa ? Estilo.Ouro : IconeDaPagina.Silhueta);
            AreaSegura.Esticar(aro.rectTransform);
            IconeDaPagina.Desenhar(ancora, id, acesa, selo * 0.62f);

            string[] rot = Rotulo(id, acesa);
            float x0 = folga + selo + Dp.Px(10f);
            var nome = Formas.Texto(placa.transform, "Nome", rot[0], 13f, acesa ? Estilo.Ouro : Estilo.TextoFosco, TextAnchor.LowerLeft);
            nome.fontStyle = FontStyle.Bold;
            Encolher(nome, 13f);
            var n = nome.rectTransform;
            n.anchorMin = new Vector2(0, acesa ? 0.62f : 0.3f); n.anchorMax = new Vector2(1, acesa ? 0.92f : 0.7f);
            n.offsetMin = new Vector2(x0, 0); n.offsetMax = new Vector2(-folga, 0);
            if (!acesa) { nome.alignment = TextAnchor.MiddleLeft; return; }
            var frase = Formas.Texto(placa.transform, "Frase", rot[1], 10.5f, Estilo.Texto, TextAnchor.UpperLeft);
            Encolher(frase, 10.5f);
            var f = frase.rectTransform;
            f.anchorMin = new Vector2(0, 0.08f); f.anchorMax = new Vector2(1, 0.6f);
            f.offsetMin = new Vector2(x0, 0); f.offsetMax = new Vector2(-folga, 0);
        }

        /// <summary>Quebra linha e encolhe ate' 75% se nao couber (tela estreita): nunca vaza da placa.</summary>
        static void Encolher(Text t, float dp)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMaxSize = t.fontSize;
            t.resizeTextMinSize = Mathf.Max(Mathf.RoundToInt(Dp.Px(dp * 0.75f)), 8);
        }
    }
}
