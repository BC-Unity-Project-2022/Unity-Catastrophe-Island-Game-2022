using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;

public class TestingTornadoes : MonoBehaviour
{
    public bool isActive = true;

    [SerializeField] private GameObject tornadoParticles;
    [SerializeField] private float movementSpeed = 10;
    [SerializeField] private float pullingStrength = 5000;
    [SerializeField] private float centreOffset = 10;
    [SerializeField] private Vector3 startingCoordinates = new Vector3(250, 50, 250);

    private Vector3 currentTargetCoordinates;
    private ParticleSystem tornadoParticleSystem;
    private Transform tornadoTransform;
    private Collider tornadoCollider;
    private List<Collider> collidersInRange = new List<Collider>();
    private System.Random random = new System.Random();

    private void OnTriggerEnter(Collider other)
    {
        if (isActive)
        {
            collidersInRange.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isActive)
        {
            collidersInRange.Remove(other);
        }
    }

    private void PullObject(Collider target)
    {
        Vector3 forceDirection = Quaternion.Euler(0, 30, 0) * (tornadoTransform.position - target.transform.position + new Vector3(0, centreOffset, 0)); ;
        target.GetComponent<Rigidbody>().AddForce(pullingStrength * forceDirection * Time.deltaTime);
    }

    void Start()
    {
        tornadoTransform = gameObject.GetComponent<Transform>();
        tornadoParticleSystem = tornadoParticles.GetComponent<ParticleSystem>();
        tornadoCollider = gameObject.GetComponent<Collider>();

        var psMain = tornadoParticleSystem.main;
        psMain.loop = true;

        currentTargetCoordinates = startingCoordinates;
        StartCoroutine(GenerateNewTargetCoordinates());
    }

    private void Move()
    {
        Vector3 direction = currentTargetCoordinates - transform.position;

        Vector3 pos = transform.position;
        pos.y = Terrain.activeTerrain.SampleHeight(transform.position);
        transform.position = pos;

        transform.position += (direction.normalized * movementSpeed * Time.deltaTime);
    }

    private IEnumerator GenerateNewTargetCoordinates()
    {
        for (; ; )
        {
            int previousX = (int)currentTargetCoordinates.x;
            int previousZ = (int)currentTargetCoordinates.z;

            int x = random.Next((int)(previousX + Math.Round((double)(0 - previousX / (450 / 38)))), (int)(previousX + Math.Round((double)(40 - previousX / (450 / 38)))));
            int z = random.Next((int)(previousZ + Math.Round((double)(0 - previousZ / (450 / 38)))), (int)(previousZ + Math.Round((double)(40 - previousZ / (450 / 38)))));

            currentTargetCoordinates = new Vector3(x, 0, z);

            yield return new WaitForSeconds((float)random.NextDouble() * 5);
        }
    }

    private void PullObjects()
    {
        foreach (Collider collider in collidersInRange)
        {
            PullObject(collider);
        }
    }

    void Update()
    {
        if (!isActive)
        {
            tornadoCollider.enabled = false;
            return;
        }
        tornadoCollider.enabled = true;

        PullObjects();
        Move();
    }
}
