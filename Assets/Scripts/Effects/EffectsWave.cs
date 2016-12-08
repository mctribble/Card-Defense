using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// all wave effects should derive from this
/// </summary>
public abstract class BaseEffectWave : BaseEffect, IEffectWave
{
    [Hide] public override TargetingType targetingType { get { return TargetingType.none; } } //wave effects dont need a target
    [Hide] public override EffectType    effectType    { get { return EffectType.wave; } }    //this is a wave effect

    public abstract WaveData alteredWaveData(WaveData currentWaveData);
}

//alters wave budget by x%
public class EffectBudgetPercentageChange : BaseEffectWave
{
    [Hide] public override string Name //returns name and strength
    {
        get
        {
            if (strength >= 0)
                return "wave strength: +" + strength + '%';
            else
                return "wave strength: " + -strength + '%';
        }
    }

    [Show] public override string XMLName { get { return "budgetPercentageChange"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        currentWaveData.budget = Mathf.RoundToInt(currentWaveData.budget * (1.0f + (strength / 100.0f)));
        return currentWaveData;
    }
}

//sets the enemy type to argument
public class EffectChangeWaveType : BaseEffectWave
{
    [Hide] private string m_argument;
    [Show] public override string argument
    {
        get { return m_argument; }
        set
        {
            m_argument = value;

            //validate argument: must be an existing enemy type
            try
            {
                EnemyTypeManagerScript.instance.getEnemyTypeByName(m_argument);
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                MessageHandlerScript.Warning("<" + cardName + ">Could not find an enemy type named " + m_argument + ": defaulting to Standard.");
                m_argument = "Standard";
            }
        }
    }

    [Hide] public override string Name { get { return "change type of incoming waves to " + argument + "."; } }   //returns name and strength
    [Show] public override string XMLName { get { return "changeWaveType"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        return new WaveData(argument, currentWaveData.budget, currentWaveData.time);
    }
}

//adjusts the wave spawn time by X%
public class EffectTimePercentageChange : BaseEffectWave
{
    [Hide] public override string Name //returns name and strength
    {
        get
        {
            if (strength >= 0)
                return "wave spawns " + strength + "% slower.";
            else
                return "wave spawns " + -strength + "% faster.";
        }
    }

    [Show] public override string XMLName { get { return "timePercentageChange"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        currentWaveData.time = currentWaveData.time * (1.0f + (strength / 100.0f));
        return currentWaveData;
    }
}

//forces the wave to spawn with exactly X enemies
public class EffectFixedSpawnCount : BaseEffectWave
{
    [Hide] public override string Name //returns name and strength
    {
        get
        {
            if (strength == 1)
                return "Unique";
            else
                return "Spawns in groups of " + strength;
        }
    }

    [Show] public override string XMLName { get { return "fixedSpawnCount"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        newData.forcedSpawnCount = Mathf.RoundToInt(strength);
        return newData;
    }
}

//enemy Attack increases proportionally with budget (ex: if budget is twice the spawn cost, health is twice as high as in the definition)
public class EffectScaleAttackWithBudget : BaseEffectWave
{
    [Hide] public override string Name { get { return "attack increases on tougher waves"; } } //returns name and strength
    [Show] public override string XMLName { get { return "scaleAttackWithBudget"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        newData.enemyData.baseAttack = Mathf.RoundToInt((((float)newData.budget) / ((float)newData.enemyData.baseSpawnCost)) * newData.enemyData.baseAttack);
        return newData;
    }
}

//enemy health increases proportionally with budget (ex: if budget is twice the spawn cost, health is twice as high as in the definition)
public class EffectScaleHealthWithBudget : BaseEffectWave
{
    [Hide] public override string Name { get { return "health increases on tougher waves"; } } 
    [Show] public override string XMLName { get { return "scaleHealthWithBudget"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        newData.enemyData.baseMaxHealth = Mathf.RoundToInt((((float)newData.budget) / ((float)newData.enemyData.baseSpawnCost)) * newData.enemyData.baseMaxHealth);
        return newData;
    }
}

//enemy health increases proportionally with budget (ex: if budget is twice the spawn cost, health is twice as high as in the definition)
public class EffectScaleSpeedWithBudget : BaseEffectWave
{
    [Hide] public override string Name { get { return "speed increases on tougher waves"; } }
    [Show] public override string XMLName { get { return "scaleSpeedWithBudget"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        newData.enemyData.baseUnitSpeed = Mathf.RoundToInt((((float)newData.budget) / ((float)newData.enemyData.baseSpawnCost)) * newData.enemyData.baseUnitSpeed);
        return newData;
    }
}