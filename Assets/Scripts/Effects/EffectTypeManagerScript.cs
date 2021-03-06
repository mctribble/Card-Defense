﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// factory class responsible for taking XMLEffects and turning them into the appropriate IEffect
/// </summary>
public class EffectTypeManagerScript : BaseBehaviour
{
    //singleton instance
    [Hide] public static EffectTypeManagerScript instance;

    // Use this for initialization
    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// instantiates and initializes an IEffect object from an XMLEffect.  returns null if that effect doesnt exist
    /// the card name is simply passed unaltered to the new effect
    /// </summary>
    /// <param name="xe">XML representation of the desired effect</param>
    /// <param name="cardName">name of the card to associate with this effect in game logs</param>
    /// <returns>a new IEffect object</returns>
    public IEffect parse(XMLEffect xe, string cardName)
    {
        IEffect ie;

        //this list is intentionally hard coded instead of using code reflection for security because the original XMLEffect may have been made by a player
        switch (xe.name)
        {
            //<<Enemy Effects (Periodic)>>
            case "poison":                     ie = new EffectPoison();                     break;
            case "regeneration":               ie = new EffectRegeneration();               break;
                                                                                            
            //<<Enemy Effects (Stat Scaling)>>                                              
            case "invScaleAttackWithDamage":   ie = new EffectinvScaleAttackWithDamage();   break;
            case "invScaleEffectWithDamage":   ie = new EffectinvScaleEffectWithDamage();   break;
            case "invScaleEffectWithTime":     ie = new EffectInvScaleEffectWithTime();     break;
            case "invScaleSpeedWithDamage":    ie = new EffectinvScaleSpeedWithDamage();    break;
            case "invScaleSpeedWithTime":      ie = new EffectInvScaleSpeedWithTime();      break;
            case "scaleAttackWithDamage":      ie = new EffectscaleAttackWithDamage();      break;
            case "scaleEffectWithDamage":      ie = new EffectScaleEffectWithDamage();      break;
            case "scaleEffectWithRank":        ie = new EffectScaleEffectWithRank();        break;
            case "scaleEffectWithTime":        ie = new EffectScaleEffectWithTime();        break;
            case "scaleSpeedWithDamage":       ie = new EffectScaleSpeedWithDamage();       break;
            case "scaleSpeedWithTime":         ie = new EffectScaleSpeedWithTime();         break;

            //<<Enemy Damaged Effects (On tower: trigger when attacking.  On Enemy type: trigger when attacked)>>
            case "armor":                      ie = new EffectArmor();                      break;
            case "chainHit":                   ie = new EffectChainHit();                   break;
            case "damagePercent":              ie = new EffectDamagePercent();              break;
            case "reduceEnemyEffectOnDamage":  ie = new EffectReduceEnemyEffectOnDamage();  break;
            case "ResonantTowerAttackMult":    ie = new EffectResonantTowerAttackMult();    break;
            case "ResonantTowerAttackMod":     ie = new EffectResonantTowerAttackMod();     break;
            case "ResonantTowerRangeMod":      ie = new EffectResonantTowerRangeMod();      break;
            case "secondaryBurst":             ie = new EffectSecondaryBurst();             break;
            case "slowTarget":                 ie = new EffectSlowTarget();                 break;
            case "splashDamage":               ie = new EffectSplashDamage();               break;

            //<<Wave effects (On Enemy type: trigger on wave creation.  On card: trigger when played)>>
            case "budgetPercentageChange":     ie = new EffectBudgetPercentageChange();     break;
            case "changeWaveType":             ie = new EffectChangeWaveType();             break;
            case "timePercentageChange":       ie = new EffectTimePercentageChange();       break;
                                                                                            
            //<<instant effects (trigger immediately when card is played)>>                 
            case "addCharges":                   ie = new EffectAddCharges();                   break;
            case "allTowersLifespanBonus":       ie = new EffectAllTowersLifespanBonus();       break;
            case "conjureCard":                  ie = new EffectConjureCard();                  break;
            case "conjureEnemyCard":             ie = new EffectConjureEnemyCard();             break;
            case "conjureSpecificCard":          ie = new EffectConjureSpecificCard();          break;
            case "conjureSpellCard":             ie = new EffectConjureSpellCard();             break;
            case "conjureTowerCard":             ie = new EffectConjureTowerCard();             break;
            case "conjureUpgradeCard":           ie = new EffectConjureUpgradeCard();           break;
            case "damageHand":                   ie = new EffectDamageHand();                   break;
            case "damagePlayer":                 ie = new EffectDamagePlayer();                 break;
            case "dieRoll":                      ie = new EffectDieRoll();                      break;
            case "discardChosenCard":            ie = new EffectDiscardChosen();                break;
            case "discardRandomCard":            ie = new EffectDiscardRandom();                break;
            case "drawCard":                     ie = new EffectDrawCard();                     break;
            case "drawEnemyCard":                ie = new EffectDrawEnemyCard();                break;
            case "drawSpellCard":                ie = new EffectDrawSpellCard();                break;
            case "drawTowerCard":                ie = new EffectDrawTowerCard();                break;
            case "drawUpgradeCard":              ie = new EffectDrawUpgradeCard();              break;
            case "replaceRandomCard":            ie = new EffectReplaceRandomCard();            break;
            case "replaceRandomCardWithSpell":   ie = new EffectReplaceRandomCardWithSpell();   break;
            case "replaceRandomCardWithTower":   ie = new EffectReplaceRandomCardWithTower();   break;
            case "replaceRandomCardWithUpgrade": ie = new EffectReplaceRandomCardWithUpgrade(); break;
            case "score":                        ie = new EffectScore();                        break;
            case "shuffle":                      ie = new EffectShuffle();                      break;
            case "upgradeAllTowers":             ie = new EffectUpgradeAllTowers();             break;
                                                                                            
            //<<Targeting effects (Determines tower targeting behavior)>>                   
            case "targetAll":                  ie = new EffectTargetAll();                  break;
            case "targetArmor":                ie = new EffectTargetArmor();                break;
            case "targetBurst":                ie = new EffectTargetBurst();                break;
            case "targetClosest":              ie = new EffectTargetClosest();              break;
            case "targetHealth":               ie = new EffectTargetHealth();               break;
            case "targetLowHealth":            ie = new EffectTargetLowHealth();            break;
            case "targetMouse":                ie = new EffectTargetMouse();                break;
            case "targetMultishot":            ie = new EffectTargetMultishot();            break;
            case "targetOrthogonal":           ie = new EffectTargetOrthogonal();           break;
            case "targetRandom":               ie = new EffectTargetRandom();               break;
            case "targetSpeed":                ie = new EffectTargetSpeed();                break;
                                                                                            
            //<<property effects(changes how something behaves, but is never triggered) >>  
            case "armorPierce":                ie = new EffectArmorPierce();                break;
            case "attackColor":                ie = new EffectAttackColor();                break;
            case "cannotBeDiscarded":          ie = new EffectCannotBeDiscarded();          break;
            case "infiniteTowerLifespan":      ie = new EffectInfiniteTowerLifespan();      break;
            case "limitedAmmo":                ie = new EffectLimitedAmmo();                break;
            case "manualFire":                 ie = new EffectManualFire();                 break;
            case "noUpgradeCost":              ie = new EffectNoUpgradeCost();              break;
            case "returnsToTopOfDeck":         ie = new EffectReturnsToTopOfDeck();         break;
            case "upgradesForbidden":          ie = new EffectUpgradesForbidden();          break;

            //<<overcharge effects (tower maximum charge increases by 100% per point of overcharge.  Towers with at least one point of overcharge apply overcharge effects before firing)>>
            case "maxOvercharge":              ie = new EffectMaxOvercharge();              break;
            case "overchargeDamage":           ie = new EffectOverchargeDamage();           break;

            //<<meta effects (target another effect)>>                                      
            case "customDescription":          ie = new EffectCustomDescription();          break;
            case "effectCharges":              ie = new EffectEffectCharges();              break;
            case "effectCooldown":             ie = new EffectEffectCooldown();             break;
            case "everyRound":                 ie = new EffectEveryRound();                 break;
            case "onAttack":                   ie = new EffectOnAttack();                   break;
            case "onCardDrawn":                ie = new EffectOnCardDrawn();                break;
            case "onSpawned":                  ie = new EffectOnSpawned();                  break;
            case "percentageChance":           ie = new EffectPercentageChance();           break;
            case "scaleEffectByTowerAttack":   ie = new EffectScaleEffectWithTowerAttack(); break;
            case "ifRollRange":                ie = new EffectIfRollRange();                break;

            //<<death effects (occur when the tower/enemy dies)>>                          
            case "redrawTowerOnDeath":         ie = new EffectRedrawTowerOnDeath();         break;
            case "spawnEnemyOnDeath":          ie = new EffectSpawnEnemyOnDeath();          break;

            //<<upgrade effects (occur when a tower is upgraded.  only valid on upgrade cards)>>
            case "reloadAmmo":                 ie = new EffectReloadAmmo();                 break;
            case "setRange":                   ie = new EffectSetRange();                   break;
            case "statPercentChangePerRound":  ie = new EffectStatPercentChangePerRound();  break;

            default: Debug.LogWarning("Effect type " + xe.name + " is not implemented."); return null;
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
                Debug.LogWarning("Found an inner effect on an effect that can't use them.");
            }
        }

        //catch effect classes returning the wrong xml name, which can cause weird issues elsewhere that are difficult to diagnose
        if (Debug.isDebugBuild)
            if (ie != null)
                if (xe.name != ie.XMLName)
                    Debug.LogWarning(xe.name + " is returning the wrong XML name (" + ie.XMLName + ")");

        return ie;
    }

    /// <summary>
    /// returns a list of all classes that implement IEffect
    /// </summary>
    public static IEnumerable<Type> IEffectTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
               .SelectMany(s => s.GetTypes())
               .Where(p => typeof(IEffect).IsAssignableFrom(p) && 
                           p.IsClass);
    }

    /// <summary>
    /// returns a list of XML names that correspond to valid effects
    /// </summary>
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

    /// <summary>
    /// provides a button in the unity inspector to print out a list of all effect names sorted by length.  
    /// To be used for looking for effect names that are either too long or are unhelpful
    /// </summary>
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