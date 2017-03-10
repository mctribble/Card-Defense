using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// compares two enemies by their distance to their goals.  Used for sorting the enemy list
/// </summary>
public class EnemyDistanceToGoalComparer : Comparer<EnemyScript>
{
    public override int Compare(EnemyScript x, EnemyScript y)
    {
        return x.distanceToGoal().CompareTo(y.distanceToGoal());
    }
}

/// <summary>
/// compares two enemies by how quickly they will reach their goals.  Used for sorting the enemy list
/// </summary>
public class EnemyTimeToGoalComparer : Comparer<EnemyScript>
{
    public override int Compare(EnemyScript x, EnemyScript y)
    {
        return x.timeToGoal().CompareTo(y.timeToGoal());
    }
}



/// <summary>
/// responsible for tracking active enemies and performing group operations on them
/// </summary>
public class EnemyManagerScript : BaseBehaviour
{
    public enum SortMethod { distanceToGoal, timeToGoal }
    [Show, OnChanged("targetingMethodChanged")] public SortMethod defaultTargetingMethod;

    public static EnemyManagerScript instance; //this class is a singleton
    public List<EnemyScript> activeEnemies; //excludes enemies that expect to die but have not yet done so
    public List<EnemyScript> survivors; //list of enemies that survived their run and should be added as a new wave on the next round

    private EnemyDistanceToGoalComparer distanceComparer;
    private EnemyTimeToGoalComparer     timeComparer;

    // Use this for initialization
    private void Awake()
    {
        instance = this;
        activeEnemies = new List<EnemyScript>();
        survivors = null;
        distanceComparer = new EnemyDistanceToGoalComparer();
        timeComparer     = new EnemyTimeToGoalComparer();
    }

    //called to reset the manager
    private void Reset()
    {
        activeEnemies = new List<EnemyScript>();
        survivors = null;
    }

    /// <summary>
    /// call when an enemy is spawned to add it to the active enemies list
    /// </summary>
    public void EnemySpawned(EnemyScript e)
    {
        //choose a comparer based on the current targeting method
        IComparer<EnemyScript> comparerToUse = null;
        switch(defaultTargetingMethod)
        {
            case SortMethod.distanceToGoal: comparerToUse = distanceComparer; break;
            case SortMethod.timeToGoal:     comparerToUse = timeComparer;     break;
        }

        //perform a sorted insert.  We expect this enemy to be near the end of the list, so start at the back
        //we insert it after the first enemy that is not further away than this one
        for (int i = activeEnemies.Count - 1; i > 0; i--)
        {
            if (comparerToUse.Compare(activeEnemies[i], e) <= 0)
            {
                activeEnemies.Insert(i + 1, e);
                return;
            }
        }

        //if we made it here, then this enemy actually belongs at the START of the list
        activeEnemies.Insert(0, e);
    }

    /// <summary>
    /// call when an enemy EXPECTS to die, not when it actually dies, to remove it from the active list and stop towers from targeting it
    /// </summary>
    public void EnemyExpectedDeath(EnemyScript e)
    {
        activeEnemies.Remove(e);
    }

    /// <summary>
    /// called when the enemy path changes, such as when the enemy is moved back to the start after surviving a wave
    /// </summary>
    public void EnemyPathChanged(EnemyScript e)
    {
        //always reposition the enemy in the list if its path changes
        updateEnemyPosition(e);
    }

    /// <summary>
    /// called when the enemy speed changes, such as when they become slowed or slowness wears off
    /// </summary>
    public void EnemySpeedChanged(EnemyScript e)
    {
        //dead enemies should be removed instead of repositioned
        if (e == null)
        {
            EnemyExpectedDeath(e);
            return;
        }

        //speed change only warrants a reposition if using the timeToGoal sort
        if (defaultTargetingMethod == SortMethod.timeToGoal)
            updateEnemyPosition(e);
    }

    /// <summary>
    /// repositions the enemy to its proper place in the list.
    /// </summary>
    /// <param name=""></param>
    private void updateEnemyPosition(EnemyScript e)
    {
        EnemyExpectedDeath(e);
        EnemySpawned(e);
    }

    /// <summary>
    /// call when an enemy makes it to the goal.  removes it from the active list and puts it on the survivors list to so it can come back in the next wave
    /// </summary>
    public void EnemySurvived(EnemyScript e)
    { 
        activeEnemies.Remove(e);

        if (survivors == null)
            survivors = new List<EnemyScript>();

        survivors.Add(e);
    }

    /// <summary>
    /// returns a list of all enemies that are within the given range of the given position, limiting it to at most max items
    /// if more than max enemies are found, the ones given the highest targeting priority are returned
    /// </summary>
    /// <param name="targetPosition">center of the circle</param>
    /// <param name="range">radius of the circle</param>
    /// <param name="max">max number of enemies to return</param>
    /// <returns>up to max enemies within the circle</returns>
    public List<EnemyScript> enemiesInRange(Vector2 targetPosition, float range, int max = int.MaxValue)
    {
        List<EnemyScript> targetList = new List<EnemyScript>();

        //search loop: finds the valid target that is closest to its goal.  Valid targets must be...
        for (int e = 0; (e < activeEnemies.Count) && (targetList.Count < max); e++) //active enemies... (also: stop searching if we hit the max)
        {
            if (activeEnemies[e].expectedHealth > 0) //that are not expecting to die... (enemies that expect to die are supposed to be inactive anyway, but sometimes the list takes a frame or two to catch up)
            {
                if (Vector2.Distance(targetPosition, activeEnemies[e].transform.position) <= range) //and are in range
                {
                    targetList.Add(EnemyManagerScript.instance.activeEnemies[e]); //valid target found
                }
            }
        }

        return targetList;
    }

    /// <summary>
    /// re-sorts the entire enemy list
    /// </summary>
    private void sortEnemyList()
    {
        activeEnemies.Sort(distanceComparer);
    }

    //called every frame
    private void Update()
    {
        //if we are sorting by distance instead of time, we have to re-sort the list every now and then to account for enemies passing each other
        if (defaultTargetingMethod == SortMethod.distanceToGoal)
            if (Time.frameCount % 10 == 0)
                sortEnemyList();
    }

    //called when the targeting method changes
    private void targetingMethodChanged(SortMethod newMethod)
    {
        Debug.Log("targeting method changed to " + newMethod);
        sortEnemyList();
    }
}