using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Game Over pantaila kudeatzen duen klasea.
/// Jokoa berriro hasteko aukera ematen du.
/// </summary>
public class GameOver : MonoBehaviour
{
    /// <summary>
    /// Nivel1 eszena berriro kargatzen du.
    /// </summary>
    public void Reintentar()
    {
        SceneManager.LoadScene("Nivel1");
    }
}