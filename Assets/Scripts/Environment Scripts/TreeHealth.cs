using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeHealth : MonoBehaviour
{
    public float health = 100;


    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Damage(float dmg)
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.GetComponent<Rigidbody>() == null)
        {
            if (health <= 0)
            {
                Rigidbody gameObjectsRigidBody = gameObject.AddComponent<Rigidbody>();
            }
        }
    }
}
