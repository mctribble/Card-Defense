using UnityEngine;
using Vexe.Runtime.Types;

//all effects in this file effect the current wave.  This base effect handles behavior common to them all
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

    [Hide] public override string Name { get { return "change monster type of incoming waves to " + argument + "."; } }   //returns name and strength
    [Show] public override string XMLName { get { return "changeWaveType"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        currentWaveData.type = argument;
        return currentWaveData;
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
                return "wave spawns " + strength + " percent slower.";
            else
                return "wave spawns " + -strength + " percent faster.";
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
                return "Wave always spawns exactly 1 enemy";
            else
                return "Wave always spawns exactly " + strength + " enemies";
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
    [Hide] public override string Name { get { return "enemy attack increases on tougher waves"; } } //returns name and strength
    [Show] public override string XMLName { get { return "scaleAttackWithBudget"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        newData.enemyData.attack = Mathf.RoundToInt((((float)newData.budget) / ((float)newData.enemyData.spawnCost)) * newData.enemyData.attack);
        return newData;
    }
}

//enemy health increases proportionally with budget (ex: if budget is twice the spawn cost, health is twice as high as in the definition)
public class EffectScaleHealthWithBudget : BaseEffectWave
{
    [Hide] public override string Name { get { return "enemy health increases on tougher waves"; } } 
    [Show] public override string XMLName { get { return "scaleHealthWithBudget"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        newData.enemyData.maxHealth = Mathf.RoundToInt((((float)newData.budget) / ((float)newData.enemyData.spawnCost)) * newData.enemyData.maxHealth);
        return newData;
    }
}

//enemy health increases proportionally with budget (ex: if budget is twice the spawn cost, health is twice as high as in the definition)
public class EffectScaleSpeedWithBudget : BaseEffectWave
{
    [Hide] public override string Name { get { return "enemy speed increases on tougher waves"; } }
    [Show] public override string XMLName { get { return "scaleSpeedWithBudget"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newData = currentWaveData;
        newData.enemyData.unitSpeed = Mathf.RoundToInt((((float)newData.budget) / ((float)newData.enemyData.spawnCost)) * newData.enemyData.unitSpeed);
        return newData;
    }
}