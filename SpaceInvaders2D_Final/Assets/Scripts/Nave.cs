using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Jokalariaren navea kudeatzen duen klasea.
/// Mugimendua, tiroak, talkak eta puntuazioa kontrolatzen ditu.
/// </summary>
public class Nave : MonoBehaviour
{
    // Navearen mugimendu-abiadura.
    [SerializeField] float velocidad = 9;

    // Jokalariaren tiroaren abiadura.
    private float velocidadDisparo = 2;

    // Tiroa sortzeko erabiliko den prefaba.
    [SerializeField] Transform prefabDisparo;

    // Pantailan puntuazioa erakusteko testua.
    [SerializeField] UnityEngine.UI.Text textoPuntos;


    // =========================
    // 2. HOBEKUNTZA: TIROEN COOLDOWN-A
    // =========================

    // Tiro bakoitzaren arteko gutxieneko denbora segundotan.
    [SerializeField] float cooldownDisparo = 0.5f;

    // Hurrengo tiroa noiz egin daitekeen gordetzen du.
    private float siguienteDisparo = 0f;


    // =========================
    // 5. HOBEKUNTZA: PUNTUAZIO ERREALA
    // =========================

    // Jokalariaren uneko puntuazioa gordetzen du.
    private int puntos = 0;


    /// <summary>
    /// Objektua sortzen denean behin exekutatzen da.
    /// Hasierako puntuazioa pantailan erakusten du.
    /// </summary>
    void Start()
    {
        ActualizarTextoPuntos();
    }


    /// <summary>
    /// Frame bakoitzean exekutatzen da.
    /// Navearen mugimendua eta tiroak kontrolatzen ditu.
    /// </summary>
    void Update()
    {
        // Mugimendu horizontalaren balioa lortzen da.
        float horizontal = Input.GetAxis("Horizontal");

        // Navea horizontalki mugitzen da.
        transform.Translate(
            horizontal * velocidad * Time.deltaTime,
            0,
            0);


        // =========================
        // 1. HOBEKUNTZA: PANTAILAREN MUGAK
        // =========================

        // Navearen X posizioa pantailaren mugen barruan mantentzen da.
        Vector3 posicion = transform.position;

        // X posizioa -4 eta 4 arteko balioetara mugatzen da.
        posicion.x = Mathf.Clamp(
            posicion.x,
            -4f,
            4f);

        // Mugatutako posizioa naveari aplikatzen zaio.
        transform.position = posicion;


        // =========================
        // 2. HOBEKUNTZA: TIROEN COOLDOWN-A
        // =========================

        // Fire1 sakatzen bada eta cooldown-a amaitu bada,
        // tiro berri bat sortzen da.
        if (Input.GetButtonDown("Fire1") &&
            Time.time >= siguienteDisparo)
        {
            // Hurrengo tiroa egin ahal izango den unea kalkulatzen da.
            siguienteDisparo =
                Time.time + cooldownDisparo;

            // Tiroaren soinua erreproduzitzen da.
            GetComponent<AudioSource>().Play();

            // Tiroaren prefaba navearen posizioan sortzen da.
            Transform disparo = Instantiate(
                prefabDisparo,
                transform.position,
                Quaternion.identity);

            // Tiroari goranzko abiadura ematen zaio.
            disparo.gameObject
                .GetComponent<Rigidbody2D>()
                .linearVelocity =
                new Vector2(0, velocidadDisparo);
        }
    }


    // =========================
    // 5. HOBEKUNTZA: PUNTUAZIO ERREALA
    // =========================

    /// <summary>
    /// Jokalariaren puntuazioari emandako puntu kopurua gehitzen dio.
    /// </summary>
    /// <param name="cantidad">
    /// Gehitu beharreko puntu kopurua.
    /// </param>
    public void SumarPuntos(int cantidad)
    {
        puntos += cantidad;

        ActualizarTextoPuntos();
    }


    /// <summary>
    /// Pantailako puntuazio-testua eguneratzen du.
    /// </summary>
    private void ActualizarTextoPuntos()
    {
        textoPuntos.text = "Puntos: " + puntos;
    }


    /// <summary>
    /// Naveak beste Collider2D batekin talka egiten duenean exekutatzen da.
    /// </summary>
    /// <param name="collision">
    /// Navearekin talka egin duen objektuaren Collider2D-a.
    /// </param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Pum");
    }
}