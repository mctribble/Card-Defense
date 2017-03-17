using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vexe.Runtime.Types;

//order in which multiple targeting effects are prioritized.  Higher effects go first
public enum TargetingPriority
{
    DEFAULT    = 0,
    RANDOM     = 1,
    BY_HEALTH  = 2,
    BY_STAT    = 3,
    MOUSE      = 4,
    MULTIPLE   = 5,
    ORTHOGONAL = 6,
    ALL        = 7,
}


/// <summary>
/// all TowerTargeting effects trigger when an enemy is damaged.  
/// The effect itself could be attached either to the attacking tower or the defending enemy.  
/// This base effect handles behavior common to them all
/// </summary>
[ForbidEffectContext(EffectContext.enemyUnit)]
[ForbidEffectContext(EffectContext.enemyCard)]
[ForbidEffectDuplicates]
public abstract class BaseEffectTowerTargeting : BaseEffect, IEffectTowerTargeting
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public override EffectType    effectType    { get { return EffectType.towerTargeting; } }  //effect type

    [Hide] public abstract TargetingPriority priority { get; }

    public abstract IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange);
}

/// <summary>
/// default targeting effect to be used when no other is found
/// </summary>
public class EffectTargetDefault : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: imminent threat"; } } //returns name and strength
    [Show] public override string XMLName { get { return "<NO_XML_NAME>"; } } //name used to refer to this effect in XML.  This should never happen for this effect since it is a placeholder
    [Hide] public override TargetingPriority priority { get { return TargetingPriority.DEFAULT; } } //priority of this targeting effect

    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
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
    [Hide] public override TargetingPriority priority { get { return TargetingPriority.BY_STAT; } } //priority of this targeting effect

    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
    {
        IEnumerable<EnemyScript> enemiesInRange = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //get a list of all valid targets
        if ((enemiesInRange == null) || (enemiesInRange.Any() == false))
            return enemiesInRange;

        float highestArmor = -1;
        EnemyScript target = null;
        foreach (EnemyScript curEnemy in enemiesInRange)
        {
            //fetch enemy armor value.  uses a helper function for performance
            float curArmor = 0;
            EffectData curEnemyEffects = curEnemy.effectData;
            if (curEnemyEffects != null)
            {
                foreach (IEffect curEffect in curEnemyEffects.effects)
                {
                    //drill down through meta effects, if there are any
                    IEffect finalCurEffect = curEffect;
                    while (finalCurEffect.triggersAs(EffectType.meta))
                        finalCurEffect = ((IEffectMeta)finalCurEffect).innerEffect;

                    if (finalCurEffect.XMLName == "armor")
                    {
                        curArmor += finalCurEffect.strength;
                    }
                }
            }

            //if this is the highest, update vars
            if (curArmor > highestArmor)
            {
                highestArmor = curArmor;
                target = curEnemy;
            }
        }

        //return a list with just the chosen target
        List<EnemyScript> result = new List<EnemyScript>();
        result.Add(target);
        return result;
    }
}

//targets all enemies in range
public class EffectTargetAll : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: all in range"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetAll"; } } //name used to refer to this effect in XML
    [Hide] public override TargetingPriority priority { get { return TargetingPriority.ALL; } } //priority of this targeting effect

    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
    {
        return EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //simply returns all targets in range
    }
}

//like targetAll, but uses a burst shot instead of individual projectiles
public class EffectTargetBurst : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: all in range"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetBurst"; } } //name used to refer to this effect in XML
    [Hide] public override TargetingPriority priority { get { return TargetingPriority.ALL; } } //priority of this targeting effect

    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
    {
        return EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //simply returns all targets in range
    }
}

//targets the enemy closest to the tower itself
public class EffectTargetClosest : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: closest"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetClosest"; } } //name used to refer to this effect in XML.
    [Hide] public override TargetingPriority priority { get { return TargetingPriority.BY_STAT; } } //priority of this targeting effect

    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
    {
        //fetch all valid targets
        IEnumerable<EnemyScript> validTargets = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange, 1);

        //find the closest one
        EnemyScript closest = null;
        float closestDist = float.MaxValue;
        foreach (EnemyScript candidate in validTargets)
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
            return new List<EnemyScript>();

        //target it
        List<EnemyScript> result = new List<EnemyScript>();
        result.Add(closest);
        return result;
    }
}

//targets up to X enemies closest to their goals
public class EffectTargetMultishot : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: " + Mathf.Floor(strength) + " most imminent threats"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetMultishot"; } } //name used to refer to this effect in XML
    [Hide] public override TargetingPriority priority { get { return TargetingPriority.MULTIPLE; } } //priority of this targeting effect

    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
    {
        //fetch valid targets
        IEnumerable<EnemyScript> validTargets = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange);

        //bail if the list is empty
        if ((validTargets == null) || (validTargets.Any() == false))
            return validTargets;

        //return the first X enemies in the list
        return validTargets.Take(Mathf.FloorToInt(strength));
    }
}

//targets random enemy in range
public class EffectTargetRandom : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: random"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetRandom"; } } //name used to refer to this effect in XML
    [Hide] public override TargetingPriority priority { get { return TargetingPriority.RANDOM; } } //priority of this targeting effect

    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
    {
        List<EnemyScript> validTargets = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //get all in range

        //bail if the list is empty
        if ((validTargets == null) || (validTargets.Count == 0))
            return validTargets;
        
        EnemyScript target = validTargets[UnityEngine.Random.Range(0, validTargets.Count)];                     //pick one at random
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
    [Hide] public override TargetingPriority priority { get { return TargetingPriority.BY_HEALTH; } } //priority of this targeting effect

    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
    {
        List<EnemyScript> enemiesInRange = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //get a list of all valid targets

        //bail if the list is empty
        if ((enemiesInRange == null) || (enemiesInRange.Count == 0))
            return enemiesInRange;

        float highestHealth = -1;
        EnemyScript target = null;
        foreach (EnemyScript curEnemy in enemiesInRange)
        {
            //fetch enemy health
            float curHealth = curEnemy.expectedHealth;

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

//targets lowest health
public class EffectTargetLowHealth : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: lowest health"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetLowHealth"; } } //name used to refer to this effect in XML
    [Hide] public override TargetingPriority priority { get { return TargetingPriority.BY_HEALTH; } } //priority of this targeting effect

    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
    {
        List<EnemyScript> enemiesInRange = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //get a list of all valid targets

        //bail if the list is empty
        if ((enemiesInRange == null) || (enemiesInRange.Count == 0))
            return enemiesInRange;

        float lowestHealth = float.MaxValue;
        EnemyScript target = null;
        foreach (EnemyScript curEnemy in enemiesInRange)
        {
            //fetch enemy health
            float curHealth = curEnemy.expectedHealth;

            //if this is the lowest, update vars
            if ( (curHealth > 0.0f) && (curHealth < lowestHealth) )
            {
                lowestHealth = curHealth;
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
//caches the list of enemies for efficency when multiple towers make the same test in one frame
public class EffectTargetMouse : BaseEffectTowerTargeting
{
    private static List<EnemyScript> cachedEnemiesNearMouse; //results of a previous call to find enemies near the mouse
    private static float            cachedRange;            //range of the cached result
    private static int              cachedFrame;            //what frame those results are from

    [Hide] public override string Name { get { return "Target: imminent threat near mouse"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetMouse"; } } //name used to refer to this effect in XML
    [Hide] public override TargetingPriority priority { get { return TargetingPriority.MOUSE; } } //priority of this targeting effect

    public EffectTargetMouse() { cachedEnemiesNearMouse = null; cachedFrame = -1; cachedRange = 0.0f; }

    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
    {
        if (cachedFrame == Time.frameCount)
        {
            //we already have a cached result.  See if we can use it.
            if (cachedRange == towerRange)
            {
                return cachedEnemiesNearMouse; //yes, we can
            }
            else if (cachedRange > towerRange)
            {
                //we have a cached result, but it contains more than we need.  Instead of doing a whole new test, return a subset of the cached one
                return cachedEnemiesNearMouse.FindAll(go => Vector3.Distance(go.transform.position, Camera.main.ScreenToWorldPoint(Input.mousePosition)) <= towerRange);
            }
        }

        //either we did not have a cached result, or it was unusable.  do a normal test
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        cachedEnemiesNearMouse = EnemyManagerScript.instance.enemiesInRange(mouseWorldPosition, towerRange, 1);
        cachedFrame = Time.frameCount;
        cachedRange = towerRange;
        return cachedEnemiesNearMouse;
    }
}

//targets highest Speed
public class EffectTargetSpeed : BaseEffectTowerTargeting
{
    [Hide] public override string Name { get { return "Target: highest Speed"; } } //returns name and strength
    [Show] public override string XMLName { get { return "targetSpeed"; } } //name used to refer to this effect in XML
    [Hide] public override TargetingPriority priority { get { return TargetingPriority.BY_STAT; } } //priority of this targeting effect

    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
    {
        List<EnemyScript> enemiesInRange = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //get a list of all valid targets

        //bail if the list is empty
        if ((enemiesInRange == null) || (enemiesInRange.Count == 0))
            return enemiesInRange;

        float highestSpeed = -1;
        EnemyScript target = null;
        foreach (EnemyScript curEnemy in enemiesInRange)
        {
            //fetch enemy Speed
            float curSpeed = curEnemy.unitSpeed;

            //account for slowing
            if (curEnemy.slowedForSeconds > 0.2f)
                curSpeed /= 2;

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
    [Hide] public override TargetingPriority priority { get { return TargetingPriority.ORTHOGONAL; } } //priority of this targeting effect

    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
    {
        List<EnemyScript> enemiesInRange = EnemyManagerScript.instance.enemiesInRange(towerPosition, towerRange); //get a list of all valid targets
        
        //bail if the list is empty
        if ((enemiesInRange == null) || (enemiesInRange.Count == 0))
            return enemiesInRange;

        //construct two boxes around the tower position
        Rect regionH = new Rect(towerPosition.x - towerRange - 0.25f, towerPosition.y - 0.25f, towerRange + towerRange + 0.5f , 0.5f);
        Rect regionV = new Rect(towerPosition.x - 0.25f, towerPosition.y - towerRange - 0.25f, 0.5f , towerRange + towerRange +  0.5f);

        //attack all enemies that are in either region
        List<EnemyScript> targets = new List<EnemyScript>();
        foreach (EnemyScript t in EnemyManagerScript.instance.activeEnemies)
            if ( (regionH.Contains(t.transform.position)) || (regionV.Contains(t.transform.position)) )
                targets.Add(t);

        return targets;
    }
}