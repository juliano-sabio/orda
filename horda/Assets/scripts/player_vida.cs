using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int vidaMaxima = 100;
    private int vidaAtual;

    void Start()
    {
        vidaAtual = vidaMaxima;
    }

    public void ReceberDano(int quantidade)
    {
        vidaAtual -= quantidade;
        Debug.Log("Player levou dano! Vida atual: " + vidaAtual);

        if (vidaAtual <= 0)
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
