
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Jokoaren pausa-sistema kudeatzen du.
/// ESC teklaren bidez partida gelditu eta
/// berriro jarraitzeko aukera ematen du.
/// </summary>
public class PausaJuego : MonoBehaviour
{
    // Pausaren egoera gordetzen da.
    private bool pausatuta = false;

    [Header("Pausa")]
    [SerializeField] private Text textoPausa;

    /// <summary>
    /// Jokoa hastean pausaren mezua ezkutatzen du.
    /// </summary>
    private void Start()
    {
        if (textoPausa != null)
        {
            textoPausa.enabled = false;
        }
    }

    /// <summary>
    /// ESC tekla sakatzen denean pausaren egoera aldatzen du.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            AldatuPausa();
        }
    }

    /// <summary>
    /// Jokoaren denbora, audioa eta pausa-mezua
    /// uneko egoeraren arabera eguneratzen ditu.
    /// </summary>
    private void AldatuPausa()
    {
        // Pausaren egoera aldatzen da.
        pausatuta = !pausatuta;

        // Jokoaren denbora gelditzen edo berrezartzen da.
        Time.timeScale = pausatuta ? 0f : 1f;

        // Soinuak pausatu edo berriro aktibatzen dira.
        AudioListener.pause = pausatuta;

        // Pausa-mezua erakusten edo ezkutatzen da.
        if (textoPausa != null)
        {
            textoPausa.enabled = pausatuta;
        }
    }

    /// <summary>
    /// Osagaia desgaitzean jokoaren egoera normala
    /// berrezartzen du.
    /// </summary>
    private void OnDisable()
    {
        pausatuta = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (textoPausa != null)
        {
            textoPausa.enabled = false;
        }
    }
}
