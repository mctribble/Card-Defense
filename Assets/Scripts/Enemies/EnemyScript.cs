using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI; 
using System.Xml.Serialization;

//a Color class that is nicely formatted in xml
[System.Serializable]
public class XMLColor {
	[XmlAttribute] public float r;
	[XmlAttribute] public float g;
	[XmlAttribute] public float b;
	[XmlAttribute] public float a;

	public Color toColor() {
		return new Color (r, g, b, a);
	}
};

//contains everything needed to define an enemy type
[System.Serializable]
public class EnemyData {
	[XmlAttribute] public string 		name;		//used to identify this enemy type
	[XmlAttribute] public int			spawnCost;	//used for wave generation: more expensive enemies spawn in smaller numbers
    [XmlAttribute] public int           damage;     //number of charges knocked off if the enemy reaches the goal
    [XmlAttribute] public int			maxHealth;	//max health
	[XmlAttribute] public float			unitSpeed;	//speed, measured in distance/second
				   public XMLColor		unitColor;	//used to colorize the enemy sprite
};

public class EnemyScript : MonoBehaviour {

	public List<Vector2>	path;				//list of points this unit must go to
	public int 				currentDestination; //index in the path that indicates the current destination
	public EnemyData 		data;				//contains all the data specific to this type of enemy
	public Vector2			startPos;

	private Transform 	parentTransform;	//reference to the transform of this enemy
	public int			curHealth;			//current health
	public int			expectedHealth;		//what health will be after all active shots reach this enemy

	//used for health bar
	public Color	healthyColor;		//color when healthy
	public Color	dyingColor;			//color when near death

	// Use this for initialization
	void Start () {
		//init vars
		curHealth = data.maxHealth; 
		expectedHealth = data.maxHealth;
		parentTransform = GetComponentInParent<Transform> ();
		startPos = parentTransform.position;
	}
	
	// LateUpdate is called once per frame, after other objects have done a regular Update().  We use LateUpdate to make sure bullets get to move first this frame.
	void LateUpdate ()
    {
		Vector2 curLocation = new Vector2 (parentTransform.position.x, parentTransform.position.y); //fetch current location
		Vector2 newLocation = Vector2.MoveTowards (curLocation, path[currentDestination], data.unitSpeed * Time.deltaTime); //calculate new location

		//save position
		parentTransform.position = new Vector3(newLocation.x, newLocation.y, parentTransform.position.z);

		//if reached the current destination, attempt to move to the next one
		if (curLocation == newLocation) {
			currentDestination++;

			if (path.Count == currentDestination) {
                //reached the end.  damage player...
                DeckManagerScript.instance.SendMessage("Damage", data.damage);

				//...and go back to start for another lap
				parentTransform.position = startPos;
				currentDestination = 0;

                //if the enemy is not expected to die, update the enemy list with the new pathing info
                if (expectedHealth > 0)
                    EnemyManagerScript.instance.SendMessage("EnemyPathChanged",gameObject);
			}
		}

		//update health bar fill and color
		Image healthbar = gameObject.GetComponentInChildren<Image> ();
		float normalizedHealth = (float)curHealth / (float)data.maxHealth;
		healthbar.color = Color.Lerp (dyingColor, healthyColor, normalizedHealth);
		healthbar.fillAmount = normalizedHealth;
	}

	//receives damage
	void OnDamage (DamageEventData e) {
		if (curHealth <= 0) {
			//Debug.Log ("SPLEEN!");
			return;
		}

		//take damage
        int damage = Mathf.CeilToInt(e.rawDamage);
        damage = System.Math.Min(damage, curHealth);
        curHealth -= damage;
        LevelManagerScript.instance.WaveTotalRemainingHealth -= damage;

		if (curHealth <= 0)
        {
            //if dead, report the kill to the tower that shot it
            LevelManagerScript.instance.deadThisWave++;
			Destroy (gameObject);
		}
	}
   
	//tracks damage that WILL arrive so that towers dont keep shooting something that is about to be dead
	void OnExpectedDamage (DamageEventData e) {
		//expect to take damage
		expectedHealth -= Mathf.CeilToInt(e.rawDamage);
        
        //if a death is expected, report self as dead so towers ignore this unit
        if (expectedHealth <= 0)
            EnemyManagerScript.instance.SendMessage("EnemyExpectedDeath", gameObject);
    }

	//stores a new path for this unit to follow
	void SetPath (List<Vector2> p) {
		path = p;						//save path
		currentDestination = 0; 	//go towards the first destination
	}

	//stores the data specific to this type of enemy
	void SetData (EnemyData d) {
		data = d;
		this.GetComponent<SpriteRenderer> ().color = d.unitColor.toColor();
	}

    //returns the distance from this enemy's current position to the goal, following its current path
    public float distanceToGoal()
    {
        float result = Vector2.Distance(transform.position, path[currentDestination]); //start with distance to the current destination...
        
        //..and add the length of each subsequent segment
        for (int segment = currentDestination + 1; segment < path.Count; segment++)
            result += Vector2.Distance(path[segment - 1], path[segment]);

        return result;
    }
}
