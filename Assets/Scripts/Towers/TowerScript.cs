﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

//container passed to enemy to tell them they were damaged
public struct DamageEventData
{
	public float	 	rawDamage;
	public GameObject 	source;
	public GameObject 	dest;
}

//tower class itself
public class TowerScript : MonoBehaviour {

	public GameObject	bulletPrefab;		//prefab to instantiate as a bullet
	public string		towerName;			//name of the tower
	public ushort		upgradeCount;		//number of times this tower has been upgraded
	public float 		rechargeTime;		//time, in seconds, between shots.
	public float		range;				//distance the tower can shoot
	public float		attackPower;		//damage done on hit
    public int          wavesRemaining;     //number of waves this tower has left before disappearing

	public Image		towerImage;			//reference to image for the tower itself
	public Image		rangeImage;			//reference to image for the range overlay
	public Image		buttonImage;		//reference to image for the button object
    public Image        tooltipPanel;       //reference to image for the tooltip background
	public Text			tooltipText;		//reference to text for the tooltip

	private float 		deltaTime;			//time since last frame
	private float 		shotCharge;			//represents how charged the next. 0.0 is empty, 1.0 is full.
	
	private List<GameObject> enemiesInRange; //list of enemies within range of the tower

	// Use this for initialization
	void Awake () {
		//init vars
		enemiesInRange = new List<GameObject>();
		rangeImage.enabled = false;
		upgradeCount = 0;

		//set scale of range image and collider to match range
		rangeImage.gameObject.GetComponent<RectTransform> ().localScale = new Vector3 (range, range, 1.0f);
		GetComponent<CircleCollider2D> ().radius = range;

		//hide tooltip until moused over
		tooltipText.enabled = false;
	}
	
	// Update is called once per frame
	void Update () {
		deltaTime = Time.deltaTime;					//update time since last frame

		//update tooltip position
		if (tooltipText.enabled) {
			//pos
			tooltipPanel.transform.position = Input.mousePosition;

			//pivot
			//default: pivot in lower right
			int x = 1;
			int y = 0;

			//if too close to the left, move pivot to the left
			if (Input.mousePosition.x < tooltipText.preferredWidth) {
				x = 0;
			}

			//if too close to the top, move pivot to the bottom
			if (Input.mousePosition.y > (Screen.height - tooltipText.preferredHeight)) {
				y = 1;
			}

			//set pivot
			tooltipPanel.rectTransform.pivot = new Vector2(x,y);
		}

		shotCharge += deltaTime / rechargeTime;		//increase shot charge

		if (shotCharge > 1.0f) {				//if shot is charged...
			shotCharge = 1.0f;
			if (enemiesInRange.Count != 0) {	//and there is at least one enemy in range...

				//find the closest one and shoot at it
				GameObject closest = null;
				float closestDistance = float.MaxValue;
				Vector3 towerPosition = this.transform.position;

				//search loop: finds closest enemy
				foreach (GameObject e in enemiesInRange){

					//skip dead enemies
					if (e == null)
						continue;

					Vector3 enemyPosition = e.transform.position;
					float distance = Vector3.Distance(towerPosition, enemyPosition);
					if (distance < closestDistance) {
						closest = e;
						closestDistance = distance;
					}
				} //end search loop

				//if no enemies were found, bail
				if (closest == null)
					return;

				//call another function to actually fire
				fire(closest);

			}
		}
		buttonImage.fillAmount = shotCharge;		//update guage


	}

	void TowerMouseEnter(){
		rangeImage.enabled = true;
        tooltipPanel.enabled = true;
		tooltipText.enabled = true;
	}

	//called when the mouse is no longer 
	void TowerMouseExit(){
		rangeImage.enabled = false;
        tooltipPanel.enabled = false;
		tooltipText.enabled = false;
	}

	//called when an enemy first enters range
	void OnTriggerEnter2D(Collider2D coll) {
		if (coll.gameObject.tag.Equals ("Enemy"))
			enemiesInRange.Add (coll.gameObject);
	}

	//called when an enemy is no longer in range
	void OnTriggerExit2D(Collider2D coll){
		if (coll.gameObject.tag.Equals ("Enemy"))
			enemiesInRange.Remove (coll.gameObject);
	}

	//fires on an enemy unit
	void fire(GameObject enemy){
		//create a struct and fill it with data about the attack
		DamageEventData e = new DamageEventData();
		e.rawDamage = attackPower;
		e.source = gameObject;
		e.dest = enemy;

		//create a bullet and send it the data
		GameObject bullet = (GameObject) Instantiate (bulletPrefab, transform.position, Quaternion.identity);
		bullet.SendMessage ("InitBullet", e);

		//also send the data to the enemy directly so it knows what to expect to aid in targeting
		e.dest.SendMessage ("OnExpectedDamage", e);

		//reset for the next shot
		shotCharge = 0.0f;
	}

	//called when an enemy is killed by this tower
	void OnEnemyKilled(GameObject enemy){
		enemiesInRange.Remove (enemy);
	}

	//called when an enemy is expected to die
	void OnEnemyDeath(GameObject enemy){
		if (enemiesInRange != null) //I have no idea why this check has to be here, but it does -*-+9*
			enemiesInRange.Remove (enemy);
	}

	//saves new tower definition data and updates components
	IEnumerator SetData (TowerData d) {
		towerName = d.towerName;
		rechargeTime = d.rechargeTime;
		range = d.range;
		attackPower = d.attackPower;
        wavesRemaining = d.lifespan;

		//set scale of range image and collider to match range

		//yes, I know its awkward, but we're setting the sprite with WWW.  
		WWW www = new WWW ("file:///" + Application.dataPath + "/StreamingAssets/Art/Sprites/" + d.towerSpriteName + ".png");
		yield return www;
		towerImage.sprite = Sprite.Create (www.texture, new Rect (0, 0, www.texture.width, www.texture.height), new Vector2 (0.5f, 0.5f));

		rangeImage.gameObject.GetComponent<RectTransform> ().localScale = new Vector3 (range, range, 1.0f);
		GetComponent<CircleCollider2D> ().radius = range;

		//update tooltip text
		UpdateTooltipText ();
	}

	//receives upgrade data and uses it to modify the tower
	void Upgrade (UpgradeData d) {
		//Debug.Log ("Upgrade");

		//each stat = oldStat * statMult + statMod.
		rechargeTime	= rechargeTime 	* d.rechargeMultiplier 	+ d.rechargeModifier;
		attackPower		= attackPower 	* d.attackMultiplier 	+ d.attackModifier;
		range			= range			* d.rangeMultiplier 	+ d.rangeModifier;

		//set scale of range image and collider to match range
		rangeImage.gameObject.GetComponent<RectTransform> ().localScale = new Vector3 (range, range, 1.0f);
		GetComponent<CircleCollider2D> ().radius = range;

        //count the upgrade
        upgradeCount++;

        //update tooltip text
        UpdateTooltipText ();
	}

	//updates the tooltip text to reflect new values
	void UpdateTooltipText () {
		tooltipText.text = 
			towerName + "\n" + 
			upgradeCount + " upgrades\n" +
			"attack: " + attackPower + "\n" +
			"range: " + range + "\n" +
			"charge time: " + rechargeTime + "\n" +
            "waves remaining: " + wavesRemaining;
	}

    //called whenever a wave ends.  Updates the lifespan and destroys the tower if it hits zero.
    void WaveOver()
    {
        wavesRemaining -= 1;
        UpdateTooltipText();

        if (wavesRemaining == 0)
            Destroy(gameObject);
    }
}
