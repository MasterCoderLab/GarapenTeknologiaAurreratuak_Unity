using UnityEngine;

public class Disparo : MonoBehaviour
{
    [SerializeField] Transform prefabExplosion;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // Comprueba en cada fotograma si supera la altura de 5
        if (transform.position.y > 5)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.tag == "Enemigo")
        {
            Transform explosion = Instantiate(prefabExplosion, 
                otro.transform.position, 
                Quaternion.identity);
            Destroy(otro.gameObject);
            Destroy(explosion.gameObject, 1.5f);
            Destroy(gameObject);
        }
    }
}
