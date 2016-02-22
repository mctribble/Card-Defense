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

	//default.  these values are meant to be VERY noticable if a wave is left with default data
	public WaveData () {
		type = "Swarm";
		budget = 999999999;
		time = 300.0f;
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
	const float WAVEGROWTH_LINEAR = 6.0f;
	const float WAVEGROWTH_SQUARED = 3.2f;
	const float WAVEGROWTH_EXPONENTIAL = 1.2f;

	//time: min(wave*linear, maxwavetime)
	const float WAVETIME_LINEAR = 1.1f;
	const float WAVETIME_MAX = 20.0f;

	public bool levelLoaded;	//indicates whether a level has been loaded or not

	public GameObject spawnerPrefab; 			//prefab used to create spawners
	public GameObject towerPrefab;				//prefab used to create towers

	public static LevelManagerScript instance;	//singleton pattern
	public LevelData data;						//data for the level itself
	public int  currentWave;					//which wave is current
	public bool waveOngoing;					//indicates whether there is currently an active wave
	public int deadThisWave { get; set; }		//number of monsters dead this wave

	public LevelData Data { get { return data; } set { data = value; } }

	public List<GameObject> spawnerObjects;

	// Use this for initialization
	void Awake () {
		instance = this;
		waveOngoing = false;
		currentWave = 0;
		deadThisWave = 0;
		levelLoaded = false;
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
				Time.timeScale =  2.0f;
			else
				Time.timeScale = 1.0f;
		}
	}

	// Handles spawning of a single wave
	IEnumerator spawnWave(WaveData d) {
		//init vars
		EnemyData 	spawnType = d.getEnemyData ();
		int 		spawnerCount = spawnerObjects.Count;								//number of spawners
		float 		rawSpawnCount = (float)d.budget / (float)spawnType.spawnCost;		//unrounded number of monsters to spawn
		int 		spawnCount = Mathf.RoundToInt (rawSpawnCount);						//rounded number of monsters to spawn
		if (spawnCount < 1) {spawnCount = 1; Debug.LogWarning("Wave spawn count was zero.  forced to spawn 1 monster.");} //spawn at least one monster
		float		timeBetweenSpawns = d.time / spawnCount;							//delay between each spawn

		GameObject.FindGameObjectWithTag ("Hand").SendMessage ("Hide"); //hide the hand

		//set type to spawners
		foreach (GameObject s in spawnerObjects) {
			s.SendMessage("SetType", spawnType);
		}

		//slight delay before spawning
		yield return new WaitForSeconds (1.0f);

		//spawn monsters.  each one is placed at a random spawn point.
		for (uint i = 0; i < spawnCount; i++) {
			spawnerObjects[ Random.Range(0, spawnerCount) ].SendMessage("Spawn"); 
			yield return new WaitForSeconds (timeBetweenSpawns);						
		}

		//wait for all monsters to be dead
		bool monstersAlive = true;
		while (monstersAlive) {
			yield return new WaitForSeconds (1.0f);
			if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
				monstersAlive = false;
		}

		//wave is over
		waveOngoing = false;
		currentWave++;
		GameObject.FindGameObjectWithTag ("Hand").SendMessage ("Show"); //show the hand
		deadThisWave = 0;

		//print message if this is the last wave TODO: handle end of level
		if (currentWave == data.waves.Count) {
			Debug.Log ("LEVEL COMPLETE!");
			Time.timeScale = 0.0f;
		}

		//draw a new card
		yield return new WaitForSeconds (1.0f);
		GameObject.FindGameObjectWithTag ("Hand").SendMessage ("drawCard");
	}
}
