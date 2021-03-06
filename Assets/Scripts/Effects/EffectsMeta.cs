﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using Vexe.Runtime.Types;
using System.Linq;

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

    [Show] public virtual IEffect innerEffect { get; set; } //effect targeted by this effect

    public abstract bool shouldApplyInnerEffect(); //determines whether the inner effect should be applied

    //each of these trigger functions check shouldApplyInnerEffect() and pass the call through if it returns true
    public virtual WaveData alteredWaveData(WaveData currentWaveData) { if (shouldApplyInnerEffect()) { return ((IEffectWave)innerEffect).alteredWaveData(currentWaveData); } else { return currentWaveData;} }
    public virtual IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange) { if (shouldApplyInnerEffect()) { return ((IEffectTowerTargeting)innerEffect).findTargets(towerPosition, towerRange); } else { return null; } }
    public virtual void UpdateEnemy(EnemyScript e, float deltaTime) { if (shouldApplyInnerEffect()) { ((IEffectPeriodic)innerEffect).UpdateEnemy(e, deltaTime); } }
    public virtual void trigger(ref PlayerCard card, GameObject card_gameObject) { if (shouldApplyInnerEffect()) { ((IEffectSelf)innerEffect).trigger(ref card, card_gameObject); } }
    public virtual void trigger(ref DamageEventData d, int pointsOfOvercharge) { if (shouldApplyInnerEffect()) { ((IEffectOvercharge)innerEffect).trigger(ref d, pointsOfOvercharge); } }
    public virtual void trigger() { if (shouldApplyInnerEffect()) { ((IEffectInstant)innerEffect).trigger(); } }
    public virtual void trigger(EnemyScript enemy) { if (shouldApplyInnerEffect()) { ((IEffectEnemyReachedGoal)innerEffect).trigger(enemy); } }
    public virtual void onEnemyDeath(EnemyScript enemy) { if (shouldApplyInnerEffect()) { ((IEffectDeath)innerEffect).onEnemyDeath(enemy); } }
    public virtual void onTowerDeath(TowerScript tower) { if (shouldApplyInnerEffect()) { ((IEffectDeath)innerEffect).onTowerDeath(tower); } }
    public virtual void onTowerSpawned(TowerScript tower) { if (shouldApplyInnerEffect()) { ((IEffectOnSpawned)innerEffect).onTowerSpawned(tower); } }
    public virtual void onEnemySpawned(EnemyScript enemy) {  if (shouldApplyInnerEffect()) { ((IEffectOnSpawned)innerEffect).onEnemySpawned(enemy); } }
    public virtual void playerCardDrawn(CardScript playerCard) { if (shouldApplyInnerEffect()) { ((IEffectCardDrawn)innerEffect).playerCardDrawn(playerCard); } }
    public virtual void enemyCardDrawn(EnemyScript enemyCard) { if (shouldApplyInnerEffect()) { ((IEffectCardDrawn)innerEffect).enemyCardDrawn(enemyCard); } }
    public virtual void towerAttack(TowerScript tower) { if (shouldApplyInnerEffect()) { ((IEffectAttack)innerEffect).towerAttack(tower); } }
    public virtual void enemyAttack(EnemyScript enemy) { if (shouldApplyInnerEffect()) { ((IEffectAttack)innerEffect).enemyAttack(enemy); } }

    //this one is special: rankChanged is used for scaling effects, so those should ALWAYS be passed through, even if the condition on this effect is false
    public virtual void rankChanged(int rank) { { ((IEffectRank)innerEffect).rankChanged(rank); } }

    //source tracking forwards down to the inner effect, if it cares to know.  This cascades down the tree even if we should not apply the inner effect
    private TowerScript _effectSource;
    public virtual TowerScript effectSource
    {
        get
        {
            return _effectSource;
        }
        set
        {
            _effectSource = value;
            if (innerEffect.triggersAs(EffectType.sourceTracked))
                ((IEffectSourceTracked)innerEffect).effectSource = value;
        }
    }

    //returns the XMLName, skipping over any meta effects if they are present.  See also: EffectTypeManagerScript.parse()
    [Hide] public override string FinalXMLName { get { return innerEffect.FinalXMLName; } } 

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

    //targeting effects return priority of child
    [Hide] public TargetingPriority priority { get { return ((IEffectTowerTargeting)innerEffect).priority; } }

    //returns whether or not the inner effect requires us to cache values
    protected bool shouldCacheValue()
    {
        if (innerEffect.triggersAs(EffectType.enemyDamaged)) return true; 

        //it doesnt make sense for a property to be targeted by another effect, since it doesnt actually do anything on its own
        if (innerEffect.triggersAs(EffectType.property))
        {
            Debug.LogWarning("<" + cardName + ">meta effects should not target property effects!");
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
    public override string Name { get { return "[ " + strength + "% chance]" + innerEffect.Name; } }

    private bool? cachedApplyInner;
    public override bool shouldApplyInnerEffect()
    {
        //never apply inner effect if it is null
        if (innerEffect == null)
        {
            Debug.LogWarning("<" + cardName + "> " + XMLName + " has no target and did nothing.");
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
                Debug.LogWarning("<" + cardName + "> " + XMLName + " could not convert argument to an int.  defaulted to 2");
                rangeMax = 2;
            }
        }
    }

    public override string XMLName { get { return "ifRollRange"; } }
    public override string Name
    {
        get
        {
            string rangeString;
            if (rangeMin == rangeMax)
                rangeString = "[" + rangeMin + "]";
            else
                rangeString = "[" + rangeMin + " - " + rangeMax + "]";

            return rangeString + innerEffect.Name;
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
        }
    }

    //whether we should trigger is cached, since the roll should only happen once and we dont want later rolls to mess with it
    public override bool shouldApplyInnerEffect()
    {
        //throw warning if no die has been rolled
        if (parentData.propertyEffects.dieRoll == null)
        {
            Debug.LogWarning("<" + cardName + "> " + XMLName + ":no die has been rolled!");
            return false;
        }

        if (rangeMin <= rangeMax)
        {
            if (parentData.propertyEffects.dieRoll < rangeMin) return false;
            if (parentData.propertyEffects.dieRoll > rangeMax) return false;

            return true;
        }
        else
        {
            Debug.LogWarning("<" + cardName + "> " + XMLName + ": range max is lower than range min!  Try switching strength and argument");
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

    public override string Name { get { return "[" + strength + " times]" + innerEffect.Name; } }
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
    public override IEnumerable<EnemyScript> findTargets(Vector2 towerPosition, float towerRange)
    {
        IEnumerable<EnemyScript> result = base.findTargets(towerPosition, towerRange);

        if (result != null)
            if (result.Any())
                LevelManagerScript.instance.StartCoroutine(cooldown());

        return result;
    }
}

//target instant effect triggers once every round (using IEffectInstant)
public class EffectEveryRound : BaseEffectMeta
{
    public override string Name    { get { return "[per round]" + innerEffect.Name; } }
    public override string XMLName { get { return "everyRound"; } }
    public override bool   shouldApplyInnerEffect() { return true; }

    //regardless of how the inner effect normally triggers, we want it to fire once every round.  We need to keep the triggertype == EffectType.meta so we dont break EffectData.cloneEffect()
    public override bool triggersAs(EffectType triggerType) { return (triggerType == EffectType.everyRound) || (triggerType == EffectType.meta); }

    //because we use IEffectInstant, we can only target instant or everyRound effects
    public override IEffect innerEffect
    {
        get { return base.innerEffect; }
        set
        {
            if (value.triggersAs(EffectType.instant) || value.triggersAs(EffectType.everyRound))
                base.innerEffect = value;
            else
                Debug.LogError(cardName + ": EffectEveryRound can only target instant or everyRound effects");
        }
    }
}

//target instant effect triggers when the enemy is spawned (using IEffectInstant)
public class EffectOnSpawned : BaseEffectMeta
{
    public override string Name { get { return "[on spawn]" + innerEffect.Name; } }
    public override string XMLName { get { return "onSpawned"; } }
    public override bool shouldApplyInnerEffect() { return true; }

    //regardless of how the inner effect normally triggers, we want it to fire once every round.  We need to keep the triggertype == EffectType.meta so we dont break EffectData.cloneEffect()
    public override bool triggersAs(EffectType triggerType) { return (triggerType == EffectType.spawn) || (triggerType == EffectType.meta) || base.triggersAs(triggerType); }

    //because we use IEffectInstant, we can only target instant or everyRound effects
    public override IEffect innerEffect
    {
        get { return base.innerEffect; }
        set
        {
            if (value.triggersAs(EffectType.instant))
                base.innerEffect = value;
            else
                Debug.LogError(cardName + ": Effect onSpawned can only target instant");
        }
    }

    //on enemy spawned, trigger the child
    public override void onEnemySpawned(EnemyScript e)
    {
        ((IEffectInstant)innerEffect).trigger();
    }
}

//target enemy effect scales up as the enemy takes damage.  Increases proportionally if x = 1.Higher / lower values cause it to increase faster/slower, respectively.
public class EffectScaleEffectWithDamage : BaseEffectMeta
{
    public override string Name { get { return "[scales up as it takes damage]" + innerEffect.Name; } } //returns name and strength
    public override string XMLName { get { return "scaleEffectWithDamage"; } } //name used to refer to this effect in XML

    private float? effectBaseStrength; //original strength of the inner effect

    public override bool shouldApplyInnerEffect() { return true; } //always trigger inner effect

    //allow this to trigger as an onDamage effect even if the child does not
    public override bool triggersAs(EffectType triggerType) { return triggerType == EffectType.enemyDamaged || base.triggersAs(triggerType); }

    //we dont need to do anything on expected damage
    public override void expectedDamage(ref DamageEventData d) { if (innerEffect.triggersAs(EffectType.enemyDamaged)) base.expectedDamage(ref d); } //pass to child if it is also an enemyDamaged effect

    //recalculate effect strength
    public override void actualDamage(ref DamageEventData d)
    {
        EnemyScript e = d.dest;

        //on first hit, cache base strength
        if (effectBaseStrength == null)
            effectBaseStrength = innerEffect.strength;

        float scaleRatio = 1 - ((float)e.curHealth / (float)e.maxHealth);                //ratio we are scaling by
        float scaleFactor = ((scaleRatio -1 ) * strength) + 1;                           //factor to use for scaling
        innerEffect.strength = Mathf.RoundToInt(scaleFactor * effectBaseStrength.Value); //scale

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

//target enemy effect weakens as it takes damage.  Decreases proportionally if x = 1.  Higher/lower values cause it to decrease faster/slower, respectively.
public class EffectinvScaleEffectWithDamage : BaseEffectMeta
{
    public override string Name { get { return "[scales down as it takes damage]" + innerEffect.Name; } } //returns name and strength
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
        EnemyScript e = d.dest;

        //on first hit, cache references
        if (effectBaseStrength == null)
            effectBaseStrength = innerEffect.strength;

        float healthRatio = (float)e.curHealth / (float)e.maxHealth;                                 //how much health the unit still has (0: dead.  1: full health)
        innerEffect.strength = Mathf.CeilToInt(effectBaseStrength.Value * (healthRatio / strength)); //scale
        innerEffect.strength = Mathf.Max(innerEffect.strength, 1.0f);                                //enforce minimum

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

//on towers, multiplies effect strength by attack power of the tower.  Use this to create effects whose strength vary with the strength of the tower, like the poison on "Poison Ammo".
public class EffectScaleEffectWithTowerAttack : BaseEffectMeta, IEffectSourceTracked
{
    //name is in yellow if an upgrade that changes the effect strength is being hovered over the tower
    private bool upgrading = false;
    public override string Name
    {
        get
        {
            string result = "";

            if (upgrading)
                result += "<color=yellow>";

            result += "[scales with tower attack]" + innerEffect.Name;

            if (upgrading)
                result += "</color>";

            return result;
        }
    }

    public override string XMLName { get { return "scaleEffectByTowerAttack"; } } //name used to refer to this effect in XML

    private float? effectBaseStrength; //original strength of the inner effect

    public override bool shouldApplyInnerEffect() { return true; } //always trigger inner effect

    public override bool triggersAs(EffectType triggerType)
    {
        return triggerType == EffectType.enemyDamaged ||  //allow this to trigger as an onDamage effect even if the child does not
               triggerType == EffectType.sourceTracked || //we want to be notified of the tower when we get added so we can set up the description
               base.triggersAs(triggerType);              //maintain trigger types from the base class
    }

    //we dont need to do anything on expected damage
    public override void expectedDamage(ref DamageEventData d) { if (innerEffect.triggersAs(EffectType.enemyDamaged)) base.expectedDamage(ref d); } //pass to child if it is also an enemyDamaged effect

    //recalculate effect strength when attacks hit
    public override void actualDamage(ref DamageEventData d)
    {
        if (effectBaseStrength == null)
            effectBaseStrength = innerEffect.strength;

        scaleInnerEffect();
    }

    //scales the child effect appropriately
    private void scaleInnerEffect()
    {
        if (effectSource != null)
            innerEffect.strength = effectBaseStrength.Value * effectSource.attackPower;
        else
            innerEffect.strength = effectBaseStrength.Value;
    }

    //recalculate effect strength when we are told what tower has this effect.  Note that this also gets called when hovering over to do an upgrade, and so may be later set to null again.
    public override TowerScript effectSource
    {
        get { return base.effectSource; }
        set
        {
            //fetch the base effect strength if we dont know it already
            if (effectBaseStrength == null)
                effectBaseStrength = innerEffect.strength;

            //if we the old source was a tower, deregister for the events
            if (base.effectSource != null)
            {
                base.effectSource.towerUpgradingEvent -= towerUpgrading;
                base.effectSource.towerUpgradedEvent  -= towerUpgraded;
            }
            //if the new value is a tower, register for the events
            if (value != null)
            {
                value.towerUpgradingEvent += towerUpgrading;
                value.towerUpgradedEvent  += towerUpgraded;
            }

            base.effectSource = value; //update effectSource

            scaleInnerEffect(); //rescale the effect
        }
    }

    //rescales the effect when an upgrade is hovered over the tower with this effect.  If upgrade is null, that means the player changed their mind and moved away from a previous upgrade
    private void towerUpgrading(TowerScript hoveredTower, UpgradeData upgrade)
    {
        if (hoveredTower == effectSource)
        {

            if (upgrade == null)
            {
                scaleInnerEffect(); //an upgrade was cancelled.  revert to normal strength
                upgrading = false;  //and remove the 'upgrading' flag that changes name color
            }
            else
            {
                innerEffect.strength = effectBaseStrength.Value * (effectSource.attackPower * upgrade.attackMultiplier + upgrade.attackModifier); //scale as if the upgrade had already been applied

                //if the upgrade actually changes the attack, then set the 'upgrading' flag that changes name color
                if (upgrade.attackMultiplier != 1.0f || upgrade.attackModifier != 0.0f)
                    upgrading = true; 
            }

            hoveredTower.UpdateTooltipText(); //make the tower update its tooltip
        }
        else
            Debug.LogWarning("EffectScaleEffectWithTowerAttack should not be registered to hear about upgrades from that tower, as it is not the source of the effect");
    }

    //rescales the effect when the tower this upgrade is on gets upgraded
    private void towerUpgraded(TowerScript upgradedTower)
    {
        if (upgradedTower == effectSource)
        {
            scaleInnerEffect();
            upgradedTower.UpdateTooltipText();
        }
        else
            Debug.LogWarning("EffectScaleEffectWithTowerAttack should not be registered to hear about upgrades from that tower, as it is not the source of the effect");
    }

    //clone the effect at its base strength, since it will get scaled again anyway when the effectSource gets updated
    public override IEffect cloneInnerEffect()
    {
        IEffect clone = base.cloneInnerEffect();

        if (effectBaseStrength != null)
            clone.strength = effectBaseStrength.Value;
        else
            clone.strength = innerEffect.strength;

        return clone;
    }
}

//target enemy effect gets stronger by X/s
public class EffectScaleEffectWithTime : BaseEffectMeta
{
    public override string Name { get { return "[increases by " + strength + "/s]"; } } //returns name and strength
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

//target enemy effect weakens by x/s.  
public class EffectInvScaleEffectWithTime : BaseEffectMeta
{
    public override string Name { get { return "[decreases by " + strength + "/s]"; } } //returns name and strength
    public override string XMLName { get { return "InvScaleEffectWithTime"; } } //name used to refer to this effect in XML

    public override bool shouldApplyInnerEffect() { return true; } //always trigger inner effect

    //allow this to trigger as an onDamage effect even if the child does not
    public override bool triggersAs(EffectType triggerType)
    {
        return triggerType == EffectType.periodic || base.triggersAs(triggerType);
    }

    public override void UpdateEnemy(EnemyScript e, float deltaTime)
    {
        innerEffect.strength -= (strength * deltaTime);               //weaken target effect
        innerEffect.strength = Mathf.Max(innerEffect.strength, 0.0f); //enforce minimum
        if (innerEffect.strength == 0.0f)                             //if at minimum, we are done
            done = true;

        if (innerEffect.triggersAs(EffectType.periodic)) base.UpdateEnemy(e, deltaTime); //pass to child if it is also a periodic effect
    }

    //since we altered the inner effect, when it gets cloned we need to copy over the changes
    public override IEffect cloneInnerEffect()
    {
        IEffect clone = base.cloneInnerEffect();
        clone.strength = innerEffect.strength;
        return clone;
    }

    //once we reach the floor, this effect can be removed
    private bool done;
    public override bool shouldBeRemoved() { return base.shouldBeRemoved() || done; }
}

//target enemy effect scales up with the rank of the wave (multiplied by X per rank, like how stats scale)
public class EffectScaleEffectWithRank : BaseEffectMeta
{
    public override string Name { get { return "[ranked]" + innerEffect.Name; } } //returns name and strength
    public override string XMLName { get { return "scaleEffectWithRank"; } } //name used to refer to this effect in XML

    public override bool shouldApplyInnerEffect() { return true; } //always trigger inner effect

    //allow this to trigger as an onDamage effect even if the child does not
    public override bool triggersAs(EffectType triggerType)
    {
        return triggerType == EffectType.rank || base.triggersAs(triggerType);
    }

    private bool  alreadyScaled = false;
    private float baseEffectStrength = 0.0f;

    public override void rankChanged(int rank)
    {
        if (alreadyScaled == false)
        {
            baseEffectStrength = innerEffect.strength;
            alreadyScaled = true;
        }

        innerEffect.strength = baseEffectStrength * Mathf.Pow(strength, (rank - 1));

        if (innerEffect.triggersAs(EffectType.rank))
            base.rankChanged(rank);
    }

    //since we altered the inner effect, when it gets cloned we need to copy over the changes
    public override IEffect cloneInnerEffect()
    {
        IEffect clone = base.cloneInnerEffect();
        clone.strength = innerEffect.strength;
        return clone;
    }
}

//target instant triggers when the card is drawn
public class EffectOnCardDrawn : BaseEffectMeta
{
    public override string Name { get { return "[when drawn]" + innerEffect.Name; } }
    public override string XMLName { get { return "onCardDrawn"; } }

    public override bool shouldApplyInnerEffect() { return true; } 

    public override bool triggersAs(EffectType triggerType) { return triggerType == EffectType.meta || triggerType==EffectType.cardDrawn; }

    //behave the same regardless of what type of card is drawn
    public override void playerCardDrawn(CardScript playerCard) { ((IEffectInstant)innerEffect).trigger(); }
    public override void enemyCardDrawn(EnemyScript enemyCard) { ((IEffectInstant)innerEffect).trigger(); }
}

//target instant triggers when the tower or enemy attacks something
public class EffectOnAttack : BaseEffectMeta
{
    public override string Name { get { return "When this attacks, " + innerEffect.Name; } }
    public override string XMLName { get { return "onAttack"; } }

    public override bool shouldApplyInnerEffect() { return true; }

    //regardless of how the child normally triggers, we only want it to fire when this attacks.  
    public override bool triggersAs(EffectType triggerType) { return triggerType == EffectType.attack || triggerType == EffectType.meta; }

    //behave the same regardless of what type of entity attacked
    public override void towerAttack(TowerScript tower) { ((IEffectInstant)innerEffect).trigger(); }
    public override void enemyAttack(EnemyScript enemy) { ((IEffectInstant)innerEffect).trigger(); }
}

//replaces the description of the inner effect with Y.  If Y is absent or empty, then the effect is not listed.
public class EffectCustomDescription : BaseEffectMeta
{
    public override string Name { get { if (argument == "") return null; else return argument; } }
    public override string XMLName { get { return "customDescription"; } }

    public override bool shouldApplyInnerEffect() { return true; }
}