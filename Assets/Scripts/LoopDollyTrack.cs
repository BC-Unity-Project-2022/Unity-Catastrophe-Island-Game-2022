using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class LoopDollyTrack : MonoBehaviour
{
    [SerializeField] private float speed;
    
    private CinemachineTrackedDolly _dolly;

    private void Start()
    {
        _dolly = GetComponentInChildren<CinemachineTrackedDolly>();
    }

    void Update()
    {
        _dolly.m_PathPosition += speed * Time.deltaTime;
    }
}
