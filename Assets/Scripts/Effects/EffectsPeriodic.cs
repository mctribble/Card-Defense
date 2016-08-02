using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using System;

//all effects in this file trigger when the object they are attached to is updated.  They can be applied to multiple types of entities

//reduces incoming damage by a fixed amount (but attacks always do at least 1 damage)
public class EffectRegeneration : IEffectPeriodic
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.periodic; } }        //effect type
    [Show, Display(2)] public float strength { get; set; }                             //how much health is healed per second
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "Regeneration: " + strength; } } //returns name and strength

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
        LevelManagerScript.instance.WaveTotalRemainingHealth += healThisFrame;
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