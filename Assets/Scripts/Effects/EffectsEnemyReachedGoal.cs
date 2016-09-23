using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using System;

//effects in this file take effect when the enemy reaches its goal.  This base effect handles behavior common to them all
public abstract class BaseEffectEnemyReachedGoal : BaseEffect, IEffectEnemyReachedGoal
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } }    //this effect doesnt need a target
    [Hide] public override EffectType effectType { get { return EffectType.enemyReachedGoal; } } //effect type

    public abstract void trigger(EnemyScript enemy);
}

//enemy attack increases as it takes damage (range: 1x to 2x, rounds down)
public class EffectScaleAttackWithHealth: BaseEffectEnemyReachedGoal
{
    [Hide] public override string Name { get { return "attack increases as the enemy gets damaged."; } }
    [Show] public override string XMLName { get { return "scaleAttackWithHealth"; } }

    public override void trigger(EnemyScript enemy)
    {
        enemy.damage = enemy.damage + Mathf.FloorToInt(enemy.damage * (((float)enemy.curHealth) / ((float)enemy.maxHealth)));
    }
}

//enemy attack decreases as it takes damage (range: 1x to 0x, rounds up)
public class EffectInvScaleAttackWithHealth: BaseEffectEnemyReachedGoal
{
    [Hide] public override string Name { get { return "attack decreases as the enemy gets damaged."; } }
    [Show] public override string XMLName { get { return "invScaleAttackWithHealth"; } }

    public override void trigger(EnemyScript enemy)
    {
        enemy.damage = Mathf.CeilToInt(enemy.damage * (((float)enemy.curHealth) / ((float)enemy.maxHealth)));
    }
}

//deals X damage to the players hand
public class EffectDamageHand : BaseEffectEnemyReachedGoal
{
    [Hide] public override string Name { get { return "deals " + strength + " damage to the players hand"; } }
    [Show] public override string XMLName { get { return "damageHand"; } }

    public override void trigger(EnemyScript enemy)
    {
        DeckManagerScript.instance.DamageHand(Mathf.FloorToInt(strength));
    }
}

