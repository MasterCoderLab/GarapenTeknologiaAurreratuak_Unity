
using UnityEngine;

/// <summary>
/// Jokoaren egoera orokorra kudeatzen du.
/// Puntuazioa, bizitzak eta mailaren informazioa
/// eszena-aldaketetan mantentzen ditu.
/// </summary>
public class GameStatus : MonoBehaviour
{
    // Joko osoan erabiliko den instantzia bakarra.
    public static GameStatus Instancia { get; private set; }

    // Jokalariaren datuak.
    public int puntos = 0;
    public int vidas = 3;

    // Mailen informazioa.
    public int nivelActual = 1;
    public int nivelMasAlto = 3;

    /// <summary>
    /// GameStatus instantzia bakarra mantentzen du
    /// eta eszena berrietan datuak ez galtzea ziurtatzen du.
    /// </summary>
    private void Awake()
    {
        // Beste instantzia bat badago, berria ezabatzen da.
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        // Instantzia nagusia gordetzen da.
        Instancia = this;

        // Objektua eszena-aldaketetan mantentzen da.
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Instantzia nagusia suntsitzen denean
    /// erreferentzia estatikoa garbitzen du.
    /// </summary>
    private void OnDestroy()
    {
        if (Instancia == this)
        {
            Instancia = null;
        }
    }
}
