using UnityEngine;
using System.Collections;

//all effects in this file effect the current wave


//alters wave budget by x%
public class EffectBudgetPercentageChange : IEffectWave {

	//generic interface
	public TargetingType targetingType { get { return TargetingType.none; } }	//wave effects dont need a target
	public EffectType effectType { get { return EffectType.wave; } }            //this is a wave effect
    public float strength { get; set; }                                         //how strong this effect is.  (unused in this effect)
    public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    public string Name { get { return "Increase wave strength by " + strength + " percent."; } }	//returns name and strength
	public WaveData alteredWaveData(WaveData currentWaveData) {
		currentWaveData.budget = Mathf.RoundToInt(currentWaveData.budget * (1.0f + (strength / 100.0f)));
		return currentWaveData;
	}
	
}

//sets the enemy type to argument
public class EffectChangeWaveType : IEffectWave
{

    //generic interface
    public TargetingType targetingType { get { return TargetingType.none; } }   //wave effects dont need a target
    public EffectType effectType { get { return EffectType.wave; } }            //this is a wave effect
    public float strength { get; set; }                                         //how strong this effect is.  (unused in this effect)
    public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    public string Name { get { return "change monster type of next wave to " + argument + "."; } }   //returns name and strength
    public WaveData alteredWaveData(WaveData currentWaveData)
    {
        currentWaveData.type = argument;
        return currentWaveData;
    }

}

//adjusts the wave spawn time by X%
public class EffectTimePercentageChange : IEffectWave
{

    //generic interface
    public TargetingType targetingType { get { return TargetingType.none; } }   //wave effects dont need a target
    public EffectType effectType { get { return EffectType.wave; } }            //this is a wave effect
    public float strength { get; set; }                                         //how strong this effect is.  (unused in this effect)
    public string argument { get; set; }                                        //effect argument (unused in this effect)

    //this effect
    public string Name { get { return "Increase wave time by " + strength + " percent."; } }    //returns name and strength
    public WaveData alteredWaveData(WaveData currentWaveData)
    {
        currentWaveData.time = currentWaveData.time * (1.0f + (strength / 100.0f));
        return currentWaveData;
    }

}