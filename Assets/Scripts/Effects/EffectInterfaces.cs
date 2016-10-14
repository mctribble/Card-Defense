using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;

//quick note on VFW attributes, since effects use them fairly heavily: 
//[Hide]: prevents field from appearing in unity inspector
//[Show]: forces field to show in unity inspector
//[Display(x)]: alters display order in the inspector
//Usually you dont need these, since VFW is good at figuring it out on its own, but I found myself picky on this point for effects so I specify by hand

//All effects in the game must implement one of these interfaces.
//However, they should not use IEffect directly, but instead a derivitave such as IEffectInstant or IEffectWave

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
    property         = unchecked((int)0xA52A2AFF), //effect is never triggered.  instead, other code tests whether or not it exists on a given object and behave accordingly
    enemyDamaged     = unchecked((int)0x008000FF), //effect triggers when an enemy is damaged.  Could be attached to the attacking tower or the defending enemy
    enemyReachedGoal = unchecked((int)0x111111FF), //effect triggers when an enemy reaches their goal
    instant          = unchecked((int)0x00FFFFFF), //effect triggers instantly without the need for a target
    overcharge       = unchecked((int)0xFF00FFFF), //effect triggers when a tower attacks with at least one full point of overcharge, before enemyDamaged effects
    periodic         = unchecked((int)0x333333FF), //effect triggers on every update() call
    self             = unchecked((int)0x0000A0FF), //effect affects the card it is attached to (i.e.: to gain/lose charges when cast)
    towerTargeting   = unchecked((int)0xADD8E6FF), //effect alters the way a tower taragets enemies.  if multiple are present, only the last is actually used
    wave             = unchecked((int)0x0000FFFF), //effect alters the current wave
    death            = unchecked((int)0xFF0000FF), //effect triggers when the tower/enemy is destroyed
    everyRound       = unchecked((int)0x00FF00FF), //effect triggers once every round (uses IEffectInstant)
    meta             = unchecked((int)0x000000FF)  //effect targets another effect.  These usually trigger in the same manner as their target
};

//represents an effect in XML
[System.Serializable]
public class XMLEffect : System.Object
{
    private string[] getXMLNames() { return EffectTypeManagerScript.instance.listEffectXMLNames(); } //used to provide a popup list of effect options in the inspector
    [XmlAttribute][Popup("getXMLNames",CaseSensitive = true, Filter = true, HideUpdate = true, TextField = true)] public string name;

    //effect params
    [XmlAttribute] public float  strength;
    [XmlAttribute] public string argument;

    //only write strength/argument if they contain an actual value
    [XmlIgnore] public bool strengthSpecified { get { return strength != 0.0f; } set { } }
    [XmlIgnore] public bool argumentSpecified { get { return (argument != null) && (argument != ""); } set { } }

    [XmlElement("Effect")]
    public XMLEffect innerEffect;

    //attempts to retrieve a help string from 'available effects.txt' for display in the inspector
    private static Dictionary<string, string> cachedHelpStrings = null;
    [Show] public string usage
    {
        get
        {
            //if the dictionary is not built yet, build it
            if (cachedHelpStrings == null)
            {
                string helpFile = Path.Combine(Application.dataPath, "StreamingAssets/XML/Documentation/available effects.txt");
                cachedHelpStrings = new Dictionary<string, string>();
                try
                {
                    // Open the text file using a stream reader.
                    using (StreamReader sr = new StreamReader(helpFile))
                    {
                        //read the contents
                        while (sr.EndOfStream == false)
                        {
                            //read a line from the file...
                            string line = sr.ReadLine();

                            //skip it if it is empty...
                            if (line.Length == 0)
                                continue;

                            //or starts with <...
                            if (line.StartsWith("<"))
                                continue;

                            //this line should be in the format effectName | helpString.  retrieve values
                            string[] parts = line.Split('|');

                            //if we dont have two strings, then the line was formatted wrong.  bail.
                            if (parts.Length != 2)
                                return "could not parse line: " + line;

                            //we do have two strings.  Trim leading/trailing spaces from them and use them as keys and values, respectively
                            cachedHelpStrings.Add(parts[0].Trim(), parts[1].Trim());
                        }
                    }
                }
                catch (System.Exception e)
                {
                    return "Could not open " + helpFile + "(" + e.Message + ")";
                }
            }

            //now retrieve the string in question from the dictionary
            string helpString;
            bool helpStringFound = cachedHelpStrings.TryGetValue(name, out helpString);

            if (helpStringFound)
                return helpString;
            else
                return "could not find help for " + name;
        }
    }

    public override string ToString()
    {
        string result = name;

        if (strength != 0.0f || ((argument != null) && (argument != "")))
        {
            result += "(";

            if (strength != 0.0f)
                result += "X: " + strength;

            if ((strength != 0.0f) && (argument != null) && (argument != ""))
                result += ", ";

            if (argument != null)
                if (argument != "")
                    result += "Y: " + argument;

                result += ")";
        }

        if (innerEffect != null)
            result += "{" + innerEffect.ToString() + "}";

        //result += " <" + getHelpString() + ">";

        return result;
    }
}

//convenience struct that indicates which property effects are contained in this effectData
public struct PropertyEffects
{
    public bool   armorPierce;
    public bool   infiniteTowerLifespan;
    public bool   returnsToTopOfDeck;
    public bool   manualFire;
    public bool   upgradesForbidden;
    public Color? attackColor;
    public int?   limitedAmmo;
    public int?   maxOvercharge;
}

//represents everything needed to apply effects to an object
[System.Serializable]
public class EffectData : System.Object
{
    //list of effects from xml
    [XmlArray("Effects")]
    [XmlArrayItem("Effect")]
    [Display(Seq.GuiBox | Seq.PerItemRemove)]
    public List<XMLEffect> XMLEffects = new List<XMLEffect>();

    //only write effect data if there is data to write
    [XmlIgnore] public bool XMLEffectsSpecified { get { return (XMLEffects != null) && (XMLEffects.Count > 0); } set { } }

    [XmlIgnore] [Hide]
    public bool effectsSpecified //hide effect list if it is empty
    {
        get { return XMLEffects.Count > 0; }
        set { }
    }

    //cached values for utility functions
    //the '?' on some of these is shorthand for Nullable<T> (https://msdn.microsoft.com/en-us/library/1t3y8s4s.aspx)
    [XmlIgnore] private TargetingType?              cachedCardTargetingType; 
    [XmlIgnore] private List<IEffectTowerTargeting> cachedTowerTargetingList;
    [XmlIgnore] private List<IEffectPeriodic>       cachedPeriodicEffectList;
    [XmlIgnore] private PropertyEffects?            cachedPropertyEffects;

    //list of effect objects
    [XmlIgnore][Show] private List<IEffect> Effects = new List<IEffect>(); 
    [XmlIgnore]
    public ReadOnlyCollection<IEffect> effects
    {
        get
        {
            if (Effects.Count == 0 && XMLEffects.Count > 0)
                parseEffects();

            return Effects.AsReadOnly();
        }
    }

    //adds the given effect to the list, clearing cached values so that future calls return correctly
    public void Add(IEffect e)
    {
        if (testForEffectRequirements(e))
        {
            Effects.Add(e);
            resetCachedValues();
        }
    }

    //verification step: called when an effect is added to see if everything it needs is present
    private bool testForEffectRequirements(IEffect e)
    {
        switch (e.XMLName)
        {
            //budget scaling effects should have fixedSpawnCount
            case "scaleAttackhWithBudget":
            case "scaleEffectWithBudget":
            case "scaleHealthWithBudget":
            case "scaleSpeedWithBudget":
                if (effects.Any(x => x.XMLName == "fixedSpawnCount") == false)
                {
                    MessageHandlerScript.Warning("<" + e.cardName + ">: units must have fixedSpawnCount in order to use budget scaling effects.  Skipping " + e.XMLName);
                    return false;
                }
                break;

            //die rolling effects should have a die roll
            case "ifRollRange":
                if (effects.Any(x => x.XMLName == "dieRoll") == false)
                {
                    MessageHandlerScript.Warning("<" + e.cardName + ">: units must have dieRoll in order to use roll effects.  Skipping " + e.XMLName);
                    return false;
                }
                break;
        }
        return true;
    }

    //resets cached results so they will be recalculated if something asks for them
    private void resetCachedValues()
    {
        cachedCardTargetingType  = null;
        cachedTowerTargetingList = null;
        cachedPeriodicEffectList = null;
        cachedPropertyEffects    = null;
        lastUsedTargetingEffect  = null;
    }

    //helper function that returns how the card containing these effects must be used.
    //the result is cached since it is needed regularly but changes rarely
    [XmlIgnore]
    public TargetingType cardTargetingType
    {
        get
        {
            if (cachedCardTargetingType != null)
                return cachedCardTargetingType.Value;

            if (Effects.Count == 0) parseEffects(); //make sure we have actual code references to the effects

            //return the target type of the first effect that requires a target.  no card can have effects that target two different types of things
            foreach (IEffect e in Effects)
            {
                if (e.targetingType != TargetingType.none)
                {
                    cachedCardTargetingType = e.targetingType;
                    return e.targetingType;
                }
            }

            cachedCardTargetingType = TargetingType.none;
            return TargetingType.none; //if no effect needs a target, return none
        }
    }

    //helper function that returns the targeting effect currently in use by the tower
    //the result is cached since it is needed regularly but changes rarely
    //the XMLName of the actual effect used is cached for use by anything that wants to know how the targeting happened
    public string lastUsedTargetingEffect { get; set; }
    public List<GameObject> doTowerTargeting(Vector2 towerPosition, float towerRange)
    {
        //cache a list of targeting effects on this object.  each one is tested in turn, and the first that returns a non-null response has its result returned to the tower
        if (cachedTowerTargetingList == null)
        {
            cachedTowerTargetingList = new List<IEffectTowerTargeting>();
            cachedTowerTargetingList.Add(EffectTargetDefault.instance);

            foreach (IEffect e in Effects)
                if (e.triggersAs(EffectType.towerTargeting))
                    cachedTowerTargetingList.Add( (IEffectTowerTargeting)e );

            cachedTowerTargetingList.Reverse();
        }

        //find the first targeting effect that returns an actual result
        List<GameObject> res = null;
        foreach (IEffectTowerTargeting ie in cachedTowerTargetingList)
        {
            res = ie.findTargets(towerPosition, towerRange);

            if (res == null)
            {
                continue;
            }
            else
            {
                //found a targeting effect that works.  Cache the XMLName and leave the loop
                IEffect lastUsed = ie;
                while (lastUsed.triggersAs(EffectType.meta))
                    lastUsed = ((IEffectMeta)lastUsed).innerEffect;

                lastUsedTargetingEffect = lastUsed.XMLName;
                break;
            }
        }

        if (res == null)
        {
            MessageHandlerScript.Error("no targeting effect, not even the default, provided a result list!");
            return new List<GameObject>();
        }
        else
        {
            return res;
        }
    }

    //helper that returns a struct containing information on all property effects in this set
    //results are cached to save performance
    [XmlIgnore]
    public PropertyEffects propertyEffects
    {
        set
        {
            //change effect list to account for any properties that have been altered.  Most properties have no reason to actually change, so changing them is not currently supported
            foreach (IEffect e in Effects)
            {
                if (e.triggersAs(EffectType.property))
                {
                    switch (e.XMLName)
                    {
                        case "armorPierce": if (value.armorPierce != propertyEffects.armorPierce) Debug.LogWarning("updating that property is not supported"); break;
                        case "attackColor": if (value.attackColor != propertyEffects.attackColor) Debug.LogWarning("updating that property is not supported"); break;
                        case "infiniteTowerLifespan": if (value.infiniteTowerLifespan != propertyEffects.infiniteTowerLifespan) Debug.LogWarning("updating that property is not supported"); break;
                        case "returnsToTopOfDeck": if (value.returnsToTopOfDeck != propertyEffects.infiniteTowerLifespan) Debug.LogWarning("updating that property is not supported"); break;
                        case "manualFire": if (value.manualFire != propertyEffects.manualFire) Debug.LogWarning("updating that property is not supported"); break;
                        case "maximumOvercharge": if (value.maxOvercharge != propertyEffects.maxOvercharge) Debug.LogWarning("updating that property is not supported"); break;
                        case "upgradesForbidden": if (value.upgradesForbidden != propertyEffects.upgradesForbidden) Debug.LogWarning("updating that property is not supported"); break;

                        case "limitedAmmo":
                            if (value.limitedAmmo != propertyEffects.limitedAmmo)
                            {
                                e.strength = value.limitedAmmo.Value; //update effect
                                cachedPropertyEffects = null; //and force recalculating the property effects on the next get call
                            }
                            break;

                        default: Debug.LogWarning("propertyEffects set does not recognize " + e.XMLName); break;
                    }

                }
            }
        }
        get
        {
            if (cachedPropertyEffects == null)
            {
                PropertyEffects newPropertyEffects = new PropertyEffects();
                foreach (IEffect e in effects)
                {
                    if (e.triggersAs(EffectType.property))
                    {
                        switch (e.XMLName)
                        {
                            case "attackColor":
                                Color temp;
                                bool successful = ColorUtility.TryParseHtmlString(e.argument, out temp);
                                if (successful)
                                    newPropertyEffects.attackColor = temp;
                                else
                                    Debug.LogWarning("Could not convert " + e.argument + " to a color");
                                break;

                            case "armorPierce": newPropertyEffects.armorPierce = true; break;
                            case "infiniteTowerLifespan": newPropertyEffects.infiniteTowerLifespan = true; break;
                            case "limitedAmmo": newPropertyEffects.limitedAmmo = Mathf.RoundToInt(e.strength); break;
                            case "manualFire": newPropertyEffects.manualFire = true; break;
                            case "maxOvercharge": newPropertyEffects.maxOvercharge = Mathf.FloorToInt(e.strength); break;
                            case "returnsToTopOfDeck": newPropertyEffects.returnsToTopOfDeck = true; break;
                            case "upgradesForbidden": newPropertyEffects.upgradesForbidden = true; break;
                            default: Debug.LogWarning("Unknown property effect."); break;
                        }
                    }
                }
                cachedPropertyEffects = newPropertyEffects;
            }

            return cachedPropertyEffects.Value;
        }
    }

    //helper function that updates all periodic effects on an enemy
    //the list of periodic effects is cached since it is used every frame but changes very rarely
    public void triggerAllPeriodicEnemy (EnemyScript enemy, float deltaTime)
    {
        if (cachedPeriodicEffectList == null)
        {
            cachedPeriodicEffectList = new List<IEffectPeriodic>();
            foreach (IEffect e in Effects)
                if (e.triggersAs(EffectType.periodic))
                    cachedPeriodicEffectList.Add((IEffectPeriodic)e);
        }

        foreach (IEffectPeriodic e in cachedPeriodicEffectList)
            e.UpdateEnemy(enemy, deltaTime);
    }

    //translates the XMLeffects into code references
    public void parseEffects(string cardName = "<UNKNOWN_CARD>")
    {
        foreach (XMLEffect xe in XMLEffects)
        {
            IEffect ie = EffectTypeManagerScript.instance.parse(xe, cardName);
            if (ie != null)
                Effects.Add(ie);
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

    //clones an individual IEffect without needing to know its type
    public static IEffect cloneEffect(IEffect original)
    {
        //make a new XMLEffect from the original effect, parse it to get an effect object of the proper type, then return that
        XMLEffect temp = new XMLEffect();
        temp.name      = original.XMLName;
        temp.strength  = original.strength;
        temp.argument  = original.argument;

        IEffect result = EffectTypeManagerScript.instance.parse(temp, original.cardName);
        Debug.Assert(result != null);

        //special handling of meta effects: clone the inner effect as well
        if (original.triggersAs(EffectType.meta))
            ((BaseEffectMeta)result).innerEffect = cloneEffect(((BaseEffectMeta)original).innerEffect);

        return result;
    }

    //cleans anything unnecessary out of the effect list 
    public void cleanEffects()
    {
        Effects.RemoveAll(ie => ie.shouldBeRemoved());
    }

    //provides a short string for the debugger
    public override string ToString()
    {
        if (effects.Count == 0)
            return "no effects.";
        else if (effects.Count == 1)
            return effects[0].ToString();
        else
            return effects.Count + " effects.";
    }
}

//base interface
public interface IEffect
{
    string        cardName      { get; set; } //name of the card containing this effect
    float         strength      { get; set; } //specifies how strong the effect is.  not used in every effect.
    string        argument      { get; set; } //specifies any other information the effect requires.  not used in every effect.

    string        Name            { get; } //user-friendly name of this effect
    string        XMLName         { get; } //name used to refer to this effect in XML.  See also: EffectTypeManagerScript.parse()
    TargetingType targetingType   { get; } //specifies what this card must target when casting, if anything
    string        effectColorHex  { get; } //hex string that gives the color that should be used for this effect

    bool shouldBeRemoved(); //returns true if this effect is no longer necessary and can be removed
    bool triggersAs(EffectType triggerType); //returns true if this effect triggers as an effect of this type (usually only true if it IS an effect of that type, but there are exceptions)
}

//base effect class
public abstract class BaseEffect : IEffect
{
    [Hide] public string cardName { get; set; } //name of the card containing this effect

    [Show] public virtual float  strength { get; set; } //specifies how strong the effect is.  not used in every effect.
    [Show] public virtual string argument { get; set; } //specifies any other information the effect requires.  not used in every effect.
    [Hide] public virtual string effectColorHex { get { return effectType.ToString("X"); } } //returns the hex color of this effect by extracting it from the enum value

    [Hide] public abstract TargetingType targetingType { get; } //specifies what this card must target when casting, if anything
    [Hide] public abstract EffectType    effectType    { get; } //specifies what kind of effect this is

    [Hide] public  abstract string Name    { get; }                 //user-friendly name of this effect
    [Show] public  abstract string XMLName { get; }                 //name used to refer to this effect in XML.  See also: EffectTypeManagerScript.parse()

    //returns true if this effect is no longer necessary and can be removed
    public virtual bool shouldBeRemoved() { return false; } 

    //returns true if this effect triggers as an effect of this type (usually only true if it IS an effect of that type, but there are exceptions)
    public virtual bool triggersAs(EffectType triggerType) { return triggerType == effectType; } 
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

//effect is never triggered or called at all.  Used for effects that dont do anything directly but need to be checked for by other code
//examples:
//EffectReturnsToTopOfDeck doesnt do anything.  CardScript just checks whether or not it exists when a card is played and sends it to the top or bottom accordingly
//IgnoreLifespan causes a tower to never decay.  This does something, but does not make sense to "trigger" in any sense.  TowerScript just behaves differently if it is present
public interface IEffectProperty : IEffect
{

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

//effect triggers on every update
public interface IEffectPeriodic : IEffect
{
    void UpdateEnemy(EnemyScript e, float deltaTime);
}

//effect triggers when a tower fires and has at least one point of overcharge
//these effects alter the given damage event and take effect before enemyDamaged effects
public interface IEffectOvercharge : IEffect
{
    void trigger(ref DamageEventData d, int pointsOfOvercharge);
}

//effect targets another effect
public interface IEffectMeta : IEffect, IEffectEnemyDamaged, IEffectEnemyReachedGoal, IEffectInstant, IEffectOvercharge, IEffectPeriodic, IEffectProperty, IEffectSelf, IEffectTowerTargeting, IEffectWave, IEffectDeath
{
    IEffect innerEffect { get; set; } //effect targeted by this metaEffect
}

//effect is triggered when a tower or enemy dies
public interface IEffectDeath : IEffect
{
    void onEnemyDeath(EnemyScript e);
    void onTowerDeath(TowerScript t);
}