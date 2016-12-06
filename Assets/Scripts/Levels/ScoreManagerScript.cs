using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// XML representation of a score
/// </summary>
[Serializable]
public class XMLScore
{
    [XmlAttribute("level")]    public string   levelName;
    [XmlAttribute("score")]    public uint     score;
    [XmlAttribute("dateTime")] public DateTime dateTime;
}

/// <summary>
/// tracks the players high scores across all levels
/// </summary>
[XmlRoot("PlayerScores")]
[Serializable]
public class ScoreCollection
{
    //import the (teeny tiny) javascript lib being used to make sure saving persists on webGL builds
    [DllImport("__Internal")]
    private static extern void SyncFiles();

    //list of decks
    [XmlArray("Scores")]
    [XmlArrayItem("Score")]
    [Display(Seq.GuiBox | Seq.PerItemDuplicate | Seq.PerItemRemove)]
    public List<XMLScore> scores;

    /// <summary>
    /// default constructor.  creates an empty collection.
    /// </summary>
    public ScoreCollection()
    {
        scores = new List<XMLScore>();
    }

    //DEV: saves changes to this collection, with a confirmation box
    [Show]
    private IEnumerator saveScores()
    {
        yield return ScoreManagerScript.instance.StartCoroutine(MessageHandlerScript.PromptYesNo("Are you sure you want to save these scores?"));
        if (MessageHandlerScript.responseToLastPrompt == "Yes")
        {
            Save(Path.Combine(Application.persistentDataPath, "playerScores.xml"));
            Debug.Log("Scores Saved.");
        }
    }

    /// <summary>
    /// saves this collection to the given file
    /// </summary>
    public void Save(string path)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(ScoreCollection));

        using (StreamWriter stream = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
        {
            serializer.Serialize(stream, this);
        }

        //on web builds, javascript call to try and make sure the changes persist (see http://answers.unity3d.com/questions/1095407/saving-webgl.html and HandleIO.jslib)
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            SyncFiles();
    }

    /// <summary>
    /// returns a new ScorekCollection loaded from the given file
    /// </summary>
    public static ScoreCollection Load(Stream stream, string filePath)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(ScoreCollection));
        ScoreCollection result = serializer.Deserialize(stream) as ScoreCollection;
        return result;
    }

    /// <summary>
    /// returns the player's top score on the given level, if it exists
    /// if it does not exist, 0 is returned
    /// </summary>
    public uint getTopScoreForLevel(string levelName)
    {
        if (scores.Any(xs => xs.levelName == levelName))
        {
            return scores.Find(xs => xs.levelName == levelName).score;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// if this is the highest score recorded on this level, then it is saved and returns true.  Otherwise, does nothing and returns false.
    /// </summary>
    public bool recordScoreForLevel(string levelName, uint score)
    {
        uint existingScore = getTopScoreForLevel(levelName);
        if (score > existingScore)
        {
            scores.RemoveAll(xs => xs.levelName == levelName);

            XMLScore newScore = new XMLScore();
            newScore.levelName = levelName;
            newScore.score = score;
            newScore.dateTime = DateTime.Now;
            scores.Add(newScore);
            return true;
        }

        return false;
    }
}


/// <summary>
/// handles all the score tracking and provides a report at the end of a level
/// </summary>
public class ScoreManagerScript : BaseBehaviour
{
    [Hide] public static ScoreManagerScript instance;

    //scoring constants
    public uint CLEAR_VALUE;      //for winning
    public uint WAVE_VALUE;       //for clearing a wave
    public uint ENEMY_DRAW_VALUE; //for drawing an enemy card
    public uint FLAWLESS_VALUE;   //for winning without taking damage from enemies

    //current score tracking
    private bool levelLoaded() { return (LevelManagerScript.instance != null) && (LevelManagerScript.instance.levelLoaded); }
    [VisibleWhen("levelLoaded")] public uint wavesCleared;    //score from wave clears
    [VisibleWhen("levelLoaded")] public uint enemyCardsDrawn; //score from drawing enemies
    [VisibleWhen("levelLoaded")] public int  bonusPoints;     //score from cards with the score effect
    [VisibleWhen("levelLoaded")] public bool flawless;        //set to false if the player is damaged by an enemy

    //high score tracking
    private bool showScores() { return (playerScores != null) && (Application.isPlaying); }
    [VisibleWhen("showScores")] public ScoreCollection playerScores; 

	//Use this for initialization
	void Awake()
    {
        instance = this;
        wavesCleared = 0;
        enemyCardsDrawn = 0;
        flawless = true;

        //load player decks if the file exists, or create an empty collection if not.  
        //This file is local even on web builds, so it doesn't need special handling
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, "playerScores.xml");
            using (FileStream stream = new FileStream(filePath, FileMode.Open))
                playerScores = ScoreCollection.Load(stream, filePath);
        }
        catch (Exception e)
        {
            Debug.Log("no score save file found. (" + e.Message + ")");
            playerScores = new ScoreCollection();
        }
    }

    //use to reset the manager
    void Reset()
    {
        wavesCleared = 0;
        enemyCardsDrawn = 0;
        flawless = true;
    }

    /// <summary>
    /// returns a string of the score report.  PlayerWon indicates whether or not the player won the level
    /// </summary>
    public string report(bool PlayerWon)
    {
        string result = "";
        uint totalScore = 0;

        if (PlayerWon)
        {
            result += "Victory!:   " + CLEAR_VALUE + '\n';
            totalScore += CLEAR_VALUE;
        }
        else
        {
            result += "Defeat!:    " + 0 + '\n';
        }

        result +=     "Wave bonus:   " + wavesCleared * WAVE_VALUE + '\n';
        totalScore += wavesCleared * WAVE_VALUE;

        result +=     "Draw bonus:   " + enemyCardsDrawn * ENEMY_DRAW_VALUE + '\n';
        totalScore += enemyCardsDrawn * ENEMY_DRAW_VALUE;

        if (flawless)
        {
            result += "Flawless!:    " + FLAWLESS_VALUE + '\n';
            totalScore += FLAWLESS_VALUE;
        }

        if (bonusPoints != 0)
        {
            result += "Bonus points: " + bonusPoints + '\n';
            totalScore += Convert.ToUInt32(bonusPoints);
        }

        result +=     "=================\n";
        result +=     "Total:      " + totalScore;

        //record the score, and save the collection if it is a new high for this level
        if (playerScores.recordScoreForLevel(LevelManagerScript.instance.data.fileName, totalScore))
        {
            playerScores.Save(Path.Combine(Application.persistentDataPath, "playerScores.xml")); //TODO: make save file per-user if/when user accounts exist
            result += "\n<<<<<NEW HIGH SCORE!>>>>>";
        }

        return result;
    }

    /// <summary>
    /// returns the minimum score a player would get for simply clearing the level with no other modifiers
    /// </summary>
    /// <param name="data"></param>
    public uint parScore(LevelData data)
    {
        //uses .Count<>() instead of .Count in case this is tested while a level is already in progress
        return CLEAR_VALUE + (Convert.ToUInt32(data.waves.Count(wd => wd.isRandomWave == false) + data.randomWaveCount) * WAVE_VALUE);
    }
}
