using UnityEngine;

/// <summary>
/// Jokalariaren tiroa kudeatzen duen klasea.
/// Tiroaren talkak, etsaien suntsipena, leherketak eta puntuazioa kontrolatzen ditu.
/// </summary>
public class Disparo : MonoBehaviour
{
    // Etsaia suntsitzean sortuko den leherketaren prefaba.
    [SerializeField] Transform prefabExplosion;


    /// <summary>
    /// Frame bakoitzean exekutatzen da.
    /// Tiroa pantailaren goiko mugatik ateratzen bada, suntsitzen du.
    /// </summary>
    void Update()
    {
        // Tiroa pantailaren goiko aldetik ateratzen bada,
        // objektua ezabatzen da.
        if (transform.position.y > 5f)
        {
            Destroy(gameObject);
        }
    }


    /// <summary>
    /// Tiroak beste Collider2D batekin talka egiten duenean exekutatzen da.
    /// Etsaia bada, suntsitu, leherketa sortu eta puntuak gehitzen ditu.
    /// </summary>
    /// <param name="otro">
    /// Tiroarekin talka egin duen objektuaren Collider2D-a.
    /// </param>
    private void OnTriggerEnter2D(Collider2D otro)
    {
        // Talka egin duen objektua etsaia bada...
        if (otro.CompareTag("Enemigo"))
        {
            // =========================
            // 5. HOBEKUNTZA: PUNTUAZIO ERREALA
            // =========================

            // Eszenan dagoen jokalariaren navea bilatzen da.
            Nave nave = FindFirstObjectByType<Nave>();

            // Navea aurkitzen bada, etsaia suntsitzeagatik
            // 100 puntu gehitzen zaizkio jokalariari.
            if (nave != null)
            {
                nave.SumarPuntos(100);
            }


            // Etsaiaren posizioan leherketa sortzen da.
            Transform explosion = Instantiate(
                prefabExplosion,
                otro.transform.position,
                Quaternion.identity);

            // Etsaia suntsitzen da.
            Destroy(otro.gameObject);

            // Leherketa 1,5 segundo igaro ondoren suntsitzen da.
            Destroy(explosion.gameObject, 1.5f);

            // Jokalariaren tiroa suntsitzen da.
            Destroy(gameObject);
        }
    }
}