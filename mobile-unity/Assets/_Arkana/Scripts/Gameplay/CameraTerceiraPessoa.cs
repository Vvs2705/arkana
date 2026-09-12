using UnityEngine;
using Arkana.Menu;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// A CAMERA EM 3a PESSOA, pura: pivo no ombro, orbita por arrasto (sensibilidade da Config), tres modos —
    /// Normal (atras do mago), Castelo (o pivo vai ao coracao do castelo: a decisao de onde saltar se toma vendo o
    /// mapa) e Queda (inclina para o horizonte: se le' o chao chegando). A colisao e' da casca (SphereCast).
    /// </summary>
    public sealed class CameraLogica
    {
        public enum Modo { Normal, Castelo, Queda }

        /// <summary>rad por px com o slider no PADRAO (3.0). KNOB: mexer aqui move a faixa inteira do menu.</summary>
        public const float SENS_BASE = 0.008f / 3f;
        // CASTELO e QUEDA recalibrados pela FOTO de 11/09: com braco de 26 m a camera ficava colada na torre (o castelo
        // tem 35,8 m de envergadura) e a ilha — o que se precisa ver para escolher onde saltar — mal aparecia; na queda,
        // 7 graus olhava o horizonte em vez do chao que chega. KNOB: calibrar de novo no aparelho.
        public const float BRACO = 4.15f, BRACO_CASTELO = 70f, BRACO_QUEDA = 9f;
        public const float OMBRO_X = 0.78f, OMBRO_Y = 0.22f, ALTURA_PIVO = 1.85f;
        public const float FOV = 62f;
        public const float PITCH_MIN = -0.5f, PITCH_MAX = 1.1f;
        public const float PITCH_PADRAO = 0.30f, PITCH_CASTELO = 0.75f, PITCH_QUEDA = 0.55f;
        public const float RAIO_COLISAO = 0.35f;   // 0,25 deixava a parede a 0,3 m da lente (foto de 11/09)
        /// <summary>Braco livre abaixo disto (m) = camera em canto: vale tentar olhar de CIMA.</summary>
        public const float BRACO_CURTO = 1.2f;
        public const float RESPOSTA_MODO = 4f;

        /// <summary>Yaw em rad (0 = olhando para +Z). Pitch positivo = olhando para baixo.</summary>
        public float Yaw;
        public float Pitch = PITCH_PADRAO;
        public float Sens = SENS_BASE * 3f, SensMira = 1f, InverterY = 1f;
        public Modo ModoAtual = Modo.Normal;
        public float Braco { get; private set; } = BRACO;

        public void Config(float sensibilidade, float sensMira, bool inverterY)
        {
            Sens = SENS_BASE * Mathf.Clamp(sensibilidade, 0.1f, 10f);
            SensMira = Mathf.Clamp(sensMira, 0.1f, 3f);
            InverterY = inverterY ? -1f : 1f;
        }

        /// <summary>Arrasto em px (y para cima). "Sensibilidade ao mirar" so' enquanto o dedo mira a partir do Fogo.</summary>
        public void Olhar(Vector2 deltaPx, bool mirando)
        {
            float s = Sens * (mirando ? SensMira : 1f);
            Yaw += deltaPx.x * s;
            Pitch = Mathf.Clamp(Pitch - deltaPx.y * s * InverterY, PITCH_MIN, PITCH_MAX);
        }

        /// <summary>Relogio dos modos: fora do Normal o pitch e o braco caminham para o do modo (o dedo ainda orbita).</summary>
        public void Tick(float dt)
        {
            float k = Mathf.Min(RESPOSTA_MODO * dt, 1f);
            float bracoAlvo = BRACO;
            switch (ModoAtual)
            {
                case Modo.Castelo: bracoAlvo = BRACO_CASTELO; Pitch = Mathf.Lerp(Pitch, PITCH_CASTELO, k); break;
                case Modo.Queda: bracoAlvo = BRACO_QUEDA; Pitch = Mathf.Lerp(Pitch, PITCH_QUEDA, k); break;
            }
            Braco = Mathf.Lerp(Braco, bracoAlvo, k);
        }

        public Quaternion Rotacao => Quaternion.Euler(Pitch * Mathf.Rad2Deg, Yaw * Mathf.Rad2Deg, 0f);
        public Vector3 Frente => new Vector3(Mathf.Sin(Yaw), 0f, Mathf.Cos(Yaw));
        public Vector3 Direita => new Vector3(Mathf.Cos(Yaw), 0f, -Mathf.Sin(Yaw));
        public Vector3 Direcao3D => Rotacao * Vector3.forward;

        /// <summary>Do pivo (pe' do mago) ao olho (ombro) e a posicao DESEJADA (antes da colisao).</summary>
        public void Posicionar(Vector3 pivoPe, out Vector3 olho, out Vector3 desejada)
        {
            bool ombro = ModoAtual == Modo.Normal;
            olho = pivoPe + Vector3.up * (ombro ? ALTURA_PIVO : 0f) + (ombro ? Direita * OMBRO_X + Vector3.up * OMBRO_Y : Vector3.zero);
            desejada = olho - Direcao3D * Braco;
        }

        /// <summary>Direcao de mira a partir de um gesto na tela (x direita, y frente); zero = a propria camera.</summary>
        public Vector3 MiraDoGesto(Vector2 gesto)
        {
            if (gesto.sqrMagnitude < 0.0001f) return Direcao3D;
            Vector3 d = (Direita * gesto.x + Frente * gesto.y).normalized;
            d.y = Direcao3D.y;   // a elevacao continua sendo a da camera
            return d.normalized;
        }
    }

    /// <summary>Casca: a Camera (MainCamera) que segue o Pawn com colisao por SphereCast, ignorando o proprio corpo.</summary>
    public sealed class CameraTerceiraPessoa : MonoBehaviour
    {
        public CameraLogica Logica { get; private set; } = new CameraLogica();
        public Camera Cam { get; private set; }
        public Pawn Alvo;
        /// <summary>Quando o mago viaja no castelo, o pivo e' o castelo.</summary>
        public Transform Castelo;

        public static CameraTerceiraPessoa Criar(Pawn alvo)
        {
            var go = new GameObject("CameraJogador");
            go.tag = "MainCamera";   // a HUD acha por Camera.main
            var c = go.AddComponent<CameraTerceiraPessoa>();
            c.Cam = go.AddComponent<Camera>();
            c.Cam.fieldOfView = CameraLogica.FOV;
            c.Cam.nearClipPlane = 0.1f;
            Ilha.LigarPos(c.Cam);   // tonemapping/bloom/cor do volume da ilha
            // Sem AudioListener aqui: o UNICO da cena vive no Main (o Sfx atenua por distancia sozinho).
            // Dois ouvintes ou nenhum = aviso do Unity por frame (visto no portao PlayMode de 11/09).
            c.Alvo = alvo;
            c.AplicarConfig(ConfigLogica.Atual);
            ConfigLogica.Mudou += c.AplicarConfig;
            return c;
        }

        void OnDestroy() { ConfigLogica.Mudou -= AplicarConfig; }

        void AplicarConfig(ConfigLogica cfg)
        {
            Logica.Config(cfg.Float(ConfigLogica.K_SENSIBILIDADE), cfg.Float(ConfigLogica.K_SENS_MIRA), cfg.Bool(ConfigLogica.K_INVERTER_Y));
        }

        void LateUpdate()
        {
            if (Alvo == null) return;
            Queda q = Alvo.Queda;
            bool noCastelo = q != null && q.Fase == Queda.NO_CASTELO && Castelo != null;
            Logica.ModoAtual = noCastelo ? CameraLogica.Modo.Castelo : (q != null && q.NoAr ? CameraLogica.Modo.Queda : CameraLogica.Modo.Normal);
            Logica.Tick(Time.deltaTime);
            Vector3 pivo = noCastelo ? Castelo.position : Alvo.Pos;
            Vector3 olho, desejada;
            Logica.Posicionar(pivo, out olho, out desejada);
            Vector3 centro = noCastelo ? pivo : pivo + Vector3.up * CameraLogica.ALTURA_PIVO;
            bool deCima;
            transform.position = Colidir(centro, olho, desejada, Alvo.transform, out deCima);
            // de cima, a camera olha para a cabeca do mago (a rotacao da Logica apontaria para o horizonte, de dentro do canto)
            transform.rotation = deCima ? Quaternion.LookRotation(centro - transform.position, Vector3.up) : Logica.Rotacao;
        }

        /// <summary>
        /// Encosta a camera na primeira parede entre o olho e a posicao desejada (o proprio mago nao conta).
        /// TRES camadas, porque uma so' falhou na foto de 11/09 (camera dentro da rocha, mago encostado nela):
        /// 1) o OMBRO (0,78 m ao lado) pode estar dentro da parede; um SphereCast que COMECA dentro de um colisor o
        ///    reporta a distancia ZERO, e o grampo de 0,2 m deixava a camera dentro. Entao primeiro se vai do centro da
        ///    cabeca (livre: o corpo do mago ocupa) ate' o ombro, e o olho recua para onde o caminho e' livre. 2) o SphereCast de sempre. 3) se ainda assim o ponto final esta' dentro
        ///    de algo, recua em passos ate' sair (ou fica no olho). Camera dentro de parede e' tela preta: nunca.
        /// </summary>
        public static Vector3 Colidir(Vector3 centro, Vector3 olho, Vector3 desejada, Transform ignorar)
        {
            bool deCima;
            return Colidir(centro, olho, desejada, ignorar, out deCima);
        }

        /// <summary>
        /// `deCima` = a camera em canto (braco atras curto) subiu para olhar o mago de cima — a saida classica de
        /// toda 3a pessoa: o jogador encostado numa rocha ve' a rocha na lente, e de cima ve' o jogo.
        /// </summary>
        public static Vector3 Colidir(Vector3 centro, Vector3 olho, Vector3 desejada, Transform ignorar, out bool deCima)
        {
            deCima = false;
            float bracoPedido = (desejada - olho).magnitude;
            Vector3 olhoLivre;
            Vector3 pos = ColidirUma(centro, olho, desejada, ignorar, out olhoLivre);
            float livre = (pos - olhoLivre).magnitude;
            if (livre >= CameraLogica.BRACO_CURTO || bracoPedido < 0.001f) return pos;
            // canto: tenta de CIMA (70 graus), pelo mesmo caminho
            Vector3 dir = desejada - olho; dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f) dir = Vector3.back; else dir.Normalize();
            Vector3 desejadaAlta = centro + (Vector3.up * 0.94f + dir * 0.34f) * bracoPedido;
            Vector3 olhoAlto;
            Vector3 posAlta = ColidirUma(centro, centro, desejadaAlta, ignorar, out olhoAlto);
            float livreAlta = (posAlta - centro).magnitude;
            if (livreAlta > livre * 1.5f && livreAlta >= CameraLogica.BRACO_CURTO * 0.5f) { deCima = true; return posAlta; }
            return pos;
        }

        static Vector3 ColidirUma(Vector3 centro, Vector3 olho, Vector3 desejada, Transform ignorar, out Vector3 olhoLivre)
        {
            float r = CameraLogica.RAIO_COLISAO;
            // 1) do centro da cabeca ao ombro
            Vector3 ao = olho - centro;
            float dOmbro = ao.magnitude;
            if (dOmbro > 0.001f)
            {
                RaycastHit[] hs = Physics.SphereCastAll(centro, r, ao / dOmbro, dOmbro, ~0, QueryTriggerInteraction.Ignore);
                float livre = dOmbro;
                for (int i = 0; i < hs.Length; i++)
                    if (Conta(hs[i].collider, ignorar) && hs[i].distance < livre) livre = Mathf.Max(hs[i].distance - 0.05f, 0f);
                if (livre < dOmbro) olho = centro + ao / dOmbro * livre;
            }
            olhoLivre = olho;
            // 2) do olho a posicao desejada
            Vector3 d = desejada - olho;
            float dist = d.magnitude;
            if (dist < 0.001f) return olho;
            Vector3 dir = d / dist;
            float melhor = dist;
            RaycastHit[] hits = Physics.SphereCastAll(olho, r, dir, dist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
                if (Conta(hits[i].collider, ignorar) && hits[i].distance < melhor) melhor = Mathf.Max(hits[i].distance, 0.2f);
            Vector3 pos = olho + dir * melhor;
            // 3) a guarda: dentro de algo? recua ate' sair
            for (int passo = 0; passo < 8 && Dentro(pos, r, ignorar); passo++)
            {
                melhor *= 0.5f;
                pos = olho + dir * melhor;
            }
            return Dentro(pos, r, ignorar) ? olho : pos;
        }

        static bool Conta(Collider c, Transform ignorar) => c != null && (ignorar == null || !c.transform.IsChildOf(ignorar));

        static readonly Collider[] _perto = new Collider[8];
        static bool Dentro(Vector3 p, float r, Transform ignorar)
        {
            int n = Physics.OverlapSphereNonAlloc(p, r, _perto, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (Conta(_perto[i], ignorar)) return true;
            return false;
        }
    }
}
