using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Arkana.Core;

namespace Arkana.UI
{
    public enum EstadoGesto { Ocioso, Mirando, Cancelando }

    /// <summary>
    /// A maquina PURA do gesto unico (GDD §19.3): pressionar -> arrastar mira -> soltar dispara;
    /// toque curto (&lt;= TapMaxMs) dispara para onde a camera olha; voltar o dedo ao centro antes de soltar CANCELA.
    /// Cancelar e' estado de 1a classe: Estado == Cancelando e' o que o botao MOSTRA (X) antes do dedo soltar.
    /// Sem cena, sem Input: a casca converte dp->px e alimenta Pressionar/Arrastar/Soltar.
    /// NOTA: o Godot revisou para "clicar dispara + segurar auto-fogo" na R17; aqui vale o GDD §19.3 (ordem do coordenador).
    /// </summary>
    public sealed class GestoLogica
    {
        /// <summary>dir normalizada (mira) — Vector2.zero = disparo curto para onde a camera olha.</summary>
        public event Action<Vector2> Disparou;
        public event Action Cancelou;
        /// <summary>Mira em andamento (dir normalizada). Serve para a camera/reticulo acompanharem.</summary>
        public event Action<Vector2> MiraMudou;

        public EstadoGesto Estado { get; private set; } = EstadoGesto.Ocioso;
        public float DeadzonePx;
        public int TapMaxMs;

        bool _pressionado;
        bool _mirou;          // ja' passou da deadzone alguma vez neste toque
        Vector2 _origem;
        Vector2 _offset;
        long _inicioMs;

        public GestoLogica(float deadzonePx, int tapMaxMs) { DeadzonePx = deadzonePx; TapMaxMs = tapMaxMs; }

        public Vector2 Offset => _offset;
        public bool Pressionado => _pressionado;

        public void Pressionar(Vector2 pos, long nowMs)
        {
            _pressionado = true;
            _mirou = false;
            _origem = pos;
            _offset = Vector2.zero;
            _inicioMs = nowMs;
            Estado = EstadoGesto.Mirando;
        }

        public void Arrastar(Vector2 pos)
        {
            if (!_pressionado) return;
            _offset = pos - _origem;
            bool fora = _offset.magnitude > DeadzonePx;
            if (fora)
            {
                _mirou = true;
                Estado = EstadoGesto.Mirando;
                MiraMudou?.Invoke(_offset.normalized);
            }
            else if (_mirou)
            {
                Estado = EstadoGesto.Cancelando;   // voltou ao centro: soltar agora NAO dispara
            }
        }

        public void Soltar(long nowMs)
        {
            if (!_pressionado) return;
            _pressionado = false;
            EstadoGesto antes = Estado;
            Estado = EstadoGesto.Ocioso;
            if (antes == EstadoGesto.Cancelando) { Cancelou?.Invoke(); return; }
            if (_mirou) { Disparou?.Invoke(_offset.normalized); return; }
            if (nowMs - _inicioMs <= TapMaxMs) { Disparou?.Invoke(Vector2.zero); return; }
            Cancelou?.Invoke();   // segurou parado alem do tap: nao e' tiro, e' desistencia
        }

        /// <summary>Anel aceso = mira valida; X = cancelando. O visual diz se soltar dispara.</summary>
        public bool MostraAnel => Estado == EstadoGesto.Mirando && _mirou;
        public bool MostraXis => Estado == EstadoGesto.Cancelando;
    }

    /// <summary>
    /// Casca do botao de disparo (canto inferior direito). Cinza e sem rotulo de maos nuas (Lei das Luvas);
    /// acende na cor + rotulo do elemento da luva. Eventos repassam a intencao — quem decide (mana, cadencia) e' o Player.
    /// </summary>
    public sealed class BotaoDisparo : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public GestoLogica Gesto { get; private set; }
        public event Action<Vector2> Disparou;
        public event Action Cancelou;
        public event Action<Vector2> MiraMudou;

        public bool Desarmado { get; private set; } = true;

        Image _disco;
        Image _anel;
        Image _xis;
        Text _rotulo;
        Color _cor = Color.gray;
        string _texto = "";
        int _ponteiro = int.MinValue;

        public static BotaoDisparo Criar(Transform pai, float ladoPx)
        {
            var go = new GameObject("BotaoDisparo", typeof(RectTransform), typeof(Image), typeof(BotaoDisparo));
            go.transform.SetParent(pai, false);
            var b = go.GetComponent<BotaoDisparo>();
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(ladoPx, ladoPx);
            b._disco = go.GetComponent<Image>();
            b._disco.sprite = Formas.Disco();
            b._disco.raycastTarget = true;
            var borda = Formas.Imagem(go.transform, "Borda", Formas.Anel(), new Color(1, 1, 1, 0.3f));
            AreaSegura.Esticar(borda.rectTransform);
            b._anel = Formas.Imagem(go.transform, "AnelMira", Formas.Anel(), new Color(0.2f, 1f, 0.4f, 1f));
            b._anel.rectTransform.sizeDelta = new Vector2(ladoPx * 1.16f, ladoPx * 1.16f);
            b._xis = Formas.Imagem(go.transform, "Xis", Formas.Xis(), new Color(1f, 0.36f, 0.30f, 1f));
            b._xis.rectTransform.sizeDelta = new Vector2(ladoPx * 0.7f, ladoPx * 0.7f);
            b._rotulo = Formas.Texto(go.transform, "Rotulo", "", 13f, new Color(1, 1, 0.92f, 0.92f));
            AreaSegura.Esticar(b._rotulo.rectTransform);
            b.Gesto = new GestoLogica(Dp.Deadzone, (int)Balance.Touch.TapMaxMs);
            b.Gesto.Disparou += d => b.Disparou?.Invoke(d);
            b.Gesto.Cancelou += () => b.Cancelou?.Invoke();
            b.Gesto.MiraMudou += d => b.MiraMudou?.Invoke(d);
            b.Pintar();
            return b;
        }

        /// <summary>Maos nuas: cinza, sem rotulo, e o gesto NAO e' repassado.</summary>
        public void Desarmar()
        {
            Desarmado = true;
            Pintar();
        }

        public void Armar(Color cor, string rotulo)
        {
            Desarmado = false;
            _cor = cor;
            _texto = rotulo;
            Pintar();
        }

        void Pintar()
        {
            Color c = Desarmado ? new Color(0.42f, 0.44f, 0.5f) : _cor;
            _disco.color = Formas.ComAlfa(Formas.Escurecer(c, 0.35f), 0.38f);
            _rotulo.text = Desarmado ? "" : _texto;
            _anel.enabled = Gesto != null && Gesto.MostraAnel && !Desarmado;
            _xis.enabled = Gesto != null && Gesto.MostraXis;
        }

        static long AgoraMs => (long)(Time.realtimeSinceStartup * 1000f);

        public void OnPointerDown(PointerEventData e)
        {
            if (_ponteiro != int.MinValue || Desarmado) return;
            _ponteiro = e.pointerId;
            Gesto.Pressionar(e.position, AgoraMs);
            Pintar();
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != _ponteiro) return;
            Gesto.Arrastar(e.position);
            Pintar();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _ponteiro) return;
            _ponteiro = int.MinValue;
            Gesto.Soltar(AgoraMs);
            Pintar();
        }
    }
}
