using UnityEngine;
using System.Collections;

//all effects in this file take place instantly with no particular target


//draws x cards
public class EffectDrawCard : IEffectInstant {

	//generic interface
	public TargetingType targetingType { get { return TargetingType.none; } }	//this effect doesnt need a target
	public EffectType effectType { get { return EffectType.instant; } }			//this is an instant effect
	public float strength { get; set; }											//how strong this effect is

	//this effect
	public string Name { get { return "Draw " + strength + " cards"; } }		//returns name and strength
	public void trigger() {
		for (uint i = 0; i < strength; i++)
			GameObject.FindGameObjectWithTag ("Hand").SendMessage ("drawCard");
	}

}