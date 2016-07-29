using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Types;

//tower class itself
public class TowerScript : BaseBehaviour
{
    public GameObject   bulletPrefab;   //prefab to instantiate as a bullet
    public string       towerName;      //name of the tower
    public ushort       upgradeCount;   //number of times this tower has been upgraded
    public float        rechargeTime;   //time, in seconds, between shots.
    public float        range;          //distance the tower can shoot
    public float        attackPower;	//damage done on hit
    public int          wavesRemaining; //number of waves this tower has left before disappearing
    public EffectData   effects;        //effects on this tower

    public Image        towerImage;     //reference to image for the tower itself
    public Image        rangeImage;     //reference to image for the range overlay
    public Image        buttonImage;	//reference to image for the button object
    public Image        tooltipPanel;   //reference to image for the tooltip background
    public Text         tooltipText;	//reference to text for the tooltip
    public Text         lifespanText;   //reference to text that shows the lifespan

    private float       deltaTime;      //time since last frame
    private float       shotCharge;		//represents how charged the next. 0.0 is empty, 1.0 is full.

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

        //while a shot is charged and at least one enemy in range...
        //(it is technically possible to fire multiple times per frame if the frame took a long time for some reason or the tower fires extremely quickly)
        while (shotCharge > 1.0f)
        {
            //bail if there are no enemies on the map
            if (EnemyManagerScript.instance.activeEnemies.Count == 0)
                break;

            //look for any targeting effects on this tower and use the most recent
            IEffectTowerTargeting targetingEffect = null;
            if (effects != null)
                foreach (IEffect e in effects.effects)
                    if (e.effectType == EffectType.towerTargeting)
                        targetingEffect = (IEffectTowerTargeting)e;

            //find the target(s) we are shooting at using the current targeting effect, or the default if there is none
            List<GameObject> targets;
            if (targetingEffect != null)
                targets = targetingEffect.findTargets(transform.position, range);
            else
                targets = defaultTargeting();

            //call another function to actually fire on each valid target, if there are any
            if ((targets != null) && (targets.Count != 0))
            {
                //reduce charge meter (if the gauge was overcharged, retain the excess)
                shotCharge -= 1.0f;

                foreach (GameObject t in targets)
                    fire(t);
            }
            else
            {
                break; //no targets
            }
        }
    }

    private void TowerMouseEnter()
    {
        rangeImage.enabled = true;
        tooltipPanel.enabled = true;
        tooltipText.enabled = true;
    }

    //called when the mouse is no longer
    private void TowerMouseExit()
    {
        rangeImage.enabled = false;
        tooltipPanel.enabled = false;
        tooltipText.enabled = false;
    }

    //fires on an enemy unit
    private void fire(GameObject enemy)
    {
        //create a struct and fill it with data about the attack
        DamageEventData e = new DamageEventData();
        e.rawDamage = attackPower;
        e.source = gameObject;
        e.dest = enemy;

        //if there are enemyDamaged effects on this tower, pass them to the bullet
        if (effects != null)
            e.effects = effects;
        else
            e.effects = null;

        //create a bullet and send it the data
        GameObject bullet = (GameObject) Instantiate (bulletPrefab, transform.position, Quaternion.identity);
        bullet.SendMessage("InitBullet", e);
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
        effects = d;
    }

    //adds the new effects to the tower
    public void AddEffects(EffectData newEffectData)
    {
        //make sure we have an effectData object to add to
        if (effects == null)
            effects = new EffectData();

        //add the new effects to it
        foreach (IEffect newEffect in newEffectData.effects)
            effects.effects.Add(newEffect);

        //update tooltip text
        UpdateTooltipText();
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
            "range: " + range + "\n" +
            "waves remaining: " + wavesRemaining;

        if (effects != null)
            foreach (IEffect e in effects.effects)
                tooltipText.text += "\n<Color=#" + e.effectType.ToString("X") + ">" + e.Name + "</Color>";

        lifespanText.text = wavesRemaining.ToString();
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
        colorString = "magenta";
        if (wavesRemainingChange < 0)
            colorString = "red";
        else if (wavesRemainingChange > 0)
            colorString = "lime";
        else
            colorString = "white";
        tooltipText.text += "waves remaining: " + attackPower + " <color=" + colorString + "> " + wavesRemainingChange.ToString("+ ####0.##;- ####0.##") + "</color>";
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
        wavesRemaining -= 1;
        UpdateTooltipText();

        if (wavesRemaining == 0)
            Destroy(gameObject);
    }

    //placeholder used in case the default targeting behavior changes
    private List<GameObject> defaultTargeting()
    {
        return EnemyManagerScript.instance.enemiesInRange(transform.position, range, 1);
    }
}
