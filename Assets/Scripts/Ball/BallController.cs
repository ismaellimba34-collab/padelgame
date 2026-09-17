using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BallController : MonoBehaviour
{
    public enum TipoGolpe { Smash, Bandeja, Vibora, Globo, Dejada }

    [System.Serializable]
    public struct ConfiguracionGolpe
    {
        public TipoGolpe tipo;
        public float velocidad;
        [Tooltip("Fuerza lateral que curva la trayectoria (efecto viborero). 0 = sin efecto")]
        public float curvatura;
        [Tooltip("Duración del efecto de curvatura o del frenado, en segundos")]
        public float duracionEfecto;
        [Tooltip("Deceleración progresiva tras el golpe, usada por la dejada. 0 = sin freno")]
        public float frenado;
    }

    [Header("Gravedad (vista lateral)")]
    [Tooltip("Multiplicador de gravedad de la pelota. 0 = sin gravedad (vista superior); >0 = cae y traza arcos (vista lateral)")]
    [SerializeField] private float gravedad = 2.5f;

    [Header("Rebote contra paredes")]
    [Tooltip("Si está activo, la pelota conserva su velocidad al rebotar (ángulo de entrada = ángulo de salida)")]
    [SerializeField] private bool mantenerVelocidadConstante = true;
    [SerializeField] private float velocidadMinima = 3f;
    public UnityEvent OnRebotePared;

    [Header("Prueba en el editor")]
    [SerializeField] private Vector2 direccionPrueba = Vector2.right;
    [SerializeField] private float velocidadPrueba = 10f;

    [Header("Configuración de golpes")]
    [SerializeField]
    private ConfiguracionGolpe[] configuraciones = new ConfiguracionGolpe[]
    {
        new ConfiguracionGolpe { tipo = TipoGolpe.Smash,   velocidad = 18f, curvatura = 0f, duracionEfecto = 0f,    frenado = 0f },
        new ConfiguracionGolpe { tipo = TipoGolpe.Bandeja, velocidad = 10f, curvatura = 0f, duracionEfecto = 0f,    frenado = 0f },
        new ConfiguracionGolpe { tipo = TipoGolpe.Vibora,  velocidad = 13f, curvatura = 6f, duracionEfecto = 0.35f, frenado = 0f },
        new ConfiguracionGolpe { tipo = TipoGolpe.Globo,   velocidad = 7f,  curvatura = 0f, duracionEfecto = 0f,    frenado = 0f },
        new ConfiguracionGolpe { tipo = TipoGolpe.Dejada,  velocidad = 4f,  curvatura = 0f, duracionEfecto = 0.6f,  frenado = 6f },
    };

    private Rigidbody2D rb;
    private Dictionary<TipoGolpe, ConfiguracionGolpe> configPorTipo;
    private Coroutine efectoActual;

    // En una esquina, la pelota puede tocar dos paredes en el mismo paso de física:
    // Unity dispara OnCollisionEnter2D una vez por cada una. Si resolviéramos el rebote
    // ahí mismo, la segunda llamada pisaría el resultado de la primera usando la MISMA
    // velocidad de entrada pero reflejada solo contra su propia pared, dando una dirección
    // final que no es un rebote de esquina real (casi paralela a una de las paredes, lo que
    // se ve como que la pelota se desliza). Por eso acumulamos las normales del mismo paso y
    // resolvemos una sola vez en FixedUpdate, que corre después de todos los callbacks de ese paso.
    private readonly List<Vector2> normalesPendientes = new List<Vector2>();
    private Vector2 velocidadEntrantePendiente;
    private bool hayColisionPendiente;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravedad;

        configPorTipo = new Dictionary<TipoGolpe, ConfiguracionGolpe>();
        foreach (var config in configuraciones)
        {
            configPorTipo[config.tipo] = config;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        DetenerEfecto();

        if (!hayColisionPendiente)
        {
            // relativeVelocity es la velocidad de acercamiento previa a la resolución física:
            // para cuando este callback se ejecuta, el motor ya aplicó fricción/bounciness del
            // Physics Material 2D sobre rb.velocity, dejando sobre todo la componente tangencial.
            velocidadEntrantePendiente = collision.relativeVelocity;
            hayColisionPendiente = true;
        }

        normalesPendientes.Add(collision.GetContact(0).normal);
    }

    private void FixedUpdate()
    {
        if (!hayColisionPendiente)
        {
            return;
        }

        Vector2 normal = Vector2.zero;
        foreach (var n in normalesPendientes)
        {
            normal += n;
        }
        normal.Normalize();

        // Si la pelota llega casi sin velocidad propia (p. ej. empujada por el jugador),
        // Reflect(0, normal) da (0,0): usamos la normal del choque como dirección de salida.
        Vector2 velocidadReflejada = velocidadEntrantePendiente.sqrMagnitude > 0.0001f
            ? Vector2.Reflect(velocidadEntrantePendiente, normal)
            : normal;

        if (mantenerVelocidadConstante)
        {
            float velocidadPrevia = Mathf.Max(velocidadEntrantePendiente.magnitude, velocidadMinima);
            velocidadReflejada = velocidadReflejada.normalized * velocidadPrevia;
        }

        rb.velocity = velocidadReflejada;
        OnRebotePared?.Invoke();

        normalesPendientes.Clear();
        hayColisionPendiente = false;
    }

    public void Golpear(Vector2 direccion, TipoGolpe tipo)
    {
        if (!configPorTipo.TryGetValue(tipo, out var config))
        {
            Debug.LogWarning($"BallController: no hay configuración para el golpe {tipo}");
            return;
        }

        DetenerEfecto();

        Vector2 dir = direccion.normalized;
        rb.velocity = dir * config.velocidad;

        if (config.curvatura != 0f && config.duracionEfecto > 0f)
        {
            efectoActual = StartCoroutine(AplicarCurvatura(config.curvatura, config.duracionEfecto));
        }
        else if (config.frenado > 0f)
        {
            efectoActual = StartCoroutine(AplicarFrenado(config.frenado));
        }
    }

    [ContextMenu("Lanzar de prueba")]
    private void LanzarDePrueba()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("BallController: entrá en Play para probar el lanzamiento.");
            return;
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        DetenerEfecto();
        rb.velocity = direccionPrueba.normalized * velocidadPrueba;
    }

    private void DetenerEfecto()
    {
        if (efectoActual != null)
        {
            StopCoroutine(efectoActual);
            efectoActual = null;
        }
    }

    private IEnumerator AplicarCurvatura(float curvatura, float duracion)
    {
        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracion && rb.velocity.sqrMagnitude > 0.01f)
        {
            Vector2 lateral = new Vector2(-rb.velocity.y, rb.velocity.x).normalized;
            rb.velocity += lateral * curvatura * Time.fixedDeltaTime;
            tiempoTranscurrido += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        efectoActual = null;
    }

    private IEnumerator AplicarFrenado(float frenado)
    {
        while (rb.velocity.magnitude > 0.2f)
        {
            rb.velocity = Vector2.MoveTowards(rb.velocity, Vector2.zero, frenado * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
        rb.velocity = Vector2.zero;
        efectoActual = null;
    }
}
