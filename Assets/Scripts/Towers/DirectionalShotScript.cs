using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

//data required to initialize one of these projectiles
public struct DirectionalShotData
{
    public DamageEventData  damageEvent; //damage event provided by the tower.  dest is ignored.
    public List<GameObject> targetList;  //enemies to be hit
    public Vector2          attackDir;   //max size of the burst animation
}

//projectile intended to hit everything along a straight line
public class DirectionalShotScript : MonoBehaviour
{
    public SpriteRenderer sprite; //reference to the sprite

    public float speed;      //projectile speed
    public float timeToLive; //max lifetime of this projectile
    public Color color;      //projectile color

    private bool                  initialized;  //whether or not this object is ready for action
    private Vector3               attackDir;    //direction the attack is moving
    private List<DamageEventData> damageEvents; //event to construct new damage events from

	// Use this for initialization
	void Awake ()
    {
        initialized = false;
        sprite.color = color;
	}
	
    //initializes the attack
    void SetData (DirectionalShotData data)
    {
        Debug.Log("!");
        attackDir = data.attackDir;
        transform.rotation.SetLookRotation(attackDir); //turn to face the proper direction

        //construct events
        damageEvents = new List<DamageEventData>(data.targetList.Count);

        initialized = true; //flag ready
    }

	// Update is called once per frame
	void Update ()
    {
	    if (initialized)
        {
            //update lifespan and destroy if dead
            timeToLive -= Time.deltaTime;                                 
            if (timeToLive <= 0)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += (attackDir * (speed * Time.deltaTime)); //move

            //TODO: attack anything behind us
            
        }
	}
}
