
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu nagusiko Jugar botoiaren funtzionamendua kudeatzen du.
/// Partida berri bat hastean jokoaren datuak berrezartzen ditu
/// eta lehenengo maila kargatzen du.
/// </summary>
public class BotonJugar : MonoBehaviour
{
    /// <summary>
    /// Jokalariak Jugar botoia sakatzen duenean exekutatzen da.
    /// Puntuazioa, bizitzak eta uneko maila hasieratzen ditu,
    /// eta Nivel1 eszena kargatzen du.
    /// </summary>
    public void LanzarJuego()
    {
        // Jokoaren egoera orokorra eskuratzen da.
        GameStatus gameStatus = FindFirstObjectByType<GameStatus>();

        // GameStatus objektua existitzen dela egiaztatzen da.
        if (gameStatus == null)
        {
            Debug.LogError(
                "Ezin da partida hasi: GameStatus objektua ez da aurkitu.");
            return;
        }

        // Partida berriaren puntuazioa berrezartzen da.
        gameStatus.puntos = 0;

        // Hasierako hiru bizitzak ezartzen dira.
        gameStatus.vidas = 3;

        // Lehenengo maila ezartzen da.
        gameStatus.nivelActual = 1;

        // Partida berriaren lehenengo eszena kargatzen da.
        SceneManager.LoadScene("Nivel1");
    }
}
