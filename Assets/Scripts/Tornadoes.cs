using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using TMPro;


public class Tornadoes : MonoBehaviour
{
    
}


public class Tornado : Disaster
{
    public GameObject tornado;

    public Tornado(GameObject tornado, Article article, DisasterName name, float strength, int executionLength, TMP_Text displayText) :
        base(article, name, strength, executionLength, displayText)
    { this.tornado = tornado; }

    public override void ExecuteDisaster()
    {
        base.ExecuteDisaster();  // Execute Base Class Execution Method

        Tornadoes tornadoScript = tornado.GetComponent<Tornadoes>();
    }
}
