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
            case "addCharges":                ie = new EffectAddCharges(); break;
            case "allTowersLifespanBonus":    ie = new EffectAllTowersLifespanBonus(); break;
            case "armor":                     ie = new EffectArmor(); break;
            case "attackDecreasesWithHealth": ie = new EffectAttackDecreasesWithHealth(); break;
            case "budgetPercentageChange":    ie = new EffectBudgetPercentageChange(); break;
            case "changeWaveType":            ie = new EffectChangeWaveType(); break;
            case "damagePlayer":              ie = new EffectDamagePlayer(); break;
            case "discardRandomCard":         ie = new EffectDiscardRandom(); break;
            case "drawCard":                  ie = new EffectDrawCard(); break;
            case "fixedSpawnCount":           ie = new EffectFixedSpawnCount(); break;
            case "healthIncreasesWithBudget": ie = new EffectHealthIncreasesWithBudget(); break;
            case "regeneration":              ie = new EffectRegeneration(); break;
            case "reduceEnemyEffectOnDamage": ie = new EffectReduceEnemyEffectOnDamage(); break;
            case "returnsToTopOfDeck":        ie = new EffectReturnsToTopOfDeck(); break;
            case "shuffle":                   ie = new EffectShuffle(); break;
            case "targetAll":                 ie = new EffectTargetAll(); break;
            case "targetArmor":               ie = new EffectTargetArmor(); break;
            case "targetHealth":              ie = new EffectTargetHealth(); break;
            case "targetMultishot":           ie = new EffectTargetMultishot(); break;
            case "targetRandom":              ie = new EffectTargetRandom(); break;
            case "targetSpeed":               ie = new EffectTargetSpeed(); break;
            case "timePercentageChange":      ie = new EffectTimePercentageChange(); break;
            default:                          Debug.LogWarning("Effect type " + xe.name + " is not implemented."); return null;
        }
        ie.strength = xe.strength;
        ie.argument = xe.argument;
        return ie;
    }
}