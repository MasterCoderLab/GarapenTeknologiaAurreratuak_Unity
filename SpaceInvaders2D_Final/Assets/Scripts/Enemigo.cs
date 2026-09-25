using System.Collections;
using UnityEngine;

/// <summary>
/// Etsaia kudeatzen duen klasea.
/// Etsaiaren mugimendua eta tiroen sorrera kontrolatzen ditu.
/// </summary>
public class Enemigo : MonoBehaviour
{
    // Etsaiaren abiadura horizontala.
    private float velocidadX = 2f;

    // Etsaiaren abiadura bertikala.
    private float velocidadY = 1.2f;

    // Etsaiaren tiroaren abiadura.
    // Balio negatiboa da tiroa beherantz mugitzeko.
    private float velocidadDisparo = -2f;

    // Etsaiak sortuko duen tiroaren prefaba.
    [SerializeField] Transform prefabDisparo;


    /// <summary>
    /// Objektua sortzen denean behin exekutatzen da.
    /// Etsaiaren tiroen korutina abiarazten du.
    /// </summary>
    void Start()
    {
        StartCoroutine(Disparar());
    }


    /// <summary>
    /// Frame bakoitzean exekutatzen da.
    /// Etsaia mugitu eta pantailaren mugen barruan mantentzen du.
    /// </summary>
    void Update()
    {
        // Etsaia horizontalki eta bertikalki mugitzen da.
        transform.Translate(
            velocidadX * Time.deltaTime,
            velocidadY * Time.deltaTime,
            0);

        // Etsaia pantailaren ezkerreko edo eskuineko mugara iristen bada,
        // mugimendu horizontalaren norabidea alderantzikatzen da.
        if (transform.position.x < -4f ||
            transform.position.x > 4f)
        {
            velocidadX = -velocidadX;
        }

        // Etsaia goiko edo beheko mugara iristen bada,
        // mugimendu bertikalaren norabidea alderantzikatzen da.
        if (transform.position.y < -2.5f ||
            transform.position.y > 2.5f)
        {
            velocidadY = -velocidadY;
        }
    }


    // =========================
    // 3. HOBEKUNTZA: ETSAIAREN TIROEK NAVEA SUNTSITZEA
    // =========================

    /// <summary>
    /// Etsaiaren tiroak automatikoki sortzen ditu.
    /// Tiro bakoitzaren artean ausazko denbora bat itxaroten du.
    /// </summary>
    /// <returns>
    /// Tiroen arteko itxaronaldia kudeatzeko IEnumerator-a.
    /// </returns>
    IEnumerator Disparar()
    {
        // Prefaba ez badago esleituta, korutina gelditzen da.
        if (prefabDisparo == null)
        {
            Debug.LogError(
                "Etsaiaren tiroaren prefaba ez dago esleituta.",
                this);

            yield break;
        }

        while (true)
        {
            // Hurrengo tiroa egiteko ausazko denbora kalkulatzen da.
            float pausa = Random.Range(3f, 7.5f);

            // Kalkulatutako denbora itxaroten da.
            yield return new WaitForSeconds(pausa);

            // Etsaiaren tiroa etsaiaren uneko posizioan sortzen da.
            Transform disparo = Instantiate(
                prefabDisparo,
                transform.position,
                Quaternion.identity);

            // Tiroari beheranzko abiadura ematen zaio.
            disparo.gameObject
                .GetComponent<Rigidbody2D>()
                .linearVelocity =
                new Vector2(0, velocidadDisparo);
        }
    }
}