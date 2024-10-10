using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private Transform elementsParent;
    [SerializeField] private GameObject elementPrefab;
    [SerializeField] private GameObject cursorPrefab;
    [SerializeField] private GameObject cursorCanvasPrefab;


    private string pendingNewElementName;

    private void Start() {
        PhotonNetwork.AutomaticallySyncScene = true;

    }
    [PunRPC] 
    public void RPC_SetPendingNewElementName(string pendingNewElementName) {
        this.pendingNewElementName = pendingNewElementName;
    }
    public Element SpawnElement(string elementName, Vector3 pos) {
        photonView.RPC(nameof(RPC_SetPendingNewElementName), RpcTarget.All, elementName);
        GameObject elementGO = PhotonNetwork.Instantiate(elementPrefab.name, pos, Quaternion.identity);

        return elementGO.GetComponent<Element>();
    }
    public void SetupElement(GameObject elementGO) {
        elementGO.transform.SetParent(elementsParent, true);
        elementGO.transform.SetAsLastSibling();
        Element element = elementGO.GetComponent<Element>();
        element.SetElementName(pendingNewElementName);
        elementGO.transform.localScale = Vector3.one;
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        if (PlayerCursor.LocalCursorInstance == null) {
            Debug.LogFormat("We are Instantiating cursor from {0}", SceneManagerHelper.ActiveSceneName);
            GameObject cursorCanvas = GameObject.Instantiate(cursorCanvasPrefab);
            GameObject cursorGO = PhotonNetwork.Instantiate(cursorPrefab.name, Vector3.zero, Quaternion.identity);
            cursorGO.transform.parent = cursorCanvas.transform;
            DontDestroyOnLoad(cursorCanvas);
            Debug.Log("cursor spawned");
        }
    }
    public override void OnPlayerEnteredRoom(Player other) {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName); // not seen if you're the player connecting


        //GameObject cursorGO = PhotonNetwork.Instantiate(cursorPrefab.name, Vector3.zero, Quaternion.identity);
        //cursorGO.GetComponent<PhotonView>().TransferOwnership(other);

        if (PhotonNetwork.IsMasterClient) {
            Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom

            LoadArena();
        }
        Debug.Log(PhotonNetwork.CurrentRoom.PlayerCount + " players are in the room");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        //PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);
    }

    public override void OnPlayerLeftRoom(Player other) {
        Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName); // seen when other disconnects
        PhotonNetwork.DestroyPlayerObjects(other);

        if (PhotonNetwork.IsMasterClient) {
            Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom

            LoadArena();
        }
    }

    private void LoadArena() {
        if (!PhotonNetwork.IsMasterClient) {
            Debug.LogError("PhotonNetwork : Trying to Load a level but we are not the master Client");
            return;
        }
        Debug.LogFormat("PhotonNetwork : Loading Level : {0}", PhotonNetwork.CurrentRoom.PlayerCount);
        PhotonNetwork.LoadLevel("GameScene");
    }
    public override void OnLeftRoom() {
        SceneManager.LoadScene(0);
    }
    public void LeaveRoom() {
        PhotonNetwork.LeaveRoom();
    }
}
