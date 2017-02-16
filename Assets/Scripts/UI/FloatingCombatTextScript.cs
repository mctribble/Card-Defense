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
    public float   damageScale;

    //setting presets for error messages
    public float errorSpeed;
    public Color errorColor;
    public float errorTimeToLive;
    public float errorScale;

    //current status
    private Vector3 velocity;
    private float   timeToLive;

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
    /// <param name="message">message to show</param>
    public void damageText(string message)
    {
        text.text  = message;
        text.color = damageColor;
        velocity   = damageVelocity;
        timeToLive = damageTimeToLive;

        transform.position = transform.position + (spawnPositionVariance * Random.Range(-1.0f, 1.0f));
        transform.localScale *= damageScale;
    }

    /// <summary>
    /// sets up the text to show an error message to the player, travelling in the specified direction
    /// </summary>
    /// <param name="message">message to show</param>
    /// <param name="direction">direction for the message to move.  Should be normalized unless you want the message to travel faster/slower than usual</param>
    public void errorText(string message, Vector2 direction)
    {
        text.text  = message;
        text.color = errorColor;
        velocity   = direction * errorSpeed;
        timeToLive = errorTimeToLive;

        transform.position = transform.position + (spawnPositionVariance * Random.Range(-1.0f, 1.0f));
        transform.localScale *= errorScale;
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
