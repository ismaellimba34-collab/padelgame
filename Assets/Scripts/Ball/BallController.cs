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

    [Header("Rebote contra paredes")]
    [Tooltip("Si está activo, la pelota conserva su velocidad al rebotar (ángulo de entrada = ángulo de salida)")]
    [SerializeField] private bool mantenerVelocidadConstante = true;
    [SerializeField] private float velocidadMinima = 3f;
    public UnityEvent OnRebotePared;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        configPorTipo = new Dictionary<TipoGolpe, ConfiguracionGolpe>();
        foreach (var config in configuraciones)
        {
            configPorTipo[config.tipo] = config;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        DetenerEfecto();

        Vector2 normal = collision.GetContact(0).normal;
        Vector2 velocidadEntrante = rb.velocity;

        // Si la pelota llega casi sin velocidad propia (p. ej. empujada por el jugador),
        // Reflect(0, normal) da (0,0): usamos la normal del choque como dirección de salida.
        Vector2 velocidadReflejada = velocidadEntrante.sqrMagnitude > 0.0001f
            ? Vector2.Reflect(velocidadEntrante, normal)
            : normal;

        if (mantenerVelocidadConstante)
        {
            float velocidadPrevia = Mathf.Max(velocidadEntrante.magnitude, velocidadMinima);
            velocidadReflejada = velocidadReflejada.normalized * velocidadPrevia;
        }

        rb.velocity = velocidadReflejada;
        OnRebotePared?.Invoke();
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
