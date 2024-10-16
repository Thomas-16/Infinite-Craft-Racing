using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Shapes2D;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using UnityEngine.Video;

public class LLement : MonoBehaviour
{
    public ElementData elementData;
    public PhotonView photonView;
    private UIDragHandler dragHandler;
    private Shape shape;

    [SerializeField] private Color unselectedColor1;
    [SerializeField] private Color unselectedColor2;
    [SerializeField] private Color selectedColor1;
    [SerializeField] private Color selectedColor2;
    [SerializeField] private Canvas canvas;
    [SerializeField] private GraphicRaycaster raycaster;
    [SerializeField] private EventSystem eventSystem;

    public string ElementName;
    [SerializeField] private TextMeshProUGUI elementNameLabel;

    public bool isPreoccupied = true;
    private bool isBeingDragged;

    void Start()
    {
        transform.SetParent(NewGameManager.Instance.ElementParentTransform, true);
        transform.SetAsLastSibling();
        transform.localScale = Vector3.one;

        // Get the UIDragHandler component on this GameObject
        dragHandler = GetComponent<UIDragHandler>();

        if (dragHandler != null)
        {
            // Subscribe to the OnDragStart and OnDragEnd events
            dragHandler.OnClickDown.AddListener(HandleDragStart);
            dragHandler.OnDragEnd.AddListener(HandleDragEnd);
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

        photonView = GetComponent<PhotonView>();
        if (photonView.IsMine) {
            photonView.RPC(nameof(SetNameRPC), RpcTarget.AllBuffered, ElementName);            
        }


    }

    public void SetElementData(ElementData eData) {
        elementData = eData;
        SetName(elementData.word);
        photonView.RPC(nameof(SetColorsRPC), RpcTarget.AllBuffered, elementData.PrimaryColor.r, elementData.PrimaryColor.g, elementData.PrimaryColor.b, elementData.PrimaryColor.a, elementData.SecondaryColor.r, elementData.SecondaryColor.g, elementData.SecondaryColor.b, elementData.SecondaryColor.a);            
    }

    [PunRPC]
    public void SetColorsRPC(float r1, float g1, float b1, float a1, float r2, float g2, float b2, float a2)
    {
        Color primaryColor = new Color(r1, g1, b1, a1);
        Color secondaryColor = new Color(r2, g2, b2, a2);
        shape.settings.fillColor = primaryColor;
        shape.settings.fillColor2 = secondaryColor;
    }

    // Method to send the color through an RPC call
    public void ChangeColor(Color color)
    {
        photonView.RPC("SetObjectColor", RpcTarget.AllBuffered, color.r, color.g, color.b, color.a);
    }

    private void Update() {
        if (isPreoccupied) {
            shape.settings.outlineColor = Color.grey;

            //shape.settings.fillColor = selectedColor1;
            //shape.settings.fillColor2 = selectedColor2;
        }
        else {
            shape.settings.outlineColor = Color.black;

            //shape.settings.fillColor = unselectedColor1;
            //shape.settings.fillColor2 = unselectedColor2; 
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
            dragHandler.OnClickDown.RemoveListener(HandleDragStart);
            dragHandler.OnDragEnd.RemoveListener(HandleDragEnd);
        }
    }

    public void SetName(string newName) {
        photonView.RPC(nameof(SetNameRPC), RpcTarget.AllBuffered, newName);            
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

    }

    private void CheckHoverCoroutine() {

    }

    private void HandleDragEnd()
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
        }

        // Check if we have detected any objects underneath
        if (detectedLLement.Count > 0)
        {
            Debug.Log("detected element: " + detectedLLement[0].ElementName);
            NewGameManager.Instance.CombineElements(this, detectedLLement[0]);
        }

        SetPreoccupied(false);
    }


/*
    private void HandleDragEnd()
    {
        // Add any additional logic here for when the drag ends
        photonView.RPC(nameof(OnDragRPC), RpcTarget.AllBuffered, false);

        // Get the RectTransform of the dragged element
        RectTransform rectTransform = GetComponent<RectTransform>();

        // Perform a raycast to check for UI elements under each corner of the dragged element
        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        // List to hold detected UI objects under the dragged image
        List<LLement> detectedLLement = new List<LLement>();

        foreach (Vector3 corner in corners)
        {
            PointerEventData pointerEventData = new PointerEventData(eventSystem)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, corner)
            };

            List<RaycastResult> raycastResults = new List<RaycastResult>();
            raycaster.Raycast(pointerEventData, raycastResults);

            foreach (RaycastResult result in raycastResults)
            {
                LLement llement = result.gameObject.GetComponent<LLement>();
                // Avoid adding duplicates and ignore itself
                if (llement != null && !llement.isPreoccupied && result.gameObject != gameObject && !detectedLLement.Contains(llement))
                {
                    detectedLLement.Add(llement);
                }
            }
        }

        // Check if we have detected any objects underneath
        if (detectedLLement.Count > 0)
        {
            Debug.Log("detected element: " + detectedLLement[0].ElementName);
            NewGameManager.Instance.CombineElements(this, detectedLLement[0]);
        }

        SetPreoccupied(false);
    }*/

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

    // listen to on ui drag start

    // listen to on ui drag end

    // register that a llement is over another one, beginning the combination process. Call game manager for this.

    // change color based on is-selected




}
