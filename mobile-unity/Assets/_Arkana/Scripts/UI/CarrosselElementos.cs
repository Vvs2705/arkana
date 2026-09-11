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
    /// </summary>
    public sealed class CarrosselElementos : MonoBehaviour, IPointerDownHandler
    {
        public event Action<Elemento> Escolheu;
        public Elemento Selecionado { get; private set; } = Elemento.Fogo;

        RectTransform _rt;
        Image[] _fundos;
        Image[] _aneis;

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
            c._fundos = new Image[n];
            c._aneis = new Image[n];
            for (int i = 0; i < n; i++)
            {
                Elemento e = Elementos.Todos[i];
                var slot = Formas.No(go.transform, "Slot" + Elementos.Id(e));
                slot.anchorMin = new Vector2(0, 0.5f); slot.anchorMax = new Vector2(0, 0.5f); slot.pivot = new Vector2(0.5f, 0.5f);
                slot.sizeDelta = new Vector2(slotPx, slotPx);
                slot.anchoredPosition = new Vector2(slotPx * (i + 0.5f), 0);
                float r = slotPx - Dp.Px(8f);
                Color col = Estilo.CorElemento(e);
                c._fundos[i] = Formas.Imagem(slot, "Fundo", Formas.Disco(), Formas.ComAlfa(Formas.Escurecer(col, 0.12f), 0.18f));
                c._fundos[i].rectTransform.sizeDelta = new Vector2(r, r);
                c._aneis[i] = Formas.Imagem(slot, "Anel", Formas.Anel(), Color.white);
                c._aneis[i].rectTransform.sizeDelta = new Vector2(r + Dp.Px(4f), r + Dp.Px(4f));
                var icone = Formas.Imagem(slot, "Icone", Formas.DoElemento(e), col);
                icone.rectTransform.sizeDelta = new Vector2(r * 0.6f, r * 0.6f);
            }
            c.Pintar();
            return c;
        }

        public void Visivel(bool on) { gameObject.SetActive(on); }

        public void Selecionar(Elemento e) { Selecionado = e; Pintar(); }

        void Pintar()
        {
            for (int i = 0; i < _fundos.Length; i++)
            {
                bool sel = Elementos.Todos[i] == Selecionado;
                Color col = Estilo.CorElemento(Elementos.Todos[i]);
                _fundos[i].color = Formas.ComAlfa(Formas.Escurecer(col, 0.12f), sel ? 0.38f : 0.18f);
                _aneis[i].enabled = sel;
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
