using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes2D;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class LLement : MonoBehaviourPun, IPunObservable
{
    public ElementData elementData;
    private UIDragHandler dragHandler;
    public Shape shape;

    [SerializeField] private Canvas canvas;
    [SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private EventSystem eventSystem;

    public string ElementName;
    public TextMeshProUGUI elementNameLabel;

    public bool isPreoccupied = false;
    private bool isBeingDragged;

    private void Awake() {
        elementNameLabel = GetComponentInChildren<TextMeshProUGUI>();
    }
    void Start()
    {
        isPreoccupied = false;

        transform.SetParent(NewGameManager.Instance.ElementParentTransform, true);
        transform.SetAsLastSibling();
        transform.localScale = Vector3.one;

        // Get the UIDragHandler component on this GameObject
        dragHandler = GetComponent<UIDragHandler>();

        if (dragHandler != null)
        {
            // Subscribe to the OnDragStart and OnDragEnd events
            dragHandler.OnClickDownEvent.AddListener(HandleDragStart);
            dragHandler.OnPointerUpEvent.AddListener(HandleDragEnd);
            dragHandler.OnDoubleClick.AddListener(HandleDoubleClick);
            dragHandler.OnRightClick.AddListener(HandleRightClick);
        }
        else
        {
            Debug.LogWarning("UIDragHandler component not found on this GameObject.");
        }

        shape = GetComponent<Shape>();
        // Get the Canvas and GraphicRaycaster components for UI raycasting
        canvas = GetComponentInParent<Canvas>();
        raycaster = canvas.GetComponent<GraphicRaycaster>();
        eventSystem = EventSystem.current;

        if (photonView.IsMine) {
            photonView.RPC(nameof(SetNameRPC), RpcTarget.AllBuffered, ElementName);
            SetElementData(elementData);
        }
    }

    public void SetElementData(ElementData eData) {
        elementData = eData;
        if(elementData.word != "...")
            elementData.word = elementData.word.Replace(".", "");
        SetName(elementData.word);
        photonView.RPC(nameof(SetColorsRPC), RpcTarget.AllBuffered, elementData.Colour.r, elementData.Colour.g, elementData.Colour.b);            
    }

    [PunRPC]
    public void SetColorsRPC(float r1, float g1, float b1)
    {
        Color color = new Color(r1, g1, b1);
        elementNameLabel.color = Color.white;
        shape.settings.fillColor = color;
        shape.settings.fillColor2 = MultiplyColorVBy(color, .5f);
    }

    private void Update() {
        if (isPreoccupied) {
            shape.settings.outlineColor = Color.grey;
        }
        else {
            shape.settings.outlineColor = Color.black;
        }


        if (isPreoccupied && !photonView.IsMine) {
            dragHandler.enabled = false;
        }
        else {
            dragHandler.enabled = true;
        }
    }


    private void OnDestroy()
    {
        // Unsubscribe from events to avoid potential memory leaks
        if (dragHandler != null)
        {
            dragHandler.OnClickDownEvent.RemoveListener(HandleDragStart);
            dragHandler.OnPointerUpEvent.RemoveListener(HandleDragEnd);
            dragHandler.OnDoubleClick.RemoveListener(HandleDoubleClick);
            dragHandler.OnRightClick.RemoveListener(HandleRightClick);
        }
    }

    public void SetName(string newName) {
        photonView.RPC(nameof(SetNameRPC), RpcTarget.AllBuffered, newName);            
    }
    private void HandleDoubleClick() {
        LLement newElement = NewGameManager.Instance.SpawnLLement(ElementName, transform.position + new Vector3(10f, -10f, 0));
        newElement.elementData = new ElementData { word = ElementName, color = this.elementData.color };
    }
    private void HandleRightClick() {
        SFXManager.Instance.PlaySelectSFX();
        photonView.RPC(nameof(DeleteElement), RpcTarget.AllBuffered);
    }

    private void HandleDragStart()
    {
        // Request ownership when the drag starts if this PhotonView doesn't belong to the current player
        if (!photonView.IsMine) {
            photonView.RequestOwnership();
        }
        

        // Add any additional logic here for when the drag starts
        photonView.RPC(nameof(OnDragRPC), RpcTarget.AllBuffered, true);
        SetPreoccupied(true);
        transform.SetAsLastSibling();

        SFXManager.Instance.PlaySelectSFX();

    }
    private async void HandleDragEnd()
    {

        // Add any additional logic here for when the drag ends
        photonView.RPC(nameof(OnDragRPC), RpcTarget.AllBuffered, false);

        // Get the cursor position when dragging ends
        Vector2 cursorPosition = Input.mousePosition;

        // Set up the PointerEventData with the current cursor position
        PointerEventData pointerEventData = new PointerEventData(eventSystem)
        {
            position = cursorPosition
        };

        // Perform a raycast from the cursor position
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        raycaster.Raycast(pointerEventData, raycastResults);

        // List to hold detected UI objects under the cursor
        List<LLement> detectedLLement = new List<LLement>();

        // Check raycast results for valid LLement objects
        foreach (RaycastResult result in raycastResults)
        {
            LLement llement = result.gameObject.GetComponent<LLement>();
            if (llement != null && !llement.isPreoccupied && result.gameObject != gameObject && !detectedLLement.Contains(llement))
            {
                detectedLLement.Add(llement);
            }
            ElementStation droppedOnStation = result.gameObject.GetComponent<ElementStation>();
            if(droppedOnStation != null) {
                Debug.Log($"Dropped on: {droppedOnStation.GetStationText()}");

                elementNameLabel.text = "...";

                string[] results = await droppedOnStation.GetStationResult(this.ElementName);

                if (results.Length == 1) {
                    string colour = await NewGameManager.Instance.ChatGPTClient.SendChatRequest("Give me ONLY the a HEX code for the colour that represents " + results[0] + " with the # at the start");
                    SetElementData(new ElementData { word = results[0], color = colour });
                }
                else {
                    string colour1 = await NewGameManager.Instance.ChatGPTClient.SendChatRequest("Give me ONLY the a HEX code for the colour that represents " + results[0] + " with the # at the start");
                    string colour2 = await NewGameManager.Instance.ChatGPTClient.SendChatRequest("Give me ONLY the a HEX code for the colour that represents " + results[0] + " with the # at the start");
                    
                    Instantiate(this.gameObject, transform.position + new Vector3(0, -10f, 0), Quaternion.identity, transform.parent).GetComponent<LLement>().
                        SetElementData(new ElementData { word = results[0], color = colour1 });
                    Instantiate(this.gameObject, transform.position + new Vector3(0f, -100f, 0), Quaternion.identity, transform.parent).GetComponent<LLement>().
                        SetElementData(new ElementData { word = results[1], color = colour2 });

                    Destroy(gameObject);
                }
            }
        }

        // Check if we have detected any objects underneath
        if (detectedLLement.Count > 0)
        {
            if(!elementNameLabel.text.Contains("+") && !(elementNameLabel.text == "...")
                && !detectedLLement[0].elementNameLabel.text.Contains("+") && !(detectedLLement[0].elementNameLabel.text == "...")) {
                NewGameManager.Instance.CombineElements(this, detectedLLement[0]);
            }
        }

        SetPreoccupied(false);
    }


    [PunRPC]
    private void SetNameRPC(string newName) {
        ElementName = newName;
        elementNameLabel.text = newName;
        gameObject.name = newName;
    }

    [PunRPC]
    private void OnDragRPC(bool isDragged) {
        if (isDragged) {
            transform.SetAsLastSibling();
        }
    }
    
    public void SetPreoccupied(bool val) {
        photonView.RPC(nameof(SetPreoccupiedRPC), RpcTarget.AllBuffered, val);            
    }

    [PunRPC]
    private void SetPreoccupiedRPC(bool val) {
        isPreoccupied = val;
    }
    [PunRPC]
    private void DeleteElement() {
        Destroy(gameObject);
    }

    // listen to on ui drag start

    // listen to on ui drag end

    // register that a llement is over another one, beginning the combination process. Call game manager for this.

    // change color based on is-selected

    // Method to decrease a color's V (brightness) by 20%
    private Color MultiplyColorVBy(Color color, float multi) {
        // Convert the RGB color to HSV
        Color.RGBToHSV(color, out float h, out float s, out float v);

        // Decrease the V (brightness) by 20%
        v *= multi;

        // Clamp the value to ensure it's between 0 and 1
        v = Mathf.Clamp01(v);

        // Convert back to RGB and return the modified color
        return Color.HSVToRGB(h, s, v);
    }
    private Color IncreaseColorVBy(Color color, float multi) {
        // Convert the RGB color to HSV
        Color.RGBToHSV(color, out float h, out float s, out float v);

        // Decrease the V (brightness) by 20%
        v += multi;

        // Clamp the value to ensure it's between 0 and 1
        v = Mathf.Clamp01(v);

        // Convert back to RGB and return the modified color
        return Color.HSVToRGB(h, s, v);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            stream.SendNext(shape.settings.fillColor.r);
            stream.SendNext(shape.settings.fillColor.g);
            stream.SendNext(shape.settings.fillColor.b);

            stream.SendNext(elementData.word);
            stream.SendNext(elementData.color);
        }
        else {
            float r = (float)stream.ReceiveNext();
            float g = (float)stream.ReceiveNext();
            float b = (float)stream.ReceiveNext();
            shape.settings.fillColor = new Color(r, g, b);
            shape.settings.fillColor2 = MultiplyColorVBy(shape.settings.fillColor, .5f);

            elementData.word = (string)stream.ReceiveNext();
            elementData.color = (string)stream.ReceiveNext();
        }
    }
}
