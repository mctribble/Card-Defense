using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using System.Collections.Generic;

/// <summary>
/// handles mod dependencies
/// </summary>
public class DependencyManagerScript : BaseBehaviour
{
    public static DependencyManagerScript instance; //singleton instance

    //lists of dependencies that have been met 
    //technically speaking, files are added to this list BEFORE being loaded, but that shouldn't matter in practice since
    //we return the list of files to load in the order they should be loaded
    public List<string> enemyFilesLoaded;
    public List<string> cardFilesLoaded;

    //status indicators for which dependencies have been figured out
    public bool enemyDepenciesHandled;
    public bool cardDependenciesHandled;

	//Use this for initialization
	void Awake()
    {
        instance = this;
        enemyDepenciesHandled = false;
        cardDependenciesHandled = false;
	}

    /// <summary>
    /// sorts enemy collections in the order they should be loaded, removing any with unmet dependencies
    /// to use, provide this function with a list of all the enemy files you want to load, and store the result
    /// iterate through the result and load the files in order, and all dependencies will be met
    /// if this is not possible, offending files are removed from the list entirely, producing warnings
    /// </summary>
    public List<EnemyTypeCollection> handleEnemyFileDependencies(List<EnemyTypeCollection> files)
    {
        //create a new list that is large enough to hold everything we have been given
        List<EnemyTypeCollection> handled = new List<EnemyTypeCollection>(files.Count);

        //repeatedly search the list, adding everything with met dependencies to the list each time, until an iteration passes without changes
        bool changed = true;
        while (changed)
        {
            changed = false;

            foreach (EnemyTypeCollection etc in files)
            {
                //skip anything already in the handled list
                if (handled.Contains(etc))
                    continue;

                //if the dependency list is null, set it to an empty string instead to avoid nullReference
                if (etc.dependencies == null)
                    etc.dependencies = "";

                //split up the dependency list
                string[] dependencies = etc.dependencies.Split(',');

                //search the dependency list for anything unmet
                bool unmetDependencies = false;
                foreach (string d in dependencies)
                {
                    string dTrimmed = d.Trim(); //ignore leading/trailing whitespace

                    if (dTrimmed != "") //empty strings always count as met
                    {
                        if (enemyFilesLoaded.Contains(dTrimmed) == false)
                        {
                            unmetDependencies = true;
                            break;
                        }
                    }
                }

                //if all dependencies have been met, list it as loaded (even though, technically, that happens after this function returns) and put it on the handled list
                if (unmetDependencies == false)
                {
                    enemyFilesLoaded.Add(etc.fileName);
                    handled.Add(etc);
                    changed = true;
                }
            }
        }

        //now that we have done all we can, report anything we havent managed to handle
        foreach (EnemyTypeCollection etc in files)
            if(handled.Contains(etc) == false)
                MessageHandlerScript.Warning(etc.fileName + " has unmet dependencies and was not loaded!");

        //return the rest.
        enemyDepenciesHandled = true;
        return handled;
    }

    /// <summary>
    /// use handleEnemyFileDependencies first, since card files may depend on those
    /// sorts card type collections in the order they should be loaded, removing any with unmet dependencies
    /// to use, provide this function with a list of all the enemy files you want to load, and store the result
    /// iterate through the result and load the files in order, and all dependencies will be met
    /// if this is not possible, offending files are removed from the list entirely, producing warnings
    /// </summary>
    public List<CardTypeCollection> handleCardFileDependencies(List<CardTypeCollection> files)
    {
        //error if enemies are not done first since cards might rely on enemies but not the other way around
        while (enemyDepenciesHandled == false)
            throw new System.InvalidOperationException("Cant handle card dependencies until after enemies are dealt with");

        //create a new list that is large enough to hold everything we have been given
        List<CardTypeCollection> handled = new List<CardTypeCollection>(files.Count);

        //repeatedly search the list, adding everything with met dependencies to the list each time, until an iteration passes without changes
        bool changed = true;
        while (changed)
        {
            changed = false;

            foreach (CardTypeCollection ctc in files)
            {
                //skip anything already in the handled list
                if (handled.Contains(ctc))
                    continue;

                //if either dependency list is null, set it to an empty string instead to avoid nullReference
                if (ctc.cardDependencies == null)
                    ctc.cardDependencies = "";
                if (ctc.enemyDependencies == null)
                    ctc.enemyDependencies = "";

                //split up the dependency list
                string[] cardDependencies = ctc.cardDependencies.Split(',');
                string[] enemyDependencies = ctc.enemyDependencies.Split(',');

                //search the dependency lists for anything unmet
                bool unmetDependencies = false;
                foreach (string d in cardDependencies)
                {
                    string dTrimmed = d.Trim(); //ignore leading/trailing whitespace

                    if (dTrimmed != "") //empty strings always count as met
                    {
                        if (cardFilesLoaded.Contains(dTrimmed) == false)
                        {
                            unmetDependencies = true;
                            break;
                        }
                    }
                }
                foreach (string d in enemyDependencies)
                {
                    string dTrimmed = d.Trim(); //ignore leading/trailing whitespace

                    if (dTrimmed != "") //empty strings always count as met
                    {
                        if (enemyFilesLoaded.Contains(dTrimmed) == false)
                        {
                            unmetDependencies = true;
                            break;
                        }
                    }
                }

                //if all dependencies have been met, list it as loaded (even though, technically, that happens after this function returns) and put it on the handled list
                if (unmetDependencies == false)
                {
                    cardFilesLoaded.Add(ctc.fileName);
                    handled.Add(ctc);
                    changed = true;
                }
            }
        }

        //now that we have done all we can, report anything we havent managed to handle
        foreach (CardTypeCollection ctc in files)
            if (handled.Contains(ctc) == false)
                MessageHandlerScript.Warning(ctc.fileName + " has unmet dependencies and was not loaded!");

        //return the rest.
        cardDependenciesHandled = true;
        return handled;
    }

    /// <summary>
    /// tests if dependencies for this level have been met
    /// </summary>
    public bool testLevelDependencies(LevelData level)
    {
        string[] dependencies;

        //enemies
        if (level.enemyDependencies != null)
        {
            dependencies = level.enemyDependencies.Split(',');
            foreach (string d in dependencies)
                if (enemyFilesLoaded.Contains(d.Trim()) == false)
                    return false;
        }

        //cards
        if (level.cardDependencies != null)
        {
            dependencies = level.cardDependencies.Split(',');
            foreach (string d in dependencies)
                if (cardFilesLoaded.Contains(d.Trim()) == false)
                    return false;
        }

        return true;
    }
}
