using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 6f;

    [Header("Joystick virtual (mobile)")]
    [Tooltip("Si se deja vacío, el jugador se mueve con teclado (WASD / flechas) para probar en el editor")]
    [SerializeField] private VirtualJoystick joystick;

    [Header("Límites de cancha (opcional)")]
    [SerializeField] private bool limitarAlCampo = false;
    [SerializeField] private Vector2 limiteMin;
    [SerializeField] private Vector2 limiteMax;

    private Rigidbody2D rb;
    private Vector2 direccionMovimiento;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        Vector2 entradaJoystick = joystick != null ? joystick.Direccion : Vector2.zero;

        if (entradaJoystick.sqrMagnitude > 0.01f)
        {
            direccionMovimiento = entradaJoystick;
        }
        else
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            direccionMovimiento = new Vector2(horizontal, vertical);
            if (direccionMovimiento.sqrMagnitude > 1f)
            {
                direccionMovimiento.Normalize();
            }
        }
    }

    private void FixedUpdate()
    {
        Vector2 nuevaPosicion = rb.position + direccionMovimiento * velocidad * Time.fixedDeltaTime;

        if (limitarAlCampo)
        {
            nuevaPosicion.x = Mathf.Clamp(nuevaPosicion.x, limiteMin.x, limiteMax.x);
            nuevaPosicion.y = Mathf.Clamp(nuevaPosicion.y, limiteMin.y, limiteMax.y);
        }

        rb.MovePosition(nuevaPosicion);
    }
}
