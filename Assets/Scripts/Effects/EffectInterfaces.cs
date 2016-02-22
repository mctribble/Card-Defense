using UnityEngine;
using System.Collections;

//All effects in the game must implement one of these interfaces.  
//Most will not use Effect directly, but instead a derivitave such as EffectInstant or EffectWave

//different targeting types
public enum TargetingType {
	none, 	//this effect does not require a target
	tower 	//this effect targets a tower
};

//different effect types
public enum EffectType {
	instant,
	wave
};

//base interface
public interface IEffect {
	
	string Name { get; } 				//user-friendly name of this effect
	TargetingType targetingType { get; }//specifies what this card must target when casting, if anything
	EffectType effectType { get; }		//specifies what kind of effect this is
	float strength { get; set; }		//specifies how strong the effect is

}

//for instantaneous effects that happen once and then go away
public interface IEffectInstant : IEffect {

	void trigger(); //called when this effect triggers

};

//for effects that apply to enemy waves, altering their properties
public interface IEffectWave : IEffect {

	WaveData alteredWaveData (WaveData currentWaveData); //alters the current wave data and returns the new values

};

