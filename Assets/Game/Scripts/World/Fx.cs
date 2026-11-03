using System.Collections;
using UnityEngine;

namespace Rescate
{
    /// <summary>
    /// Efectos de partículas baratos y reutilizables. Dos ParticleSystem persistentes
    /// (polvo y chispas) en espacio de mundo que se emiten bajo demanda con EmitParams:
    /// sin instanciar objetos durante el juego. El material vive en Resources/FX.
    /// Si el material no existe, los efectos se ignoran sin errores.
    /// </summary>
    public class Fx : MonoBehaviour
    {
        static Fx instance;
        static bool failed;

        ParticleSystem dust;
        ParticleSystem spark;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            instance = null;
            failed = false;
        }

        static Fx Get()
        {
            if (instance != null) return instance;
            if (failed) return null;

            Material mat = Resources.Load<Material>("FX/FxParticle");
            if (mat == null)
            {
                failed = true;
                Debug.LogWarning("[Fx] Falta Resources/FX/FxParticle.mat; efectos desactivados.");
                return null;
            }

            GameObject go = new GameObject("[Fx]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<Fx>();
            instance.dust = CreateSystem(go.transform, "Dust", mat, -0.05f, 22, 160);
            instance.spark = CreateSystem(go.transform, "Spark", mat, 1.1f, 30, 220);
            return instance;
        }

        static ParticleSystem CreateSystem(Transform parent, string name, Material mat,
            float gravity, int order, int max)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.maxParticles = max;
            main.startLifetime = 0.5f;
            main.startSpeed = 0f;
            main.startSize = 0.2f;

            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.45f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.25f));

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 30f;
            limit.dampen = 0.12f;

            ParticleSystemRenderer r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortingOrder = order;

            ps.Play();
            return ps;
        }

        static void Emit(ParticleSystem ps, Vector3 pos, Color color, int count,
            float speed, float size, float life, Vector2 bias, float spread)
        {
            if (ps == null) return;
            ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                float a = Random.Range(-spread, spread) * Mathf.Deg2Rad;
                Vector2 dir = Rotate(bias.sqrMagnitude > 0.0001f ? bias.normalized : Random.insideUnitCircle.normalized, a);
                p.position = pos + (Vector3)(Random.insideUnitCircle * 0.08f);
                p.velocity = dir * speed * Random.Range(0.55f, 1f);
                p.startSize = size * Random.Range(0.7f, 1.2f);
                p.startLifetime = life * Random.Range(0.75f, 1.1f);
                p.startColor = color;
                ps.Emit(p, 1);
            }
        }

        static Vector2 Rotate(Vector2 v, float rad)
        {
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        // ------------------------------------------------------------ API

        /// <summary>Polvo de pasos / aterrizaje. dir: -1, 0 o 1 (hacia dónde se levanta).</summary>
        public static void Dust(Vector3 pos, int count, float strength, float dirX = 0f)
        {
            Fx f = Get();
            if (f == null) return;
            Color c = new Color(0.86f, 0.80f, 0.68f, 0.75f);
            if (Mathf.Abs(dirX) > 0.01f)
                Emit(f.dust, pos, c, count, 1.6f * strength, 0.28f * strength, 0.38f, new Vector2(dirX, 0.45f), 25f);
            else
            {
                Emit(f.dust, pos, c, count / 2 + 1, 2.2f * strength, 0.30f * strength, 0.42f, new Vector2(-1f, 0.25f), 20f);
                Emit(f.dust, pos, c, count / 2 + 1, 2.2f * strength, 0.30f * strength, 0.42f, new Vector2(1f, 0.25f), 20f);
            }
        }

        /// <summary>Chispas radiales (recogida, golpe, muerte, checkpoint).</summary>
        public static void Burst(Vector3 pos, Color color, int count, float speed = 4f, float size = 0.16f, float life = 0.45f)
        {
            Fx f = Get();
            if (f == null) return;
            Emit(f.spark, pos, color, count, speed, size, life, Vector2.zero, 180f);
        }

        /// <summary>Chorro direccional (lanzamiento mágico, impacto).</summary>
        public static void Spray(Vector3 pos, Color color, Vector2 dir, int count, float speed = 4f, float spread = 30f)
        {
            Fx f = Get();
            if (f == null) return;
            Emit(f.spark, pos, color, count, speed, 0.14f, 0.35f, dir, spread);
        }

        // ------------------------------------------------------------ game feel

        public static void HitStop(float seconds)
        {
            Fx f = Get();
            if (f == null || seconds <= 0f) return;
            if (Time.timeScale < 0.99f) return; // no interferir con pausa/derrota
            f.StartCoroutine(f.HitStopRoutine(seconds));
        }

        IEnumerator HitStopRoutine(float seconds)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(seconds);
            if (Time.timeScale > 0.01f && Time.timeScale < 0.99f) Time.timeScale = 1f;
        }
    }
}
