using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class MultiplayerBoardManager : NetworkBehaviour
{
    [SerializeField] private Sprite playerXSprite, playerOSprite;
    Button[,] cellButtons = new Button[3, 3];
    public override void OnNetworkSpawn()
    {
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
        }

        // If button is clicked by client, then change button sprite as O

        else if (!NetworkManager.Singleton.IsHost && MultiplayerTicTacToeManager.Instance.activePlayerTurn.Value == 1)
        {
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
        cellButtons[r, c].GetComponent<Image>().sprite = playerXSprite;
        cellButtons[r, c].interactable = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SyncServerMarkServerRpc(int r, int c)
    {
        cellButtons[r, c].GetComponent<Image>().sprite = playerOSprite;
        cellButtons[r, c].interactable = false;
        MultiplayerTicTacToeManager.Instance.activePlayerTurn.Value = 0;
    }

    private void CheckGameStatus(int r, int c)
    {
        if (CheckVictoryCondition(r, c))
        {
            MultiplayerTicTacToeManager.Instance.DisplayResult("won");
        }
        else
        {
            if (CheckDrawCondition())
            {
                MultiplayerTicTacToeManager.Instance.DisplayResult("draw");
            }
        }
    }

    public bool CheckVictoryCondition(int r, int c)
    {
        Sprite clickedButtonSprite = cellButtons[r, c].GetComponent<Image>().sprite;
        // Checking Column
        if (cellButtons[0, c].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
            cellButtons[1, c].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
            cellButtons[2, c].GetComponentInChildren<Image>().sprite == clickedButtonSprite)
        {
            return true;
        }

        // Checking Row

        else if (cellButtons[r, 0].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
                 cellButtons[r, 1].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
                 cellButtons[r, 2].GetComponentInChildren<Image>().sprite == clickedButtonSprite)
        {
            return true;
        }

        // Checking First Diagonal

        else if (cellButtons[0, 0].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
                 cellButtons[1, 1].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
                 cellButtons[2, 2].GetComponentInChildren<Image>().sprite == clickedButtonSprite)
        {
            return true;
        }

        // Checking 2nd Diagonal
        else if (cellButtons[0, 2].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
                 cellButtons[1, 1].GetComponentInChildren<Image>().sprite == clickedButtonSprite &&
                 cellButtons[2, 0].GetComponentInChildren<Image>().sprite == clickedButtonSprite)
        {
            return true;
        }

        return false;
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
