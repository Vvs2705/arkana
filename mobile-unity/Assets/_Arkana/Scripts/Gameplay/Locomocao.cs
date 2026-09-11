using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O CORPO EM MOVIMENTO, puro (porte de Pawn.gd: move_velocity/accel_step/dash_speed_at/apply_gravity/pular).
    /// Recebe a INTENCAO ja' moldada (o joystick virtual aplica deadzone e curva: JoystickLogica e' a fonte unica;
    /// aqui NAO se reaplica, senao sobra degrau) e devolve o DESLOCAMENTO do frame — quem move o corpo e' a casca.
    /// A velocidade maxima chega de fora, SEMPRE por Velocidade.Produto (ver Speed) — ninguem escreve m/s.
    /// A mana da flutuacao e' cobrada AQUI (por ref): a regra "sem mana a magia larga o corpo" e' testavel sem cena.
    /// </summary>
    public sealed class Locomocao
    {
        /// <summary>m/s^2 de decaimento do empurrao — KNOB local (pedir entrada em Balance se sobreviver ao playtest).</summary>
        public const float KNOCK_DECAY = 9f;
        /// <summary>Descida (m/s) a partir da qual o corpo assume a pose de QUEDA (parado sem chao NAO e' cair).</summary>
        public const float QUEDA_ANIM_V = 1.5f;
        public const float GRAVIDADE = 9.81f;
        /// <summary>Vy no chao: o CharacterController precisa de um empurrao para baixo para manter isGrounded.</summary>
        public const float COLA_CHAO = -1f;

        /// <summary>Velocidade horizontal INTENCIONAL (sem knockback).</summary>
        public Vector3 VelH;
        public float Vy;
        public Vector3 Knockback;
        /// <summary>A velocidade REAL aplicada no ultimo Tick (o que a animacao e o banking leem).</summary>
        public Vector3 Vel;
        public float DodgeLeft, DodgeCd, IframesLeft;
        public float FlutuaS;
        public bool Flutuando;
        /// <summary>Histerese idle/run (RunAnimEnter/Exit).</summary>
        public bool RunAnim;
        /// <summary>Inclinacao do visual (rad) pra dentro da curva.</summary>
        public float Bank;
        /// <summary>Ultima velocidade maxima recebida (frac de velocidade para giro e banking).</summary>
        public float VelMax = Balance.Player.Speed;

        private Vector3 _dodgeDir;
        private float _prevYaw;
        private bool _yawInit;

        public float VelocidadeHorizontal => new Vector2(Vel.x, Vel.z).magnitude;
        public bool Esquivando => DodgeLeft > 0f;
        public bool DodgePronto => DodgeCd <= 0f;
        /// <summary>1 = acabou de usar, 0 = pronta. A UI so' LE.</summary>
        public float DodgeCdFrac => DodgeCd / Balance.Dodge.Cooldown;
        /// <summary>Flutuar e' UMA vez por salto: o chao devolve o saldo.</summary>
        public float FlutuaRestante => Mathf.Max(Balance.Flutuar.DurMax - FlutuaS, 0f);
        /// <summary>0..1 de quanto o corpo esta' rapido em relacao ao maximo (pivo e banking).</summary>
        public float Frac => Mathf.Clamp01(new Vector2(VelH.x, VelH.z).magnitude / Mathf.Max(VelMax, 0.01f));

        /// <summary>O produto unico. Quem chama junta terreno (agua), status (lentidao) e postura (derrubado).</summary>
        public static float Speed(float terreno, float status, float postura) =>
            Velocidade.Produto(Balance.Player.Speed, terreno, status, postura);

        /// <summary>Stick (x direita, y frente; magnitude 0..1 ja' moldada) -> direcao no mundo pelo yaw da camera (rad).</summary>
        public static Vector3 DirDoStick(Vector2 stick, float yawCam)
        {
            var frente = new Vector3(Mathf.Sin(yawCam), 0f, Mathf.Cos(yawCam));
            var direita = new Vector3(Mathf.Cos(yawCam), 0f, -Mathf.Sin(yawCam));
            Vector3 d = direita * stick.x + frente * stick.y;
            return d.sqrMagnitude > 1f ? d.normalized : d;
        }

        /// <summary>Um passo de aceleracao horizontal: a velocidade PERSEGUE o alvo. Soltou = freia; inverteu = strafe seco; no ar = menos.</summary>
        public static Vector3 AccelStep(Vector3 cur, Vector3 target, float dt, bool noChao)
        {
            float rate = Balance.Move.Accel;
            if (target.sqrMagnitude < 0.0001f) rate = Balance.Move.Brake;
            else if (Vector3.Dot(cur, target) < 0f) rate = Balance.Move.TurnAccel;
            if (!noChao) rate *= Balance.Move.AirControl;
            return Vector3.MoveTowards(cur, target, rate * dt);
        }

        /// <summary>Rampa linear de burst*media ate' (2-burst)*media: a MEDIA continua distance/duration, so' o arranque muda.</summary>
        public static float DashSpeedAt(float p)
        {
            float media = Balance.Dodge.Distance / Balance.Dodge.Duration;
            return media * Mathf.Lerp(Balance.Dodge.Burst, 2f - Balance.Dodge.Burst, Mathf.Clamp01(p));
        }

        /// <summary>Dash em `dir` (XZ) com i-frames. Quem chama resolve o "parado = para onde olha".</summary>
        public bool Dodge(Vector3 dir)
        {
            if (!DodgePronto) return false;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return false;
            _dodgeDir = dir.normalized;
            DodgeLeft = Balance.Dodge.Duration;
            DodgeCd = Balance.Dodge.Cooldown;
            IframesLeft = Balance.Dodge.Iframes;
            return true;
        }

        /// <summary>SALTO ARCANO: so' do chao (sem pulo duplo) e nunca nadando.</summary>
        public bool Pular(bool noChao, bool nadando)
        {
            if (!noChao || nadando) return false;
            Vy = Balance.Player.JumpV;
            return true;
        }

        public void Empurrar(Vector3 v) { Knockback += v; }

        /// <summary>
        /// Um passo. `dir` = intencao no mundo (0..1). Devolve o DESLOCAMENTO do frame (a casca faz Move).
        /// `mana` e' cobrada pela flutuacao (Balance.Flutuar.ManaPorS) enquanto `querFlutuar` e ha' saldo.
        /// </summary>
        public Vector3 Tick(float dt, Vector3 dir, bool noChao, bool nadando, float velMax, bool atordoado, bool querFlutuar, ref float mana)
        {
            if (dt <= 0f) return Vector3.zero;
            VelMax = velMax;
            DodgeCd = Mathf.Max(DodgeCd - dt, 0f);
            IframesLeft = Mathf.Max(IframesLeft - dt, 0f);
            Knockback = Vector3.MoveTowards(Knockback, Vector3.zero, KNOCK_DECAY * dt);
            if (atordoado) dir = Vector3.zero;   // o corpo nao obedece (teto Status.StunCap)
            dir.y = 0f;

            Vector3 h;
            if (DodgeLeft > 0f)
            {
                float dur = Balance.Dodge.Duration;
                // amostra no MEIO do passo: rampa linear -> a soma dos passos da' a distancia exata (sem isso fica ~5% curto)
                float p = Mathf.Clamp01((dur - DodgeLeft + dt * 0.5f) / dur);
                DodgeLeft -= dt;
                h = _dodgeDir * DashSpeedAt(p);
                // momentum: ao acabar o dash o corpo JA' esta' correndo pra la'
                VelH = _dodgeDir * velMax * Balance.Dodge.ExitMomentum;
            }
            else
            {
                VelH = AccelStep(VelH, dir * velMax, dt, noChao);
                h = VelH + Knockback;
            }

            if (nadando)
            {
                // a lamina segura o corpo: gravidade zero (quem persegue a superficie e' Agua.Flutuar)
                Vy = 0f; FlutuaS = 0f; Flutuando = false;
            }
            else if (noChao && Vy <= 0f)
            {
                FlutuaS = 0f; Flutuando = false;   // o chao devolve a flutuacao inteira
                Vy = COLA_CHAO;
            }
            else
            {
                bool quer = querFlutuar && !noChao && FlutuaRestante > 0f && mana > 0f;
                if (quer)
                {
                    // FLUTUAR: descida lenta e constante, nunca sobe (voo livre desequilibra). Paga a mesma mana do tiro.
                    FlutuaS += dt;
                    mana = Mathf.Max(mana - Balance.Flutuar.ManaPorS * dt, 0f);
                    Vy = -Balance.Flutuar.DescV;
                    Flutuando = mana > 0f;   // mana no fim = a magia larga o corpo, na hora
                }
                else
                {
                    Flutuando = false;
                    Vy -= GRAVIDADE * dt;
                }
            }
            Vel = new Vector3(h.x, Vy, h.z);
            return Vel * dt;
        }

        /// <summary>Banking: inclina pra dentro da curva, proporcional ao giro deste frame e a quanto esta' rapido.</summary>
        public void AtualizarBank(float yawRad, float dt)
        {
            if (dt <= 0f) return;
            if (!_yawInit) { _prevYaw = yawRad; _yawInit = true; }   // sem isso o 1o frame acusa um giro gigante
            float yawRate = Mathf.DeltaAngle(_prevYaw * Mathf.Rad2Deg, yawRad * Mathf.Rad2Deg) * Mathf.Deg2Rad / dt;
            _prevYaw = yawRad;
            float alvo = Mathf.Clamp(yawRate * Balance.Move.BankGain, -Balance.Move.BankMax, Balance.Move.BankMax) * Frac;
            Bank = Mathf.Lerp(Bank, alvo, Mathf.Min(Balance.Move.BankRate * dt, 1f));
        }

        /// <summary>"run"/"idle" pela velocidade REAL com histerese (um limiar so' pisca na fronteira).</summary>
        public string AnimNoChao()
        {
            float v = VelocidadeHorizontal;
            if (RunAnim) { if (v < Balance.Move.RunAnimExit) RunAnim = false; }
            else if (v > Balance.Move.RunAnimEnter) RunAnim = true;
            return RunAnim ? "run" : "idle";
        }
    }
}
