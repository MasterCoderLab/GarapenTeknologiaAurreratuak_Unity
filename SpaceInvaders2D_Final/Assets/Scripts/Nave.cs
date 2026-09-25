using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Jokalariaren navea kudeatzen duen klasea.
/// Mugimendua, tiroak eta talkak kontrolatzen ditu.
/// </summary>
public class Nave : MonoBehaviour
{
    // Navearen mugimendu-abiadura.
    [SerializeField] float velocidad = 9;

    // Jokalariaren tiroaren abiadura.
    private float velocidadDisparo = 2;

    // Tiroa sortzeko erabiliko den prefaba.
    [SerializeField] Transform prefabDisparo;

    // Pantailan informazioa erakusteko testua.
    [SerializeField] UnityEngine.UI.Text textoPuntos;


    /// <summary>
    /// Objektua sortzen denean behin exekutatzen da.
    /// </summary>
    void Start()
    {

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

        // Navearen X posizioa pantailaren mugen barruan mantentzen da.
        Vector3 posicion = transform.position;
        posicion.x = Mathf.Clamp(posicion.x, -4f, 4f);
        transform.position = posicion;

        // Fire1 botoia sakatzen denean tiro bat sortzen da.
        if (Input.GetButtonDown("Fire1"))
        {
            textoPuntos.text = "Has disparado";

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
                .linearVelocity = new Vector2(0, velocidadDisparo);
        }
    }


    /// <summary>
    /// Naveak beste Collider2D batekin talka egiten duenean exekutatzen da.
    /// </summary>
    /// <param name="collision">Navearekin talka egin duen objektuaren Collider2D-a.</param>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Pum");
    }
}