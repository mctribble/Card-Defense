using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// this script is for the editor only object EditorBalanceTester
/// it contains utilities for testing game balance and outputting spreadsheets
/// </summary>
public class EditorBalanceTesterScript : BaseBehaviour
{
    //prefab used to create test towers
    public GameObject towerPrefab;

    //spreadsheet settings
    public bool includeModdedCards;
    public bool includeLimitedAmmoCards;
    public bool includeUpgradesWithoutStatChanges;
    public bool includeFormulas;

    //cell formulas and headers we can use to do statistical calculations in the resulting file.  Formulas are for openOffice calc.  (empty sections are for padding)
    //the formulas are messy because we use relative references everywhere so we dont have to track our location in the spreadsheet
    //they are simply added in as solid strings
    private const string FORMULA_HEADERS = "," + ",Median" + ",Min" + ",Max" + ",Average" + ",Std Deviation" + ",outlier if below:" + ",outlier if above:";
    private const string FORMULAS = 
        "," +
        ",=MEDIAN(INDIRECT(ADDRESS(ROW();2)):INDIRECT(ADDRESS(ROW();COLUMN()-2)))" +
        ",=MIN(INDIRECT(ADDRESS(ROW();2)):INDIRECT(ADDRESS(ROW();COLUMN()-3)))" +
        ",=MAX(INDIRECT(ADDRESS(ROW();2)):INDIRECT(ADDRESS(ROW();COLUMN()-4)))" +
        ",=AVERAGE(INDIRECT(ADDRESS(ROW();2)):INDIRECT(ADDRESS(ROW();COLUMN()-5)))" +
        ",=STDEV(INDIRECT(ADDRESS(ROW();2)):INDIRECT(ADDRESS(ROW();COLUMN()-6)))" +
        ",=QUARTILE(INDIRECT(ADDRESS(ROW();2)):INDIRECT(ADDRESS(ROW();COLUMN()-7));1) - ((QUARTILE(INDIRECT(ADDRESS(ROW();2)):INDIRECT(ADDRESS(ROW();COLUMN()-7));3)-QUARTILE(INDIRECT(ADDRESS(ROW();2)):INDIRECT(ADDRESS(ROW();COLUMN()-7));1)) * 3)" +
        ",=QUARTILE(INDIRECT(ADDRESS(ROW();2)):INDIRECT(ADDRESS(ROW();COLUMN()-8));3) + ((QUARTILE(INDIRECT(ADDRESS(ROW();2)):INDIRECT(ADDRESS(ROW();COLUMN()-8));3)-QUARTILE(INDIRECT(ADDRESS(ROW();2)):INDIRECT(ADDRESS(ROW();COLUMN()-8));1)) * 3)";

    /// <summary>
    /// generates a large .CSV file listing how each tower has a stat affected by each upgrade, applied various times
    /// the first table in the resulting file is base stats of every tower
    /// each subsequent table is how a tower changes with a given upgrade applied x times. Each tower gets a table for however many upgrades it can actually hold
    /// </summary>
    [Comment("generates a spreadsheet that shows the stats of each tower with upgrades applied in varying quantiites.")]
    [Show] public void TowerUpgradeStats()
    {
        //bail if not loaded
        if ( (CardTypeManagerScript.instance == null) || (CardTypeManagerScript.instance.areTypesLoaded() == false) )
        {
            Debug.LogWarning("Can't generate spreadsheet: card types not loaded.");
            return;
        }

        //gather up the relevant types
        List<PlayerCardData> towerCards   = CardTypeManagerScript.instance.types.cardTypes.Where(pcd => pcd.cardType == PlayerCardType.tower  ).ToList();
        List<PlayerCardData> upgradeCards = CardTypeManagerScript.instance.types.cardTypes.Where(pcd => pcd.cardType == PlayerCardType.upgrade).ToList();

        //prune lists based on settings
        if (includeModdedCards == false)
        {
            towerCards  .RemoveAll(pcd => pcd.isModded);
            upgradeCards.RemoveAll(pcd => pcd.isModded);
        }
        if (includeLimitedAmmoCards == false)
        {
            towerCards  .RemoveAll(pcd => (pcd.effectData != null) && (pcd.effectData.propertyEffects.limitedAmmo != null));
            upgradeCards.RemoveAll(pcd => (pcd.effectData != null) && (pcd.effectData.propertyEffects.limitedAmmo != null));
        }
        if (includeUpgradesWithoutStatChanges == false)
        {
            upgradeCards.RemoveAll(pcd => (pcd.upgradeData.attackModifier     == 0.0f) &&
                                          (pcd.upgradeData.rangeModifier      == 0.0f) &&
                                          (pcd.upgradeData.rechargeModifier   == 0.0f) &&
                                          (pcd.upgradeData.attackMultiplier   == 1.0f) &&
                                          (pcd.upgradeData.rangeMultiplier    == 1.0f) &&
                                          (pcd.upgradeData.rechargeMultiplier == 1.0f));
        }

        //remove the old file, if it exists
        string filePath = Path.Combine(Application.dataPath, "towerUpgradeStats.csv");
        if (File.Exists(filePath))
            File.Delete(filePath);

        using (StreamWriter spreadsheet = new StreamWriter(filePath)) //create a new one
        {
            //create a table for the base stats

            //header
            spreadsheet.Write("base stats");
            towerCards.ForEach(pcd => spreadsheet.Write(',' + pcd.cardName));
            if (includeFormulas)
                spreadsheet.Write(FORMULA_HEADERS);
            spreadsheet.WriteLine();

            //stats
            spreadsheet.Write("damage");       towerCards.ForEach(pcd => spreadsheet.Write(',' + pcd.towerData.attackPower .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
            spreadsheet.Write("recharge");     towerCards.ForEach(pcd => spreadsheet.Write(',' + pcd.towerData.rechargeTime.ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
            spreadsheet.Write("DPS_1");        towerCards.ForEach(pcd => spreadsheet.Write(',' + DPS_1(pcd)                .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
            spreadsheet.Write("DPS_10");       towerCards.ForEach(pcd => spreadsheet.Write(',' + DPS_10(pcd)               .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
            spreadsheet.Write("DPS_10_range"); towerCards.ForEach(pcd => spreadsheet.Write(',' + DPS_10_range(pcd)         .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
            spreadsheet.Write("DPS_100");      towerCards.ForEach(pcd => spreadsheet.Write(',' + DPS_100(pcd)              .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
            spreadsheet.Write("range");        towerCards.ForEach(pcd => spreadsheet.Write(',' + pcd.towerData.range       .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
            spreadsheet.Write("lifespan");     towerCards.ForEach(pcd => spreadsheet.Write(',' + pcd.towerData.lifespan    .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();

            //a line of padding
            spreadsheet.WriteLine();

            //now we need tables for every tower
            foreach (PlayerCardData curTower in towerCards)
            {
                //and for every quantity of upgrades on those towers
                for (int quantity = 1; quantity <= curTower.towerData.upgradeCap; quantity++)
                {
                    //header
                    spreadsheet.Write(curTower.cardName + '(' + quantity + '/' + curTower.towerData.upgradeCap + ')');
                    upgradeCards.ForEach(pcd => spreadsheet.Write(',' + pcd.cardName));
                    if (includeFormulas)
                        spreadsheet.Write(FORMULA_HEADERS);
                    spreadsheet.WriteLine();

                    //create test towers and give them the upgrades
                    List<TowerScript> testTowers = new List<TowerScript>();
                    foreach (PlayerCardData curUpgrade in upgradeCards)
                    {
                        //spawn tower
                        TowerScript testTower = Instantiate(towerPrefab).GetComponent<TowerScript>();
                        testTower.SetData(curTower.towerData);
                        if (curTower.effectData != null)
                            testTower.AddEffects(curTower.effectData);
                        
                        //apply upgrades, provided the tower permits it
                        for (int i = 0; i < quantity; i++)
                        {
                            if (testTower.effects == null || testTower.effects.propertyEffects.upgradesForbidden == false)
                            {
                                testTower.Upgrade(curUpgrade.upgradeData);
                                if (curUpgrade.effectData != null)
                                    testTower.AddEffects(curUpgrade.effectData);
                            }
                        }

                        //put it on the list
                        testTowers.Add(testTower);
                    }

                    //use the upgraded towers to populate the spreadsheet stats
                    spreadsheet.Write("damage");       testTowers.ForEach(tower => spreadsheet.Write(',' + tower.attackPower   .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
                    spreadsheet.Write("recharge");     testTowers.ForEach(tower => spreadsheet.Write(',' + tower.rechargeTime  .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
                    spreadsheet.Write("DPS_1");        testTowers.ForEach(tower => spreadsheet.Write(',' + DPS_1(tower)        .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
                    spreadsheet.Write("DPS_10");       testTowers.ForEach(tower => spreadsheet.Write(',' + DPS_10(tower)       .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
                    spreadsheet.Write("DPS_10_range"); testTowers.ForEach(tower => spreadsheet.Write(',' + DPS_10_range(tower) .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
                    spreadsheet.Write("DPS_100");      testTowers.ForEach(tower => spreadsheet.Write(',' + DPS_100(tower)      .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
                    spreadsheet.Write("range");        testTowers.ForEach(tower => spreadsheet.Write(',' + tower.range         .ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();
                    spreadsheet.Write("lifespan");     testTowers.ForEach(tower => spreadsheet.Write(',' + tower.wavesRemaining.ToString("F3"))); if (includeFormulas) spreadsheet.Write(FORMULAS); spreadsheet.WriteLine();

                    //a line of padding
                    spreadsheet.WriteLine();

                    //destroy the test towers
                    testTowers.ForEach(tt => Destroy(tt.gameObject));
                }
            }
        }

        System.Diagnostics.Process.Start(filePath); //open the new spreadsheet with the default program
    }

    //helper functions to get values for DPS_1, DPS_10, DPS_10_range, and DPS_100
    private int   maxTargets  (PlayerCardData pcd) { if (pcd.effectData == null) return 1; else return pcd.effectData.maxTargets; }                         //max targets the given tower card can attack
    private float DPS_1       (PlayerCardData pcd) { return pcd.towerData.attackPower / pcd.towerData.rechargeTime; }                                       //damage per second the given tower card does, if it has 1  enemy in range
    private float DPS_10      (PlayerCardData pcd) { return Mathf.Min(maxTargets(pcd), 10)  * DPS_1(pcd); }                                                 //damage per second the given tower card does, if it has 10 enemies in range
    private float DPS_10_range(PlayerCardData pcd) { return Mathf.Min(maxTargets(pcd), 10)  * DPS_1(pcd) + (pcd.towerData.range * (maxTargets(pcd) - 1)); } //damage per second the given tower card does, if it has 10*range enemies in range
    private float DPS_100     (PlayerCardData pcd) { return Mathf.Min(maxTargets(pcd), 100) * DPS_1(pcd); }                                                 //damage per second the given tower card does, if it has 100 enemies in range
                                                                                                                                                            
    private int   maxTargets  (TowerScript tower) { if (tower.effects == null) return 1; else return tower.effects.maxTargets; }                         //max targets the given tower card can attack
    private float DPS_1       (TowerScript tower) { return tower.attackPower / tower.rechargeTime; }                                                     //damage per second the given tower card does, if it has 1  enemy in range
    private float DPS_10      (TowerScript tower) { return Mathf.Min(maxTargets(tower), 10)  * DPS_1(tower); }                                           //damage per second the given tower card does, if it has 10 enemies in range
    private float DPS_10_range(TowerScript tower) { return Mathf.Min(maxTargets(tower), 10)  * DPS_1(tower) + (tower.range * (maxTargets(tower) - 1)); } //damage per second the given tower card does, if it has 10*range enemies in range
    private float DPS_100     (TowerScript tower) { return Mathf.Min(maxTargets(tower), 100) * DPS_1(tower); }                                           //damage per second the given tower card does, if it has 100 enemies in range
    
}
