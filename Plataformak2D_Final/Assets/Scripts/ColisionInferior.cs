
using UnityEngine;

/// <summary>
/// Jokalaria beheko eremu arriskutsura erortzen denean,
/// hasierako posiziora itzultzeko prozesua kudeatzen du.
/// </summary>
public class ColisionInferior : MonoBehaviour
{
    /// <summary>
    /// Objektu bat beheko detekzio-eremuan sartzen denean
    /// exekutatzen da. Jokalaria den egiaztatu ondoren,
    /// hasierako posiziora eramaten du.
    /// </summary>
    /// <param name="other">
    /// Detekzio-eremuan sartu den objektuaren Collider2D osagaia.
    /// </param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Eremuan sartu den objektua jokalaria den egiaztatzen da.
        Player jokalaria = other.GetComponentInParent<Player>();

        // Objektua ez bada jokalaria, ez da inolako ekintzarik egiten.
        if (jokalaria == null)
        {
            return;
        }

        // Kontsolan erorketa gertatu dela adierazten da.
        Debug.Log("ColisionInferior: jokalaria beheko eremura erori da.");

        // Jokalaria hasierako posiziora itzultzen da.
        jokalaria.Recolocar();
    }
}
