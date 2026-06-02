using System.Collections;
using UnityEngine;

public class PulsoRitmicoSkillBehavior : SkillBehavior
{
    float baseDano      = 8f;
    float multiplicador = 0.3f;
    float intervalo     = 2.5f;
    float raio          = 3.5f;
    float timer;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano  = data.attackBonus > 0f        ? data.attackBonus        : 15f;
        intervalo = data.activationInterval > 0f ? data.activationInterval : 1.2f;
        raio      = data.specialValue > 0f       ? data.specialValue       : 3.5f;
        timer     = intervalo;
    }

    static readonly Color COR_ORIG = new Color(0.3f, 0.9f, 0.5f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    void Update()
    {
        if (playerStats == null) return;
        timer -= Time.deltaTime;
        if (timer <= 0f) { timer = intervalo; Pulsar(); }
    }

    public override void ApplyEffect() => Pulsar();

    void Pulsar()
    {
        Vector2 centro = playerStats.transform.position;

        // Dano em área
        var hits = Physics2D.OverlapCircleAll(centro, raio);
        foreach (var col in hits)
        {
            var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
            if (ic == null || ic.estaMorrendo) continue;
            ic.ReceberDano(DanoAtual, false);
            SkillElementEffect.Aplicar(skillData, ic.gameObject, DanoAtual, this);
        }

        StartCoroutine(VisualPulso(centro));
    }

    IEnumerator VisualPulso(Vector2 centro)
    {
        const int SEGS = 32;
        var go = new GameObject("PulsoRitmico");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 11;

        float dur = 0.35f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            float r = Mathf.Lerp(0.2f, raio * 1.1f, p);
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.22f, 0.02f, p);
            { Color ce = CorElemento(); lr.startColor = lr.endColor = new Color(ce.r, ce.g, ce.b, Mathf.Lerp(1f, 0f, p)); }
            for (int i = 0; i < SEGS; i++)
            {
                float ang = 360f / SEGS * i * Mathf.Deg2Rad;
                lr.SetPosition(i, centro + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r);
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }
}
