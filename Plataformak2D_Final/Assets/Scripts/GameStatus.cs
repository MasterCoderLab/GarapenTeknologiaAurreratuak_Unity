
using UnityEngine;

/// <summary>
/// Jokoaren egoera orokorra gordetzen du.
/// Puntuazioa, bizitzak eta mailaren informazioa
/// eszena desberdinen artean mantentzen ditu.
/// </summary>
public class GameStatus : MonoBehaviour
{
    // Joko osoan GameStatus instantzia bakarra mantentzen da.
    private static GameStatus instancia;

    // Jokalariaren hasierako datuak.
    public int puntos = 0;
    public int vidas = 3;

    // Mailen informazioa.
    public int nivelActual = 1;
    public int nivelMasAlto = 3;

    /// <summary>
    /// GameStatus instantzia bakarra dagoela ziurtatzen du.
    /// Eszena aldatzean jokoaren datuak mantentzen ditu.
    /// </summary>
    private void Awake()
    {
        // Aurretik beste instantzia bat badago, berria ezabatzen da.
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        // Instantzia nagusia gordetzen da.
        instancia = this;

        // Objektua eszena-aldaketetan ez suntsitzeko ezartzen da.
        DontDestroyOnLoad(gameObject);
    }
}
