using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

//compares two enemies by their distance to their goals.  Used for sorting the enemy list (
public class EnemyDistanceToGoalComparer : Comparer<GameObject>
{
    public override int Compare(GameObject x, GameObject y)
    {
        return x.GetComponent<EnemyScript>().distanceToGoal().CompareTo( y.GetComponent<EnemyScript>().distanceToGoal() );
    }
}

//responsible for tracking active enemies and performing group operations on them
public class EnemyManagerScript : MonoBehaviour {

    public static EnemyManagerScript instance; //this class is a singleton
    public List<GameObject> activeEnemies; //excludes enemies that expect to die but have not yet done so

	// Use this for initialization
	void Awake () {
        instance = this;
        activeEnemies = new List<GameObject>();
	}
	
    //called when an enemy is spawned
    public void EnemySpawned (GameObject e)
    {
        activeEnemies.Add(e);
        //sortEnemyList();
    }

    //called when an enemy expects to die
    public void EnemyExpectedDeath (GameObject e)
    {
        activeEnemies.Remove(e);
    }

    //sorts the enemy list by how close they are to their goal
    void sortEnemyList ()
    {
        activeEnemies.Sort(new EnemyDistanceToGoalComparer());
    }

	// Update is called once per frame
	void Update () {
        //sortEnemyList();
	}
}
