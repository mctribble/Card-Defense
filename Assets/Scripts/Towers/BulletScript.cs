using UnityEngine;
using Vexe.Runtime.Types;

//container passed to enemy to tell them they were damaged
public struct DamageEventData
{
    public float      rawDamage;
    public EffectData effects;
    public GameObject source;
    public GameObject dest;
}

public class BulletScript : BaseBehaviour
{
    public float speed;             //projectile speed

    private bool initialized;       //whether or not the bullet is ready for action
    private DamageEventData data;   //details about the attack
    private float deltaTime;        //time since the last frame

    // Use this for initialization
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        //bail if the bullet is uninitialized
        if (initialized == false)
            return;

        //destroy self if the target is dead
        if (data.dest == null)
        {
            Destroy(gameObject);
            return;
        }

        deltaTime = Time.deltaTime; //update frame time

        //fetch position of destination object
        Vector2 curDestination = data.dest.transform.position;

        //fetch current location
        Vector2 curLocation = gameObject.transform.position;

        //calculate new location
        Vector2 newLocation = Vector2.MoveTowards (curLocation, curDestination, speed * deltaTime);

        //if destination is reached, trigger effects and pass data to target and destroy self
        if (newLocation == curDestination)
        {
            if (data.effects != null)
                foreach (IEffect i in data.effects.effects)
                    if (i.effectType == EffectType.enemyDamaged)
                        ((IEffectEnemyDamaged)i).actualDamage(ref data);

            data.dest.SendMessage("OnDamage", data);
            Destroy(gameObject);
        }

        //save position
        gameObject.transform.position = new Vector3(newLocation.x, newLocation.y, gameObject.transform.position.z);
    }

    //sets up the bullet data and handles expectedDamage effects
    private void InitBullet(DamageEventData newData)
    {
        //init
        data = newData;
        initialized = true;

        //trigger effects
        if (data.effects != null)
            foreach (IEffect i in data.effects.effects)
                if (i.effectType == EffectType.enemyDamaged)
                    ((IEffectEnemyDamaged)i).expectedDamage(ref data);

        //tell enemy to expect the damage
        data.dest.GetComponent<EnemyScript>().onExpectedDamage(ref data);
    }
}