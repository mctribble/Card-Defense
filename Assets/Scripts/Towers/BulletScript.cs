using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// contains details about an attack:
/// rawDamage: damage to deal
/// effects: any effects applied to this attack.
/// source: reference to the attacking tower
/// dest: reference to the target enemy
/// </summary>
public struct DamageEventData
{
    public float      rawDamage;
    public EffectData effects;
    public TowerScript source;
    public EnemyScript dest;
}

public class BulletScript : BaseBehaviour
{
    public float  speed;            //projectile speed
    public Color  color;            //default color to use for the bullet

    public SpriteRenderer spriteRenderer; //component reference

    private bool initialized = false; //whether or not the bullet is ready for action
    private DamageEventData data;     //details about the attack
    private EnemyScript enemyRef;     //reference to the EnemyScript attached to the target

    //set default color when spawned
    private void Start() { spriteRenderer.color = color; }

    //overrides the default color
    public void SetColor(Color newColor)
    {
        color = newColor;
        spriteRenderer.color = newColor;
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

        //fetch position of destination object
        Vector2 curDestination = data.dest.transform.position;

        //fetch current location
        Vector2 curLocation = gameObject.transform.position;

        //calculate new location
        Vector2 newLocation = Vector2.MoveTowards (curLocation, curDestination, speed * Time.deltaTime);

        //turn in direction of travel ('forward' and 'up' intentionally switched, since we technically want the object to 'look' towards the screen)
        transform.localRotation = Quaternion.LookRotation(Vector3.forward, curLocation - newLocation); 

        //if the enemy is in the brief "final chance" state between reaching the goal and dealing damage, blink instantaneously to the target.  This way, enemies wont escape just because projectile speed is low.
        //TODO: visual lightning-bolt effect or something for this
        if (enemyRef.goalFinalChance)
            newLocation = curDestination;

        //if destination is reached, trigger effects and pass data to target and destroy self
        if (newLocation == curDestination)
        {
            //trigger effects
            if (data.effects != null)
            {
                foreach (IEffect i in data.effects.effects)
                {
                    if (i.triggersAs(EffectType.enemyDamaged))
                    {
                        float damageBefore = data.rawDamage;
                        ((IEffectEnemyDamaged)i).actualDamage(ref data);

                        //warn if damage amount changed in .actualDamage(), as this causes hard-to-find bugs.  anything that changes amount of damage done should happen in expectedDamage()
                        if (damageBefore != data.rawDamage)
                            Debug.LogWarning("damage amount altered in .actualDamage() call of " + i.XMLName + "!");
                    }
                }
            }

            enemyRef.onDamage(data);
            Destroy(gameObject);
        }

        //save position
        gameObject.transform.position = new Vector3(newLocation.x, newLocation.y, gameObject.transform.position.z);
    }

    //sets up the bullet data and handles expectedDamage effects
    public void InitBullet(DamageEventData newData)
    {
        //init
        data = newData;
        enemyRef = data.dest.GetComponent<EnemyScript>();

        //trigger effects
        if (data.effects != null)
            foreach (IEffect i in data.effects.effects)
                if (i.triggersAs(EffectType.enemyDamaged))
                    ((IEffectEnemyDamaged)i).expectedDamage(ref data);

        //tell enemy to expect the damage
        enemyRef.onExpectedDamage(ref data);

        initialized = true;
    }

    //destroys self immediately if the target is e
    public void AbortAttack(GameObject e)
    {
        if (data.dest == e)
            Destroy(gameObject);
    }
}