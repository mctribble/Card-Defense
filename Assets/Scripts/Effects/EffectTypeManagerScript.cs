using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vexe.Runtime.Types;

//this class is responsible for taking XMLEffects and returning an IEffect to match
public class EffectTypeManagerScript : BaseBehaviour
{
    //singleton instance
    [Hide] public static EffectTypeManagerScript instance;

    // Use this for initialization
    private void Awake()
    {
        instance = this;
    }

    //instantiates and initializes an effect object from the xmlEffect.  returns null if that effect doesnt exist
    //the card name is simply passed unaltered to the new effect
    //TODO: find a cleaner way to implement this?  Code reflection is an option but may be insecure
    public IEffect parse(XMLEffect xe, string cardName)
    {
        IEffect ie;
        switch (xe.name)
        {
            //<<Enemy Effects (Periodic)>>
            case "poison":                    ie = new EffectPoison(); break;
            case "regeneration":              ie = new EffectRegeneration(); break;

            //<<Enemy Effects (Reached Goal)>>
            case "damageHand":                ie = new EffectDamageHand(); break;

            //<<Enemy Effects (Stat Scaling)>>
            case "fixedSpawnCount":           ie = new EffectFixedSpawnCount(); break;
            case "invScaleAttackWithHealth":  ie = new EffectInvScaleAttackWithHealth(); break;
            case "invScaleEffectWithDamage":  ie = new EffectInvScaleEffectWithDamage(); break;
            case "invScaleEffectWithTime":    ie = new EffectInvScaleEffectWithTime(); break;
            case "invScaleSpeedWithDamage":   ie = new EffectInvScaleSpeedWithDamage(); break;
            case "invScaleSpeedWithTime":     ie = new EffectInvScaleSpeedWithTime(); break;
            case "scaleAttackhWithBudget":    ie = new EffectScaleAttackWithBudget(); break;
            case "scaleEffectWithBudget":     ie = new EffectScaleEffectWithBudget(); break;
            case "scaleEffectWithDamage":     ie = new EffectScaleEffectWithDamage(); break;
            case "scaleEffectWithTime":       ie = new EffectScaleEffectWithTime(); break;
            case "scaleHealthWithBudget":     ie = new EffectScaleHealthWithBudget(); break;
            case "scaleSpeedWithBudget":      ie = new EffectScaleSpeedWithBudget(); break;
            case "scaleSpeedWithDamage":      ie = new EffectScaleSpeedWithDamage(); break;
            case "scaleSpeedWithTime":        ie = new EffectScaleSpeedWithTime(); break;

            //<<Enemy Damaged Effects (On tower: trigger when attacking.  On Enemy type: trigger when attacked)>>
            case "armor":                     ie = new EffectArmor(); break;
            case "chainHit":                  ie = new EffectChainHit(); break;
            case "damagePercent":             ie = new EffectDamagePercent(); break;
            case "reduceEnemyEffectOnDamage": ie = new EffectReduceEnemyEffectOnDamage(); break;
            case "secondaryBurst":            ie = new EffectSecondaryBurst(); break;
            case "splashDamage":              ie = new EffectSplashDamage(); break;

            //<<Wave effects (On Enemy type: trigger on wave creation.  On card: trigger when played)>>
            case "budgetPercentageChange":    ie = new EffectBudgetPercentageChange(); break;
            case "changeWaveType":            ie = new EffectChangeWaveType(); break;
            case "timePercentageChange":      ie = new EffectTimePercentageChange(); break;

            //<<instant effects (trigger immediately when card is played)>>
            case "addCharges":                ie = new EffectAddCharges(); break;
            case "allTowersLifespanBonus":    ie = new EffectAllTowersLifespanBonus(); break;
            case "damagePlayer":              ie = new EffectDamagePlayer(); break;
            case "discardRandomCard":         ie = new EffectDiscardRandom(); break;
            case "drawCard":                  ie = new EffectDrawCard(); break;
            case "drawEnemyCard":             ie = new EffectDrawEnemyCard(); break;
            case "replaceRandomCard":         ie = new EffectReplaceRandomCard(); break;
            case "score":                     ie = new EffectScore(); break;
            case "shuffle":                   ie = new EffectShuffle(); break;
            case "dieRoll":                   ie = new EffectDieRoll(); break;

            //<<Targeting effects (Determines tower targeting behavior)>>
            case "targetAll":                 ie = new EffectTargetAll(); break;
            case "targetArmor":               ie = new EffectTargetArmor(); break;
            case "targetBurst":               ie = new EffectTargetBurst(); break;
            case "targetClosest":             ie = new EffectTargetClosest(); break;
            case "targetHealth":              ie = new EffectTargetHealth(); break;
            case "targetMultishot":           ie = new EffectTargetMultishot(); break;
            case "targetOrthogonal":          ie = new EffectTargetOrthogonal(); break;
            case "targetRandom":              ie = new EffectTargetRandom(); break;
            case "targetSpeed":               ie = new EffectTargetSpeed(); break;

            //<<property effects(changes how something behaves, but is never triggered) >>
            case "armorPierce":               ie = new EffectArmorPierce(); break;
            case "attackColor":               ie = new EffectAttackColor(); break;
            case "infiniteTowerLifespan":     ie = new EffectInfiniteTowerLifespan(); break;
            case "limitedAmmo":               ie = new EffectLimitedAmmo(); break;
            case "manualFire":                ie = new EffectManualFire(); break;
            case "returnsToTopOfDeck":        ie = new EffectReturnsToTopOfDeck(); break;
            case "upgradesForbidden":         ie = new EffectUpgradesForbidden(); break;

            //<<overcharge effects (tower maximum charge increases by 100% per point of overcharge.  Towers with at least one point of overcharge apply overcharge effects before firing)>>
            case "maxOvercharge":             ie = new EffectMaxOvercharge(); break;
            case "overchargeDamage":          ie = new EffectOverchargeDamage(); break;

            //<<meta effects (target another effect)>>
            case "effectCharges":             ie = new EffectEffectCharges(); break;
            case "effectCooldown":            ie = new EffectEffectCooldown(); break;
            case "everyRound":                ie = new EffectEveryRound(); break;
            case "percentageChance":          ie = new EffectPercentageChance(); break;
            case "ifRollRange":               ie = new EffectIfRollRange(); break;

            //<<death effects (occur when the tower/enemy dies)>>
            case "spawnEnemyOnDeath":         ie = new EffectSpawnEnemyOnDeath(); break;

            default:                          Debug.LogWarning("Effect type " + xe.name + " is not implemented."); return null;
        }
        ie.strength = xe.strength;
        ie.argument = xe.argument;
        ie.cardName = cardName;

        //if there is an inner effect,attempt to pass that too
        if (xe.innerEffect != null)
        {
            try
            {
                ((IEffectMeta)ie).innerEffect = parse(xe.innerEffect, cardName);
            }
            catch (InvalidCastException)
            {
                MessageHandlerScript.Warning("Found an inner effect on an effect that can't use them.");
            }
        }

        //catch effect classes returning the wrong xml name, which can cause weird issues elsewhere that are difficult to diagnose
        if (Debug.isDebugBuild)
            if (ie != null)
                if (xe.name != ie.XMLName)
                    Debug.LogWarning(xe.name + " is returning the wrong XML name (" + ie.XMLName + ")");

        return ie;
    }

    //returns a list of all classes that implement IEffect
    public IEnumerable<Type> IEffectTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
               .SelectMany(s => s.GetTypes())
               .Where(p => typeof(IEffect).IsAssignableFrom(p) && 
                           p.IsClass);
    }

    //returns a list of XML names that correspond to valid effects
    private String[] cachedEffectXMLNames;
    public String[] listEffectXMLNames()
    {
        if (cachedEffectXMLNames != null)
            return cachedEffectXMLNames;

        if (Application.isPlaying == false)
        {
            Debug.Log("This only works when in game");
            return null;
        }

        //create an object for every IEffect in the game
        List<String> XMLNames = new List<string>();
        foreach (Type t in IEffectTypes())
        {
            //not all effects have public constructors (ex: targetDefault).  These are not meant to be named anyway, so we can safely skip them
            IEffect ie;
            try
            {
                ie = (IEffect)Activator.CreateInstance(t);
            }
            catch (MissingMethodException) { continue; }

            XMLNames.Add(ie.XMLName);
        }

        XMLNames.Sort();
        cachedEffectXMLNames = XMLNames.ToArray();

        return cachedEffectXMLNames;
    }

    //provides a button in the unity inspector to print out a list of all effect names sorted by length.  To be used for looking for effect names that are either too long or are unhelpful
    [Show] private void listEffectNames()
    {
        if ( (LevelManagerScript.instance == null) || (LevelManagerScript.instance.levelLoaded == false) )
        {
            Debug.Log("This only works when a level is loaded");
            return;
        }

        //effect names vary on strength and argument, so we create a series of effect objects with strength 999 and argument "argument"
        Debug.Log("creating objects...");
        List<IEffect> effectObjects = new List<IEffect>();
        foreach (Type t in IEffectTypes())
        {
            //not all effects have public constructors (ex: targetDefault).  These are not meant to be named anyway, so we can safely skip them
            IEffect ie;
            try
            {
                ie = (IEffect) Activator.CreateInstance(t);
            }
            catch (MissingMethodException) { continue; }

            //skip listing effects without a name
            if (ie.Name == null)
                continue;

            //Debug.Log(ie.XMLName); //use when effect creation is spewing errors
            ie.strength = 99.9f;
            ie.argument = "argument";
            effectObjects.Add(ie);
        }
        Debug.Log("Effect names: ");

        effectObjects.Sort((ie1, ie2) => ie2.Name.Length.CompareTo(ie1.Name.Length));   //sort them by name length

        //print them
        foreach(IEffect ie in effectObjects)
                Debug.Log(ie.Name + "\n(" + ie.XMLName + ')');
        
        Debug.Log("Done!");
    }
}