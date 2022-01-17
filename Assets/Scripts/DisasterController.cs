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
    Sinkhole
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
        RunDisasterClock();
    }

    private async void RunDisasterClock()
    {
        while (true)
        {
            disasterInfo.currentDisaster = disasterInfo.nextDisaster;
            disasterInfo.nextDisaster = GetRandomDisaster();

            int waitTime = RandomInteger(minTimeBetweenDisasters, maxTimeBetweenDisasters);
            await DisplayIncomingText(waitTime);

            await disasterInfo.currentDisaster.ExecuteDisaster();
        }
    }

    async private Task DisplayIncomingText(int waitTime)
    {
        for (int secondsLeft = waitTime; secondsLeft >= 0; secondsLeft--)
        {
            displayText.text = $"{disasterInfo.currentDisaster.article} {disasterInfo.currentDisaster.name} is imminent in {secondsLeft}s!\nWatch out!";
            await Task.Delay(1000);
        }
    }

    public Disaster GetRandomDisaster()
    {
        float strength = RandomFloat(minStrength, maxStrength);
        int executionTime = RandomInteger(minExecutionTime, maxExecutionTime);
        DisasterName disasterName = RandomEnum<DisasterName>();
        
        Article article;
        if ("aeiou".IndexOf(disasterName.ToString().ToLower()[0]) >= 0)
        {
            article = Article.An;
        } else
        {
            article = Article.A;
        }

        Disaster disaster = new Disaster(article, disasterName, strength, executionTime, displayText);
        return disaster;
    }
}

public class Disaster
{
    public string name;
    public float strength;
    public int executionLength;
    public string article;
    
    TMP_Text displayText;

    public Disaster(Article article, DisasterName name, float strength, int executionLength, TMP_Text displayText)
    {
        this.name = name.ToString();
        this.strength = strength;
        this.executionLength = executionLength;
        this.article = article.ToString();
        this.displayText = displayText;
    }

    async public Task ExecuteDisaster()
    {
        displayText.text = $"{article} {name} is active!\nStrength: {strength}\tLength: {executionLength}s";

        // Spawn physical animation here

        await Task.Delay(executionLength * 1000);

    }
}
