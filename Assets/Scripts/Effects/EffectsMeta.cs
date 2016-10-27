using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Vexe.Runtime.Types;

//this file contains effects that apply to other effects.
//they are implemented as wrappers around another effect, so they don't have an interface or type of their own: they use that of whichever the target effect is
//this means they must implement ALL effect interfaces, since we dont know what type the underlying effect has
//This base effect handles behavior common to them all

/// <summary>
/// all meta effects should inherit from this.  
/// </summary>
public abstract class BaseEffectMeta : BaseEffect, IEffectMeta
{
    //constructor
    public BaseEffectMeta() { innerEffect = EffectDoNothing.instance; }

    //this is a meta effect, but we mimic our target so that we can trigger and display in the same way they do
    public override EffectType    effectType    { get { return EffectType.meta; } }
    public override TargetingType targetingType { get { return innerEffect.targetingType; } }
    public override bool triggersAs(EffectType triggerType) { return (base.triggersAs(triggerType) || innerEffect.triggersAs(triggerType)); } //trigger as a meta effect, but also as whatever the target effect is
    public override string effectColorHex { get { if (innerEffect == null) return "000000FF"; else return innerEffect.effectColorHex; } } //if no target, color black.  otherwise, use same color as the target

    [Show] public virtual IEffect innerEffect { get; set; } //effect targeted by this effect

    public abstract bool shouldApplyInnerEffect(); //determines whether the inner effect should be applied

    //each of these trigger functions check shouldApplyInnerEffect() and pass the call through if it returns true
    public virtual WaveData alteredWaveData(WaveData currentWaveData) { if (shouldApplyInnerEffect()) { return ((IEffectWave)innerEffect).alteredWaveData(currentWaveData); } else { return currentWaveData;} }
    public virtual List<GameObject> findTargets(Vector2 towerPosition, float towerRange) { if (shouldApplyInnerEffect()) { return ((IEffectTowerTargeting)innerEffect).findTargets(towerPosition, towerRange); } else { return null; } }
    public virtual void UpdateEnemy(EnemyScript e, float deltaTime) { if (shouldApplyInnerEffect()) { ((IEffectPeriodic)innerEffect).UpdateEnemy(e, deltaTime); } }
    public virtual void trigger(ref Card card, GameObject card_gameObject) { if (shouldApplyInnerEffect()) { ((IEffectSelf)innerEffect).trigger(ref card, card_gameObject); } }
    public virtual void trigger(ref DamageEventData d, int pointsOfOvercharge) { if (shouldApplyInnerEffect()) { ((IEffectOvercharge)innerEffect).trigger(ref d, pointsOfOvercharge); } }
    public virtual void trigger() { if (shouldApplyInnerEffect()) { ((IEffectInstant)innerEffect).trigger(); } }
    public virtual void trigger(EnemyScript enemy) { if (shouldApplyInnerEffect()) { ((IEffectEnemyReachedGoal)innerEffect).trigger(enemy); } }
    public virtual void onEnemyDeath(EnemyScript enemy) { if (shouldApplyInnerEffect()) { ((IEffectDeath)innerEffect).onEnemyDeath(enemy); } }
    public virtual void onTowerDeath(TowerScript tower) { if (shouldApplyInnerEffect()) { ((IEffectDeath)innerEffect).onTowerDeath(tower); } }

    //enemyDamage effects need special care since they are handled twice but should only be tested once
    ushort enemyDamageEFfectTriggers;
    public virtual void expectedDamage(ref DamageEventData d)
    {
        if ( shouldApplyInnerEffect() )
        {
            enemyDamageEFfectTriggers++;
            ((IEffectEnemyDamaged)innerEffect).expectedDamage(ref d);
        }
    } 
    public virtual void actualDamage(ref DamageEventData d)
    {
        if (enemyDamageEFfectTriggers > 0)
        {
            enemyDamageEFfectTriggers--;
            ((IEffectEnemyDamaged)innerEffect).actualDamage(ref d);
        }
    } 

    //returns whether or not the inner effect requires us to cache values
    protected bool shouldCacheValue()
    {
        if (innerEffect.triggersAs(EffectType.enemyDamaged)) return true; 

        //it doesnt make sense for a property to be targeted by another effect, since it doesnt actually do anything on its own
        if (innerEffect.triggersAs(EffectType.property))
        {
            MessageHandlerScript.Warning("<" + cardName + ">meta effects should not target property effects!");
            return true;
        }

        return false;
    }

    //if the inner effect can be cleaned out, or there is no inner effect, then this can be removed
    public override bool shouldBeRemoved() { return ( (innerEffect == null) || (innerEffect.shouldBeRemoved()) ); }

    //returns a clone of the inner effect.  should be overridden by meta effects that alter their inner effects.
    public virtual IEffect cloneInnerEffect()
    {
        return EffectData.cloneEffect(innerEffect);
    }
}

//placeholder do-nothing effect to use as a default inner effect
public class EffectDoNothing : BaseEffectInstant
{
    private static EffectDoNothing m_instance;
    public static EffectDoNothing instance
    {
        get
        {
            if (m_instance == null)
                m_instance = new EffectDoNothing();

            return m_instance;
        }
    }

    public override string Name { get { return "Do Nothing"; } }
    public override string XMLName { get { return "doNothing"; } }

    public override void trigger() { }
}

//child effect has an X% chance of triggering
public class EffectPercentageChance : BaseEffectMeta
{
    public override string XMLName { get { return "percentageChance"; } }
    public override string Name
    {
        get
        {
            if (innerEffect == null)
                return strength + "[% chance]do nothing";
            else
                return strength + "[% chance]" + innerEffect.Name;
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
public class EffectIfRollRange : BaseEffectMeta
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
                return "[" + rangeMin + " - " + rangeMax + "]" + "do nothing";
            else
                return "[" + rangeMin + " - " + rangeMax + "]" + innerEffect.Name;
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

//child effect can only occur up to X times
public class EffectEffectCharges : BaseEffectMeta
{
    public override float strength
    {
        get { return base.strength; }
        set { base.strength = Mathf.RoundToInt(value); }
    }

    public override string Name { get { return "[x" + strength + "]" + innerEffect.Name; } }
    public override string XMLName { get { return "effectCharges"; } }

    public override bool shouldApplyInnerEffect()
    {
        if (strength > 0)
        {
            strength--;
            return true;
        }
        else
        {
            return false;
        }
    }

    //the effect can be cleaned up if it is out of charges, or if the base call says so
    public override bool shouldBeRemoved() { return ( strength <= 0 || base.shouldBeRemoved() ); }
}

//child effect triggers under normal conditions, but only if it has been at least X seconds since the last trigger
public class EffectEffectCooldown : BaseEffectMeta
{
    public override string Name    { get { return "[" + strength + "s cooldown]" + innerEffect.Name; } }
    public override string XMLName { get { return "effectCooldown"; } }

    private bool onCooldown = false;

    private IEnumerator cooldown()
    {
        onCooldown = true;
        yield return new WaitForSeconds(strength);
        onCooldown = false;
    }

    public override bool shouldApplyInnerEffect()
    {
        if (onCooldown)
        {
            return false;
        }
        else
        {
            //targeting effects do not trigger the cooldown here, since we dont know if they found anything. Instead, see the findTargets() override
            if (innerEffect.triggersAs(EffectType.towerTargeting) == false)
                LevelManagerScript.instance.StartCoroutine(cooldown());
            return true;
        }
    }

    //targeting effects trigger the cooldown IF AND ONLY IF they found something
    public override List<GameObject> findTargets(Vector2 towerPosition, float towerRange)
    {
        List<GameObject> result = base.findTargets(towerPosition, towerRange);

        if (result != null)
            if (result.Count > 0)
                LevelManagerScript.instance.StartCoroutine(cooldown());

        return result;
    }
}

//target instant effect triggers once every round (using IEffectInstant)
public class EffectEveryRound : BaseEffectMeta
{
    public override string Name    { get { return "[every round]" + innerEffect.Name; } }
    public override string XMLName { get { return "everyRound"; } }
    public override bool   shouldApplyInnerEffect() { return true; }

    //regardless of how the inner effect normally triggers, we want it to fire once every round.  We need to keep the triggertype == EffectType.meta so we dont break EffectData.cloneEffect()
    public override bool triggersAs(EffectType triggerType)
    {
        return (triggerType == EffectType.everyRound) || (triggerType == EffectType.meta);
    }

    //because we use IEffectInstant, we can only target instant or everyRound effects
    public override IEffect innerEffect
    {
        get { return base.innerEffect; }
        set
        {
            if (value.triggersAs(EffectType.instant) || value.triggersAs(EffectType.everyRound))
                base.innerEffect = value;
            else
                MessageHandlerScript.Error(cardName + ": EffectEveryRound can only target instant or everyRound effects");
        }
    }
}

//enemy effect scales up as it takes damage (range: base to base*strength)
public class EffectScaleEffectWithDamage : BaseEffectMeta
{
    public override string Name { get { return "Enemy " + argument + " increases up to " + strength + " times as it takes damage"; } } //returns name and strength
    public override string XMLName { get { return "scaleEffectWithDamage"; } } //name used to refer to this effect in XML

    private float? effectBaseStrength; //original strength of the inner effect

    public override bool shouldApplyInnerEffect() { return true; } //always trigger inner effect

    //allow this to trigger as an onDamage effect even if the child does not
    public override bool triggersAs(EffectType triggerType)
    {
        return triggerType == EffectType.enemyDamaged || base.triggersAs(triggerType);
    }

    //we dont need to do anything on expected damage
    public override void expectedDamage(ref DamageEventData d) { if (innerEffect.triggersAs(EffectType.enemyDamaged)) base.expectedDamage(ref d); } //pass to child if it is also an enemyDamaged effect

    //recalculate effect strength
    public override void actualDamage(ref DamageEventData d)
    {
        EnemyScript e = d.dest.GetComponent<EnemyScript>();

        //on first hit, cache base strength
        if (effectBaseStrength == null)
            effectBaseStrength = innerEffect.strength;

        float damageRatio = 1 - (e.curHealth / e.maxHealth);
        innerEffect.strength = Mathf.Lerp(effectBaseStrength.Value, (effectBaseStrength.Value * strength), damageRatio);

        if (innerEffect.triggersAs(EffectType.enemyDamaged)) base.expectedDamage(ref d); //pass to child if it is also an enemyDamaged effect
    }

    //since we altered the inner effect, when it gets cloned we need to copy over the changes
    public override IEffect cloneInnerEffect()
    {
        IEffect clone = base.cloneInnerEffect();
        clone.strength = innerEffect.strength;
        return clone;
    }
}

//enemy effect scales down as it takes damage (range: base to 1)
public class EffectInvScaleEffectWithDamage : BaseEffectMeta
{
    public override string Name { get { return "Enemy " + argument + " drops to 1 as it takes damage"; } } //returns name and strength
    public override string XMLName { get { return "invScaleEffectWithDamage"; } } //name used to refer to this effect in XML

    private float? effectBaseStrength; //original strength of the inner effect

    public override bool shouldApplyInnerEffect() { return true; } //always trigger inner effect

    //allow this to trigger as an onDamage effect even if the child does not
    public override bool triggersAs(EffectType triggerType)
    {
        return triggerType == EffectType.enemyDamaged || base.triggersAs(triggerType);
    }

    //we dont need to do anything on expected damage
    public override void expectedDamage(ref DamageEventData d) { if (innerEffect.triggersAs(EffectType.enemyDamaged)) base.expectedDamage(ref d); } //pass to child if it is also an enemyDamaged effect

    //recalculate effect strength
    public override void actualDamage(ref DamageEventData d)
    {
        EnemyScript e = d.dest.GetComponent<EnemyScript>();

        //on first hit, cache references
        if (effectBaseStrength == null)
            effectBaseStrength = innerEffect.strength;

        float damageRatio = 1 - (e.curHealth / e.maxHealth);
        innerEffect.strength = Mathf.Lerp(effectBaseStrength.Value, 1, damageRatio);

        if (innerEffect.triggersAs(EffectType.enemyDamaged)) base.expectedDamage(ref d); //pass to child if it is also an enemyDamaged effect
    }

    //since we altered the inner effect, when it gets cloned we need to copy over the changes
    public override IEffect cloneInnerEffect()
    {
        IEffect clone = base.cloneInnerEffect();
        clone.strength = innerEffect.strength;
        return clone;
    }
}

//enemy effect Y gets stronger by X/second
public class EffectScaleEffectWithTime : BaseEffectMeta
{
    public override string Name { get { return argument + " increases by " + strength + "/s"; } } //returns name and strength
    public override string XMLName { get { return "scaleEffectWithTime"; } } //name used to refer to this effect in XML

    public override bool shouldApplyInnerEffect() { return true; } //always trigger inner effect

    //allow this to trigger as an onDamage effect even if the child does not
    public override bool triggersAs(EffectType triggerType)
    {
        return triggerType == EffectType.periodic || base.triggersAs(triggerType);
    }

    public override void UpdateEnemy(EnemyScript e, float deltaTime)
    {
        innerEffect.strength += (strength * deltaTime);

        if (innerEffect.triggersAs(EffectType.periodic)) base.UpdateEnemy(e, deltaTime); //pass to child if it is also a periodic effect
    }

    //since we altered the inner effect, when it gets cloned we need to copy over the changes
    public override IEffect cloneInnerEffect()
    {
        IEffect clone = base.cloneInnerEffect();
        clone.strength = innerEffect.strength;
        return clone;
    }
}

//enemy effect Y gets weaker by X/second (min 0)
public class EffectInvScaleEffectWithTime : BaseEffectMeta
{
    public override string Name { get { return argument + " decreases by " + strength + "/s"; } } //returns name and strength
    public override string XMLName { get { return "InvScaleEffectWithTime"; } } //name used to refer to this effect in XML

    public override bool shouldApplyInnerEffect() { return true; } //always trigger inner effect

    //allow this to trigger as an onDamage effect even if the child does not
    public override bool triggersAs(EffectType triggerType)
    {
        return triggerType == EffectType.periodic || base.triggersAs(triggerType);
    }

    public override void UpdateEnemy(EnemyScript e, float deltaTime)
    {
        innerEffect.strength -= (strength * deltaTime);
        innerEffect.strength = Mathf.Max(innerEffect.strength, 0.0f);

        if (innerEffect.triggersAs(EffectType.periodic)) base.UpdateEnemy(e, deltaTime); //pass to child if it is also a periodic effect
    }

    //since we altered the inner effect, when it gets cloned we need to copy over the changes
    public override IEffect cloneInnerEffect()
    {
        IEffect clone = base.cloneInnerEffect();
        clone.strength = innerEffect.strength;
        return clone;
    }
}

//enemy health increases proportionally with budget (ex: if budget is twice the spawn cost, health is twice as high as in the definition)
public class EffectScaleEffectWithBudget : BaseEffectMeta
{
    public override string Name { get { return "[scaled]" + innerEffect.Name; } } //returns name and strength
    public override string XMLName { get { return "scaleEffectWithBudget"; } } //name used to refer to this effect in XML

    public override bool shouldApplyInnerEffect() { return true; } //always trigger inner effect

    //allow this to trigger as an onDamage effect even if the child does not
    public override bool triggersAs(EffectType triggerType)
    {
        return triggerType == EffectType.wave || base.triggersAs(triggerType);
    }

    bool alreadyScaled = false;

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        if (alreadyScaled)
        {
            Debug.LogWarning("ScaleEffectWithBudget triggered repeatedly!");
        }

        innerEffect.strength = Mathf.RoundToInt((((float)currentWaveData.budget) / ((float)currentWaveData.enemyData.spawnCost)) * innerEffect.strength);
        alreadyScaled = true;

        if (innerEffect.triggersAs(EffectType.wave))
            return base.alteredWaveData(currentWaveData);
        else
            return currentWaveData;
    }

    //since we altered the inner effect, when it gets cloned we need to copy over the changes
    public override IEffect cloneInnerEffect()
    {
        IEffect clone = base.cloneInnerEffect();
        clone.strength = innerEffect.strength;
        return clone;
    }
}