using UnityEngine;

public class KongFist : MonoBehaviour
{

    private Player owner;
    private ulong ownerId;
    private GameManager gameManager;
    private SpriteRenderer spriteRenderer;

    private const float duration = 0.1f;
    private float timer = 0;
    private Vector3 direction;
    private const float speed = 5f;
    private const float paddingStart = 0.3f;
    
    private bool IsLocalOwner =>
        Unity.Netcode.NetworkManager.Singleton != null &&
        Unity.Netcode.NetworkManager.Singleton.LocalClientId == ownerId;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    
    public void Initialize(Vector3 direction, ulong ownerId)
    {
        this.direction = direction;
        
        this.ownerId = ownerId;
        owner = gameManager.GetPlayerFromClientId(ownerId);
        
        var distanceFromPlayer = Mathf.Sqrt(owner.width.Value * owner.width.Value + owner.height.Value * owner.height.Value);
        
        var randomX = Random.Range(-owner.width.Value, owner.width.Value);
        var randomY = Random.Range(-owner.height.Value, owner.height.Value);
        
        transform.position = owner.transform.position + direction * distanceFromPlayer + new Vector3(randomX, randomY, 0);
        
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        
        transform.localRotation = Quaternion.Euler(0, 0, angle);
    }

    // Update is called once per frame
    private void Update()
    {
        var speedScalar = 1 - timer / duration;
        
        transform.position += direction * (speed * Time.deltaTime * speedScalar);
        spriteRenderer.color = new Color(spriteRenderer.color.r, spriteRenderer.color.g, spriteRenderer.color.b, speedScalar);
        
        timer += Time.deltaTime;
        
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}
