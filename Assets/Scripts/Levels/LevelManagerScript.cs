using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

//contains an upgrade and a number to indicate how many times it should be applied.  used by PremadeTower
[System.Serializable]
public class PremadeTowerUpgrade
{
    [XmlAttribute] public string Name;  //upgrade to apply
    [XmlAttribute] public int    Count;	//number of applications
}

//container that holds everything needed to create a tower at level start
[System.Serializable]
public class PremadeTower
{
    [XmlAttribute] public string name; //name of the tower to place

    [XmlArray("Upgrades")]
    [XmlArrayItem("Upgrade")]
    public List<PremadeTowerUpgrade> upgrades;  //a list of all upgrades to apply, in order

    [XmlAttribute] public float x; //x to spawn at
    [XmlAttribute] public float y; //y to spawn at
};

[XmlRoot("Level")]
[System.Serializable]
public class LevelData
{
    //level background
    [XmlAttribute("background")]
    public string background = "Default_bg";

    //wave generation parameters
    public int randomWaveCount;

    //budget: absolute + (wave * linear) + (wave * squared)^2 + (exponential^wave)
    public float waveGrowthAbsolute     = 40.0f;
    public float waveGrowthLinear       = 4.0f;
    public float waveGrowthSquared      = 2.2f;
    public float waveGrowthExponential  = 1.1f;

    //time: min(wave*linear, maxwavetime)
    public float waveTimeLinear = 1.1f;
    public float waveTimeMax    = 20.0f;

    [XmlArray("Waves")]
    [XmlArrayItem("Wave")]
    public List<WaveData> waves;

    [XmlArray("Spawners")]
    [XmlArrayItem("Spawner")]
    public List<SpawnerData> spawners;

    [XmlArray("PathSegments")]
    [XmlArrayItem("Segment")]
    public List<PathSegment> pathSegments;

    [XmlArray("Towers")]
    [XmlArrayItem("Tower")]
    public List<PremadeTower> towers;

    public bool shuffleDeck;

    //levelDeck is the deck set for use on this level.
    //It could be defined directly in the level file, or the level could just provide the name of a premade deck in Decks.xml instead.
    public XMLDeck levelDeck;
    public string premadeDeckName;

    public void Save(string path)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(LevelData));

        using (StreamWriter stream = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
        {
            serializer.Serialize(stream, this);
        }
    }

    public static LevelData Load(string path)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(LevelData));
        using (var stream = new FileStream(path, FileMode.Open))
        {
            return serializer.Deserialize(stream) as LevelData;
        }
    }
}

//contains all the data required to spawn a wave
[System.Serializable]
public class WaveData
{
    [XmlAttribute] public string type;
    [XmlAttribute] public int    budget;
    [XmlAttribute] public float  time;
    [XmlAttribute] public string message;
    [XmlIgnore]    public int forcedSpawnCount; //if this is negative, no spawn count was forced

    [XmlIgnore] private EnemyData data;
    [XmlIgnore] public EnemyData enemyData
    {
        get
        {
            if ( (data == null) || (data.name != type) )
                data = EnemyTypeManagerScript.instance.getEnemyTypeByName(type);

            return data;
        }
        set { data = value; }
    }

    //default.  these values are meant to be VERY noticable if a wave is left with default data
    public WaveData()
    {
        type = "Swarm";
        budget = 999999999;
        time = 300.0f;
        message = null;
        forcedSpawnCount = -1;
    }

    //specific data
    public WaveData(string waveType, int waveBudget, float waveTime)
    {
        type = waveType;
        budget = waveBudget;
        time = waveTime;
        message = null;
        forcedSpawnCount = -1;
    }    
}

public class LevelManagerScript : BaseBehaviour
{
    //other objects can refer to these to be informed when a level is loaded (https://unity3d.com/learn/tutorials/topics/scripting/events)
    public delegate void LevelLoadedHandler();

    public RawImage background; //reference to the background texture

    public event LevelLoadedHandler LevelLoadedEvent;

    public bool levelLoaded;                    //indicates whether a level has been loaded or not

    public GameObject spawnerPrefab;            //prefab used to create spawners
    public GameObject towerPrefab;              //prefab used to create towers
    public GameObject explosionPrefab;          //prefab used to create explosions

    public static LevelManagerScript instance;  //singleton pattern
    public LevelData data;                      //data for the level itself
    public int currentWave;                     //which wave is current
    public bool waveOngoing;                    //indicates whether there is currently an active wave
    public int deadThisWave { get; set; }		//number of monsters dead this wave

    private int spawnCount;                     //how many enemies still need spawning this wave
    private int waveTotalRemainingHealth;       //health remaining across all enemies in this wave

    public LevelData Data { get { return data; } set { data = value; } }
    public int SpawnCount { get { return spawnCount; } }
    public int WaveTotalRemainingHealth { get { return waveTotalRemainingHealth; } set { waveTotalRemainingHealth = value; } }

    public List<GameObject> spawnerObjects;

    public float desiredTimeScale;

    // Use this for initialization
    private void Awake()
    {
        instance = this;
        waveOngoing = false;
        currentWave = 0;
        deadThisWave = 0;
        levelLoaded = false;
        spawnCount = -1;
        desiredTimeScale = 1.0f;
    }

    // Called after all objects have initialized
    private void Start()
    {
    }

    //loads the given level
    private IEnumerator loadLevel(string level)
    {
        data = LevelData.Load(Path.Combine(Application.dataPath, level)); //load the level

        //set background
        string filename = Application.dataPath + "/StreamingAssets/Art/Backgrounds/" + data.background + ".png";
        if (File.Exists(filename))
        {
            WWW www = new WWW ("file:///" + filename);
            yield return www;
            background.texture = www.texture;
        }
        else
        {
            Debug.LogWarning("Could not find background: " + filename);
        }

        //wait a few frames to give other managers a chance to catch up
        yield return null;
        yield return null;
        yield return null;

        //apply wave effects on predefined waves
        for( int i = 0; i < data.waves.Count; i++)
            if (data.waves[i].enemyData.effectData != null)
                foreach (IEffect e in data.waves[i].enemyData.effectData.effects)
                    if (e.effectType == EffectType.wave)
                        data.waves[i] = ((IEffectWave)e).alteredWaveData(data.waves[i]);

        //initialize waves
        for (uint i = 0; i < data.randomWaveCount; i++)
        {
            //figure out which wave we are making
            int wave = data.waves.Count + 1;

            //budget: wave * linear + (wave * squared)^2 + exponential^wave
            int waveBudget = Mathf.RoundToInt(data.waveGrowthAbsolute +								//absolute growth
			                                  (wave * data.waveGrowthLinear) + 	                    //linear growth
			                                  (Mathf.Pow( (wave * data.waveGrowthSquared), 2.0f)) +	//squared growth
			                                  (Mathf.Pow( data.waveGrowthExponential, wave)));      //exponential growth (WARNING: this gets HUGE!)

            //enemy type: random (TODO: maybe make harder enemy types more common in later waves?  How would we define this?)
            EnemyData waveEnemy = EnemyTypeManagerScript.instance.getRandomEnemyType(waveBudget);
            //dont spawn enemies that are more expensive than the entire wave budget
            while (waveEnemy.spawnCost > waveBudget)
            {
                waveEnemy = EnemyTypeManagerScript.instance.getRandomEnemyType(waveBudget);
            }

            //time: min(wave*linear, maxwavetime)
            float waveTime = Mathf.Min(wave*data.waveTimeLinear, data.waveTimeMax);

            //create the wave data
            WaveData waveData = new WaveData(waveEnemy.name, waveBudget, waveTime);

            //if there are wave effects on the enemy type, apply them now
            if (waveEnemy.effectData != null)
                foreach(IEffect e in waveEnemy.effectData.effects)
                    if (e.effectType == EffectType.wave)
                        waveData = ((IEffectWave)e).alteredWaveData(waveData);

            data.waves.Add(waveData);
        }

        //create the spawners
        foreach (SpawnerData sd in data.spawners)
        {
            GameObject s = (GameObject) GameObject.Instantiate(spawnerPrefab); //create spawner
            s.SendMessage("SetData", sd); //provide its data
            spawnerObjects.Add(s);
        }

        //create the towers
        foreach (PremadeTower pt in data.towers)
        {
            GameObject t = (GameObject) GameObject.Instantiate(towerPrefab, new Vector3(pt.x, pt.y, -3), Quaternion.identity);  //summon tower
            CardData c = CardTypeManagerScript.instance.getCardByName(pt.name);
            c.towerData.towerName = pt.name;
            t.SendMessage("SetData", c.towerData); //pass it the definition
            if ((c.effectData != null) && (c.effectData.effects.Count > 0))
                t.SendMessage("AddEffects", c.effectData); //pass it the effects

            //apply upgrades
            foreach (PremadeTowerUpgrade ptu in pt.upgrades)
            {
                for (int i = 0; i < ptu.Count; i++)
                {
                    t.SendMessage("Upgrade", CardTypeManagerScript.instance.getCardByName(ptu.Name).upgradeData);
                }
            }
        }

        //if no deck has been loaded yet, then use the level deck
        if (DeckManagerScript.instance.deckSize == 0)
        {
            XMLDeck levelDeck;
            if (data.levelDeck != null)
            {
                levelDeck = data.levelDeck; //the level deck is defined in the level file
            }
            else
            {
                //the level uses one of the premade decks in Decks.XML
                levelDeck = DeckManagerScript.instance.premadeDecks.getDeckByName(data.premadeDeckName);
            }

            DeckManagerScript.instance.SendMessage("SetDeck", levelDeck);

            //shuffle it, if the level file says to
            if (data.shuffleDeck)
                DeckManagerScript.instance.SendMessage("Shuffle");
        }

        //init wave stats
        UpdateWaveStats();

        //fire the level loaded event so interested objects can act on it
        LevelLoadedEvent();

        //and set the flag
        levelLoaded = true;
    }

    // Update is called once per frame
    private void Update()
    {
        //spacebar starts wave
        if (Input.GetKeyUp(KeyCode.Space) && waveOngoing == false && currentWave < data.waves.Count)
        {
            waveOngoing = true;
            StartCoroutine("spawnWave", data.waves[currentWave]);
        }

        //F toggles fast forward  (actual timeScale may still be lower if performance is bad.  see below)
        if (Input.GetKeyUp(KeyCode.F) && Time.timeScale > 0.0f)
        {
            if (desiredTimeScale == 1.0f)
                desiredTimeScale = 3.0f;
            else
                desiredTimeScale = 1.0f;
        }

        //attempt to regulate timeScale so the game slows down if the framerate tanks but then speeds back up when things settle down
        //the time scale will go down if frame rate is below the reduce threshold, and up if frame rate is aove the increase threshold
        const float timeScaleReduceThreshold = (1.0f / 10.0f);    //10 FPS
        const float timeScaleIncreaseThreshold = (1.0f / 20.0f);  //20 FPS
        const float timeScaleMin = 0.5f;                //minimum allowed sim speed
        const float timeScaleInterval = 0.1f;           //amount to adjust at each change

        if (Time.timeScale > desiredTimeScale) //if we are going faster than the player wants...
            Time.timeScale = desiredTimeScale; //then slow down!

        float unscaledSmoothDeltaTime = Time.smoothDeltaTime / Time.timeScale;  //smooth delta time scales by the sim speed, so we have to undo that for framerate calculations

        if (unscaledSmoothDeltaTime > timeScaleReduceThreshold)                                   //if the frame rate is too low...
            if (Time.timeScale > timeScaleMin)                                                    //and we are still above our minimum...
                Time.timeScale = Mathf.Max(timeScaleMin, Time.timeScale - timeScaleInterval);     //reduce it by the interval without allowing it to go below the minimum

        if (unscaledSmoothDeltaTime < timeScaleIncreaseThreshold)                                 //if the frame rate is doing well...
            if (Time.timeScale < desiredTimeScale)                                                //and the player wants a higher sim speed...
                Time.timeScale = Mathf.Min(desiredTimeScale, Time.timeScale + timeScaleInterval); //increase it by the interval without allowing it to go above the desired setting
    }

    // Handles spawning of a single wave
    private IEnumerator spawnWave(WaveData d)
    {
        //init vars
        EnemyData   spawnType = d.enemyData;
        int         spawnerCount = spawnerObjects.Count; //number of spawners
        if (spawnCount < 1) { spawnCount = 1; Debug.LogWarning("Wave spawn count was zero.  forced to spawn 1 monster."); } //spawn at least one monster
        float       timeBetweenSpawns = d.time / spawnCount; //delay between each spawn

        HandScript.playerHand.SendMessage("Hide"); //hide the hand

        //set type to spawners
        foreach (GameObject s in spawnerObjects)
        {
            s.SendMessage("SetType", spawnType);
        }

        //slight delay before spawning
        yield return new WaitForSeconds(1.0f);

        //spawn monsters.  Distribute spawns as evenly as possible
        int curSpawner = Random.Range(0, spawnerCount);
        float timeToNextSpawn = timeBetweenSpawns;
        while (spawnCount > 0)
        {
            yield return null;                 //wait for the next frame
            timeToNextSpawn -= Time.deltaTime; //reduce time until next spawn by amount of time between frames
            d.time -= Time.deltaTime;          //update the wave data also so that the status text can update
            while (timeToNextSpawn < 0.0)      //this is a loop in case multiple spawns happen in one frame
            {
                timeToNextSpawn += timeBetweenSpawns; //update spawn timer
                spawnerObjects[curSpawner].SendMessage("Spawn", timeToNextSpawn); //spawn enemy.  spawn timer provided so the enemy can place itself properly when framerate is low
                curSpawner = (curSpawner + 1) % spawnerCount; //move to next spawner, looping back to the first if we are at the end of the list
                spawnCount--; //update spawn counter

                //bail if we have finished spawning baddies
                if (spawnCount == 0)
                    break;
            }
        }

        //wait for all monsters to be dead
        bool monstersAlive = true;
        while (monstersAlive)
        {
            yield return new WaitForSeconds(1.0f);
            if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
                monstersAlive = false;
        }

        HandScript.playerHand.SendMessage("Show"); //show the hand

        //find all towers in the level and tell them a wave ended
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        foreach (GameObject t in towers)
        {
            t.SendMessage("WaveOver");
        }

        deadThisWave = 0;

        //draw new cards until seven in hand
        yield return new WaitForSeconds(1.0f);
        HandScript.playerHand.SendMessage("drawToHandSize", 10);

        //wave is over
        waveOngoing = false;
        currentWave++;

        //print message if this is the last wave
        if (currentWave == data.waves.Count)
        {
            yield return StartCoroutine(MessageHandlerScript.ShowAndYield("Level Complete!")); //tell user they won and wait for them to answer
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game"); //then restart the scene
            yield break;
        }

        //update stats for the next wave
        UpdateWaveStats();
    }

    //called when the wave changes to update the enemy spawn counter and health tracker
    public void UpdateWaveStats()
    {
        //show the wave message, if there is one, and then blank it out so it only shows once
        if (data.waves[currentWave].message != null)
        {
            MessageHandlerScript.ShowNoYield(data.waves[currentWave].message);
            data.waves[currentWave].message = null;
        }

        if (data.waves[currentWave].forcedSpawnCount > 0)
            spawnCount = data.waves[currentWave].forcedSpawnCount;
        else
            spawnCount = Mathf.RoundToInt(((float)data.waves[currentWave].budget / (float)data.waves[currentWave].enemyData.spawnCost));

        waveTotalRemainingHealth = spawnCount * data.waves[currentWave].enemyData.maxHealth;
    }

    //spawns an explosion
    public void createExplosion(BurstShotData data, Vector2 position)
    {
        GameObject instance = Instantiate(explosionPrefab);
        instance.transform.position = new Vector3(position.x, position.y, -3.0f);
        instance.SendMessage("SetData", data);

        //apply attackColor property, if it is present
        if (data.damageEvent.effects != null)
            if (data.damageEvent.effects.propertyEffects.attackColor != null)
                instance.SendMessage("SetColor", data.damageEvent.effects.propertyEffects.attackColor);
    }
}