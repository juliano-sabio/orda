using System.Collections;
using UnityEngine;

public class ImposicaoPassiva : MonoBehaviour
{
    public float cooldown  = 18f;
    public float duracao   = 3f;
    public float reducao   = 0.30f; // 30% menos dano
    public float raio      = 4f;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= cooldown)
        {
            timer = 0f;
            Ativar();
        }
    }

    void Ativar()
    {
        var cols = Physics2D.OverlapCircleAll(transform.position, raio);
        foreach (var col in cols)
        {
            var ic = col.GetComponent<InimigoController>()
                  ?? col.GetComponentInParent<InimigoController>();
            if (ic != null && !ic.estaMorrendo)
            {
                var debuff = ic.GetComponent<ImposicaoDebuff>();
                if (debuff == null)
                    ic.gameObject.AddComponent<ImposicaoDebuff>().Aplicar(reducao, duracao);
                else
                    debuff.Renovar(duracao);
            }
        }

        StartCoroutine(EfeitoAura());
    }

    IEnumerator EfeitoAura()
    {
        const int   SEGS    = 48;
        const float RAIO_MX = 4.2f;

        var go           = new GameObject("ImposicaoAura");
        go.transform.position = transform.position;
        var lr           = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop          = true;
        lr.positionCount = SEGS;
        lr.material      = new Material(Shader.Find("Sprites/Default"));
        lr.sortingOrder  = 11;

        Vector2 centro = transform.position;

        for (float t = 0f; t < duracao; t += Time.deltaTime)
        {
            float fade = 1f - (t / duracao);
            float r    = Mathf.Lerp(RAIO_MX * 0.6f, RAIO_MX, t / duracao);
            lr.startColor = lr.endColor = new Color(0.55f, 0.15f, 0.9f, fade * 0.75f);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.3f, 0.08f, t / duracao);
            for (int i = 0; i < SEGS; i++)
            {
                float a = i / (float)SEGS * Mathf.PI * 2f;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
            }
            yield return null;
        }
        Destroy(go);
    }
}

public class ImposicaoDebuff : MonoBehaviour
{
    private InimigoController ic;
    private float danoOriginal;
    private Coroutine timer;

    public void Aplicar(float reducao, float dur)
    {
        ic = GetComponent<InimigoController>();
        if (ic == null) { Destroy(this); return; }
        danoOriginal  = ic.danoAtual;
        ic.danoAtual *= (1f - reducao);
        timer = StartCoroutine(Expirar(dur));
    }

    public void Renovar(float dur)
    {
        if (timer != null) StopCoroutine(timer);
        timer = StartCoroutine(Expirar(dur));
    }

    IEnumerator Expirar(float dur)
    {
        yield return new WaitForSeconds(dur);
        Restaurar();
        Destroy(this);
    }

    void OnDestroy() => Restaurar();

    void Restaurar()
    {
        if (ic != null) ic.danoAtual = danoOriginal;
        ic = null;
    }
}
