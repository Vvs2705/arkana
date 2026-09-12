using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.Menu;

namespace Arkana.UI
{
    /// <summary>
    /// Carrossel dos 5 elementos, cor + FORMA (GDD §10). Escondido de maos nuas e com luva de elemento travado
    /// (a luva impoe o elemento; mostrar VENTO e sair FOGO era mentira — video do Diretor 26/08).
    /// O toque so' PEDE (Escolheu); a selecao mostrada vem do Bus.ElementChanged.
    /// Visual no idioma da HUD: bandeja em capsula (a placa das barras) com 5 soquetes escuros; o escolhido acende —
    /// disco tingido, aro e brilho na cor do elemento, icone maior — e os outros ficam de icone esmaecido.
    /// </summary>
    public sealed class CarrosselElementos : MonoBehaviour, IPointerDownHandler
    {
        static readonly Color Fundo = new Color(0.03f, 0.04f, 0.08f);   // o disco escuro do BotaoAcao
        const float AnelInterno = 0.84f;

        public event Action<Elemento> Escolheu;
        public Elemento Selecionado { get; private set; } = Elemento.Fogo;

        RectTransform _rt;
        Image[] _brilhos, _fundos, _aneis, _icones;

        public static CarrosselElementos Criar(Transform pai, float slotPx)
        {
            int n = Elementos.Todos.Length;
            var go = new GameObject("Carrossel", typeof(RectTransform), typeof(Image), typeof(CarrosselElementos));
            go.transform.SetParent(pai, false);
            var c = go.GetComponent<CarrosselElementos>();
            c._rt = (RectTransform)go.transform;
            c._rt.sizeDelta = new Vector2(slotPx * n, slotPx);
            var fundo = go.GetComponent<Image>();
            fundo.color = new Color(0, 0, 0, 0.01f);   // invisivel, mas alvo de toque
            fundo.raycastTarget = true;
            // bandeja: capsula (raio = meia altura) — cada soquete e' concentrico com a ponta redonda
            AreaSegura.Esticar(Hud.Placa(go.transform, "Bandeja", slotPx * 0.5f).rectTransform);
            c._brilhos = new Image[n]; c._fundos = new Image[n]; c._aneis = new Image[n]; c._icones = new Image[n];
            float r = slotPx - Dp.Px(10f);
            for (int i = 0; i < n; i++)
            {
                Elemento e = Elementos.Todos[i];
                var slot = Formas.No(go.transform, "Slot" + Elementos.Id(e));
                slot.anchorMin = new Vector2(0, 0.5f); slot.anchorMax = new Vector2(0, 0.5f); slot.pivot = new Vector2(0.5f, 0.5f);
                slot.sizeDelta = new Vector2(slotPx, slotPx);
                slot.anchoredPosition = new Vector2(slotPx * (i + 0.5f), 0);
                // ordem = ordem de desenho: brilho por baixo do disco, icone por cima de tudo
                c._brilhos[i] = Formas.Imagem(slot, "Brilho", Formas.Halo(), Color.white);
                c._brilhos[i].rectTransform.sizeDelta = new Vector2(r, r) * Formas.HaloEscala;
                c._fundos[i] = Formas.Imagem(slot, "Fundo", Formas.Disco(), Color.white);
                c._fundos[i].rectTransform.sizeDelta = new Vector2(r, r);
                c._aneis[i] = Formas.Imagem(slot, "Anel", Formas.Anel(AnelInterno), Color.white);
                c._aneis[i].rectTransform.sizeDelta = new Vector2(r, r);
                c._icones[i] = Formas.Imagem(slot, "Icone", Formas.DoElemento(e), Color.white);
                c._icones[i].rectTransform.sizeDelta = new Vector2(r * 0.6f, r * 0.6f);
            }
            c.Pintar();
            return c;
        }

        public void Visivel(bool on) { gameObject.SetActive(on); }

        public void Selecionar(Elemento e) { Selecionado = e; Pintar(); }

        /// <summary>So' na troca (nunca por frame). Le' a cor de novo: o modo daltonismo vale a partir da proxima troca.</summary>
        void Pintar()
        {
            for (int i = 0; i < _fundos.Length; i++)
            {
                bool sel = Elementos.Todos[i] == Selecionado;
                Color col = Estilo.CorElemento(Elementos.Todos[i]);
                _brilhos[i].enabled = sel;
                _brilhos[i].color = Formas.ComAlfa(col, 0.6f);
                _fundos[i].color = Formas.ComAlfa(Color.Lerp(Fundo, col, sel ? 0.24f : 0.08f), sel ? 0.92f : 0.5f);
                _aneis[i].color = Formas.ComAlfa(col, sel ? 1f : 0.3f);
                _icones[i].color = Formas.ComAlfa(col, sel ? 1f : 0.6f);
                _icones[i].rectTransform.localScale = sel ? new Vector3(1.12f, 1.12f, 1f) : Vector3.one;
            }
        }

        /// <summary>Conta pura: x local (0..largura) -> indice do slot.</summary>
        public static int SlotDe(float xLocal, float largura, int n)
        {
            if (largura <= 0f || n <= 0) return 0;
            return Mathf.Clamp((int)(xLocal / (largura / n)), 0, n - 1);
        }

        public void OnPointerDown(PointerEventData e)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, e.position, e.pressEventCamera, out local);
            float x = local.x + _rt.rect.width * _rt.pivot.x;
            int i = SlotDe(x, _rt.rect.width, Elementos.Todos.Length);
            Escolheu?.Invoke(Elementos.Todos[i]);
        }
    }
}
