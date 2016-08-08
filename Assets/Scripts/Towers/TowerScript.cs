using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

//tower class itself
public class TowerScript : BaseBehaviour
{
    public GameObject bulletPrefab;    //prefab to instantiate as a bullet
    public GameObject burstShotPrefab; //prefab to instantiate a burst shot

    public Color gaugeColorLifespan; //color of the attack guage when the tower has a limited lifespan
    public Color gaugeColorAmmo;     //color of the attack guage when the tower has limited ammo
    public Color gaugeColorBoth;     //color of the attack guage when the tower has both a limited lifespan and limited ammo
    public Color gaugeColorNeither;  //color of the attack guage when the tower has neither a limited lifespan nor limited ammo

    public string      towerName;      //name of the tower
    public ushort      upgradeCount;   //number of times this tower has been upgraded
    public float       rechargeTime;   //time, in seconds, between shots.
    public float       range;          //distance the tower can shoot
    public float       attackPower;	   //damage done on hit
    public int         wavesRemaining; //number of waves this tower has left before disappearing
    public EffectData  effects;        //effects on this tower

    public Image towerImage;   //reference to image for the tower itself
    public Image rangeImage;   //reference to image for the range overlay
    public Image buttonImage;  //reference to image for the button object
    public Image tooltipPanel; //reference to image for the tooltip background
    public Text  tooltipText;  //reference to text for the tooltip
    public Text  lifespanText; //reference to text that shows the lifespan

    private float deltaTime;            //time since last frame
    private float shotCharge;           //represents how charged the next. 0.0 is empty, 1.0 is full.
    private bool  waitingForManualFire; //whether user is being prompeted to fire manually

    // Use this for initialization
    private void Awake()
    {
        //init vars
        rangeImage.enabled = false;
        upgradeCount = 0;
        effects = null;

        //set scale of range image and collider to match range
        rangeImage.gameObject.GetComponent<RectTransform>().localScale = new Vector3(range, range, 1.0f);
        GetComponent<CircleCollider2D>().radius = range;

        //hide tooltip until moused over
        tooltipText.enabled = false;

        //update gauge color to reflect tower properties
        if ( (effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false) )
        {
            if ((effects == null) || (effects.propertyEffects.limitedAmmo == null))
                buttonImage.color = gaugeColorLifespan;
            else
                buttonImage.color = gaugeColorBoth;
        }
        else
        {
            if ((effects == null) || (effects.propertyEffects.limitedAmmo == null))
                buttonImage.color = gaugeColorNeither;
            else
                buttonImage.color = gaugeColorAmmo;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        deltaTime = Time.deltaTime;                 //update time since last frame

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
                positionOffset.x = 1;
            else
                positionOffset.x = -1;

            if (y == 0)
                positionOffset.y = 1;
            else
                positionOffset.y = -1;

            //set pos
            tooltipPanel.transform.position = Input.mousePosition + positionOffset;
        }

        //increase shot charge if the gauge is not already full
        //it can still overcharge slightly, if the last frame was longer than the remaining charge time
        if (shotCharge < 1.0f)
        {
            shotCharge += deltaTime / rechargeTime;
            buttonImage.fillAmount = shotCharge; //update guage
        }

        //fire gauge fades in and out if we are waiting on the user to fire
        if (waitingForManualFire)
        {
            Color temp = buttonImage.color;
            temp.a = Mathf.PingPong(Time.time, 1);
            buttonImage.color = temp;
        }
            

        //while a shot is charged...
        //(this is a loop because it is technically possible to fire multiple times per frame if the frame took a long time for some reason or the tower fires extremely quickly)
        while (shotCharge > 1.0f)
        {
            //if the tower has the manualFire property effect, flag that we are waiting for the player instead of firing now
            if ((effects != null) && (effects.propertyEffects.manualFire == true))
            {
                waitingForManualFire = true;
                return;
            }

            //call helpers to perform targeting and shooting
            List<GameObject> targets = target();
            bool result = fire(targets);
            if (result == false)
                break; //bail if we failed to fire for any reason
        }
    }

    //called when the mouse is over the tower
    private void TowerMouseEnter()
    {
        rangeImage.enabled = true;
        tooltipPanel.enabled = true;
        tooltipText.enabled = true;
    }

    //called when the mouse is no longer over the tower
    private void TowerMouseExit()
    {
        rangeImage.enabled = false;
        tooltipPanel.enabled = false;
        tooltipText.enabled = false;
    }

    //called when the tower is clicked on
    private void TowerMouseClick()
    {
        if (waitingForManualFire)
        {
            //call helpers to perform targeting and shooting
            List<GameObject> targets = target();
            bool result = fire(targets);
            if (result == false)
                return; //bail if we failed to fire for any reason

            //return the gauge opacity to full
            Color temp = buttonImage.color;
            temp.a = 1.0f;
            buttonImage.color = temp;

            //unflag the wait
            waitingForManualFire = false;
        }
    }

    //returns a list of all the enemies to fire on
    private List<GameObject> target()
    {
        //bail if there are no enemies on the map
        if (EnemyManagerScript.instance.activeEnemies.Count == 0)
            return null;

        //if we are limited by ammo and are out of ammo, bail
        if (effects != null)
            if (effects.propertyEffects.limitedAmmo != null)
                if (effects.propertyEffects.limitedAmmo == 0)
                    return null;

        //get the tower targeting type from effectData.  this uses a helper function for performance reasons
        IEffectTowerTargeting targetingEffect = null;
        if (effects != null)
            targetingEffect = effects.towerTargetingType;
        else
            targetingEffect = EffectTargetDefault.instance; //EffectTargetDefault is a placeholder used when there is no target
        Debug.Assert(targetingEffect != null); //there must always be a targeting effect

        //find the target(s) we are shooting at using the current targeting effect, or the default if there is none
        return targetingEffect.findTargets(transform.position, range);
    }

    //fires on each enemy in the given list, if possible
    private bool fire(List<GameObject> targets)
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

            foreach (GameObject t in targets)
                spawnBullet(t);

            return true; //success
        }
        else
        {
            return false; //failure
        }
    }

    //spawns a bullet to attack an enemy unit
    private void spawnBullet(GameObject enemy)
    {
        //create a struct and fill it with data about the attack
        DamageEventData e = new DamageEventData();
        e.rawDamage = attackPower;
        e.source = gameObject;
        e.dest = enemy;

        //if there are effects on this tower, pass them to the event
        if (effects != null)
            e.effects = effects;
        else
            e.effects = null;

        GameObject bullet = (GameObject) Instantiate (bulletPrefab, transform.position, Quaternion.identity);
        bullet.SendMessage("InitBullet", e);

        //apply attackColor property, if it is present
        if (effects != null)
            if (effects.propertyEffects.attackColor != null)
                bullet.SendMessage("SetColor", effects.propertyEffects.attackColor);
    }

    //fires on all enemy units in range
    private void burstFire(List<GameObject> targets)
    {
        //construct damage event
        DamageEventData e = new DamageEventData();
        e.rawDamage = attackPower;
        e.source = gameObject;
        e.effects = effects;
        
        //construct burst shot event
        BurstShotData data = new BurstShotData();
        data.targetList = targets;
        data.burstRange = range;
        data.damageEvent = e;

        //create a burst shot and give it the data
        GameObject shot = Instantiate(burstShotPrefab); //create it
        shot.transform.position = transform.position;   //center it on the tower
        shot.SendMessage("SetData", data);              //initialize it
    }

    //saves new tower definition data and updates components
    private IEnumerator SetData(TowerData d)
    {
        towerName = d.towerName;
        rechargeTime = d.rechargeTime;
        range = d.range;
        attackPower = d.attackPower;
        wavesRemaining = d.lifespan;

        //set scale of range image and collider to match range

        //yes, I know its awkward, but we're setting the sprite with WWW.
        WWW www = new WWW ("file:///" + Application.dataPath + "/StreamingAssets/Art/Sprites/" + d.towerSpriteName + ".png");
        yield return www;
        towerImage.sprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));

        rangeImage.gameObject.GetComponent<RectTransform>().localScale = new Vector3(range, range, 1.0f);
        GetComponent<CircleCollider2D>().radius = range;

        //update tooltip text
        UpdateTooltipText();
    }

    //sets effect data for this tower
    private void SetEffectData(EffectData d)
    {
        effects = d.clone();

        //update gauge color to reflect tower properties
        if ((effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false))
        {
            if ((effects == null) || (effects.propertyEffects.limitedAmmo == null))
                buttonImage.color = gaugeColorLifespan;
            else
                buttonImage.color = gaugeColorBoth;
        }
        else
        {
            if ((effects == null) || (effects.propertyEffects.limitedAmmo == null))
                buttonImage.color = gaugeColorNeither;
            else
                buttonImage.color = gaugeColorAmmo;
        }
    }

    //adds the new effects to the tower
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

        //update gauge color to reflect tower properties
        if ((effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false))
        {
            if ((effects == null) || (effects.propertyEffects.limitedAmmo == null))
                buttonImage.color = gaugeColorLifespan;
            else
                buttonImage.color = gaugeColorBoth;
        }
        else
        {
            if ((effects == null) || (effects.propertyEffects.limitedAmmo == null))
                buttonImage.color = gaugeColorNeither;
            else
                buttonImage.color = gaugeColorAmmo;
        }
    }

    //receives upgrade data and uses it to modify the tower
    private void Upgrade(UpgradeData d)
    {
        //each stat = oldStat * statMult + statMod.
        rechargeTime    = rechargeTime  * d.rechargeMultiplier  + d.rechargeModifier;
        attackPower     = attackPower   * d.attackMultiplier    + d.attackModifier;
        range           = range         * d.rangeMultiplier     + d.rangeModifier;

        //also increase waves
        wavesRemaining += d.waveBonus;

        //set scale of range image and collider to match range
        rangeImage.gameObject.GetComponent<RectTransform>().localScale = new Vector3(range, range, 1.0f);
        GetComponent<CircleCollider2D>().radius = range;

        //count the upgrade
        upgradeCount++;

        //update tooltip text
        UpdateTooltipText();
    }

    //updates the tooltip text to reflect new values
    private void UpdateTooltipText()
    {
        tooltipText.text =
            towerName + "\n" +
            upgradeCount + " upgrades\n" +
            "attack: " + attackPower + "\n" +
            "charge time: " + rechargeTime + "\n" +
            "Damage Per Second: " + (attackPower / rechargeTime).ToString("F1") + "\n" +
            "range: " + range;

        if ((effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false)) //skip this section entirely if we have infinite lifespan
            tooltipText.text += "\nwaves remaining: " + wavesRemaining;

        if ((effects != null) && (effects.propertyEffects.limitedAmmo != null)) //skip this section entirely if we have infinite ammot
            tooltipText.text += "\nammo remaining: " + effects.propertyEffects.limitedAmmo;

        if (effects != null)
            foreach (IEffect e in effects.effects)
                tooltipText.text += "\n<Color=#" + e.effectType.ToString("X") + ">" + e.Name + "</Color>";

        //show any stats that indicate when the tower will die
        if ((effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false))
        {
            if ((effects == null) || (effects.propertyEffects.limitedAmmo == null))
                lifespanText.text = wavesRemaining.ToString(); //lifespan only
            else
                lifespanText.text = wavesRemaining.ToString() + effects.propertyEffects.limitedAmmo.ToString(); //lifespan and ammo
        }
        else
        {
            if ((effects == null) || (effects.propertyEffects.limitedAmmo == null))
                lifespanText.text = "∞"; //tower will not decay
            else
                lifespanText.text = effects.propertyEffects.limitedAmmo.ToString(); //ammo only
        }
    }

    //updates the tooltip text to show what the given upgrade would change
    private void UpgradeTooltip(UpgradeData u)
    {
        //we already know how these lines change
        tooltipText.text =
            towerName + "\n" +
            upgradeCount + " upgrades <color=lime>+ 1</color>\n";

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
        tooltipText.text += "attack: " + attackPower + " <color=" + colorString + "> " + attackChange.ToString("+ ####0.##;- ####0.##") + "</color>\n";

        //charge time
        colorString = "magenta";
        if (rechargeChange < 0)
            colorString = "lime";
        else if (rechargeChange > 0)
            colorString = "red";
        else
            colorString = "white";
        tooltipText.text += "charge time: " + rechargeTime + " <color=" + colorString + "> " + rechargeChange.ToString("+ ####0.##;- ####0.##") + "</color>\n";

        //DPS
        colorString = "magenta";
        if (DPSChange < 0)
            colorString = "red";
        else if (DPSChange > 0)
            colorString = "lime";
        else
            colorString = "white";
        tooltipText.text += "Damage Per Second: " + curDPS + " <color=" + colorString + "> " + DPSChange.ToString("+ ####0.##;- ####0.##") + "</color>\n";

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
        if ( (effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false)) //skip this section entirely if we have infinite lifespan
        {
            colorString = "magenta";
            if (wavesRemainingChange < 0)
                colorString = "red";
            else if (wavesRemainingChange > 0)
                colorString = "lime";
            else
                colorString = "white";
            tooltipText.text += "waves remaining: " + attackPower + " <color=" + colorString + "> " + wavesRemainingChange.ToString("+ ####0.##;- ####0.##") + "</color>";
        }
    }

    //shows new effects on the tooltip
    private void NewEffectTooltip (EffectData newEffectData)
    {
        foreach (IEffect e in newEffectData.effects)
            tooltipText.text += "\n<Color=#" + e.effectType.ToString("X") + ">+" + e.Name + "</Color>";
    }

    //called whenever a wave ends.  Updates the lifespan and destroys the tower if it hits zero.
    private void WaveOver()
    {
        if ( (effects == null) || (effects.propertyEffects.infiniteTowerLifespan == false)) //dont reduce lifespan if we have infinite lifespan
            wavesRemaining -= 1;

        UpdateTooltipText();

        if (wavesRemaining == 0)
            Destroy(gameObject);
    }
}
