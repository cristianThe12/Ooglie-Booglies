using Unity.Netcode;
using UnityEngine;

public class KongCollider : MonoBehaviour
{
    ulong ownerId;
    private Player owner;
    
    private GameManager gameManager;
    
    public KongGattlingManager gattlingManager;
    private float damageScale = 1;
    
    private bool IsLocalOwner =>
        Unity.Netcode.NetworkManager.Singleton != null &&
        Unity.Netcode.NetworkManager.Singleton.LocalClientId == ownerId;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }

    public void SetOwnerId(ulong id)
    {
        ownerId = id;

        owner = gameManager.GetPlayerFromClientId(id);
        
        transform.localScale = new Vector3(owner.width.Value * 2f, owner.height.Value * 2f, 1);
        
        damageScale = Mathf.Sqrt(owner.powers["KongOrgan"]);

        if (damageScale < 1) damageScale = 1;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (gattlingManager.state == KongGattlingManager.State.Jet) return;
        
        if (!IsLocalOwner) return;
        
        var o = other.gameObject;

        if (!o.CompareTag("Player")) return;
        
        var player = o.GetComponent<Player>();

        if (player == owner) return;
        
        player.DamageServerRpc(owner.bulletDamage.Value * Time.deltaTime * 3f * damageScale, false, ownerId);
    }
    
    
}
