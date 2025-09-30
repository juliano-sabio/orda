
using Unity.VisualScripting;
using Unity.Collections;
using UnityEngine;

public class movi_inimigo : MonoBehaviour
{
    private Transform player_position;
    stats_iniimigo stats_Iniimigo;
   
    
    void Start()
    {
        stats_Iniimigo = GetComponent<stats_iniimigo>();
        player_position = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        movimento_seguir();
    }
    private void movimento_seguir()
    {
        if (player_position != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, player_position.position, stats_Iniimigo.speed * Time.deltaTime);
        }
    }
}
