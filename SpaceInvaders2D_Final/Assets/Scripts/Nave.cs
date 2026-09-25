using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Nave : MonoBehaviour
{
    [SerializeField] float velocidad = 9;
    private float velocidadDisparo = 2;
    [SerializeField] Transform prefabDisparo;
    [SerializeField] UnityEngine.UI.Text textoPuntos;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }
    // Update is called once per frame
    void Update()
    {
      
        float horizontal = Input.GetAxis("Horizontal");
        transform.Translate(
            horizontal * velocidad * Time.deltaTime, 
            0, 0);

        if (Input.GetButtonDown("Fire1"))
        {
            textoPuntos.text = "Has disparado";
            GetComponent<AudioSource>().Play();
            Transform disparo = Instantiate(prefabDisparo, transform.position, Quaternion.identity);
            disparo.gameObject.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0, velocidadDisparo);
        }
    
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Pum");
    }
}
