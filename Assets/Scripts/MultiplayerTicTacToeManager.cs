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
    [SerializeField] private TextMeshProUGUI Message;

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

    private void OnDestroy()
    {
        // Prevent error in the editor
        if (NetworkManager.Singleton == null) { return; }

        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
    }

    private async void Start()
    {
        // Setup client connection events for board generation
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientJoined;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
    }

    private void HandleServerStarted()
    {
        // Temporary workaround to treat host as client
        if (NetworkManager.Singleton.IsHost)
        {
            HandleClientConnected(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        // Are we the client that is connecting?
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Message.gameObject.SetActive(false);

            passwordEntryUI.SetActive(false);
            leaveButton.SetActive(true);
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        Message.gameObject.SetActive(true);
        Message.text = "You left, Rejoin using Code or create new";

        // Are we the client that is disconnecting?
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            passwordEntryUI.SetActive(true);
            leaveButton.SetActive(false);
        }
    }

    public void Leave()
    {
        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
        }

        // Shutdown the network manager to stop the host or client.
        NetworkManager.Singleton.Shutdown();

        // Re-enable UI elements for entering the game
        passwordEntryUI.SetActive(true);
        leaveButton.SetActive(false);
    }

    private void OnClientJoined(ulong clientId)
    {
        Debug.Log("Player " + clientId + " connected");

        Message.gameObject.SetActive(true);
        Message.text = "Waiting for the other player to join!";

        // Start game if two players are connected
        if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count == 2)
        {
            InitializeGameBoard();
        }
    }

    private void InitializeGameBoard()
    {
        Message.gameObject.SetActive(false);

        // Instantiate and spawn the board
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
