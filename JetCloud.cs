using Unity.Netcode;
using UnityEngine;

public class JetCloud : NetworkBehaviour
{

    private const float duration = 0.75f;
    private float timer = 0;
    private Vector3 direction;
    private float speed;
    
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    public void Initialize(float speed, Vector3 direction)
    {
        this.speed = speed;
        this.direction = direction;
        
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        transform.localRotation = Quaternion.Euler(angle, 57.41f, 0);
    }

    // Update is called once per frame
    private void Update()
    {
        var speedScalar = 1 - timer / duration;
        
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, speedScalar);
        
        if (!IsServer) return;
        
        timer += Time.deltaTime;
        
        transform.position += direction * (speedScalar * speed * Time.deltaTime);
        
        if (timer >= duration)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
