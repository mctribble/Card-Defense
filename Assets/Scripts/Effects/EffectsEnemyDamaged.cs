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

//enemy slows down as it takes damage (range: base -> 1)
public class EffectInvScaleSpeedWithDamage : IEffectEnemyDamaged
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.enemyDamaged; } }    //effect type
    [Show, Display(2)] public float strength { get; set; }                             //max speed multiplier 
    [Hide] public string argument { get; set; }                                        //effect to reduce

    [Hide] public string Name { get { return "Enemy gets up to " + argument + " times faster as it takes damage"; } } //returns name and strength

    [Show, Display(1)] public string XMLName { get { return "invScaleSpeedWithDamage"; } } //name used to refer to this effect in XML

    //we dont need to do anything on expected damage
    public void expectedDamage(ref DamageEventData d)
    {
    } 

    //recalculate speed
    public void actualDamage(ref DamageEventData d)
    {
        EnemyScript e = d.dest.GetComponent<EnemyScript>();
        float damageRatio = 1 - (e.curHealth / e.maxHealth);
        e.unitSpeed = Mathf.Lerp(e.baseUnitSpeed, 1, damageRatio);
    }
}

//enemy speeds up as it takes damage (range: base -> base*strength)
public class EffectScaleSpeedWithDamage : IEffectEnemyDamaged
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.enemyDamaged; } }    //effect type
    [Show, Display(2)] public float strength { get; set; }                             //max speed multiplier 
    [Hide] public string argument { get; set; }                                        //effect to reduce

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
        e.unitSpeed = e.unitSpeed = Mathf.Lerp(e.baseUnitSpeed, (e.baseUnitSpeed * strength), damageRatio);
    }
}

//enemy effect scales up as it takes damage (range: base to base*strength)
public class EffectScaleEffectWithDamage : IEffectEnemyDamaged
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.enemyDamaged; } }    //effect type
    [Show, Display(2)] public float strength { get; set; }                             //max effect multiplier
    [Show, Display(3)] public string argument { get; set; }                            //effect to scale

    [Hide] public string Name { get { return "Enemy " + argument + " increases up to " + strength + " times as it takes damage"; } } //returns name and strength

    //cached values to avoid searching the effect list every time
    [Hide] private IEffect effectToScale;
    [Hide] private float   effectBaseStrength;

    [Show, Display(1)] public string XMLName { get { return "scaleEffectWithDamage"; } } //name used to refer to this effect in XML

    //we dont need to do anything on expected damage
    public void expectedDamage(ref DamageEventData d)
    {
    } 

    //recalculate speed
    public void actualDamage(ref DamageEventData d)
    {
        EnemyScript e = d.dest.GetComponent<EnemyScript>();

        //on first hit, cache references
        if (effectToScale == null)
        {
            foreach (IEffect effect in e.effectData.effects)
            {
                if (effect.Name == argument)
                {
                    effectToScale = effect;
                    effectBaseStrength = effect.strength;
                    break;
                }
            }
            Debug.LogWarning("ScaleEffectWithDamage cant find effect " + argument);
        }

        float damageRatio = 1 - (e.curHealth / e.maxHealth);
        effectToScale.strength = Mathf.Lerp(effectBaseStrength, (effectBaseStrength * strength), damageRatio);
    }
}

//enemy effect scales down as it takes damage (range: base to 1)
public class EffectInvScaleEffectWithDamage : IEffectEnemyDamaged
{
    [Hide] public TargetingType targetingType { get { return TargetingType.noCast; } } //this effect should never be on a card, and thus should never be cast
    [Hide] public EffectType effectType { get { return EffectType.enemyDamaged; } }    //effect type
    [Hide] public float strength { get; set; }                                         //effect strength (unused)
    [Show, Display(2)] public string argument { get; set; }                            //effect to scale

    [Hide] public string Name { get { return "Enemy " + argument + " drops to 1 as it takes damage"; } } //returns name and strength

    //cached values to avoid searching the effect list every time
    [Hide] private IEffect effectToScale;
    [Hide] private float   effectBaseStrength;

    [Show, Display(1)] public string XMLName { get { return "invScaleEffectWithDamage"; } } //name used to refer to this effect in XML

    //we dont need to do anything on expected damage
    public void expectedDamage(ref DamageEventData d)
    {
    } 

    //recalculate speed
    public void actualDamage(ref DamageEventData d)
    {
        EnemyScript e = d.dest.GetComponent<EnemyScript>();

        //on first hit, cache references
        if (effectToScale == null)
        {
            foreach (IEffect effect in e.effectData.effects)
            {
                if (effect.Name == argument)
                {
                    effectToScale = effect;
                    effectBaseStrength = effect.strength;
                    break;
                }
            }
            Debug.LogWarning("invScaleEffectWithDamage cant find effect " + argument);
        }

        float damageRatio = 1 - (e.curHealth / e.maxHealth);
        effectToScale.strength = Mathf.Lerp(effectBaseStrength, 1, damageRatio);
    }
}