using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class MultiplayerBoardManager : NetworkBehaviour
{
    [SerializeField] private Sprite playerXSprite, playerOSprite;
    Button[,] cellButtons = new Button[3, 3];          // 2D array cellButtons holds references to each button on the board.
    public override void OnNetworkSpawn()
    {
        //when the network initializes this object
        InitializeBoardCells(); 
    }

    private void InitializeBoardCells()
    {
        var buttons = GetComponentsInChildren<Button>();
        int buttonIndex = 0;
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                cellButtons[row, col] = buttons[buttonIndex++];
                int r = row, c = col;
                cellButtons[row, col].onClick.AddListener(() => ProcessCellClick(r, c));
            }
        }
    }

    private void ProcessCellClick(int r, int c)
    {
        // If button clicked by host, then change button sprite as X

        if (NetworkManager.Singleton.IsHost && MultiplayerTicTacToeManager.Instance.activePlayerTurn.Value == 0)
        {
            cellButtons[r, c].GetComponent<Image>().sprite = playerXSprite;
            cellButtons[r, c].interactable = false;
            // Also change on Client side
            SyncClientMarkClientRpc(r, c);
            CheckGameStatus(r, c);
            MultiplayerTicTacToeManager.Instance.activePlayerTurn.Value = 1;

            MultiplayerTicTacToeManager.Instance.TurnText.SetActive(false);
        }

        // If button is clicked by client, then change button sprite as O

        else if (!NetworkManager.Singleton.IsHost && MultiplayerTicTacToeManager.Instance.activePlayerTurn.Value == 1)
        {
            MultiplayerTicTacToeManager.Instance.TurnText.SetActive(true);

            cellButtons[r, c].GetComponent<Image>().sprite = playerOSprite;
            cellButtons[r, c].interactable = false;
            CheckGameStatus(r, c);
            // Also change on host side
            SyncServerMarkServerRpc(r, c);
        }
    }

    [ClientRpc]
    private void SyncClientMarkClientRpc(int r, int c)
    {
        MultiplayerTicTacToeManager.Instance.TurnText.SetActive(true);

        cellButtons[r, c].GetComponent<Image>().sprite = playerXSprite;
        cellButtons[r, c].interactable = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SyncServerMarkServerRpc(int r, int c)
    {
        MultiplayerTicTacToeManager.Instance.TurnText.SetActive(false);

        cellButtons[r, c].GetComponent<Image>().sprite = playerOSprite;
        cellButtons[r, c].interactable = false;
        MultiplayerTicTacToeManager.Instance.activePlayerTurn.Value = 0;
    }

    private void CheckGameStatus(int r, int c)
    {
        if (CheckVictoryCondition(r, c))
        {
            MultiplayerTicTacToeManager.Instance.DisplayResult("victory");
        }
        else
        {
            if (CheckDrawCondition())
            {
                MultiplayerTicTacToeManager.Instance.DisplayResult("tie");
            }
        }
    }

    private bool CheckVictoryCondition(int row, int col)
    {
        Sprite sprite = cellButtons[row, col].GetComponent<Image>().sprite;
        return (cellButtons[0, col].GetComponent<Image>().sprite == sprite &&
                cellButtons[1, col].GetComponent<Image>().sprite == sprite &&
                cellButtons[2, col].GetComponent<Image>().sprite == sprite) ||
               (cellButtons[row, 0].GetComponent<Image>().sprite == sprite &&
                cellButtons[row, 1].GetComponent<Image>().sprite == sprite &&
                cellButtons[row, 2].GetComponent<Image>().sprite == sprite) ||
               (cellButtons[0, 0].GetComponent<Image>().sprite == sprite &&
                cellButtons[1, 1].GetComponent<Image>().sprite == sprite &&
                cellButtons[2, 2].GetComponent<Image>().sprite == sprite) ||
               (cellButtons[0, 2].GetComponent<Image>().sprite == sprite &&
                cellButtons[1, 1].GetComponent<Image>().sprite == sprite &&
                cellButtons[2, 0].GetComponent<Image>().sprite == sprite);
    }

    private bool CheckDrawCondition()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (cellButtons[i, j].GetComponent<Image>().sprite != playerXSprite &&
                    cellButtons[i, j].GetComponent<Image>().sprite != playerOSprite)
                {
                    return false;
                }
            }
        }
        return true;
    }
}
