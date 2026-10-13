
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Garaipen-pantaila kudeatzen du.
/// Azken puntuazioa erakusten du eta menu nagusira
/// itzultzeko aukera ematen du.
/// </summary>
public class PantallaVictoria : MonoBehaviour
{
    [Header("Garaipen-pantaila")]
    [SerializeField] private Text textoVictoria;
    [SerializeField] private Button botonMenu;

    /// <summary>
    /// Garaipen-pantaila hasieratzen du eta
    /// jokalariaren azken puntuazioa erakusten du.
    /// </summary>
    private void Start()
    {
        // Jokoaren egoera orokorra bilatzen da.
        GameStatus gameStatus = FindFirstObjectByType<GameStatus>();

        // Gordetako puntuazioa eskuratzen da.
        int puntuak = gameStatus != null ? gameStatus.puntos : 0;

        // Garaipen-mezua azken puntuazioarekin eguneratzen da.
        textoVictoria.text =
            "ZORIONAK!\n\n" +
            "Hiru mailak gainditu dituzu!\n\n" +
            "Puntuak: " + puntuak;

        // Botoia menu nagusira itzultzeko prestatzen da.
        botonMenu.onClick.AddListener(ItzuliMenura);
    }

    /// <summary>
    /// Menu nagusiko eszena kargatzen du.
    /// </summary>
    private void ItzuliMenura()
    {
        SceneManager.LoadScene("Menu");
    }
}
