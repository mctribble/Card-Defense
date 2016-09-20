﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

//this file contains effects that apply to other effects.
//they are implemented as wrappers around another effect, so they don't have an interface or type of their own: they use that of whichever the target effect is
//this means they must implement ALL effect interfaces, since we dont know what type the underlying effect has

//provides basic handling of the wrapper shenanigans that should work for most effects
public abstract class BaseMetaEffect : IEffectMeta
{
    //effect properties we fetch from the child instead of handling ourselves so we can mimic their usage
    public EffectType effectType { get { return innerEffect.effectType; } }
    public TargetingType targetingType { get { return innerEffect.targetingType; } }

    //properties we can leave as default
    public string cardName     { get; set; } 
    public virtual float   strength    { get; set; }
    public virtual string  argument    { get; set; }
    public virtual IEffect innerEffect { get; set; } 

    //properties the inherited class has to deal with
    public abstract string XMLName { get; }
    public abstract string Name { get; }

    public abstract bool shouldApplyInnerEffect(); //determines whether the inner effect should be applied

    //each of these trigger functions check shouldApplyInnerEffect() and pass the call through if it returns true
    public WaveData alteredWaveData(WaveData currentWaveData) { if (shouldApplyInnerEffect()) { return ((IEffectWave)innerEffect).alteredWaveData(currentWaveData); } else { return currentWaveData;} }
    public List<GameObject> findTargets(Vector2 towerPosition, float towerRange) { if (shouldApplyInnerEffect()) { return ((IEffectTowerTargeting)innerEffect).findTargets(towerPosition, towerRange); } else { return EffectTargetDefault.instance.findTargets(towerPosition, towerRange); } }
    public void UpdateEnemy(EnemyScript e, float deltaTime) { if (shouldApplyInnerEffect()) { ((IEffectPeriodic)innerEffect).UpdateEnemy(e, deltaTime); } }
    public void trigger(ref Card card, GameObject card_gameObject) { if (shouldApplyInnerEffect()) { ((IEffectSelf)innerEffect).trigger(ref card, card_gameObject); } }
    public void trigger(ref DamageEventData d, int pointsOfOvercharge) { if (shouldApplyInnerEffect()) { ((IEffectOvercharge)innerEffect).trigger(ref d, pointsOfOvercharge); } }
    public void trigger() { if (shouldApplyInnerEffect()) { ((IEffectInstant)innerEffect).trigger(); } }
    public void trigger(EnemyScript enemy) { if (shouldApplyInnerEffect()) { ((IEffectEnemyReachedGoal)innerEffect).trigger(enemy); } }
    public void expectedDamage(ref DamageEventData d) { if (shouldApplyInnerEffect()) { ((IEffectEnemyDamaged)innerEffect).expectedDamage(ref d); } }
    public void actualDamage(ref DamageEventData d) { if (shouldApplyInnerEffect()) { ((IEffectEnemyDamaged)innerEffect).actualDamage(ref d); } }

    //returns whether or not the inner effect requires us to cache values
    protected bool shouldCacheValue()
    {
        if (innerEffect.effectType == EffectType.enemyDamaged) return true; 

        //it doesnt make sense for a property to be targeted by another effect, since it doesnt actually do anything on its own
        if (innerEffect.effectType == EffectType.property)
        {
            MessageHandlerScript.Warning("<" + cardName + ">meta effects should not target property effects!");
            return true;
        }

        return false;
    }
}

//child effect has an X% chance of triggering
public class EffectPercentageChance : BaseMetaEffect
{
    public override string XMLName { get { return "percentageChance"; } }
    public override string Name
    {
        get
        {
            if (innerEffect == null)
                return strength + "% chance: do nothing";
            else
                return strength + "% chance: " + innerEffect.Name;
        }
    }

    private bool? cachedApplyInner;
    public override bool shouldApplyInnerEffect()
    {
        //never apply inner effect if it is null
        if (innerEffect == null)
        {
            MessageHandlerScript.Warning("<" + cardName + "> " + XMLName + " has no target and did nothing.");
            return false;
        }

        if (shouldCacheValue())
        {
            if (cachedApplyInner == null)
            {
                cachedApplyInner = (strength < UnityEngine.Random.Range(0.0f, 100.0f));
            }
            return cachedApplyInner.Value;
        }
        else
            return strength < UnityEngine.Random.Range(0.0f, 100.0f);
    }
}

//child effect triggers if the die roll is between X and Y (inclusive)
public class EffectIfRollRange : BaseMetaEffect
{
    private int rangeMin = -1;
    private int rangeMax = -1;

    //override property accessors to use the integer min/max
    public override float strength
    {
        get { return rangeMin; }
        set { rangeMin = Mathf.RoundToInt(value); }
    }
    public override string argument
    {
        get { return rangeMax.ToString(); }
        set
        {
            try
            {
                rangeMax = Convert.ToInt32(value);
            }
            catch (Exception)
            {
                MessageHandlerScript.Warning("<" + cardName + "> " + XMLName + " could not convert argument to an int.  defaulted to 2");
                rangeMax = 2;
            }
        }
    }

    public override string XMLName { get { return "ifRollRange"; } }
    public override string Name
    {
        get
        {
            if (innerEffect == null)
                return rangeMin + " - " + rangeMax + ": " + "do nothing";
            else
                return rangeMin + " - " + rangeMax + ": " + innerEffect.Name;
        }
    }
    public override IEffect innerEffect
    {
        get
        {
            return base.innerEffect;
        }

        set
        {
            base.innerEffect = value;

            //because the die roll is an instant effect, a die roll can only happen when a card is played.
            //Therefore, applying this to an effect marked noCast, such as any kind of targeting effect, will try to access a roll without having made one.
            //such behavior would cause all kinds of strange problems, so we refuse to support such usage
            if (innerEffect.targetingType == TargetingType.noCast)
            {
                MessageHandlerScript.Warning("<" + cardName + "> " + XMLName + " is not compatible with this target effect. ");
                base.innerEffect = null;
            }
        }
    }

    public override bool shouldApplyInnerEffect()
    {
        if (rangeMin <= rangeMax)
        {
            if (EffectDieRoll.roll < rangeMin) return false;
            if (EffectDieRoll.roll > rangeMax) return false;

            return true;
        }
        else
        {
            MessageHandlerScript.Warning("<" + cardName + "> " + XMLName + ": range max is lower than range min!  Try switching strength and argument");
            return false;
        }
    }
}