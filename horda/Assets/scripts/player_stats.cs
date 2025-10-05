using Unity.VisualScripting;
using UnityEngine;

public class player_stats : MonoBehaviour
{
    public float speed;
    public int Hp_max;
    public int Hp_atual;
    private void Start()
    {
        Hp_atual = Hp_max;
    }
    private void Update()
    {
        
    }


    public void ReceberDano(int quantidade)
    {
        Hp_atual -= quantidade;
        Debug.Log("Player levou dano! Vida atual: " + Hp_atual);

        if (Hp_max <= 0)
        {
            Morrer();
        }
    }

    void Morrer()
    {
        Debug.Log("Player morreu!");
        // Aqui você pode colocar animação de morte, game over, etc.
    }
}
