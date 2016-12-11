using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

/// <summary>
/// represents an XML tower definition
/// </summary>
[System.Serializable]
public class TowerData : System.Object
{
    [Hide][XmlIgnore] public string towerName; //name of the card which summoned this tower.  Populated when the tower is summoned

    [XmlAttribute("Sprite")]
    public string towerSpriteName { get; set; }

    //optional field that colorizes the sprite
    public XMLColor towerColor;

    [XmlAttribute("Recharge")]   public float rechargeTime; //how long it takes the tower to charge
    [XmlAttribute("Range")]      public float range;        //how far away from itself, in world space, the tower can shoot
    [XmlAttribute("Damage")]     public float attackPower;  // amount of damage this dower does before any modifiers such as armor
    [XmlAttribute("Lifespan")]   public int   lifespan;	    // amount of waves this tower remains on the field
    [XmlAttribute("UpgradeCap")] public int   upgradeCap;   //max number of upgrades this tower can have

    //provides a short string for the debugger
    public override string ToString()
    {
        return towerName +
            "{recharge:" + rechargeTime +
            " range:"    + range +
            " damage:"   + attackPower +
            " lifespan:" + lifespan + "}";
    }
}

/// <summary>
/// represents a tower that has been built into the world.
/// </summary>
public class TowerScript : BaseBehaviour
{
    //functions used to determine what should be shown when in the inspector
    private bool isEditor() { return Application.isPlaying == false; }
    private bool hasData() { return ((towerName != null) && (towerName != "")); }

    //object prefabs
    [VisibleWhen("isEditor")] public GameObject bulletPrefab;
    [VisibleWhen("isEditor")] public GameObject burstShotPrefab;
    [VisibleWhen("isEditor")] public GameObject directionalShotPrefab;

    //text colors to switch between based on how the tower decays
    [VisibleWhen("isEditor")] public Color textColorLifespan;
    [VisibleWhen("isEditor")] public Color textColorAmmo;
    [VisibleWhen("isEditor")] public Color textColorBoth;
    [VisibleWhen("isEditor")] public Color textColorNeither;

    [VisibleWhen("hasData")] public string      towerName;      //name of the tower
    [VisibleWhen("hasData")] public ushort      upgradeCount;   //number of times this tower has been upgraded
    [VisibleWhen("hasData")] public ushort      upgradeCap;     //max number of times this tower can be upgraded
    [VisibleWhen("hasData")] public float       rechargeTime;   //time, in seconds, between shots.
    [VisibleWhen("hasData")] public float       range;          //distance the tower can shoot
    [VisibleWhen("hasData")] public float       attackPower;	//damage done on hit
    [VisibleWhen("hasData")] public int         wavesRemaining; //number of waves this tower has left before disappearing
    [VisibleWhen("hasData")] public EffectData  effects;        //effects on this tower

    //component references for all the display objects
    [VisibleWhen("isEditor")] public Image towerImage;
    [VisibleWhen("isEditor")] public Image rangeImage;
    [VisibleWhen("isEditor")] public Image chargeGaugeImage1x;
    [VisibleWhen("isEditor")] public Image chargeGaugeImage2x;
    [VisibleWhen("isEditor")] public Image chargeGaugeImage3x;
    [VisibleWhen("isEditor")] public Image tooltipPanel;
    [VisibleWhen("isEditor")] public Text  tooltipText;
    [VisibleWhen("isEditor")] public Text  lifespanText;
    [VisibleWhen("isEditor")] public ParticleSystem manualFireParticles;

    [VisibleWhen("hasData")] private float deltaTime;            //time since last frame
    [VisibleWhen("hasData")] private float shotCharge;           //represents how charged the next shot is. 0.0 is empty, maxCharge is full
    [VisibleWhen("hasData")] private float maxCharge;            //max shot charge (default 1.0)
    [VisibleWhen("hasData")] private bool  waitingForManualFire; //whether user is being prompted to fire manually

    // Use this for initialization
    private void Awake()
    {
        //init vars
        rangeImage.enabled = false;
        upgradeCount = 0;
        maxCharge = 1.0f;
        effects = null;

        //set scale of range image
        rangeImage.gameObject.GetComponent<RectTransform>().localScale = new Vector3(range, range, 1.0f);

        //hide tooltip until moused over
        tooltipText.enabled = false;

        updateLifespanText();
    }

    // Update is called once per frame
    private void Update()
    {
        //clean out the effect list every 32 frames
        if (effects != null)
        {
            if ((Time.frameCount % 32) == 0)
            {
                effects.cleanEffects();
                UpdateTooltipText();
            }
        }

        deltaTime = Time.deltaTime; //update time since last frame

        //update tooltip position
        if (tooltipText.enabled)
        {
            //calculate pivot
            //default: pivot in lower right
            int x = 1;
            int y = 0;

            //if too close to the left, move pivot to the left
            if (Input.mousePosition.x < tooltipText.preferredWidth)
            {
                x = 0;
            }

            //if too close to the top, move pivot to the bottom
            if (Input.mousePosition.y > (Screen.height - tooltipText.preferredHeight))
            {
                y = 1;
            }

            //set pivot
            tooltipPanel.rectTransform.pivot = new Vector2(x, y);

            //calculate position offset
            Vector3 positionOffset = Vector3.zero;

            if (x == 0)
                positionOffset.x = 2;
            else
                positionOffset.x = -2;

            if (y == 0)
                positionOffset.y = 2;
            else
                positionOffset.y = -2;

            //set pos
            tooltipPanel.transform.position = Input.mousePosition + positionOffset;
        }

        //increase shot charge if the gauge is not already full
        //note that this is a "soft cap": a single frame can bring the value over, but it would stop charging afterwards
        //this avoids issues when towers should be shooting every frame, or even multiple times per frame
        if (shotCharge < maxCharge)
        {
            shotCharge += deltaTime / rechargeTime; //charge

            //update charge gauges
            float fillAmount = Mathf.Min(shotCharge, maxCharge); //we fill the gauges based on max charge instead of current charge if we are over, so the wonkiness of a soft cap is hidden from the player
            chargeGaugeImage1x.fillAmount = fillAmount;
            chargeGaugeImage2x.fillAmount = fillAmount - 1.0f;
            chargeGaugeImage3x.fillAmount = fillAmount - 2.0f;
        }

        //while a shot is charged...
        //(this is a loop because it is technically possible to fire multiple times per frame if the frame took a long time for some reason or the tower fires extremely quickly)
        //(towers with overcharge effects will use multiple charges on one shot, but those without will simply fire multiple times on account of this loop
        while (shotCharge > 1.0f)
        {
            //if the tower has the manualFire property effect, flag that we are waiting for the player instead of firing now
            if ((effects != null) && (effects.propertyEffects.manualFire == true))
            {
                if (waitingForManualFire == false)
                {
                    waitingForManualFire = true;
                    manualFireParticles.Play(true);
                }
                return;
            }

            //call helpers to perform targeting and shooting
            List<EnemyScript> targets = target();
            bool result = fire(targets);
            if (result == false)
                break; //bail if we failed to fire for any reason
        }
    }

    /// <summary> called when the mouse is over the tower to activate the tooltip </summary>
    private void TowerMouseEnter()
    {
        rangeImage.enabled = true;
        tooltipPanel.enabled = true;
        tooltipText.enabled = true;
    }

    /// <summary> called when the mouse is over the tower to deactivate the tooltip </summary>
    private void TowerMouseExit()
    {
        rangeImage.enabled = false;
        tooltipPanel.enabled = false;
        tooltipText.enabled = false;
    }

    /// <summary> called when the tower is clicked on.  responsible for triggering a manual fire on towers that have it</summary>
    private void TowerMouseClick()
    {
        if (waitingForManualFire)
        {
            //call helpers to perform targeting and shooting
            List<EnemyScript> targets = target();
            bool result = fire(targets);
            if (result == false)
                return; //bail if we failed to fire for any reason

            //update text
            updateLifespanText();

            //unflag the wait
            manualFireParticles.Stop(true);
            waitingForManualFire = false;

            //if out of ammo, destroy tower
            if (effects.propertyEffects.limitedAmmo == 0)
                onDeath();
        }
    }

    /// <summary>
    /// finds all the enemies that this tower should hit if it makes an attack now.  Said list might be empty, but it will not be null.
    /// </summary>
    /// <returns>the resulting list</returns>
    private List<EnemyScript> target()
    {
        //bail if there are no enemies on the map
        if (EnemyManagerScript.instance.activeEnemies.Count == 0)
            return null;

        //if we are limited by ammo and are out of ammo, bail
        if (effects != null)
            if (effects.propertyEffects.limitedAmmo != null)
                if (effects.propertyEffects.limitedAmmo == 0)
                    return null;

        //get the target list from effect data. (this is for performance reasons: effectData can cache the list of different targeting effects on the object and return the first that doesn't error)
        List<EnemyScript> targets;
        if (effects != null)
            targets = effects.doTowerTargeting(transform.position, range);
        else
            targets = EffectTargetDefault.instance.findTargets(transform.position, range); //EffectTargetDefault is a placeholder used when there is no target

        Debug.Assert(targets != null); //there must always be a target list, even if it is empty

        return targets;
    }

    /// <summary>
    /// fires on each enemy in the given list, if possible
    /// </summary>
    /// <param name="targets">the enemies to attack</param>
    /// <returns>whether or not the attack was successful</returns>
    private bool fire(List<EnemyScript> targets)
    {
        //only fire if we have valid targets
        if ((targets != null) && (targets.Count != 0))
        {
            //reduce charge meter (if the gauge was overcharged, retain the excess)
            shotCharge -= 1.0f;

            //if we are operating on ammo, decrease that too
            if (effects != null)
            {
                if (effects.propertyEffects.limitedAmmo != null)
                {
                    PropertyEffects temp = effects.propertyEffects;
                    temp.limitedAmmo--;
                    effects.propertyEffects = temp;
                    UpdateTooltipText();
                }
            }

            //trigger attack effects
            if (effects != null)
                foreach (IEffect effect in effects.effects)
                    if (effect.triggersAs(EffectType.attack))
                        ((IEffectAttack)effect).towerAttack(this);

            //create a struct and fill it with data about the attack
            DamageEventData ded = new DamageEventData();
            ded.rawDamage = attackPower;
            ded.source = this;
            ded.effects = effects;

            //if overcharged and there are overcharge effects, try to apply them
            if ((shotCharge >= 1.0f) && (effects != null) && (effects.propertyEffects.maxOvercharge != null))
            {
                int availableOvercharge = Mathf.FloorToInt(shotCharge); //calculate available points of overcharge
                int usedOvercharge = Mathf.Min(availableOvercharge, effects.propertyEffects.maxOvercharge.Value); //if there are more available than our max, still only use the max
                shotCharge -= usedOvercharge; //decrement gauge

                //trigger overcharge effects
                foreach (IEffect effect in effects.effects)
                    if (effect.triggersAs(EffectType.overcharge))
                        ((IEffectOvercharge)effect).trigger(ref ded, usedOvercharge);
            }

            //determine projectile spawning by targeting effect
            if (effects != null)
            {
                switch (effects.lastUsedTargetingEffect)
                {
                    case "targetOrthogonal":
                        //split target list into four others, one for each direction
                        List<EnemyScript> left  = new List<EnemyScript>();
                        List<EnemyScript> right = new List<EnemyScript>();
                        List<EnemyScript> up    = new List<EnemyScript>();
                        List<EnemyScript> down  = new List<EnemyScript>();
                        foreach (EnemyScript t in targets)
                        {
                            if (t.transform.position.x < this.transform.position.x)
                                left.Add(t);
                            else if (t.transform.position.x > this.transform.position.x)
                                right.Add(t);
                            else if (t.transform.position.y > this.transform.position.y)
                                up.Add(t);
                            else if (t.transform.position.y < this.transform.position.y)
                                down.Add(t);
                        }

                        //and fire each separately
                        if (left.Count > 0)
                            directionalShot(left, ded, Vector2.left);
                        if (right.Count > 0)
                            directionalShot(right, ded, Vector2.right);
                        if (up.Count > 0)
                            directionalShot(up, ded, Vector2.up);
                        if (down.Count > 0)
                            directionalShot(down, ded, Vector2.down);

                        break;

                    case "targetBurst":
                        burstFire(targets, ded);
                        break;

                    default:
                        foreach (EnemyScript t in targets)
                            spawnBullet(t, ded);
                        break;
                }
            }
            else //no targeting effects present: use bullets
            {
                foreach (EnemyScript t in targets)
                    spawnBullet(t, ded);
            }

            return true; //success
        }
        else
        {
            return false; //failure
        }
    }

    /// <summary>
    /// spawns a bullet to attack an enemy unit
    /// </summary>
    /// <param name="enemy">the enemy to attack</param>
    /// <param name="damageEvent">details of the attack the new bullet is meant to perform</param>
    private void spawnBullet(EnemyScript enemy, DamageEventData damageEvent)
    {
        //tell the event who our target is
        damageEvent.dest = enemy;

        GameObject bullet = (GameObject) Instantiate (bulletPrefab, transform.position, Quaternion.identity);
        bullet.SendMessage("InitBullet", damageEvent);

        //apply attackColor property, if it is present
        if (effects != null)
            if (effects.propertyEffects.attackColor != null)
                bullet.SendMessage("SetColor", effects.propertyEffects.attackColor);
    }

    /// <summary>
    /// spawns a burstShot to attack all enemies in range
    /// </summary>
    /// <param name="targets">list of enemies in range.  The burstShot itself will catch enemies that enter the region during the attack</param>
    /// <param name="damageEvent">details of the attack.  damageEvent.dest is ignored.</param>
    private void burstFire(List<EnemyScript> targets, DamageEventData damageEvent)
    {
        //construct burst shot event
        BurstShotData data = new BurstShotData();
        data.targetList = targets;
        data.burstRange = range;
        data.damageEvent = damageEvent;

        //create a burst shot and give it the data
        GameObject shot = Instantiate(burstShotPrefab); //create it
        shot.transform.position = transform.position;   //center it on the tower
        shot.SendMessage("SetData", data);              //initialize it
    }

    /// <summary>
    /// spawns a directionalShot to attack all enemies in a straight line
    /// </summary>
    /// <param name="targets">list of enemies in range.  The DirectionalShot itself will find other enemies on its own as it goes by</param>
    /// <param name="damageEvent">details of the attack.  damageEvent.dest is ignored.</param>
    /// <param name="attackDir">direction the attack should travel</param>
    private void directionalShot(List<EnemyScript> targets, DamageEventData damageEvent, Vector2 attackDir)
    {
        //construct event
        DirectionalShotData data = new DirectionalShotData();
        data.attackDir = attackDir;
        data.damageEvent = damageEvent;
        data.targetList = targets;

        //spawn it
        GameObject shot = Instantiate(directionalShotPrefab);
        shot.transform.position = transform.position;   //center it on the tower
        shot.SendMessage("SetData", data);              //initialize it
    }

    /// <summary>
    /// sets the tower definition.  Uses a coroutine behind the scenes, but calling could should not need to care.
    /// </summary>
    public void SetData(TowerData d) { StartCoroutine(SetDataCoroutine(d)); }

    /// <summary>
    /// handles setting tower data.  
    /// <see cref="SetData(TowerData)"/>
    /// </summary>
    private IEnumerator SetDataCoroutine(TowerData d)
    {
        towerName = d.towerName;
        rechargeTime = d.rechargeTime;
        range = d.range;
        attackPower = d.attackPower;
        wavesRemaining = d.lifespan;
        upgradeCap = (ushort)d.upgradeCap;

        //yes, I know its awkward, but we're setting the sprite with WWW, even on pc
        string spritePath = "";
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            spritePath = "file:///";
        spritePath += Application.streamingAssetsPath + "/Art/Sprites/" + d.towerSpriteName;
        WWW www = new WWW (spritePath);
        yield return www;

        if (www.error == null)
            towerImage.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
        else
            towerImage.sprite = Resources.Load<Sprite>("Sprites/Error");

        //if a color was provided, use it
        if (d.towerColor != null)
            towerImage.color = d.towerColor.toColor();

        //set scale of range image
        rangeImage.gameObject.GetComponent<RectTransform>().localScale = new Vector3(range, range, 1.0f);

        //update text
        UpdateTooltipText();

        updateLifespanText();
    }

    private void SetEffectData(EffectData d)
    {
        effects = d.clone();

        updateLifespanText();

        //max tower charge is 1.0 unless an effect overrides it
        maxCharge = 1.0f;
        maxCharge = 1.0f;
        if (effects != null)
            if (effects.propertyEffects.maxOvercharge != null)
                maxCharge += effects.propertyEffects.maxOvercharge.Value;

        //if the tower has any everyRound effects, trigger them now
        if (effects != null)
            foreach (IEffect ie in effects.effects)
                if (ie.triggersAs(EffectType.everyRound))
                    ((IEffectInstant)ie).trigger();
    }

    /// <summary>
    /// copies effects from newEffectData and adds them to the ones already present
    /// </summary>
    public void AddEffects(EffectData newEffectData)
    {
        //make sure we have an effectData object to add to
        if (effects == null)
            effects = new EffectData();

        //add the new effects to it
        foreach (IEffect newEffect in newEffectData.effects)
            effects.Add(EffectData.cloneEffect(newEffect));

        //update tooltip text
        UpdateTooltipText();

        updateLifespanText();

        //max tower charge is 1.0 unless an effect overrides it
        maxCharge = 1.0f;
        if (effects != null)
            if (effects.propertyEffects.maxOvercharge != null)
                maxCharge += effects.propertyEffects.maxOvercharge.Value;
    }

    //helper functions to allow calling Upgrade through sendMessage (Unity message handling doesn't understand default parameters)
    public void FreeUpgrade(UpgradeData d) { Upgrade(d, true, true); }
    public void Upgrade(UpgradeData d) { Upgrade(d, false, false); }
    public void UpgradeIgnoreCap(UpgradeData d) { Upgrade(d, true, false); }

    /// <summary>
    /// receives upgrade data and uses it to modify the tower.  If ignoreCap is true, then it can exceed the upgrade cap to do so
    /// </summary>
    public void Upgrade(UpgradeData d, bool ignoreCap, bool noUpgradeCost)
    {
        //ignore if upgrades are forbidden
        if (effects != null)
            if (effects.propertyEffects.upgradesForbidden)
                return;

        //ignore if at the upgrade cap
        if (ignoreCap == false)
            if (upgradeCount >= upgradeCap)
                return;

        //each stat = oldStat * statMult + statMod.
        rechargeTime = rechargeTime * d.rechargeMultiplier + d.rechargeModifier;
        attackPower = attackPower * d.attackMultiplier + d.attackModifier;
        range = range * d.rangeMultiplier + d.rangeModifier;

        //also increase waves
        wavesRemaining += d.waveBonus;

        //set scale of range image
        rangeImage.gameObject.GetComponent<RectTransform>().localScale = new Vector3(range, range, 1.0f);

        //if the upgrade consumes a slot, then count it
        if (noUpgradeCost == false)
            upgradeCount++;

        //update text
        UpdateTooltipText();
        updateLifespanText();
    }

    //helpers to call ShowUpgradeTooltip since unity SendMessage() doesn't understand default parameters
    private void UpgradeTooltip(UpgradeData u) { ShowUpgradeTooltip(u, false); }
    private void FreeUpgradeTooltip(UpgradeData u) { ShowUpgradeTooltip(u, true); }

    /// <summary>
    /// updates the tooltip text to show what the given upgrade would change
    /// </summary>
    private void ShowUpgradeTooltip(UpgradeData u, bool noUpgradeCost)
    {
        //show special message if the target has upgradesForbidden
        if (effects != null)
        {
            if (effects.propertyEffects.upgradesForbidden)
            {
                tooltipText.text = "<color=red>This tower is forbidden from receiving upgrades!</color>";
                return;
            }
        }

        //show special message if the target is at the upgrade cap and the upgrade is not free
        if (noUpgradeCost == false)
        {
            if (upgradeCount >= upgradeCap)
            {
                tooltipText.text = "<color=red>This tower cannot hold any more upgrades!</color>";
                return;
            }
        }

        //tower name does not change
        tooltipText.text = towerName + "\n";

        //we may or may not use an upgrade slot
        if (noUpgradeCost)
            tooltipText.text += upgradeCount + " upgrades <color=lime>+ 0</color>/" + upgradeCap + "\n";
        else
            tooltipText.text += upgradeCount + " upgrades <color=lime>+ 1</color>/" + upgradeCap + "\n";

        //for the others, we need to do some calculations:
        float newAttackPower     = attackPower     * u.attackMultiplier   + u.attackModifier;
        float newRechargeTime    = rechargeTime    * u.rechargeMultiplier + u.rechargeModifier;
        float newRange           = range           * u.rangeMultiplier    + u.rangeModifier;
        float curDPS             = attackPower     / rechargeTime;
        float newDPS             = newAttackPower  / newRechargeTime;
        int   newWavesRemaining  = wavesRemaining  + u.waveBonus;

        float attackChange       = newAttackPower  - attackPower;
        float rechargeChange     = newRechargeTime - rechargeTime;
        float DPSChange          = newDPS          - curDPS;
        float rangeChange        = newRange        - range;
        int wavesRemainingChange = newWavesRemaining - wavesRemaining;

        //now we can update the status text appropriately, with color coding

        //attack
        string colorString = "magenta";
        if (attackChange < 0)
            colorString = "red";
        else if (attackChange > 0)
            colorString = "lime";
        else
            colorString = "white";
        tooltipText.text += attackPower.ToString("F2") + " <color=" + colorString + "> " + attackChange.ToString("+ ####0.##;- ####0.##") + "</color> damage ";

        //charge time
        colorString = "magenta";
        if (rechargeChange < 0)
            colorString = "lime";
        else if (rechargeChange > 0)
            colorString = "red";
        else
            colorString = "white";
        tooltipText.text += "every " + rechargeTime.ToString("F2") + " <color=" + colorString + "> " + rechargeChange.ToString("+ ####0.##;- ####0.##") + "</color> seconds\n";

        //DPS
        colorString = "magenta";
        if (DPSChange < 0)
            colorString = "red";
        else if (DPSChange > 0)
            colorString = "lime";
        else
            colorString = "white";
        tooltipText.text += "(Damage Per Second: " + curDPS + " <color=" + colorString + "> " + DPSChange.ToString("+ ####0.#;- ####0.#") + "</color>)\n";

        //range
        colorString = "magenta";
        if (rangeChange < 0)
            colorString = "red";
        else if (rangeChange > 0)
            colorString = "lime";
        else
            colorString = "white";
        tooltipText.text += "range: " + range + " <color=" + colorString + "> " + rangeChange.ToString("+ ####0.##;- ####0.##") + "</color>\n";

        //waves remaining
        if ( (effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false)) //special display on infinite lifespan
        {
            colorString = "magenta";
            if (wavesRemainingChange < 0)
                colorString = "red";
            else if (wavesRemainingChange > 0)
                colorString = "lime";
            else
                colorString = "white";
            tooltipText.text += "waves remaining: " + wavesRemaining + " <color=" + colorString + "> " + wavesRemainingChange.ToString("+ ####0.##;- ####0.##") + "</color>";
        }
        else
        {
            tooltipText.text += "\nwaves remaining: <color=green>∞</color>";
        }

        if (effects != null)
            foreach (IEffect e in effects.effects)
                if (e.Name != null)
                    tooltipText.text += "\n" + "-" + e.Name;
    }

    /// <summary>
    /// shows new effects on the tooltip
    /// </summary>
    private void NewEffectTooltip (EffectData newEffectData)
    {
        //ignore if the tower cannot be upgraded
        if (effects != null)
            if (effects.propertyEffects.upgradesForbidden)
                return;
        if (upgradeCount >= upgradeCap)
            return;

        tooltipText.text += "<color=lime>";
        foreach (IEffect e in newEffectData.effects)
            if (e.Name != null)
                tooltipText.text += "\n" + "++" + e.Name;
        tooltipText.text += "</color>";
    }

    /// <summary>
    /// called whenever a wave ends.  Updates the lifespan and destroys the tower if it hits zero.
    /// </summary>
    private void WaveOver()
    {
        if ( (effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false)) //dont reduce lifespan if we have infinite lifespan
            wavesRemaining -= 1;

        //if the tower is not dead, trigger everyRound effects
        if (wavesRemaining > 0)
            if (effects != null)
                foreach (IEffect ie in effects.effects)
                    if (ie.triggersAs(EffectType.everyRound))
                        ((IEffectInstant)ie).trigger();

        UpdateTooltipText();
        updateLifespanText();

        if (wavesRemaining == 0)
            onDeath();
    }

    /// <summary>
    /// called when the tower is destroyed.  responsible for death effects
    /// </summary>
    private void onDeath()
    {
        //trigger effects
        if (effects != null)
            foreach (IEffect ie in effects.effects)
                if (ie.triggersAs(EffectType.death))
                    ((IEffectDeath)ie).onTowerDeath(this);

        //destroy self
        Destroy(gameObject);
    }

    /// <summary>
    /// called when an upgrade card is being played. Change the lifespanText to show remaining upgrade slots instead
    /// </summary>
    private void showUpgradeInfo()
    {
        lifespanText.text = (upgradeCap - upgradeCount).ToString();
        if (upgradeCount == upgradeCap)
            lifespanText.color = textColorAmmo;
        else
            lifespanText.color = textColorLifespan;
    }

    /// <summary>
    /// called when the upgrade is done.  Restores text to normal after a previous call to showUpgradeInfo
    /// </summary>
    private void hideUpgradeInfo()
    {
        updateLifespanText();
    }

    //these update text associated with the tower when things change
    private void UpdateTooltipText()
    {
        tooltipText.text =
            towerName + "\n" +
            upgradeCount + "/" + upgradeCap + " upgrades\n" +
            attackPower + " damage every " + rechargeTime.ToString("F2") + " seconds\n" +
            "(" + (attackPower / rechargeTime).ToString("F2") + " per second)\n" +
            "range: " + range;

        if ((effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false)) //special display on infinite lifespan
            tooltipText.text += "\nwaves remaining: " + wavesRemaining;
        else
            tooltipText.text += "\nwaves remaining: <color=green>∞</color>";

        if ((effects != null) && (effects.propertyEffects.limitedAmmo != null)) //skip this section entirely if we have infinite ammo
            tooltipText.text += "\nammo remaining: " + effects.propertyEffects.limitedAmmo;

        if (effects != null)
            foreach (IEffect e in effects.effects)
                if (e.Name != null)
                    tooltipText.text += "\n" + "-" + e.Name;
    }
    private void updateLifespanText()
    {
        //show any stats that indicate when the tower will die
        if ((effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false))
        {
            if ((effects == null) || (effects.propertyEffects.limitedAmmo == null))
                lifespanText.text = wavesRemaining.ToString(); //lifespan only
            else
                lifespanText.text = wavesRemaining.ToString() + "/" + effects.propertyEffects.limitedAmmo.ToString(); //lifespan and ammo
        }
        else
        {
            if ((effects == null) || (effects.propertyEffects.limitedAmmo == null))
                lifespanText.text = "∞"; //tower will not decay
            else
                lifespanText.text = effects.propertyEffects.limitedAmmo.ToString(); //ammo only
        }

        //update text color to reflect tower properties
        if ((effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false))
        {
            if ((effects == null) || (effects.propertyEffects.limitedAmmo == null))
                lifespanText.color = textColorLifespan;
            else
                lifespanText.color = textColorBoth;
        }
        else
        {
            if ((effects == null) || (effects.propertyEffects.limitedAmmo == null))
                lifespanText.color = textColorNeither;
            else
                lifespanText.color = textColorAmmo;
        }
    }
}
