using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Steamworks;
using TMPro;

public class HealthBar : NetworkBehaviour
{
    public List<GameObject> objects;
    
    public SpriteRenderer BackBar;
    public SpriteRenderer FrontBar;
    public SpriteRenderer profilePicture;
    public TextMeshPro score;

    private SteamManager steamManager;
    private GameManager gameManager;

    private Player player;

    private float healthBarShakeTimer = 0;
    
    private Vector3 healthBarRestingPosition;

    public NetworkVariable<ulong> playerId = new NetworkVariable<ulong>();

    // Fired by Steam once a friend's avatar image finishes downloading/caching
    private Callback<AvatarImageLoaded_t> m_AvatarImageLoaded;

    void Awake()
    {
        steamManager = FindObjectOfType<SteamManager>();
        gameManager = FindObjectOfType<GameManager>();
    }

    void OnEnable()
    {
        if (SteamManager.Initialized)
        {
            m_AvatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
        }
    }

    void OnDisable()
    {
        m_AvatarImageLoaded?.Dispose();
        m_AvatarImageLoaded = null;
    }

    public override void OnNetworkSpawn()
    {
        playerId.OnValueChanged += OnPlayerIdChanged;

        TryFindPlayer();
    }

    private void OnPlayerIdChanged(ulong oldId, ulong newId)
    {
        TryFindPlayer();
    }

    private void TryFindPlayer()
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                playerId.Value, out NetworkObject playerObject))
            return;

        var foundPlayer = playerObject.GetComponent<Player>();

        if (foundPlayer == null)
            return;

        // Avoid double-subscribing if TryFindPlayer runs again for the same player
        if (player != null)
        {
            player.steamId.OnValueChanged -= OnPlayerSteamIdChanged;
        }

        player = foundPlayer;
        player.steamId.OnValueChanged += OnPlayerSteamIdChanged;

        BackBar.color = Color.black;
        FrontBar.color = player.playerColor.Value;
        score.color = player.playerColor.Value;

        float dy = (player.playerNumber.Value - 1) * 0.45f;
        transform.position = new Vector3(7f, 4.7f - dy, 1);

        if (player.steamId.Value != 0)
        {
            ApplyAvatar(player.steamId.Value);
        }
    }

    private void OnPlayerSteamIdChanged(ulong oldId, ulong newId)
    {
        ApplyAvatar(newId);
    }

    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        // Only care if this is the avatar for the player we're currently displaying
        if (player == null || player.steamId.Value == 0) return;
        if (callback.m_steamID.m_SteamID != player.steamId.Value) return;

        ApplyAvatar(player.steamId.Value);
    }

    private void ApplyAvatar(ulong steamId)
    {
        if (steamId == 0) return;

        Texture2D avatarTex = steamManager.GetFriendAvatar(new CSteamID(steamId));

        // avatarTex will be null if Steam hasn't finished loading this avatar yet.
        // OnAvatarImageLoaded will retry this once it's available.
        if (avatarTex != null)
        {
            profilePicture.sprite = Sprite.Create(
                avatarTex,
                new Rect(0, 0, avatarTex.width, avatarTex.height),
                new Vector2(0.5f, 0.5f),
                avatarTex.width); // pixelsPerUnit = width -> sprite is always 1x1 world units
        }
    }

    private void Update()
    {
        if (player == null)
        {
            TryFindPlayer();
            return;
        }

        var scale = player.health.Value / player.maxHealth.Value;

        FrontBar.transform.localScale = new Vector3(scale, 1, 1);
        FrontBar.transform.localPosition = new Vector3(-0.5f + scale / 2, 0, 1);
        
        score.text = player.playerScore.Value.ToString();
        
        UpdatePosition(scale);
        
        SetObjectsVisible(gameManager.gameState.Value is GameManager.GameState.Playing or GameManager.GameState.InLobby);

        if (gameManager.choosingPlayerClientId.Value == player.OwnerClientId)
        {
            SetObjectsVisible(true);
        }
    }

    private void UpdatePosition(float scale)
    {
        healthBarShakeTimer -= Time.deltaTime;

        FrontBar.transform.localPosition = new Vector3(-0.5f + scale / 2, 0, 1);
        BackBar.transform.localPosition = Vector3.zero;
    }

    private void SetObjectsVisible(bool visible)
    {
        foreach (var obj in objects)
        {
            obj.SetActive(visible);
        }
    }

    [ClientRpc]
    public void ShakeHealthBarClientRpc(float duration)
    {
        if (healthBarShakeTimer > duration) return;
        
        healthBarShakeTimer = duration;
    }

    public void SetPlayer(Player p)
    {
        if (player != null)
        {
            player.steamId.OnValueChanged -= OnPlayerSteamIdChanged;
        }

        player = p;
        player.steamId.OnValueChanged += OnPlayerSteamIdChanged;

        playerId.Value = p.NetworkObjectId;

        BackBar.color = Color.black;
        FrontBar.color = player.playerColor.Value;
        score.color = player.playerColor.Value;

        var dy = player.playerNumber.Value * 1f;

        transform.position = new Vector3(7f, 5.5f - dy, 1);

        if (player.steamId.Value != 0)
        {
            ApplyAvatar(player.steamId.Value);
        }
        
        healthBarRestingPosition = transform.position;
    }
    
    
}