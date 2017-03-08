using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
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
    [VisibleWhen("isEditor")] public Image          towerImage;
    [VisibleWhen("isEditor")] public Image          rangeImage;
    [VisibleWhen("isEditor")] public Image          rangeImage2;
    [VisibleWhen("isEditor")] public Image          upgradeRangeImage;
    [VisibleWhen("isEditor")] public Image          upgradeRangeImage2;
    [VisibleWhen("isEditor")] public Image          chargeGaugeImage1x;
    [VisibleWhen("isEditor")] public Image          chargeGaugeImage2x;
    [VisibleWhen("isEditor")] public Image          chargeGaugeImage3x;
    [VisibleWhen("isEditor")] public Image          tooltipPanel;
    [VisibleWhen("isEditor")] public Text           tooltipText;
    [VisibleWhen("isEditor")] public Text           lifespanText;
    [VisibleWhen("isEditor")] public ParticleSystem manualFireParticles;
    [VisibleWhen("isEditor")] public Sprite         defaultRangeSprite;
    [VisibleWhen("isEditor")] public Sprite         mouseRangeSprite;
    [VisibleWhen("isEditor")] public Sprite         orthogonalRangeSprite;
    [VisibleWhen("isEditor")] public Sprite         hollowOrthogonalRangeSprite;

    [VisibleWhen("hasData")] private float deltaTime;            //time since last frame
    [VisibleWhen("hasData")] private float shotCharge;           //represents how charged the next shot is. 0.0 is empty, maxCharge is full
    [VisibleWhen("hasData")] private float maxCharge;            //max shot charge (default 1.0)
    [VisibleWhen("hasData")] private float damageDealtThisRound; //amount of damage dealt by this tower this round
    [VisibleWhen("hasData")] private bool  waitingForManualFire; //whether user is being prompted to fire manually

    private bool towerMousedOver; //whether or not the tower is being moused over

    [VisibleWhen("isEditor")] public float tooltipPositionBuffer; //amount of extra space to buffer when positioning tooltips

    //events effects can register to if they need to respond to upgrades in some way
    public delegate void towerUpgradedHandler(TowerScript upgradedTower);
    public event towerUpgradedHandler towerUpgradedEvent;
    public delegate void towerUpgradingHandler(TowerScript hoveredTower, UpgradeData upgrade); //if upgrade is null, that means the player changed their mind and moved away from the tower
    public event towerUpgradingHandler towerUpgradingEvent;

    // Use this for initialization
    private void Awake()
    {
        //init vars
        rangeImage.enabled = false;
        rangeImage2.enabled = false;
        upgradeRangeImage.enabled = false;
        upgradeRangeImage2.enabled = false;
        upgradeCount = 0;
        shotCharge = 0.99f; //not quite full, so that Update() still does stuff that is meant to happen when a shot becomes ready
        maxCharge = 1.0f;
        effects = null;

        //register for event
        LevelManagerScript.instance.RoundStartedEvent += WaveStarted;

        //set scale of range image
        updateRangeImage();

        //hide tooltip until moused over
        tooltipText.enabled = false;

        updateLifespanText();
    }

    /// <summary>
    /// triggers any effects on this tower that are meant to run when the enemy spawns.
    /// </summary>
    public void triggerOnTowerSpawned()
    {
        if (effects != null)
            foreach (IEffect ie in effects.effects)
                if (ie.triggersAs(EffectType.spawn))
                    ((IEffectOnSpawned)ie).onTowerSpawned(this);
    }

    // Update is called once per frame
    private void Update()
    {
        //clean out the effect list every 32 frames
        if (effects != null)
            if ((Time.frameCount % 32) == 0)
                if (effects.cleanEffects())
                    UpdateTooltipText();

        deltaTime = Time.deltaTime; //update time since last frame

        //update tooltip position
        if (tooltipText.enabled)
        {
            //calculate pivot
            //default: pivot in lower right
            int x = 1;
            int y = 0;

            //if too close to the left, move pivot to the left
            if (Input.mousePosition.x < (tooltipText.preferredWidth + tooltipPositionBuffer))
            {
                x = 0;
            }

            //if too close to the top, move pivot to the bottom
            if (Input.mousePosition.y > (Screen.height - tooltipText.preferredHeight - tooltipPositionBuffer))
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

        //decide whether or not to show the range image
        if (towerMousedOver || rangeImage.sprite == mouseRangeSprite)
        {
            rangeImage.enabled = true; //always show if moused over or the tower does mouse targeting
        }
        else if (Input.GetButton("Show Tower Range")) //the show range button might also force the image to show
        {
            //normal towers are only forced to show if their range is less than 100
            if (rangeImage.sprite == defaultRangeSprite)
                rangeImage.enabled = (range < 100);
            else //other range image types always show in response to the button
                rangeImage.enabled = true;
        }
        else
        {
            rangeImage.enabled = false; //if none of the above applies, default to hidden
        }

        //for mouse targeting, make the range image follow the mouse
        if (rangeImage.sprite == mouseRangeSprite)
        {
            if (towerMousedOver)
            {
                //if we are mousing over the tower right now, snap the rangeImage to the tower so that the upgrade ring lines up
                rangeImage.transform.localPosition = Vector3.zero;
            }
            else
            {
                //otherwise, center it on the mouse
                Vector3 mousePositionWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                rangeImage.transform.position = new Vector3(mousePositionWorld.x, mousePositionWorld.y, 0.0f);
            }
        }

        //orthogonal targeting requires two images instead of one.  make the second image match state with the first
        else if (rangeImage.sprite == orthogonalRangeSprite)
            rangeImage2.enabled = rangeImage.enabled;

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

            //if out of ammo, destroy tower
            if (effects != null)
                if (effects.propertyEffects.limitedAmmo != null)
                    if (effects.propertyEffects.limitedAmmo == 0)
                        Destroy(gameObject);
        }
    }

    /// <summary> called when the mouse is over the tower to activate the tooltip </summary>
    private void TowerMouseEnter()
    {
        towerMousedOver = true;
        tooltipPanel.enabled = true;
        tooltipText.enabled = true;
    }

    /// <summary> called when the mouse is over the tower to deactivate the tooltip </summary>
    private void TowerMouseExit()
    {
        towerMousedOver = false;
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
                Destroy(gameObject);
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
                    updateLifespanText();
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
                        //if there is at least one target in range, fire directional shots in four different directions, each targeting enemies in that direction
                        if (targets.Count > 0)
                        {
                            directionalShot(targets.Where(e => e.transform.position.x < this.transform.position.x).ToList(), ded, Vector2.left);
                            directionalShot(targets.Where(e => e.transform.position.x > this.transform.position.x).ToList(), ded, Vector2.right);
                            directionalShot(targets.Where(e => e.transform.position.y < this.transform.position.y).ToList(), ded, Vector2.up);
                            directionalShot(targets.Where(e => e.transform.position.y > this.transform.position.y).ToList(), ded, Vector2.down);
                        }
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
        updateRangeImage();

        //update text
        UpdateTooltipText();

        updateLifespanText();

        //colorize range according to tower color
        rangeImage. color = towerImage.color;
        rangeImage2.color = towerImage.color;
        upgradeRangeImage. color = new Color(1.0f - rangeImage.color.r, 1.0f - rangeImage.color.g, 1.0f - rangeImage.color.b);
        upgradeRangeImage2.color = new Color(1.0f - rangeImage.color.r, 1.0f - rangeImage.color.g, 1.0f - rangeImage.color.b);
    }

    private void SetEffectData(EffectData d)
    {
        effects = d.clone();

        //remove any effects that are not allowed on towers and throw warnings
        effects.removeForbiddenEffects(EffectContext.tower, true);

        updateLifespanText();

        //max tower charge is 1.0 unless an effect overrides it
        maxCharge = 1.0f;
        if (effects != null)
            if (effects.propertyEffects.maxOvercharge != null)
                maxCharge += effects.propertyEffects.maxOvercharge.Value;

        //special handling of particular effect types
        if (effects != null)
        {
            foreach (IEffect ie in effects.effects)
            {
                //trigger every round effects on tower spawn
                if (ie.triggersAs(EffectType.everyRound))
                    ((IEffectInstant)ie).trigger();

                //tell effects about the tower they came from, if they want to know
                if (ie.triggersAs(EffectType.sourceTracked))
                    ((IEffectSourceTracked)ie).effectSource = this;
            }
        }

        //use the range sprite appropriate to the top targeting effect
        switch (effects.topTargetingEffect.XMLName)
        {
            case "targetMouse":
                rangeImage.sprite = mouseRangeSprite;
                break;
            case "targetOrthogonal":
                rangeImage.sprite = orthogonalRangeSprite;
                break;
            default:
                rangeImage.sprite = defaultRangeSprite;
                break;
        }

        //make sure the scale is being set right
        updateRangeImage();

    }

    /// <summary>
    /// copies effects from newEffectData and adds them to the ones already present
    /// </summary>
    public void AddEffects(EffectData newEffectData)
    {
        //make sure we have an effectData object to add to
        if (effects == null)
            effects = new EffectData();

        //for each effect to add
        foreach (IEffect newEffect in newEffectData.effects)
        {
            //apply upgrade effects
            if (newEffect.triggersAs(EffectType.upgrade))
                ((IEffectUpgrade)newEffect).upgradeTower(this);

            //trigger instant effects
            if (newEffect.triggersAs(EffectType.instant))
                if (LevelManagerScript.instance.levelLoaded) //skip them while the level is being loaded so we dont trigger card draws, etc.
                    ((IEffectInstant)newEffect).trigger();

            //do not copy effects that are forbidden on towers
            if (newEffect.forbiddenInContext(EffectContext.tower))
                continue;

            //tell it about the source, if it wants to know
            if (newEffect.triggersAs(EffectType.sourceTracked))
                ((IEffectSourceTracked)newEffect).effectSource = this;

            //add it
            effects.Add(EffectData.cloneEffect(newEffect));
        }

        //update tooltip text
        UpdateTooltipText();

        updateLifespanText();

        //max tower charge is 1.0 unless an effect overrides it
        maxCharge = 1.0f;
        if (effects != null)
            if (effects.propertyEffects.maxOvercharge != null)
                maxCharge += effects.propertyEffects.maxOvercharge.Value;

        //use the range sprite appropriate to the top targeting effect
        switch (effects.topTargetingEffect.XMLName)
        {
            case "targetMouse":
                rangeImage.sprite = mouseRangeSprite;
                rangeImage2.enabled = false;
                break;
            case "targetOrthogonal":
                rangeImage.sprite = orthogonalRangeSprite;
                rangeImage2.enabled = false;
                break;
            default:
                rangeImage.sprite = defaultRangeSprite;
                rangeImage2.enabled = false;
                break;
        }

        //make sure the scale is being set right
        updateRangeImage();
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

        //if this change drops the tower to 0, destroy it
        if (wavesRemaining <= 0)
            Destroy(gameObject);

        //set scale of range image
        updateRangeImage();

        //if the upgrade consumes a slot, then count it
        if (noUpgradeCost == false)
            upgradeCount++;

        //update text
        UpdateTooltipText();
        updateLifespanText();

        //fire event
        if (towerUpgradedEvent != null)
            towerUpgradedEvent(this);
    }

    public void updateRangeImage()
    {
        if (rangeImage.sprite == orthogonalRangeSprite)
        {
            //two straight lines
            rangeImage. transform.localScale = new Vector3(0.3f, range*2, 1.0f);
            rangeImage2.transform.localScale = new Vector3(0.3f, range*2, 1.0f); //rangeImage2 has a 90 degree rotation, so despite the lines going in different directions, they should be scaled the same
        }
        else
        {
            //one circle
            rangeImage.transform.localScale = new Vector3(range, range, 1.0f);
        }
    }

    /// <summary>
    /// updates the tooltip text to show what the given upgrade would change
    /// </summary>
    public void showUpgradeTooltip(UpgradeData u, EffectData newEffectData, bool noUpgradeCost)
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

        //fire event if anything needs to know that an upgrade is being hovered over this tower
        if (towerUpgradingEvent != null)
            towerUpgradingEvent(this, u);

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

        //check for "setRange" effect: it needs special handling
        if (newEffectData != null)
        {
            IEffect setRangeEffect = newEffectData.effects.FirstOrDefault(ie => ie.XMLName == "setRange");
            if (setRangeEffect != null)
                newRange = setRangeEffect.strength;
        }

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
        tooltipText.text += "does " + attackPower.ToString("###0.##") + " <color=" + colorString + "> " + attackChange.ToString("+ ####0.##;- ####0.##") + "</color> damage\n";

        //charge time 
        colorString = "magenta";
        if (rechargeChange < 0)
            colorString = "lime";
        else if (rechargeChange > 0)
            colorString = "red";
        else
            colorString = "white";

        //show this line in a different way depending on how fast the tower is
        if (newRechargeTime > 1.0f)
            tooltipText.text += "attacks every " + rechargeTime.ToString("###0.##") + " <color=" + colorString + "> " + rechargeChange.ToString("+ ####0.##;- ####0.##") + "</color> seconds\n";
        else
            tooltipText.text += "attacks " + (1 / rechargeTime).ToString("###0.##") + " <color=" + colorString + "> " + ((1 / newRechargeTime) - (1 / rechargeTime)).ToString("+ ####0.##;- ####0.##") + "</color> times per second\n";

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

        //also show the upgrade range
        if ( (rangeImage.sprite == orthogonalRangeSprite || (newEffectData != null && newEffectData.containsEffect("targetOrthogonal"))) //if we currently target orthogonally, or if we will gain that effect
              && (newEffectData == null || newEffectData.topTargetingEffect.priority <= TargetingPriority.ORTHOGONAL) ) //AND we will not gain an effect of higher targeting priority
        {
            //we will target orthogonally after the upgrade.  Show the upgrade range as two lines
            upgradeRangeImage. transform.localScale = new Vector3(0.3f, newRange*2, 1.0f);
            upgradeRangeImage2.transform.localScale = new Vector3(0.3f, newRange * 2, 1.0f); //second upgrade range image has a 90 degree rotation, so both lines scale the same
            upgradeRangeImage.sprite = hollowOrthogonalRangeSprite;
            upgradeRangeImage2.enabled = true;
        }
        else
        {
            //we will not target orthogonally after the upgrade.  Show the upgrade range as a circle
            upgradeRangeImage.transform.localScale = new Vector3(newRange, newRange, 1.0f); 
            upgradeRangeImage.sprite = mouseRangeSprite;
            upgradeRangeImage2.enabled = false;
        }

        upgradeRangeImage.enabled = true;

        //effects
        if (newEffectData != null)
        {
            tooltipText.text += "<color=lime>"; //set color

            foreach (IEffect e in newEffectData.effects)                                           //for each effect that would be added
                if (e.Name != null)                                                                //if it has a name
                    if (e.forbiddenInContext(EffectContext.tower) == false)                        //and is not forbidden by towers
                        if ((effects == null) || (effects.additionBlockedByDuplicate(e) == false)) //and is not forbidden because of a duplicate effect already on the tower
                            tooltipText.text += "\n" + "++" + e.Name;                              //list it as an additional effect on the tooltip

            tooltipText.text += "</color>"; //reset color
        }
    }

    /// <summary>
    /// should be called whenever the tower deals damage to something in order to keep track of it for the tooltip
    /// </summary>
    /// <param name="damageDealt">the amount that was dealt</param>
    public void trackDamage(float damageDealt)
    {
        damageDealtThisRound += damageDealt;
        UpdateTooltipText();
    }

    /// <summary>
    /// called whenever a wave begins.  Resets the wave damage counter
    /// </summary>
    private void WaveStarted()
    {
        damageDealtThisRound = 0.0f;
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
            Destroy(gameObject);
    }

    /// <summary>
    /// called when the tower is destroyed.  responsible for death effects
    /// </summary>
    private void OnDestroy()
    {
        //trigger effects
        if (effects != null)
            foreach (IEffect ie in effects.effects)
                if (ie.triggersAs(EffectType.death))
                    ((IEffectDeath)ie).onTowerDeath(this);
    }

    //stops showing the upgrade tooltip.  This updates the tooltip text and fires the Upgrading event with a null upgrade so effects who need to know get notified
    public void cancelUpgradeTooltip()
    {
        UpdateTooltipText();
        if (towerUpgradingEvent != null)
            towerUpgradingEvent(this, null);
    }

    //these update text associated with the tower when things change
    public void UpdateTooltipText()
    {
        tooltipText.text =
            towerName + "\n" +
            upgradeCount + "/" + upgradeCap + " upgrades\n" +
            "does " + attackPower + " damage\n";

        //print this line differently depending on how fast the tower is
        if (rechargeTime > 1.0f)
            tooltipText.text += "attacks every " + rechargeTime.ToString("###0.##") + " seconds\n";
        else
            tooltipText.text += "attacks <color=lime>" + (1 / rechargeTime).ToString("###0.##") + " times per second</color>\n";
        
        tooltipText.text +=
            "(" + (attackPower / rechargeTime).ToString("###0.##") + " per second)\n" +
            "range: " + range;

        if ((effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false)) //special display on infinite lifespan
            tooltipText.text += "\nwaves remaining: " + wavesRemaining;
        else
            tooltipText.text += "\nwaves remaining: <color=green>∞</color>";

        if ((effects != null) && (effects.propertyEffects.limitedAmmo != null)) //skip this section entirely if we have infinite ammo
            tooltipText.text += "\nammo remaining: " + effects.propertyEffects.limitedAmmo;

        tooltipText.text += "\ntotal damage dealt this round: " + damageDealtThisRound.ToString("###0.##");

        //list effects, deferring targeting effects for later
        bool targetingEffectFound = false;
        if (effects != null)
        {
            foreach (IEffect e in effects.effects)
            {
                if (e.Name != null)
                {
                    if (e.triggersAs(EffectType.towerTargeting))
                        targetingEffectFound = true;
                    else
                        tooltipText.text += "\n" + "-" + e.Name;
                }
            }
        }

        //print the targeting effects, if any, in priority order
        if (targetingEffectFound)
        {
            foreach (IEffectTowerTargeting iett in effects.targetingEffects)
            {
                if (iett != EffectTargetDefault.instance) //skip the 'default' placeholder
                {
                    //colorize the targeting effect so that the top one is in a different color
                    if (iett == effects.targetingEffects.ElementAt(0))
                        tooltipText.text += "<color=lime>";
                    else
                        tooltipText.text += "<color=grey>";

                    tooltipText.text += "\n" + "-" + iett.Name; //effect name

                    tooltipText.text += "</color>"; //revert to the normal color
                }
            }
        }

        //disable the upgrade range
        upgradeRangeImage.enabled = false;
        upgradeRangeImage2.enabled = false;
    }
    public void updateLifespanText()
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
