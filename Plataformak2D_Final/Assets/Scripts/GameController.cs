
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

    // Jokalariak kaltea jasotzean erabiliko den soinua.
    private AudioClip sonidoDano;

    [Header("Game Over")]
    [SerializeField] private Text textoGameOver;

    [Header("HUD")]
    [SerializeField] private Text textoPuntuak;
    [SerializeField] private Text textoDiamanteak;
    [SerializeField] private Text textoBizitzak;

    /// <summary>
    /// Maila hastean, jokoaren egoera iraunkorra eskuratzen du,
    /// uneko maila identifikatzen du, soinuak prestatzen ditu
    /// eta HUDa eguneratzen du.
    /// </summary>
    private void Start()
    {
        // Eszena guztietan mantentzen den GameStatus instantzia eskuratzen da.
        gameStatus = GameStatus.Instancia;

        // GameStatus instantzia existitzen dela egiaztatzen da.
        if (gameStatus == null)
        {
            Debug.LogError("GameStatus instantzia ez da aurkitu.");
            enabled = false;
            return;
        }

        // Kaltearen soinu-efektua Resources karpetatik kargatzen da.
        sonidoDano = Resources.Load<AudioClip>("Audio/Dano");

        // Soinua aurkitu ez bada, abisua erakusten da.
        if (sonidoDano == null)
        {
            Debug.LogWarning("Kaltearen soinua ez da aurkitu: Audio/Dano");
        }

        // Mailako diamante kopurua kalkulatzen da.
        itemsRestantes = FindObjectsByType<Diamante>(
            FindObjectsSortMode.None).Length;

        // Gordetako puntuazioa eta bizitzak berreskuratzen dira.
        puntos = gameStatus.puntos;
        vidas = gameStatus.vidas;

        // Une honetan kargatuta dagoen eszenaren izena lortzen da.
        string escenaIzena = SceneManager.GetActiveScene().name;

        // Eszenaren izenetik mailaren zenbakia identifikatzen da.
        if (escenaIzena.StartsWith("Nivel") &&
            int.TryParse(escenaIzena.Substring(5), out int mailaZenbakia))
        {
            // Benetako eszenaren arabera uneko maila ezartzen da.
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
        if (textoGameOver != null)
        {
            textoGameOver.enabled = false;
        }

        // HUDaren hasierako balioak erakusten dira.
        ActualizarHUD();

        // Gordetako puntuazioa kontsolan erakusten da.
        Debug.Log("Hasierako puntuak: " + gameStatus.puntos);
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
    /// egoera iraunkorrean gordetzen du eta HUDa eguneratzen du.
    /// </summary>
    public void AnotarItemRecogido()
    {
        // GameStatus instantzia iraunkorra eskuratzen da.
        GameStatus egoera = GameStatus.Instancia;

        // Instantzia existitzen dela egiaztatzen da.
        if (egoera == null)
        {
            Debug.LogError("GameStatus instantzia ez da aurkitu.");
            return;
        }

        // Diamante bakoitzak hamar puntu ematen ditu.
        // Puntuak zuzenean egoera iraunkorrean gordetzen dira.
        egoera.puntos += 10;

        // HUDaren puntuazioa gordetako balioarekin sinkronizatzen da.
        puntos = egoera.puntos;

        // Geratzen diren diamanteen kopurua murrizten da.
        itemsRestantes--;

        // Gordetako puntuazioa kontsolan erakusten da.
        Debug.Log("Puntuak gordeta: " + egoera.puntos);
        Debug.Log("Geratzen diren diamanteak: " + itemsRestantes);

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
    /// iraunkorra eguneratzen du, kaltearen soinua erreproduzitzen du
    /// eta pertsonaia berriro kokatzen du.
    /// </summary>
    public void PerderVida()
    {
        // GameStatus instantzia iraunkorra eskuratzen da.
        GameStatus egoera = GameStatus.Instancia;

        // Instantzia existitzen dela egiaztatzen da.
        if (egoera == null)
        {
            Debug.LogError("GameStatus instantzia ez da aurkitu.");
            return;
        }

        // Jokalariaren bizitza kopurua murrizten da.
        egoera.vidas--;

        // HUDaren bizitzak gordetako balioarekin sinkronizatzen dira.
        vidas = egoera.vidas;

        // Bizitzak geratzen badira, kaltearen soinua erreproduzitzen da.
        // Azken bizitza galtzean, Game Over soinua erabiliko da.
        if (vidas > 0 && sonidoDano != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(
                sonidoDano,
                Camera.main.transform.position,
                0.7f
            );
        }

        // Jokalaria hasierako posiziora itzultzen da.
        Player jokalaria = FindFirstObjectByType<Player>();

        if (jokalaria != null)
        {
            jokalaria.Recolocar();
        }

        Debug.Log("Bizitzak: " + vidas);

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
    /// Game Over mezua erakusten du, soinu-efektua
    /// erreproduzitzen du eta hiru segundoren ondoren
    /// menu nagusira itzultzen da.
    /// </summary>
    /// <returns>
    /// Partida amaitzeko korutinaren exekuzioa.
    /// </returns>
    private IEnumerator TerminarPartida()
    {
        // Partida amaitu dela adierazten duen mezua erakusten da.
        if (textoGameOver != null)
        {
            textoGameOver.enabled = true;
        }

        // Game Over soinu-efektua erreproduzitzen da.
        if (AudioJuego.Instancia != null)
        {
            AudioJuego.Instancia.ReproducirGameOver();
        }

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
    /// Hurrengo mailara igarotzen da.
    /// Azken maila gainditzean garaipen-pantaila kargatzen du.
    /// </summary>
    public void AvanzarNivel()
    {
        // GameStatus instantzia iraunkorra eskuratzen da.
        GameStatus egoera = GameStatus.Instancia;

        // Instantzia existitzen dela egiaztatzen da.
        if (egoera == null)
        {
            Debug.LogError("GameStatus instantzia ez da aurkitu.");
            return;
        }

        // Azken maila gainditu bada, garaipen-pantaila kargatzen da.
        if (nivelActual >= egoera.nivelMasAlto)
        {
            SceneManager.LoadScene("Victoria");
            return;
        }

        // Hurrengo mailaren zenbakia kalkulatzen da.
        nivelActual++;

        // Jokoaren egoera orokorra eguneratzen da.
        egoera.nivelActual = nivelActual;

        // Hurrengo mailari dagokion eszena kargatzen da.
        SceneManager.LoadScene("Nivel" + nivelActual);
    }
}
