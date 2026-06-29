using System.Collections;
using UnityEngine;

// A máscara no chão: cai com quique + leve rotação até assentar. Co-op: fica como marcador
// (destruída pelo MascaraCaido.Levantar). SP: some sozinha após ~0.8s.
public class MascaraChao : MonoBehaviour
{
    public static GameObject Criar(Vector3 posPlayer, SpriteRenderer refCorpo, bool persistente)
    {
        var sp = Resources.Load<Sprite>("ui/mascara_servo");
        if (sp == null) return null;
        var go = new GameObject("MascaraChao");
        go.transform.position = posPlayer + Vector3.up * 0.5f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        if (refCorpo != null) { sr.sortingLayerID = refCorpo.sortingLayerID; sr.sortingOrder = refCorpo.sortingOrder; }

        // Escala: o sprite recortado é pequeno (e o PPU difere do player), então ficava minúsculo.
        // Deixa a máscara ~55% da altura RENDERIZADA do player.
        if (refCorpo != null && refCorpo.sprite != null && sp.bounds.size.y > 0.001f)
        {
            float alturaPlayer = refCorpo.sprite.bounds.size.y * Mathf.Abs(refCorpo.transform.lossyScale.y);
            go.transform.localScale = Vector3.one * (alturaPlayer * 0.55f / sp.bounds.size.y);
        }
        go.AddComponent<MascaraChao>().StartCoroutine_Queda(posPlayer, persistente);
        return go;
    }

    void StartCoroutine_Queda(Vector3 posPlayer, bool persistente) => StartCoroutine(Queda(posPlayer, persistente));

    IEnumerator Queda(Vector3 posPlayer, bool persistente)
    {
        Vector3 chao = posPlayer + Vector3.down * 0.2f; // assenta um tico abaixo dos pés
        Vector3 ini  = transform.position;
        float dur = 0.45f;
        float ang = Random.Range(-25f, 25f);
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float p = t / dur;
            float y = Mathf.Lerp(ini.y, chao.y, 1f - Mathf.Pow(1f - p, 2f)); // ease-out (gravidade)
            float quique = Mathf.Sin(p * Mathf.PI) * 0.12f * (1f - p);
            transform.position = new Vector3(Mathf.Lerp(ini.x, chao.x, p), y + quique, ini.z);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(ang, ang * 0.3f, p));
            yield return null;
        }
        transform.position = chao;

        if (!persistente) // SP: some sozinha
        {
            yield return new WaitForSecondsRealtime(0.8f);
            var sr = GetComponent<SpriteRenderer>();
            for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
            {
                if (sr == null) yield break;
                var c = sr.color; c.a = Mathf.Lerp(1f, 0f, t / 0.4f); sr.color = c;
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
