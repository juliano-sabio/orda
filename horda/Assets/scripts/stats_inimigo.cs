using UnityEngine;

public class stats_inimigo : MonoBehaviour
{
    [SerializeField] private status_inimigo status;

    public float Speed;
    public int Hp_inimigo;
    public int dano;
    void Start()
    {
        Hp_inimigo = status.Hp_inimigo;
        Speed = status.Speed;
        dano = status.dano;
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
