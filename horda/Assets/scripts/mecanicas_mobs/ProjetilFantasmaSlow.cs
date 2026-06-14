using UnityEngine;

// Adicionado aos projéteis-fantasma (que se movem manualmente via transform.position,
// sem Rigidbody2D) para que a Ultimate "Domo Retardante" consiga detectá-los
// (via CircleCollider2D trigger) e reduzir sua velocidade enquanto estiverem dentro do domo.
public class ProjetilFantasmaSlow : MonoBehaviour
{
    public float fatorVelocidade = 1f;
}
