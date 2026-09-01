using UnityEngine;
using UnityEngine.EventSystems;

public class TouchLookZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Touch Look Tracking")]
    [Tooltip("If true, pointer will only track when originating within this zone.")]
    [SerializeField] private bool requireInitialTouchInZone = true;

    private int activePointerId = -999;
    private Vector2 lastPointerPosition;
    private bool isTracking = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isTracking) return;

        activePointerId = eventData.pointerId;
        lastPointerPosition = eventData.position;
        isTracking = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isTracking || eventData.pointerId != activePointerId) return;

        Vector2 currentPos = eventData.position;
        Vector2 delta = currentPos - lastPointerPosition;
        lastPointerPosition = currentPos;

        if (MobileInputManager.Instance != null)
        {
            MobileInputManager.Instance.AddTouchLookDelta(delta);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == activePointerId)
        {
            isTracking = false;
            activePointerId = -999;
        }
    }

    private void OnDisable()
    {
        isTracking = false;
        activePointerId = -999;
    }
}
