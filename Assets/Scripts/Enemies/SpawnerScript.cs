using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;

//represents everything needed to define a working spawner from xml.  This is seperated for ease of future expansion
[System.Serializable]
public class SpawnerData
{
    //position new enemies will spawn at
    [XmlAttribute] public float spawnX;
    [XmlAttribute] public float spawnY;
}

public class SpawnerScript : BaseBehaviour
{
    public SpawnerData      data;           //xml definition of this spawner
    public GameObject       enemyPrefab;    //prefab used to create enemies

    //accessor to allow treating spawn position as a vector
    public Vector2 spawnPos
    {
        get
        {
            return new Vector2(data.spawnX, data.spawnY);
        }
        set
        {
            data.spawnX = value.x;
            data.spawnY = value.y;
        }
    }

    //sets data for this spawner
    private void SetData(SpawnerData newData)
    {
        data = newData;
    }

    //spawns an enemy
    public void Spawn(float timePassedSinceSpawn, EnemyData type)
    {
        GameObject enemy = (GameObject)Object.Instantiate(enemyPrefab, spawnPos, Quaternion.identity);                          //spawn the enemy
        enemy.SendMessage("SetData", type);                                                                                //set its type
        enemy.SendMessage("SetPath", PathManagerScript.instance.CalculatePathFromPos(new Vector2(data.spawnX, data.spawnY)));   //set its path
        enemy.SendMessage("moveForwardByTime", timePassedSinceSpawn);                                                           //move the enemy forward to account for how much time has passed between when this enemy should have spawned and when the spawner got told about it
        EnemyManagerScript.instance.EnemySpawned(enemy);                                                                        //report it to the enemy manager
    }
}