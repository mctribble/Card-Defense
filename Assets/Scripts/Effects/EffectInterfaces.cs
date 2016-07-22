using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;

//quick note on VFW attributes, since effects use them fairly heavily: 
//[Hide]: prevents field from appearing in unity inspector
//[Show]: forces field to show in unity inspector
//[Display(x)]: alters display order in the inspector
//Usually you dont need these, since VFW is good at figuring it out on its own, but I found myself picky on this point for effects so I specify by hand

//represents an effect in XML
[System.Serializable]
public class XMLEffect : System.Object
{
    [XmlAttribute] public string name;
    [XmlAttribute] public float strength;
    [XmlAttribute] public string argument;
}

//represents everything needed to apply effects to an object
[System.Serializable]
public class EffectData : System.Object
{
    //list of effects from xml
    [XmlArray("Effects")]
    [XmlArrayItem("Effect")]
    public List<XMLEffect> XMLEffects = new List<XMLEffect>();

    [XmlIgnore]
    public bool effectsSpecified //hide effect list if it is empty
    {
        get { return XMLEffects.Count > 0; }
        set { }
    }

    [XmlIgnore] public List<IEffect> effects = new List<IEffect>(); //list of effect objects

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
        foreach (XMLEffect xe in XMLEffects)
        {
            IEffect ie = EffectTypeManagerScript.instance.parse(xe);
            if (ie != null)
                effects.Add(ie);
        }
    }

    //clones this EffectData to a new object
    public EffectData clone()
    {
        //we use the same list of XMLEffects, but the clone parses them again to get its own set of effects
        //this way, changes in one enemy (e.g. armor reduction) dont propogate to all enemies of this type
        EffectData clone = new EffectData();
        clone.XMLEffects = XMLEffects;
        clone.parseEffects();
        return clone;
    }
}

//All effects in the game must implement one of these interfaces.
//Most will not use Effect directly, but instead a derivitave such as EffectInstant or EffectWave

//different targeting types.
public enum TargetingType
{
    none,   //this effect does not require the player to select a target
    tower, 	//this effect requires the player to target a tower
    noCast  //this effect does not support being cast (used for effects that are not meant to be applied to a spell)
};

//different effect types.  The values are hex colors to be used for text mentioning these effects (format RRGGBBAA).  unchecked syntax is to force stuffing the values into the signed int used by enums
public enum EffectType
{
    discard          = unchecked((int)0xA52A2AFF), //effect triggers when the card it is attached to is discarded
    enemyDamaged     = unchecked((int)0x008000FF), //effect triggers when an enemy is damaged.  Could be attached to the attacking tower or the defending enemy
    enemyReachedGoal = unchecked((int)0x111111FF), //effect triggers when an enemy reaches their goal
    instant          = unchecked((int)0x00FFFFFF), //effect triggers instantly without the need for a target
    self             = unchecked((int)0x0000A0FF), //effect affects the card it is attached to (i.e.: to gain/lose charges when cast)
    towerTargeting   = unchecked((int)0xADD8E6FF), //effect alters the way a tower taragets enemies.  if multiple are present, only the last is actually used
    wave             = unchecked((int)0x0000FFFF)  //effect alters the current wave
};

//base interface
public interface IEffect
{
    string        Name          { get; } 	  //user-friendly name of this effect
    string        XMLName       { get; }      //name used to refer to this effect in XML.  See also: EffectTypeManagerScript.parse()
    TargetingType targetingType { get; }      //specifies what this card must target when casting, if anything
    EffectType    effectType    { get; }      //specifies what kind of effect this is
    float         strength      { get; set; } //specifies how strong the effect is.  not used in every effect.
    string        argument      { get; set; } //specifies any other information the effect requires.  not used in every effect.
}

//effect triggers instantly without the need for a target
public interface IEffectInstant : IEffect
{
    void trigger(); //called when this effect triggers
};

//effect alters the current wave
public interface IEffectWave : IEffect
{
    WaveData alteredWaveData(WaveData currentWaveData); //alters the current wave data and returns the new values
};

//effect triggers when the card it is attached to is discarded
public interface IEffectDiscard : IEffect
{
    bool trigger(ref Card c); //called when this effect triggers.  returns true if the card no longer needs discarding
}

//effect affects the card it is attached to (i.e.: to gain/lose charges when cast)
public interface IEffectSelf : IEffect
{
    void trigger(ref Card card, GameObject card_gameObject); //called when this effect triggers.
}

//effect triggers when an enemy is damaged.  Could be attached to the attacking tower or the defending enemy
public interface IEffectEnemyDamaged : IEffect
{
    void expectedDamage(ref DamageEventData d); //called when it is expected to deal damage (so targeting etc. can account for damage amount changes)

    void actualDamage(ref DamageEventData d); //called when damage actually happens (for things that should happen when the actual hit occurs)
}

public interface IEffectEnemyReachedGoal : IEffect
{
    void trigger(EnemyScript enemy);
}

//effect alters the way a tower taragets enemies.  if multiple are present, only the last is actually used
public interface IEffectTowerTargeting : IEffect
{
    List<GameObject> findTargets(Vector2 towerPosition, float towerRange);
}