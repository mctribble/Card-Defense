using UnityEngine;
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
    public float   strength    { get; set; }
    public string  argument    { get; set; }
    public IEffect innerEffect { get; set; } 

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
            MessageHandlerScript.Warning("meta effects should not target property effects!");
            return true;
        }

        return false;
    }
}

//child effect has an X% chance of triggering
public class EffectPercentageChance : BaseMetaEffect
{
    public override string XMLName { get { return "percentageChance"; } }
    public override string Name { get { return strength + "% chance: " + innerEffect.Name; } }

    private bool? cachedApplyInner;
    public override bool shouldApplyInnerEffect()
    {
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