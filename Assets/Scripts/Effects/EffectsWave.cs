using UnityEngine;
using System.Collections;

public class EffectBudgetPercentageChange : IEffectWave {

	//generic interface
	public TargetingType targetingType { get { return TargetingType.none; } }			//wave effects dont need a target
	public EffectType effectType { get { return EffectType.wave; } }					//this is a wave effect
	public float strength { get; set; }													//how sstrong this effect is

	//this effect
	public string Name { get { return "monster count: " + strength.ToString("P"); } }	//returns name and strength
	public WaveData alteredWaveData(WaveData currentWaveData) {
		currentWaveData.budget = Mathf.RoundToInt(currentWaveData.budget * (1.0f + strength));
		return currentWaveData;
	}
	
}
