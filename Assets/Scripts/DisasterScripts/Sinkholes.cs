using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;


public class Sinkholes : MonoBehaviour
{

}


public class Sinkhole : Disaster
{
    public GameObject sinkhole;

    public Sinkhole(GameObject sinkhole, Article article, DisasterName name, float strength, int executionLength) :
        base(article, name, strength, executionLength)
    { this.sinkhole = sinkhole; }

    public override void ExecuteDisaster()
    {
        base.ExecuteDisaster();  // Execute Base Class Execution Method

        Sinkholes sinkholeScript = sinkhole.GetComponent<Sinkholes>();
    }
}
