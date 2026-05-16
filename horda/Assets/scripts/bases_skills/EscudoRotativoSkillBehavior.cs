using UnityEngine;
using System.Collections.Generic;

public class EscudoRotativoSkillBehavior : SkillBehavior
{
    [Header("Configurações do Escudo")]
    public float velocidadeRotacao = 120f;
    public float raioOrbita = 1.8f;
    public int quantidadeEscudos = 1;

    [Header("Projétil Refletido")]
    public GameObject prefabProjetil;
    public float danoReflexao = 35f;
    public float velocidadeProjetil = 14f;
    public float vidaProjetil = 4f;
    public PlayerStats.Element elementoReflexao = PlayerStats.Element.None;

    [Header("Visual")]
    public GameObject prefabEscudo;
    public float ajusteRotacao = 0f;

    private List<EscudoPeca> pecas = new List<EscudoPeca>();
    private float anguloAtual = 0f;
    private bool isAtivo = false;
    private Transform playerTransform;

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
        playerTransform = stats.transform;
        isAtivo = true;
        SpawnEscudos();
    }

    public void UpdateFromSkillData(SkillData skill)
    {
        if (skill.attackBonus > 0)          danoReflexao = skill.attackBonus;
        elementoReflexao =                  skill.element;
        if (skill.escudoPrefabVisual != null)  prefabEscudo = skill.escudoPrefabVisual;
        if (skill.projectilePrefab2D != null)  prefabProjetil = skill.projectilePrefab2D;
        if (skill.escudoVelocidadeRotacao > 0) velocidadeRotacao = skill.escudoVelocidadeRotacao;
        if (skill.escudoRaioOrbita > 0)        raioOrbita = skill.escudoRaioOrbita;
        if (skill.escudoQuantidade > 0)        quantidadeEscudos = skill.escudoQuantidade;
    }

    private void SpawnEscudos()
    {
        foreach (var p in pecas) { if (p != null) Destroy(p.gameObject); }
        pecas.Clear();

        for (int i = 0; i < quantidadeEscudos; i++)
        {
            if (prefabEscudo == null)
            {
                Debug.LogError("❌ [EscudoRotativo] Prefab do escudo não atribuído no SkillData!");
                return;
            }

            GameObject obj = Instantiate(prefabEscudo);

            // Garantir collider trigger
            Collider2D col = obj.GetComponent<Collider2D>();
            if (col == null)
            {
                CapsuleCollider2D caps = obj.AddComponent<CapsuleCollider2D>();
                caps.isTrigger = true;
                caps.size = new Vector2(0.4f, 1.2f);
            }
            else
            {
                col.isTrigger = true;
            }

            // RB kinematic independente (necessário para OnTriggerEnter2D em EscudoPeca)
            Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
            if (rb == null) rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            EscudoPeca peca = obj.GetComponent<EscudoPeca>();
            if (peca == null) peca = obj.AddComponent<EscudoPeca>();
            peca.Initialize(this);

            pecas.Add(peca);
        }
    }

    void LateUpdate()
    {
        if (!isAtivo || playerTransform == null) return;

        anguloAtual += velocidadeRotacao * Time.deltaTime;
        if (anguloAtual >= 360f) anguloAtual -= 360f;

        float step = 360f / Mathf.Max(1, pecas.Count);

        for (int i = 0; i < pecas.Count; i++)
        {
            if (pecas[i] == null) continue;

            float ang = (anguloAtual + i * step) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raioOrbita;

            // Posição em world space via rb — correto para kinematic
            pecas[i].GetComponent<Rigidbody2D>().position = (Vector2)playerTransform.position + offset;

            // Rotação: abertura do sprite sempre apontando para o player
            float angParaPlayer = Mathf.Atan2(-offset.y, -offset.x) * Mathf.Rad2Deg;
            pecas[i].GetComponent<Rigidbody2D>().rotation = angParaPlayer + ajusteRotacao;
        }
    }

    public void ReflectirProjetil(Vector2 direcaoProjetil, Vector2 posicaoColisao)
    {
        if (prefabProjetil == null) return;

        GameObject proj = Instantiate(prefabProjetil, posicaoColisao, Quaternion.identity);
        ProjectileController2D ctrl = proj.GetComponent<ProjectileController2D>();
        if (ctrl != null)
        {
            ctrl.Initialize(null, danoReflexao, velocidadeProjetil, vidaProjetil, elementoReflexao);
            ctrl.LaunchInDirection(-direcaoProjetil.normalized, velocidadeProjetil);
        }
    }

    public override void ApplyEffect() { }

    public override void RemoveEffect()
    {
        isAtivo = false;
        foreach (var p in pecas) if (p != null) Destroy(p.gameObject);
        pecas.Clear();
    }

    void OnDestroy() => RemoveEffect();
}
