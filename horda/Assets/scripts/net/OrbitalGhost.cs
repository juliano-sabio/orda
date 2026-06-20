using UnityEngine;

// Co-op: fantasma visual de um projétil ORBITAL no cliente do colega. Orbita o MESMO
// player sincronizado com o mesmo ângulo/raio/velocidade (cálculo idêntico ao
// OrbitingProjectileSkillBehavior) e some após o mesmo número de voltas. Sem dano
// (o ProjectileController2D do fantasma fica com cosmetico=true).
public class OrbitalGhost : MonoBehaviour
{
    Transform player;
    float angle, radius, speed, maxRot, totalRot;

    public void Init(Transform player, float startAngle, float radius, float speed, int numberOfOrbits)
    {
        this.player = player;
        this.angle = startAngle;
        this.radius = radius;
        this.speed = speed;
        this.maxRot = Mathf.Max(1, numberOfOrbits) * 360f;
        Posicionar();
        Destroy(gameObject, 12f); // segurança (caso o player suma)
    }

    void Update()
    {
        if (player == null) { Destroy(gameObject); return; }

        float d = speed * Time.deltaTime;
        angle += d;
        totalRot += d;
        if (angle >= 360f) angle -= 360f;

        Posicionar();

        if (totalRot >= maxRot) Destroy(gameObject);
    }

    void Posicionar()
    {
        float r = angle * Mathf.Deg2Rad;
        Vector2 centro = (Vector2)player.position;
        Vector2 pos = centro + new Vector2(Mathf.Cos(r) * radius, Mathf.Sin(r) * radius);
        transform.position = pos;

        Vector2 paraCentro = (centro - pos).normalized;
        float rot = Mathf.Atan2(paraCentro.y, paraCentro.x) * Mathf.Rad2Deg + 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, rot);
    }
}
