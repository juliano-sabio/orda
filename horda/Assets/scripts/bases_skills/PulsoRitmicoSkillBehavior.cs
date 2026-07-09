using System.Collections;
using UnityEngine;

public class PulsoRitmicoSkillBehavior : SkillBehavior, ISkillComRecarga, IEvoluivel
{
    float baseDano      = 8f;
    float multiplicador = 0.3f;
    float intervalo     = 2.5f;
    float raio          = 3.5f;
    float timer;
    public bool  EmRecarga    => timer > 0f;
    public float TimerRecarga => timer;
    public float RecargaTotal => intervalo;

    float DanoAtual => baseDano + (playerStats != null ? playerStats.attack * multiplicador : 0f);

        public void OnEvolucaoAplicada(SkillEvolutionType tipo) { if (tipo == SkillEvolutionType.PulsoAlcance) raio *= 1.75f; }
    public override void Initialize(PlayerStats stats) => base.Initialize(stats);

    static readonly Color COR_ORIG = new Color(0.3f, 0.9f, 0.5f);
    Color CorElemento() {
        if (skillData != null && skillData.appliedElement != ElementType.None)
            return ElementRegistry.Instance?.GetCor(skillData.appliedElement) ?? COR_ORIG;
        return COR_ORIG;
    }

    public void ConfigurarDeSkillData(SkillData data)
    {
        this.skillData = data;
        baseDano  = data.attackBonus > 0f        ? data.attackBonus        : 15f;
        intervalo = data.activationInterval > 0f ? data.activationInterval : 1.2f;
        raio      = data.specialValue > 0f       ? data.specialValue       : 3.5f;
        timer     = intervalo;
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
        SomSkill.Tocar(SomSkill.Tipo.PulsoDark, centro, 0.45f); // co-op: toca tb na cópia cosmética (outro player ouve)
        // Pulso Gravitacional (Lendária): implode os inimigos pro centro e depois os arremessa
        if (!cosmetico && SkillEvolutionManager.Tem(SkillEvolutionType.PulsoRitmicoLend))
        {
            StartCoroutine(PulsoGravitacional());
            StartCoroutine(VisualPulso(centro));
            return;
        }

        if (!cosmetico) // co-op: cópia cosmética só faz o visual do pulso
        {
            float danoReal = SkillEvolutionManager.Tem(SkillEvolutionType.PulsoIntenso) ? DanoAtual * 2f : DanoAtual;

            var hits = Physics2D.OverlapCircleAll(centro, raio);
            var atingidos = new System.Collections.Generic.List<InimigoController>();
            foreach (var col in hits)
            {
                var ic = col.GetComponent<InimigoController>() ?? col.GetComponentInParent<InimigoController>();
                if (ic == null || ic.estaMorrendo) continue;
                ic.ReceberDano(danoReal, false);
                SkillElementEffect.Aplicar(skillData, ic.gameObject, danoReal, this);
                atingidos.Add(ic);
            }
            // Pulso em Cadeia: propaga para vizinhos
            if (SkillEvolutionManager.Tem(SkillEvolutionType.PulsoCadeia))
                foreach (var ic in atingidos)
                    EvolutionFX.SpawnShockwave(ic.transform.position, 2f, danoReal * 0.5f, this);
        }

        StartCoroutine(VisualPulso(centro));
    }

    IEnumerator PulsoGravitacional()
    {
        float raioG = raio * 1.7f;
        // Fase 1: implosão — puxa os inimigos pro centro
        for (float t = 0f; t < 0.45f && playerStats != null; t += Time.deltaTime)
        {
            EvolutionFX.PuxarInimigos(playerStats.transform.position, raioG, 7f, 0.2f);
            yield return null;
        }
        if (playerStats == null) yield break;
        // Fase 2: arremesso radial + dano de colisão + onda de choque
        Vector2 c = playerStats.transform.position;
        EvolutionFX.ArremessarInimigos(c, raioG, DanoAtual * 2f, this);
        EvolutionFX.SpawnShockwave(c, raioG, DanoAtual, this);
        SomSkill.Tocar(SomSkill.Tipo.PulsoDark, c, 0.6f);
    }

    IEnumerator VisualPulso(Vector2 centro)
    {
        const int SEGS = 32;
        var go = new GameObject("PulsoRitmico");
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true; lr.loop = true; lr.positionCount = SEGS;
        lr.material = new Material(Shader.Find("Sprites/Default")); lr.sortingOrder = 11;
        Destroy(go, 1f); // failsafe

        float dur = 0.35f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            if (go == null) yield break;
            float p = t / dur;
            float r = Mathf.Lerp(0.2f, raio * 1.1f, p);
            Color ce = CorElemento();
            lr.startWidth = lr.endWidth = Mathf.Lerp(0.22f, 0.02f, p);
            lr.startColor = lr.endColor = new Color(ce.r, ce.g, ce.b, Mathf.Lerp(1f, 0f, p));
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


