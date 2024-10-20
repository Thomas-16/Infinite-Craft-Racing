using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIDragHandler : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
{
    [SerializeField] private RectTransform rectTransform; // UI element's RectTransform
    [SerializeField] private Canvas canvas; // The canvas where the UI element resides
    private Vector2 pointerOffset; // Offset between pointer and element position at start of drag

    // Public Unity Events for drag start and end
	public UnityEvent OnClickDownEvent;
    public UnityEvent OnDragStartEvent;
    public UnityEvent OnDragEndEvent;
    public UnityEvent OnDoubleClick;
    public UnityEvent OnRightClick;
    public UnityEvent OnPointerUpEvent;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>(); // Get the RectTransform component
        canvas = GetComponentInParent<Canvas>(); // Get the parent Canvas component (important for accurate dragging)
    }
	private void Update() {
		if (canvas == null) {
			canvas = GetComponentInParent<Canvas>();
		}
	}

    // Called when the user clicks on the UI element
    public void OnPointerDown(PointerEventData eventData)
    {
        // Calculate the offset between the pointer position and the UI element's anchored position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );

        pointerOffset = rectTransform.anchoredPosition - localPointerPosition;
        if(Input.GetMouseButton(1)) {
            OnRightClick?.Invoke();
        } else {
            OnClickDownEvent?.Invoke();
        }
    }

    // Called when the user begins dragging the UI element
    public void OnBeginDrag(PointerEventData eventData)
    {
        OnDragStartEvent?.Invoke(); // Invoke the OnDragStart event if any listeners are attached
    }

    // Called every frame while the user is dragging the UI element
    public void OnDrag(PointerEventData eventData)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition))
        {
            // Apply the offset to the new position
            rectTransform.anchoredPosition = localPointerPosition + pointerOffset;
        }
    }

    // Called when the user releases the UI element
    public void OnEndDrag(PointerEventData eventData)
    {
        OnDragEndEvent?.Invoke(); // Invoke the OnDragEnd event if any listeners are attached
    }

    public void OnPointerUp(PointerEventData eventData) {
        OnPointerUpEvent?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData) {
        if(eventData.clickCount == 2) {
            OnDoubleClick?.Invoke();
        }
    }
}
