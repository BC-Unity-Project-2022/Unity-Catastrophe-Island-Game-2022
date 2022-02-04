using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandMove : MonoBehaviour
{

    private bool selectNewRandomPosition = true;
    private bool waitingForNewPosition = false;
    public float minX;
    public float maxX;
    public float minZ;
    public float maxZ;
    public float movementSpeed;
    public float newPositionWaitTime;
    private float moveX;
    private float moveZ;
    private float newX;
    private float newZ;
    private float stopX;
    private float stopZ;
    private float frameX;
    private float frameZ;
    private float movedX;
    private float movedZ;

    // Update is called once per frame
    void Update()
    {

        Vector3 pos = transform.position;
        pos.y = Terrain.activeTerrain.SampleHeight(transform.position);

        if (selectNewRandomPosition)
        {
            StartCoroutine(newRandomPosition());
        }
        else if (!waitingForNewPosition)
        {
            frameX = (moveX * Time.deltaTime * movementSpeed);
            frameZ = (moveZ * Time.deltaTime * movementSpeed);
            movedX += frameX;
            movedZ += frameZ;
            newX = this.transform.position.x + frameX;
            newZ = this.transform.position.z + frameZ;

            transform.position = pos;

            if (Mathf.Abs(movedZ) >= Mathf.Abs(moveX) || Mathf.Abs(movedZ) >= Mathf.Abs(moveZ))
            {
                waitingForNewPosition = true;
                selectNewRandomPosition = true;
            }
            else
            {
                this.transform.position = new Vector3(newX, transform.position.y, newZ);
            }
        }
    }

    IEnumerator newRandomPosition()
    {
        waitingForNewPosition = true;
        selectNewRandomPosition = false;
        yield return new WaitForSeconds(newPositionWaitTime);
        moveX = Random.Range(minX, maxX);
        moveZ = Random.Range(minZ, maxZ);
        stopX = this.transform.position.x + moveX;
        stopZ = this.transform.position.z + moveZ;
        movedX = 0f;
        movedZ = 0f;
        waitingForNewPosition = false;
    }
}

