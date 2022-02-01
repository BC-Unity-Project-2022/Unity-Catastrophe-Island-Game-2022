using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;


public class Floods : MonoBehaviour
{
    public bool floodIsActive = false;
    public float moveSpeed;
    public int currentExecutionLength;

    [SerializeField] private float endHeight = 28.5f;

    private Vector3 startingPosition;
    private Vector3 endingPosition;
    private Vector3 travelDifference;
    private float currentExecutionTimer;
    private float percentageTravelled;

    private void Start()
    {
        startingPosition = transform.position;
        endingPosition = transform.position + new Vector3(0, endHeight, 0);
        travelDifference = endingPosition - startingPosition;
    }

    private void Update()
    {
        if (currentExecutionTimer <= currentExecutionLength && floodIsActive)
        {
            currentExecutionTimer += Time.deltaTime;
            percentageTravelled = currentExecutionTimer / currentExecutionLength;
            transform.position = startingPosition + travelDifference * percentageTravelled;
        } else if (floodIsActive && transform.position.y >= endingPosition.y)
        {
            ResetWaterPlane();
        }
    }

    private void ResetWaterPlane()
    {
        floodIsActive = false;
        transform.position = startingPosition;
    }



    public void TriggerFlood(int executionTime)
    {
        currentExecutionLength = executionTime;
        transform.position = startingPosition;
        floodIsActive = true;
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

        floodScript.TriggerFlood(executionLength);
    }
}
