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
    private EnemyDistanceToGoalComparer comparer;

    // Use this for initialization
    void Awake () {
        instance = this;
        activeEnemies = new List<GameObject>();
        comparer = new EnemyDistanceToGoalComparer();
	}
	
    //called when an enemy is spawned
    public void EnemySpawned (GameObject e)
    {
        //perform a sorted insert.  We expect this enemy to be near the end of the list, so start at the back
        //we insert it after the first enemy that is not further away than this one
        for (int i = activeEnemies.Count - 1; i > 0; i--)
        {
            if (comparer.Compare(activeEnemies[i], e) <= 0)
            {
                activeEnemies.Insert(i + 1, e);
                return;
            }
        }

        //if we made it here, then this enemy actually belongs at the START of the list
        activeEnemies.Insert(0, e);
    }

    //called when an enemy expects to die
    public void EnemyExpectedDeath (GameObject e)
    {
        activeEnemies.Remove(e);
    }

    //called when the enemy path changes, such as when the enemy mkaes it to the end and restarts
    public void EnemyPathChanged (GameObject e)
    {
        EnemyExpectedDeath(e);
        EnemySpawned(e);
    }

    //sorts the enemy list by how close they are to their goal
    void sortEnemyList ()
    {
        activeEnemies.Sort(comparer);
    }

	// Update is called once per frame
	void Update () {
        //sortEnemyList();
	}
}
