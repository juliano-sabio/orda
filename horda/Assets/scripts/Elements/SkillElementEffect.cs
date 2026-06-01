using UnityEngine;
using System.Collections;

public static class SkillElementEffect
{
    public static void Aplicar(SkillData skill, GameObject alvo, float danoBase, MonoBehaviour caller)
    {
        if (skill == null || alvo == null || caller == null) return;
        if (skill.appliedElement == ElementType.None || skill.appliedCharacteristicIndex < 0)
        {
            // Debug.Log("[Elemento] sem elemento: " + (skill != null ? skill.skillName : "null") + " elem=" + (skill != null ? skill.appliedElement.ToString() : "?"));
            return;
        }

        var def = ElementRegistry.Instance?.Get(skill.appliedElement);
        if (def == null || def.caracteristicas == null)
        {
            Debug.LogWarning("[Elemento] ElementRegistry nao encontrou: " + skill.appliedElement);
            return;
        }
        if (skill.appliedCharacteristicIndex >= def.caracteristicas.Length) return;

        var car = def.caracteristicas[skill.appliedCharacteristicIndex];
        if (car == null) return;

        Debug.Log($"[Elemento] APLICANDO {car.nome} ({skill.appliedElement}) em {alvo.name} | dano={danoBase:F1}");
        AplicarCaracteristica(car, alvo, danoBase, caller);
    }

    static void AplicarCaracteristica(ElementCharacteristic car, GameObject alvo, float danoBase, MonoBehaviour caller)
    {
        // Visual no inimigo (tint + partículas) pela duração do efeito
        float durVisual = ObterDuracaoVisual(car);
        if (durVisual > 0f) EnemyStatusVisual.Aplicar(alvo, car.tipo, durVisual, caller);

        var ic = alvo.GetComponent<InimigoController>();

        switch (car.tipo)
        {
            case CharacteristicType.Queimadura:
                float danoTick = car.valor1 > 0 ? car.valor1 : danoBase * 0.2f;
                float numTicks = car.valor2 > 0 ? car.valor2 : 3f;
                caller.StartCoroutine(AplicarDoT(ic, danoTick, numTicks, 1f));
                break;

            case CharacteristicType.Explosao:
                AplicarAoE(alvo.transform.position, car.valor1 > 0 ? car.valor1 : 2.5f,
                    danoBase * (car.valor2 > 0 ? car.valor2 : 0.6f), alvo);
                break;

            case CharacteristicType.Recuo:
                AplicarKnockback(alvo, car.valor1 > 0 ? car.valor1 : 12f);
                break;

            case CharacteristicType.Rajada:
                break;

            case CharacteristicType.Atordoamento:
                if (Random.value <= (car.valor2 > 0 ? car.valor2 : 0.4f))
                    caller.StartCoroutine(AplicarCC(ic, car.valor1 > 0 ? car.valor1 : 1.5f, "stun"));
                break;

            case CharacteristicType.EscudoPedra:
                break;

            case CharacteristicType.Lentidao:
                caller.StartCoroutine(AplicarSlow(ic, car.valor1 > 0 ? car.valor1 : 3f,
                    car.valor2 > 0 ? car.valor2 : 0.5f));
                break;

            case CharacteristicType.Cura:
                AplicarCura(danoBase * (car.valor1 > 0 ? car.valor1 : 0.15f));
                break;

            case CharacteristicType.Cadeia:
                AplicarCadeia(alvo, car.valor1 > 0 ? car.valor1 : 4f,
                    danoBase * (car.valor2 > 0 ? car.valor2 : 0.6f), 2);
                break;

            case CharacteristicType.Paralisia:
                if (Random.value <= (car.valor2 > 0 ? car.valor2 : 0.35f))
                    caller.StartCoroutine(AplicarCC(ic, car.valor1 > 0 ? car.valor1 : 1f, "paralisia"));
                break;

            case CharacteristicType.Congelamento:
                caller.StartCoroutine(AplicarCC(ic, car.valor1 > 0 ? car.valor1 : 2f, "gelo"));
                break;

            case CharacteristicType.Fragilidade:
                caller.StartCoroutine(AplicarFragilidade(ic, car.valor1 > 0 ? car.valor1 : 3f,
                    car.valor2 > 0 ? car.valor2 : 0.25f));
                break;

            case CharacteristicType.Veneno:
                caller.StartCoroutine(AplicarDoT(ic,
                    car.valor1 > 0 ? car.valor1 : danoBase * 0.12f,
                    car.valor2 > 0 ? car.valor2 : 5f, 1f));
                break;

            case CharacteristicType.Enraizamento:
                caller.StartCoroutine(AplicarCC(ic, car.valor1 > 0 ? car.valor1 : 2.5f, "raiz"));
                break;

            case CharacteristicType.Maldicao:
                caller.StartCoroutine(AplicarMaldicao(ic, car.valor1 > 0 ? car.valor1 : 4f,
                    car.valor2 > 0 ? car.valor2 : 0.3f));
                break;

            case CharacteristicType.RouboVida:
                AplicarCura(danoBase * (car.valor1 > 0 ? car.valor1 : 0.2f));
                break;

            case CharacteristicType.Sagrado:
                break;

            case CharacteristicType.Cegamento:
                caller.StartCoroutine(AplicarCegamento(ic, car.valor1 > 0 ? car.valor1 : 3f,
                    car.valor2 > 0 ? car.valor2 : 0.4f));
                break;

            case CharacteristicType.Caos:
                break;

            case CharacteristicType.Infeccao:
                AplicarCadeia(alvo, car.valor1 > 0 ? car.valor1 : 3.5f,
                    danoBase * (car.valor2 > 0 ? car.valor2 : 0.6f), 3);
                break;
        }
    }

    static float ObterDuracaoVisual(ElementCharacteristic car)
    {
        switch (car.tipo)
        {
            case CharacteristicType.Queimadura: return (car.valor2 > 0 ? car.valor2 : 3f) * 1f;
            case CharacteristicType.Veneno:     return (car.valor2 > 0 ? car.valor2 : 5f) * 1f;
            case CharacteristicType.Lentidao:   return car.valor1 > 0 ? car.valor1 : 3f;
            case CharacteristicType.Atordoamento: return car.valor1 > 0 ? car.valor1 : 1.5f;
            case CharacteristicType.Congelamento: return car.valor1 > 0 ? car.valor1 : 2f;
            case CharacteristicType.Enraizamento: return car.valor1 > 0 ? car.valor1 : 2.5f;
            case CharacteristicType.Maldicao:   return car.valor1 > 0 ? car.valor1 : 4f;
            case CharacteristicType.Paralisia:  return car.valor1 > 0 ? car.valor1 : 1f;
            case CharacteristicType.Fragilidade: return car.valor1 > 0 ? car.valor1 : 3f;
            case CharacteristicType.Cegamento:  return car.valor1 > 0 ? car.valor1 : 3f;
            case CharacteristicType.RouboVida:  return 0.5f;
            case CharacteristicType.Cadeia:
            case CharacteristicType.Infeccao:   return 0.4f;
            default: return 0f;
        }
    }

    static IEnumerator AplicarDoT(InimigoController ic, float danoTick, float numTicks, float intervalo)
    {
        if (ic == null) yield break;
        for (int i = 0; i < (int)numTicks; i++)
        {
            yield return new WaitForSeconds(intervalo);
            if (ic == null || !ic.gameObject.activeInHierarchy) yield break;
            ic.ReceberDano(danoTick, false);
        }
    }

    static IEnumerator AplicarCC(InimigoController ic, float duracao, string tipo)
    {
        if (ic == null) yield break;
        var rb = ic.GetComponent<Rigidbody2D>();
        Vector2 velOriginal = Vector2.zero;
        if (rb != null) { velOriginal = rb.linearVelocity; rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }
        var sr = ic.GetComponent<SpriteRenderer>();
        Color corOriginal = Color.white;
        if (sr != null) { corOriginal = sr.color; sr.color = tipo == "gelo" ? new Color(0.6f, 0.85f, 1f) : new Color(0.8f, 0.8f, 0.8f); }
        yield return new WaitForSeconds(duracao);
        if (rb != null) { rb.bodyType = RigidbodyType2D.Dynamic; rb.linearVelocity = velOriginal; }
        if (sr != null) sr.color = corOriginal;
    }

    static IEnumerator AplicarSlow(InimigoController ic, float duracao, float fator)
    {
        if (ic == null) yield break;
        var movi = ic.GetComponent<movi_inimigo>();
        if (movi != null)
        {
            float orig = movi.velocidade; movi.velocidade *= fator;
            yield return new WaitForSeconds(duracao);
            if (movi != null) movi.velocidade = orig;
        }
        else yield return new WaitForSeconds(duracao);
    }

    static IEnumerator AplicarFragilidade(InimigoController ic, float duracao, float bonus)
    {
        if (ic == null) yield break;
        var marker = ic.gameObject.AddComponent<FragilidadeMarker>();
        marker.multiplicador = 1f + bonus;
        yield return new WaitForSeconds(duracao);
        if (marker != null) Object.Destroy(marker);
    }

    static IEnumerator AplicarMaldicao(InimigoController ic, float duracao, float reducao)
    {
        if (ic == null) yield break;
        var marker = ic.gameObject.AddComponent<MaldicaoMarker>();
        marker.reducaoDefesa = reducao;
        yield return new WaitForSeconds(duracao);
        if (marker != null) Object.Destroy(marker);
    }

    static IEnumerator AplicarCegamento(InimigoController ic, float duracao, float reducao)
    {
        if (ic == null) yield break;
        var marker = ic.gameObject.AddComponent<CegamentoMarker>();
        marker.reducaoDano = reducao;
        yield return new WaitForSeconds(duracao);
        if (marker != null) Object.Destroy(marker);
    }

    static void AplicarKnockback(GameObject alvo, float forca)
    {
        var rb = alvo.GetComponent<Rigidbody2D>();
        if (rb == null) return;
        var player = FindPlayer();
        if (player == null) return;
        rb.AddForce((alvo.transform.position - player.position).normalized * forca, ForceMode2D.Impulse);
    }

    static void AplicarAoE(Vector3 centro, float raio, float dano, GameObject alvoOriginal)
    {
        foreach (var hit in Physics2D.OverlapCircleAll(centro, raio))
        {
            if (hit.gameObject == alvoOriginal) continue;
            hit.GetComponent<InimigoController>()?.ReceberDano(dano, false);
        }
    }

    static void AplicarCadeia(GameObject alvoOriginal, float raio, float dano, int maxAlvos)
    {
        int count = 0;
        foreach (var hit in Physics2D.OverlapCircleAll(alvoOriginal.transform.position, raio))
        {
            if (count >= maxAlvos) break;
            if (hit.gameObject == alvoOriginal) continue;
            var ic = hit.GetComponent<InimigoController>();
            if (ic != null) { ic.ReceberDano(dano, false); count++; }
        }
    }

    static void AplicarCura(float quantidade)
    {
        var player = FindPlayer();
        if (player == null) return;
        var stats = player.GetComponent<PlayerStats>();
        if (stats == null) return;
        stats.health = Mathf.Min(stats.health + quantidade, stats.maxHealth);
    }

    static Transform _playerCache;
    static Transform FindPlayer()
    {
        if (_playerCache != null) return _playerCache;
        var go = GameObject.FindWithTag("Player");
        if (go != null) _playerCache = go.transform;
        return _playerCache;
    }

    public static float GetMultiplicadorCaos() => Random.Range(0.5f, 2.5f);

    public static float GetMultiplicadorSagrado(GameObject alvo)
    {
        if (alvo == null) return 1f;
        return alvo.GetComponent<BossController>() != null ? 1.5f : 1f;
    }
}

public class FragilidadeMarker : MonoBehaviour { public float multiplicador = 1.25f; }
public class MaldicaoMarker   : MonoBehaviour { public float reducaoDefesa = 0.3f; }
public class CegamentoMarker  : MonoBehaviour { public float reducaoDano   = 0.4f; }
