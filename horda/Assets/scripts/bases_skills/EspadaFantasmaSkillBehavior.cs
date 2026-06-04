using System.Collections;
using UnityEngine;

public class EspadaFantasmaSkillBehavior : SkillBehavior, ISkillComRecarga, IEvoluivel
{
    float baseDano      = 10f;
    float multiplicador = 0.5f;
    float intervalo     = 5f;
    float alcanceCorte  = 3f;
    float timer;
    public bool  EmRecarga    => timer > 0f;
    public float TimerRecarga => timer;
    public float RecargaTotal => intervalo;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

        public void OnEvolucaoAplicada(SkillEvolutionType tipo) { if (tipo == SkillEvolutionType.EspadaAlcance) alcanceCorte *= 1.5f; }
    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    public void ConfigurarDeSkillData(SkillData data)
    {
        baseDano    = data.attackBonus > 0f          ? data.attackBonus        : 20f;
        intervalo   = data.activationInterval > 0f   ? data.activationInterval : 2.5f;
        alcanceCorte = data.specialValue > 0f        ? data.specialValue       : 3f;
        timer       = intervalo;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; StartCoroutine(Cortes()); }
    }

    public override void ApplyEffect() => StartCoroutine(Cortes());

    IEnumerator Cortes()
    {
        var alvo = EncontrarMaisProximo();
        Vector2 origem = playerStats.transform.position;
        Vector2 dir = alvo != null
            ? ((Vector2)alvo.transform.position - origem).normalized
            : Vector2.right;

        // Espada Dupla: também corta por trás
        float[] angulos = SkillEvolutionManager.Tem(SkillEvolutionType.EspadaDuplaFantasma)
            ? new float[] { -25f, 0f, 25f, 155f, 180f, 205f }
            : new float[] { -25f, 0f, 25f };
        foreach (float angOffset in angulos)
        {
            float rad = angOffset * Mathf.Deg2Rad;
            Vector2 dirCorte = new Vector2(
                dir.x * Mathf.Cos(rad) - dir.y * Mathf.Sin(rad),
                dir.x * Mathf.Sin(rad) + dir.y * Mathf.Cos(rad));

            StartCoroutine(ExecutarCorte(playerStats.transform.position, dirCorte));
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator ExecutarCorte(Vector2 origem, Vector2 dir)
    {
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Sprite de espada
        var go = new GameObject("EspadaCorte");
        go.transform.position = origem + dir * (alcanceCorte * 0.5f);
        go.transform.rotation = Quaternion.Euler(0f, 0f, ang - 90f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GerarEspada(); sr.color = new Color(0.85f, 0.85f, 1f, 0.9f); sr.sortingOrder = 14;
        go.transform.localScale = new Vector3(0.5f, alcanceCorte * 0.4f, 1f);

        // Colisão e dano
        var hits = Physics2D.OverlapBoxAll(origem + dir * (alcanceCorte * 0.5f),
            new Vector2(0.8f, alcanceCorte), ang);
        foreach (var col in hits)
        {
            var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
            if (ic == null || ic.estaMorrendo) continue;
            ic.ReceberDano(DanoAtual, false);
            if (SkillEvolutionManager.Tem(SkillEvolutionType.EspadaFlamejante))
                EvolutionFX.AplicarChamas(ic, this, DanoAtual * 0.3f, 3f);
        }

        // Fade
        for (float t = 0f; t < 0.2f; t += Time.deltaTime)
        {
            if (sr == null) yield break;
            sr.color = new Color(0.85f, 0.85f, 1f, Mathf.Lerp(0.9f, 0f, t / 0.2f));
            go.transform.localScale = new Vector3(Mathf.Lerp(0.5f, 0.1f, t / 0.2f), alcanceCorte * 0.4f, 1f);
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    InimigoController EncontrarMaisProximo()
    {
        var todos = FindObjectsByType<InimigoController>(FindObjectsSortMode.None);
        InimigoController melhor = null; float menorDist = float.MaxValue;
        Vector2 orig = playerStats.transform.position;
        foreach (var ic in todos)
        {
            if (ic.estaMorrendo) continue;
            float d = Vector2.Distance(ic.transform.position, orig);
            if (d < menorDist) { menorDist = d; melhor = ic; }
        }
        return melhor;
    }

    static Sprite GerarEspada()
    {
        int w = 8, h = 32; var tex = new Texture2D(w, h, TextureFormat.RGBA32, false); tex.filterMode = FilterMode.Bilinear;
        float cx = w * 0.5f;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            float nx = Mathf.Abs(x + 0.5f - cx) / cx; float ny = y / (float)(h - 1);
            float larg = ny < 0.7f ? (1f - nx) : (1f - nx) * (1f - (ny - 0.7f) / 0.3f * 2f);
            float a = Mathf.Clamp01(larg + Mathf.Max(0f, 0.8f - nx * 6f));
            tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
        }
        tex.Apply(); return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
    }
}


