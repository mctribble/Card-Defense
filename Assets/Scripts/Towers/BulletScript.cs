using UnityEngine;
using System.Collections;

//container passed to enemy to tell them they were damaged
public struct DamageEventData
{
    public float rawDamage;
    public GameObject source;
    public GameObject dest;
}

public class BulletScript : MonoBehaviour {

	public float speed;				//projectile speed

	private bool initialized; 			//whether or not the bullet is ready for action
	private DamageEventData data;	//details about the attack
	private float deltaTime;		//time since the last frame

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {

		//bail if the bullet is uninitialized
		if (initialized == false)
			return;

		//destroy self if the target is dead
		if (data.dest == null) {
			Destroy(gameObject);
			return;
		}

		deltaTime = Time.deltaTime; //update frame time

		//fetch position of destination object
		Vector2 curDestination = new Vector2 (data.dest.transform.position.x,
		                                      data.dest.transform.position.y);

		//fetch current location
		Vector2 curLocation = new Vector2 (gameObject.transform.position.x, gameObject.transform.position.y); 

		//calculate new location
		Vector2 newLocation = Vector2.MoveTowards (curLocation, curDestination, speed * deltaTime); 

		//if destination is reached, pass data to target and destroy self
		if (newLocation == curDestination) {
			data.dest.SendMessage("OnDamage", data);
			Destroy(gameObject);
		}

		//save position
		gameObject.transform.position = new Vector3(newLocation.x, newLocation.y, gameObject.transform.position.z);

	}

	void InitBullet (DamageEventData newData){
		data = newData;
		initialized = true;
    }
}
