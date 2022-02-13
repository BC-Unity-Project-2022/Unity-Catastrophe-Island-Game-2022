using UnityEngine;
using TMPro;


public class Floods : MonoBehaviour
{
    public float moveSpeed;
    public int currentExecutionLength;

    [SerializeField] private float endDistance = 18f;
    [SerializeField] private float recessionTime = 3f;

    private Vector3 startingPosition;
    private Vector3 endingPosition;
    private Vector3 travelDifference;
    private float currentExecutionTimer;
    private float currentRecessionTimer = 0;
    private float percentageTravelled;
    private float percentageTravelledBack;
    private Disaster currentFlood;
    private bool isActiveBool = false;
    private bool currentlyReceding = false;

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

        if (currentRecessionTimer <= recessionTime && currentlyReceding)
        {
            currentRecessionTimer += Time.deltaTime;
            percentageTravelledBack = currentRecessionTimer / recessionTime;
            transform.position = endingPosition - travelDifference * percentageTravelledBack;
        }

        if (isActiveBool && transform.position.y >= endingPosition.y)
        {
            ResetWaterPlane();
        }
    }

    private void ResetWaterPlane()
    {
        currentExecutionTimer = 0f;
        currentRecessionTimer = 0f;
        currentFlood.isActive = false;
    }

    public void TriggerFlood(Disaster flood, int executionTime, float strength)
    {
        currentFlood = flood;
        currentExecutionLength = executionTime;
        transform.position = startingPosition;
        endDistance = 8f + 2 * strength;
        currentFlood.isActive = true;
    }

    public void Recede()
    {
        currentlyReceding = true;
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
        base.ExecuteDisaster();

        Floods floodScript = waterPlane.GetComponent<Floods>();

        floodScript.TriggerFlood(this, executionLength, strength);
    }

    public override void EndDisaster()
    {
        base.EndDisaster();  // Execute Base Class Ending Method

        Floods floodScript = waterPlane.GetComponent<Floods>();

        floodScript.Recede();
    }
}
