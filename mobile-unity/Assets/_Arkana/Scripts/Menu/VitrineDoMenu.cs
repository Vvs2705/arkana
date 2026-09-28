using UnityEngine;
using UnityEngine.Rendering;
using Arkana.Characters;
using Arkana.Gameplay;
using Arkana.World;

namespace Arkana.Menu
{
    /// <summary>
    /// O FUNDO VIVO do menu: o mago escolhido em guarda no topo do PICO (neve, torres escuras, o por do sol atras) e a
    /// camera do menu orbitando devagar em volta dele. Mora na camera do menu; o Main liga/desliga junto com ela.
    /// O mago e' so' o Visual (Mago, nunca Pawn) e SOME no OnDisable: o treino nasce justamente no pico.
    /// Trocou o mago no Elenco = mago novo no pico NO MESMO QUADRO (SelecaoPersonagem.MagoTrocou). Sem ilha, nao faz nada
    /// (fiacao defensiva, sem log). O escolhido fica em DESTAQUE: disco de neon no chao na cor do elemento e motas de luz
    /// subindo do aro; com o Elenco aberto a camera desliza e poe o mago no vao entre o painel e o cartao.
    /// </summary>
    public sealed class VitrineDoMenu : MonoBehaviour
    {
        // KNOB: o enquadramento inteiro, por foto (a referencia e' a 03-mago-de-perto: camera a ~4-5 m, um pouco acima)
        public const float Raio = 4.8f, Altura = 1.7f, Velocidade = 0.06f;   // m do mago, m acima do pe', rad/s (~105 s por volta)
        /// <summary>O olhar mira ~1 m acima do pe' e 1,3 m a ESQUERDA da tela: o mago cai no terco direito (a UI e' centrada;
        /// no Elenco o painel dos retratos e' a esquerda e o cartao, a direita dele).</summary>
        public const float OlharY = 1f, OlharLado = 1.3f;
        /// <summary>KNOB: com o Elenco aberto o olhar vai a 0,5 m (o mago em ~0,57 da tela, no meio do vao entre o painel
        /// 0-0,42 e o cartao 0,72-1; conta a ~306 px/m na foto de 2400x1080) em ~0,5 s (Deslize = taxa exponencial por s).</summary>
        public const float OlharLadoElenco = 0.5f, Deslize = 6f;
        /// <summary>KNOB: raio do disco = Base + PorM x altura da ficha (Pip 0,46 m, Pyra 0,67 m, Basalto 0,76 m); brilho HDR
        /// passa do limiar do bloom e pulsa entre 80% e 100% no periodo PulsoS.</summary>
        public const float DiscoBase = 0.35f, DiscoPorM = 0.18f, DiscoBrilho = 1.5f, PulsoS = 2.2f;

        Mago _mago;
        string _slug;
        float _ang = 0.6f;   // comeca no angulo da foto 03 (camera a +x,+z do mago)
        float _lado = OlharLado;
        /// <summary>A escolha mora no PlayerPrefs (no Android, JNI): relida so' quando o setter avisa, nunca por quadro.</summary>
        bool _reler;
        GameObject _destaque;
        Transform _disco;
        Mesh _malhaDisco;
        Material _matDisco;
        ParticleSystem _motas;
        Color _corDisco = Color.white;

        void OnEnable() { SelecaoPersonagem.MagoTrocou += Reler; }

        void Reler() { _reler = true; }

        void LateUpdate()
        {
            Ilha ilha = Ilha.Atual;
            if (ilha == null || (ilha.Relevo == null && ilha.Mestre == null)) return;
            // na IlhaMestre o pico da ilha antiga cai DENTRO do lago (o titulo mostrava a ilha de baixo d'agua, 27/09)
            Vector2 pk = ilha.Mestre != null ? RelevoMestre.Mirante : ilha.Relevo.Pico;
            var pe = new Vector3(pk.x, Ilha.AlturaDoChao(pk.x, pk.y), pk.y);
            if (_mago == null || _reler)
            {
                _reler = false;
                string slug = SelecaoPersonagem.MagoEscolhido;
                if (_mago == null || slug != _slug)
                {
                    Soltar();
                    _slug = slug;
                    _mago = Mago.Criar(null, slug);   // ja' nasce no idle de combate
                    Destacar(slug, pe);
                }
            }
            float dt = Time.unscaledDeltaTime;   // sem escala: a pausa da HUD nao congela o menu
            _ang += Velocidade * dt;
            _lado = Mathf.Lerp(_lado, SelecaoPersonagem.Aberta ? OlharLadoElenco : OlharLado, 1f - Mathf.Exp(-Deslize * dt));
            var volta = new Vector3(Mathf.Sin(_ang), 0f, Mathf.Cos(_ang));   // mago -> camera, no plano
            Vector3 cam = pe + volta * Raio;
            cam.y = Mathf.Max(pe.y + Altura, Ilha.AlturaDoChao(cam.x, cam.z) + 1f);   // a crista do pico nao engole a camera
            Vector3 esquerda = Vector3.Cross(-volta, Vector3.up);   // a esquerda de quem olha o mago
            transform.position = cam;
            transform.LookAt(pe + Vector3.up * OlharY + esquerda * _lado);
            _mago.transform.SetPositionAndRotation(pe, Quaternion.LookRotation(volta));   // de frente para a camera (+Z do modelo)
            if (_matDisco != null)
            {
                float k = DiscoBrilho * (0.9f + 0.1f * Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / PulsoS)));
                _matDisco.SetColor("_BaseColor", new Color(_corDisco.r * k, _corDisco.g * k, _corDisco.b * k, 1f));   // alfa 1: no aditivo o brilho sairia k^2
            }
        }

        /// <summary>
        /// O disco e as motas no pe' do escolhido, na cor do elemento dele. O disco e' a MARCA do loot (MalhaDaMarca.Neon:
        /// tubo de luz, brilho que esvaece, miolo fraco) na faixa macia aditiva; as motas sobem do aro (ParticulaVfx, o
        /// aditivo HDR dos kits). Montado 1x, recolorido a cada troca; o disco assenta no ponto MAIS ALTO do chao sob o aro.
        /// </summary>
        void Destacar(string slug, Vector3 pe)
        {
            IdentidadeMago id = IdentidadeMago.De(slug);
            float r = DiscoBase + DiscoPorM * id.AlturaM;
            if (_destaque == null)
            {
                _destaque = new GameObject("DiscoDoEscolhido");
                var go = new GameObject("Disco");
                _disco = go.transform;
                _disco.SetParent(_destaque.transform, false);
                _malhaDisco = MalhaDaMarca.Neon(40, 0.8f).ParaMesh("DiscoDoEscolhido");
                go.AddComponent<MeshFilter>().sharedMesh = _malhaDisco;
                var mr = go.AddComponent<MeshRenderer>();
                _matDisco = MaterialVfx.Novo(MaterialVfx.Unlit, Color.white, MaterialVfx.Mistura.Aditivo, false, MaterialVfx.FaixaSuave());
                mr.sharedMaterial = _matDisco;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                _motas = ParticulaVfx.Novo(_destaque.transform, "Motas", Color.white, Color.white, 14f,
                    new Vector2(1.4f, 2.4f), Vector2.zero, new Vector2(0.035f, 0.09f), false, 0f, 48);
                ParticleSystem.MainModule m = _motas.main;
                m.prewarm = true;   // a foto (e o dedo) chega com a coluna ja' cheia, nao com as primeiras motas
                ParticleSystem.ShapeModule sh = _motas.shape;
                sh.shapeType = ParticleSystemShapeType.Circle;   // plano XY do emissor = o chao (o Novo gira -90 em X)
                sh.radiusThickness = 0.3f;                       // sobem da faixa de fora, em volta do corpo
                // curvas de velocidade TODAS constantes, uma a uma: modos misturados logam erro (e o PlayMode reprova log)
                ParticleSystem.VelocityOverLifetimeModule v = _motas.velocityOverLifetime;
                v.enabled = true;
                v.space = ParticleSystemSimulationSpace.Local;
                v.x = 0f; v.y = 0f; v.z = 0.55f;   // z local = cima
                v.orbitalX = 0f; v.orbitalY = 0f; v.orbitalZ = 0.5f;
                v.radial = 0f;
            }
            _destaque.SetActive(true);
            float y = pe.y;
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                y = Mathf.Max(y, Ilha.AlturaDoChao(pe.x + Mathf.Cos(a) * r, pe.z + Mathf.Sin(a) * r));
            }
            _destaque.transform.position = new Vector3(pe.x, y + 0.04f, pe.z);   // 4 cm: sem briga de profundidade com o chao
            _disco.localScale = new Vector3(r, 1f, r);
            _corDisco = Estilo.CorElemento(id.Elemento);
            ParticleSystem.MainModule mm = _motas.main;
            mm.startColor = new ParticleSystem.MinMaxGradient(_corDisco, Color.Lerp(_corDisco, Color.white, 0.6f));
            ParticleSystem.ShapeModule s = _motas.shape;
            s.radius = r * 0.85f;
            _motas.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);   // as motas da cor antiga saem juntas
            _motas.Play();
        }

        void OnDisable()
        {
            SelecaoPersonagem.MagoTrocou -= Reler;
            Soltar();
            if (_destaque != null) _destaque.SetActive(false);   // na partida o pico e' do jogador
        }

        void OnDestroy()
        {
            Soltar();
            if (_destaque != null) Destroy(_destaque);
            if (_malhaDisco != null) Destroy(_malhaDisco);
            if (_matDisco != null) Destroy(_matDisco);
        }

        /// <summary>SetActive(false) ANTES do Destroy: o Destroy so' vale no fim do quadro, e a partida monta o jogador no
        /// pico NESTE quadro — o mago da vitrine nao pode aparecer (nem na primeira foto) da partida.</summary>
        void Soltar()
        {
            if (_mago != null) { _mago.gameObject.SetActive(false); Destroy(_mago.gameObject); }
            _mago = null;
            _slug = null;
        }
    }
}
