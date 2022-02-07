using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;


public class Earthquakes : MonoBehaviour
{
    
}


public class Earthquake : Disaster
{
    public GameObject earthquake;

    public Earthquake(GameObject earthquake, Article article, DisasterName name, float strength, int executionLength, TMP_Text displayText) :
        base(article, name, strength, executionLength, displayText)
    { this.earthquake = earthquake; }

    public override void ExecuteDisaster()
    {
        base.ExecuteDisaster();  // Execute Base Class Execution Method

        Earthquakes earthquakeScript = earthquake.GetComponent<Earthquakes>();
    }
}
