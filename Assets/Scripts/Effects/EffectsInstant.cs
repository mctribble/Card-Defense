using UnityEngine;
using System.Collections;

public class EffectDrawCard : IEffectInstant {

	//generic interface
	public TargetingType targetingType { get { return TargetingType.none; } }	//wave effects dont need a target
	public EffectType effectType { get { return EffectType.instant; } }			//this is a wave effect
	public float strength { get; set; }											//how sstrong this effect is

	//this effect
	public string Name { get { return "Draw " + strength + " cards"; } }		//returns name and strength
	public void trigger() {
		for (uint i = 0; i < strength; i++)
			GameObject.FindGameObjectWithTag ("Hand").SendMessage ("drawCard");
	}

}