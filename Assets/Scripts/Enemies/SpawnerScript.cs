using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

//represents everything needed to define a working spawner from xml.  This is seperated for ease of future expansion
[System.Serializable]
public class SpawnerData {
	//position new enemies will spawn at
	[XmlAttribute] public float spawnX;
	[XmlAttribute] public float spawnY;
}

public class SpawnerScript : MonoBehaviour {

	public SpawnerData 		data;			//xml definition of this spawner
	public EnemyData		enemyType;		//data to be assigned to new enemies
	public GameObject 		enemyPrefab;	//prefab used to create enemies

	//accessor to allow treating spawn position as a vector
	public Vector2 spawnPos {
		get { 
			return new Vector2 (data.spawnX, data.spawnY);
		}
		set {
			data.spawnX = value.x;
			data.spawnY = value.y;
		}
	}

	// Use this for initialization
	void Awake () {
	
	}
	
	// Update is called once per frame
	void Update () {

	}

	//sets data for this spawner
	void SetData (SpawnerData newData) {
		data = newData;
	}

	//sets the enemy type to be spawned
	void SetType (EnemyData t) {
		enemyType = t;
	}

	//spawns an enemy
	void Spawn () {
		GameObject enemy = (GameObject)Object.Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
		enemy.SendMessage("SetData", enemyType);
		enemy.SendMessage("SetPath", PathManagerScript.instance.CalculatePathFromPos(new Vector2(data.spawnX, data.spawnY)));
	}
}
