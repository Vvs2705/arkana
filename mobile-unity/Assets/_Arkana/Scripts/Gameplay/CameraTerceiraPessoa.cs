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
        public const float BRACO = 4.15f, BRACO_CASTELO = 26f, BRACO_QUEDA = 6f;
        public const float OMBRO_X = 0.78f, OMBRO_Y = 0.22f, ALTURA_PIVO = 1.85f;
        public const float FOV = 62f;
        public const float PITCH_MIN = -0.5f, PITCH_MAX = 1.1f;
        public const float PITCH_PADRAO = 0.30f, PITCH_CASTELO = 0.55f, PITCH_QUEDA = 0.12f;
        public const float RAIO_COLISAO = 0.25f;
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
            transform.position = Colidir(olho, desejada);
            transform.rotation = Logica.Rotacao;
        }

        /// <summary>Encosta a camera na primeira parede entre o olho e a posicao desejada (o proprio mago nao conta).</summary>
        private Vector3 Colidir(Vector3 olho, Vector3 desejada)
        {
            Vector3 d = desejada - olho;
            float dist = d.magnitude;
            if (dist < 0.001f) return desejada;
            Vector3 dir = d / dist;
            float melhor = dist;
            RaycastHit[] hits = Physics.SphereCastAll(olho, CameraLogica.RAIO_COLISAO, dir, dist, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == null || hits[i].collider.transform.IsChildOf(Alvo.transform)) continue;
                if (hits[i].distance < melhor) melhor = Mathf.Max(hits[i].distance, 0.2f);
            }
            return olho + dir * melhor;
        }
    }
}
