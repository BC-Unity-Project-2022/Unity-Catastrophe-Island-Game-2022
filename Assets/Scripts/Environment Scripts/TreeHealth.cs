using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeHealth : MonoBehaviour
{
    public float health = 100;

    public float timeToDisappear = 3;

    public int wood = 30;

    private float timeSinceKilled = 0;

    // returns a bool saying whether the tree has been chopped down
    public bool Damage(float dmg)
    {
        float prevHealth = health;
        health -= Mathf.Abs(dmg);
        return health <= 0 && prevHealth > 0;
    }

    public int CalculateWood()
    {
        return wood;
    }

    // Update is called once per frame
    void Update()
    {
        if (health <= 0)
        {
            // only add the component once
            if(timeSinceKilled == 0) gameObject.AddComponent<Rigidbody>();
            timeSinceKilled += Time.deltaTime;

            if (timeSinceKilled > timeToDisappear)
            {
                Destroy(gameObject);
            }
        }
    }
}
