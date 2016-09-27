using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;

//represents everything needed to define a working spawner from xml.  This is seperated for ease of future expansion
[System.Serializable]
public class SpawnerData
{
    //position new enemies will spawn at
    [XmlAttribute][Hide] public float spawnX;
    [XmlAttribute][Hide] public float spawnY;

    [Show] public Vector2 spawnVec { get { return new Vector2(spawnX, spawnY); } set { spawnX = value.x; spawnY = value.y; } } //convenience accessor
    public override string ToString() { return spawnVec.ToString(); } //for better display in the debugger
}

public class SpawnerScript : BaseBehaviour
{
    public SpawnerData      data;           //xml definition of this spawner
    public GameObject       enemyPrefab;    //prefab used to create enemies

    public Vector2? forcedFirstDestination;

    public void Awake() { forcedFirstDestination = null; } //init

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

    //allows setting a spawn position that isnt at the end of a path segment by starting the path with spawnLocation->firstDestination and doing a normal pathfind from there
    public void forceFirstPath(Vector2 spawnLocation, Vector2 firstDestination)
    {
        spawnPos = spawnLocation;
        forcedFirstDestination = firstDestination;
    }

    //spawns an enemy
    public void Spawn(float timePassedSinceSpawn, EnemyData type)
    {
        GameObject enemy = (GameObject)Object.Instantiate(enemyPrefab, spawnPos, Quaternion.identity);  //spawn the enemy
        enemy.SendMessage("SetData", type);                                                             //set its type

        //set its path
        List<Vector2> path = null;
        if (forcedFirstDestination == null)
        {
            path = PathManagerScript.instance.CalculatePathFromPos(spawnPos);
        }
        else
        {
            //put the pathfinding in a try-catch since it can fail if we spawn on the last path segment
            try
            {
                path = PathManagerScript.instance.CalculatePathFromPos(forcedFirstDestination.Value);
            }
            catch (System.InvalidOperationException)
            {
                path = new List<Vector2>();
            }
            
            path.Insert(0, forcedFirstDestination.Value);
        }
        enemy.SendMessage("SetPath", path);        

        enemy.SendMessage("moveForwardByTime", timePassedSinceSpawn);                                   //move the enemy forward to account for how much time has passed between when this enemy should have spawned and when the spawner got told about it
        EnemyManagerScript.instance.EnemySpawned(enemy);                                                //report it to the enemy manager
    }
}