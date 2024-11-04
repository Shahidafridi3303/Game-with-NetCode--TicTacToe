using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class BoardManager : NetworkBehaviour
{
    Button[,] buttons = new Button[3, 3];
    public override void OnNetworkSpawn()
    {
        var cells = GetComponentsInChildren<Button>();
        int n = 0;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                buttons[i, j] = cells[n];
                n++;

                int r = i;
                int c = j;

                buttons[i, j].onClick.AddListener(delegate
                {
                    OnClickCell(r, c);
                });
            }
        }
    }


    [SerializeField] private Sprite xSprite, oSprite;
    private void OnClickCell(int r, int c)
    {
        // If button clicked by host, then change button sprite as X

        if (NetworkManager.Singleton.IsHost && GameManager.Instance.currentTurn.Value == 0)
        {
            buttons[r, c].GetComponent<Image>().sprite = xSprite;
            buttons[r, c].interactable = false;
            // Also change on Client side
            ChangeSpriteClientRpc(r, c);
            GameManager.Instance.currentTurn.Value = 1;
        }

        // If button is clicked by client, then change button sprite as O

        else if (!NetworkManager.Singleton.IsHost && GameManager.Instance.currentTurn.Value == 1)
        {
            buttons[r, c].GetComponent<Image>().sprite = oSprite;
            buttons[r, c].interactable = false;
            // Also change on host side
            ChangeSpriteServerRpc(r, c);
        }
    }

    [ClientRpc]
    private void ChangeSpriteClientRpc(int r, int c)
    {
        buttons[r, c].GetComponent<Image>().sprite = xSprite;
        buttons[r, c].interactable = false;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeSpriteServerRpc(int r, int c)
    {
        buttons[r, c].GetComponent<Image>().sprite = oSprite;
        buttons[r, c].interactable = false;
        GameManager.Instance.currentTurn.Value = 0;
    }
}
