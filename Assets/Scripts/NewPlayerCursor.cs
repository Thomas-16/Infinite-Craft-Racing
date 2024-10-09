using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class NewPlayerCursor : MonoBehaviourPun
{
    [SerializeField] private Image cursorImage; // The UI Image component for the cursor
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

    private void Update()
    {
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
    }

    [PunRPC]
    private void SetCursorParentRPC()
    {
        transform.SetParent(NewGameManager.Instance.CanvasTransform, false);
    }
}
