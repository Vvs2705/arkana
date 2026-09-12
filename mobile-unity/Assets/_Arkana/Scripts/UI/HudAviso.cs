using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Arkana.UI
{
    /// <summary>
    /// Logica PURA da camada de avisos: faixa com PRIORIDADE (uma linha, menor numero ganha; DERRUBADO cala a faixa),
    /// badges de estado (teto 4), vinheta com intervalo minimo, arcos direcionais com teto, bussola e contagens.
    /// Nada aqui decide jogo; so' guarda o que a Hud mandou e responde "o que se mostra agora".
    /// </summary>
    public sealed class AvisoLogica
    {
        public const int P_ZONA_FORA = 0;   // morrendo na tempestade AGORA
        public const int P_TELEGRAFO = 1;   // suprema anunciada (janela de fuga)
        public const int P_ZONA = 2;        // proxima parede
        public const int P_BAU = 3;         // oportunidade, some primeiro
        public const int MAX_BADGES = 4;

        public sealed class Faixa { public string Texto; public Color Cor; public float Ate; }
        public sealed class Arco { public Vector3 Pos; public Color Cor; public float Ate; }
        public sealed class Contagem { public float Ate; public string Formato; public Color Cor; public int S = -1; }

        readonly Dictionary<int, Faixa> _faixa = new Dictionary<int, Faixa>();
        readonly Dictionary<int, Contagem> _contagens = new Dictionary<int, Contagem>();
        public readonly List<Arco> Arcos = new List<Arco>();
        public readonly List<string> Badges = new List<string>();
        public readonly Dictionary<string, KeyValuePair<Vector3, Color>> Bussola = new Dictionary<string, KeyValuePair<Vector3, Color>>();

        public float Agora { get; private set; }
        public float Vinheta { get; private set; }
        public Color VinhetaCor = new Color(0.9f, 0.15f, 0.15f);
        float _vinhetaUltima = -99f;
        public bool Caido;
        public bool Resgatando;
        public float Esvaecimento;
        public float Reerguer;
        /// <summary>Canalizacao do bau: -1 = nada; 0..1 = anel enchendo. Cancelado &gt; 0 = X vermelho esmaecendo.</summary>
        public float Canal = -1f;
        public float Cancelado;
        public const float CANCELADO_S = 0.9f;
        public static readonly Color COR_CANCELADO = new Color(1f, 0.36f, 0.30f);

        public readonly float VinhetaMinS;
        public readonly float ArcoDurS;
        public readonly int ArcoMax;
        readonly HashSet<string> _estadosConhecidos;

        public AvisoLogica(float vinhetaMinS, float arcoDurS, int arcoMax, IEnumerable<string> estadosConhecidos)
        {
            VinhetaMinS = vinhetaMinS; ArcoDurS = arcoDurS; ArcoMax = Mathf.Max(arcoMax, 1);
            _estadosConhecidos = new HashSet<string>(estadosConhecidos);
        }

        public void Avisar(int prio, string texto, Color cor, float dur = 0f)
        {
            _faixa[prio] = new Faixa { Texto = texto, Cor = cor, Ate = dur > 0f ? Agora + dur : 0f };
        }

        public void Limpar(int prio) { _faixa.Remove(prio); _contagens.Remove(prio); }

        /// <summary>Quem GANHOU a faixa neste instante ("" = ninguem). No chao a faixa cala.</summary>
        public string FaixaAtiva()
        {
            Faixa f = FaixaVencedora();
            return f == null ? "" : f.Texto;
        }

        public Faixa FaixaVencedora()
        {
            if (_faixa.Count == 0 || Caido) return null;
            int melhor = int.MaxValue;
            foreach (var k in _faixa.Keys) if (k < melhor) melhor = k;
            return _faixa[melhor];
        }

        /// <summary>Contagem regressiva numa prioridade: reescreve so' quando o SEGUNDO inteiro muda.</summary>
        public void Contar(int prio, float segundos, string formato, Color cor)
        {
            _contagens[prio] = new Contagem { Ate = Agora + segundos, Formato = formato, Cor = cor };
            TickContagens();
        }

        public void PararContagem(int prio) { _contagens.Remove(prio); }

        public void MarcarArco(Vector3 de, Color cor)
        {
            Arcos.Add(new Arco { Pos = de, Cor = cor, Ate = Agora + ArcoDurS });
            while (Arcos.Count > ArcoMax) Arcos.RemoveAt(0);   // o 4o substitui o mais antigo
        }

        /// <summary>Vinheta com intervalo minimo (anti-spam do DoT): tiques colados nao repicam.</summary>
        public bool Pulsar(Color cor, float forca = 1f)
        {
            if (Agora - _vinhetaUltima < VinhetaMinS) return false;
            _vinhetaUltima = Agora;
            VinhetaCor = cor;
            Vinheta = Mathf.Max(Vinheta, Mathf.Clamp01(forca));
            return true;
        }

        /// <summary>Estado sem rotulo conhecido e' IGNORADO: estado novo nao pode quebrar a HUD.</summary>
        public void Estado(string nome, bool ligado)
        {
            if (!_estadosConhecidos.Contains(nome)) return;
            if (ligado) { if (!Badges.Contains(nome)) Badges.Add(nome); }
            else Badges.Remove(nome);
        }

        public void SetBussola(string chave, Vector3 pos, Color cor, bool ligado = true)
        {
            if (ligado) Bussola[chave] = new KeyValuePair<Vector3, Color>(pos, cor);
            else Bussola.Remove(chave);
        }

        public void Canalizar(float prog) { Canal = Mathf.Clamp01(prog); Cancelado = 0f; }
        public void CanalizarFim() { Canal = -1f; Cancelado = 0f; }
        /// <summary>INTERROMPEU: nao some caladinho — vira marca (cor + forma: anel ambar -> X vermelho).</summary>
        public void Cancelar()
        {
            if (Canal < 0f) return;
            Canal = -1f;
            Cancelado = CANCELADO_S;
        }

        public void Derrubar(bool caido) { Caido = caido; if (!caido) { Esvaecimento = 0f; Reerguer = 0f; } }
        public void Progresso(float esv, float reer) { Esvaecimento = Mathf.Clamp01(esv); Reerguer = Mathf.Clamp01(reer); }

        public void Zerar()
        {
            _faixa.Clear(); _contagens.Clear(); Arcos.Clear(); Bussola.Clear(); Badges.Clear();
            Vinheta = 0f; _vinhetaUltima = -99f; Canal = -1f; Cancelado = 0f; Caido = false; Resgatando = false;
        }

        public void Tick(float dt)
        {
            Agora += dt;
            if (Vinheta > 0f) Vinheta = Mathf.Max(Vinheta - dt * 2.2f, 0f);
            if (Cancelado > 0f) Cancelado = Mathf.Max(Cancelado - dt, 0f);
            var vencidas = new List<int>();
            foreach (var kv in _faixa) if (kv.Value.Ate > 0f && Agora >= kv.Value.Ate) vencidas.Add(kv.Key);
            foreach (int k in vencidas) _faixa.Remove(k);
            Arcos.RemoveAll(a => Agora >= a.Ate);
            TickContagens();
        }

        void TickContagens()
        {
            var acabaram = new List<int>();
            foreach (var kv in _contagens)
            {
                int s = Mathf.CeilToInt(kv.Value.Ate - Agora);
                if (s <= 0) { acabaram.Add(kv.Key); continue; }
                if (s != kv.Value.S)
                {
                    kv.Value.S = s;
                    Avisar(kv.Key, string.Format(kv.Value.Formato, s), kv.Value.Cor);
                }
            }
            foreach (int k in acabaram) { _contagens.Remove(k); _faixa.Remove(k); }
        }
    }

    /// <summary>
    /// Casca: desenha a faixa, badges, vinheta (4 tarjas — o meio fica livre para a mira), arcos (marcas no anel),
    /// setas de bussola, anel/X de canalizacao e o painel de DERRUBADO. UMA atualizacao por frame.
    /// </summary>
    public sealed class HudAviso : MonoBehaviour
    {
        public AvisoLogica Logica;
        /// <summary>Angulo de tela (rad, 0 = frente, horario) de um ponto do mundo — a Hud injeta (precisa de camera + jogador).</summary>
        public System.Func<Vector3, float> Angulo;
        public System.Func<string, string> RotuloEstado = s => s;
        public string TituloDerrubado = "VOCE FOI DERRUBADO";
        public string TituloAliado = "ALIADO DERRUBADO";
        public string TextoEsvaecendo = "ESVAECENDO {0}s";
        public string TextoReerguendo = "REERGUENDO";
        public float EsvaecerS = 30f;

        RectTransform _raiz;
        Text _faixa;
        Image _faixaPlaca;
        Image[] _faixaLosangos;
        string _faixaTexto;     // o que a placa mediu por ultimo: remede so' quando o texto muda (1x/s na contagem)
        Color _faixaCor;
        float _faixaMax;
        Text[] _badges;
        GameObject[] _chips;
        Image[] _vinheta;
        Image[] _arcos;
        readonly List<Image> _setas = new List<Image>();
        Image _canalFundo, _canalArco, _canalXis;
        RectTransform _painelDerrubado;
        Text _tituloDerrubado, _esvText, _reergText;
        Image _esvBarra, _reergArco;
        Margens _m;

        public static HudAviso Criar(Transform pai, AvisoLogica logica)
        {
            var go = new GameObject("Avisos", typeof(RectTransform), typeof(HudAviso));
            go.transform.SetParent(pai, false);
            var h = go.GetComponent<HudAviso>();
            h.Logica = logica;
            h._raiz = (RectTransform)go.transform;
            AreaSegura.Esticar(h._raiz);
            h.Montar();
            return h;
        }

        void Montar()
        {
            _vinheta = new Image[4];
            for (int i = 0; i < 4; i++) { _vinheta[i] = Formas.Imagem(_raiz, "Vinheta" + i, null, Color.clear); _vinheta[i].enabled = false; }
            _arcos = new Image[Logica.ArcoMax];
            for (int i = 0; i < _arcos.Length; i++)
            {
                _arcos[i] = Formas.Imagem(_raiz, "Arco" + i, Formas.Disco(), Color.clear);
                _arcos[i].rectTransform.anchorMin = new Vector2(0.5f, 0.5f); _arcos[i].rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                _arcos[i].rectTransform.sizeDelta = new Vector2(Dp.Px(14f), Dp.Px(14f));
                _arcos[i].enabled = false;
            }
            // faixa: PLACA escura atras do texto (fotos 08/10: o roxo solto sumia no verde e no ceu claro) com fio e dois
            // losangos na cor da PRIORIDADE; o texto segue a cor da prioridade. Largura = texto (medida so' quando ele muda).
            _faixaPlaca = Hud.Placa(_raiz, "FaixaFundo", Dp.Px(8f));
            var pr = _faixaPlaca.rectTransform;
            pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.zero; pr.pivot = new Vector2(0.5f, 0.5f);
            _faixaLosangos = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                var l = Formas.Imagem(pr, "Losango" + i, Formas.Losango(), Color.white);
                l.rectTransform.anchorMin = new Vector2(i, 0.5f); l.rectTransform.anchorMax = new Vector2(i, 0.5f);
                l.rectTransform.sizeDelta = new Vector2(Dp.Px(7f), Dp.Px(7f));
                l.rectTransform.anchoredPosition = new Vector2((i == 0 ? 1f : -1f) * Dp.Px(11f), 0f);
                _faixaLosangos[i] = l;
            }
            _faixaPlaca.gameObject.SetActive(false);
            _faixa = Formas.Texto(_raiz, "Faixa", "", 17f, Color.white);
            _faixa.fontStyle = FontStyle.Bold;
            _badges = new Text[AvisoLogica.MAX_BADGES];
            _chips = new GameObject[_badges.Length];
            for (int i = 0; i < _badges.Length; i++)
            {
                var chip = Hud.Placa(_raiz, "Badge" + i, Dp.Px(10f));   // capsula: raio = meia altura (20dp)
                _badges[i] = Formas.Texto(chip.transform, "Txt", "", 10f, new Color(0.92f, 0.96f, 1f, 0.95f));
                AreaSegura.Esticar(_badges[i].rectTransform);
                _chips[i] = chip.gameObject;
                _chips[i].SetActive(false);
            }
            _canalFundo = Formas.Imagem(_raiz, "CanalFundo", Formas.Anel(), new Color(0, 0, 0, 0.45f));
            _canalArco = Formas.Imagem(_raiz, "CanalArco", Formas.Anel(), Color.white);
            _canalArco.type = Image.Type.Filled; _canalArco.fillMethod = Image.FillMethod.Radial360;
            _canalArco.fillOrigin = (int)Image.Origin360.Top; _canalArco.fillClockwise = true;
            _canalXis = Formas.Imagem(_raiz, "CanalXis", Formas.Xis(), AvisoLogica.COR_CANCELADO);
            _painelDerrubado = Formas.No(_raiz, "Derrubado");
            _tituloDerrubado = Formas.Texto(_painelDerrubado, "Titulo", "", 13f, new Color(1f, 0.45f, 0.35f));
            var trilho = Formas.Imagem(_painelDerrubado, "Trilho", null, new Color(0, 0, 0, 0.5f));
            _esvBarra = Formas.Imagem(_painelDerrubado, "Esvaecimento", null, new Color(1f, 0.55f, 0.30f, 0.9f));
            _esvBarra.type = Image.Type.Filled; _esvBarra.fillMethod = Image.FillMethod.Horizontal;
            _esvText = Formas.Texto(_painelDerrubado, "EsvText", "", 11f, new Color(1, 1, 1, 0.85f));
            _reergArco = Formas.Imagem(_painelDerrubado, "Reerguer", Formas.Anel(), new Color(0.45f, 1f, 0.6f));
            _reergArco.type = Image.Type.Filled; _reergArco.fillMethod = Image.FillMethod.Radial360; _reergArco.fillOrigin = (int)Image.Origin360.Top;
            _reergText = Formas.Texto(_painelDerrubado, "ReergText", TextoReerguendo, 10f, new Color(0.7f, 1f, 0.8f, 0.9f));
            // layout fixo do painel (relativo ao proprio no')
            AreaSegura.NoRect(_tituloDerrubado.rectTransform, new Rect(0, Dp.Px(44f), Dp.Px(240f), Dp.Px(20f)));
            AreaSegura.NoRect(trilho.rectTransform, new Rect(0, Dp.Px(30f), Dp.Px(240f), Dp.Px(10f)));
            AreaSegura.NoRect(_esvBarra.rectTransform, new Rect(0, Dp.Px(30f), Dp.Px(240f), Dp.Px(10f)));
            AreaSegura.NoRect(_esvText.rectTransform, new Rect(0, Dp.Px(8f), Dp.Px(240f), Dp.Px(18f)));
            AreaSegura.NoRect(_reergArco.rectTransform, new Rect(Dp.Px(102f), -Dp.Px(40f), Dp.Px(36f), Dp.Px(36f)));
            AreaSegura.NoRect(_reergText.rectTransform, new Rect(0, -Dp.Px(58f), Dp.Px(240f), Dp.Px(16f)));
        }

        /// <summary>Retangulos publicos (px de tela, origem inferior esquerda): o teste cobra area segura neles.</summary>
        public static Rect RectFaixa(Vector2 tela, Margens m, float px) =>
            new Rect(m.Esq + 8f * px, tela.y - m.Topo - 44f * px - 26f * px, Mathf.Max(tela.x - m.Esq - m.Dir - 16f * px, 1f), 26f * px);
        public static Rect RectBadges(Vector2 tela, Margens m, float px) =>
            new Rect(m.Esq + 16f * px, tela.y - m.Topo - 92f * px - 20f * px, Mathf.Max(tela.x * 0.5f - m.Esq, 1f), 20f * px);
        public static Rect RectCanalizar(Vector2 tela, Margens m, float px) =>
            new Rect((tela.x - 52f * px) / 2f, m.Baixo + 196f * px - 52f * px, 52f * px, 52f * px);
        public static Rect RectDerrubado(Vector2 tela, Margens m, float px) =>
            new Rect((tela.x - 240f * px) / 2f, m.Baixo + 150f * px - 64f * px, 240f * px, 64f * px);

        public void Layout(Vector2 tela, Margens m)
        {
            _m = m;
            float px = Dp.Px(1f);
            Rect rf = RectFaixa(tela, m, px);
            AreaSegura.NoRect(_faixa.rectTransform, rf);
            _faixaPlaca.rectTransform.anchoredPosition = rf.center;   // pivo no meio: a largura cresce para os dois lados
            _faixaMax = rf.width;
            _faixaTexto = null;   // tela nova: remede
            Rect rb = RectBadges(tela, m, px);
            float x = rb.xMin;
            for (int i = 0; i < _badges.Length; i++)
            {
                var chip = (RectTransform)_badges[i].transform.parent;
                AreaSegura.NoRect(chip, new Rect(x, rb.yMin, Dp.Px(96f), rb.height));
                x += Dp.Px(96f) + Dp.Px(6f);
            }
            Rect rc = RectCanalizar(tela, m, px);
            AreaSegura.NoRect(_canalFundo.rectTransform, rc);
            AreaSegura.NoRect(_canalArco.rectTransform, rc);
            AreaSegura.NoRect(_canalXis.rectTransform, rc);
            Rect rd = RectDerrubado(tela, m, px);
            AreaSegura.NoRect(_painelDerrubado, rd);
            float e = Mathf.Min(tela.x, tela.y) * 0.13f;
            AreaSegura.NoRect(_vinheta[0].rectTransform, new Rect(0, tela.y - e, tela.x, e));
            AreaSegura.NoRect(_vinheta[1].rectTransform, new Rect(0, 0, tela.x, e));
            AreaSegura.NoRect(_vinheta[2].rectTransform, new Rect(0, 0, e, tela.y));
            AreaSegura.NoRect(_vinheta[3].rectTransform, new Rect(tela.x - e, 0, e, tela.y));
        }

        void Update()
        {
            if (Logica == null) return;
            Logica.Tick(Time.deltaTime);
            Vector2 tela = new Vector2(Screen.width, Screen.height);
            // vinheta
            Color vc = Formas.ComAlfa(Logica.VinhetaCor, 0.42f * Logica.Vinheta);
            for (int i = 0; i < 4; i++) { _vinheta[i].enabled = Logica.Vinheta > 0f; _vinheta[i].color = vc; }
            // faixa: texto e largura da placa so' quando o texto muda; cor (texto, fio, losangos) so' quando a prioridade troca
            var f = Logica.FaixaVencedora();
            string txt = f != null && f.Texto != null ? f.Texto : "";
            if (txt != _faixaTexto)
            {
                _faixaTexto = txt;
                _faixa.text = txt;
                _faixaPlaca.gameObject.SetActive(txt.Length > 0);
                if (txt.Length > 0)
                    _faixaPlaca.rectTransform.sizeDelta = new Vector2(Mathf.Min(Mathf.Ceil(_faixa.preferredWidth + Dp.Px(46f)), _faixaMax), Dp.Px(30f));
            }
            if (f != null && f.Cor != _faixaCor)
            {
                _faixaCor = f.Cor;
                _faixa.color = f.Cor;
                _faixaPlaca.color = Formas.ComAlfa(f.Cor, 0.85f);
                _faixaLosangos[0].color = f.Cor; _faixaLosangos[1].color = f.Cor;
            }
            // badges
            for (int i = 0; i < _badges.Length; i++)
            {
                bool on = i < Logica.Badges.Count;
                if (_chips[i].activeSelf != on) _chips[i].SetActive(on);
                if (on) _badges[i].text = RotuloEstado(Logica.Badges[i]);
            }
            // arcos e bussola: angulos giram com a camera
            float raioArco = Mathf.Min(tela.x, tela.y) * 0.42f;
            for (int i = 0; i < _arcos.Length; i++)
            {
                bool on = i < Logica.Arcos.Count && Angulo != null;
                _arcos[i].enabled = on;
                if (!on) continue;
                var a = Logica.Arcos[i];
                float vida = Mathf.Clamp01((a.Ate - Logica.Agora) / Logica.ArcoDurS);
                float ang = Angulo(a.Pos);
                _arcos[i].rectTransform.anchoredPosition = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang)) * raioArco;
                _arcos[i].color = Formas.ComAlfa(a.Cor, 0.85f * vida);
            }
            int k = 0;
            float raioSeta = Mathf.Min(tela.x, tela.y) * 0.32f;
            foreach (var kv in Logica.Bussola)
            {
                if (Angulo == null) break;
                if (k >= _setas.Count)
                {
                    var s = Formas.Imagem(_raiz, "Seta" + k, Formas.Seta(), Color.white);
                    s.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); s.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    s.rectTransform.sizeDelta = new Vector2(Dp.Px(22f), Dp.Px(22f));
                    _setas.Add(s);
                }
                float ang = Angulo(kv.Value.Key);
                var img = _setas[k];
                img.enabled = true;
                img.color = kv.Value.Value;
                img.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang)) * raioSeta;
                img.rectTransform.localRotation = Quaternion.Euler(0, 0, -ang * Mathf.Rad2Deg);
                k++;
            }
            for (; k < _setas.Count; k++) _setas[k].enabled = false;
            // canalizacao (no chao o painel de DERRUBADO ocupa o lugar)
            bool canal = !Logica.Caido && Logica.Canal >= 0f;
            bool xis = !Logica.Caido && Logica.Cancelado > 0f;
            _canalFundo.enabled = canal || xis;
            _canalArco.enabled = canal;
            _canalArco.fillAmount = Mathf.Max(Logica.Canal, 0f);
            _canalXis.enabled = xis;
            float ax = Mathf.Clamp01(Logica.Cancelado / AvisoLogica.CANCELADO_S);
            _canalFundo.color = xis ? Formas.ComAlfa(AvisoLogica.COR_CANCELADO, 0.3f * ax) : new Color(0, 0, 0, 0.45f);
            _canalXis.color = Formas.ComAlfa(AvisoLogica.COR_CANCELADO, ax);
            // derrubado
            bool painel = Logica.Caido || Logica.Resgatando;
            _painelDerrubado.gameObject.SetActive(painel);
            if (painel)
            {
                _tituloDerrubado.text = Logica.Caido ? TituloDerrubado : TituloAliado;
                _esvBarra.enabled = Logica.Caido;
                _esvBarra.fillAmount = Logica.Esvaecimento;
                _esvText.text = Logica.Caido ? string.Format(TextoEsvaecendo, Mathf.CeilToInt(Logica.Esvaecimento * EsvaecerS)) : "";
                _reergArco.enabled = Logica.Reerguer > 0f;
                _reergArco.fillAmount = Logica.Reerguer;
                _reergText.enabled = Logica.Reerguer > 0f;
            }
        }

        public void CorCanal(Color c) { _canalArco.color = c; }
    }
}
