using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using UnityEngine.UI;

/// <summary>
/// text that floats briefly and disappears for use as a damage indicator
/// </summary>
public class FloatingCombatTextScript : BaseBehaviour
{
    //component reference
    public Text text;

    //spawn settings
    public Vector3 spawnPositionVariance; //max deviation from given spawn position

    //default settings
    public string  defaultMessage;    //text to spawn with
    public Color   defaultColor;      //text color to spawn with
    public Vector2 defaultVelocity;   //velocity to spawn with
    public float   defaultTimeToLive; //TTL to spawn with;

    //setting presets for when something is damaged
    public Vector2 damageVelocity;
    public Color   damageColor;
    public float   damageTimeToLive;

    //current settings
    [Show] private Vector3 velocity;
    [Show] private float   timeToLive;

    //defaults
    private void Awake()
    {
        text.text  = defaultMessage;
        text.color = defaultColor;
        velocity   = defaultVelocity;
        timeToLive = defaultTimeToLive;
    }

    /// <summary>
    /// sets up the text as a damage indicator with the given message
    /// </summary>
    public void damageText(string message)
    {
        text.text  = message;
        text.color = damageColor;
        velocity   = damageVelocity;
        timeToLive = damageTimeToLive;

        transform.position = transform.position + (spawnPositionVariance * Random.Range(-1.0f, 1.0f));
    }


	// Update is called once per frame
	private void Update ()
    {
        transform.position = transform.position + (velocity * Time.deltaTime); //position
        timeToLive -= Time.deltaTime; //lifespan

        //die if time is up
        if (timeToLive <= 0.0f)
            Destroy(gameObject);
	}
}
