using System;
using System.Collections.Generic;
using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// enemyDamaged effects trigger when an enemy is damaged.  
/// The effect itself could be attached either to the attacking tower or the defending enemy.  
/// This base effect handles behavior common to them all
/// </summary>
public abstract class BaseEffectEnemyDamaged : BaseEffect, IEffectEnemyDamaged
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.noCast; } }    //this effect should never be on a card, and thus should never be cast
    [Hide] public override EffectType    effectType    { get { return EffectType.enemyDamaged; } } //effect type

    public abstract void expectedDamage(ref DamageEventData d);
    public abstract void actualDamage(ref DamageEventData d);
}

//reduces incoming damage by a fixed amount (but attacks always do at least 1 damage)
public class EffectArmor : BaseEffectEnemyDamaged
{
    [Hide] public override string Name { get { return "Armor: " + strength; } } //returns name and strength
    [Show] public override string XMLName { get { return "armor"; } } //name used to refer to this effect in XML

    //alter damage calculations when we expect to deal damage, not when it actually happens, so that targeting etc. have an accurate number to work with
    public override void expectedDamage(ref DamageEventData d)
    {
        //skip if the attack ignores armor
        if (d.effects != null)
            if (d.effects.propertyEffects.armorPierce)
                return;

        if (d.rawDamage <= 1) return; //dont bother applying armor if the incoming attack is already at or below the min
        d.rawDamage -= strength; //reduce damage by armor value
        d.rawDamage = Mathf.Max(d.rawDamage, 1.0f); //but must deal at least one
    }

    //since damage is already recalculated, we dont need to do anything here
    public override void actualDamage(ref DamageEventData d) { }

    //effect can be removed if it drops to 0
    public override bool shouldBeRemoved()
    {
        return strength == 0;
    }
}

//reduces target effect by a fixed amount (but stops at 0)
public class EffectReduceEnemyEffectOnDamage : BaseEffectEnemyDamaged
{
    [Hide] public override string Name { get { return "Enemy " + argument + " strength: -" + strength; } } //returns name and strength
    [Show] public override string XMLName { get { return "reduceEnemyEffectOnDamage"; } } //name used to refer to this effect in XML

    //we dont need to do anything on expected damage
    public override void expectedDamage(ref DamageEventData d) { } 

    //reduce the effect
    public override void actualDamage(ref DamageEventData d)
    {
        EnemyScript enemy = d.dest;

        if (enemy.effectData != null)
            foreach (IEffect e in enemy.effectData.effects)
                if (e.XMLName == argument)
                    e.strength = Mathf.Max(0, e.strength - strength);
    }
}

//enemy slows down (to 1) as it takes damage.  Decreases proportionally if x = 1.  Higher/lower values cause it to decrease faster/slower, respectively.
public class EffectinvScaleSpeedWithDamage : BaseEffectEnemyDamaged
{
    [Hide] public override string Name { get { return "slows down as it takes damage"; } } //returns name and strength
    [Show] public override string XMLName { get { return "invScaleSpeedWithDamage"; } } //name used to refer to this effect in XML

    //we dont need to do anything on expected damage
    public override void expectedDamage(ref DamageEventData d) { } 

    //recalculate speed
    public override void actualDamage(ref DamageEventData d)
    {
        EnemyScript e = d.dest;                                                             //enemy reference
        float healthRatio = (float)e.curHealth / (float)e.maxHealth;                        //how much health the unit still has (0: dead.  1: full health)
        e.unitSpeed = Mathf.CeilToInt(e.unitSpeedWhenSpawned * (healthRatio / strength));   //scale
        e.unitSpeed = Mathf.Min(e.unitSpeed, 1.0f);                                         //enforce minimum
    }
}

//enemy gets faster as it takes damage.  Increases proportionally if x = 1.  Higher/lower values cause it to increase faster/slower, respectively.
public class EffectScaleSpeedWithDamage : BaseEffectEnemyDamaged
{
    [Hide] public override string Name { get { return "gets up to " + argument + " times faster as it takes damage"; } } //returns name and strength
    [Show] public override string XMLName { get { return "scaleSpeedWithDamage"; } } //name used to refer to this effect in XML

    //we dont need to do anything on expected damage
    public override void expectedDamage(ref DamageEventData d) { } 

    //recalculate speed
    public override void actualDamage(ref DamageEventData d)
    {
        EnemyScript e = d.dest;
        float scaleRatio = 1 - ((float)e.curHealth / (float)e.maxHealth);     //ratio we are scaling by
        float scaleFactor = ((scaleRatio -1 ) * strength) + 1;                //factor to use for scaling
        e.unitSpeed = Mathf.RoundToInt(scaleFactor * e.unitSpeedWhenSpawned); //scale
    }
}

//attack causes a secondary explosion, dealing X damage to all enemies within Y of the impact site. 
public class EffectSplashDamage : BaseEffectEnemyDamaged
{
    //explosion radius
    private float explosionRadius;
    [Show] public override string argument
    {
        get { return explosionRadius.ToString(); }
        set
        {
            try
            {
                explosionRadius = Convert.ToSingle(value);
            }
            catch (Exception)
            {
                Debug.LogWarning("<" + cardName + "> " + XMLName + " could not convert the argument to a valid number.  Defaulting to 1.0");
            }
        }
    }

    [Hide] public override string Name { get { return "deals " + strength + " damage to enemies within " + argument; } } //returns name and strength
    [Show] public override string XMLName { get { return "splashDamage"; } } //name used to refer to this effect in XML

    //we can ignore expected damage
    public override void expectedDamage(ref DamageEventData d) { }

    //but actual damage creates an explosion
    public override void actualDamage(ref DamageEventData originalDamageEvent)
    {
        //construct a damage event for the explosion
        DamageEventData explosionDamageEvent = new DamageEventData();
        explosionDamageEvent.source = originalDamageEvent.source;
        explosionDamageEvent.rawDamage = strength;
        explosionDamageEvent.effects = null; //dont copy effects, or we get an endless explosion chain!
        explosionDamageEvent.dest = null; //burstShot object ignores the destination anyway

        //construct burst shot data
        BurstShotData explosion = new BurstShotData();
        explosion.damageEvent = explosionDamageEvent;
        explosion.burstRange = explosionRadius;
        explosion.targetList = EnemyManagerScript.instance.enemiesInRange(originalDamageEvent.dest.transform.position, explosion.burstRange);

        //call on the level manager to create the actual explosion, since this effect doesnt have a prefab reference
        LevelManagerScript.instance.createExplosion(explosion, originalDamageEvent.dest.transform.position);
    }
}

//attack damages and spreads effects to all enemies within X of each other through a series of consecutive explosions.  No enemy will be hit twice.  
public class EffectChainHit : BaseEffectEnemyDamaged
{
    [Hide] public override string Name { get { return "chains to all within " + strength; } } //returns name and strength
    [Show] public override string XMLName { get { return "chainHit"; } } //name used to refer to this effect in XML

    private List<EnemyScript> enemiesAlreadyHit;

    //constructor
    public EffectChainHit()
    {
        enemiesAlreadyHit = new List<EnemyScript>();
    }

    //use expectedDamage() to prevent the attack from hitting the same enemy twice
    public override void expectedDamage(ref DamageEventData d)
    {
        //if this enemy has already been hit, nullify the attack
        if (enemiesAlreadyHit.Contains(d.dest))
        {
            d.rawDamage = 0;
            d.effects = null;
            return;
        }

        //otherwise, add it to the list
        enemiesAlreadyHit.Add(d.dest);
    }

    //but actual damage creates an explosion
    public override void actualDamage(ref DamageEventData originalDamageEvent)
    {
        //if we make it here, the enemy has not been attacked yet, and we can chain off of it.

        //construct a damage event for the explosion
        DamageEventData explosionDamageEvent = new DamageEventData();
        explosionDamageEvent.source    = originalDamageEvent.source;
        explosionDamageEvent.rawDamage = originalDamageEvent.rawDamage;
        explosionDamageEvent.effects   = originalDamageEvent.effects; 
        explosionDamageEvent.dest = null; //burstShot object ignores the destination anyway

        //construct burst shot data
        BurstShotData explosion = new BurstShotData();
        explosion.damageEvent   = explosionDamageEvent;
        explosion.burstRange    = strength;
        explosion.targetList    = EnemyManagerScript.instance.enemiesInRange(originalDamageEvent.dest.transform.position, explosion.burstRange);

        //call on the level manager to create the actual explosion, since this effect doesnt have a prefab reference
        LevelManagerScript.instance.createExplosion(explosion, originalDamageEvent.dest.transform.position);
    }
}

//damages the enemy by X% of their maximum health
public class EffectDamagePercent : BaseEffectEnemyDamaged
{
    [Hide] public override string Name { get { return "enemy loses " + strength + "% health" ; } } //returns name and strength
    [Show] public override string XMLName { get { return "damagePercent"; } } //name used to refer to this effect in XML

    public override void expectedDamage(ref DamageEventData d)
    {
        d.rawDamage += d.dest.maxHealth * (strength / 100.0f);
    }

    public override void actualDamage(ref DamageEventData d) { }
}

//when a valid attack is made, the tower also creates a burst attack with strength X and radius Y
public class EffectSecondaryBurst : BaseEffectEnemyDamaged
{
    //explosion radius
    private float explosionRadius;
    [Show] public override string argument
    {
        get { return explosionRadius.ToString(); }
        set
        {
            try
            {
                explosionRadius = Convert.ToSingle(value);
            }
            catch (Exception)
            {
                Debug.LogWarning("<" + cardName + "> " + XMLName + " could not convert the argument to a valid number.  Defaulting to 1.0");
            }
        }
    }

    public override string Name { get { return "[on attack] tower deals " + strength + " damage to enemies within " + explosionRadius; } }
    public override string XMLName { get { return "secondaryBurst"; } }

    public override void expectedDamage(ref DamageEventData d)
    {
        //construct a damage event for the explosion
        DamageEventData explosionDamageEvent = new DamageEventData();
        explosionDamageEvent.source = d.source;
        explosionDamageEvent.rawDamage = strength;
        explosionDamageEvent.effects = null; //dont copy effects, or we get an endless explosion chain!
        explosionDamageEvent.dest = null; //burstShot object ignores the destination anyway

        //construct burst shot data
        BurstShotData explosion = new BurstShotData();
        explosion.damageEvent = explosionDamageEvent;
        explosion.burstRange = explosionRadius;
        explosion.targetList = EnemyManagerScript.instance.enemiesInRange(d.source.transform.position, explosion.burstRange);

        //call on the level manager to create the actual explosion, since this effect doesnt have a prefab reference
        LevelManagerScript.instance.createExplosion(explosion, d.source.transform.position);
    }

    public override void actualDamage(ref DamageEventData d) { }
}
