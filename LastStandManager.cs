using UnityEngine;
using Unity.Netcode;

public class LastStandManager : MonoBehaviour
{

    private CameraScript camera;
    private GameManager gameManager;
    
    public bool active = false;

    private float timer = 0;

    private Player owner;
    private ulong ownerId;
    
    private bool IsLocalOwner =>
        Unity.Netcode.NetworkManager.Singleton != null &&
        Unity.Netcode.NetworkManager.Singleton.LocalClientId == ownerId;

    enum State
    {
        deadEyes,
        middle,
        sayan
    }
    private State state = State.deadEyes;

    void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        camera = FindFirstObjectByType<CameraScript>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!active) return;
        
        timer += Time.deltaTime;

        if (owner == null) return;

        if (gameManager.amountOfAlivePlayers.Value <= 2 || IsLocalOwner)
        {
            camera.SmoothMoveTo(owner.transform.position, 20f);
            camera.SmoothSetZoom(2f, 10f);
        }

        if (timer >= 1f && state == State.deadEyes)
        {
            StopDeadEyes();
        }

        if (timer > 1.7f && state == State.middle)
        {
            startSayan();
        }

        if (timer > 4f)
        {
            StopAbility();
        }
    }

    public void StartAbility(ulong ownerId)
    {
        active = true;
        
        this.ownerId = ownerId;
        
        owner = gameManager.GetPlayerFromClientId(ownerId);
        
        if (owner == null) Debug.LogError("Owner not found");

        owner.SetInteractableServerRpc(false);
        
        camera.StopShaking();
        
        owner.deadEyesSprite.SetActive(true);
        state = State.deadEyes;
    }

    private void StopDeadEyes()
    {
        state = State.middle;
        owner.deadEyesSprite.SetActive(false);
        owner.PlaySoundServerRpc(GameManager.SoundType.lastStandStopEyesSound);
        
        camera.ShakeCamera(0.5f, 0.1f);
    }

    private void startSayan()
    {
        state = State.sayan;
        owner.sayanSprite.SetActive(true);
        
        camera.ShakeCamera(2f, 0.4f);
        
        owner.PlaySoundServerRpc(GameManager.SoundType.lastStandSayanSound);
    }

    private void StopAbility()
    {
        active = false;
        owner.SetInteractableServerRpc(true);
        owner.deadEyesSprite.SetActive(false);
        owner.sayanSprite.SetActive(false);
        timer = 0;
        camera.MoveTo(new Vector2(0, 0));
        camera.SetZoom(5);
        
        owner = null;
        
        Destroy(gameObject);
    }
    
    
}
