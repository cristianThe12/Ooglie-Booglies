using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OnigiriManager : MonoBehaviour
{
    
    private GameManager gameManager;
    private CameraScript camera;
    
    [Header("Components")]
    public SpriteRenderer blackBackground;

    public SpriteRenderer sliceSprite;

    private float timer = 0;

    private const float DISTANCE = 10f;

    private Player owner;
    private Vector3 targetPosition;

    private Vector3 sliceStart;
    private Vector3 sliceDirection;
    private Vector3 sliceEndposition;
    
    private float nativeWidth;   // sprite width in world units at scale 1
    private float maxScale;

    private bool active = false;

    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        camera = FindObjectOfType<CameraScript>();

        nativeWidth = sliceSprite.sprite.bounds.size.x;
        maxScale = DISTANCE / nativeWidth;   // makes the slice span the full dash

        blackBackground.enabled = false;
        sliceSprite.enabled = false;
    }

    void Update()
    {
        if (!active) return;
        
        if (owner == null) return;
        
        timer += Time.deltaTime;
        UpdateOpacities();

        if (sliceSprite.enabled)
        {
            Vector3 scale = sliceSprite.transform.localScale;
            scale.x = Mathf.Min(scale.x + Time.deltaTime * 100f, maxScale);
            sliceSprite.transform.localScale = scale;

            // keep the back edge pinned at sliceStart, so it grows outward
            float currentLength = scale.x * nativeWidth;
            sliceSprite.transform.position = sliceStart + sliceDirection * (currentLength / 2f);
        }
        
        owner.rb.linearVelocity = Vector2.zero;
        
        if (opacity <= 0) EndOnigiri();
    }

    public void StartOnigiri(ulong ownerId)
    {
        active = true;
        
        owner = gameManager.GetPlayerFromClientId(ownerId);
        
        owner.SetInteractableServerRpc(false);

        blackBackground.enabled = true;
        sliceSprite.gameObject.SetActive(true);
        sliceSprite.enabled = true;
        
        timer = 0;

        Vector3 startPos = owner.transform.position;
        Vector3 mousePos = owner.GetMousePosition();

        Vector3 direction = mousePos - startPos;
        direction.z = 0;
        direction.Normalize();
        
        sliceEndposition = startPos + direction * DISTANCE;

        owner.transform.position = sliceEndposition;

        sliceStart = startPos;
        sliceDirection = direction;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        sliceSprite.transform.rotation = Quaternion.Euler(0, 0, angle);
        sliceSprite.transform.position = startPos;
        sliceSprite.transform.localScale = new Vector3(0, sliceSprite.transform.localScale.y, 1);
        
        camera.ShakeCamera(0.75f, 0.1f);
        
        owner.PlaySoundServerRpc(GameManager.SoundType.onigiriSliceSound);
        
        DamagePlayersInSlash(startPos, direction, angle);
    }

    private void EndOnigiri()
    {
        active = false;
        blackBackground.enabled = false;
        sliceSprite.enabled = false;
        sliceSprite.gameObject.SetActive(false);

        owner.SetInteractableServerRpc(true);
        
        owner = null;
        
        Destroy(gameObject);
    }


    private const float HOLD_TIME = 0.5f;     // time at 100% opacity
    private const float FADE_DURATION = 0.4f; // smaller = faster fade
    private float opacity = 1f;

    private void UpdateOpacities()
    {
        opacity = 1f - (timer - HOLD_TIME) / FADE_DURATION;
        opacity = Mathf.Clamp01(opacity);

        blackBackground.color = new Color(blackBackground.color.r, blackBackground.color.g, blackBackground.color.b, opacity);
        sliceSprite.color = new Color(sliceSprite.color.r, sliceSprite.color.g, sliceSprite.color.b, opacity);
    }
    
    [Header("Damage")]
    public LayerMask playerLayer;
    public float slashWidth = 1.5f;
    
    private void DamagePlayersInSlash(Vector3 startPos, Vector3 direction, float angle)
    {
        // Only the server should apply damage
        if (!NetworkManager.Singleton.IsServer) return;

        Vector2 center = startPos + direction * (DISTANCE / 2f);
        Vector2 size = new Vector2(DISTANCE, slashWidth);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle, playerLayer);

        // A player can have multiple colliders, so track who was already hit
        HashSet<Player> alreadyHit = new HashSet<Player>();

        foreach (Collider2D hit in hits)
        {
            Player target = hit.GetComponentInParent<Player>();

            if (target == null || target == owner) continue;
            if (!alreadyHit.Add(target)) continue;
            
            float damage = owner.bulletDamage.Value * 5f * Mathf.Sqrt(owner.powers["Onigiri"]);

            target.Damage(damage, true, owner.OwnerClientId);   // use whatever your Player's damage method is called
        }
    }

    
}
