using UnityEngine;

public class stats_inimigo : MonoBehaviour
{
    public float Speed;
    public int Hp_inimigo;
    public int dano;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        morte();
    }
    void morte()
    {
        if (Hp_inimigo <= 0)
        {
            Destroy(this.gameObject);
        }
    }
}
