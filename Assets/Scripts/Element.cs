using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Shapes2D;
using LLMUnity;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Photon.Pun;

[RequireComponent(typeof(PhotonView))]
public class Element : MonoBehaviourPun, IDragHandler, IBeginDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPunObservable, IPunInstantiateMagicCallback
{
    [field: SerializeField]
    private string elementName;

    [SerializeField] private LLMCharacter llmCharacter;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private bool isDragging = false;
    [SerializeField] private Color unselectedColor1;
    [SerializeField] private Color unselectedColor2;
    [SerializeField] private Color selectedColor1;
    [SerializeField] private Color selectedColor2;

    private Shape shape;
    private Vector3 offset;
    private string latestReply;
    private GameManager gameManager;
    private SFXManager sfxManager;

    private void Awake() {
        text = GetComponentInChildren<TextMeshProUGUI>();
        shape = GetComponent<Shape>();
        llmCharacter = FindObjectOfType<LLMCharacter>();
        gameManager = FindObjectOfType<GameManager>();
        sfxManager = FindObjectOfType<SFXManager>();
        text.text = elementName;
        name = elementName;
        shape.settings.fillColor = unselectedColor1;
        shape.settings.fillColor2 = unselectedColor2;
    }
    private void Start() {
        PhotonNetwork.SendRate = 60;
    }
    private void Update() {
        if(isDragging) {
            shape.settings.fillColor = selectedColor1;
            shape.settings.fillColor2 = selectedColor2;
        } else {
            shape.settings.fillColor = unselectedColor1;
            shape.settings.fillColor2 = unselectedColor2;
        }
    }
    public void OnBeginDrag(PointerEventData eventData) {
        if(elementName == "..." || isDragging) {
            return;
        }
        // Request ownership when the drag starts if this PhotonView doesn't belong to the current player
        if (!photonView.IsMine) {
            photonView.RequestOwnership();
        }

        // Calculate the offset between the object's position and the mouse position
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 globalMousePos
        );
        offset = transform.position - globalMousePos;
    }

    public void OnDrag(PointerEventData eventData) {
        if (elementName == "...") {
            return;
        }
        if (!photonView.IsMine) {
            return;
        }

        // Move the object while maintaining the original offset
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            GetComponent<RectTransform>(),
            eventData.position,
            eventData.pressEventCamera,
            out Vector3 globalMousePos
        );
        Vector3 targetPos = globalMousePos + offset;

        transform.position = targetPos;
    }

    // Detect if we dropped onto another Element
    private void DetectDropTarget(PointerEventData eventData) {
        // Raycast to check for overlapping UI elements
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current) {
            position = eventData.position
        };

        // List to store raycast results
        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        foreach (var result in raycastResults) {
            // Check if we dropped on another Element
            Element droppedOnElement = result.gameObject.GetComponent<Element>();
            if (droppedOnElement != null && droppedOnElement != this && droppedOnElement.elementName != "...") {
                Debug.Log($"Dropped on: {droppedOnElement.elementName}");

                droppedOnElement.photonView.RPC(nameof(RPC_DestroyGameObject), RpcTarget.All);
                photonView.RPC(nameof(RPC_DestroyGameObject), RpcTarget.All);
                Element elementSpawned = gameManager.SpawnElement("...", transform.position + new Vector3(10f, -10f, 0));

                _ = llmCharacter.Chat("What does " + this.elementName + " plus " + droppedOnElement.elementName + " make? (please ONLY respond with the phrase they create and also you cannot invent new words)",
                (string reply) => {
                    if (reply.Contains("=")) {
                        // Get the substring starting one character after the '='
                        reply = reply.Substring(reply.IndexOf('=') + 1).Trim();
                    }
                    else {
                        // Remove all non-letters and preserve spaces
                        reply = Regex.Replace(reply, "[^a-zA-Z\\s]", "");
                    }

                    Debug.Log(reply);
                        latestReply = reply;
                },
                () => {
                    elementSpawned.GetComponent<PhotonView>().RPC(nameof(RPC_SetElementName), RpcTarget.All, latestReply);
                    sfxManager.PlayCombineSFX();
                },
                false);

                break;
            }
        }
    }
    [PunRPC]
    public void RPC_SetAsLastSibling() {
        transform.SetAsLastSibling();
    }
    [PunRPC]
    public void RPC_DestroyGameObject() {
        Destroy(gameObject);
    }
    public void OnPointerDown(PointerEventData eventData) {
        if (elementName == "..." || isDragging) {
            return;
        }
        // Request ownership when the drag starts if this PhotonView doesn't belong to the current player
        if (!photonView.IsMine) {
            photonView.RequestOwnership();
        }
        isDragging = true;

        photonView.RPC(nameof(RPC_SetAsLastSibling), RpcTarget.All);
        if(Input.GetMouseButton(1)) {
            photonView.RPC(nameof(RPC_DestroyGameObject), RpcTarget.All);
        } else {
            sfxManager.PlaySelectSFX();
        }
    }
    public void OnPointerUp(PointerEventData eventData) {
        isDragging = false;

        DetectDropTarget(eventData);
    }
    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.clickCount == 2) {
            gameManager.SpawnElement(elementName, transform.position + new Vector3(10f, -10f, 0));
        }
    }
    public void SetElementName(string name) {
        elementName = name;
        text.text = name;
        this.name = name;
    }
    [PunRPC]
    public void RPC_SetElementName(string name) {
        SetElementName(name);
    }
    public string GetElementName() {
        return elementName;
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            // The owner sends data to other clients
            stream.SendNext(elementName);
            stream.SendNext(transform.localPosition);
            stream.SendNext(isDragging);
        }
        else {
            // Other clients receive the data
            elementName = (string)stream.ReceiveNext();
            transform.localPosition = (Vector3)stream.ReceiveNext();
            isDragging = (bool)stream.ReceiveNext();
        }
    }
    public void OnPhotonInstantiate(PhotonMessageInfo info) {
        gameManager.SetupElement(gameObject);
    }
}
