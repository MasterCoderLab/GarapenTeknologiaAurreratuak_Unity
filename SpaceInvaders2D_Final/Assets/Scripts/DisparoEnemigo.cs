using UnityEngine;

/// <summary>
/// Etsaiaren tiroa kudeatzen duen klasea.
/// Tiroaren talkak kontrolatzen ditu eta navea jotzen duenean suntsitzen du.
/// </summary>
public class DisparoEnemigo : MonoBehaviour
{
    // =========================
    // 3. HOBEKUNTZA: ETSAIAREN TIROEK NAVEA SUNTSITZEA
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
    /// Navea jotzen badu, navea eta tiroa suntsitzen dira.
    /// </summary>
    /// <param name="otro">
    /// Tiroarekin talka egin duen objektuaren Collider2D-a.
    /// </param>
    private void OnTriggerEnter2D(Collider2D otro)
    {
        // Talka egin duen objektuak Nave script-a badu,
        // jokalariaren navea dela esan nahi du.
        if (otro.GetComponent<Nave>() != null)
        {
            // Jokalariaren navea suntsitzen da.
            Destroy(otro.gameObject);

            // Etsaiaren tiroa ere suntsitzen da.
            Destroy(gameObject);
        }
    }
}