﻿using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;
using System.Linq;

/// <summary>
/// compares two enemies by their distance to their goals.  Used for sorting the enemy list
/// </summary>
public class EnemyDistanceToGoalComparer : Comparer<EnemyScript>
{
    public override int Compare(EnemyScript x, EnemyScript y)
    {
        return x.distanceToGoal.CompareTo(y.distanceToGoal);
    }
}

/// <summary>
/// compares two enemies by how quickly they will reach their goals.  Used for sorting the enemy list
/// </summary>
public class EnemyTimeToGoalComparer : Comparer<EnemyScript>
{
    public override int Compare(EnemyScript x, EnemyScript y)
    {
        int temp = x.timeToGoal.CompareTo(y.timeToGoal);
        return temp;
    }
}



/// <summary>
/// responsible for tracking active enemies and performing group operations on them
/// </summary>
public class EnemyManagerScript : BaseBehaviour
{
    public enum SortMethod { distanceToGoal, timeToGoal }
    [Show, OnChanged("targetingMethodChanged")] public SortMethod defaultTargetingMethod;

    [Hide] public static EnemyManagerScript instance; //this class is a singleton
    [VisibleWhen("showLists")]public List<EnemyScript> survivors; //list of enemies that survived their run and should be added as a new wave on the next round

    //VFW doesnt support linkedList, so we hide it in the inspector and create a property to show instead
    [Hide] public  LinkedList<EnemyScript> activeEnemies; //excludes enemies that expect to die but have not yet done so
    [Show, VisibleWhen("showLists")] private List<EnemyScript> _ActiveEnemies { get { return activeEnemies.AsEnumerable().ToList(); } }

    private bool showLists() { return instance != null; } //only show lists when running

    private EnemyDistanceToGoalComparer distanceComparer;
    private EnemyTimeToGoalComparer     timeComparer;

    // Use this for initialization
    private void Awake()
    {
        instance = this;
        activeEnemies = new LinkedList<EnemyScript>();
        survivors = null;
        distanceComparer = new EnemyDistanceToGoalComparer();
        timeComparer     = new EnemyTimeToGoalComparer();
    }

    //called to reset the manager
    private void Reset()
    {
        activeEnemies = new LinkedList<EnemyScript>();
        survivors = null;
    }

    /// <summary>
    /// call when an enemy is spawned to add it to the active enemies list
    /// </summary>
    public void EnemySpawned(EnemyScript e)
    {
        //if the list is empty, just add e
        if (activeEnemies.Count == 0)
        {
            activeEnemies.AddFirst(e);
            return;
        }

        //choose a comparer based on the current targeting method
        IComparer<EnemyScript> comparerToUse = null;
        switch(defaultTargetingMethod)
        {
            case SortMethod.distanceToGoal: comparerToUse = distanceComparer; break;
            case SortMethod.timeToGoal:     comparerToUse = timeComparer;     break;
        }

        //perform a sorted insert.  We expect this enemy to be near the end of the list, so start at the back
        //we insert it after the first enemy that is not further away than this one
        LinkedListNode<EnemyScript> searchNode = activeEnemies.Last;

        //search until we hit the start of the list or find something <= e
        while (searchNode != null && comparerToUse.Compare(searchNode.Value, e) > 0)
            searchNode = searchNode.Previous;

        //searchNode is now either null or the first item <= e
        if (searchNode == null)
            activeEnemies.AddFirst(e); //searchNode is null, so we hit the front of the list without finding something <= e, and e belongs at the front
        else
            activeEnemies.AddAfter(searchNode, e); //we found something <= e, so we can put e after it
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
    /// called when the enemy speed changes, such as when they become slowed or slowness wears off.  
    /// This version is more efficient because it doesnt have to perform a search if we already have the node.
    /// </summary>
    public void EnemySpeedChanged(LinkedListNode<EnemyScript> e)
    {
        //dead enemies should be removed instead of repositioned
        if (e.Value == null)
        {
            EnemyExpectedDeath(e.Value);
            return;
        }

        //speed change only warrants a reposition if using the timeToGoal sort
        if (defaultTargetingMethod == SortMethod.timeToGoal)
            updateEnemyPosition(e);
    }

    /// <summary>
    /// repositions the enemy to its proper place in the list.  If the enemy is not currently in the list, nothing happens
    /// </summary>
    /// <param name=""></param>
    private void updateEnemyPosition(EnemyScript e)
    {
        LinkedListNode<EnemyScript> enemyNode = activeEnemies.Find(e);
        if (enemyNode != null)
            updateEnemyPosition(enemyNode);
    }

    /// <summary>
    /// repositions the enemy to its proper place in the list.
    /// This version is more efficient because it doesnt have to perform a search if we already have the node.
    /// </summary>
    /// <param name=""></param>
    private void updateEnemyPosition(LinkedListNode<EnemyScript> e)
    {
        //choose a comparer based on the current targeting method
        IComparer<EnemyScript> comparerToUse = null;
        switch (defaultTargetingMethod)
        {
            case SortMethod.distanceToGoal: comparerToUse = distanceComparer; break;
            case SortMethod.timeToGoal: comparerToUse = timeComparer; break;
        }

        if (e.Previous != null && comparerToUse.Compare(e.Previous.Value, e.Value) > 0) //if there is a node before e, and it is larger than e
        {
            //e needs to move further up the list
            LinkedListNode<EnemyScript> searchNode = e.Previous;
            activeEnemies.Remove(e);

            //search until we hit the start of the list or find something <= e
            while (searchNode != null && comparerToUse.Compare(searchNode.Value, e.Value) > 0)
                searchNode = searchNode.Previous;

            //searchNode is now either null or the first item <= e
            if (searchNode == null)
                activeEnemies.AddFirst(e); //searchNode is null, so we hit the front of the list without finding something <= e, and e belongs at the front
            else
                activeEnemies.AddAfter(searchNode, e); //we found something <= e, so we can put e after it
        }
        else if (e.Next != null && comparerToUse.Compare(e.Next.Value, e.Value) < 0) //if there is a node after 3, and it is smaller than e
        {
            //e needs to move further down the list
            LinkedListNode<EnemyScript> searchNode = e.Next;
            activeEnemies.Remove(e);

            //search until we hit the start of the list or find something <= e
            while (searchNode.Next != null && comparerToUse.Compare(searchNode.Value, e.Value) < 0)
                searchNode = searchNode.Next;

            //searchNode is now either null or the first item >= e
            if (searchNode == null)
                activeEnemies.AddLast(e); //searchNode is null, so we hit the end of the list without finding something >= e, and e belongs at the rear
            else
                activeEnemies.AddBefore(searchNode, e); //we found something <= e, so we can put e after it
        }
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
        //find result with a Linq query:
        //we are looking for...
        return activeEnemies.Where(e => e.expectedHealth > 0)                                            //active enemies that don't already expect to die
                            .Where(e => Vector2.Distance(targetPosition, e.transform.position) <= range) //and are in range
                                         //and we want to return...
                            .Take(max)   //up to max enemies
                            .ToList();   //as a list
    }

    /// <summary>
    /// re-sorts the entire enemy list
    /// </summary>
    private void sortEnemyList()
    {
        //choose a comparer based on the current targeting method
        IComparer<EnemyScript> comparerToUse = null;
        switch (defaultTargetingMethod)
        {
            case SortMethod.distanceToGoal: comparerToUse = distanceComparer; break;
            case SortMethod.timeToGoal: comparerToUse = timeComparer; break;
        }

        //we expect the list to be mostly sorted, so we will use bubble sort
        bool listChanged = true;
        while (listChanged)
        {
            listChanged = false;

            LinkedListNode<EnemyScript> searchNode = activeEnemies.Last;
            while (searchNode != null && searchNode.Previous != null)
            {
                if (comparerToUse.Compare(searchNode.Value, searchNode.Previous.Value) <= 0)
                {
                    //searchNode is where it belongs.  move on.
                    searchNode = searchNode.Previous;
                    continue;
                }
                else
                {
                    //searchNode is not where it belongs. Remove it.
                    LinkedListNode<EnemyScript> hold = searchNode;
                    searchNode = searchNode.Previous;
                    activeEnemies.Remove(hold);

                    //continue up the list until we hit the beginning or find a smaller node
                    while (searchNode.Previous != null && comparerToUse.Compare(searchNode.Value, searchNode.Previous.Value) > 0)
                        searchNode = searchNode.Previous;

                    //put it back in the list
                    activeEnemies.AddBefore(searchNode, hold);
                    searchNode = hold.Previous;
                    listChanged = true;
                }
            }
        }
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