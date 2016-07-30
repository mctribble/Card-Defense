using UnityEngine;
using Vexe.Runtime.Types;

//all effects in this file trigger when an enemy is damaged.  The effect itself could be attached either to the attacking tower or the defending enemy

//reduces incoming damage by a fixed amount (but attacks always do at least 1 damage)
public class EffectArmor : IEffectEnemyDamaged
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.enemyDamaged; } }    //effect type
    [Show, Display(2)] public float strength { get; set; }                             //how much armor the enemy has
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name { get { return "Armor: " + strength; } } //returns name and strength

    [Show, Display(1)] public string XMLName { get { return "armor"; } } //name used to refer to this effect in XML

    //alter damage calculations when we expect to deal damage, not when it actually happens, so that targeting etc. have an accurate number to work with
    public void expectedDamage(ref DamageEventData d)
    {
        if (d.rawDamage <= 1) return; //dont bother applying armor if the incoming attack is already at or below the min
        d.rawDamage -= strength; //reduce damage by armor value
        d.rawDamage = Mathf.Max(d.rawDamage, 1.0f); //but must deal at least one
    }

    //since damage is already recalculated, we dont need to do anything here
    public void actualDamage(ref DamageEventData d)
    {
    } 
}

//reduces target effect by a fixed amount (but stops at 0)
public class EffectReduceEnemyEffectOnDamage : IEffectEnemyDamaged
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.enemyDamaged; } }    //effect type
    [Show, Display(3)] public float strength { get; set; }                             //how much to reduce the target effect strength (but stops at 0)
    [Show, Display(2)] public string argument { get; set; }                            //effect to reduce

    [Hide] public string Name { get { return "Enemy " + argument + " strength: -" + strength; } } //returns name and strength

    [Show, Display(1)] public string XMLName { get { return "reduceEnemyEffectOnDamage"; } } //name used to refer to this effect in XML

    //we dont need to do anything on expected damage
    public void expectedDamage(ref DamageEventData d)
    {
    } 

    //reduce the effect
    public void actualDamage(ref DamageEventData d)
    {
        EnemyScript enemy = d.dest.GetComponent<EnemyScript>();
        if (enemy.effectData != null)
            foreach (IEffect e in enemy.effectData.effects)
                if (e.XMLName == argument)
                    e.strength = Mathf.Max(0, e.strength - strength);
    }
}

//enemy speeds up as it takes damage
public class EffectscaleSpeedWithDamage : IEffectEnemyDamaged
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.enemyDamaged; } }    //effect type
    [Show, Display(2)] public float strength { get; set; }                             //how much to reduce the target effect strength (but stops at 0)
    [Hide] public string argument { get; set; }                            //effect to reduce

    [Hide] public string Name { get { return "Enemy gets up to " + argument + " times faster as it takes damage"; } } //returns name and strength

    [Show, Display(1)] public string XMLName { get { return "scaleSpeedWithDamage"; } } //name used to refer to this effect in XML

    //we dont need to do anything on expected damage
    public void expectedDamage(ref DamageEventData d)
    {
    } 

    //recalculate speed
    public void actualDamage(ref DamageEventData d)
    {
        EnemyScript e = d.dest.GetComponent<EnemyScript>();
        float damageRatio = 1 - (e.curHealth / e.maxHealth);
        e.unitSpeed = e.baseUnitSpeed + (damageRatio * (strength - 1));
    }
}