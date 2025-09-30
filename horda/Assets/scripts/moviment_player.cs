using UnityEngine;

public class moviment_player : MonoBehaviour
{
    Rigidbody2D rb;
    Vector2 Vector2;
    player_stats player_stats;
    
    void Start()
    {
        player_stats = GetComponent<player_stats>();
        rb = GetComponent<Rigidbody2D>();
    }

     
    void Update()
    {
        movi_inputs();
    }
    private void FixedUpdate()
    {
        movi_fisic();
    }

    void movi_inputs()
    {
        Vector2.y = Input.GetAxisRaw("Vertical");
        Vector2.x = Input.GetAxisRaw("Horizontal");
        Vector2 = Vector2.normalized;
    }
    void movi_fisic()
    {
        rb.linearVelocity = Vector2 * player_stats.speed * Time.fixedDeltaTime;
    }

}
