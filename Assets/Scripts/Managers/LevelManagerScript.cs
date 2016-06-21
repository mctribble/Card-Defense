using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;

//contains an upgrade and a number to indicate how many times it should be applied.  used by PremadeTower
[System.Serializable]
public class PremadeTowerUpgrade {
	[XmlAttribute] public string Name;	//upgrade to apply
	[XmlAttribute] public int	 Count;	//number of applications
}

//container that holds everything needed to create a tower at level start
[System.Serializable]
public class PremadeTower {
	[XmlAttribute] public string name;	//name of the tower to place

	[XmlArray("Upgrades")]
	[XmlArrayItem("Upgrade")]
	public List<PremadeTowerUpgrade> upgrades;	//a list of all upgrades to apply, in order

	[XmlAttribute] public float x;				//x to spawn at
	[XmlAttribute] public float y;				//y to spawn at
};

[XmlRoot("Level")]
[System.Serializable]
public class LevelData {

	public int randomWaveCount;
	
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
	public XMLDeck levelDeck;

	public void Save(string path)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(LevelData));

		using(StreamWriter stream = new StreamWriter( path, false, Encoding.GetEncoding("UTF-8")))
		{
			serializer.Serialize(stream, this);
		}
	}
	
	public static LevelData Load(string path)
	{
		XmlSerializer serializer = new XmlSerializer(typeof(LevelData));
		using(var stream = new FileStream(path, FileMode.Open))
		{
			return serializer.Deserialize(stream) as LevelData;
		}
	}
}

//contains all the data required to spawn a wave
[System.Serializable]
public class WaveData {
	[XmlAttribute] public string type;
	[XmlAttribute] public int budget;
	[XmlAttribute] public float time;
    [XmlAttribute] public string message;

	//default.  these values are meant to be VERY noticable if a wave is left with default data
	public WaveData () {
		type = "Swarm";
		budget = 999999999;
		time = 300.0f;
        message = "UNSPECIFIED WAVE!";
	}

	//specific data
	public WaveData (string waveType, int waveBudget, float waveTime) {
		type = waveType;
		budget = waveBudget;
		time = waveTime;
	}

	public EnemyData getEnemyData() {
		return EnemyTypeManagerScript.instance.getEnemyTypeByName (type);
	}
}

public class LevelManagerScript : MonoBehaviour {
    //wave generation parameters
    //budget: absolute + (wave * linear) + (wave * squared)^2 + (exponential^wave)
    const float WAVEGROWTH_ABSOLUTE = 40.0f;
    const float WAVEGROWTH_LINEAR = 4.0f;
    const float WAVEGROWTH_SQUARED = 2.2f;
    const float WAVEGROWTH_EXPONENTIAL = 1.1f;

    //time: min(wave*linear, maxwavetime)
    const float WAVETIME_LINEAR = 1.1f;
    const float WAVETIME_MAX = 20.0f;

    public bool levelLoaded;    //indicates whether a level has been loaded or not

    public GameObject spawnerPrefab;            //prefab used to create spawners
    public GameObject towerPrefab;              //prefab used to create towers

    public static LevelManagerScript instance;  //singleton pattern
    public LevelData data;                      //data for the level itself
    public int currentWave;                     //which wave is current
    public bool waveOngoing;                    //indicates whether there is currently an active wave
    public int deadThisWave { get; set; }		//number of monsters dead this wave

    private int spawnCount; //how many enemies still need spawning this wave
    private int waveTotalRemainingHealth; //health remaining across all enemies in this wave

    public LevelData Data { get { return data; } set { data = value; } }
    public int SpawnCount { get { return spawnCount; } }
    public int WaveTotalRemainingHealth { get { return waveTotalRemainingHealth; } set { waveTotalRemainingHealth = value; } }

	public List<GameObject> spawnerObjects;

	// Use this for initialization
	void Awake () {
		instance = this;
		waveOngoing = false;
		currentWave = 0;
		deadThisWave = 0;
		levelLoaded = false;
        spawnCount = -1;
	}

	// Called after all objects have initialized
	void Start () {

	}

	//loads the given level
	IEnumerator loadLevel (string level) {
		data = LevelData.Load (Path.Combine(Application.dataPath, level)); //load the level

		yield return null; //wait a frame to make sure things load right
		levelLoaded = true; //set flag
		//wait a couple frames to give other managers a chance to catch up
		yield return null; 
		yield return null; 

		for (uint i = 0; i < data.randomWaveCount; i++) {
			//figure out which wave we are making
			int wave = data.waves.Count + 1;
			
			//budget: wave * linear + (wave * squared)^2 + exponential^wave
			int waveBudget = Mathf.RoundToInt(WAVEGROWTH_ABSOLUTE +								//absolute growth
			                                  (wave * WAVEGROWTH_LINEAR) + 	//linear growth
			                                  (Mathf.Pow( (wave * WAVEGROWTH_SQUARED), 2.0f)) +	//squared growth
			                                  (Mathf.Pow( WAVEGROWTH_EXPONENTIAL, wave)));		//exponential growth (WARNING: this gets HUGE!)
			
			//enemy type: random (TODO: maybe make harder enemy types more common in later waves?  How would we define this?)
			EnemyData waveEnemy = EnemyTypeManagerScript.instance.getRandomEnemyType();
			//dont spawn enemies that are more expensive than the entire wave budget
			while (waveEnemy.spawnCost > waveBudget)
			{
				waveEnemy = EnemyTypeManagerScript.instance.getRandomEnemyType();
			}
			
			//time: min(wave*linear, maxwavetime)
			float waveTime = Mathf.Min(wave*WAVETIME_LINEAR, WAVETIME_MAX);
			
			data.waves.Add( new WaveData(waveEnemy.name, waveBudget, waveTime) );
		}
		
		//create the spawners
		foreach (SpawnerData sd in data.spawners) {
			GameObject s = (GameObject) GameObject.Instantiate(spawnerPrefab); //create spawner
			s.SendMessage("SetData", sd); //provide its data
			spawnerObjects.Add(s);
		}
		
		//create the towers
		foreach (PremadeTower pt in data.towers) {
			GameObject t = (GameObject) GameObject.Instantiate(towerPrefab, new Vector3(pt.x, pt.y, -3), Quaternion.identity);	//summon tower
			TowerData d = CardTypeManagerScript.instance.getCardByName(pt.name).TowerData;
			d.towerName = pt.name;
			t.SendMessage("SetData", d);							//pass it the definition
			
			//apply upgrades
			foreach (PremadeTowerUpgrade ptu in pt.upgrades) {
				for (int i = 0; i < ptu.Count; i++) {
					t.SendMessage("Upgrade", CardTypeManagerScript.instance.getCardByName(ptu.Name).upgradeData);
				}
			}
		}
		
		//set the deck
		DeckManagerScript.instance.SendMessage ("SetDeck", data.levelDeck);
        //shuffle it, if the level file says to
        if (data.shuffleDeck)
            DeckManagerScript.instance.SendMessage("Shuffle");

        //init wave stats
        UpdateWaveStats();
    }

	// Update is called once per frame
	void Update () {
		//spacebar starts wave
		if (Input.GetKeyUp (KeyCode.Space) && waveOngoing == false && currentWave < data.waves.Count) {
			waveOngoing = true;
			StartCoroutine("spawnWave",data.waves[currentWave]);
		}

		//F toggles fast forward
		if (Input.GetKeyUp (KeyCode.F) && Time.timeScale > 0.0f) {
			if (Time.timeScale == 1.0f)
				Time.timeScale =  3.0f;
			else
				Time.timeScale = 1.0f;
		}
	}

	// Handles spawning of a single wave
	IEnumerator spawnWave(WaveData d) {
		//init vars
		EnemyData 	spawnType = d.getEnemyData ();
		int 		spawnerCount = spawnerObjects.Count; //number of spawners
		if (spawnCount < 1) {spawnCount = 1; Debug.LogWarning("Wave spawn count was zero.  forced to spawn 1 monster.");} //spawn at least one monster
		float		timeBetweenSpawns = d.time / spawnCount; //delay between each spawn

		GameObject.FindGameObjectWithTag ("Hand").SendMessage ("Hide"); //hide the hand

		//set type to spawners
		foreach (GameObject s in spawnerObjects) {
			s.SendMessage("SetType", spawnType);
		}

		//slight delay before spawning
		yield return new WaitForSeconds (1.0f);

        //spawn monsters.  each one is placed at a random spawn point.
        float timeToNextSpawn = timeBetweenSpawns;
		while (spawnCount > 0) {
            yield return new WaitForFixedUpdate();  //wait for the next physics update
            timeToNextSpawn -= Time.fixedDeltaTime; //reduce time until next spawn by amount of time between physics updates
            d.time -= Time.fixedDeltaTime;          //update the wave data also so that the status text can update
            while (timeToNextSpawn < 0.0)           //this is a loop in case multiple spawns happen in one physics update
            {
                spawnerObjects[Random.Range(0, spawnerCount)].SendMessage("Spawn"); //spawn enemy
                spawnCount--; //update spawn counter
                timeToNextSpawn += timeBetweenSpawns; //update spawn timer

                //bail if we have finished spawning baddies
                if (spawnCount == 0)
                    break;
            }
		}

		//wait for all monsters to be dead
		bool monstersAlive = true;
		while (monstersAlive) {
			yield return new WaitForSeconds (1.0f);
			if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
				monstersAlive = false;
		}

		GameObject.FindGameObjectWithTag ("Hand").SendMessage ("Show"); //show the hand

        //find all towers in the level and tell them a wave ended
        GameObject[] towers = GameObject.FindGameObjectsWithTag("Tower");
        foreach (GameObject t in towers)
        {
            t.SendMessage("WaveOver");
        }

		deadThisWave = 0;

		//draw new cards until seven in hand
		yield return new WaitForSeconds (1.0f);
		GameObject.FindGameObjectWithTag ("Hand").SendMessage ("drawToHandSize", 10);

        //wave is over
        waveOngoing = false;
        currentWave++;

        //print message if this is the last wave TODO: handle end of level
        if (currentWave == data.waves.Count)
        {
            Debug.Log("LEVEL COMPLETE!");
            Time.timeScale = 0.0f;
            yield break;
        }

        //update stats for the next wave
        UpdateWaveStats();
    }

    //called when the wave changes to update the enemy spawn counter and health tracker
    public void UpdateWaveStats()
    {
        //show the wave message, if there is one, and then blank it out so it only shows once
        //TODO: replace this with an actual message box
        if (data.waves[currentWave].message != null)
        {
            Debug.Log(data.waves[currentWave].message);
            data.waves[currentWave].message = null;
        }

        spawnCount = Mathf.RoundToInt(((float)data.waves[currentWave].budget / (float)data.waves[currentWave].getEnemyData().spawnCost));
        waveTotalRemainingHealth = spawnCount * data.waves[currentWave].getEnemyData().maxHealth;
    }
}
