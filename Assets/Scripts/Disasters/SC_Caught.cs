//This script is attached automatically to each Object caught in Tornado

using UnityEngine;

public class SC_Caught : MonoBehaviour
{
    private SC_Tornado tornadoReference;
    private SpringJoint spring;
    [HideInInspector]
    public Rigidbody rigid;

    // Use this for initialization
    void Start()
    {
        rigid = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        //Lift spring so objects are pulled upwards
        Vector3 newPosition = spring.connectedAnchor;
        newPosition.y = transform.position.y;
        spring.connectedAnchor = newPosition;
    }

    void FixedUpdate()
    {
        //Rotate object around tornado center
        Vector3 direction = transform.position - tornadoReference.transform.position;
        //Project
        Vector3 projection = Vector3.ProjectOnPlane(direction, tornadoReference.GetRotationAxis());
        projection.Normalize();
        Vector3 normal = Quaternion.AngleAxis(130, tornadoReference.GetRotationAxis()) * projection;
        normal = Quaternion.AngleAxis(tornadoReference.lift, projection) * normal;
        rigid.AddForce(normal * tornadoReference.GetStrength(), ForceMode.Force);

        Debug.DrawRay(transform.position, normal * 10, Color.red);
    }

    //Call this when tornadoReference already exists
    public void Init(SC_Tornado tornadoRef, Rigidbody tornadoRigidbody, float springForce)
    {
        //Make sure this is enabled (for reentrance)
        enabled = true;

        //Save tornado reference
        tornadoReference = tornadoRef;

        //Initialize the spring
        spring = gameObject.AddComponent<SpringJoint>();
        spring.spring = springForce;
        spring.connectedBody = tornadoRigidbody;

        spring.autoConfigureConnectedAnchor = false;

        //Set initial position of the caught object relative to its position and the tornado
        Vector3 initialPosition = Vector3.zero;
        initialPosition.y = transform.position.y;
        spring.connectedAnchor = initialPosition;
    }

    public void Release()
    {
        enabled = false;
        Destroy(spring);
    }
}