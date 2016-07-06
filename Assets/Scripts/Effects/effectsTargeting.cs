using System;
using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;

//all effects in this file trigger when an enemy is damaged.  The effect itself could be attached either to the attacking tower or the defending enemy

//targets enemy in range with the highest armor, breaking ties by proximity to goal
public class EffectTargetArmor : IEffectTowerTargeting
{
    //generic interface
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.towerTargeting; } }  //effect type
    [Hide] public float strength { get; set; }                                         //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    [Hide] public string Name { get { return "Target: highest armor"; } } //returns name and strength
    [Show] public string XMLName { get { return "tagetArmor"; } } //name used to refer to this effect in XML

    public List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        List<GameObject> enemiesInRange = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //get a list of all valid targets
        if ((enemiesInRange == null) || (enemiesInRange.Count == 0))
            return enemiesInRange;
        float highestArmor = -1;
        GameObject target = null;
        foreach (GameObject curEnemy in enemiesInRange)
        {
            //fetch enemy armor value
            float curArmor = 0;
            EffectData curEnemyEffects = curEnemy.GetComponent<EnemyScript>().effectData;
            if (curEnemyEffects != null)
                foreach (IEffect curEffect in curEnemyEffects.effects)
                    if (curEffect.XMLName == "armor")
                        curArmor += curEffect.strength;

            //if this is the highest, update vars
            if (curArmor > highestArmor)
            {
                highestArmor = curArmor;
                target = curEnemy;
            }
        }

        //return a list with just the chosen target
        enemiesInRange.Clear();
        enemiesInRange.Add(target);
        return enemiesInRange;
    }
}

//targets all enemies in range
public class EffectTargetAll : IEffectTowerTargeting
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.towerTargeting; } }  //effect type
    [Hide] public float strength { get; set; }                                         //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    [Hide] public string Name { get { return "Target: all in range"; } } //returns name and strength
    [Show] public string XMLName { get { return "tagetAll"; } } //name used to refer to this effect in XML

    public List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        return EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //simply returns all targets in range
    }
}

//targets up to X enemies closest to their goals
public class EffectTargetMultishot : IEffectTowerTargeting
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.towerTargeting; } }  //effect type
    [Show, Display(2)] public float strength { get; set; }                             //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    [Hide] public string Name { get { return "Target: up to " + Mathf.Floor(strength) + " enemies"; } } //returns name and strength
    [Show, Display(1)] public string XMLName { get { return "tagetMultishot"; } } //name used to refer to this effect in XML

    public List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        //fetch valid targets
        List<GameObject> validTargets = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange);

        //bail if the list is empty
        if ((validTargets == null) || (validTargets.Count == 0))
            return validTargets;

        //prune down to strength if there are more than that
        if (validTargets.Count > Mathf.FloorToInt(strength))
            validTargets.RemoveRange(Mathf.FloorToInt(strength), int.MaxValue);

        //return the list
        return validTargets;
    }
}

//targets random enemy in range
public class EffectTargetRandom : IEffectTowerTargeting
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.towerTargeting; } }  //effect type
    [Hide] public float strength { get; set; }                                         //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    [Hide] public string Name { get { return "Target: random"; } } //returns name and strength
    [Show] public string XMLName { get { return "tagetRandom"; } } //name used to refer to this effect in XML

    public List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        List<GameObject> validTargets = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //get all in range

        //bail if the list is empty
        if ((validTargets == null) || (validTargets.Count == 0))
            return validTargets;

        GameObject target = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];                     //pick one at random
        validTargets.Clear();                                                                                  //purge the list
        validTargets.Add(target);                                                                              //add the chosen target back in
        return validTargets;                                                                                   //return it
    }
}

//targets highest health
public class EffectTargetHealth : IEffectTowerTargeting
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.towerTargeting; } }  //effect type
    [Hide] public float strength { get; set; }                                         //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    [Hide] public string Name { get { return "Target: highest health"; } } //returns name and strength
    [Show] public string XMLName { get { return "targetHealth"; } } //name used to refer to this effect in XML

    public List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        List<GameObject> enemiesInRange = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //get a list of all valid targets

        //bail if the list is empty
        if ((enemiesInRange == null) || (enemiesInRange.Count == 0))
            return enemiesInRange;

        float highestHealth = -1;
        GameObject target = null;
        foreach (GameObject curEnemy in enemiesInRange)
        {
            //fetch enemy health
            float curHealth = curEnemy.GetComponent<EnemyScript>().curHealth;

            //if this is the highest, update vars
            if (curHealth > highestHealth)
            {
                highestHealth = curHealth;
                target = curEnemy;
            }
        }

        //return a list with just the chosen target
        enemiesInRange.Clear();
        enemiesInRange.Add(target);
        return enemiesInRange;
    }
}

//targets highest Speed
public class EffectTargetSpeed : IEffectTowerTargeting
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.towerTargeting; } }  //effect type
    [Hide] public float strength { get; set; }                                         //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    [Hide] public string Name { get { return "Target: highest Speed"; } } //returns name and strength
    [Show] public string XMLName { get { return "targetSpeed"; } } //name used to refer to this effect in XML

    public List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        List<GameObject> enemiesInRange = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //get a list of all valid targets

        //bail if the list is empty
        if ((enemiesInRange == null) || (enemiesInRange.Count == 0))
            return enemiesInRange;

        float highestSpeed = -1;
        GameObject target = null;
        foreach (GameObject curEnemy in enemiesInRange)
        {
            //fetch enemy Speed
            float curSpeed = curEnemy.GetComponent<EnemyScript>().unitSpeed;

            //if this is the highest, update vars
            if (curSpeed > highestSpeed)
            {
                highestSpeed = curSpeed;
                target = curEnemy;
            }
        }

        //return a list with just the chosen target
        enemiesInRange.Clear();
        enemiesInRange.Add(target);
        return enemiesInRange;
    }
}