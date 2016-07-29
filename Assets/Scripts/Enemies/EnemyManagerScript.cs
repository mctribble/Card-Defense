using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;

//compares two enemies by their distance to their goals.  Used for sorting the enemy list (
public class EnemyDistanceToGoalComparer : Comparer<GameObject>
{
    public override int Compare(GameObject x, GameObject y)
    {
        return x.GetComponent<EnemyScript>().distanceToGoal().CompareTo(y.GetComponent<EnemyScript>().distanceToGoal());
    }
}

//responsible for tracking active enemies and performing group operations on them
public class EnemyManagerScript : BaseBehaviour
{
    public static EnemyManagerScript instance; //this class is a singleton
    public List<GameObject> activeEnemies; //excludes enemies that expect to die but have not yet done so
    private EnemyDistanceToGoalComparer comparer;

    // Use this for initialization
    private void Awake()
    {
        instance = this;
        activeEnemies = new List<GameObject>();
        comparer = new EnemyDistanceToGoalComparer();
    }

    //called when an enemy is spawned
    public void EnemySpawned(GameObject e)
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
    public void EnemyExpectedDeath(GameObject e)
    {
        activeEnemies.Remove(e);
    }

    //called when the enemy path changes, such as when the enemy mkaes it to the end and restarts
    public void EnemyPathChanged(GameObject e)
    {
        EnemyExpectedDeath(e);
        EnemySpawned(e);
    }

    //returns a list of all enemies that are within the given range of the given position, limiting it to at most max items
    public List<GameObject> enemiesInRange(Vector2 targetPosition, float range, int max = int.MaxValue)
    {
        List<GameObject> targetList = new List<GameObject>();

        //search loop: finds the valid target that is closest to its goal.  Valid targets must be...
        for (int e = 0; (e < activeEnemies.Count) && (targetList.Count < max); e++) //active enemies... (also: stop searching if we hit the max)
        {
            if (activeEnemies[e].GetComponent<EnemyScript>().expectedHealth > 0) //that are not expecting to die... (enemies that expect to die are supposed to be inactive anyway, but sometimes the list takes a frame or two to catch up)
            {
                if (Vector2.Distance(targetPosition, activeEnemies[e].transform.position) <= range) //and are in range
                {
                    targetList.Add(EnemyManagerScript.instance.activeEnemies[e]); //valid target found
                }
            }
        }

        return targetList;
    }

    //sorts the enemy list by how close they are to their goal
    private void sortEnemyList()
    {
        activeEnemies.Sort(comparer);
    }

    // Update is called once per frame
    private void Update()
    {
    }
}