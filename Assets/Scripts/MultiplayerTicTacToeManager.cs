using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class MultiplayerTicTacToeManager : NetworkBehaviour
{
    public NetworkVariable<int> activePlayerTurn = new NetworkVariable<int>(0);
    public static MultiplayerTicTacToeManager Instance;
    [SerializeField] private GameObject boardPrefab;
    private GameObject currentBoardInstance;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartHostGame()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClientGame()
    {
        NetworkManager.Singleton.StartClient();
    }

    private async void Start()
    {
        // Setup client connection events for board generation
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientJoined;
    }

    private void OnClientJoined(ulong clientId)
    {
        Debug.Log("Player " + clientId + " connected");

        // Start game if two players are connected
        if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count == 2)
        {
            InitializeGameBoard();
        }
    }

    private void InitializeGameBoard()
    {
        currentBoardInstance = Instantiate(boardPrefab);
        currentBoardInstance.GetComponent<NetworkObject>().Spawn();
    }

    public void DisplayResult(string result)
    {
        if (result == "victory")
        {
            resultText.text = "Congratulations! You Win!";
            resultPanel.SetActive(true);
            NotifyOpponent("You Lose");
        }
        else if (result == "tie")
        {
            resultText.text = "It's a Draw!";
            resultPanel.SetActive(true);
            NotifyOpponent("It's a Draw!");
        }
    }


    private void NotifyOpponent(string msg)
    {
        if (IsHost)
        {
            // Then use ClientRpc to show Message at Client Side
            NotifyClientResultClientRpc(msg);
        }
        else
        {
            // Use ServerRpc to show message at Server Side
            NotifyServerResultServerRpc(msg);
        }
    }

    [ClientRpc]
    private void NotifyClientResultClientRpc(string msg)
    {
        if (IsHost) return;
        resultText.text = msg;
        resultPanel.SetActive(true);
    }


    [ServerRpc(RequireOwnership = false)]
    private void NotifyServerResultServerRpc(string msg)
    {
        resultText.text = msg;
        resultPanel.SetActive(true);
    }

    public void RestartGame()
    {
        // If this is client, then call SererRpc to destroy current board and create new board
        // If this is client then Client will also call ServerRpc to hide result panel on host side

        if (!IsHost)
        {
            RestartServerRpc();
            resultPanel.SetActive(false);
        }
        else
        {
            Destroy(currentBoardInstance);
            InitializeGameBoard();
            RestartClientRpc();
        }

        // Destroy the current Game Board
        // Spawn a new board
        // Hide the Result Panel
    }

    [ServerRpc(RequireOwnership = false)]
    private void RestartServerRpc()
    {
        Destroy(currentBoardInstance);
        InitializeGameBoard();
        resultPanel.SetActive(false);
    }


    [ClientRpc]
    private void RestartClientRpc()
    {
        resultPanel.SetActive(false);
    }
}
