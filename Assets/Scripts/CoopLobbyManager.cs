using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputManager))]
public class CoopLobbyManager : MonoBehaviour
{
    [Header("Pontos de Spawn")]
    [SerializeField] private Transform player1Spawn;
    [SerializeField] private Transform player2Spawn;

    private PlayerInputManager inputManager;
    private List<PlayerInput> joinedPlayers = new List<PlayerInput>();

    private void Awake()
    {
        inputManager = GetComponent<PlayerInputManager>();
    }

    private void OnEnable()
    {
        if (inputManager != null)
        {
            inputManager.onPlayerJoined += OnPlayerJoined;
            inputManager.onPlayerLeft += OnPlayerLeft;
        }
    }

    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.onPlayerJoined -= OnPlayerJoined;
            inputManager.onPlayerLeft -= OnPlayerLeft;
        }
    }

    private void OnPlayerJoined(PlayerInput playerInput)
    {
        joinedPlayers.Add(playerInput);
        int playerIndex = joinedPlayers.Count;

        Debug.Log($"Player {playerIndex} entrou usando o dispositivo: {playerInput.devices[0].displayName}");

        if (playerIndex == 1 && player1Spawn != null)
        {
            playerInput.transform.position = player1Spawn.position;
        }
        else if (playerIndex == 2 && player2Spawn != null)
        {
            playerInput.transform.position = player2Spawn.position;
        }

        if (joinedPlayers.Count >= 2)
        {
            inputManager.DisableJoining();
        }
    }

    private void OnPlayerLeft(PlayerInput playerInput)
    {
        if (joinedPlayers.Contains(playerInput))
        {
            joinedPlayers.Remove(playerInput);
            inputManager.EnableJoining();
        }
    }
}