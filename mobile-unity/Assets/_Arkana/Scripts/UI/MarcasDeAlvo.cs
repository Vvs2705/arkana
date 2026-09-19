using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.Characters;
using Arkana.Gameplay;
using Arkana.Menu;

namespace Arkana.UI
{
    /// <summary>
    /// Logica PURA das MARCAS DE ALVO (barra de escudo + vida e nome sobre a cabeca do inimigo): quem aparece, quanto
    /// fica, em que ordem e com que rastro. Aparece quem o jogador ACERTOU (segura SeguraS e esvaece em SomeS) e quem esta'
    /// SOB A MIRA e a vista (Visada) ate' MiraM, mesmo sem acerto; some atras da camera, alem de LongeM, morto ou caido. No maximo Max, as
    /// mais perto primeiro. Todo corpo da arena e' medido todo quadro: o rastro precisa do valor de ANTES do golpe (o
    /// Bus.DamageApplied sai depois que a vida ja' caiu). Nada aqui desenha.
    /// ALIADO (Dupla.Aliado) nunca recebe marca de inimigo: o PARCEIRO tem a dele, AZUL e permanente (nome + vida; fora de
    /// vista, presa na borda com a seta — NaBorda).
    /// </summary>
    public sealed class MarcasLogica
    {
        /// <summary>s que a marca fica depois do ultimo acerto e s do esvaecer. KNOB por foto.</summary>
        public const float SeguraS = 3f, SomeS = 0.45f;
        /// <summary>A MIRA: alcance (m), folga entre o raio da camera e o meio do corpo (m) e quanto a marca segura depois
        /// que a mira sai (s: o balanco do dedo nao pisca a barra). KNOB por foto.</summary>
        public const float MiraM = 40f, MiraFolgaM = 1f, MiraSeguraS = 0.3f;
        /// <summary>m alem dos quais nada aparece (nem acertado) e o teto de marcas na tela.</summary>
        public const float LongeM = 60f;
        public const int Max = 6;
        /// <summary>O RASTRO, o mesmo da barra do jogador (Hud.BarraHud): segura o valor velho e desce a fracao/s.</summary>
        public const float RastroEsperaS = 0.35f, RastroVel = 0.8f;
        /// <summary>m do pe' ao meio do corpo (distancia e mira medem daqui) e quanto a visada para antes dele (a capsula do
        /// corpo tem 0,35 m de raio: tocar nela seria "bloqueado pelo proprio alvo").</summary>
        const float MeioM = 0.9f, RecuoM = 0.6f;

        /// <summary>Uma barra com RASTRO: `Fantasma` segura o valor de antes do golpe e desce ate' `Frac`. Cura sobe sem rastro.</summary>
        public struct Rastro
        {
            public float Frac, Fantasma;
            float _espera;

            public void Encher(float f) { Frac = Fantasma = f; _espera = 0f; }

            public void Medir(float f, float dt)
            {
                if (f < Frac - 0.0001f) _espera = RastroEsperaS;   // caiu: golpe seguido segura mais (a rajada le' inteira)
                Frac = f;
                if (Fantasma <= f) { Fantasma = f; return; }
                if (_espera > 0f) { _espera -= dt; return; }
                Fantasma = Mathf.Max(Fantasma - RastroVel * dt, f);
            }
        }

        public sealed class Marca
        {
            public IEntidade Alvo;
            public Rastro Vida, Escudo;
            public int Nivel;
            /// <summary>m ate' a camera; 0..1 do esvaecer; o raio da camera passa por ele neste quadro.</summary>
            public float Dist, Alfa;
            public bool NaMira;
            /// <summary>Segura a marca inteira ate' `Ate`; a mira nao a traz de volta antes de `Calado` (acabou de morrer).</summary>
            public float Ate = -99f, Calado = -99f;
        }

        public float Agora { get; private set; }
        /// <summary>O inimigo SOB A MIRA mais perto neste quadro (null = nenhum): o parceiro bot mira o mesmo (contrato 17G).</summary>
        public IEntidade SobAMira { get; private set; }
        /// <summary>O parceiro medido (null = sem dupla, ou fora) e a vida dele com rastro.</summary>
        public IEntidade Parceiro { get; private set; }
        public Rastro VidaParceiro;
        /// <summary>LINHA DE VISADA da mira (olho, ponto) -> livre? Sem ela a mira varria pedra e muro e acendia quem esta'
        /// atras (wallhack). Null = tudo livre (o teste puro). O acerto nao pede: quem apanhou foi visto.</summary>
        public System.Func<Vector3, Vector3, bool> Visada;
        /// <summary>O que a tela desenha neste quadro, da mais perto para a mais longe (no maximo Max).</summary>
        public readonly List<Marca> Visiveis = new List<Marca>(Max + 1);
        readonly Dictionary<IEntidade, Marca> _marcas = new Dictionary<IEntidade, Marca>();   // uma por corpo, a partida inteira

        static float Frac(float cur, float max) => max > 0f ? Mathf.Clamp01(cur / max) : 0f;

        /// <summary>O jogador acertou: a marca acende (ou renova) por SeguraS.</summary>
        public void Acertou(IEntidade alvo)
        {
            if (alvo == null || alvo.Vital == null) return;
            Marca m = Pegar(alvo);
            m.Ate = Agora + SeguraS;
            m.Calado = -99f;
        }

        /// <summary>Morreu: a marca sai NA HORA (a faixa ELIMINADO assume) e a mira nao a traz de volta por SeguraS — o
        /// boneco do treino levanta no quadro seguinte, e a barra cheia em cima da alma confundia.</summary>
        public void Esquecer(IEntidade alvo)
        {
            Marca m;
            if (alvo == null || !_marcas.TryGetValue(alvo, out m)) return;
            m.Ate = -99f;
            m.Alfa = 0f;
            m.Calado = Agora + SeguraS;
        }

        /// <summary>Por quadro: mede cada corpo (vida, escudo, rastro), decide quem aparece e ordena. `olho`/`frente` = a
        /// camera (frente normalizada). Arena nula (fora de partida) = nada na tela.</summary>
        public void Atualizar(float dt, IList<IEntidade> arena, IEntidade jogador, Vector3 olho, Vector3 frente)
        {
            Agora += dt;
            Visiveis.Clear();
            SobAMira = null;
            if (arena == null) return;
            float sob = float.PositiveInfinity;
            for (int i = 0; i < arena.Count; i++)
            {
                IEntidade e = arena[i];
                if (e == null || e == jogador || e.EhPlayer || e.Vital == null || Dupla.Aliado(jogador, e)) continue;   // aliado: a marca azul e' outra
                Vitalidade v = e.Vital;
                Marca m = Pegar(e);
                m.Vida.Medir(Frac(v.Hp, v.HpMax), dt);
                m.Escudo.Medir(Frac(v.Escudo, v.EscudoMax), dt);
                m.Nivel = v.Nivel;
                m.NaMira = false;
                if (!v.Viva || Derrubado.Esta(e)) continue;   // o caido ja' tem o losango do VisualDoAbate em cima
                Vector3 d = e.Pos + Vector3.up * MeioM - olho;
                float z = Vector3.Dot(d, frente);
                m.Dist = d.magnitude;
                if (z <= 0f || m.Dist > LongeM) continue;
                // a visada por ultimo (so' quem ja' esta' no raio paga o raycast); termina ANTES da capsula do proprio alvo
                m.NaMira = m.Dist <= MiraM && Agora >= m.Calado && (d - frente * z).sqrMagnitude <= MiraFolgaM * MiraFolgaM
                    && (Visada == null || Visada(olho, olho + d * (1f - RecuoM / m.Dist)));
                if (m.NaMira) m.Ate = Mathf.Max(m.Ate, Agora + MiraSeguraS);
                if (m.NaMira && m.Dist < sob) { sob = m.Dist; SobAMira = e; }
                m.Alfa = Agora < m.Ate ? 1f : Mathf.Clamp01(1f - (Agora - m.Ate) / SomeS);
                if (m.Alfa <= 0f) continue;
                // insercao por distancia com teto (a lista nasce com Max + 1 de capacidade: zero lixo por quadro)
                int k = Visiveis.Count;
                while (k > 0 && Visiveis[k - 1].Dist > m.Dist) k--;
                if (k >= Max) continue;
                Visiveis.Insert(k, m);
                if (Visiveis.Count > Max) Visiveis.RemoveAt(Max);
            }
        }

        /// <summary>O PARCEIRO por quadro: a vida com rastro (parceiro novo nasce cheio, sem rastro). Null/morto = sem marca.</summary>
        public void MedirParceiro(float dt, IEntidade parceiro)
        {
            if (parceiro == null || parceiro.Vital == null || !parceiro.Vital.Viva) { Parceiro = null; return; }
            float f = Frac(parceiro.Vital.Hp, parceiro.Vital.HpMax);
            if (parceiro != Parceiro) VidaParceiro.Encher(f); else VidaParceiro.Medir(f, dt);
            Parceiro = parceiro;
        }

        /// <summary>Espaco da camera (x direita, y cima, olha para -z) -> viewport para a marca do parceiro. Na frente, a
        /// projecao; ATRAS, um ponto muito longe na direcao (x, -z) — de lado e para baixo — que o NaBorda prende na borda (a
        /// projecao pura espelharia quem esta' as costas para a frente da tela).</summary>
        public static Vector2 ParaViewport(Vector3 c, float tanMeiaV, float aspecto)
        {
            float z = -c.z;
            if (z > 0.01f) return new Vector2(0.5f + 0.5f * c.x / (z * tanMeiaV * aspecto), 0.5f + 0.5f * c.y / (z * tanMeiaV));
            Vector2 d = new Vector2(c.x, -Mathf.Max(c.z, 0.01f));
            return new Vector2(0.5f, 0.5f) + d.normalized * 100f;
        }

        /// <summary>Prende `vp` na `caixa` (viewport): dentro = no lugar (false); fora = na BORDA, na direcao do centro para
        /// ele, e o angulo da SETA em rad (0 = para cima, horario) medido na tela (`aspecto` = largura/altura).</summary>
        public static bool NaBorda(Vector2 vp, Rect caixa, float aspecto, out Vector2 pos, out float angulo)
        {
            angulo = 0f;
            if (caixa.Contains(vp)) { pos = vp; return false; }
            Vector2 c = caixa.center, d = vp - c;
            if (d.sqrMagnitude < 1e-8f) d = Vector2.down;
            float kx = Mathf.Abs(d.x) > 1e-6f ? caixa.width * 0.5f / Mathf.Abs(d.x) : float.PositiveInfinity;
            float ky = Mathf.Abs(d.y) > 1e-6f ? caixa.height * 0.5f / Mathf.Abs(d.y) : float.PositiveInfinity;
            pos = c + d * Mathf.Min(kx, ky);
            angulo = Mathf.Atan2(d.x * aspecto, d.y);
            return true;
        }

        /// <summary>A marca do corpo; a primeira nasce com o valor de agora, sem rastro (nao houve golpe).</summary>
        Marca Pegar(IEntidade e)
        {
            Marca m;
            if (_marcas.TryGetValue(e, out m)) return m;
            m = new Marca { Alvo = e, Nivel = e.Vital.Nivel };
            m.Vida.Encher(Frac(e.Vital.Hp, e.Vital.HpMax));
            m.Escudo.Encher(Frac(e.Vital.Escudo, e.Vital.EscudoMax));
            _marcas[e] = m;
            return m;
        }
    }

    /// <summary>
    /// A casca das MARCAS DE ALVO: Max placas prontas (o pool e' o teto: nada de Instantiate/Destroy por golpe). Cada uma
    /// e' o NOME curto do mago em cima e a PLACA da HUD (fio de ouro fosco + miolo escuro) com a barra de ESCUDO fina na
    /// cor do nivel — com os separadores de nivel da barra do jogador: forma antes da cor — sobre a barra de VIDA, as duas
    /// com o RASTRO do golpe. Ancora no ponto de viewport acima da cabeca (TELA CHEIA, nao area segura: o mesmo cuidado
    /// do numero de dano), encolhe de leve com a distancia e esvaece com a logica; quem esta' sob a mira ganha o fio no
    /// ouro vivo. A mais perto desenha por cima. Por quadro so' toca no que mudou.
    /// </summary>
    public sealed class MarcasDeAlvo
    {
        /// <summary>dp: largura da placa, barra de escudo, barra de vida, folga do fio ate' as barras, vao entre elas e o
        /// nome. 58dp ~ 143 px no Poco F4: le' de longe sem tampar o corpo de perto. KNOB por foto.</summary>
        const float LarguraDp = 58f, EscudoDp = 3.5f, VidaDp = 5.5f, FolgaDp = 2f, VaoDp = 1f, NomeDp = 10f;
        /// <summary>m acima do topo da cabeca (a altura de cada mago) onde a placa pousa.</summary>
        const float AcimaM = 0.3f;
        /// <summary>A escala: 1 ate' EscalaPertoM e EscalaLonge em MarcasLogica.LongeM — leve de proposito. KNOB por foto.</summary>
        const float EscalaPertoM = 8f, EscalaLonge = 0.72f;
        /// <summary>Alfa do rastro do ESCUDO na cor do nivel: o N1 e' BRANCO, e o creme da vida sumia em cima dele.</summary>
        const float RastroEscudoAlfa = 0.4f;
        static readonly Color CorVida = new Color(0.88f, 0.22f, 0.2f);            // a da barra do jogador
        static readonly Color CorRastro = new Color(1f, 0.93f, 0.8f, 0.85f);      // idem

        public readonly MarcasLogica Logica = new MarcasLogica();
        readonly Tela[] _telas;
        readonly Color[] _niveis;
        readonly TelaParceiro _parceiro;

        public MarcasDeAlvo(RectTransform pai)
        {
            var raiz = Formas.No(pai, "Marcas");
            AreaSegura.Esticar(raiz);
            string[] cores = Balance.Escudo.Cores;
            _niveis = new Color[cores.Length];
            for (int i = 0; i < cores.Length; i++) _niveis[i] = Formas.Cor(cores[i]);   // 1x: o Hex aloca string
            _telas = new Tela[MarcasLogica.Max];
            for (int i = 0; i < _telas.Length; i++) _telas[i] = new Tela(raiz, i);
            _parceiro = new TelaParceiro(raiz);   // depois das de inimigo: o aliado desenha por cima
            // ponytail: corpo (CharacterController) nao tampa a visada — o do proprio jogador cruzava a linha de perto. Parede
            // ATRAS de um corpo passa, e moita sem colisor nao esconde: RaycastNonAlloc com a lista toda se isso aparecer em jogo.
            Logica.Visada = (a, b) =>
            {
                RaycastHit h;
                return !Physics.Linecast(a, b, out h, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || h.collider is CharacterController;
            };
        }

        /// <summary>Por quadro (dt sem escala): mede a arena da partida pela camera do jogo e recoloca as placas.</summary>
        public void Pintar(float dt, IEntidade jogador)
        {
            Partida p = Partida.Atual;
            Camera cam = Camera.main;
            if (p != null && cam != null) Logica.Atualizar(dt, p.Arena, jogador, cam.transform.position, cam.transform.forward);
            else Logica.Atualizar(dt, null, jogador, Vector3.zero, Vector3.forward);
            Logica.MedirParceiro(dt, p != null ? p.ParceiroVivo() : null);
            Posicionar();
        }

        /// <summary>So' reprojeta (a logica nao anda). A Hud.Layout chama: a foto troca o alvo da camera sem renderizar.</summary>
        public void Posicionar()
        {
            Camera cam = Camera.main;
            List<MarcasLogica.Marca> vis = Logica.Visiveis;
            int n = cam != null ? vis.Count : 0;
            // a mais PERTO no ultimo irmao usado: desenha por cima das de longe
            for (int k = 0; k < _telas.Length; k++) _telas[k].Pintar(k < n ? vis[n - 1 - k] : null, cam, _niveis);
            _parceiro.Pintar(cam != null ? Logica.Parceiro : null, cam, Logica.VidaParceiro);
        }

        /// <summary>A caixa (viewport) em que a marca do parceiro fica presa quando ele sai da vista: longe dos cantos da HUD
        /// (barras, relogio e minimapa em cima, joystick e botoes embaixo) e da faixa de aviso. KNOB por foto.</summary>
        static readonly Rect CaixaDaBorda = Rect.MinMaxRect(0.2f, 0.26f, 0.8f, 0.8f);

        /// <summary>
        /// A marca do PARCEIRO: a mesma placa das de inimigo com o fio no AZUL-ALIADO, o nome e a vida (com rastro) no azul.
        /// Permanente (nao esvaece, nao conta no teto). Na vista, pousa acima da cabeca; fora dela (ou as costas), presa na
        /// borda da CaixaDaBorda com a SETA apontando para onde ele esta'.
        /// </summary>
        sealed class TelaParceiro
        {
            readonly RectTransform _rt, _seta;
            readonly Image _vida, _rastro;
            readonly Text _nome;
            IEntidade _dono;
            bool _ligada, _borda;
            float _fv = -1f, _rv = -1f;

            public TelaParceiro(RectTransform pai)
            {
                float folga = Dp.Px(FolgaDp), hv = Dp.Px(VidaDp + 1f);
                float placaH = 2f * folga + hv;
                _rt = Formas.No(pai, "MarcaParceiro");
                _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
                _rt.pivot = new Vector2(0.5f, 0f);
                _rt.sizeDelta = new Vector2(Dp.Px(LarguraDp), placaH + Dp.Px(NomeDp + 4f));
                var fio = Hud.Placa(_rt, "Placa", Dp.Px(3f));
                fio.color = Dupla.CorAliado;
                Tela.Faixa(fio.rectTransform, 0f, placaH, 0f);
                RectTransform tv = Tela.Faixa(Formas.No(fio.transform, "TrilhoVida"), folga, hv, folga);
                _rastro = Tela.Barra(tv, "Rastro", CorRastro, hv, false);
                _vida = Tela.Barra(tv, "Fill", Dupla.CorAliado, hv, true);
                _nome = Formas.Texto(_rt, "Nome", "", NomeDp, Color.Lerp(Dupla.CorAliado, Color.white, 0.55f));
                _nome.fontStyle = FontStyle.Bold;
                Tela.Faixa(_nome.rectTransform, placaH + Dp.Px(1f), Dp.Px(NomeDp + 3f), 0f);
                _nome.GetComponent<Shadow>().effectDistance = new Vector2(Dp.Px(1.2f), -Dp.Px(1.2f));
                var o = _nome.gameObject.AddComponent<Outline>();
                o.effectColor = Formas.ComAlfa(Estilo.NoiteFunda, 0.85f);
                o.effectDistance = new Vector2(Dp.Px(0.8f), -Dp.Px(0.8f));
                var seta = Formas.Imagem(_rt, "Seta", Formas.Seta(), Dupla.CorAliado);
                _seta = seta.rectTransform;
                _seta.anchorMin = _seta.anchorMax = new Vector2(0.5f, 0.5f);
                _seta.sizeDelta = Vector2.one * Dp.Px(14f);
                _seta.gameObject.SetActive(false);
                _rt.gameObject.SetActive(false);
            }

            public void Pintar(IEntidade e, Camera cam, MarcasLogica.Rastro vida)
            {
                bool liga = e != null && cam != null;
                if (liga != _ligada) { _ligada = liga; _rt.gameObject.SetActive(liga); }
                if (!liga) return;
                if (e != _dono) { _dono = e; _nome.text = (e.Nome ?? "").ToUpperInvariant(); }
                Rect r = cam.pixelRect;
                float aspecto = r.width / Mathf.Max(r.height, 1f);
                Vector3 c = cam.worldToCameraMatrix.MultiplyPoint3x4(Topo(e));
                Vector2 vp = MarcasLogica.ParaViewport(c, Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), aspecto);
                Vector2 pos;
                float ang;
                bool borda = MarcasLogica.NaBorda(vp, CaixaDaBorda, aspecto, out pos, out ang);
                _rt.anchorMin = _rt.anchorMax = pos;
                if (borda != _borda)
                {
                    _borda = borda;
                    _rt.pivot = borda ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0f);   // na vista o pe' pousa acima da cabeca
                    _seta.gameObject.SetActive(borda);
                }
                if (borda)
                {
                    Vector2 meia = _rt.sizeDelta * 0.5f + Vector2.one * Dp.Px(9f);
                    _seta.anchoredPosition = new Vector2(Mathf.Sin(ang) * meia.x, Mathf.Cos(ang) * meia.y);
                    _seta.localRotation = Quaternion.Euler(0f, 0f, -ang * Mathf.Rad2Deg);   // a Seta aponta para cima; o giro e' anti-horario
                }
                Tela.Ancorar(_rastro, vida.Fantasma, ref _rv);
                Tela.Ancorar(_vida, vida.Frac, ref _fv);
            }
        }

        /// <summary>
        /// Mundo -> viewport (z = m a frente da camera) no alvo ATUAL da camera (pixelRect). O WorldToViewportPoint usa o
        /// aspecto que a camera guardou, e a foto troca o alvo (2400x1080 sobre a tela de 640x480 do teste) sem renderizar:
        /// a marca de quem esta' fora do centro sairia do corpo.
        /// </summary>
        static Vector3 NaTela(Camera cam, Vector3 mundo)
        {
            Vector3 c = cam.worldToCameraMatrix.MultiplyPoint3x4(mundo);   // espaco da camera: ela olha para -z
            float z = -c.z;
            if (z <= 0.01f) return new Vector3(0f, 0f, z);
            float t = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            Rect r = cam.pixelRect;
            float aspecto = r.width / Mathf.Max(r.height, 1f);
            return new Vector3(0.5f + 0.5f * c.x / (z * t * aspecto), 0.5f + 0.5f * c.y / (z * t), z);
        }

        /// <summary>O ponto acima da cabeca: a altura de cada mago (Pip 0,6 m, Basalto 2,3 m), como o losango do caido.</summary>
        static Vector3 Topo(IEntidade e)
        {
            var p = e as Pawn;
            float alt = p != null && p.Visual != null ? p.Visual.Altura : Mago.AlturaRef;
            return e.Pos + Vector3.up * (alt + AcimaM);
        }

        /// <summary>Uma placa do pool. Guarda o ultimo valor aplicado de cada coisa: parado nao toca em nada.</summary>
        sealed class Tela
        {
            readonly RectTransform _rt;
            readonly CanvasGroup _grupo;
            readonly Image _fio, _vida, _rastroVida, _escudo, _rastroEscudo;
            readonly Image[] _segs = new Image[3];   // separadores dos niveis 2..4
            readonly Text _nome;
            IEntidade _dono;
            bool _ligada, _foco;
            int _nivel = -1;
            float _alfa = -1f, _escala = -1f, _fv = -1f, _rv = -1f, _fe = -1f, _re = -1f;

            public Tela(RectTransform pai, int i)
            {
                float folga = Dp.Px(FolgaDp), he = Dp.Px(EscudoDp), hv = Dp.Px(VidaDp), vao = Dp.Px(VaoDp);
                float placaH = 2f * folga + he + vao + hv;
                _rt = Formas.No(pai, "Marca" + i);
                _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
                _rt.pivot = new Vector2(0.5f, 0f);   // o pe' da marca pousa acima da cabeca; a escala encolhe para ele
                _rt.anchoredPosition = Vector2.zero;
                _rt.sizeDelta = new Vector2(Dp.Px(LarguraDp), placaH + Dp.Px(NomeDp + 4f));
                _grupo = _rt.gameObject.AddComponent<CanvasGroup>();   // o esvaecer pega placa, barras e nome de uma vez
                _grupo.blocksRaycasts = false;
                _grupo.interactable = false;
                _fio = Hud.Placa(_rt, "Placa", Dp.Px(3f));
                Faixa(_fio.rectTransform, 0f, placaH, 0f);
                RectTransform tv = Faixa(Formas.No(_fio.transform, "TrilhoVida"), folga, hv, folga);
                RectTransform te = Faixa(Formas.No(_fio.transform, "TrilhoEscudo"), folga + hv + vao, he, folga);
                _rastroVida = Barra(tv, "Rastro", CorRastro, hv, false);
                _vida = Barra(tv, "Fill", CorVida, hv, true);
                _rastroEscudo = Barra(te, "Rastro", Color.white, he, false);
                _escudo = Barra(te, "Fill", Color.white, he, true);
                for (int s = 0; s < _segs.Length; s++)
                {
                    _segs[s] = Formas.Imagem(te, "Seg" + (s + 2), null, new Color(0f, 0f, 0f, 0.6f));
                    _segs[s].rectTransform.sizeDelta = new Vector2(Mathf.Max(Dp.Px(1f), 1f), 0f);
                    _segs[s].enabled = false;
                }
                _nome = Formas.Texto(_rt, "Nome", "", NomeDp, new Color(1f, 1f, 1f, 0.95f));
                _nome.fontStyle = FontStyle.Bold;
                Faixa(_nome.rectTransform, placaH + Dp.Px(1f), Dp.Px(NomeDp + 3f), 0f);
                // contorno escuro + sombra de 1,2dp: o Shadow de 1 px do Formas.Texto some a 395 ppi, e o nome fica no ceu
                _nome.GetComponent<Shadow>().effectDistance = new Vector2(Dp.Px(1.2f), -Dp.Px(1.2f));
                var o = _nome.gameObject.AddComponent<Outline>();
                o.effectColor = Formas.ComAlfa(Estilo.NoiteFunda, 0.85f);
                o.effectDistance = new Vector2(Dp.Px(0.8f), -Dp.Px(0.8f));
                _rt.gameObject.SetActive(false);
            }

            /// <summary>Faixa horizontal do pai: de `y` a `y + h` px a partir de baixo, recuada `x` px dos lados.</summary>
            internal static RectTransform Faixa(RectTransform rt, float y, float h, float x)
            {
                rt.anchorMin = Vector2.zero; rt.anchorMax = new Vector2(1f, 0f); rt.pivot = new Vector2(0.5f, 0f);
                rt.offsetMin = new Vector2(x, y); rt.offsetMax = new Vector2(-x, y + h);
                return rt;
            }

            internal static Image Barra(RectTransform trilho, string nome, Color cor, float h, bool degrade)
            {
                var img = Formas.Arredondada(trilho, nome, cor, Mathf.Max(Mathf.Min(Dp.Px(1.5f), h * 0.5f), 0.5f), degrade);
                AreaSegura.Esticar(img.rectTransform);
                return img;
            }

            /// <summary>Preenche ate' a fracao `f` (anchorMax.x, como a BarraHud) so' se mudou.</summary>
            internal static void Ancorar(Image img, float f, ref float atual)
            {
                if (f == atual) return;
                atual = f;
                img.enabled = f > 0.001f;
                img.rectTransform.anchorMax = new Vector2(f, 1f);
            }

            public void Pintar(MarcasLogica.Marca m, Camera cam, Color[] niveis)
            {
                Vector3 vp = m != null ? NaTela(cam, Topo(m.Alvo)) : Vector3.zero;
                bool liga = m != null && vp.z > 0f;
                if (liga != _ligada) { _ligada = liga; _rt.gameObject.SetActive(liga); }
                if (!liga) return;
                _rt.anchorMin = _rt.anchorMax = new Vector2(vp.x, vp.y);
                if (m.Alvo != _dono) { _dono = m.Alvo; _nome.text = (m.Alvo.Nome ?? "").ToUpperInvariant(); }   // 1x por dono
                if (m.Alfa != _alfa) { _alfa = m.Alfa; _grupo.alpha = m.Alfa; }
                float s = Mathf.Lerp(1f, EscalaLonge, Mathf.InverseLerp(EscalaPertoM, MarcasLogica.LongeM, m.Dist));
                if (s != _escala) { _escala = s; _rt.localScale = new Vector3(s, s, 1f); }
                Ancorar(_rastroVida, m.Vida.Fantasma, ref _rv);
                Ancorar(_vida, m.Vida.Frac, ref _fv);
                Ancorar(_rastroEscudo, m.Escudo.Fantasma, ref _re);
                Ancorar(_escudo, m.Escudo.Frac, ref _fe);
                if (m.Nivel != _nivel)
                {
                    _nivel = m.Nivel;
                    int n = Mathf.Clamp(_nivel, 1, niveis.Length);
                    Color cor = niveis[n - 1];
                    _escudo.color = cor;
                    _rastroEscudo.color = Formas.ComAlfa(cor, RastroEscudoAlfa);
                    for (int j = 0; j < _segs.Length; j++)
                    {
                        _segs[j].enabled = j < n - 1;
                        float x = (j + 1) / (float)n;
                        _segs[j].rectTransform.anchorMin = new Vector2(x, 0f);
                        _segs[j].rectTransform.anchorMax = new Vector2(x, 1f);
                    }
                }
                if (m.NaMira != _foco) { _foco = m.NaMira; _fio.color = _foco ? Estilo.Ouro : Formas.ComAlfa(Estilo.OuroFosco, 0.95f); }
            }
        }
    }
}
