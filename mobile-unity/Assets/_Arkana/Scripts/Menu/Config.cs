using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Arkana.Core;
using Arkana.UI;

namespace Arkana.Menu
{
    /// <summary>Onde a config persiste. PlayerPrefs em jogo; memoria no teste. Guarda o TIPO (int/float), nunca texto duplicado.</summary>
    public interface IPrefs
    {
        bool Tem(string chave);
        int LerInt(string chave, int padrao);
        float LerFloat(string chave, float padrao);
        void GravarInt(string chave, int v);
        void GravarFloat(string chave, float v);
        void Salvar();
    }

    public sealed class PrefsPlayer : IPrefs
    {
        public bool Tem(string c) => PlayerPrefs.HasKey(c);
        public int LerInt(string c, int d) => PlayerPrefs.GetInt(c, d);
        public float LerFloat(string c, float d) => PlayerPrefs.GetFloat(c, d);
        public void GravarInt(string c, int v) => PlayerPrefs.SetInt(c, v);
        public void GravarFloat(string c, float v) => PlayerPrefs.SetFloat(c, v);
        public void Salvar() => PlayerPrefs.Save();
    }

    /// <summary>Memoria (testes): expoe em que TIPO cada chave foi gravada.</summary>
    public sealed class PrefsMemoria : IPrefs
    {
        public readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
        public readonly Dictionary<string, float> Floats = new Dictionary<string, float>();
        public int Salvos;
        public bool Tem(string c) => Ints.ContainsKey(c) || Floats.ContainsKey(c);
        public int LerInt(string c, int d) { int v; return Ints.TryGetValue(c, out v) ? v : d; }
        public float LerFloat(string c, float d) { float v; return Floats.TryGetValue(c, out v) ? v : d; }
        public void GravarInt(string c, int v) { Ints[c] = v; Floats.Remove(c); }
        public void GravarFloat(string c, float v) { Floats[c] = v; Ints.Remove(c); }
        public void Salvar() { Salvos++; }
    }

    /// <summary>
    /// Logica PURA das configuracoes (GDD §12): dicionario chave -> valor com PADRAO tipado. Set() so' aceita o tipo do
    /// padrao (arquivo adulterado cai no padrao, nunca derruba o boot). Persistencia por tipo: bool/int -> int, float -> float.
    /// R21: so' entra opcao que muda o jogo — o que nao tem consumidor fica fora.
    /// </summary>
    public sealed class ConfigLogica
    {
        public const string K_TELA_CHEIA = "video.tela_cheia";
        public const string K_QUALIDADE = "video.qualidade";        // 0 Baixa 1 Media 2 Alta
        public const string K_FPS_LIMITE = "video.fps_limite";      // 0:30 1:60 2:120 3:Ilimitado
        public const string K_VSYNC = "video.vsync";
        public const string K_CONTADOR_FPS = "video.contador_fps";
        public const string K_VOL_GERAL = "audio.geral";
        public const string K_VOL_MUSICA = "audio.musica";
        public const string K_VOL_EFEITOS = "audio.efeitos";
        public const string K_VOL_INTERFACE = "audio.interface";
        public const string K_SENSIBILIDADE = "controles.sensibilidade";
        public const string K_SENS_MIRA = "controles.sens_mira";
        public const string K_INVERTER_Y = "controles.inverter_y";
        public const string K_LAYOUT = "controles.layout";          // 0 padrao (Fogo a direita) 1 espelhado
        public const string K_ESCALA_BOTOES = "controles.escala_botoes";
        public const string K_DALTONISMO = "jogo.daltonismo";       // 0 Nenhum 1 Prot 2 Deut 3 Trit
        public const string K_NUMEROS_DANO = "jogo.numeros_dano";

        public static readonly string[] Secoes = { "video", "audio", "controles", "jogo" };
        public static readonly int[] FpsOpcoes = { 30, 60, 120, -1 };
        public static readonly string[] FpsRotulos = { "30", "60", "120", "Ilimitado" };
        public static readonly string[] Qualidades = { "Baixa", "Média", "Alta" };
        public static readonly string[] Layouts = { "Padrão", "Espelhado" };

        /// <summary>Padrao de fabrica — o TIPO de cada valor e' o contrato da chave.</summary>
        public static readonly Dictionary<string, object> Padrao = new Dictionary<string, object>
        {
            { K_TELA_CHEIA, true }, { K_QUALIDADE, 1 }, { K_FPS_LIMITE, 1 }, { K_VSYNC, true }, { K_CONTADOR_FPS, false },
            { K_VOL_GERAL, 80f }, { K_VOL_MUSICA, 70f }, { K_VOL_EFEITOS, 85f }, { K_VOL_INTERFACE, 70f },
            { K_SENSIBILIDADE, 3f }, { K_SENS_MIRA, 1f }, { K_INVERTER_Y, false }, { K_LAYOUT, 0 }, { K_ESCALA_BOTOES, 1f },
            { K_DALTONISMO, 0 }, { K_NUMEROS_DANO, true },
        };

        readonly Dictionary<string, object> _v = new Dictionary<string, object>();

        public ConfigLogica() { foreach (var kv in Padrao) _v[kv.Key] = kv.Value; }

        public IEnumerable<string> Chaves => Padrao.Keys;
        public static string SecaoDe(string chave) { int i = chave.IndexOf('.'); return i < 0 ? chave : chave.Substring(0, i); }

        public bool Bool(string k) { object o; return _v.TryGetValue(k, out o) && o is bool && (bool)o; }
        public int Int(string k) { object o; return _v.TryGetValue(k, out o) && o is int ? (int)o : 0; }
        public float Float(string k)
        {
            object o;
            if (!_v.TryGetValue(k, out o)) return 0f;
            if (o is float) return (float)o;
            if (o is int) return (int)o;
            return 0f;
        }

        /// <summary>Tipo errado ou chave desconhecida = IGNORADO (entrada nao confiavel). Devolve se aceitou.</summary>
        public bool Set(string k, object valor)
        {
            object padrao;
            if (!Padrao.TryGetValue(k, out padrao) || valor == null) return false;
            if (padrao is bool) { if (valor is bool) { _v[k] = valor; return true; } return false; }
            if (padrao is int) { if (valor is int) { _v[k] = valor; return true; } return false; }
            if (padrao is float)
            {
                if (valor is float) { _v[k] = valor; return true; }
                if (valor is int) { _v[k] = (float)(int)valor; return true; }
                if (valor is double) { _v[k] = (float)(double)valor; return true; }
            }
            return false;
        }

        public void Restaurar(string secao)
        {
            foreach (var kv in Padrao) if (SecaoDe(kv.Key) == secao) _v[kv.Key] = kv.Value;
        }

        public void RestaurarTudo() { foreach (var kv in Padrao) _v[kv.Key] = kv.Value; }

        /// <summary>Le do store pelo TIPO do padrao; o que nao esta' la' fica no padrao.</summary>
        public void Carregar(IPrefs p)
        {
            foreach (var kv in Padrao)
            {
                if (!p.Tem(kv.Key)) continue;
                if (kv.Value is bool) _v[kv.Key] = p.LerInt(kv.Key, (bool)kv.Value ? 1 : 0) != 0;
                else if (kv.Value is int) _v[kv.Key] = p.LerInt(kv.Key, (int)kv.Value);
                else if (kv.Value is float) _v[kv.Key] = p.LerFloat(kv.Key, (float)kv.Value);
            }
        }

        public void Salvar(IPrefs p)
        {
            foreach (var kv in _v)
            {
                if (kv.Value is bool) p.GravarInt(kv.Key, (bool)kv.Value ? 1 : 0);
                else if (kv.Value is int) p.GravarInt(kv.Key, (int)kv.Value);
                else if (kv.Value is float) p.GravarFloat(kv.Key, (float)kv.Value);
            }
            p.Salvar();
        }

        // ---------- a config EM VIGOR (sobrevive a troca de cena) ----------
        static ConfigLogica _atual;
        public static IPrefs Store = new PrefsPlayer();
        /// <summary>Chega no boot e a CADA mexida no menu; HUD, Sfx e Player leem daqui.</summary>
        public static event Action<ConfigLogica> Mudou;

        public static ConfigLogica Atual
        {
            get
            {
                if (_atual == null) { _atual = new ConfigLogica(); try { _atual.Carregar(Store); } catch (Exception) { } }
                return _atual;
            }
        }

        /// <summary>Aplica o que nao tem dono (video, volume geral, daltonismo) e AVISA quem tem (Mudou).</summary>
        public static void Aplicar(ConfigLogica c)
        {
            _atual = c;
            int fps = c.Int(K_FPS_LIMITE);
            Application.targetFrameRate = FpsOpcoes[Mathf.Clamp(fps, 0, FpsOpcoes.Length - 1)];
            QualitySettings.vSyncCount = c.Bool(K_VSYNC) ? 1 : 0;
            if (Application.isMobilePlatform || Application.isEditor == false) Screen.fullScreen = c.Bool(K_TELA_CHEIA);
            int q = Mathf.Clamp(c.Int(K_QUALIDADE), 0, 2);
            if (QualitySettings.names.Length > 0) QualitySettings.SetQualityLevel(Mathf.Clamp(q * (QualitySettings.names.Length - 1) / 2, 0, QualitySettings.names.Length - 1), false);
            AudioListener.volume = Mathf.Clamp01(c.Float(K_VOL_GERAL) / 100f);
            FiltroDaltonismo.Modo = Mathf.Clamp(c.Int(K_DALTONISMO), 0, 3);
            Mudou?.Invoke(c);
        }
    }

    /// <summary>
    /// A TELA de configuracoes: 4 secoes numa lista rolavel, RESTAURAR PADRAO por secao, VOLTAR salva. Cada controle
    /// aplica na hora (Aplicar) — opcao que grava e nao muda nada e' pior que opcao nenhuma. Usada no menu E na pausa.
    /// </summary>
    public sealed class Config : MonoBehaviour
    {
        public const string T_TITULO = Textos.CfgTitulo, T_VOLTAR = Textos.Voltar, T_RESTAURAR = Textos.CfgRestaurar;
        /// <summary>Layout/escala dos botoes (GDD §19.3) ainda nao tem texto no Core; ponytail: mover para Textos.</summary>
        public const string T_LAYOUT = "Layout dos botões", T_ESCALA = "Tamanho dos botões";
        public event Action VoltarPedido;

        ConfigLogica _cfg;
        RectTransform _lista;
        RectTransform _raiz;

        public static Config Criar(Transform pai)
        {
            var go = new GameObject("Config", typeof(RectTransform), typeof(Config));
            go.transform.SetParent(pai, false);
            var c = go.GetComponent<Config>();
            c._raiz = (RectTransform)go.transform;
            AreaSegura.Esticar(c._raiz);
            c._cfg = ConfigLogica.Atual;
            c.Montar();
            return c;
        }

        void Montar()
        {
            foreach (Transform t in _raiz) Destroy(t.gameObject);
            Estilo.Fundo(_raiz);
            Margens m = AreaSegura.Atual();
            var conteudo = Formas.No(_raiz, "Conteudo");
            AreaSegura.EsticarDentro(conteudo, m, Dp.Px(Estilo.RespiroDp));
            float alt = Estilo.AlturaAlvo() + 8f;
            var titulo = Formas.Texto(conteudo, "Titulo", T_TITULO, 26f, Estilo.Ouro);
            titulo.rectTransform.anchorMin = new Vector2(0, 1); titulo.rectTransform.anchorMax = new Vector2(1, 1); titulo.rectTransform.pivot = new Vector2(0.5f, 1);
            titulo.rectTransform.anchoredPosition = Vector2.zero; titulo.rectTransform.sizeDelta = new Vector2(0, alt);
            var voltar = Estilo.Botao(conteudo, "BtnVoltarConfig", T_VOLTAR, 150f, Estilo.AlvoDp, 16f);
            var vrt = (RectTransform)voltar.transform;
            vrt.anchorMin = new Vector2(0, 1); vrt.anchorMax = new Vector2(0, 1); vrt.pivot = new Vector2(0, 1); vrt.anchoredPosition = Vector2.zero;
            voltar.onClick.AddListener(() => { _cfg.Salvar(ConfigLogica.Store); VoltarPedido?.Invoke(); });

            // lista rolavel
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(Mask));
            scrollGo.transform.SetParent(conteudo, false);
            var srt = (RectTransform)scrollGo.transform;
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one; srt.offsetMin = Vector2.zero; srt.offsetMax = new Vector2(0, -alt - 4f);
            scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.2f);
            scrollGo.GetComponent<Mask>().showMaskGraphic = true;
            var scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.horizontal = false;
            _lista = Formas.No(srt, "Lista");
            _lista.anchorMin = new Vector2(0, 1); _lista.anchorMax = new Vector2(1, 1); _lista.pivot = new Vector2(0.5f, 1);
            // RectTransform novo nasce com sizeDelta 100x100: esticado, a lista ficava 100 px MAIS LARGA que a mascara e cortava
            // o rotulo a esquerda e o botao a direita (foto 50-config). A altura vem do ContentSizeFitter.
            _lista.sizeDelta = Vector2.zero;
            var v = _lista.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = Dp.Px(6f); v.padding = new RectOffset(8, 8, 8, 8);
            v.childControlWidth = true; v.childControlHeight = false; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            _lista.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _lista;

            Secao(Textos.CfgAbaVideo);
            Opcao(ConfigLogica.K_TELA_CHEIA, Textos.CfgTelaCheia, null);
            Opcao(ConfigLogica.K_QUALIDADE, Textos.CfgQualidade, Textos.CfgQualidades);
            Opcao(ConfigLogica.K_FPS_LIMITE, Textos.CfgFpsLimite, Textos.CfgFpsOpcoes);
            Opcao(ConfigLogica.K_VSYNC, Textos.CfgVsync, null);
            Opcao(ConfigLogica.K_CONTADOR_FPS, Textos.CfgContadorFps, null);
            Restaurar("video");
            Secao(Textos.CfgAbaAudio);
            Deslizante(ConfigLogica.K_VOL_GERAL, Textos.CfgVolGeral, 0f, 100f, 0);
            Deslizante(ConfigLogica.K_VOL_MUSICA, Textos.CfgVolMusica, 0f, 100f, 0);
            Deslizante(ConfigLogica.K_VOL_EFEITOS, Textos.CfgVolEfeitos, 0f, 100f, 0);
            Deslizante(ConfigLogica.K_VOL_INTERFACE, Textos.CfgVolInterface, 0f, 100f, 0);
            Restaurar("audio");
            Secao(Textos.CfgAbaControles);
            Deslizante(ConfigLogica.K_SENSIBILIDADE, Textos.CfgSensibilidade, 0.1f, 10f, 1);
            Deslizante(ConfigLogica.K_SENS_MIRA, Textos.CfgSensMira, 0.1f, 3f, 2);
            Opcao(ConfigLogica.K_INVERTER_Y, Textos.CfgInverterY, null);
            Opcao(ConfigLogica.K_LAYOUT, T_LAYOUT, ConfigLogica.Layouts);
            Deslizante(ConfigLogica.K_ESCALA_BOTOES, T_ESCALA, 0.8f, 1.4f, 2);
            Nota(Textos.CfgEsquemaNota);
            Nota(Textos.CfgTeclasNota);
            Restaurar("controles");
            Secao(Textos.CfgAbaJogo);
            Opcao(ConfigLogica.K_DALTONISMO, Textos.CfgDaltonismo, Textos.CfgDaltonismos);
            Opcao(ConfigLogica.K_NUMEROS_DANO, Textos.CfgNumerosDano, null);
            Nota(Textos.CfgIdiomaNota);
            Nota(Textos.CfgComboNota);
            Restaurar("jogo");
        }

        void Gravar() { ConfigLogica.Aplicar(_cfg); _cfg.Salvar(ConfigLogica.Store); }

        RectTransform Linha(string rotulo)
        {
            var h = Formas.No(_lista, "Linha");
            h.sizeDelta = new Vector2(0, Estilo.AlturaAlvo());
            var le = h.gameObject.AddComponent<LayoutElement>();
            le.minHeight = Estilo.AlturaAlvo(); le.preferredHeight = Estilo.AlturaAlvo();
            var l = Formas.Texto(h, "Rotulo", rotulo, 16f, Estilo.Texto, TextAnchor.MiddleLeft);
            l.rectTransform.anchorMin = new Vector2(0, 0); l.rectTransform.anchorMax = new Vector2(0.5f, 1); l.rectTransform.offsetMin = new Vector2(Dp.Px(8f), 0); l.rectTransform.offsetMax = Vector2.zero;
            return h;
        }

        void Secao(string nome)
        {
            var h = Formas.No(_lista, "Secao");
            h.gameObject.AddComponent<LayoutElement>().preferredHeight = Dp.Px(28f);
            var t = Formas.Texto(h, "Titulo", nome, 14f, Estilo.Ouro, TextAnchor.MiddleLeft);
            AreaSegura.Esticar(t.rectTransform);
        }

        void Nota(string texto)
        {
            var h = Formas.No(_lista, "Nota");
            h.gameObject.AddComponent<LayoutElement>().preferredHeight = Dp.Px(36f);
            var t = Estilo.Paragrafo(h, texto, 12f, Estilo.TextoFosco);
            AreaSegura.Esticar(t.rectTransform);
        }

        /// <summary>Sem lista = liga/desliga; com lista = toca para ciclar. Um botao so', alvo 48dp.</summary>
        void Opcao(string chave, string rotulo, string[] opcoes)
        {
            var h = Linha(rotulo);
            var b = Estilo.Botao(h, "Opt_" + chave, "", 200f, Estilo.AlvoDp, 15f);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f); rt.pivot = new Vector2(1, 0.5f); rt.anchoredPosition = new Vector2(-Dp.Px(8f), 0);
            var txt = b.GetComponentInChildren<Text>();
            Action pintar = () =>
            {
                if (opcoes == null) txt.text = _cfg.Bool(chave) ? "LIGADO" : "DESLIGADO";
                else txt.text = opcoes[Mathf.Clamp(_cfg.Int(chave), 0, opcoes.Length - 1)];
            };
            pintar();
            b.onClick.AddListener(() =>
            {
                if (opcoes == null) _cfg.Set(chave, !_cfg.Bool(chave));
                else _cfg.Set(chave, (_cfg.Int(chave) + 1) % opcoes.Length);
                pintar();
                Gravar();
            });
        }

        void Deslizante(string chave, string rotulo, float min, float max, int casas)
        {
            var h = Linha(rotulo);
            var go = new GameObject("Sld_" + chave, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(h, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f); rt.pivot = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-Dp.Px(8f), 0); rt.sizeDelta = new Vector2(Dp.Px(220f), Estilo.AlturaAlvo());
            var fundo = Formas.Imagem(rt, "Fundo", null, new Color(0, 0, 0, 0.45f));
            fundo.rectTransform.anchorMin = new Vector2(0, 0.4f); fundo.rectTransform.anchorMax = new Vector2(1, 0.6f); fundo.rectTransform.offsetMin = Vector2.zero; fundo.rectTransform.offsetMax = Vector2.zero;
            var fillArea = Formas.No(rt, "FillArea");
            fillArea.anchorMin = new Vector2(0, 0.4f); fillArea.anchorMax = new Vector2(1, 0.6f); fillArea.offsetMin = Vector2.zero; fillArea.offsetMax = Vector2.zero;
            var fill = Formas.Imagem(fillArea, "Fill", null, Estilo.OuroFosco);
            AreaSegura.Esticar(fill.rectTransform);
            var handleArea = Formas.No(rt, "HandleArea");
            AreaSegura.Esticar(handleArea);
            var handle = Formas.Imagem(handleArea, "Handle", Formas.Disco(), Estilo.Ouro);
            handle.rectTransform.sizeDelta = new Vector2(Dp.Px(28f), Dp.Px(28f));
            handle.raycastTarget = true;
            var s = go.GetComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.minValue = min; s.maxValue = max;
            s.value = Mathf.Clamp(_cfg.Float(chave), min, max);
            var num = Formas.Texto(h, "Num", "", 14f, Estilo.Ouro, TextAnchor.MiddleRight);
            num.rectTransform.anchorMin = new Vector2(1, 0.5f); num.rectTransform.anchorMax = new Vector2(1, 0.5f); num.rectTransform.pivot = new Vector2(1, 0.5f);
            num.rectTransform.anchoredPosition = new Vector2(-Dp.Px(236f), 0); num.rectTransform.sizeDelta = new Vector2(Dp.Px(60f), Estilo.AlturaAlvo());
            num.text = s.value.ToString("F" + casas);
            s.onValueChanged.AddListener(val =>
            {
                _cfg.Set(chave, val);
                num.text = val.ToString("F" + casas);
                ConfigLogica.Aplicar(_cfg);   // so' aplica; gravar a cada pixel do arrasto castiga a flash
            });
        }

        void Restaurar(string secao)
        {
            var h = Formas.No(_lista, "Rodape");
            h.gameObject.AddComponent<LayoutElement>().preferredHeight = Estilo.AlturaAlvo() + Dp.Px(8f);
            var b = Estilo.Botao(h, "BtnRestaurar_" + secao, T_RESTAURAR, 220f, Estilo.AlvoDp, 14f);
            var rt = (RectTransform)b.transform;
            rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f); rt.pivot = new Vector2(1, 0.5f); rt.anchoredPosition = new Vector2(-Dp.Px(8f), 0);
            b.onClick.AddListener(() => { _cfg.Restaurar(secao); Gravar(); Montar(); });   // remontar e' mais barato (e menos bug) que sincronizar 5 widgets
        }

        void OnDestroy() { if (_cfg != null) _cfg.Salvar(ConfigLogica.Store); }
    }
}
