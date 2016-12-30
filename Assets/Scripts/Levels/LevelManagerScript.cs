using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;
using UnityEngine.Analytics;

/// <summary>
/// XML representation of an upgrade to be applied to a premade tower
/// Name: name of the upgrade card to use
/// Count: number of times to apply it
/// </summary>
[System.Serializable]
public class PremadeTowerUpgrade
{
    //upgrade to apply, with an inspector popup menu of valid upgrades
    private string[] getUpgradeNames() { return CardTypeManagerScript.instance.getUpgradeNames(); }
    [XmlAttribute]
    [Popup("getUpgradeNames",CaseSensitive = true,Filter = true,HideUpdate = true,TextField = true)]
    public string Name;  

    [XmlAttribute]
    public int Count;	//number of applications.  This is allowed to exceed the tower's upgrade cap

    public override string ToString() { return Name + "x" + Count; }
}

/// <summary>
/// XML representation of a tower that should be present at the start of a level
/// name: tower card to use
/// upgrades: a list of upgrades to apply to it
/// </summary>
[System.Serializable]
public class PremadeTower
{
    //name of the tower to place, with an inspector popup menu of valid towers
    private string[] getTowerNames() { return CardTypeManagerScript.instance.getTowerNames(); }
    [XmlAttribute]
    [Popup("getTowerNames",CaseSensitive = true,Filter = true,HideUpdate = true,TextField = true)]
    public string name; 

    [XmlArray("Upgrades")]
    [XmlArrayItem("Upgrade")]
    public List<PremadeTowerUpgrade> upgrades;  //a list of all upgrades to apply, in order

    //dont serialize the upgrade list if it is empty
    [XmlIgnore]
    public bool upgradesSpecified
    {
        get { return (upgrades != null) && (upgrades.Count > 0); }
        set { }
    }

    [XmlAttribute] public float x; //x to spawn at
    [XmlAttribute] public float y; //y to spawn at

    public override string ToString()
    {
        if ( (name == null) || (upgrades == null) )
        {
            return "null";
        }
        else
        {
            return name + "(" + upgrades.Count + " upgrades)";
        }
    }
};

/// <summary>
/// container for the level definition as defined through XML.
/// </summary>
[XmlRoot("Level")]
[System.Serializable]
public class LevelData
{
    //used to specify the proper .xsd file in the serialized xml
    [Hide]
    [XmlAttribute("noNamespaceSchemaLocation", Namespace = System.Xml.Schema.XmlSchema.InstanceNamespace)]
    public readonly string schema = "../Level.xsd";

    //where this level was loaded from
    [XmlIgnore] public string fileName;

    //difficulty of the level.  Appears on level select screen.
    public string difficulty;

    //a brief description that appears in the level select screen
    public string description;

    //comma separated lists of mod files that this level is dependent on, if any
    [XmlAttribute("enemyFileDependencies")][DefaultValue("")][Hide] public string enemyDependencies;
    [XmlAttribute( "cardFileDependencies")][DefaultValue("")][Hide] public string  cardDependencies;

    //level background
    [XmlAttribute("background")]
    public string background = "Default_bg";

    //wave generation parameters
    public int randomWaveCount;

    //budget: absolute + (wave * linear) + (wave * squared)^2 + (exponential^wave)
    [DefaultValue(50.0f)] public float waveGrowthAbsolute    = 50.0f;
    [DefaultValue(8.0f)]  public float waveGrowthLinear      = 8.0f;
    [DefaultValue(2.2f)]  public float waveGrowthSquared     = 2.2f;
    [DefaultValue(1.1f)]  public float waveGrowthExponential = 1.1f;

    //time: min(wave*linear, maxwavetime)
    [DefaultValue(1.1f)]  public float waveTimeLinear = 1.1f;
    [DefaultValue(20.0f)] public float waveTimeMax    = 20.0f;

    [XmlArray("Waves")]
    [XmlArrayItem("Wave")]
    [Display(Seq.GuiBox | Seq.LineNumbers | Seq.PerItemRemove)]
    public List<WaveData> waves;

    [XmlArray("Spawners")]
    [XmlArrayItem("Spawner")]
    [Display(Seq.GuiBox | Seq.PerItemRemove)]
    public List<SpawnerData> spawners;

    [XmlArray("PathSegments")]
    [XmlArrayItem("Segment")]
    [Display(Seq.GuiBox | Seq.PerItemRemove)]
    public List<PathSegment> pathSegments;

    [XmlArray("Towers")]
    [XmlArrayItem("Tower")]
    [Display(Seq.GuiBox| Seq.PerItemRemove)]
    public List<PremadeTower> towers;

    public bool shuffleDeck;

    //levelDeck is the deck set for use on this level.
    //It could be defined directly in the level file, or the level could just provide the name of a premade deck in Decks.xml instead.
    [XmlIgnore] public bool usingLevelDeck; //set by other objects so theyc an track whether this deck is actually in use
    [VisibleWhen("usingLevelDeck")] public XMLDeck levelDeck; //the deck itself, only shown if it is in use

    //only save the levelDeck to XML if we aren't using a premade deck
    [XmlIgnore] public bool levelDeckSpecified { get { return premadeDeckName == ""; } set { } }

    //provides a popup menu in the inspector to pick a premade deck
    private string[] getDeckNames() { return DeckManagerScript.instance.premadeDecks.getNames(); }
    [Popup("getDeckNames",CaseSensitive = true,Filter = true,HideUpdate = true,TextField = true)]
    [VisibleWhen("usingLevelDeck")]
    public string premadeDeckName; 

    //only serialize the premadeDeckName if there is one
    [XmlIgnore]
    public bool premadeDeckNameSpecified
    {
        get { return (premadeDeckName != null) && (premadeDeckName != ""); }
        set { }
    }

    //DEV: provides a button in the editor to save the level data
    [Show] private void SaveChanges()
    {
        Save(Path.Combine(Path.Combine(Application.streamingAssetsPath, "XML/Levels/"), fileName));
    } 

    /// <summary>
    /// saves the level data to a file of the given name
    /// </summary>
    public void Save(string path)
    {
        //temporarily remove random waves from the list
        List<WaveData> temp = new List<WaveData>(waves);
        waves.RemoveAll(wd => wd.isRandomWave);

        XmlSerializer serializer = new XmlSerializer(typeof(LevelData));

        using (StreamWriter stream = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
        {
            serializer.Serialize(stream, this);
        }

        //restore the list to how it was
        waves = temp;
    }

    /// <summary>
    /// returns a new LevelData created from the provided XML file
    /// </summary>
    public static LevelData Load(Stream levelStream, string fileName)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(LevelData));
        LevelData result = serializer.Deserialize(levelStream) as LevelData;
        result.fileName = fileName;
        return result;
    }
}


/// <summary>
/// workhorse class that is used to load a level.  Also does all the bookkeeping to keep it running, such as generating/tracking/spawning enemy waves
/// </summary>
public class LevelManagerScript : BaseBehaviour
{
    //other objects can refer to these to be informed when a level is loaded (https://unity3d.com/learn/tutorials/topics/scripting/events)
    public delegate void LevelLoadedHandler();
    public event LevelLoadedHandler LevelLoadedEvent;

    //and this one is for when a round is over
    public delegate void RoundOverHandler();
    public event RoundOverHandler RoundOverEvent;

    [Hide] public static LevelManagerScript instance;  //singleton pattern
    
    //object references (not visible during play, since there is no reason to modify them in-game)
    private bool shouldShowRefs() { return !Application.isPlaying; }     //function used to hide these when in game
    [VisibleWhen("shouldShowRefs")] public GameObject  spawnerPrefab;     //prefab used to create spawners
    [VisibleWhen("shouldShowRefs")] public GameObject  towerPrefab;       //prefab used to create towers
    [VisibleWhen("shouldShowRefs")] public GameObject  explosionPrefab;   //prefab used to create explosions
    [VisibleWhen("shouldShowRefs")] public GameObject  pathTooltipPrefab; //prefab used to create the path laying tooltip
    [VisibleWhen("shouldShowRefs")] public RawImage    background;        //reference to the background texture

    //audio settings
    [VisibleWhen("shouldShowRefs")] public AudioClip[] levelMusic;  //array of songs to use for playing music
    [VisibleWhen("shouldShowRefs")] public AudioSource musicSource; //audio source to use for playing level music
    [VisibleWhen("shouldShowRefs")] public float minSilence;        //minimum delay between songs
    [VisibleWhen("shouldShowRefs")] public float maxSilence;        //maximum delay between songs

    //data for the level itself
    [Hide] public bool levelLoaded;                     //indicates whether a level has been loaded or not
    [VisibleWhen("levelLoaded")] public LevelData data; //the actual level data (only visible if there is a loaded level to show)
    [Hide] public List<GameObject> spawnerObjects;      //list of the enemy spawner objects

    //current status (only visible if there is a loaded level, since the values are only meaningful in that context)
    [VisibleWhen("levelLoaded")] public int   wavesInDeck;          //number of enemy groups remaining in the deck
    [VisibleWhen("levelLoaded")] public int   wavesSpawning;        //number of waves currently attacking
    [VisibleWhen("levelLoaded")] public int   totalSpawnedThisWave; //how many enemies have already spawned this wave
    [VisibleWhen("levelLoaded")] public float desiredTimeScale;     //the game speed the player wants to play at
    [VisibleWhen("levelLoaded")] public bool  endurance;            //whether or not we are playing endurance

    //private vars
    private int        totalSpawnCount;          //how many enemies still need spawning this wave
    private int        waveTotalRemainingHealth; //health remaining across all enemies in this wave
    private GameObject pathTooltip;              //tooltip used for laying paths

    //properties
    public LevelData Data { get { return data; } set { data = value; } } 
    public int SpawnCount { get { return totalSpawnCount; } }
    public int totalRemainingHealth { get { return waveTotalRemainingHealth; } set { waveTotalRemainingHealth = value; } }
    public float currentWaveTime { get { return Mathf.Max( ((data.waves.Count - wavesInDeck) * data.waveTimeLinear), data.waveTimeMax); } } //returns the time that the current wave should take to spawn.  used for survivors.

    // Use this for initialization
    private void Awake()
    {
        //shuffle music
        for (int i = 0; i < levelMusic.Length; i++)
        {
            int j = UnityEngine.Random.Range(0, levelMusic.Length);
            AudioClip temp = levelMusic[i];
            levelMusic[i] = levelMusic[j];
            levelMusic[j] = temp;
        }

        //init vars
        instance = this;
        wavesSpawning = 0;
        wavesInDeck = 0;
        levelLoaded = false;
        totalSpawnCount = -1;
        desiredTimeScale = 1.0f;
        endurance = false;
    }

    /// <summary>
    /// [COROUTINE] loads the given level and sets up the scene to get it going
    /// </summary>
    private IEnumerator loadLevel(LevelData levelToLoad)
    {
        data = levelToLoad;

        //test dependencies, if we are on a platform that does that
        if (Application.platform != RuntimePlatform.WebGLPlayer)
        {
            //wait for dependency manager to be ready to test dependencies
            while (DependencyManagerScript.instance == null || DependencyManagerScript.instance.enemyDepenciesHandled == false || DependencyManagerScript.instance.cardDependenciesHandled == false)
                yield return null;

            //test for mod dependencies.  If unmet, show message and reload the scene
            if (DependencyManagerScript.instance.testLevelDependencies(data) == false)
            {
                MessageHandlerScript.Error("Could not load level: unmet dependencies");
                UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
                yield break;
            }
        }

        //set background using WWW, even if we are not on a web build, since this is the "right" way to load images at runtime
        string filename = Application.streamingAssetsPath + "/Art/Backgrounds/" + data.background;
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            filename = "file:///" + filename;

        WWW www = new WWW (filename);
        yield return www;

        if (www.error == null)
            background.texture = www.texture;
        else
            Debug.LogWarning("Could not load background: " + filename + " (" + www.error + ")");

        //if no deck has been loaded yet, then use the level deck
        if (DeckManagerScript.instance.deckSize == 0)
            loadLevelDeck();

        //wait a few frames to give other managers a chance to catch up
        yield return null;
        yield return null;
        yield return null;

        //apply wave effects on predefined waves
        for (int i = 0; i < data.waves.Count; i++)
        {
            if (data.waves[i].enemyData.effectData != null)
                foreach (IEffect e in data.waves[i].enemyData.effectData.effects)
                    if (e.triggersAs(EffectType.wave))
                        data.waves[i] = ((IEffectWave)e).alteredWaveData(data.waves[i]);

            //Debug.Log("wave " + (i+1) + ": " + data.waves[i].ToString());
        }

        //init wave count
        wavesInDeck = data.waves.Count;

        //generate the random waves
        for (uint i = 0; i < data.randomWaveCount; i++)
        {
            generateRandomWave();
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
            TowerScript t = ((GameObject)Instantiate(towerPrefab, new Vector3(pt.x, pt.y, -3), Quaternion.identity)).GetComponent<TowerScript>();  //summon tower
            PlayerCardData c = CardTypeManagerScript.instance.getCardByName(pt.name);
            c.towerData.towerName = pt.name;
            t.SetData(c.towerData); //pass it the definition
            if ((c.effectData != null) && (c.effectData.effects.Count > 0))
                t.AddEffects(c.effectData); //pass it the effects

            //trigger effects
            t.triggerOnTowerSpawned();

            //apply upgrades
            foreach (PremadeTowerUpgrade ptu in pt.upgrades)
            {
                for (int i = 0; i < ptu.Count; i++)
                {
                    //we need a different message depending on whether or not the upgrade costs a slot
                    PlayerCardData upgradeCardData = CardTypeManagerScript.instance.getCardByName(ptu.Name);

                    if ( (upgradeCardData.effectData != null) && (upgradeCardData.effectData.propertyEffects.noUpgradeCost) )
                        t.FreeUpgrade(upgradeCardData.upgradeData);
                    else
                        t.UpgradeIgnoreCap(upgradeCardData.upgradeData);

                    //also give it the effects
                    if (upgradeCardData.effectData != null)
                    {
                        t.AddEffects(upgradeCardData.effectData);
                    }
                }
            }
        }

        //set the flag and wait a moment to let objects that were waiting on this finish their initializations
        levelLoaded = true;
        yield return null;

        //init wave stats
        UpdateWaveStats();

        //start the music
        StartCoroutine(playMusic());

        //fire the level loaded event so interested objects can act on it
        LevelLoadedEvent();

        //track it in analytics
        AnalyticsResult ar = Analytics.CustomEvent("levelLoaded", new Dictionary<string, object> { {"levelName", data.fileName} });
        if (ar != AnalyticsResult.Ok)
            Debug.LogWarning("Failed to track levelLoaded: " + ar + ')');

        //fire every round effects on any premade towers
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Tower"))
        {
            TowerScript t = go.GetComponent<TowerScript>();
            if (t.effects != null)
                foreach (IEffect e in t.effects.effects)
                    if (e.triggersAs(EffectType.everyRound))
                        ((IEffectInstant)e).trigger();
        }
    }

    /// <summary>
    /// generates a random wave and adds it to the list
    /// </summary>
    public void generateRandomWave()
    {
        //figure out which wave we are making
        int wave = data.waves.Count + 1;

        //budget: wave * linear + (wave * squared)^2 + exponential^wave
        int waveBudget = Mathf.RoundToInt(data.waveGrowthAbsolute +								    //absolute growth
			                                  (wave * data.waveGrowthLinear) + 	                    //linear growth
			                                  (Mathf.Pow( (wave * data.waveGrowthSquared), 2.0f)) +	//squared growth
			                                  (Mathf.Pow( data.waveGrowthExponential, wave)));      //exponential growth (WARNING: this gets HUGE!)

        if (waveBudget < 0)
        {
            waveBudget = 0;
            Debug.LogWarning("negative wave budget!  reset to 0.");
        }

        //enemy type: random (TODO: maybe make harder enemy types more common in later waves?  How would we define this?)
        EnemyData waveEnemy = EnemyTypeManagerScript.instance.getRandomEnemyType(waveBudget);
        waveEnemy = EnemyTypeManagerScript.instance.getRandomEnemyType(waveBudget).clone(); //tries to find an enemy type that the current budget can afford

        //forbid random generation from producing multiple ping waves in succession
        int loopCount = 0;
        while ((wave > 1) && (waveEnemy.name == "Ping") && (data.waves[data.waves.Count - 1].enemyData.name == "Ping")) //while both this and the ping wave are Ping...
        {
            waveEnemy = EnemyTypeManagerScript.instance.getRandomEnemyType(waveBudget).clone(); //change this to something else

            //just in case: avoid infinite loops by only trying up to 100 times
            loopCount++;
            if (loopCount == 100)
            {
                Debug.LogWarning("Random wave generation is still picking Ping aft 100 tries!  Giving up and letting it have sequential Ping waves.");
                break;
            }
        }

        //time: min(wave*linear, maxwavetime)
        float waveTime = Mathf.Min(wave*data.waveTimeLinear, data.waveTimeMax);

        //create the wave data
        WaveData waveData = new WaveData(waveEnemy, waveBudget, waveTime);

        //mark it as a random wave so it doesnt get saved from the inspector
        waveData.isRandomWave = true;

        //if there are wave effects on the enemy type, apply them now
        if (waveEnemy.effectData != null)
            foreach (IEffect e in waveEnemy.effectData.effects)
                if (e.triggersAs(EffectType.wave))
                    waveData = ((IEffectWave)e).alteredWaveData(waveData);

        //Debug.Log("wave " + wave + ": " + waveData.ToString());
        data.waves.Add(waveData);
        wavesInDeck++;
    }

    /// <summary>
    /// plays the first song in the list, waits a random period, then plays the next.  loops back around if it needs to
    /// </summary>
    /// <returns></returns>
    private IEnumerator playMusic()
    {
        int curSong = 0;
        while (true)
        {
            musicSource.clip = levelMusic[curSong];
            musicSource.Play();

            while (musicSource.isPlaying)
                yield return new WaitForSeconds(0.1f);

            curSong = (curSong + 1) % levelMusic.Length;
            yield return new WaitForSeconds(UnityEngine.Random.Range(minSilence, maxSilence));
        }
    }

    /// <summary>
    /// sends the deck defined as the level deck to the deck manager, and shuffles if needed
    /// this is public so that deck manager can call it to reload the level deck without needing to worry about the logistics of how that is done.
    /// </summary>
    public void loadLevelDeck()
    {
        data.usingLevelDeck = true;
        XMLDeck levelDeck;
        if ((data.levelDeck != null) && (data.levelDeck.cardCount > 0))
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

    // Update is called once per frame
    private void Update()
    {
        //spacebar starts wave
        if (Input.GetKeyUp(KeyCode.Space) && wavesSpawning == 0 && HandScript.enemyHand.currentHandSize > 0)
        {
            StartCoroutine("spawnWaves");
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
        //the time scale will go down if frame rate is below the reduce threshold, and up if frame rate is above the increase threshold
        const float timeScaleReduceThreshold = (1.0f / 10.0f);    //10 FPS
        const float timeScaleIncreaseThreshold = (1.0f / 15.0f);  //15 FPS
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

    /// <summary>
    /// [COROUTINE] spawns all of the incoming waves
    /// </summary>
    private IEnumerator spawnWaves()
    {
        //start the waves
        foreach (WaveData d in HandScript.enemyHand.IncomingWaves)
            StartCoroutine(spawnWave(d));

        //wait for them to finish
        while (wavesSpawning > 0)
            yield return null;

        //wait for all monsters to be dead
        while (true)
        {
            yield return new WaitForSeconds(1.0f);
            if (wavesSpawning <= 0)
                if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
                    break;
        }

        HandScript.playerHand.SendMessage("Show"); //show the hand

        //find all towers in the level and tell them a wave ended
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        foreach (GameObject t in towers)
        {
            t.SendMessage("WaveOver");
        }

        //draw
        yield return new WaitForSeconds(1.0f);
        HandScript.playerHand.drawCard();

        //if there are any survivors, draw a new survivor card to represent them
        if ((EnemyManagerScript.instance.survivors != null) && (EnemyManagerScript.instance.survivors.Count > 0))
        {
            HandScript.enemyHand.drawCard(true, true, true, true);
        }
        else if ( //if there were no survivors... 
                  (wavesInDeck == 0) && //and there are no more enemies in the deck... 
                  (HandScript.enemyHand.currentHandSize == 0) && //and the enemy hand is empty...
                  (LevelManagerScript.instance.endurance == false) ) //and we are not in endurance...
        {
            //then the player wins!

            //tell user they won and wait for them to answer
            yield return StartCoroutine(MessageHandlerScript.ShowAndYield("Level Complete!\n" + ScoreManagerScript.instance.report(true, false))); 

            //prompt userr to continue in endurance
            yield return StartCoroutine(MessageHandlerScript.PromptYesNo("Continue in endurance?"));

            if (MessageHandlerScript.responseToLastPrompt == "Yes")
            {
                LevelManagerScript.instance.endurance = true;               //if player wants to, then set it to endurance and keep going
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Game"); //if not, then restart the scene
                yield break;
            }
        }

        //draw a new enemy card
        HandScript.enemyHand.drawCard();

        //fire event
        RoundOverEvent();

        //update stats for the next wave
        UpdateWaveStats();
    }

    /// <summary>
    ///  [COROUTINE] Handles spawning of a single wave
    /// </summary>
    private IEnumerator spawnWave(WaveData d)
    {
        //flag the wave as started
        wavesSpawning++;

        //init vars
        int   spawnerCount = spawnerObjects.Count;          //number of spawners
        float timeBetweenSpawns = d.time / totalSpawnCount; //delay between each spawn

        HandScript.playerHand.SendMessage("Hide"); //hide the hand

        //slight delay before spawning
        yield return new WaitForSeconds(1.0f);

        //spawn monsters.  Distribute spawns as evenly as possible
        int curSpawner = UnityEngine.Random.Range(0, spawnerCount);
        float timeToNextSpawn = 0;
        while ( (d.spawnCount - d.spawnedThisWave) > 0)
        {
            yield return null;                 //wait for the next frame
            timeToNextSpawn -= Time.deltaTime; //reduce time until next spawn by amount of time between frames
            d.time -= Time.deltaTime;          //update the wave data also so that the status text can update
            while (timeToNextSpawn < 0.0)      //this is a loop in case multiple spawns happen in one frame
            {
                if (d.isSurvivorWave)
                {
                    //special case: survivor waves are responsible for their own spawning
                    d.spawn();
                }
                else
                {
                    //standard case: spawners are responsible for spawning
                    spawnerObjects[curSpawner].GetComponent<SpawnerScript>().Spawn(-timeToNextSpawn, d.enemyData); //spawn enemy.  spawn timer provided so the enemy can place itself properly when framerate is low
                    curSpawner = (curSpawner + 1) % spawnerCount; //move to next spawner, looping back to the first if we are at the end of the list
                }

                timeToNextSpawn += timeBetweenSpawns; //update spawn timer

                //update spawn counters
                d.spawnedThisWave++;
                totalSpawnedThisWave++;

                //bail if we have finished spawning baddies
                if (d.spawnedThisWave == d.spawnCount)
                    break;
            }
        }

        //wave is over
        wavesSpawning--;

        //discard the card associated with this wave
        HandScript.enemyHand.discardWave(d);

        //count this as a cleared wave for scoring (this counts waves that still had surviving enemies, but does not count waves made up from those survivors.)
        if (d.isSurvivorWave == false)
            ScoreManagerScript.instance.wavesCleared++;
    }

    /// <summary>
    /// [COROUTINE] called by effects to spawn enemies mid-wave
    /// </summary>
    /// <param name="wave">the wave to spawn</param>
    /// <param name="spawnLocation">where to spawn it</param>
    /// <param name="firstDestination">the first location for them to travel to before pathfinding.  This should be the start or end of a path segment.</param>
    public IEnumerator spawnWaveAt(WaveData wave, Vector2 spawnLocation, Vector2 firstDestination)
    {
        //flag the wave as started
        wavesSpawning++;

        totalRemainingHealth += wave.totalRemainingHealth;
        totalSpawnCount += wave.spawnCount;

        //init
        float timeBetweenSpawns = wave.time / wave.spawnCount; //delay between each spawn
        SpawnerScript spawner = ((GameObject)Instantiate(spawnerPrefab)).GetComponent<SpawnerScript>();
        spawner.forceFirstPath(spawnLocation, firstDestination);

        //slight delay before spawning
        yield return new WaitForSeconds(0.1f);

        //spawn monsters.
        float timeToNextSpawn = 0;
        while ((wave.spawnCount - wave.spawnedThisWave) > 0)
        {
            yield return null;                 //wait for the next frame
            timeToNextSpawn -= Time.deltaTime; //reduce time until next spawn by amount of time between frames
            wave.time -= Time.deltaTime;       //update the wave data also so that the status text can update
            while (timeToNextSpawn < 0.0)      //this is a loop in case multiple spawns happen in one frame
            {
                spawner.Spawn(-timeToNextSpawn, wave.enemyData); //spawn enemy.  spawn timer provided so the enemy can place itself properly when framerate is low

                timeToNextSpawn += timeBetweenSpawns; //update spawn timer

                //update spawn counters
                wave.spawnedThisWave++;
                totalSpawnedThisWave++;

                //bail if we have finished spawning baddies
                if (wave.spawnedThisWave == wave.spawnCount)
                    break;
            }
        }

        //wave is over
        wavesSpawning--;
        Destroy(spawner.gameObject);
        yield break;
    }

    /// <summary>
    /// called when the wave changes to update the enemy spawn counter and health tracker
    /// </summary>
    public void UpdateWaveStats()
    {
        HandScript.enemyHand.UpdateWaveStats();
        totalSpawnCount = HandScript.enemyHand.spawnCount;
        totalRemainingHealth = HandScript.enemyHand.totalRemainingHealth;
        totalSpawnedThisWave = 0;
    }

    /// <summary>
    /// spawns an explosion
    /// </summary>
    /// <param name="data">attack data for the explosion</param>
    /// <param name="position">where to put it</param>
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

    /// <summary>
    /// called by the enemy hand when it wants to draw a new enemy card.  Updates the wave counter and returns the wave on top of the deck.
    /// if the deck is empty, this either generates a new wave to draw or returns null, depending on if we are in endurance mode
    /// </summary>
    public WaveData DrawEnemy()
    {
        //special case: enemy deck empty
        if (wavesInDeck == 0)
        {
            if (endurance)
                generateRandomWave(); //in endurance, we generate a new wave on the spot and draw that
            else
                return null; //otherwise, we return null to indicate the deck is empty
        }

        //general case: return the next wave
        return data.waves[data.waves.Count - wavesInDeck--];
    }

    /// <summary>
    /// DEV: creates a button in the unity debugger to reload the level from scratch
    /// </summary>
    [Show][VisibleWhen("levelLoaded")] private void reloadLevel() { StopAllCoroutines(); StartCoroutine(reloadLevelCoroutine()); }

    /// <summary>
    /// [COROUTINE] destroys everything being used by the current level, resets all the managers, and then loads the level again
    /// </summary>
    public IEnumerator reloadLevelCoroutine()
    {
        yield return null;

        bool usingLevelDeck = data.usingLevelDeck; //backup this flag since the reload will force it to false

        //delete objects from the level we already have open
        levelLoaded = false;
        foreach (GameObject e in GameObject.FindGameObjectsWithTag("Enemy")) Destroy(e);
        foreach (GameObject e in GameObject.FindGameObjectsWithTag("EnemySpawner")) Destroy(e);
        spawnerObjects.Clear();
        foreach (GameObject e in GameObject.FindGameObjectsWithTag("Tower")) Destroy(e);
        foreach (GameObject e in GameObject.FindGameObjectsWithTag("Bullet")) Destroy(e);
        foreach (GameObject e in GameObject.FindGameObjectsWithTag("AreaAttack")) Destroy(e);
        foreach (GameObject e in GameObject.FindGameObjectsWithTag("Path")) Destroy(e);
        data.waves.RemoveAll(wd => wd.isRandomWave);

        //dump the hands
        HandScript.playerHand.SendMessage("Show");
        yield return StartCoroutine(HandScript.playerHand.discardRandomCards(null, 999, false));
        yield return StartCoroutine(HandScript.enemyHand.discardRandomCards(null, 999, false));

        Debug.Log("Reloading Level...");

        //reset game objects
        EnemyManagerScript.instance.SendMessage("Reset");
        DeckManagerScript.instance.SendMessage("Reset");
        ScoreManagerScript.instance.SendMessage("Reset");
        PathManagerScript.instance.SendMessage("Reset");
        HandScript.playerHand.SendMessage("Reset");
        HandScript.enemyHand.SendMessage("Reset");

        //reload the level
        Awake();
        yield return StartCoroutine(loadLevel(data));

        data.usingLevelDeck = usingLevelDeck; //restore the level deck flag to its original value
    }

    /// <summary>
    /// [DEV] spawns a pathTooltip into the world, if there is not one already, to use for laying new paths
    /// if the tooltip already exists, destroy it
    /// </summary>
    [Show][VisibleWhen("levelLoaded")] private void addPaths()
    {
        if (pathTooltip == null)
        {
            pathTooltip = GameObject.Instantiate(pathTooltipPrefab);
            pathTooltip.transform.SetParent(HandScript.playerHand.transform.root); //put it in the UI Canvas, found through the player hand since it must also be in the UI Canvas.
        }
        else
        {
            Destroy(pathTooltip);
        }
    }

    /// <summary>
    /// [DEV] removes all spawners from the level then adds new ones at every path that begins where no other path ends
    /// </summary>
    [Show][VisibleWhen("levelLoaded")] private void autoPlaceSpawners()
    {
        //destroy the existing spawners
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("EnemySpawner"))
            Destroy(go);
        spawnerObjects.Clear();

        //clear the spawner list
        data.spawners.Clear();

        //for every path that starts where no other path ends...
        foreach(PathSegment path in data.pathSegments.FindAll(s => !data.pathSegments.Exists(ss => s.startPos == ss.endPos)))
        {
            //... add a spawner to the list
            SpawnerData newData = new SpawnerData();
            newData.spawnVec = path.startPos;
            data.spawners.Add(newData);
        }

        //create the spawner objects
        foreach (SpawnerData sd in data.spawners)
        {
            GameObject s = (GameObject) GameObject.Instantiate(spawnerPrefab); //create spawner
            s.SendMessage("SetData", sd); //provide its data
            spawnerObjects.Add(s);
        }
    }

    /// <summary>
    /// adds the a path segment from startPos to endPos into the world, updating the levelData accordingly
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    public void addPathSegment(Vector2 startPos, Vector2 endPos)
    {
        //skip if start and end are the same
        if (startPos == endPos)
            return;

        //construct the new segment
        PathSegment newSegment = new PathSegment();
        newSegment.startPos = startPos;
        newSegment.endPos = endPos;

        //report and add it
        Debug.Log("new path: " + newSegment.ToString());
        data.pathSegments.Add(newSegment);

        //destroy the paths and create them again to reflect the new addition
        foreach (GameObject e in GameObject.FindGameObjectsWithTag("Path"))
            Destroy(e);
        PathManagerScript.instance.SendMessage("Reset");
    }
}