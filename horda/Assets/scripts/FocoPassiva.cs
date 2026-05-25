using System.Collections;
using UnityEngine;

public class FocoPassiva : MonoBehaviour
{
    public float cooldown = 18f;
    public float duracao  = 6f; // janela de crítico garantido

    private PlayerStats stats;
    private float timer = 0f;

    void Start()
    {
        stats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= cooldown)
        {
            timer = 0f;
            StartCoroutine(AtivarFoco());
        }
    }

    IEnumerator AtivarFoco()
    {
        float critOriginal = stats.critChance;
        stats.critChance = 1f;

        StartCoroutine(EfeitoCiano());

        yield return new WaitForSeconds(duracao);

        if (stats != null) stats.critChance = critOriginal;
    }

    IEnumerator EfeitoCiano()
    {
        const int   SEGS    = 36;
        const float DUR     = 0.5f;
        const float RAIO_MX = 1.8f;

        var go           = new GameObject("FocoBurst");
        go.transform.position = transform.position;
        var lr           = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 15;

        Vector2 centro = transform.position;

        for (float t = 0f; t < DUR; t += Time.deltaTime)
        {
            float p    = t / DUR;
            float raio = Mathf.Lerp(0f, RAIO_MX, p);
            float a    = Mathf.Lerp(1f, 0f, p);
            lr.startColor = lr.endColor = new Color(0.1f, 0.9f, 1f, a);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.3f, 0.03f, p);
            for (int i = 0; i < SEGS; i++)
            {
                float ang = i / (float)SEGS * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio);
            }
            yield return null;
        }
        Destroy(go);

        // Piscar sprite amarelo durante a janela
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        Color original = sr.color;
        for (int i = 0; i < 3; i++)
        {
            sr.color = new Color(1f, 0.95f, 0.2f);
            yield return new WaitForSeconds(0.1f);
            sr.color = original;
            yield return new WaitForSeconds(0.1f);
        }
    }
}
