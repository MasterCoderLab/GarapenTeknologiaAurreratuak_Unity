using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BotonBienvenida : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void LanzarJuego()
    {
        SceneManager.LoadScene("Nivel1");
    }
}
