using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerlinMove : MonoBehaviour
{
    // "Bobbing" animation from 1D Perlin noise.

    // Range over which height varies.
    float heightScale = 1.0f;

    // Distance covered per second along X axis of Perlin plane.
    public float xScale = 1.0f;

    public float speed = 2;

    public float degreesPerSecond = 20;

    private float halfMap = 125;

    void Update()
    {
        float height = heightScale * Mathf.PerlinNoise(Time.time * xScale, 0.0f);
        //Vector3 pos = transform.position;
        //pos.y = height;
        //transform.position = pos;

        Vector3 direction = Vector3.forward;

        direction = Quaternion.Euler(0, height * 360, 0) * direction;

        //Debug.Log(height * 360);
        //transform.Rotate(new Vector3(0, height, 0));

        //transform.Translate(0, 0, 1);

        transform.position += direction * speed * Time.deltaTime;

        
        //Debug.Log("X = ");
        //Debug.Log(transform.position.x);
        //Debug.Log("Y = ");
        //Debug.Log(transform.position.y);
        //Debug.Log("Z = ");
        //Debug.Log(transform.position.z);

        if (transform.position.x > halfMap | transform.position.x < -halfMap)
        {
            Debug.Log("5");
        }
        if (transform.position.z > halfMap | transform.position.z < -halfMap)
        {
            Debug.Log("5");
        }
    }
}
