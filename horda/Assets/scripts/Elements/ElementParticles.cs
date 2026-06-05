using UnityEngine;

public class ElementParticles : MonoBehaviour
{
    // Adiciona partículas de trail ao projétil baseado no elemento
    public static void AdicionarTrail(GameObject alvo, ElementType tipo)
    {
        if (tipo == ElementType.None) return;
        var def = ElementRegistry.Instance?.Get(tipo);
        Color cor = def != null ? def.cor : Color.white;

        var child = new GameObject("ElementTrail");
        child.transform.SetParent(alvo.transform, false);

        var ps = child.AddComponent<ParticleSystem>();
        ConfigurarTrail(ps, tipo, cor);
    }

    // Spawna burst de impacto na posição
    public static void SpawnarImpacto(Vector3 posicao, ElementType tipo)
    {
        if (tipo == ElementType.None) return;
        var def = ElementRegistry.Instance?.Get(tipo);
        Color cor = def != null ? def.cor : Color.white;

        var go = new GameObject("ElementImpact_" + tipo);
        go.transform.position = posicao;
        var ps = go.AddComponent<ParticleSystem>();
        ConfigurarImpacto(ps, tipo, cor);
        Destroy(go, 2f);
    }

    static void ConfigurarTrail(ParticleSystem ps, ElementType tipo, Color cor)
    {
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 0.4f;
        main.startSpeed = 0.5f;
        main.startSize = 0.12f;
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        var gradient = new Gradient();

        switch (tipo)
        {
            case ElementType.Fogo:
                main.startSize = 0.18f;
                main.startSpeed = 1.2f;
                main.startLifetime = 0.5f;
                emission.rateOverTime = 30f;
                gradient.SetKeys(
                    new[] { new GradientColorKey(new Color(1f,0.5f,0.1f), 0f), new GradientColorKey(new Color(1f,0.2f,0f), 0.5f), new GradientColorKey(Color.black, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                vel.y = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
                vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                break;

            case ElementType.Raio:
                main.startSize = 0.08f;
                main.startSpeed = 3f;
                main.startLifetime = 0.2f;
                emission.rateOverTime = 40f;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f,1f,0.3f), 0.5f), new GradientColorKey(cor, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.3f), new GradientAlphaKey(0f, 1f) });
                shape.radius = 0.05f;
                break;

            case ElementType.Gelo:
                main.startSize = 0.14f;
                main.startSpeed = 0.3f;
                main.startLifetime = 0.8f;
                emission.rateOverTime = 15f;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.7f,0.9f,1f), 0.5f), new GradientColorKey(cor, 1f) },
                    new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.7f), new GradientAlphaKey(0f, 1f) });
                break;

            case ElementType.Trevas:
                main.startSize = 0.2f;
                main.startSpeed = 0.4f;
                main.startLifetime = 0.7f;
                emission.rateOverTime = 25f;
                gradient.SetKeys(
                    new[] { new GradientColorKey(cor, 0f), new GradientColorKey(new Color(0.2f,0f,0.3f), 0.5f), new GradientColorKey(Color.black, 1f) },
                    new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0.4f, 0.6f), new GradientAlphaKey(0f, 1f) });
                var velT = ps.velocityOverLifetime;
                velT.enabled = true;
                velT.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                velT.y = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
                velT.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                break;

            case ElementType.Luz:
                main.startSize = 0.1f;
                main.startSpeed = 1.5f;
                main.startLifetime = 0.3f;
                emission.rateOverTime = 50f;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f,1f,0.7f), 0.5f), new GradientColorKey(cor, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
                shape.radius = 0.05f;
                break;

            case ElementType.Corrompido:
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.22f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
                emission.rateOverTime = 35f;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(cor, 0.4f), new GradientColorKey(new Color(0.1f,0f,0.2f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.4f), new GradientAlphaKey(0f, 1f) });
                break;

            default:
                // Ar, Terra, Água, Planta, Gelo fallback
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(cor, 0.3f), new GradientColorKey(cor, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;
        }

        colorLife.color = gradient;
        ps.Play();
    }

    static void ConfigurarImpacto(ParticleSystem ps, ElementType tipo, Color cor)
    {
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
        main.maxParticles = 30;

        var burst = new ParticleSystem.Burst(0f, 20);
        var impactEmission = ps.emission;
        impactEmission.rateOverTime = 0f;
        impactEmission.SetBursts(new[] { burst });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;

        var colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        var gradient = new Gradient();

        switch (tipo)
        {
            case ElementType.Fogo:
                main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
                burst.count = 25;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f,0.6f,0f), 0.3f), new GradientColorKey(new Color(1f,0.1f,0f), 0.8f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;

            case ElementType.Raio:
                main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
                main.startLifetime = 0.25f;
                burst.count = 40;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f,1f,0.5f), 0.4f), new GradientColorKey(cor, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;

            case ElementType.Gelo:
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
                main.startLifetime = 0.8f;
                burst.count = 15;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.8f,0.95f,1f), 0.5f), new GradientColorKey(cor, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
                break;

            case ElementType.Trevas:
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 3f);
                main.startLifetime = 0.7f;
                burst.count = 18;
                gradient.SetKeys(
                    new[] { new GradientColorKey(cor, 0f), new GradientColorKey(new Color(0.2f,0f,0.3f), 0.6f), new GradientColorKey(Color.black, 1f) },
                    new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.5f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;

            case ElementType.Luz:
                main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.2f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
                main.startLifetime = 0.3f;
                burst.count = 35;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f,1f,0.8f), 0.4f), new GradientColorKey(cor, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
                break;

            case ElementType.Corrompido:
                main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.3f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 8f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.7f);
                burst.count = 30;
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(cor, 0.3f), new GradientColorKey(new Color(0.1f,0f,0.15f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;

            default:
                gradient.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(cor, 0.4f), new GradientColorKey(cor, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) });
                break;
        }

        impactEmission.SetBursts(new[] { burst });
        colorLife.color = gradient;
        ps.Play();
    }
}
