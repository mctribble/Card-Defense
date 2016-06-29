using UnityEngine;
using System.Collections;
using System;

//all effects in this file trigger when an enemy is damaged.  The effect itself could be attached either to the attacking tower or the defending enemy

//reduces incoming damage by a fixed amount (but attacks always do at least 1 damage)
public class EffectArmor : IEffectEnemyDamaged
{
    //generic interface
    public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    public EffectType effectType { get { return EffectType.enemyDamaged; } }    //effect type
    public float strength { get; set; }                                         //how much armor the enemy has
    public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    public string Name { get { return "Armor: " + strength; } }     //returns name and strength
    public string XMLName { get { return "armor"; } } //name used to refer to this effect in XML

    //alter damage calculations when we expect to deal damage, not when it actually happens, so that targeting etc. have an accurate number to work with
    public void expectedDamage(DamageEventData d)
    {
        d.rawDamage -= strength; //reduce damage by armor value
        d.rawDamage = Mathf.Max(d.rawDamage, 1.0f); //but must deal at least one
    }

    public void actualDamage(DamageEventData d) { } //since damage is already recalculated, we dont need to do anything here
}

//reduces target effect by a fixed amount (but stops at 0)
public class EffectReduceEnemyEffectOnDamage : IEffectEnemyDamaged
{
    //generic interface
    public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    public EffectType effectType { get { return EffectType.enemyDamaged; } }    //effect type
    public float strength { get; set; }                                         //how much to reduce the target effect strength (but stops at 0)
    public string argument { get; set; }                                        //effect to reduce

    //this effect
    public string Name { get { return "Enemy " + argument + "strength: -" + strength; } }     //returns name and strength
    public string XMLName { get { return "reduceEnemyEffectOnDamage"; } } //name used to refer to this effect in XML

    public void expectedDamage(DamageEventData d) { } //we dont need to do anything on expected damage

    //reduce the effect
    public void actualDamage(DamageEventData d)
    {
        foreach (IEffect e in d.dest.GetComponent<EnemyScript>().data.effectData.effects)
            if (e.XMLName == argument)
                e.strength = Mathf.Max(0, e.strength - strength);
    }
}