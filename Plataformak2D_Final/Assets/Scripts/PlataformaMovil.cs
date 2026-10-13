
using UnityEngine;

/// <summary>
/// Plataforma baten mugimendu automatikoa kudeatzen du.
/// Hasierako posiziotik helmugara mugitzen da,
/// eta ondoren hasierako posiziora itzultzen da.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlataformaMovil : MonoBehaviour
{
    [Header("Mugimendua")]
    [SerializeField] private Vector2 desplazamiento = new Vector2(0.5f, 0f);
    [SerializeField] private float velocidad = 0.35f;

    private Rigidbody2D cuerpo;
    private Vector2 posicionInicial;
    private float tiempoTranscurrido;

    /// <summary>
    /// Plataformaren osagai fisikoa eta hasierako
    /// posizioa gordetzen ditu.
    /// </summary>
    private void Awake()
    {
        // Plataformaren Rigidbody2D osagaia eskuratzen da.
        cuerpo = GetComponent<Rigidbody2D>();

        // Hasierako posizioa gordetzen da.
        posicionInicial = cuerpo.position;
    }

    /// <summary>
    /// Plataforma bi punturen artean mugitzen du,
    /// fisikaren eguneratze bakoitzean.
    /// </summary>
    private void FixedUpdate()
    {
        // Mugimenduaren distantzia kalkulatzen da.
        float distantzia = desplazamiento.magnitude;

        // Distantzia edo abiadura baliogabea bada, ez da mugitzen.
        if (distantzia <= 0f || velocidad <= 0f)
        {
            return;
        }

        // Mugimendurako igarotako denbora eguneratzen da.
        tiempoTranscurrido += Time.fixedDeltaTime;

        // Joan-etorriko distantzia kalkulatzen da.
        float ibilbidea = Mathf.PingPong(
            tiempoTranscurrido * velocidad,
            distantzia
        );

        // Plataformaren hurrengo posizioa kalkulatzen da.
        Vector2 helburua = posicionInicial +
            desplazamiento.normalized * ibilbidea;

        // Plataforma fisikaren bidez mugitzen da.
        cuerpo.MovePosition(helburua);
    }
}
