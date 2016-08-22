using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using System;

//all effects in this file trigger when the object they are attached to is updated.  They can be applied to multiple types of entities

//enemy recovers X health per second
public class EffectRegeneration : IEffectPeriodic
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.periodic; } }        //effect type
    [Show, Display(2)] public float strength { get; set; }                             //how much health is healed per second
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "Regeneration: " + strength + "/s"; } } //returns name and strength

    [Show, Display(1)] public string XMLName { get { return "regeneration"; } } //name used to refer to this effect in XML

    [Hide] private float carryOver; //enemy health is an int, and rounding every frame will cause issues, so fractions are carried to the next frame

    public EffectRegeneration () { carryOver = 0; }  //default constructor inits carryover to 0

    public void UpdateEnemy(EnemyScript e, float deltaTime)
    {
        //skip if the enemy already expects to die, so we dont screw with targeting in all manner of broken ways
        if (e.expectedHealth <= 0)
            return;

        float healAmount = strength * deltaTime; //scale by time
        healAmount += carryOver; //   

        //prevent over heal
        int healthMissing = e.maxHealth - e.curHealth;
        if (healAmount > healthMissing)
            healAmount = healthMissing;

        //carry fractions to avoid rounding errors
        int healThisFrame = Mathf.FloorToInt(healAmount);
        carryOver = healAmount - healThisFrame;

        //heal
        e.curHealth += healThisFrame;
        e.expectedHealth += healThisFrame;
        LevelManagerScript.instance.totalRemainingHealth += healThisFrame;
    }
}

//enemy loses X health per second for Y seconds 
public class EffectPoison : IEffectPeriodic
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.periodic; } }        //effect type
    [Show, Display(2)] public float strength { get; set; }                             //how much health is healed per second

    //effect lifespan (this is a string to match the interface, but actually updates the float maxPoisonTime)
    [Hide] public string argument
    {
        get
        {
            return Convert.ToString(maxPoisonTime);
        }
        set
        {
            //convert argument to a float, if possible
            try
            {
                maxPoisonTime = Convert.ToSingle(value);
            }
            catch (FormatException ex)
            {
                MessageHandlerScript.Warning("poison effect could not convert the argument to a number (" + ex.Message + ")");
                maxPoisonTime = 999999.9f;
            }

            curPoisonTime = 0;
        }
    }              

    [Hide] public string Name { get { return "Poison: " + strength + "/s for " + maxPoisonTime + " seconds"; } } //returns name and strength

    [Show, Display(1)] public string XMLName { get { return "poison"; } } //name used to refer to this effect in XML

    [Show, Display(3)] public float curPoisonTime; //how much time has passed
    [Show, Display(4)] public float maxPoisonTime; //stop dealing damage after this window has passed

    public EffectPoison() { curPoisonTime = 0; maxPoisonTime = 0; }  //default constructor inits internal variables to 0

    public void UpdateEnemy(EnemyScript e, float deltaTime)
    {
        ////do nothing if the effect time is already over
        //if (curPoisonTime > maxPoisonTime)
        //    return;

        //update timer
        curPoisonTime += Time.deltaTime;

        //construct event
        DamageEventData damageEvent = new DamageEventData();
        damageEvent.source = null;
        damageEvent.dest = e.gameObject;
        damageEvent.rawDamage = strength * Time.deltaTime;
        damageEvent.effects = null;

        //deal damage
        e.onExpectedDamage(ref damageEvent);
        e.onDamage(damageEvent);
    }
}

//enemy slows down by X/second (min 1)
public class EffectInvScaleSpeedWithTime : IEffectPeriodic
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.periodic; } }        //effect type
    [Show, Display(2)] public float strength { get; set; }                             //how much speed is gained per second
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "Speed decreases by " + strength + "/s"; } } //returns name and strength

    [Show, Display(1)] public string XMLName { get { return "invScaleSpeedWithTime"; } } //name used to refer to this effect in XML

    public void UpdateEnemy(EnemyScript e, float deltaTime)
    {
        e.unitSpeed -= (strength * deltaTime);
        e.unitSpeed = Mathf.Max(e.unitSpeed, 1.0f);
    }
}

//enemy speeds up by X/second
public class EffectScaleSpeedWithTime : IEffectPeriodic
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.periodic; } }        //effect type
    [Show, Display(2)] public float strength { get; set; }                             //how much speed is gained per second
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "Speed increases by " + strength + "/s"; } } //returns name and strength

    [Show, Display(1)] public string XMLName { get { return "scaleSpeedWithTime"; } } //name used to refer to this effect in XML

    public void UpdateEnemy(EnemyScript e, float deltaTime)
    {
        e.unitSpeed += (strength * deltaTime);
    }
}

//enemy effect Y gets stronger by X/second
public class EffectScaleEffectWithTime : IEffectPeriodic
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.periodic; } }        //effect type
    [Show, Display(2)] public float strength { get; set; }                             //how much speed is gained per second
    [Show, Display(3)] public string argument { get; set; }                            //effect to scale
    
    [Hide] public string Name { get { return argument + " increases by " + strength + "/s"; } } //returns name and strength
    
    [Show, Display(1)] public string XMLName { get { return "scaleEffectWithTime"; } } //name used to refer to this effect in XML

    private IEffect effectToScale; //cached effect to avoid searching the list every frame

    public void UpdateEnemy(EnemyScript e, float deltaTime)
    {
        if (effectToScale == null)
        {
            foreach (IEffect effect in e.effectData.effects)
            {
                if (effect.XMLName == argument)
                {
                    effectToScale = effect;
                    break;
                }
            }
        }

        effectToScale.strength += (strength * deltaTime);
    }
}

//enemy effect Y gets weaker by X/second (min 0)
public class EffectInvScaleEffectWithTime : IEffectPeriodic
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.periodic; } }        //effect type
    [Show, Display(2)] public float strength { get; set; }                             //how much speed is gained per second
    [Show, Display(3)] public string argument { get; set; }                            //effect to scale
    
    [Hide] public string Name { get { return argument + " decreases by " + strength + "/s"; } } //returns name and strength
    
    [Show, Display(1)] public string XMLName { get { return "InvScaleEffectWithTime"; } } //name used to refer to this effect in XML

    private IEffect effectToScale; //cached effect to avoid searching the list every frame

    public void UpdateEnemy(EnemyScript e, float deltaTime)
    {
        if (effectToScale == null)
        {
            foreach (IEffect effect in e.effectData.effects)
            {
                if (effect.XMLName == argument)
                {
                    effectToScale = effect;
                    break;
                }
            }
        }

        effectToScale.strength -= (strength * deltaTime);
        effectToScale.strength = Mathf.Max(effectToScale.strength, 1.0f);
    }
}