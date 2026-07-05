using System.Collections;
using UnityEngine;

// Efeito ao INFUNDIR um elemento numa skill: partículas da cor do elemento convergem no player
// (feito pra lembrar o "fragmentos voando pro player" das evoluções). Mundo-espaço, no player
// local — em co-op cada um vê no seu próprio. Usa tempo NÃO-escalado (a infusão ocorre pausada).
public static class InfusaoFXParticulas
{
    public static void Disparar(Vector3 pos, Color cor)
    {
        var go = new GameObject("InfusaoFX");
        go.transform.position = pos;
        go.AddComponent<InfusaoFXRunner>().Iniciar(cor);
    }
}

public class InfusaoFXRunner : MonoBehaviour
{
    public void Iniciar(Color cor) => StartCoroutine(Anim(cor));

    IEnumerator Anim(Color cor)
    {
        const int N = 16;
        var parts  = new Transform[N];
        var srs    = new SpriteRenderer[N];
        var origem = new Vector2[N];

        for (int i = 0; i < N; i++)
        {
            var p   = new GameObject("infp");
            p.transform.SetParent(transform, false);
            float ang = i / (float)N * Mathf.PI * 2f + Random.Range(-0.15f, 0.15f);
            float r   = 2.4f + Random.Range(-0.4f, 0.4f);
            origem[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r;
            p.transform.localPosition = origem[i];
            p.transform.localScale    = Vector3.one * Random.Range(0.18f, 0.34f);
            var sr = p.AddComponent<SpriteRenderer>();
            sr.sprite       = FogoSprites.Disco; // disco branco cacheado, tingido pela cor do elemento
            sr.color        = cor;
            sr.sortingOrder = 30;
            parts[i] = p.transform; srs[i] = sr;
        }

        // Anel-flash na cor do elemento explodindo do player (leitura de "infundido").
        var anelGO = new GameObject("infAnel");
        anelGO.transform.SetParent(transform, false);
        var anelSr = anelGO.AddComponent<SpriteRenderer>();
        anelSr.sprite = FogoSprites.Anel;
        anelSr.color  = cor;
        anelSr.sortingOrder = 29;

        float dur = 0.5f;
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float pnorm = Mathf.Clamp01(t / dur);
            float e     = pnorm * pnorm; // acelera ao convergir no centro (player)
            for (int i = 0; i < N; i++)
            {
                if (parts[i] == null) continue;
                parts[i].localPosition = Vector2.Lerp(origem[i], Vector2.zero, e);
                parts[i].localScale    = Vector3.one * Mathf.Lerp(0.34f, 0.05f, pnorm);
                var c = cor; c.a = Mathf.Lerp(1f, 0f, pnorm); srs[i].color = c;
            }
            // anel expande e some
            anelGO.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 2.6f, pnorm);
            var ca = cor; ca.a = Mathf.Lerp(0.8f, 0f, pnorm); anelSr.color = ca;
            yield return null;
        }
        Destroy(gameObject);
    }
}
