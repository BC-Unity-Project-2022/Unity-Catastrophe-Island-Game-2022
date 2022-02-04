using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainTornado : MonoBehaviour
{
    public Transform tornadoCenter;
    public float pullforce;
    public float refreshRate;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "OJB")
        {
            StartCoroutine(pullObject(other, true));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "OJB")
        {
            StartCoroutine(pullObject(other, false));
        }
    }

    IEnumerator pullObject(Collider x, bool shouldPull)
    {
        if (shouldPull)
        {
            Vector3 ForeDir = tornadoCenter.position - x.transform.position;
            x.GetComponent<Rigidbody>().AddForce(ForeDir.normalized * pullforce * Time.deltaTime);
            yield return refreshRate;
            StartCoroutine(pullObject(x, shouldPull));

        }
    }
    // Range over which perlin varies.
    float perlinScale = 1.0f;

    // Distance covered per second along X axis of Perlin plane.
    public float xScale = 1.0f;

    public float speed = 2;

    private float halfMap = 125;

    public ParticleSystem ps;

    public bool disabled = false;

    void Start()
    {
        var main = ps.main;
        main.loop = true;
    }

    void Update()
    {
        float perlin = perlinScale * Mathf.PerlinNoise(Time.time * xScale, 0.0f);

        //Y coordinate
        Vector3 pos = transform.position;
        pos.y = Terrain.activeTerrain.SampleHeight(transform.position);
        transform.position = pos;

        //Facing direction
        Vector3 direction = Vector3.forward;
        direction = Quaternion.Euler(0, perlin * 360, 0) * direction;

        //Move forward
        transform.position += (direction * speed * Time.deltaTime);

        transform.position = new Vector3(transform.position.x, pos.y, transform.position.z);

        if (transform.position.x > halfMap | transform.position.x < -halfMap)
        {
            ps.Stop(true);
            disabled = true;
        }
        if (transform.position.z > halfMap | transform.position.z < -halfMap)
        {
            ps.Stop(true);
            disabled = true;
        }
    }
}