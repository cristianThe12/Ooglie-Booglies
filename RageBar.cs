using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Random = System.Random;

public class RageBar : MonoBehaviour
{
    
    public List<GameObject> objects;
    
    GameManager gameManager;
    
    public SpriteRenderer BackBar;
    public SpriteRenderer FrontBar;
    public TextMeshPro Text;

    private Player player;

    private Vector3 restingPosition;

    private bool rageIsMax => player.rage.Value >= player.rageMax.Value;

    private void Awake()
    {
        gameManager = FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        var maxRage = player.rageMax.Value;
        var scale = maxRage > 0f ? Mathf.Clamp01(player.rage.Value / maxRage) : 0f;

        FrontBar.transform.localScale = new Vector3(scale, FrontBar.transform.localScale.y, 1);
        FrontBar.transform.localPosition = new Vector3(-0.5f + scale / 2, 0, 1);

        Text.text = rageIsMax ? "R" : "";
        
        UpdateBarPosition();

        if (gameManager.gameState.Value == GameManager.GameState.Menu)
        {
            Destroy(gameObject);
        }

        SetObjectsActive(gameManager.gameState.Value == GameManager.GameState.Playing);
    }

    private const float ShakeCoefficient = 0.01f;
    private void UpdateBarPosition()
    {
        transform.position = restingPosition;

        if (!rageIsMax) return;
        
        var dx = UnityEngine.Random.Range(-ShakeCoefficient, ShakeCoefficient);
        var dy = UnityEngine.Random.Range(-ShakeCoefficient, ShakeCoefficient);
        
        transform.Translate(dx, dy, 0);
    }

    public void SetPlayer(Player p)
    {
        player = p;

        BackBar.color = Color.black;
        FrontBar.color = player.playerColor.Value;
        Text.color = player.playerColor.Value;
        
        var dy = (player.playerNumber.Value - 1) * 0.3f;
        
        transform.position = new Vector3(-4f, 4.7f - dy, 1);
        
        restingPosition = transform.position;
    }

    private void SetObjectsActive(bool active)
    {
        foreach (var obj in objects)
        {
            obj.SetActive(active);
        }
    }
}
