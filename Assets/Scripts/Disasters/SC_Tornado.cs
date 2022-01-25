//Tornado script controls tornado physics

using System.Collections.Generic;
using UnityEngine;

public class SC_Tornado : MonoBehaviour
{
    [Tooltip("Distance after which the rotation physics starts")]
    public float maxDistance = 20;

    [Tooltip("The axis that the caught objects will rotate around")]
    public Vector3 rotationAxis = new Vector3(0, 1, 0);

    [Tooltip("Angle that is added to the object's velocity (higher lift -> quicker on top)")]
    [Range(0, 90)]
    public float lift = 45;

    [Tooltip("The force that will drive the caught objects around the tornado's center")]
    public float rotationStrength = 50;

    [Tooltip("Tornado pull force")]
    public float tornadoStrength = 2;

    Rigidbody r;

    List<SC_Caught> caughtObject = new List<SC_Caught>();

    // Start is called before the first frame update
    void Start()
    {
        //Normalize the rotation axis given by the user
        rotationAxis.Normalize();

        r = GetComponent<Rigidbody>();
        r.isKinematic = true;
    }

    void FixedUpdate()
    {
        //Apply force to caught objects
        for (int i = 0; i < caughtObject.Count; i++)
        {
            if(caughtObject[i] != null)
            {
                Vector3 pull = transform.position - caughtObject[i].transform.position;
                if (pull.magnitude > maxDistance)
                {
                    caughtObject[i].rigid.AddForce(pull.normalized * pull.magnitude, ForceMode.Force);
                    caughtObject[i].enabled = false;
                }
                else
                {
                    caughtObject[i].enabled = true;
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.attachedRigidbody) return;
        if (other.attachedRigidbody.isKinematic) return;

        //Add caught object to the list
        SC_Caught caught = other.GetComponent<SC_Caught>();
        if (!caught)
        {
            caught = other.gameObject.AddComponent<SC_Caught>();
        }

        caught.Init(this, r, tornadoStrength);

        if (!caughtObject.Contains(caught))
        {
            caughtObject.Add(caught);
        }
    }

    void OnTriggerExit(Collider other)
    {
        //Release caught object
        SC_Caught caught = other.GetComponent<SC_Caught>();
        if (caught)
        {
            caught.Release();

            if (caughtObject.Contains(caught))
            {
                caughtObject.Remove(caught);
            }
        }
    }

    public float GetStrength()
    {
        return rotationStrength;
    }

    //The axis the caught objects rotate around
    public Vector3 GetRotationAxis()
    {
        return rotationAxis;
    }

    //Draw tornado radius circle in Editor
    void OnDrawGizmosSelected()
    {
        Vector3[] positions = new Vector3[30];
        Vector3 centrePos = transform.position;
        for (int pointNum = 0; pointNum < positions.Length; pointNum++)
        {
            // "i" now represents the progress around the circle from 0-1
            // we multiply by 1.0 to ensure we get a fraction as a result.
            float i = (float)(pointNum * 2) / positions.Length;

            // get the angle for this step (in radians, not degrees)
            float angle = i * Mathf.PI * 2;

            // the X & Y position for this angle are calculated using Sin & Cos
            float x = Mathf.Sin(angle) * maxDistance;
            float z = Mathf.Cos(angle) * maxDistance;

            Vector3 pos = new Vector3(x, 0, z) + centrePos;
            positions[pointNum] = pos;
        }

        Gizmos.color = Color.cyan;
        for (int i = 0; i < positions.Length; i++)
        {
            if (i == positions.Length - 1)
            {
                Gizmos.DrawLine(positions[0], positions[positions.Length - 1]);
            }
            else
            {
                Gizmos.DrawLine(positions[i], positions[i + 1]);
            }
        }
    }
}