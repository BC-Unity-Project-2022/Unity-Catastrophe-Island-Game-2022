using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System;


public class Tornadoes : MonoBehaviour
{
    [SerializeField] private GameObject tornadoParticles;
    [SerializeField] private float movementSpeed = 10;
    [SerializeField] private float pullingStrengthModifier = 100;
    [SerializeField] private float centreOffset = 10;
    [SerializeField] private Vector3 middleCoordinates = new Vector3(250, 50, 250);
    [SerializeField] private List<Vector3> possibleStartingCoordinates = new List<Vector3>() {
        new Vector3(70, 25, 70),
        new Vector3(70, 25, 430),
        new Vector3(430, 25, 70),
        new Vector3(430, 25, 430),
        new Vector3(70, 25, 250),
        new Vector3(250, 25, 70),
        new Vector3(430, 25, 250),
        new Vector3(250, 25, 430),
        new Vector3(250, 50, 250)
    };

    private float pullingStrength = 5f;
    private bool isActive = false;
    private List<Collider> collidersInRange = new List<Collider>();
    private System.Random random = new System.Random();
    private Vector3 currentTargetCoordinates;
    private ParticleSystem tornadoParticleSystem;
    private Transform tornadoTransform;
    private Collider tornadoCollider;
    private Tornado currentTornado;
    private float lastStartTime;
    private int executionLength;
    private Terrain currentTerrain;

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
        currentTerrain = GameObject.FindGameObjectWithTag("Ground").GetComponent<Terrain>();

        var psMain = tornadoParticleSystem.main;
        psMain.loop = true;
        tornadoParticleSystem.Stop();

        currentTargetCoordinates = middleCoordinates;
        StartCoroutine(GenerateNewTargetCoordinates());
    }

    private void Move()
    {
        Vector3 direction = currentTargetCoordinates - transform.position;

        Vector3 pos = transform.position;
        pos.y = currentTerrain.SampleHeight(transform.position);
        transform.position = pos;

        transform.position += (direction.normalized * movementSpeed * Time.deltaTime);
    }

    private IEnumerator GenerateNewTargetCoordinates()
    {
        for (; ; )
        {
            int previousX = (int)currentTargetCoordinates.x;
            int previousZ = (int)currentTargetCoordinates.z;

            int x = random.Next((int)(previousX + Math.Round((double)(0 - previousX / (480 / 49)))), (int)(previousX + Math.Round((double)(50 - previousX / (480 / 49)))));
            int z = random.Next((int)(previousZ + Math.Round((double)(0 - previousZ / (480 / 49)))), (int)(previousZ + Math.Round((double)(50 - previousZ / (480 / 49)))));

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
        if (currentTornado == null)
        {
            isActive = false;
        }
        else
        {
            isActive = currentTornado.isActive;
        }
        if (!isActive)
        {
            tornadoCollider.enabled = false;
            return;
        }

        tornadoCollider.enabled = true;

        PullObjects();
        Move();

        CheckShouldStop();
    }

    private Vector3 GetRandomSpawnPoint()
    {
        int index = random.Next(1, possibleStartingCoordinates.Count);
        return possibleStartingCoordinates[index];
    }

    private void CheckShouldStop()
    {
        if (Time.time - executionLength - 1 > lastStartTime)
        {
            currentTornado.isActive = false;
        }
    }

    public void TriggerTornado(Tornado tornado, int length, float strength)
    {
        pullingStrength = strength * pullingStrengthModifier;
        currentTornado = tornado;
        executionLength = length;

        Vector3 spawnPoint = GetRandomSpawnPoint();
        transform.position = spawnPoint;

        lastStartTime = Time.time;
        currentTornado.isActive = true;
        tornadoParticleSystem.Play();
    }

    public void Fade()
    {
        tornadoParticleSystem.Stop();
    }
}


public class Tornado : Disaster
{
    public GameObject tornado;

    public Tornado(GameObject tornado, Article article, DisasterName name, float strength, int executionLength, TMP_Text displayText) :
        base(article, name, strength, executionLength, displayText)
    { this.tornado = tornado; }

    public override void ExecuteDisaster()
    {
        base.ExecuteDisaster();  // Execute Base Class Execution Method

        Tornadoes tornadoScript = tornado.GetComponent<Tornadoes>();

        tornadoScript.TriggerTornado(this, executionLength, strength);
    }

    public override void EndDisaster()
    {
        base.EndDisaster();  // Execute Base Class Ending Method

        Tornadoes tornadoScript = tornado.GetComponent<Tornadoes>();

        tornadoScript.Fade();
    }
}
