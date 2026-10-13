
using UnityEngine;

/// <summary>
/// Jokalaria beheko eremu arriskutsura erortzen denean,
/// bizitza bat galtzeko eta hasierako posiziora
/// itzultzeko prozesua kudeatzen du.
/// </summary>
public class ColisionInferior : MonoBehaviour
{
    /// <summary>
    /// Objektu bat beheko detekzio-eremuan sartzen denean
    /// exekutatzen da. Jokalaria bada, bizitza bat galtzen du.
    /// </summary>
    /// <param name="other">
    /// Detekzio-eremuan sartu den objektuaren Collider2D osagaia.
    /// </param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Detekzio-eremuan sartu den objektua jokalaria den egiaztatzen da.
        Player jokalaria = other.GetComponentInParent<Player>();

        // Objektua ez bada jokalaria, ez da ekintzarik egiten.
        if (jokalaria == null)
        {
            return;
        }

        // Uneko mailaren GameController osagaia eskuratzen da.
        GameController kontrolatzailea =
            FindFirstObjectByType<GameController>();

        // Kontrolatzailea existitzen dela egiaztatzen da.
        if (kontrolatzailea == null)
        {
            Debug.LogError("GameController ez da aurkitu.");
            return;
        }

        // Jokalariaren erorketa kontsolan erregistratzen da.
        Debug.Log("ColisionInferior: jokalaria hutsera erori da.");

        // Bizitza bat kentzen da eta jokalaria berriro kokatzen da.
        // GameController arduratzen da HUDa eta Game Over kudeatzeaz.
        kontrolatzailea.PerderVida();
    }
}
