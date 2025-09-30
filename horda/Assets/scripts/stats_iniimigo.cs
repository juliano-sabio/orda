using UnityEngine;

public class stats_iniimigo : MonoBehaviour
{
    public float speed;
    public int Hp;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        death();
    }
    void death()
    {
        if (Hp <= 0)
        {
            Destroy(this.gameObject);
        }
    }
}
