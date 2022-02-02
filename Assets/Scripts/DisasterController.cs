using System.Collections;
using System;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;

struct DisasterInfo
{
    public Disaster currentDisaster;
    public Disaster nextDisaster;
}

public enum DisasterName
{
    Tornado,
    Earthquake,
    Sinkhole, 
    Flood
}

public enum Article
{
    A,
    An,
    The
}

public class DisasterController : MonoBehaviour
{
    [SerializeField] private int minTimeBetweenDisasters;
    [SerializeField] private int maxTimeBetweenDisasters;
    [SerializeField] private float minStrength;
    [SerializeField] private float maxStrength;
    [SerializeField] private int minExecutionTime;
    [SerializeField] private int maxExecutionTime;
    [SerializeField] private TMP_Text displayText;

    [SerializeField] private GameObject waterPlane;
    [SerializeField] private GameObject tornado;
    [SerializeField] private GameObject sinkhole;
    [SerializeField] private GameObject earthquake;

    DisasterInfo disasterInfo = new DisasterInfo();

    private static System.Random RNG = new System.Random();
    private float lastDisasterTime;
    private float lastCountdownTime;
    private int waitTime = 0;
    private int textIteration;
    private bool wasActiveLastFrame;

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
        disasterInfo.currentDisaster = GetRandomDisaster();
        disasterInfo.nextDisaster = GetRandomDisaster();
        lastDisasterTime = Time.time;

        waitTime = RandomInteger(minTimeBetweenDisasters, maxTimeBetweenDisasters);
        textIteration = waitTime;
        wasActiveLastFrame = false;
    }

    private void Update()
    {
        if (disasterInfo.currentDisaster.isActive) {
            wasActiveLastFrame = true;
            return;
        }
        else if (!disasterInfo.currentDisaster.isActive && wasActiveLastFrame)
        {
            EndDisaster();
        }

        if (Time.time - waitTime - 1 > lastDisasterTime)
        {
            RunDisaster();
        }
        else if (Time.time - lastCountdownTime >= 1 || textIteration == waitTime)
        {
            ChangeIncomingText();
        }
    }

    private void EndDisaster()
    {
        disasterInfo.currentDisaster.EndDisaster();
        wasActiveLastFrame = false;
        lastDisasterTime = Time.time;
        waitTime = RandomInteger(minTimeBetweenDisasters, maxTimeBetweenDisasters);
        textIteration = waitTime;
    }

    private void RunDisaster()
    {
        disasterInfo.currentDisaster = disasterInfo.nextDisaster;
        disasterInfo.nextDisaster = GetRandomDisaster();

        disasterInfo.currentDisaster.ExecuteDisaster();
    }
    
    private void ChangeIncomingText()
    {
        displayText.text = $"{disasterInfo.currentDisaster.article} {disasterInfo.currentDisaster.name} is imminent in {textIteration}s!\nWatch out!";

        textIteration -= 1;
        lastCountdownTime = Time.time;
    }

    public Disaster GetRandomDisaster()
    {
        float strength = RandomFloat(minStrength, maxStrength);
        int executionTime = RandomInteger(minExecutionTime, maxExecutionTime);
        // DisasterName disasterName = RandomEnum<DisasterName>();
        DisasterName disasterName = DisasterName.Flood;
        Disaster disaster;

        Article article;
        if ("aeiou".IndexOf(disasterName.ToString().ToLower()[0]) >= 0)
        {
            article = Article.An;
        }
        else
        {
            article = Article.A;
        }

        // Execute Chosen Disaster
        switch (disasterName)
        {
            case DisasterName.Flood:
                disaster = new Flood(waterPlane, article, disasterName, strength, executionTime, displayText);
                break;
            case DisasterName.Tornado:
                disaster = new Tornado(tornado, article, disasterName, strength, executionTime, displayText);
                break;
            case DisasterName.Earthquake:
                disaster = new Earthquake(earthquake, article, disasterName, strength, executionTime, displayText);
                break;
            case DisasterName.Sinkhole:
                disaster = new Sinkhole(sinkhole, article, disasterName, strength, executionTime, displayText);
                break;
            default:
                throw new ArgumentException();
        }

        return disaster;
    }
}

// Base Disaster Class
public class Disaster
{
    public string name;
    public float strength;
    public int executionLength;
    public string article;
    public bool isActive;

    TMP_Text displayText;

    public Disaster(Article article, DisasterName name, float strength, int executionLength, TMP_Text displayText)
    {
        this.name = name.ToString();
        this.strength = strength;
        this.executionLength = executionLength;
        this.article = article.ToString();
        this.displayText = displayText;
        this.isActive = false;
    }

    public virtual void ExecuteDisaster()
    {
        displayText.text = $"{article} {name} is active!\nStrength: {strength}\tLength: {executionLength}s";
    }

    public virtual void EndDisaster()
    {

    }
}
 