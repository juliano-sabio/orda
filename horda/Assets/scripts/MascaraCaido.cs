using System.Collections;
using UnityEngine;

// Animação de "morte/caído": o corpo do personagem some (fade) e a máscara cai no chão.
// Co-op: máscara persiste (marcador de revive). SP: animação de morte (máscara some sozinha).
// Anexado em runtime (moviment_player2.Start) → cobre SP e co-op. Dirigido pelo estado caído,
// que em co-op é sincronizado (PlayerNet.downed) → roda em todas as cópias sem RPC.
public class MascaraCaido : MonoBehaviour
{
    SpriteRenderer corpo;
    GameObject mascaraAtual;
    Coroutine anim;

    void Awake() => corpo = GetComponentInChildren<SpriteRenderer>();

    public void Cair(bool persistente)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(RotinaCair(persistente));
    }

    public void Levantar()
    {
        if (anim != null) StopCoroutine(anim);
        if (mascaraAtual != null) { Destroy(mascaraAtual); mascaraAtual = null; }
        anim = StartCoroutine(RotinaLevantar());
    }

    IEnumerator RotinaCair(bool persistente)
    {
        if (corpo != null)
        {
            Color c0 = corpo.color;
            for (float t = 0f; t < 0.16f; t += Time.unscaledDeltaTime)
            {
                corpo.color = new Color(c0.r, c0.g, c0.b, Mathf.Lerp(c0.a, 0f, t / 0.16f));
                yield return null;
            }
            corpo.enabled = false;
            corpo.color = c0; // restaura a cor pra reaparecer no revive
        }
        mascaraAtual = MascaraChao.Criar(transform.position, corpo, persistente);
        anim = null;
    }

    IEnumerator RotinaLevantar()
    {
        if (corpo != null)
        {
            corpo.enabled = true;
            Color c0 = corpo.color;
            for (float t = 0f; t < 0.2f; t += Time.unscaledDeltaTime)
            {
                corpo.color = new Color(c0.r, c0.g, c0.b, Mathf.Lerp(0f, 1f, t / 0.2f));
                yield return null;
            }
            corpo.color = c0;
        }
        anim = null;
    }
}
