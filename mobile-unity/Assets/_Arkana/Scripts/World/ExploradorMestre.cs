using UnityEngine;
using UnityEngine.InputSystem;

namespace Arkana.World
{
    /// <summary>
    /// Passeio de verificacao da ilha do Documento Mestre (doc §17.2), so' no Play da cena IlhaMestre (teclado + mouse):
    /// capsula do doc §3 (1,80 m, raio 0,45) andando a 6 m/s, correndo a 9 m/s (Shift), salto de 1,2 m (Espaco);
    /// F liga o VOO (60 m/s, Q/E desce/sobe) para atravessar a ilha. Botao direito do mouse gira a camera.
    /// O canto da tela mostra a posicao em coordenadas do DOC (x leste, y norte, z cima) e a regiao mais proxima.
    /// Nao e' o controle do jogo: e' a trena para julgar escala e caminho.
    /// </summary>
    public sealed class ExploradorMestre : MonoBehaviour
    {
        public float anda = 6f, corre = 9f, voo = 60f, salto = 1.2f;
        CharacterController cc;
        Transform cam;
        float yaw = 45f, pitch = 12f, vy;
        bool voando;
        IlhaMestre ilha;

        void Start()
        {
            if (!Application.isPlaying) return;
            ilha = GetComponent<IlhaMestre>();
            var corpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            corpo.name = "Explorador_1_80m";
            Destroy(corpo.GetComponent<Collider>());
            corpo.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            cc = corpo.AddComponent<CharacterController>();
            cc.height = 2f;       // na escala 0,9 da capsula: 1,80 m
            cc.radius = 0.5f;     // 0,45 m
            float x = -900f, y = -350f;   // acampamento (R06)
            corpo.transform.position = new Vector3(x, (ilha != null && ilha.D != null ? ilha.Altura(x, y) : 150f) + 2f, y);
            cam = Camera.main != null ? Camera.main.transform : new GameObject("CamExplorador").AddComponent<Camera>().transform;
            cam.GetComponent<Camera>().nearClipPlane = 0.1f;
        }

        void Update()
        {
            if (cc == null) return;
            var k = Keyboard.current;
            var m = Mouse.current;
            if (k == null) return;
            if (k.fKey.wasPressedThisFrame) { voando = !voando; vy = 0f; }
            if (m != null && m.rightButton.isPressed)
            {
                Vector2 d = m.delta.ReadValue();
                yaw += d.x * 0.15f;
                pitch = Mathf.Clamp(pitch - d.y * 0.15f, -30f, 70f);
            }
            Vector3 frente = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, lado = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            Vector3 dir = Vector3.zero;
            if (k.wKey.isPressed) dir += frente;
            if (k.sKey.isPressed) dir -= frente;
            if (k.dKey.isPressed) dir += lado;
            if (k.aKey.isPressed) dir -= lado;
            dir = dir.sqrMagnitude > 0f ? dir.normalized : dir;
            if (voando)
            {
                float sobe = (k.eKey.isPressed ? 1f : 0f) - (k.qKey.isPressed ? 1f : 0f);
                cc.Move((dir + Vector3.up * sobe) * voo * (k.leftShiftKey.isPressed ? 3f : 1f) * Time.deltaTime);
            }
            else
            {
                if (cc.isGrounded) vy = k.spaceKey.wasPressedThisFrame ? Mathf.Sqrt(2f * 9.81f * salto) : -2f;
                vy -= 9.81f * Time.deltaTime;
                cc.Move((dir * (k.leftShiftKey.isPressed ? corre : anda) + Vector3.up * vy) * Time.deltaTime);
            }
            Vector3 alvo = cc.transform.position + Vector3.up * 1.2f;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            cam.position = alvo - rot * Vector3.forward * (voando ? 25f : 6f);
            cam.rotation = rot;
        }

        void OnGUI()
        {
            if (cc == null) return;
            Vector3 p = cc.transform.position;
            string reg = "";
            float melhor = float.MaxValue;
            if (ilha != null && ilha.D != null)
                foreach (var r in ilha.D.regioes)
                {
                    float d = new Vector2(p.x - r.x, p.z - r.y).magnitude;
                    if (r.id != "R11" && d < melhor) { melhor = d; reg = $"{r.id} {r.nome} ({d:0} m)"; }
                }
            GUI.Label(new Rect(12, 10, 900, 60),
                $"x {p.x:0}  y {p.z:0}  z {p.y - 0.9f:0}   |   {(voando ? "VOO (F)" : "a pe'")}  |  perto: {reg}\n" +
                "WASD anda, Shift corre, Espaco salta, F voa (Q/E), botao direito gira a camera");
        }
    }
}
