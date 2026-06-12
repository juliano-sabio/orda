using System.Collections;
using UnityEngine;

public class ProjetilEspecialPrincesa : MonoBehaviour
{
    public enum Tipo { Queima }

    public Tipo  tipo;
    public float dano = 20f;

    bool atingiu;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (atingiu || !other.CompareTag("Player")) return;
        atingiu = true;

        var ps = other.GetComponent<PlayerStats>();

        EfeitoRunner.Criar().StartCoroutine(Queimar(ps, dano * 0.4f, 3f));
        EfeitoRunner.Criar().StartCoroutine(EfeitoQueima(other.gameObject, 3f));

        Destroy(gameObject);
    }

    static IEnumerator Queimar(PlayerStats ps, float danoPorSegundo, float duracao)
    {
        float elapsed = 0f, tick = 0.5f;
        while (elapsed < duracao && ps != null)
        {
            yield return new WaitForSeconds(tick);
            elapsed += tick;
            ps.TakeDamage(danoPorSegundo * tick);
        }
    }

    static IEnumerator EfeitoQueima(GameObject player, float duracao)
    {
        if (player == null) yield break;

        var sr = player.GetComponent<SpriteRenderer>();
        Color corOriginal = sr != null ? sr.color : Color.white;

        // Textura de partícula de fogo (4×4 px)
        var texFogo = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texFogo.filterMode = FilterMode.Point;
        for (int y = 0; y < 4; y++)
        for (int x = 0; x < 4; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(2f, 2f));
            float t = Mathf.Clamp01(1f - d / 2f);
            // laranja no centro → vermelho nas bordas
            Color c = Color.Lerp(new Color(1f, 0.15f, 0f), new Color(1f, 0.65f, 0f), t);
            c.a = t;
            texFogo.SetPixel(x, y, c);
        }
        texFogo.Apply();
        var sprFogo = Sprite.Create(texFogo, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f);

        // Pool de partículas de fogo
        const int QTD = 8;
        var particulas  = new GameObject[QTD];
        var velocidades = new Vector2[QTD];
        var tempos      = new float[QTD];

        int sortLayer = sr != null ? sr.sortingLayerID : 0;
        int sortOrder = sr != null ? sr.sortingOrder + 1 : 1;

        for (int i = 0; i < QTD; i++)
            particulas[i] = CriarParticulaFogo(sprFogo, sortLayer, sortOrder);

        void ReiniciarParticula(int i)
        {
            if (player == null || particulas[i] == null) return;
            float ox = UnityEngine.Random.Range(-0.3f, 0.3f);
            particulas[i].transform.position = player.transform.position + new Vector3(ox, -0.2f, 0f);
            float sx = UnityEngine.Random.Range(0.08f, 0.18f);
            particulas[i].transform.localScale = new Vector3(sx, sx, 1f);
            velocidades[i] = new Vector2(UnityEngine.Random.Range(-0.4f, 0.4f),
                                         UnityEngine.Random.Range(0.8f, 1.6f));
            tempos[i] = 0f;
        }

        // Distribui os tempos para que não apareçam todos ao mesmo tempo
        for (int i = 0; i < QTD; i++)
        {
            ReiniciarParticula(i);
            tempos[i] = UnityEngine.Random.Range(0f, 0.5f); // offset inicial
        }

        float elapsed = 0f;
        while (elapsed < duracao)
        {
            elapsed += Time.deltaTime;
            float prog = elapsed / duracao;

            // Flickering laranja no sprite do player
            if (sr != null)
            {
                float flicker = Mathf.Sin(elapsed * 18f) * 0.5f + 0.5f;
                sr.color = Color.Lerp(new Color(1f, 0.45f, 0.1f), corOriginal, flicker * 0.5f);
            }

            // Anima cada partícula
            for (int i = 0; i < QTD; i++)
            {
                if (particulas[i] == null) continue;
                tempos[i] += Time.deltaTime;
                float vida = 0.45f;
                if (tempos[i] >= vida) { ReiniciarParticula(i); continue; }

                float tp = tempos[i] / vida;
                particulas[i].transform.position += new Vector3(velocidades[i].x, velocidades[i].y, 0f) * Time.deltaTime;
                velocidades[i].x *= 0.95f; // amortece drift lateral

                float alpha = Mathf.Lerp(0.9f, 0f, tp * tp);
                float scl   = Mathf.Lerp(1f, 0.3f, tp);
                var psr = particulas[i].GetComponent<SpriteRenderer>();
                if (psr != null)
                {
                    Color c = psr.color;
                    psr.color = new Color(c.r, c.g, c.b, alpha * (1f - prog * 0.5f));
                    float s = particulas[i].transform.localScale.x * scl / (scl + Time.deltaTime * 0.01f);
                    particulas[i].transform.localScale = new Vector3(s, s, 1f);
                }
            }

            yield return null;
        }

        // Restaura cor e destrói partículas
        if (sr != null) sr.color = corOriginal;
        for (int i = 0; i < QTD; i++)
            if (particulas[i] != null) UnityEngine.Object.Destroy(particulas[i]);
        UnityEngine.Object.Destroy(texFogo);
    }

    static GameObject CriarParticulaFogo(Sprite spr, int sortLayerID, int sortOrder)
    {
        var go = new GameObject("FogoParticula");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite         = spr;
        sr.sortingLayerID = sortLayerID;
        sr.sortingOrder   = sortOrder;
        return go;
    }

}

// Runner leve para executar coroutines independentes do projétil/efeito,
// para que não sejam interrompidas quando o objeto que as criou é destruído.
public class EfeitoRunner : MonoBehaviour
{
    public static EfeitoRunner Criar()
    {
        var go = new GameObject("EfeitoRunner");
        Destroy(go, 10f);
        return go.AddComponent<EfeitoRunner>();
    }

    public void Run(IEnumerator rotina) => StartCoroutine(rotina);
}
