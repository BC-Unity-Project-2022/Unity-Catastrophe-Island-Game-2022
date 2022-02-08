using UnityEngine;
using System.Collections;

//Moves the target the tornado is chasing to random positons
public class Movement : MonoBehaviour
{
    //Speed of tornado/object movement
    float speed = 5f;

    //The target the goal is moving towards
    Vector3 goal;

    float mapHalfSize = 125f;



    void Start()
    {
        goal = new Vector3(Random.Range(-mapHalfSize, mapHalfSize), 0f, Random.Range(-mapHalfSize, mapHalfSize));
    }



    void Update()
    {
        //Move towards the goal
        transform.LookAt(goal);

        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        //If the goal/target is reach find new goal
        if ((transform.position - goal).sqrMagnitude < 2f)
        {
            goal = new Vector3(Random.Range(-mapHalfSize, mapHalfSize), 0f, Random.Range(-mapHalfSize, mapHalfSize));
        }
    }
}