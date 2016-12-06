using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;

//All effects in the game must implement one of these interfaces.
//However, they should not use IEffect directly, but instead a derivative such as IEffectInstant or IEffectWave

/// <summary>
/// The targeting type determines how an effect must be targeted when placed on a card
/// none:   does not require the player to select a target
/// tower:  requires the player to target a tower
/// noCast: does not support being cast (used for effects that are not meant to be applied to a spell)
/// </summary>
public enum TargetingType
{
    none, 
    tower,
    noCast,
};

/// <summary>
/// different effect types.  Each type is triggered under different circumstances  
/// property        : is never triggered.  instead, other code tests whether or not it exists on a given object and behave accordingly
/// enemyDamaged    : triggers when an enemy is damaged.  Could be attached to the attacking tower or the defending enemy
/// enemyReachedGoal: triggers when an enemy reaches their goal
/// instant         : triggers instantly without the need for a target
/// overcharge      : triggers when a tower attacks with at least one full point of overcharge, before enemyDamaged effects
/// periodic        : triggers on every update() call
/// self            : affects the card it is attached to (i.e.: to gain/lose charges when cast)
/// towerTargeting  : alters the way a tower targets enemies.  if multiple are present, only the last is actually used
/// wave            : alters the current wave
/// death           : triggers when the tower/enemy is destroyed
/// everyRound      : triggers once every round (uses IEffectInstant)
/// meta            : targets another effect.  These usually trigger in the same manner as their target
/// </summary>
public enum EffectType
{
    cardDrawn,
    death,
    enemyDamaged,
    enemyReachedGoal,
    enemySpawned,
    everyRound,
    instant,
    meta,
    overcharge,
    periodic,
    property,
    self,
    towerTargeting,
    wave,
};

/// <summary>
/// represents an effect in XML
/// </summary>
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

    /// <summary>
    /// attempts to retrieve a help string from 'available effects.txt' for display in the inspector
    /// if one could not be found, returns an error message instead
    /// </summary>
    private static Dictionary<string, string> cachedHelpStrings = null;
    [Show] public string usage
    {
        get
        {
            //if the dictionary is not built yet, build it
            if (cachedHelpStrings == null)
            {
                string helpFile = Path.Combine(Application.streamingAssetsPath, "XML/Documentation/available effects.txt");
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

/// <summary>
/// convenience struct that indicates which property effects are contained in an effectData
/// </summary>
public struct PropertyEffects
{
    public bool   armorPierce;
    public bool   infiniteTowerLifespan;
    public bool   returnsToTopOfDeck;
    public bool   manualFire;
    public bool   noUpgradeCost;
    public bool   upgradesForbidden;
    public Color? attackColor;
    public int?   limitedAmmo;
    public int?   maxOvercharge;
    public int?   dieRoll;
}

/// <summary>
/// provides a list of all effects on an object, and several utility functions to search it
/// </summary>
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

    /// <summary>
    /// read only list of effect objects
    /// </summary>
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

    /// <summary>
    /// adds the given effect to the list
    /// </summary>
    public void Add(IEffect e)
    {
        if (testForEffectRequirements(e))
        {
            Effects.Add(e); //add it

            //save reference to this container in the effect and all its children
            e.parentData = this;
            while (e.triggersAs(EffectType.meta))
            {
                e = ((IEffectMeta)e).innerEffect;
                e.parentData = this;
            }

            //reset the caches
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
                if (containsEffect("fixedSpawnCount") == false)
                {
                    MessageHandlerScript.Warning("<" + e.cardName + ">: units must have fixedSpawnCount in order to use budget scaling effects.  Skipping " + e.XMLName);
                    return false;
                }
                break;

            //die rolling effects should have a die roll
            case "ifRollRange":
                if (containsEffect("dieRoll") == false)
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

    /// <summary>
    /// searches the effect data, by XMLName, and returns whether or not it exists anywhere inside the effectData
    /// note that this will find it regardless of how many levels of meta effects it may be nested in
    /// </summary>
    /// <param name="XMLName">XMLName of the effect to locate</param>
    /// <returns></returns>
    public bool containsEffect (string XMLName)
    {
        foreach (IEffect ie in effects)
        {
            //while the effect we are looking at is a meta effect, check its child as well.  This is a loop because the amount of nesting is arbitrary
            IEffect examining = ie;
            while (examining.triggersAs(EffectType.meta))
            {
                if (examining.XMLName == XMLName)
                    return true;
                else
                    examining = ((IEffectMeta)examining).innerEffect;
            }

            if (examining.XMLName == XMLName) return true;
        }

        //we got through the whole list without finding it, so it is not present
        return false;
    }

    /// <summary>
    /// helper function that returns how the card containing these effects must be used.
    /// </summary>
    //the result is cached since it is needed regularly but changes rarely
    [XmlIgnore]
    public TargetingType cardTargetingType
    {
        get
        {
            if (cachedCardTargetingType != null)
                return cachedCardTargetingType.Value;

            if (effects.Count == 0) parseEffects(); //make sure we have actual code references to the effects

            //return the target type of the first effect that requires a target.  no card can have effects that target two different types of things
            foreach (IEffect e in effects)
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

    /// <summary>
    /// helper function that returns the targeting effect currently in use by the tower
    /// the XMLName of the actual effect used is also cached in lastUsedTargetingEffect for use by anything that wants to know how the targeting happened
    /// </summary>
    //the result is cached since it is needed regularly but changes rarely
    public List<EnemyScript> doTowerTargeting(Vector2 towerPosition, float towerRange)
    {
        //cache a list of targeting effects on this object.  each one is tested in turn, and the first that returns a non-null response has its result returned to the tower
        if (cachedTowerTargetingList == null)
        {
            cachedTowerTargetingList = new List<IEffectTowerTargeting>();
            cachedTowerTargetingList.Add(EffectTargetDefault.instance);

            foreach (IEffect e in effects)
                if (e.triggersAs(EffectType.towerTargeting))
                    cachedTowerTargetingList.Add( (IEffectTowerTargeting)e );

            cachedTowerTargetingList.Reverse();
        }

        //find the first targeting effect that returns an actual result
        List<EnemyScript> res = null;
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
            return new List<EnemyScript>();
        }
        else
        {
            return res;
        }
    }

    /// <summary>
    /// contains the XMLName of the targeting effect used by the last call to doTowerTargeting()
    /// </summary>
    public string lastUsedTargetingEffect { get; set; }

    /// <summary>
    /// helper that returns a struct containing information on all property effects in this set
    /// </summary>
    //results are cached to save performance
    [XmlIgnore]
    public PropertyEffects propertyEffects
    {
        set
        {
            //change effect list to account for any properties that have been altered.  Most properties have no reason to actually change, so changing them is not currently supported
            foreach (IEffect e in effects)
            {
                if (e.triggersAs(EffectType.property))
                {
                    //if this is a meta effect, find the child at the end of the chain and check that instead
                    IEffect examining = e;
                    while (examining.triggersAs(EffectType.meta))
                        examining = ((IEffectMeta)examining).innerEffect;

                    switch (examining.XMLName)
                    {
                        //properties that shouldnt change
                        case "armorPierce": if (value.armorPierce != propertyEffects.armorPierce) Debug.LogWarning("updating that property is not supported"); break;
                        case "attackColor": if (value.attackColor != propertyEffects.attackColor) Debug.LogWarning("updating that property is not supported"); break;
                        case "infiniteTowerLifespan": if (value.infiniteTowerLifespan != propertyEffects.infiniteTowerLifespan) Debug.LogWarning("updating that property is not supported"); break;
                        case "returnsToTopOfDeck": if (value.returnsToTopOfDeck != propertyEffects.infiniteTowerLifespan) Debug.LogWarning("updating that property is not supported"); break;
                        case "manualFire": if (value.manualFire != propertyEffects.manualFire) Debug.LogWarning("updating that property is not supported"); break;
                        case "maximumOvercharge": if (value.maxOvercharge != propertyEffects.maxOvercharge) Debug.LogWarning("updating that property is not supported"); break;
                        case "noUpgradeCost": if (value.noUpgradeCost != propertyEffects.noUpgradeCost) Debug.LogWarning("updating that property is not supported"); break;
                        case "upgradesForbidden": if (value.upgradesForbidden != propertyEffects.upgradesForbidden) Debug.LogWarning("updating that property is not supported"); break;


                        //if the dieRoll changed, we can just reset the cache.  EffectDieRoll updates itself when the roll happens
                        case "dieRoll":
                            if (value.dieRoll != propertyEffects.dieRoll)
                                resetCachedValues();
                            break; 

                        //Ammo can be updated by towers.  In that case, we must store the new value in the effect
                        case "limitedAmmo":
                            if (value.limitedAmmo != propertyEffects.limitedAmmo)
                            {
                                e.strength = value.limitedAmmo.Value; //update effect

                                //and clear the caches since things changed
                                cachedPropertyEffects = null;
                                cachedPeriodicEffectList = null;
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
                        //the property effect may be behind meta effects, so if e is a meta effect then check its child
                        //do not check children if that meta effect says it should not trigger the child
                        IEffect examining = e;
                        while (examining.triggersAs(EffectType.meta))
                        {
                            if (((IEffectMeta)examining).shouldApplyInnerEffect())
                            {
                                examining = ((IEffectMeta)examining).innerEffect;
                            }
                            else
                            {
                                //since the child should not trigger, we know we don't want to modify the effects from it, but we're in a nested loop, so we cant skip over with continue
                                //instead, we set examining to EffectDoNothing, since that won't do anything anyway
                                examining = EffectDoNothing.instance; 
                            }
                        }

                        switch (examining.XMLName)
                        {
                            case "attackColor":
                                Color temp;
                                bool successful = ColorUtility.TryParseHtmlString(examining.argument, out temp);
                                if (successful)
                                    newPropertyEffects.attackColor = temp;
                                else
                                    Debug.LogWarning("Could not convert " + examining.argument + " to a color");
                                break;

                            case "armorPierce": newPropertyEffects.armorPierce = true; break;
                            case "infiniteTowerLifespan": newPropertyEffects.infiniteTowerLifespan = true; break;
                            case "limitedAmmo": newPropertyEffects.limitedAmmo = Mathf.RoundToInt(examining.strength); break;
                            case "manualFire": newPropertyEffects.manualFire = true; break;
                            case "maxOvercharge": newPropertyEffects.maxOvercharge = Mathf.FloorToInt(examining.strength); break;
                            case "noUpgradeCost": newPropertyEffects.noUpgradeCost = true; break;
                            case "returnsToTopOfDeck": newPropertyEffects.returnsToTopOfDeck = true; break;
                            case "upgradesForbidden": newPropertyEffects.upgradesForbidden = true; break;

                            case "dieRoll": newPropertyEffects.dieRoll = System.Convert.ToInt32(examining.argument); break;

                            default: Debug.LogWarning("Unknown property effect."); break;
                        }
                    }
                }
                cachedPropertyEffects = newPropertyEffects;
            }

            return cachedPropertyEffects.Value;
        }
    }

    /// <summary>
    /// helper function that updates all periodic effects on an enemy
    /// </summary>
    /// <param name="enemy">enemy to update</param>
    /// <param name="deltaTime">time passed since last update</param>
    //the list of periodic effects is cached since it is used every frame but changes very rarely
    public void triggerAllPeriodicEnemy (EnemyScript enemy, float deltaTime)
    {
        if (cachedPeriodicEffectList == null)
        {
            cachedPeriodicEffectList = new List<IEffectPeriodic>();
            foreach (IEffect e in effects)
                if (e.triggersAs(EffectType.periodic))
                    cachedPeriodicEffectList.Add((IEffectPeriodic)e);
        }

        foreach (IEffectPeriodic e in cachedPeriodicEffectList)
            e.UpdateEnemy(enemy, deltaTime);
    }

    /// <summary>
    /// translates the XMLeffects into code references, if they aren't already
    /// </summary>
    /// <param name="cardName">optional name to provide the new effects for logging purposes</param>
    public void parseEffects(string cardName = "<UNKNOWN_CARD>")
    {
        foreach (XMLEffect xe in XMLEffects)
        {
            IEffect ie = EffectTypeManagerScript.instance.parse(xe, cardName);
            if (ie != null)
                Add(ie);
        }
    }

    /// <summary>
    /// clones this EffectData to a new object
    /// </summary>
    public EffectData clone()
    {
        EffectData clone = new EffectData();
        clone.XMLEffects = XMLEffects; //copy the list of xml effects over so we dont lose it

        //parse our own effects if needed
        if (effects.Count > XMLEffects.Count)
            parseEffects();

        //clone the effects also
        foreach (IEffect ie in effects)
            clone.Add(cloneEffect(ie));

        return clone;
    }

    /// <summary>
    /// clones an individual IEffect without needing to know its type
    /// </summary>
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
            ((BaseEffectMeta)result).innerEffect = ((BaseEffectMeta)original).cloneInnerEffect();

        return result;
    }

    /// <summary>
    /// removes any effects that have no chance of triggering again and can be destroyed safely
    /// </summary>
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

    EffectData parentData { get; set; } //contains a reference to the parent effectData

    bool shouldBeRemoved(); //returns true if this effect is no longer necessary and can be removed
    bool triggersAs(EffectType triggerType); //returns true if this effect triggers as an effect of this type (usually only true if it IS an effect of that type, but there are exceptions)
}

//base effect class
public abstract class BaseEffect : IEffect
{
    [Hide] public string cardName { get; set; } //name of the card containing this effect

    [Show] public virtual float strength { get; set; } //specifies how strong the effect is.  not used in every effect.
    [Show] public virtual string argument { get; set; } //specifies any other information the effect requires.  not used in every effect.
    [Hide] public virtual string effectColorHex { get { return effectType.ToString("X"); } } //returns the hex color of this effect by extracting it from the enum value

    [Hide] public abstract TargetingType targetingType { get; } //specifies what this card must target when casting, if anything
    [Hide] public abstract EffectType effectType { get; } //specifies what kind of effect this is

    [Hide] public abstract string Name { get; } //user-friendly name of this effect
    [Show] public abstract string XMLName { get; } //name used to refer to this effect in XML.  See also: EffectTypeManagerScript.parse()

    //contains a reference to the parent effectData
    [Hide] public EffectData parentData {get; set;}

    //returns true if this effect is no longer necessary and can be removed
    public virtual bool shouldBeRemoved() { return false; } 

    //returns true if this effect triggers as an effect of this type (usually only true if it IS an effect of that type, but there are exceptions)
    public virtual bool triggersAs(EffectType triggerType) { return triggerType == effectType; } 
}

//effect triggers when a card is drawn
public interface IEffectCardDrawn : IEffect
{
    void playerCardDrawn(CardScript playerCard);
    void enemyCardDrawn(EnemyScript enemyCard);
};

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

//effect triggers when an enemy is spawned.  applies to the enemy in question, and triggered individually for each unit.  If the enemy survives, the effect IS NOT triggered again.
public interface IEffectOnEnemySpawned
{
    void onEnemySpawned(EnemyScript enemy);
}

//effect affects the card it is attached to (i.e.: to gain/lose charges when cast)
public interface IEffectSelf : IEffect
{
    void trigger(ref PlayerCard card, GameObject card_gameObject); //called when this effect triggers.
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

//effect alters the way a tower targets enemies.  if multiple are present, only the last is actually used
public interface IEffectTowerTargeting : IEffect
{
    List<EnemyScript> findTargets(Vector2 towerPosition, float towerRange);
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

//effect targets another effect.  As such, it must implement interfaces for ALL other effect types, since we dont know what kind the child may be.
//we also provide a way to check what the inner effect is, and whether it would be applied if this one is triggered
public interface IEffectMeta : IEffect, IEffectEnemyDamaged, IEffectEnemyReachedGoal, IEffectInstant, IEffectOvercharge, IEffectPeriodic, IEffectProperty, IEffectSelf, IEffectTowerTargeting, IEffectWave, IEffectDeath, IEffectOnEnemySpawned, IEffectCardDrawn
{
    IEffect innerEffect { get; set; } //effect targeted by this metaEffect
    bool shouldApplyInnerEffect(); //returns whether or not the innerIeffect would trigger if this effect is
}

//effect is triggered when a tower or enemy dies
public interface IEffectDeath : IEffect
{
    void onEnemyDeath(EnemyScript e);
    void onTowerDeath(TowerScript t);
}