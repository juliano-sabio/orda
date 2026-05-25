using System.Collections;
using UnityEngine;

public class PulsoVitalPassiva : MonoBehaviour
{
    public float cooldown  = 20f;
    public float raio      = 4f;
    public float drenagem  = 8f;

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
            Pulsar();
        }
    }

    void Pulsar()
    {
        var cols = Physics2D.OverlapCircleAll(transform.position, raio);
        float totalCura = 0f;
        foreach (var col in cols)
        {
            var ic = col.GetComponent<InimigoController>()
                  ?? col.GetComponentInParent<InimigoController>();
            if (ic != null && !ic.estaMorrendo)
            {
                ic.ReceberDano(drenagem, false);
                totalCura += drenagem;
            }
        }
        if (totalCura > 0f) stats.Heal(totalCura);

        var go = new GameObject("PulsoVitalEfeito");
        go.transform.position = transform.position;
        go.AddComponent<PulsoVitalEfeito>().Iniciar(raio);
    }
}

public class PulsoVitalEfeito : MonoBehaviour
{
    public void Iniciar(float raioFinal) => StartCoroutine(Animar(raioFinal));

    IEnumerator Animar(float raioFinal)
    {
        const int   SEGS   = 48;
        const float DURACAO = 0.65f;

        var lr            = gameObject.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.loop           = true;
        lr.positionCount  = SEGS;
        lr.material       = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder   = 12;

        // segundo anel interno (mais rápido)
        var go2           = new GameObject("Inner");
        var lr2           = go2.AddComponent<LineRenderer>();
        lr2.useWorldSpace = true;
        lr2.loop          = true;
        lr2.positionCount = SEGS;
        lr2.material      = new Material(Shader.Find("Sprites/Default"));
        lr2.sortingOrder  = 13;

        Vector2 centro = transform.position;

        for (float t = 0f; t < DURACAO; t += Time.deltaTime)
        {
            float p = t / DURACAO;

            float r1 = Mathf.Lerp(0f, raioFinal, p);
            float a1 = Mathf.Lerp(0.9f, 0f, p);
            lr.startColor = lr.endColor = new Color(0.85f, 0.1f, 0.2f, a1);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.35f, 0.04f, p);
            Circulo(lr, centro, r1, SEGS);

            float r2 = Mathf.Lerp(0f, raioFinal * 0.55f, Mathf.Min(p * 1.8f, 1f));
            float a2 = Mathf.Lerp(1f, 0f, Mathf.Min(p * 2f, 1f));
            lr2.startColor = lr2.endColor = new Color(1f, 0.5f, 0.55f, a2);
            lr2.startWidth = lr2.endWidth = Mathf.Lerp(0.25f, 0.02f, p);
            Circulo(lr2, centro, r2, SEGS);

            yield return null;
        }
        Destroy(go2);
        Destroy(gameObject);
    }

    static void Circulo(LineRenderer lr, Vector2 c, float r, int segs)
    {
        for (int i = 0; i < segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            lr.SetPosition(i, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
        }
    }
}
