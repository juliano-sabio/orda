using System.Collections;
using UnityEngine;

public class ZonaGeloController : MonoBehaviour
{
    PlayerStats player;
    Vector2     centro;
    float       raioGelo, duracaoGelo, danoGelo, fatorLentidao;

    public void Iniciar(PlayerStats ps, Vector2 c, float raio, float dur, float dano, float fator)
    {
        player        = ps;
        centro        = c;
        raioGelo      = raio;
        duracaoGelo   = dur;
        danoGelo      = dano;
        fatorLentidao = fator;
        StartCoroutine(Ciclo());
    }

    IEnumerator Ciclo()
    {
        float elapsed      = 0f;
        float proxDano     = 0f;
        bool  playerNaZona = false;
        var   playerSr     = player != null ? player.GetComponent<SpriteRenderer>() : null;
        Color corOrig      = playerSr != null ? playerSr.color : Color.white;

        while (elapsed < duracaoGelo)
        {
            elapsed  += Time.deltaTime;
            proxDano -= Time.deltaTime;

            bool dentro = player != null &&
                          Vector2.Distance(player.transform.position, centro) <= raioGelo;

            if (dentro && !playerNaZona)
            {
                playerNaZona = true;
                if (player != null) player.AplicarSlow(1f - fatorLentidao, duracaoGelo);
                if (playerSr != null) playerSr.color = new Color(0.6f, 0.87f, 1f);
            }
            else if (!dentro && playerNaZona)
            {
                playerNaZona = false;
                if (player != null) player.CancelarSlow();
                if (playerSr != null) playerSr.color = corOrig;
            }

            if (dentro && proxDano <= 0f && player != null)
            {
                player.TakeDamage(danoGelo);
                proxDano = 1f;
            }

            yield return null;
        }

        if (playerNaZona)
        {
            if (player != null) player.CancelarSlow();
            if (playerSr != null) playerSr.color = corOrig;
        }

        Destroy(gameObject);
    }
}
