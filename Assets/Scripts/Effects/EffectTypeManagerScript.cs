using UnityEngine;
using Vexe.Runtime.Types;

//this class is responsible for taking XMLEffects and returning an IEffect to match
public class EffectTypeManagerScript : BaseBehaviour
{
    //singleton instance
    public static EffectTypeManagerScript instance;

    // Use this for initialization
    private void Awake()
    {
        instance = this;
    }

    // Update is called once per frame
    private void Update()
    {
    }

    //instantiates and initializes an effect object from the xmlEffect.  returns null if that effect doesnt exist
    //TODO: find a cleaner way to implement this?
    public IEffect parse(XMLEffect xe)
    {
        IEffect ie;
        switch (xe.name)
        {
            //<<Enemy Effects (Periodic)>>
            case "regeneration":              ie = new EffectRegeneration(); break;

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

            //<<Trigger on enemy damaged (On tower: trigger when attacking.  On Enemy type: trigger when attacked)>>
            case "armor":                     ie = new EffectArmor(); break;
            case "reduceEnemyEffectOnDamage": ie = new EffectReduceEnemyEffectOnDamage(); break;
            case "splashDamage":              ie = new EffectSplashDamage(); break;

            //<<Wave effects (On Enemy type: trigger on wave creation.  On card: trigger when played)>>
            case "budgetPercentageChange":    ie = new EffectBudgetPercentageChange(); break;
            case "changeWaveType":            ie = new EffectChangeWaveType(); break;
            case "timePercentageChange":      ie = new EffectTimePercentageChange(); break;

            //<<Trigger on card played>>
            case "addCharges":                ie = new EffectAddCharges(); break;
            case "allTowersLifespanBonus":    ie = new EffectAllTowersLifespanBonus(); break;
            case "damagePlayer":              ie = new EffectDamagePlayer(); break;
            case "discardRandomCard":         ie = new EffectDiscardRandom(); break;
            case "drawCard":                  ie = new EffectDrawCard(); break;
            case "returnsToTopOfDeck":        ie = new EffectReturnsToTopOfDeck(); break;
            case "shuffle":                   ie = new EffectShuffle(); break;

            //<<Targeting effects (Determines tower targeting behavior)>>
            case "targetAll":                 ie = new EffectTargetAll(); break;
            case "targetArmor":               ie = new EffectTargetArmor(); break;
            case "targetClosest":             ie = new EffectTargetClosest(); break;
            case "targetHealth":              ie = new EffectTargetHealth(); break;
            case "targetMultishot":           ie = new EffectTargetMultishot(); break;
            case "targetRandom":              ie = new EffectTargetRandom(); break;
            case "targetSpeed":               ie = new EffectTargetSpeed(); break;

            default:                          Debug.LogWarning("Effect type " + xe.name + " is not implemented."); return null;
        }
        ie.strength = xe.strength;
        ie.argument = xe.argument;
        return ie;
    }
}