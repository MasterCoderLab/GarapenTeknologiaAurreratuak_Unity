using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Jokoaren musika eta soinu-efektuak kudeatzen ditu.
/// Eszena-aldaketetan mantentzen da eta audio-fitxategiak
/// Resources/Audio karpetatik automatikoki kargatzen ditu.
/// </summary>
public class AudioJuego : MonoBehaviour
{
    // Joko osoan audio-kudeatzaile bakarra mantentzen da.
    public static AudioJuego Instancia { get; private set; }

    private AudioSource musikaIturria;
    private AudioSource efektuIturria;

    private AudioClip soinuSalto;
    private AudioClip soinuGameOver;
    private AudioClip soinuVictoria;
    private AudioClip musikaFondo;

    /// <summary>
    /// Lehenengo eszena kargatu aurretik audio-kudeatzailea sortzen du.
    /// Horrela, jokoa edozein eszenatatik has daiteke.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SortuAudioKudeatzailea()
    {
        if (Instancia == null)
        {
            GameObject objektua = new GameObject("AudioJuego");
            objektua.AddComponent<AudioJuego>();
        }
    }

    /// <summary>
    /// Soinuak kargatzen ditu, audio-iturriak prestatzen ditu
    /// eta eszena-aldaketak kontrolatzeko gertaera erregistratzen du.
    /// </summary>
    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        Instancia = this;
        DontDestroyOnLoad(gameObject);

        // Audio-fitxategiak proiektuaren Resources/Audio karpetatik kargatzen dira.
        soinuSalto = Resources.Load<AudioClip>("Audio/Salto");
        soinuGameOver = Resources.Load<AudioClip>("Audio/GameOver");
        soinuVictoria = Resources.Load<AudioClip>("Audio/Victoria");
        musikaFondo = Resources.Load<AudioClip>("Audio/MusicaFondo");

        // Musika jarraituarentzako audio-iturria konfiguratzen da.
        musikaIturria = gameObject.AddComponent<AudioSource>();
        musikaIturria.clip = musikaFondo;
        musikaIturria.loop = true;
        musikaIturria.playOnAwake = false;
        musikaIturria.spatialBlend = 0f;
        musikaIturria.volume = 0.15f;

        // Efektuentzako beste audio-iturri bat prestatzen da.
        efektuIturria = gameObject.AddComponent<AudioSource>();
        efektuIturria.playOnAwake = false;
        efektuIturria.spatialBlend = 0f;
        efektuIturria.volume = 0.75f;

        SceneManager.sceneLoaded += EszenaKargatzean;
    }

    /// <summary>
    /// Eszenaren arabera musika edo garaipen-soinua erreproduzitzen du.
    /// </summary>
    /// <param name="eszena">Kargatu berri den eszena.</param>
    /// <param name="modua">Eszena kargatzeko erabilitako modua.</param>
    private void EszenaKargatzean(Scene eszena, LoadSceneMode modua)
    {
        if (eszena.name == "Victoria")
        {
            // Garaipen-pantailan hondoko musika gelditzen da.
            musikaIturria.Stop();
            efektuIturria.Stop();

            if (soinuVictoria != null)
            {
                efektuIturria.PlayOneShot(soinuVictoria);
            }
        }
        else if (musikaFondo != null && !musikaIturria.isPlaying)
        {
            // Menuan eta maila guztietan hondoko musika mantentzen da.
            efektuIturria.Stop();
            musikaIturria.Play();
        }
    }

    /// <summary>
    /// Jokalariak benetako jauzi bat egiten duenean
    /// dagokion soinu-efektua erreproduzitzen du.
    /// </summary>
    public void ReproducirSalto()
    {
        if (soinuSalto != null)
        {
            efektuIturria.PlayOneShot(soinuSalto);
        }
    }

    /// <summary>
    /// Bizitzak agortzean hondoko musika eten
    /// eta Game Over soinua erreproduzitzen du.
    /// </summary>
    public void ReproducirGameOver()
    {
        musikaIturria.Pause();

        if (soinuGameOver != null)
        {
            efektuIturria.PlayOneShot(soinuGameOver);
        }
    }

    /// <summary>
    /// Audio-kudeatzailea desagertzean eszenaren
    /// gertaerarekiko harpidetza kentzen du.
    /// </summary>
    private void OnDestroy()
    {
        if (Instancia == this)
        {
            SceneManager.sceneLoaded -= EszenaKargatzean;
            Instancia = null;
        }
    }
}
