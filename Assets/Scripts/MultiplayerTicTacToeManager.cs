using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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

    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private GameObject passwordEntryUI;
    [SerializeField] private GameObject leaveButton;

    private string serverPassword = "defaultPassword";

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
        // Set server password before starting the host
        serverPassword = passwordInputField.text;

        // Hook up password approval check
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
        NetworkManager.Singleton.StartHost();
    }

    public void StartClientGame()
    {
        // Set password ready to send to the server to validate
        NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(passwordInputField.text);
        NetworkManager.Singleton.StartClient();
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Decode the password sent by the client
        string password = Encoding.ASCII.GetString(request.Payload);

        // Check if the password matches the server's password
        bool approveConnection = password == serverPassword;

        // Set response values based on password check
        response.Approved = approveConnection;
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
