using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collision : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    /*
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("-----");
        Debug.Log(collision.gameObject.name);
    }
    */
    private void OnTriggerEnter(Collider collision)
    {
        Debug.Log("-----");
        Debug.Log(collision.gameObject.name);
    }
}
