using UnityEngine;

public class RavenDeath : MonoBehaviour
{
    [Header("Physical parameters of the drop")]
    public float Gravity = -30f;          
    public float StartXVelocity = -2f;    
    public float DespawnY = -6f;          

    private bool isDead = false;
    private Vector2 velocity;
    private Animator anim;
    private Collider2D col;

    void Start()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }
    public void Die(float hitDirX = 0)
    {
        if (isDead) return;
        isDead = true;

        if (anim) anim.enabled = false;

        if (col) col.enabled = false;

        if (Mathf.Approximately(hitDirX, 0))
            velocity = new Vector2(StartXVelocity, 0);
        else
            velocity = new Vector2(Mathf.Sign(hitDirX) * Mathf.Abs(StartXVelocity), 0);
    }

    void Update()
    {
        if (!isDead) return;

        velocity.y += Gravity * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);

        if (transform.position.y <= DespawnY)
            Destroy(gameObject);
    }
}
