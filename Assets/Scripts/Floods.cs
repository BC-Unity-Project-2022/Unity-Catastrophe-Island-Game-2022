using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;


public class Floods : MonoBehaviour
{
    public float moveSpeed;
    public int currentExecutionLength;

    [SerializeField] private float endDistance = 10f;

    private Vector3 startingPosition;
    private Vector3 endingPosition;
    private Vector3 travelDifference;
    private float currentExecutionTimer;
    private float percentageTravelled;
    private Disaster currentFlood;
    private bool isActiveBool = false;

    private void Start()
    {
        startingPosition = transform.position;
        endingPosition = transform.position + new Vector3(0, endDistance, 0);
        travelDifference = endingPosition - startingPosition;
    }

    private void Update()
    {
        if (currentFlood == null)
        {
            isActiveBool = false;
        } else
        {
            isActiveBool = currentFlood.isActive;
        }
        if (currentExecutionTimer <= currentExecutionLength && isActiveBool)
        {
            currentExecutionTimer += Time.deltaTime;
            percentageTravelled = currentExecutionTimer / currentExecutionLength;
            transform.position = startingPosition + travelDifference * percentageTravelled;
        }

        if (isActiveBool && transform.position.y >= endingPosition.y)
        {
            ResetWaterPlane();
        }
    }

    private void ResetWaterPlane()
    {
        currentFlood.isActive = false;
        transform.position = startingPosition;
        currentExecutionTimer = 0f;
    }

    public void TriggerFlood(Disaster flood, int executionTime)
    {
        currentFlood = flood;
        currentExecutionLength = executionTime;
        transform.position = startingPosition;
        currentFlood.isActive = true;
    }
}


public class Flood : Disaster
{
    public GameObject waterPlane;

    public Flood(GameObject waterPlane, Article article, DisasterName name, float strength, int executionLength, TMP_Text displayText) :
        base(article, name, strength, executionLength, displayText)
    { this.waterPlane = waterPlane; }

    public override void ExecuteDisaster()
    {
        base.ExecuteDisaster();  // Execute Base Class Execution Method

        Floods floodScript = waterPlane.GetComponent<Floods>();

        floodScript.TriggerFlood(this, executionLength);
    }
}
