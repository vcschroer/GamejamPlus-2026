
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeathTracker : MonoBehaviour
{
    public static PlayerDeathTracker Instance { get; private set; }

    [Header("Game Over")]
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private int totalPlayers = 2;

    private readonly HashSet<PlayerMovement> deadPlayers =
        new HashSet<PlayerMovement>();

    private bool gameOverTriggered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterPlayerDeath(PlayerMovement player)
    {
        if (player == null || gameOverTriggered)
            return;

        deadPlayers.Add(player);

        Debug.Log(
            "Jogadores fantasmas: " +
            deadPlayers.Count + "/" + totalPlayers);

        if (deadPlayers.Count >= totalPlayers)
            TriggerGameOver();
    }

    public void RegisterPlayerRevive(PlayerMovement player)
    {
        if (player == null || gameOverTriggered)
            return;

        deadPlayers.Remove(player);

        Debug.Log(
            "Jogador revivido. Fantasmas restantes: " +
            deadPlayers.Count);
    }

    public void RegisterGhostTimeExpired(PlayerMovement player)
    {
        if (player == null || gameOverTriggered)
            return;

        if (player.IsDead)
            TriggerGameOver();
    }

    private void TriggerGameOver()
    {
        if (gameOverTriggered)
            return;

        gameOverTriggered = true;

        if (pauseMenu == null)
            pauseMenu = FindFirstObjectByType<PauseMenu>();

        if (pauseMenu != null)
        {
            pauseMenu.StartGameOver();
        }
        else
        {
            Debug.LogError(
                "PlayerDeathTracker não encontrou o PauseMenu!");
        }
    }
}
