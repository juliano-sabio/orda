using UnityEngine;
using System.Collections;

public class EscudoEspinhosoSkillBehavior : SkillBehavior
{
    [Header("Configurações do Escudo Espinhoso")]
    public float dano = 20f;
    public float cooldown = 3f;
    public int maxHits = 3;

    [Header("Visual")]
    public GameObject prefabEscudo;

    private EscudoEspinhoPeca peca;

    public override void Initialize(PlayerStats stats)
    {
        base.Initialize(stats);
        SpawnEscudo(stats.transform);
    }

    public void UpdateFromSkillData(SkillData skill)
    {
        if (skill.attackBonus > 0)            dano = skill.attackBonus;
        if (skill.cooldown > 0)               cooldown = skill.cooldown;
        if (skill.escudoPrefabVisual != null) prefabEscudo = skill.escudoPrefabVisual;
    }

    private void SpawnEscudo(Transform player)
    {
        if (peca != null) Destroy(peca.gameObject);

        if (prefabEscudo == null)
        {
            Debug.LogError("❌ [EscudoEspinhoso] Prefab não atribuído no SkillData!");
            return;
        }

        // Filho do player — segue automaticamente, sem LateUpdate
        GameObject obj = Instantiate(prefabEscudo, player.position, Quaternion.identity, player);
        obj.transform.localPosition = Vector3.zero;

        Collider2D col = obj.GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circle = obj.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.5f;
        }
        else
        {
            col.isTrigger = true;
        }

        // Remove Rigidbody se houver — o trigger detecta via RB do inimigo
        Rigidbody2D rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null) Destroy(rb);

        // Garante que o escudo sempre renderiza na frente do player
        foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>(true))
            sr.sortingOrder = 6;

        peca = obj.GetComponent<EscudoEspinhoPeca>();
        if (peca == null) peca = obj.AddComponent<EscudoEspinhoPeca>();
        peca.Initialize(this);
    }

    public void OnEnemyHit()
    {
        if (peca != null) StartCoroutine(CooldownReativacao());
    }

    private IEnumerator CooldownReativacao()
    {
        peca.gameObject.SetActive(false);
        yield return new WaitForSeconds(cooldown);
        if (peca != null)
        {
            peca.ResetarHits();
            peca.gameObject.SetActive(true);
        }
    }

    public float GetDano() => dano;

    public override void ApplyEffect() { }

    public override void RemoveEffect()
    {
        if (peca != null) Destroy(peca.gameObject);
    }

    void OnDestroy() => RemoveEffect();
}
