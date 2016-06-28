using UnityEngine;
using System.Collections;
using System.Xml.Serialization;
using System.Collections.Generic;

//represents an effect in XML
[System.Serializable]
public class XMLEffect : System.Object
{
    [XmlAttribute]
    public string name;
    [XmlAttribute]
    public float strength;
    [XmlAttribute]
    public string argument;
}

//represents everything needed to apply effects to an object
[System.Serializable]
public class EffectData : System.Object
{
    //list of effects from xml
    [XmlArray("Effects")]
    [XmlArrayItem("Effect")]
    public List<XMLEffect> XMLeffects = new List<XMLEffect>();
    [XmlIgnore]
    public bool effectsSpecified //hide effect list if it is empty
    {
        get { return XMLeffects.Count > 0; }
        set { }
    }

    [XmlIgnore]
    public List<IEffect> effects = new List<IEffect>(); //list of effect classes
    [XmlIgnore]
    public TargetingType targetingType
    {
        get
        {
            if (effects.Count == 0) parseEffects(); //make sure we have actual code references to the effects

            //return the target type of the first effect that requires a target.  no card can have effects that target two different types of things
            foreach (IEffect e in effects)
            {
                if (e.targetingType != TargetingType.none)
                    return e.targetingType;
            }
            return TargetingType.none; //if no effect needs a target, return none
        }
    }

    //translates the XMLeffects into code references
    public void parseEffects()
    {
        foreach (XMLEffect xe in XMLeffects)
            effects.Add(EffectTypeManagerScript.instance.parse(xe));
    }

}

//All effects in the game must implement one of these interfaces.  
//Most will not use Effect directly, but instead a derivitave such as EffectInstant or EffectWave

//different targeting types.
public enum TargetingType {
	none, 	//this effect does not require the player to select a target
	tower, 	//this effect requires the player to target a tower 
};

//different effect types
public enum EffectType {
	instant,
	wave,
    discard,
    self
};

//base interface
public interface IEffect {
	
	string Name { get; } 				//user-friendly name of this effect
	TargetingType targetingType { get; }//specifies what this card must target when casting, if anything
	EffectType effectType { get; }		//specifies what kind of effect this is
	float strength { get; set; }		//specifies how strong the effect is.  not used in every effect.
    string argument { get; set; }       //specifies any other information the effect requires.  not used in every effect.

}

//for instantaneous effects that happen once and then go away
public interface IEffectInstant : IEffect {

	void trigger(); //called when this effect triggers

};

//for effects that apply to enemy waves, altering their properties
public interface IEffectWave : IEffect {

	WaveData alteredWaveData (WaveData currentWaveData); //alters the current wave data and returns the new values

};

//for effects that apply on discard
public interface IEffectDiscard : IEffect
{

    bool trigger(ref Card c); //called when this effect triggers.  returns true if the card no longer needs discarding

}

//for effects that target the card itself
public interface IEffectSelf : IEffect
{

    void trigger(ref Card card, GameObject card_gameObject); //called when this effect triggers.

}

