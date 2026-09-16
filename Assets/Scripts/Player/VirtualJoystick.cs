using UnityEngine;
using UnityEngine.EventSystems;

// Colocar en una Image de UI (fondo del joystick) con un raycast target activo,
// dentro de un Canvas. La manija es otra Image, hija del fondo.
[RequireComponent(typeof(RectTransform))]
public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform fondo;
    [SerializeField] private RectTransform manija;
    [SerializeField] private float rangoMovimiento = 100f;

    public Vector2 Direccion { get; private set; }

    private void Reset()
    {
        fondo = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            fondo, eventData.position, eventData.pressEventCamera, out Vector2 posicionLocal);

        Vector2 offset = Vector2.ClampMagnitude(posicionLocal, rangoMovimiento);
        manija.anchoredPosition = offset;
        Direccion = offset / rangoMovimiento;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        manija.anchoredPosition = Vector2.zero;
        Direccion = Vector2.zero;
    }
}
