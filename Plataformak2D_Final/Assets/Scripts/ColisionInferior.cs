
using UnityEngine;

/// <summary>
/// Jokalaria beheko eremu arriskutsura erortzen denean,
/// bizitza bat galtzeko eta hasierako posiziora
/// itzultzeko prozesua kudeatzen du.
/// </summary>
public class ColisionInferior : MonoBehaviour
{
    // Erorketa bera behin baino gehiagotan ez zenbatzeko.
    private float azkenErorketa = -Mathf.Infinity;

    // Bi erorketa erregistratzeko gutxieneko denbora-tartea.
    private const float ErorketenArtekoDenbora = 1f;

    /// <summary>
    /// Objektu bat beheko detekzio-eremuan sartzen denean
    /// exekutatzen da. Jokalaria bada, bizitza bat kentzen dio
    /// eta hasierako posiziora itzultzen du.
    /// </summary>
    /// <param name="other">
    /// Detekzio-eremuan sartu den objektuaren Collider2D osagaia.
    /// </param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Eremuan sartu den objektua jokalaria den egiaztatzen da.
        Player jokalaria = other.GetComponentInParent<Player>();

        // Objektua ez bada jokalaria, ez da ekintzarik egiten.
        if (jokalaria == null)
        {
            return;
        }

        // Erorketa bera behin baino gehiagotan ez zenbatzea.
        if (Time.unscaledTime - azkenErorketa <
            ErorketenArtekoDenbora)
        {
            return;
        }

        // Uneko mailaren GameController osagaia bilatzen da.
        GameController kontrolatzailea =
            FindFirstObjectByType<GameController>();

        // Kontrolatzailea existitzen dela egiaztatzen da.
        if (kontrolatzailea == null)
        {
            Debug.LogError("ColisionInferior: GameController ez da aurkitu.");
            return;
        }

        // Erorketaren unea gordetzen da.
        azkenErorketa = Time.unscaledTime;

        // Erorketa kontsolan erregistratzen da.
        Debug.Log("ColisionInferior: jokalaria erori da.");

        // Bizitza bat kentzen da.
        // GameController arduratzen da HUDa eguneratzeaz,
        // jokalaria berriro kokatzeaz eta Game Over kudeatzeaz.
        kontrolatzailea.PerderVida();
    }
}
