using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCursor : MonoBehaviour
{
    public static GameObject LocalCursorInstance;
    private PhotonView photonView;

    private Color[] colours = {
        Color.yellow,
        Color.magenta,
        Color.blue,
        Color.red,
        Color.green,
        Color.black,
        Color.gray,
        Color.cyan
    };

    private void Awake() {
        photonView = GetComponent<PhotonView>();
        if (photonView == null) {
            Debug.LogError($"Can't find photon view for {this.gameObject.name}!");
            return;
        }
        if (photonView.IsMine) {
            LocalCursorInstance = gameObject;
        }
    }

    private void Start() {
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber % colours.Length;
        photonView.RPC(nameof(RPC_AssignCursorColor), RpcTarget.All, colours[playerIndex].r, colours[playerIndex].g, colours[playerIndex].b);
    }

    void Update()
    {
        Cursor.visible = false;
        if (photonView.IsMine) {
            transform.localPosition = NormalizeMousePositionToCanvas();
        }
    }

    /*public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext(transform.GetChild(0).localPosition);
        }
        else {
            transform.GetChild(0).localPosition = (Vector3)stream.ReceiveNext();
        }
    }*/

    [PunRPC]
    public void RPC_AssignCursorColor(int r, int g, int b) {
        GetComponentInChildren<Image>().color = new Color(r,g,b);
    }

    private Vector3 NormalizeMousePositionToCanvas() {
        // Get the current mouse position in screen space (0, 0 at bottom left, Screen.width, Screen.height at top right)
        Vector3 mousePosition = Input.mousePosition;

        // Screen resolution width and height (substitute with local screen size if needed)
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        // Canvas size in local space (half-width = 960, half-height = 540 for 1920x1080 canvas)
        float canvasHalfWidth = 960f;
        float canvasHalfHeight = 540f;

        // Normalize the mouse position to range from -1 to 1, with 0,0 being at the center
        float normalizedX = (mousePosition.x / screenWidth) * 2f - 1f;
        float normalizedY = (mousePosition.y / screenHeight) * 2f - 1f;

        // Convert the normalized values to the canvas space, with 960,540 being the top-right and -960,-540 being bottom-left
        float canvasX = normalizedX * canvasHalfWidth;
        float canvasY = normalizedY * canvasHalfHeight;

        // Return the canvas space Vector3 (Z is 0 since it's UI)
        return new Vector3(canvasX, canvasY, 0);
    }
}

