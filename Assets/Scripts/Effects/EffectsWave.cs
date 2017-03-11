using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// all wave effects should derive from this
/// </summary>
[ForbidEffectContext(EffectContext.enemyUnit)]
[ForbidEffectContext(EffectContext.tower)]
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
                Debug.LogWarning("<" + cardName + ">Could not find an enemy type named " + m_argument + ": defaulting to Standard.");
                m_argument = "Standard";
            }
        }
    }

    [Hide] public override string Name { get { return "change type of incoming waves to " + argument + "."; } }   //returns name and strength
    [Show] public override string XMLName { get { return "changeWaveType"; } } //name used to refer to this effect in XML

    public override WaveData alteredWaveData(WaveData currentWaveData)
    {
        WaveData newWave =  new WaveData(EnemyTypeManagerScript.instance.getEnemyTypeByName(argument), currentWaveData.budget, currentWaveData.time);

        //apply wave effects and update ranks
        if (newWave.enemyData.effectData != null)
            foreach (IEffect e in newWave.enemyData.effectData.effects)
                if (e.triggersAs(EffectType.wave))
                    newWave = ((IEffectWave)e).alteredWaveData(newWave);

        newWave.recalculateRank();

        return newWave;
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