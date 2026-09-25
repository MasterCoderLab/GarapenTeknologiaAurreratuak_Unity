using System.Collections;
using UnityEngine;

public class Enemigo : MonoBehaviour
{
    private float velocidadX = 2f;
    private float velocidadY = 1.2f;
    private float velocidadDisparo = -2;
    [SerializeField] Transform prefabDisparo = null;

    void Start()
    {
        StartCoroutine(Disparar());
    }

    void Update()
    {
        transform.Translate(
           velocidadX * Time.deltaTime,
           velocidadY * Time.deltaTime,
           0);

        if ((transform.position.x < -4) || (transform.position.x > 4))
            velocidadX = -velocidadX;

        if ((transform.position.y < -2.5) || (transform.position.y > 2.5))
            velocidadY = -velocidadY;
    }

    IEnumerator Disparar()
    {
        while (true)
        {
            float pausa = Random.Range(3, 7.5F);
            yield return new WaitForSeconds(pausa);
            Transform disparo = Instantiate(prefabDisparo, transform.position, Quaternion.identity);
            disparo.gameObject.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0, velocidadDisparo);
        }
    }
}