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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
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
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            Debug.Log("Client with id " + clientId + " joined");
            if (NetworkManager.Singleton.IsHost &&
                NetworkManager.Singleton.ConnectedClients.Count == 2)
            {
                InitializeGameBoard();
            }
        };
    }

    private void InitializeGameBoard()
    {
        currentBoardInstance = Instantiate(boardPrefab);
        currentBoardInstance.GetComponent<NetworkObject>().Spawn();
    }

    public void DisplayResult(string msg)
    {
        if (msg.Equals("won"))
        {
            resultText.text = "You Won";
            resultPanel.SetActive(true);
            // Show Panel with text that Opponent Won
            NotifyOpponent("You Lose");
        }
        else if (msg.Equals("draw"))
        {
            resultText.text = "Game Draw";
            resultPanel.SetActive(true);
            NotifyOpponent("Game Draw");
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
