using UnityEngine;
using Vexe.Runtime.Types;

//all effects in this file effect the current wave

//alters wave budget by x%
public class EffectBudgetPercentageChange : IEffectWave
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //wave effects dont need a target
    [Hide] public EffectType effectType { get { return EffectType.wave; } }            //this is a wave effect
    [Show, Display(2)] public float strength { get; set; }                             //% change
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name //returns name and strength
    {
        get
        {
            if (strength >= 0)
                return "wave strength: +" + strength + '%';
            else
                return "wave strength: " + -strength + '%';
        }
    }

    [Show, Display(1)] public string XMLName { get { return "budgetPercentageChange"; } } //name used to refer to this effect in XML

    public WaveData alteredWaveData(WaveData currentWaveData)
    {
        currentWaveData.budget = Mathf.RoundToInt(currentWaveData.budget * (1.0f + (strength / 100.0f)));
        return currentWaveData;
    }
}

//sets the enemy type to argument
public class EffectChangeWaveType : IEffectWave
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //wave effects dont need a target
    [Hide] public EffectType effectType { get { return EffectType.wave; } }            //this is a wave effect
    [Hide] public float strength { get; set; }                                         //how strong this effect is.  (unused in this effect)
    [Show, Display(2)] public string argument { get; set; }                            //new wave type

    [Hide] public string Name { get { return "change monster type of incoming waves to " + argument + "."; } }   //returns name and strength

    [Show, Display(1)] public string XMLName { get { return "changeWaveType"; } } //name used to refer to this effect in XML

    public WaveData alteredWaveData(WaveData currentWaveData)
    {
        currentWaveData.type = argument;
        return currentWaveData;
    }
}

//adjusts the wave spawn time by X%
public class EffectTimePercentageChange : IEffectWave
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //wave effects dont need a target
    [Hide] public EffectType effectType { get { return EffectType.wave; } }            //this is a wave effect
    [Show, Display(2)] public float strength { get; set; }                             //% change
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name //returns name and strength
    {
        get
        {
            if (strength >= 0)
                return "wave spawns " + strength + " percent slower.";
            else
                return "wave spawns " + -strength + " percent faster.";
        }
    }

    [Show, Display(1)] public string XMLName { get { return "timePercentageChange"; } } //name used to refer to this effect in XML

    public WaveData alteredWaveData(WaveData currentWaveData)
    {
        currentWaveData.time = currentWaveData.time * (1.0f + (strength / 100.0f));
        return currentWaveData;
    }
}

//forces the wave to spawn with exactly X enemies
public class EffectFixedSpawnCount : IEffectWave
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //wave effects dont need a target
    [Hide] public EffectType effectType { get { return EffectType.wave; } }            //this is a wave effect
    [Show, Display(2)] public float strength { get; set; }                             //enemy count
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name //returns name and strength
    {
        get
        {
            if (strength == 1)
                return "Wave always spawns exactly 1 enemy";
            else
                return "Wave always spawns exactly " + strength + " enemies";
        }
    }

    [Show] public string XMLName { get { return "fixedSpawnCount"; } } //name used to refer to this effect in XML

    public WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        newData.forcedSpawnCount = Mathf.RoundToInt(strength);
        return newData;
    }
}

//enemy Attack increases proportionally with budget (ex: if budget is twice the spawn cost, health is twice as high as in the definition)
public class EffectScaleAttackWithBudget : IEffectWave
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //wave effects dont need a target
    [Hide] public EffectType effectType { get { return EffectType.wave; } }            //this is a wave effect
    [Hide] public float strength { get; set; }                                         //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name //returns name and strength
    {
        get
        {
            return "enemy attack increases on tougher waves";
        }
    }

    [Show] public string XMLName { get { return "scaleAttackWithBudget"; } } //name used to refer to this effect in XML

    public WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        newData.enemyData.attack = Mathf.RoundToInt((((float)newData.budget) / ((float)newData.enemyData.spawnCost)) * newData.enemyData.attack);
        return newData;
    }
}

//enemy health increases proportionally with budget (ex: if budget is twice the spawn cost, health is twice as high as in the definition)
public class EffectScaleEffectWithBudget : IEffectWave
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //wave effects dont need a target
    [Hide] public EffectType effectType { get { return EffectType.wave; } }            //this is a wave effect
    [Hide] public float strength { get; set; }                                         //effect strength (unused in this effect)
    [Show, Display(2)] public string argument { get; set; }                            //effect argument (effect to be scaled up)

    [Hide] public string Name //returns name and strength
    {
        get
        {
            return "enemy " + argument + " increases on tougher waves";
        }
    }

    [Show, Display(1)] public string XMLName { get { return "scaleEffectWithBudget"; } } //name used to refer to this effect in XML

    public WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;

        foreach (IEffect e in newData.enemyData.effectData.effects)
            if (e.Name == argument)
                e.strength = Mathf.RoundToInt((((float)newData.budget) / ((float)newData.enemyData.spawnCost)) * e.strength);

        return newData;
    }
}

//enemy health increases proportionally with budget (ex: if budget is twice the spawn cost, health is twice as high as in the definition)
public class EffectScaleHealthWithBudget : IEffectWave
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //wave effects dont need a target
    [Hide] public EffectType effectType { get { return EffectType.wave; } }            //this is a wave effect
    [Hide] public float strength { get; set; }                                         //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name //returns name and strength
    {
        get
        {
            return "enemy health increases on tougher waves";
        }
    }

    [Show] public string XMLName { get { return "scaleHealthWithBudget"; } } //name used to refer to this effect in XML

    public WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        newData.enemyData.maxHealth = Mathf.RoundToInt((((float)newData.budget) / ((float)newData.enemyData.spawnCost)) * newData.enemyData.maxHealth);
        return newData;
    }
}

//enemy health increases proportionally with budget (ex: if budget is twice the spawn cost, health is twice as high as in the definition)
public class EffectScaleSpeedWithBudget : IEffectWave
{
    [Hide] public TargetingType targetingType { get { return TargetingType.none; } }   //wave effects dont need a target
    [Hide] public EffectType effectType { get { return EffectType.wave; } }            //this is a wave effect
    [Hide] public float strength { get; set; }                                         //effect strength (unused in this effect)
    [Hide] public string argument { get; set; }                                        //effect argument (unused in this effect)

    [Hide] public string Name //returns name and strength
    {
        get
        {
            return "enemy speed increases on tougher waves";
        }
    }

    [Show] public string XMLName { get { return "scaleSpeedWithBudget"; } } //name used to refer to this effect in XML

    public WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        newData.enemyData.unitSpeed = Mathf.RoundToInt((((float)newData.budget) / ((float)newData.enemyData.spawnCost)) * newData.enemyData.unitSpeed);
        return newData;
    }
}