using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// all TowerTargeting effects trigger when an enemy is damaged.  
/// The effect itself could be attached either to the attacking tower or the defending enemy.  
/// This base effect handles behavior common to them all
/// </summary>
public abstract class BaseEffectTowerTargeting : BaseEffect, IEffectTowerTargeting
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public override EffectType    effectType    { get { return EffectType.towerTargeting; } }  //effect type

    public abstract List<GameObject> findTargets(Vector2 towerPosition, float towerRange);
}

/// <summary>
/// default targeting effect to be used when no other is found
/// </summary>
public class EffectTargetDefault : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: default"; } } //returns name and strength
    [Show] public override string XMLName { get { return "<NO_XML_NAME>"; } } //name used to refer to this effect in XML.  This should never happen for this effect since it is a placeholder

    public override List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        return EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange, 1);
    }

    //this effect is a singleton since it is only a placeholder for the absence of other targeting effects
    private static EffectTargetDefault m_instance;
    public static EffectTargetDefault instance
    {
        get
        {
            if (m_instance == null)
                m_instance = new EffectTargetDefault();

            return m_instance;
        }
    }
    private EffectTargetDefault() { }
}

//targets enemy in range with the highest armor, breaking ties by proximity to goal
public class EffectTargetArmor : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: highest armor"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetArmor"; } } //name used to refer to this effect in XML

    public override List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        List<GameObject> enemiesInRange = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //get a list of all valid targets
        if ((enemiesInRange == null) || (enemiesInRange.Count == 0))
            return enemiesInRange;
        float highestArmor = -1;
        GameObject target = null;
        foreach (GameObject curEnemy in enemiesInRange)
        {
            //fetch enemy armor value.  uses a helper function for performance
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
public class EffectTargetAll : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: all in range"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetAll"; } } //name used to refer to this effect in XML

    public override List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        return EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //simply returns all targets in range
    }
}

//like targetAll, but uses a burst shot instead of individual projectiles
public class EffectTargetBurst : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: all in range"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetBurst"; } } //name used to refer to this effect in XML

    public override List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        return EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //simply returns all targets in range
    }
}

//targets the enemy closest to the tower itself
public class EffectTargetClosest : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: closest"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetClosest"; } } //name used to refer to this effect in XML.  This should never happen for this effect since it is a placeholder

    public override List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        //fetch all valid targets
        List <GameObject> validTargets = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange, 1);

        //find the closest one
        GameObject closest = null;
        float closestDist = float.MaxValue;
        foreach (GameObject candidate in validTargets)
        {
            float curDist = Vector2.Distance(candidate.transform.localPosition, towerPosition);
            if ( curDist < closestDist )
            {
                closest = candidate;
                closestDist = curDist;
            }
        }

        //if closest is still null, then we have no valid target.
        if (closest == null)
            return new List<GameObject>();

        //target it
        List<GameObject> result = new List<GameObject>();
        result.Add(closest);
        return result;
    }
}

//targets up to X enemies closest to their goals
public class EffectTargetMultishot : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: up to " + Mathf.Floor(strength) + " enemies"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetMultishot"; } } //name used to refer to this effect in XML

    public override List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
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
public class EffectTargetRandom : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: random"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetRandom"; } } //name used to refer to this effect in XML

    public override List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
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
public class EffectTargetHealth : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: highest health"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetHealth"; } } //name used to refer to this effect in XML

    public override List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
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

//tower targets enemies near the mouse.  Range is how far away FROM THE MOUSE to search for targets.
public class EffectTargetMouse : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: near mouse"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetMouse"; } } //name used to refer to this effect in XML

    public override List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        return EnemyManagerScript.instance.enemiesInRange(mouseWorldPosition, towerRange, 1);
    }
}

//targets highest Speed
public class EffectTargetSpeed : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: highest Speed"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetSpeed"; } } //name used to refer to this effect in XML

    public override List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
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

//targets all enemies in a horizontal line with this tower, so long as at least one is in range
public class EffectTargetOrthogonal : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: Orthogonal"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetOrthogonal"; } } //name used to refer to this effect in XML

    public override List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        List<GameObject> enemiesInRange = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //get a list of all valid targets
        
        //bail if the list is empty
        if ((enemiesInRange == null) || (enemiesInRange.Count == 0))
            return enemiesInRange;

        //construct two boxes around the tower position
        Rect regionH = new Rect(towerPosition.x - towerRange - 0.25f, towerPosition.y - 0.25f, towerRange + towerRange + 0.5f , 0.5f);
        Rect regionV = new Rect(towerPosition.x - 0.25f, towerPosition.y - towerRange - 0.25f, 0.5f , towerRange + towerRange +  0.5f);

        //attack all enemies that are in either region
        List<GameObject> targets = new List<GameObject>();
        foreach (GameObject t in EnemyManagerScript.instance.activeEnemies)
            if ( (regionH.Contains(t.transform.position)) || (regionV.Contains(t.transform.position)) )
                targets.Add(t);

        return targets;
    }
}