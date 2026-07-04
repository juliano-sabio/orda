using UnityEngine;
using System.Collections;

// Rastreia a cor original do inimigo para evitar corrida entre efeitos sobrepostos
public class EnemyColorTracker : MonoBehaviour
{
    public Color originalColor = Color.white;
    public int   activeEffects = 0;
    public bool  baseCapturada = false; // true quando a cor-base do spawn já foi fixada (InimigoController)
}

// Adiciona/remove efeitos visuais em inimigos quando recebem status elemental
public static class EnemyStatusVisual
{
    // Ponto de entrada: adiciona visual para a duração do efeito
    public static void Aplicar(GameObject alvo, CharacteristicType tipo, float duracao, MonoBehaviour caller)
    {
        if (alvo == null || caller == null) return;
        caller.StartCoroutine(RotinhaVisual(alvo, tipo, duracao));
    }

    static IEnumerator RotinhaVisual(GameObject alvo, CharacteristicType tipo, float duracao)
    {
        if (alvo == null) yield break;

        var sr = alvo.GetComponent<SpriteRenderer>();
        if (sr == null) { yield return new WaitForSeconds(duracao); yield break; }

        // Tracker garante que a cor original (pré-qualquer tint) não é corrompida por efeitos sobrepostos.
        // A base é fixada no spawn pelo InimigoController (baseCapturada). Só capturamos aqui como
        // FALLBACK (inimigo sem InimigoController) e, mesmo assim, só quando não há efeito ativo —
        // nunca sobrescrevendo a base do spawn (evita salvar cor tingida → slime preta).
        var tracker = alvo.GetComponent<EnemyColorTracker>();
        if (tracker == null) tracker = alvo.AddComponent<EnemyColorTracker>();
        if (!tracker.baseCapturada && tracker.activeEffects == 0)
        {
            tracker.originalColor = sr.color;
            tracker.baseCapturada = true;
        }
        tracker.activeEffects++;

        // Adiciona partículas do efeito
        GameObject particulas = CriarParticulasStatus(alvo, tipo);

        // Aplica tint no sprite
        AplicarTint(sr, tipo);

        yield return new WaitForSeconds(duracao);

        // Remove efeitos — só restaura cor quando todos os efeitos terminaram
        if (tracker != null)
        {
            tracker.activeEffects--;
            if (tracker.activeEffects <= 0 && sr != null)
                sr.color = tracker.originalColor;
        }
        if (particulas != null) Object.Destroy(particulas);
    }

    static void AplicarTint(SpriteRenderer sr, CharacteristicType tipo)
    {
        switch (tipo)
        {
            case CharacteristicType.Queimadura:
            case CharacteristicType.Explosao:
                sr.color = new Color(1f, 0.55f, 0.2f, 1f); break;      // laranja fogo

            case CharacteristicType.Lentidao:
                sr.color = new Color(0.5f, 0.75f, 1f, 1f); break;      // azul gelo suave

            case CharacteristicType.Atordoamento:
                sr.color = new Color(0.85f, 0.85f, 0.85f, 1f); break;  // cinza atordoado

            case CharacteristicType.Congelamento:
                sr.color = new Color(0.65f, 0.88f, 1f, 1f); break;     // azul gelo

            case CharacteristicType.Veneno:
                sr.color = new Color(0.45f, 1f, 0.35f, 1f); break;     // verde veneno

            case CharacteristicType.Enraizamento:
                sr.color = new Color(0.4f, 0.75f, 0.2f, 1f); break;    // verde escuro raiz

            case CharacteristicType.Maldicao:
                sr.color = new Color(0.65f, 0.3f, 0.85f, 1f); break;   // roxo maldição

            case CharacteristicType.Paralisia:
                sr.color = new Color(1f, 1f, 0.3f, 1f); break;         // amarelo elétrico

            case CharacteristicType.Fragilidade:
                sr.color = new Color(0.8f, 0.85f, 1f, 0.85f); break;   // branco-azul frágil

            case CharacteristicType.Cegamento:
                sr.color = new Color(1f, 1f, 0.85f, 0.7f); break;      // branco ofuscado

            case CharacteristicType.RouboVida:
                sr.color = new Color(0.8f, 0.1f, 0.6f, 1f); break;     // magenta roubo vida

            case CharacteristicType.Cadeia:
            case CharacteristicType.Infeccao:
                sr.color = new Color(0.7f, 0.5f, 1f, 1f); break;       // roxo claro propagação
        }
    }

    static GameObject CriarParticulasStatus(GameObject alvo, CharacteristicType tipo)
    {
        var go = new GameObject("StatusVFX_" + tipo);
        go.transform.SetParent(alvo.transform, false);
        go.transform.localPosition = Vector3.zero;

        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 0.6f;
        main.startSpeed = 0.6f;
        main.startSize = 0.14f;
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.rateOverTime = 15f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.35f;

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        var gradient = new Gradient();

        switch (tipo)
        {
            // ── FOGO ────────────────────────────────────────────────────────────
            case CharacteristicType.Queimadura:
                main.startSize = 0.18f;
                main.startSpeed = 1.0f;
                main.startLifetime = 0.5f;
                emission.rateOverTime = 25f;
                gradient.SetKeys(
                    new[] { new GradientColorKey(new Color(1f,0.9f,0.3f), 0f), new GradientColorKey(new Color(1f,0.3f,0f), 0.5f), new GradientColorKey(new Color(0.2f,0.2f,0.2f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
                var velFogo = ps.velocityOverLifetime;
                velFogo.enabled = true;
                velFogo.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                velFogo.y = new ParticleSystem.MinMaxCurve(0.8f, 2f);
                velFogo.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                shape.radius = 0.3f;
                break;

            // ── LENTIDÃO ────────────────────────────────────────────────────────
            case CharacteristicType.Lentidao:
                main.startSize = 0.1f;
                main.startSpeed = 0.2f;
                main.startLifetime = 1.2f;
                emission.rateOverTime = 12f;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f,0.85f,1f), 0.5f), new GradientColorKey(new Color(0.3f,0.6f,1f), 1f) },
                    new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.4f, 0.7f), new GradientAlphaKey(0f, 1f) });
                break;

            // ── ATORDOAMENTO ────────────────────────────────────────────────────
            case CharacteristicType.Atordoamento:
                main.startSize = 0.12f;
                main.startSpeed = 1.5f;
                main.startLifetime = 0.8f;
                emission.rateOverTime = 20f;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.1f;
                var velStun = ps.velocityOverLifetime;
                velStun.enabled = true;
                velStun.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                velStun.y = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
                velStun.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                go.transform.localPosition = new Vector3(0f, 0.6f, 0f); // acima da cabeça
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f,1f,0.3f), 0.4f), new GradientColorKey(new Color(0.8f,0.8f,0.8f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;

            // ── CONGELAMENTO ────────────────────────────────────────────────────
            case CharacteristicType.Congelamento:
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
                main.startSpeed = 0.1f;
                main.startLifetime = 1.5f;
                emission.rateOverTime = 10f;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.7f,0.92f,1f), 0.5f), new GradientColorKey(new Color(0.4f,0.7f,1f), 1f) },
                    new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.7f), new GradientAlphaKey(0f, 1f) });
                break;

            // ── VENENO ──────────────────────────────────────────────────────────
            case CharacteristicType.Veneno:
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
                main.startLifetime = 1.0f;
                emission.rateOverTime = 18f;
                var velVeneno = ps.velocityOverLifetime;
                velVeneno.enabled = true;
                velVeneno.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                velVeneno.y = new ParticleSystem.MinMaxCurve(-0.3f, -0.8f);
                velVeneno.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                gradient.SetKeys(
                    new[] { new GradientColorKey(new Color(0.6f,1f,0.3f), 0f), new GradientColorKey(new Color(0.2f,0.8f,0.1f), 0.6f), new GradientColorKey(new Color(0.1f,0.3f,0f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;

            // ── ENRAIZAMENTO ────────────────────────────────────────────────────
            case CharacteristicType.Enraizamento:
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
                main.startLifetime = 1.2f;
                emission.rateOverTime = 12f;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.4f;
                go.transform.localPosition = new Vector3(0f, -0.3f, 0f); // nas pernas
                gradient.SetKeys(
                    new[] { new GradientColorKey(new Color(0.5f,1f,0.2f), 0f), new GradientColorKey(new Color(0.2f,0.6f,0.05f), 0.5f), new GradientColorKey(new Color(0.3f,0.2f,0f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.7f), new GradientAlphaKey(0f, 1f) });
                break;

            // ── MALDIÇÃO ────────────────────────────────────────────────────────
            case CharacteristicType.Maldicao:
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
                main.startSpeed = 0.4f;
                main.startLifetime = 1.0f;
                emission.rateOverTime = 20f;
                var velMald = ps.velocityOverLifetime;
                velMald.enabled = true;
                velMald.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                velMald.y = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
                velMald.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                gradient.SetKeys(
                    new[] { new GradientColorKey(new Color(0.7f,0.2f,1f), 0f), new GradientColorKey(new Color(0.3f,0f,0.6f), 0.5f), new GradientColorKey(Color.black, 1f) },
                    new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;

            // ── PARALISIA ───────────────────────────────────────────────────────
            case CharacteristicType.Paralisia:
                main.startSize = 0.08f;
                main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
                main.startLifetime = 0.3f;
                emission.rateOverTime = 35f;
                shape.radius = 0.2f;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f,1f,0.4f), 0.3f), new GradientColorKey(new Color(1f,0.8f,0f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
                break;

            // ── FRAGILIDADE ─────────────────────────────────────────────────────
            case CharacteristicType.Fragilidade:
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.0f);
                main.startLifetime = 0.8f;
                emission.rateOverTime = 15f;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.85f,0.9f,1f), 0.5f), new GradientColorKey(new Color(0.5f,0.6f,0.8f), 1f) },
                    new[] { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0.4f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;

            // ── CEGAMENTO ───────────────────────────────────────────────────────
            case CharacteristicType.Cegamento:
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
                main.startLifetime = 0.5f;
                emission.rateOverTime = 30f;
                go.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f,1f,0.8f), 0.5f), new GradientColorKey(new Color(0.9f,0.9f,0.6f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;

            // ── ROUBO DE VIDA ───────────────────────────────────────────────────
            case CharacteristicType.RouboVida:
                main.startSize = 0.1f;
                main.startSpeed = 1.5f;
                main.startLifetime = 0.5f;
                emission.rateOverTime = 20f;
                var velRV = ps.velocityOverLifetime;
                velRV.enabled = true;
                velRV.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                velRV.y = new ParticleSystem.MinMaxCurve(-1f, -2f);
                velRV.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                gradient.SetKeys(
                    new[] { new GradientColorKey(new Color(1f,0.1f,0.5f), 0f), new GradientColorKey(new Color(0.8f,0f,0.4f), 0.5f), new GradientColorKey(new Color(0.5f,0f,0.2f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;

            // ── CORROMPIDO (Cadeia, Infecção) ────────────────────────────────────
            case CharacteristicType.Cadeia:
            case CharacteristicType.Infeccao:
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.2f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                emission.rateOverTime = 25f;
                gradient.SetKeys(
                    new[] { new GradientColorKey(new Color(0.8f,0.4f,1f), 0f), new GradientColorKey(new Color(0.5f,0.1f,0.8f), 0.5f), new GradientColorKey(Color.black, 1f) },
                    new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;

            default:
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) });
                break;
        }

        colorLife.color = gradient;
        ps.Play();
        return go;
    }
}
