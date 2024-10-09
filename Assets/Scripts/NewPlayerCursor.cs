using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewPlayerCursor : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    [SerializeField] private Image cursorImage; // The UI Image component for the cursor
    [SerializeField] private TextMeshProUGUI playerNameText;
    private RectTransform cursorRectTransform;

    private void Start()
    {
        if (!photonView.IsMine)
        {
            // Disable other players' cursors from being controlled by this client
            photonView.RPC(nameof(SetCursorParentRPC), RpcTarget.AllBuffered);
            return;
        }

        transform.SetParent(NewGameManager.Instance.CanvasTransform, false);
        
        // Get the RectTransform for positioning on the canvas
        cursorRectTransform = GetComponent<RectTransform>();
    }
    [PunRPC]
    public void SetPlayerNameText(string name) {
        playerNameText.text = name;
    }

    private void Update()
    {
        Cursor.visible = false;

        // Update the cursor position for the local player based on the mouse position
        if (photonView.IsMine)
        {
            Vector2 mousePosition = Input.mousePosition;
            cursorRectTransform.position = mousePosition;
        }
    }

    public void SetColor(Color color)
    {
        cursorImage.color = color;
        photonView.RPC(nameof(RPC_SetColor), RpcTarget.AllBuffered, color.r, color.g, color.b, color.a);
    }

    [PunRPC]
    private void RPC_SetColor(float r, float g, float b, float a)
    {
        cursorImage.color = new Color(r, g, b, a);
        playerNameText.color = new Color(r, g, b, a);
    }

    [PunRPC]
    private void SetCursorParentRPC()
    {
        transform.SetParent(NewGameManager.Instance.CanvasTransform, false);
    }
    public void OnPhotonInstantiate(PhotonMessageInfo info) {
        photonView.RPC(nameof(SetPlayerNameText), RpcTarget.AllBuffered, photonView.Owner.NickName);
    }
}
