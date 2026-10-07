using UnityEngine;

/// <summary>
/// Etsaiaren tiroa kudeatzen duen klasea.
/// Tiroaren talkak kontrolatzen ditu eta navea jotzen duenean
/// jokalariari bizitza bat kentzen dio.
/// </summary>
public class DisparoEnemigo : MonoBehaviour
{
    // =========================
    // 3. HOBEKUNTZA: ETSAIAREN TIROEK NAVEA JOTZEA
    // =========================

    /// <summary>
    /// Frame bakoitzean exekutatzen da.
    /// Tiroa pantailatik ateratzen bada, objektua ezabatzen da.
    /// </summary>
    void Update()
    {
        // Tiroa pantailaren behealdetik ateratzen bada, suntsitzen da.
        if (transform.position.y < -5f)
        {
            Destroy(gameObject);
        }
    }


    /// <summary>
    /// Tiroak beste Collider2D batekin talka egiten duenean exekutatzen da.
    /// Navea jotzen badu, bizitza bat kentzen dio eta tiroa suntsitzen du.
    /// </summary>
    /// <param name="otro">
    /// Tiroarekin talka egin duen objektuaren Collider2D-a.
    /// </param>
    private void OnTriggerEnter2D(Collider2D otro)
    {
        // Collider-a Nave objektuan edo bere seme batean egon daiteke.
        Nave nave = otro.GetComponentInParent<Nave>();

        // Navea aurkitzen bada, jokalariari kaltea egiten zaio.
        if (nave != null)
        {
            // =========================
            // 6. HOBEKUNTZA: BIZITZEN SISTEMA
            // =========================

            // Jokalariari bizitza bat kentzen zaio.
            nave.PerderVida();

            // Etsaiaren tiroa suntsitzen da.
            Destroy(gameObject);
        }
    }
}