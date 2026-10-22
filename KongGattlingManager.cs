using UnityEngine;

public class KongGattlingManager : MonoBehaviour
{

    private GameManager gameManager;

    [Header("Components")]
    private AudioSource audioSource;

    public AudioClip jetSoundClip;
    public AudioClip gatlingSoundClip;

    public GameObject kongPunchPrefab;

    public GameObject collider;

    [Header("Constants")]
    private const float jetSpeed = 7.5f;
    private const float jetDuration = 2.5f;

    private const float gatlingDuration = 3f;
    
    private const float fistCooldown = 0.05f;

    private float ownerRadius =>
        Mathf.Sqrt(owner.width.Value * owner.width.Value + owner.height.Value * owner.height.Value);

    [Header("Variables")]
    private float timer = 0;

    private float fistTimer = 0;
    
    private ulong ownerId;
    private Player owner;

    private Vector3 ownerMousePosition;
    
    private Vector3 gatlingPosition;
    
    private Vector3 direction;

    public enum State
    {
        Jet,
        Gatling
    }

    public State state = State.Jet;
    
    private bool IsLocalOwner =>
        Unity.Netcode.NetworkManager.Singleton != null &&
        Unity.Netcode.NetworkManager.Singleton.LocalClientId == ownerId;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        audioSource = GetComponent<AudioSource>();
    }
    
    public void StartAbility(ulong ownerId)
    {
        this.ownerId = ownerId;
        owner = gameManager.GetPlayerFromClientId(ownerId);
        
        state = State.Jet;
        timer = 0;
        
        owner.SetWeightlessServerRpc(true);
        owner.SetVulnerableServerRpc(true);

        audioSource.clip = jetSoundClip;
        
        collider.GetComponent<KongCollider>().SetOwnerId(ownerId);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (owner == null) return;
        
        transform.position = owner.transform.position;
        ownerMousePosition = owner.GetMousePosition();

        ownerMousePosition.z = 0;
        
        direction = (ownerMousePosition - owner.transform.position).normalized;

        direction.z = 0;

        if (state == State.Jet)
        {
            if (IsLocalOwner) owner.transform.position += direction * (jetSpeed * Time.deltaTime);
            
            if (!audioSource.isPlaying)
            {
                audioSource.Play();

                if (IsLocalOwner)
                {
                    gameManager.complexPowerManager.CreateJetCloudServerRpc(owner.transform.position, 5f,  -direction);
                }
            }
            
            if (timer >= jetDuration)
            {
                StartGatling();
            }
        }
        
        else
        {
            if (IsLocalOwner) owner.transform.position = gatlingPosition;

            collider.transform.position = transform.position + ownerRadius * direction;
                
            fistTimer += Time.deltaTime;

            if (fistTimer >= fistCooldown)
            {
                var kongPunch = Instantiate(kongPunchPrefab, gatlingPosition, Quaternion.identity).GetComponent<KongFist>();
                    
                kongPunch.Initialize(direction, ownerId);
                    
                fistTimer = 0;
            }
        }
            
        if (timer >= gatlingDuration)
        {
            Stop();
        }
    }

    private void StartGatling()
    {
        state = State.Gatling;
        gatlingPosition = owner.transform.position;
        timer = 0;
        
        audioSource.clip = gatlingSoundClip;
        audioSource.Play();
    }

    private void Stop()
    {
        owner.SetWeightlessServerRpc(false);
        owner.SetVulnerableServerRpc(false);
        
        audioSource.Stop();
        
        Destroy(gameObject);
    }
}
