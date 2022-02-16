using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OverlayManager : MonoBehaviour
{
    [SerializeField] private TMP_Text _timer;
    [SerializeField] private TMP_Text _disasterText;

    public void SetTimerText(string t)
    {
        _timer.text = t;
    }

    public void SetDisasterText(string t)
    {
        _disasterText.SetText(t);
    }

    public void Hide()
    {
        _timer.enabled = false;
        _disasterText.enabled = false;
    }

    public void Show()
    {
        _timer.enabled = true;
        _disasterText.enabled = true;
    }
}
