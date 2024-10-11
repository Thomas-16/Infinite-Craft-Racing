using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Shapes2D;
using LLMUnity;
using System.Collections.Generic;
using Photon.Pun;
using System.Text.RegularExpressions;
using UnityEngine.VFX;
using Unity.VisualScripting;

public class NewElement : MonoBehaviour, IDragHandler, IBeginDragHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
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

    private void Awake() {
        text = GetComponentInChildren<TextMeshProUGUI>();
        shape = GetComponent<Shape>();
        llmCharacter = FindObjectOfType<LLMCharacter>();
        text.text = elementName;
        name = elementName;
        shape.settings.fillColor = unselectedColor1;
        shape.settings.fillColor2 = unselectedColor2;
    }
    private void Start() {
        SetName(elementName);
    }
    private void Update() {
        if (isDragging) {
            shape.settings.fillColor = selectedColor1;
            shape.settings.fillColor2 = selectedColor2;
        }
        else {
            shape.settings.fillColor = unselectedColor1;
            shape.settings.fillColor2 = unselectedColor2;
        }
    }
    public void OnBeginDrag(PointerEventData eventData) {
        if (elementName == "..." || isDragging) {
            return;
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
    private async void DetectDropTarget(PointerEventData eventData) {
        // Raycast to check for overlapping UI elements
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current) {
            position = eventData.position
        };

        // List to store raycast results
        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        foreach (var raycastResult in raycastResults) {
            ElementStation droppedOnStation = raycastResult.gameObject.GetComponent<ElementStation>();
            if (droppedOnStation != null) {
                Debug.Log($"Dropped on: {droppedOnStation.GetStationText()}");

                text.text = "...";
                string[] results = await droppedOnStation.GetStationResult(this.elementName);

                if (results.Length == 1) {
                    SetName(results[0]);
                } else {
                    Instantiate(this.gameObject, transform.position + new Vector3(0, -10f, 0), Quaternion.identity, transform.parent).GetComponent<NewElement>().
                        SetName(results[0]);
                    Instantiate(this.gameObject, transform.position + new Vector3(0f, -100f, 0), Quaternion.identity, transform.parent).GetComponent<NewElement>().
                        SetName(results[1]);

                    Destroy(gameObject);
                }

                break;
            }
        }
    }
    public void OnPointerDown(PointerEventData eventData) {
        if (elementName == "..." || isDragging) {
            return;
        }
        isDragging = true;

        if (Input.GetMouseButton(1)) {
            Destroy(gameObject);
        }
    }
    public void OnPointerUp(PointerEventData eventData) {
        isDragging = false;

        DetectDropTarget(eventData);
    }
    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.clickCount == 2) {
            Instantiate(this.gameObject, transform.position + new Vector3(10f, -10f, 0), Quaternion.identity, transform.parent);
        }
    }
    public void SetName(string name) {
        elementName = name;
        text.text = name;
        gameObject.name = name;
    }
}
