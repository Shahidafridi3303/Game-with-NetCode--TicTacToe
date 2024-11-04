using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public NetworkVariable<int> currentTurn = new NetworkVariable<int>(0);
    public static GameManager Instance;

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

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
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
                SpwanBoard();
            }
        };
    }

    [SerializeField] private GameObject boardPrefab;
    private GameObject newBoard;
    private void SpwanBoard()
    {
        newBoard = Instantiate(boardPrefab);
        newBoard.GetComponent<NetworkObject>().Spawn();
    }

    [SerializeField] private GameObject gameEndPanel;
    [SerializeField] private TextMeshProUGUI msgText;

    public void ShowMsg(string msg)
    {
        if (msg.Equals("won"))
        {
            msgText.text = "You Won";
            gameEndPanel.SetActive(true);
            // Show Panel with text that Opponent Won
            ShowOpponentMsg("You Lose");
        }
        else if (msg.Equals("draw"))
        {
            msgText.text = "Game Draw";
            gameEndPanel.SetActive(true);
            ShowOpponentMsg("Game Draw");
        }
    }


    private void ShowOpponentMsg(string msg)
    {
        if (IsHost)
        {
            // Then use ClientRpc to show Message at Client Side
            OpponentMsgClientRpc(msg);
        }
        else
        {
            // Use ServerRpc to show message at Server Side
            OpponentMsgServerRpc(msg);
        }
    }

    [ClientRpc]
    private void OpponentMsgClientRpc(string msg)
    {
        if (IsHost) return;
        msgText.text = msg;
        gameEndPanel.SetActive(true);
    }


    [ServerRpc(RequireOwnership = false)]
    private void OpponentMsgServerRpc(string msg)
    {
        msgText.text = msg;
        gameEndPanel.SetActive(true);
    }

    public void Restart()
    {
        // If this is client, then call SererRpc to destroy current board and create new board
        // If this is client then Client will also call ServerRpc to hide result panel on host side

        if (!IsHost)
        {
            RestartServerRpc();
            gameEndPanel.SetActive(false);
        }
        else
        {
            Destroy(newBoard);
            SpwanBoard();
            RestartClientRpc();
        }

        // Destroy the current Game Board
        // Spawn a new board
        // Hide the Result Panel
    }

    [ServerRpc(RequireOwnership = false)]
    private void RestartServerRpc()
    {
        Destroy(newBoard);
        SpwanBoard();
        gameEndPanel.SetActive(false);
    }


    [ClientRpc]
    private void RestartClientRpc()
    {
        gameEndPanel.SetActive(false);
    }
}
