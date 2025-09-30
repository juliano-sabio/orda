using Unity.VisualScripting;
using UnityEngine;

public class player_stats : MonoBehaviour
{
    public float speed;
    public float Hp;


    private void Update()
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
