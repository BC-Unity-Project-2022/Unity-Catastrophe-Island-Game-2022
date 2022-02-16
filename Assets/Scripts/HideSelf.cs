using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideSelf : MonoBehaviour
{
    void Start()
    {
        GetComponent<MeshRenderer>().enabled = false;
    }
}
