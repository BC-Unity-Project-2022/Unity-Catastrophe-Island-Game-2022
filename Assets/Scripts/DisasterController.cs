using System.Collections;
using System;
using UnityEngine;

struct DisasterInfo
{
    public Disaster currentDisaster;
    public Disaster nextDisaster;
}

public enum DisasterName
{
    Tornado,
    Earthquake,
    Sinkhole
}

public class DisasterController : MonoBehaviour
{
    [SerializeField] private int minTimeBetweenDisasters;
    [SerializeField] private int maxTimeBetweenDisasters;
    [SerializeField] private float minStrength;
    [SerializeField] private float maxStrength;
    [SerializeField] private int minExecutionTime;
    [SerializeField] private int maxExecutionTime;

    DisasterInfo disasterInfo = new DisasterInfo();

    private static System.Random RNG = new System.Random();

    public static T RandomEnum<T>()
    {
        Type type = typeof(T);
        Array values = Enum.GetValues(type);
        lock (RNG)
        {
            object value = values.GetValue(RNG.Next(values.Length));
            return (T)Convert.ChangeType(value, type);
        }
    }

    static float RandomFloat(float min, float max)
    {
        double val = (RNG.NextDouble() * (max - min) + min);
        return (float)Math.Round(val, 1);
    }

    static int RandomInteger(int min, int max)
    {
        return RNG.Next(min, max);
    }

    void Start()
    {
        disasterInfo.nextDisaster = GetRandomDisaster();
        StartCoroutine(RunDisasterClock());
    }
    IEnumerator RunDisasterClock()
    {
        while (true)
        {
            disasterInfo.currentDisaster = disasterInfo.nextDisaster;
            disasterInfo.nextDisaster = GetRandomDisaster();
            disasterInfo.currentDisaster.ExecuteDisaster();

            yield return new WaitForSeconds(RandomInteger(minTimeBetweenDisasters, maxTimeBetweenDisasters));
        }
    }

    public Disaster GetRandomDisaster()
    {
        float strength = RandomFloat(minStrength, maxStrength);
        int executionTime = RandomInteger(minExecutionTime, maxExecutionTime);
        DisasterName disasterName = RandomEnum<DisasterName>();

        Disaster disaster = new Disaster(disasterName, strength, executionTime);
        return disaster;
    }
}

public class Disaster
{
    string name;
    float strength;
    int executionLength;

    public Disaster(DisasterName name, float strength, int executionLength)
    {
        this.name = name.ToString();
        this.strength = strength;
        this.executionLength = executionLength;
    }

    public void ExecuteDisaster()
    {
        // Run the disaster
        Debug.Log($"Running Disaster '{name}', which has a strength of {strength} and an length of {executionLength}s");
    }
}

