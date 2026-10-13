
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Jokoaren puntuazioa, bizitzak, diamanteak eta mailen arteko
/// trantsizioak kudeatzen ditu.
/// </summary>
public class GameController : MonoBehaviour
{
    private int itemsRestantes;
    private int puntos;
    private int vidas;
    private int nivelActual;

    private GameStatus gameStatus;

    [Header("Game Over")]
    [SerializeField] private Text textoGameOver;

    [Header("HUD")]
    [SerializeField] private Text textoPuntuak;
    [SerializeField] private Text textoDiamanteak;
    [SerializeField] private Text textoBizitzak;

    /// <summary>
    /// Maila hastean, jokoaren datuak berreskuratzen ditu,
    /// uneko maila identifikatzen du eta HUDa eguneratzen du.
    /// </summary>
    private void Start()
    {
        // Jokoaren egoera orokorra eskuratzen da.
        gameStatus = FindFirstObjectByType<GameStatus>();

        // Mailako diamante kopurua kalkulatzen da.
        itemsRestantes = FindObjectsByType<Diamante>(
            FindObjectsSortMode.None).Length;

        // Aurreko mailetako puntuazioa eta bizitzak berreskuratzen dira.
        puntos = gameStatus.puntos;
        vidas = gameStatus.vidas;

        // Une honetan kargatuta dagoen eszenaren izena lortzen da.
        string escenaIzena = SceneManager.GetActiveScene().name;

        // Eszenaren izenetik mailaren zenbakia identifikatzen da.
        if (escenaIzena.StartsWith("Nivel") &&
            int.TryParse(escenaIzena.Substring(5), out int mailaZenbakia))
        {
            // Uneko maila benetako eszenaren arabera ezartzen da.
            nivelActual = mailaZenbakia;

            // Jokoaren egoera orokorra sinkronizatzen da.
            gameStatus.nivelActual = nivelActual;
        }
        else
        {
            // Eszenaren izena egokia ez bada, gordetako maila erabiltzen da.
            nivelActual = gameStatus.nivelActual;
        }

        // Game Over mezua hasieran ezkutatzen da.
        textoGameOver.enabled = false;

        // HUDaren hasierako balioak erakusten dira.
        ActualizarHUD();
    }

    /// <summary>
    /// Garapen-fasean N teklaren bidez hurrengo mailara
    /// igarotzeko aukera ematen du.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            AvanzarNivel();
        }
    }

    /// <summary>
    /// Jokalariak diamante bat jasotzen duenean puntuazioa
    /// eta geratzen diren diamanteen kopurua eguneratzen ditu.
    /// </summary>
    public void AnotarItemRecogido()
    {
        // Jasotako diamante bakoitzak hamar puntu ematen ditu.
        puntos += 10;
        gameStatus.puntos = puntos;

        // Mailan geratzen diren diamanteen kopurua murrizten da.
        itemsRestantes--;

        Debug.Log("Puntos: " + puntos);
        Debug.Log("Items restantes: " + itemsRestantes);

        // Pantailako datuak eguneratzen dira.
        ActualizarHUD();

        // Diamante guztiak jasotzean hurrengo mailara igarotzen da.
        if (itemsRestantes <= 0)
        {
            AvanzarNivel();
        }
    }

    /// <summary>
    /// Jokalariak bizitza bat galtzen duenean egoera
    /// eguneratzen du eta pertsonaia berriro kokatzen du.
    /// </summary>
    public void PerderVida()
    {
        // Jokalariaren bizitza kopurua murrizten da.
        vidas--;
        gameStatus.vidas = vidas;

        // Jokalaria hasierako posiziora itzultzen da.
        FindFirstObjectByType<Player>().Recolocar();

        Debug.Log("Vidas: " + vidas);

        // Bizitzen kopurua pantailan eguneratzen da.
        ActualizarHUD();

        // Bizitzak agortu badira, partida amaitzen da.
        if (vidas <= 0)
        {
            StartCoroutine(TerminarPartida());
        }
    }

    /// <summary>
    /// HUDaren testuak jokoaren uneko balioekin eguneratzen ditu.
    /// </summary>
    private void ActualizarHUD()
    {
        // Jokalariaren puntuazioa erakusten da.
        if (textoPuntuak != null)
        {
            textoPuntuak.text = "Puntuak: " + puntos;
        }

        // Geratzen diren diamanteen kopurua erakusten da.
        if (textoDiamanteak != null)
        {
            textoDiamanteak.text = "Diamanteak: " + itemsRestantes;
        }

        // Jokalariaren bizitza kopurua erakusten da.
        if (textoBizitzak != null)
        {
            textoBizitzak.text = "Bizitzak: " + vidas;
        }
    }

    /// <summary>
    /// Game Over mezua erakusten du eta hiru segundoren
    /// ondoren menu nagusira itzultzen da.
    /// </summary>
    /// <returns>
    /// Partida amaitzeko korutinaren exekuzioa.
    /// </returns>
    private IEnumerator TerminarPartida()
    {
        // Partida amaitu dela adierazten duen mezua erakusten da.
        textoGameOver.enabled = true;

        // Jokoaren abiadura moteltzen da.
        Time.timeScale = 0.1f;

        // Hiru segundo itxaroten dira, denbora-eskala kontuan hartu gabe.
        yield return new WaitForSecondsRealtime(3f);

        // Jokoaren abiadura normala berreskuratzen da.
        Time.timeScale = 1f;

        // Menu nagusira itzultzen da.
        SceneManager.LoadScene("Menu");
    }

    /// <summary>
    /// Hurrengo mailara igarotzen da. Azken maila gainditzean,
    /// lehenengo mailara itzultzen da.
    /// </summary>
    public void AvanzarNivel()
    {
        // Uneko mailaren zenbakia handitzen da.
        nivelActual++;

        // Azken maila gainditu bada, lehenengo mailara itzultzen da.
        if (nivelActual > gameStatus.nivelMasAlto)
        {
            nivelActual = 1;
        }

        // Hurrengo maila jokoaren egoera orokorrean gordetzen da.
        gameStatus.nivelActual = nivelActual;

        // Dagokion eszena kargatzen da.
        SceneManager.LoadScene("Nivel" + nivelActual);
    }
}
