using UnityEngine;

// Vista lateral: el jugador se mueve en el eje horizontal y puede saltar.
// El eje vertical queda a cargo de la gravedad real del Rigidbody2D, por eso el
// movimiento usa rb.velocity en vez de MovePosition (MovePosition ignoraría/pelearía
// con la aceleración que la gravedad y el salto le aplican a rb.velocity.y).
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 6f;

    [Header("Salto")]
    [SerializeField] private float fuerzaSalto = 8f;
    [Tooltip("Punto (hijo del jugador) ubicado a la altura de los pies, usado para detectar el piso")]
    [SerializeField] private Transform chequeoSuelo;
    [SerializeField] private float radioChequeoSuelo = 0.15f;
    [SerializeField] private LayerMask capaSuelo;

    [Header("Joystick virtual (mobile)")]
    [Tooltip("Si se deja vacío, el jugador se mueve con teclado (flechas/A-D) para probar en el editor")]
    [SerializeField] private VirtualJoystick joystick;

    [Header("Límites de cancha (opcional)")]
    [SerializeField] private bool limitarAlCampo = false;
    [SerializeField] private float limiteMinX;
    [SerializeField] private float limiteMaxX;

    private Rigidbody2D rb;
    private float entradaHorizontal;
    private bool enElSuelo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
    }

    private void Update()
    {
        float horizontalJoystick = joystick != null ? joystick.Direccion.x : 0f;

        entradaHorizontal = Mathf.Abs(horizontalJoystick) > 0.1f
            ? horizontalJoystick
            : Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            Saltar();
        }
    }

    private void FixedUpdate()
    {
        enElSuelo = chequeoSuelo != null
            && Physics2D.OverlapCircle(chequeoSuelo.position, radioChequeoSuelo, capaSuelo);

        Vector2 velocidadActual = rb.velocity;
        velocidadActual.x = entradaHorizontal * velocidad;
        rb.velocity = velocidadActual;

        if (limitarAlCampo)
        {
            float x = Mathf.Clamp(rb.position.x, limiteMinX, limiteMaxX);
            rb.position = new Vector2(x, rb.position.y);
        }
    }

    // Conectar también al OnClick de un botón de salto en la UI para mobile.
    public void Saltar()
    {
        if (!enElSuelo)
        {
            return;
        }

        rb.velocity = new Vector2(rb.velocity.x, fuerzaSalto);
    }
}
