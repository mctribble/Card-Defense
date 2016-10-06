using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Vexe.Runtime.Types;

//contains all the data required to spawn a wave
[System.Serializable]
public class WaveData
{
    //enemy type name, annotated to give a popup in the inspector
    private string[] getEnemyNames() { return EnemyTypeManagerScript.instance.getEnemyNames(); }
    [XmlAttribute][Popup("getEnemyNames",CaseSensitive = true, Filter = true, HideUpdate = true, TextField = true)] public string type;

    //DEV: indicates whether or not this wave was randomly generated.  Random waves are not written back to the file when saving level definitions
    [XmlIgnore][Comment("random waves are not saved to the level file.",helpButton:true)]
    public bool isRandomWave;
    
    [XmlAttribute]    public int    budget;
    [XmlAttribute]    public float  time;
    [XmlAttribute]    public string message;
    [Hide, XmlIgnore] public int    forcedSpawnCount; //if this is negative, no spawn count was forced
    [Hide, XmlIgnore] public int    spawnedThisWave;  //number of enemies in this wave that have already been spawned

    [XmlIgnore] private EnemyData data;
    [XmlIgnore] public  EnemyData enemyData
    {
        get
        {
            if (isSurvivorWave)
                return null;

            if ( (data == null) || (data.name != type) )
                data = EnemyTypeManagerScript.instance.getEnemyTypeByName(type).clone();

            return data;
        }
        set { data = value; }
    }

    [XmlIgnore] private List<GameObject> enemyList;

    //default.  these values are meant to be VERY noticable if a wave is left with default data
    public WaveData()
    {
        type = "Swarm";
        budget = 999999999;
        time = 300.0f;
        message = null;
        forcedSpawnCount = -1;
        spawnedThisWave = 0;
        isRandomWave = false;
    }

    //specific data
    public WaveData(string waveType, int waveBudget, float waveTime)
    {
        type = waveType;
        budget = waveBudget;
        time = waveTime;
        message = null;
        forcedSpawnCount = -1;
        spawnedThisWave = 0;
        isRandomWave = false;
    }

    //survivor wave constructor
    public WaveData(List<GameObject> enemies, int spawnCount, int totalRemainingHealth, float waveTime)
    {
        data = null; //enemy data doesnt apply for survival waves, since they have multiple enemy types
        budget = int.MaxValue; //budget doesnt apply either, so make sure it stands out if used accidentally
        time = waveTime;
        message = null;
        forcedSpawnCount = -1;
        spawnedThisWave = 0;
        enemyList = enemies;
        cachedSpawnCount = enemies.Count;
        isRandomWave = false;
    }

    //returns number of enemies to spawn this wave.  Cache the value to make sure we give the original spawn count, unaltered by anything that happens during the wave
    private int? cachedSpawnCount;
    public int spawnCount
    {
        get
        {
            if (cachedSpawnCount == null)
            {
                int result = 0;

                if (forcedSpawnCount > 0)
                    result = forcedSpawnCount;
                else if (enemyData == null)
                    result = enemyList.Count;
                else
                    result = Mathf.FloorToInt(budget / enemyData.spawnCost);

                if (result < 1)
                {
                    result = 1; //always spawn at least one enemy
                    Debug.LogWarning("Wave spawn count was zero.  forced to spawn 1 monster.");
                }

                cachedSpawnCount = result;
            }

            return cachedSpawnCount.Value;
        }
    }

    //returns the total health of all remaining enemies
    public int totalRemainingHealth
    {
        get
        {
            if (enemyList == null)
                return (spawnCount - spawnedThisWave) * enemyData.maxHealth;
            else
                return enemyList.Sum(x => x.GetComponent<EnemyScript>().curHealth);
        }
    }

    //returns whether or not this is a survivor wave
    public bool isSurvivorWave { get { return enemyList != null; } }

    //used for survivor waves.  reactivates the first survivor on the list and removes it from said list
    public void spawn()
    {
        //error if the enemy list is empty
        if ((enemyList == null) || (enemyList.Count == 0))
        {
            MessageHandlerScript.Error("WaveData can't spawn an enemy because the list is already empty");
            return;
        }

        GameObject e = enemyList.First();
        enemyList.Remove(e);
        e.SetActive(true);
        EnemyManagerScript.instance.EnemySpawned(e);
    }

    //returns a user-friendly string showing the contents of this wave for the debugger
    public override string ToString()
    {
        return type + "(" + spawnCount + ")" + " {Bu: " + budget + " Ti: " + time.ToString("F1") + "}"; 
    }
}

public class EnemyCardScript : BaseBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //references
    public Text       description;
    public GameObject art;
    public Text       title;
    public Image      cardBack;

    //object settings
    public float survivorWaveTime; //time assigned to survivor waves
    public float motionSpeed;
    public float rotationSpeed;
    public float scaleSpeed;
    public Vector2 discardLocation;

    //wave stats
    [Hide] public int spawnCount;           //number of enemies that still need spawning in this wave
    [Hide] public int totalRemainingHealth; //total health of all enemies that still need spawning
    public WaveData wave;                   //wave associated with this card

    //private info
    private State      state;          //state of the FSM
    private Vector2    idleLocation;   //where the card should be when idle
    private Vector2    targetLocation; //where the card currently wants to be
    private GameObject hand;           //reference to parent hand
    private bool       hidden;         //whether or not the card should be hidden offscreen
    private bool       faceDown;       //whether or not the card is face down
    private int        siblingIndex;   //temp storage of this cards proper place in the sibling list, used to restore proper draw order after a card is no longer being moused over
    private string     enemyType;      //name of the enemy type currently depicted.  Cached to detect enemy type changes

    private List<GameObject> survivorList;

    //init
    private void Awake()
    {
        state = State.idle;
        idleLocation = transform.position;
        targetLocation = idleLocation;
        hidden = false;
        faceDown = true;
        cardBack.enabled = true;
        survivorList = null;
    }
  
    //sets the wave
    public void SetWave(WaveData w)
    {
        wave = w;
        description.text = w.enemyData.getDescription();
        foreach (Image i in art.GetComponentsInChildren<Image>())
            i.color = w.enemyData.unitColor.toColor();
        enemyType = w.enemyData.name;
    }

    //sets this up as a survivor wave
    public void SurvivorWave()
    {
        description.text = "These are survivors from the previous round, come to attack again.  Other cards cannot alter this wave.";

        //use a list of surviving enemies to initialize the card, and remove said list from the enemy manager so that it makes a new one for anything that survives this wave instead of putting them back into the same one
        wave = null;
        survivorList = EnemyManagerScript.instance.survivors;
        EnemyManagerScript.instance.survivors = null;

        //search the list to find what we need to finish the setup
        spawnCount = 0;
        totalRemainingHealth = 0;
        int numTypesFound = 0;
        string[] typesFound = new string[6];
        SpriteRenderer[] spritesToSet = new SpriteRenderer[6];

        //error catch: purch survivors that are actually dead
        survivorList.RemoveAll(go => go == null);

        foreach (GameObject go in survivorList)
        {
            EnemyScript e = go.GetComponent<EnemyScript>();
            spawnCount++;
            totalRemainingHealth += e.curHealth;

            if ( (numTypesFound <= 6) && (typesFound.Contains(e.enemyTypeName) == false) )
            {
                typesFound[numTypesFound] = e.enemyTypeName;
                spritesToSet[numTypesFound] = e.enemyImage;
                numTypesFound++;
            }
        }

        //set the sprites
        Image[] artImages = art.GetComponentsInChildren<Image>();
        int spriteIndex = 0;
        foreach(Image i in artImages)
        {
            i.sprite = spritesToSet[spriteIndex].sprite;
            i.color = spritesToSet[spriteIndex].color;
            spriteIndex = (spriteIndex + 1) % numTypesFound;
        }

        //setup the WaveData object
        wave = new WaveData(survivorList, spawnCount, totalRemainingHealth, survivorWaveTime);
    }

    //simple FSM
    private enum State
    {
        idle,
        moving,
        attacking,
        discarding
    }

    //tells the card where it should be idling
    private void SetIdleLocation(Vector2 newIdle)
    {
        idleLocation = newIdle; //update location

        //if card is not hidden or dying, tell it to relocate itself
        if ((hidden == false) && (state != State.discarding))
        {
            state = State.moving;
            targetLocation = idleLocation;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        //update title text (???????x????)
        if (survivorList == null)
            title.text = "<color=#" + wave.enemyData.unitColor.toHex() + ">" + wave.enemyData.name + "</color> x" + (wave.spawnCount - wave.spawnedThisWave);
        else
            title.text = "Survivors x" + (wave.spawnCount - wave.spawnedThisWave);

        //if this is not a survivor wave, and the enemy type changed, update description and art as well
        if ((survivorList == null) && (enemyType != wave.enemyData.name))
        {
            description.text = wave.enemyData.getDescription();
            foreach (Image i in art.GetComponentsInChildren<Image>())
                i.color = wave.enemyData.unitColor.toColor();
            enemyType = wave.enemyData.name;
        }

        //bail early if idle
        if (state == State.idle)
            return;

        //calculate new position
        Vector2 newPosition = Vector2.MoveTowards(transform.localPosition,
                                                  targetLocation,
                                                  motionSpeed * Time.deltaTime);
        //move there
        transform.localPosition = newPosition;

        //go idle or die if reached target
        if (newPosition == targetLocation)
        {
            if (state == State.discarding)
            {
                Destroy(gameObject);
            }
            else
            {
                state = State.idle;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //ignore this event if hidden or discarding
        if (hidden || (state == State.discarding))
            return;

        siblingIndex = transform.GetSiblingIndex(); //save the current index for later
        transform.SetAsLastSibling(); //move to front

        //tell card to move up when moused over
        targetLocation = idleLocation;
        targetLocation.y -= 200;
        state = State.moving;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //ignore this event if hidden or discarding
        if (hidden || (state == State.discarding))
            return;

        transform.SetSiblingIndex(siblingIndex); //restore to old position in the draw order

        //tell card to reset when no longer moused over
        targetLocation = idleLocation;
        state = State.moving;
    }

    //called by the hand to pass a reference to said hand
    private void SetHand(GameObject go)
    {
        hand = go;
    }

    //helper coroutine that simply waits until this card is idle (initial delay of one frame in case the card starts moving in the same frame as this is called)
    public IEnumerator waitForIdle() { yield return null; while (state != State.idle) yield return null; }

    //turns the card to the given quaternion at rotationSpeed degrees/second
    public IEnumerator turnToQuaternion(Quaternion target)
    {
        while (transform.rotation != target)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotationSpeed * Time.deltaTime);
            yield return null;
        }
    }

    //scales the card to the given size over time
    public IEnumerator scaleToVector(Vector3 targetSize)
    {
        while (transform.localScale != targetSize)
        {
            transform.localScale = Vector3.MoveTowards(transform.localScale, targetSize, scaleSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private void Hide()
    {
        //ignore if discarding
        if (state == State.discarding)
            return;

        //cards hide just underneath the center of the screen
        targetLocation.x = 0;
        targetLocation.y = transform.root.GetComponent<RectTransform>().rect.yMax + 200;

        state = State.moving;       //mark this card as in motion
        hidden = true;              //mark this card as hidden
    }

    private void Show()
    {
        //ignore if not hidden
        if (hidden == false)
            return;

        //ignore if discarding
        if (state == State.discarding)
            return;

        //go back to where it was spawned
        targetLocation = idleLocation;
        state = State.moving;

        hidden = false;//clear hidden flag
    }

    //discards this card
    private void Discard()
    {
        state = State.discarding;
        targetLocation = discardLocation;
        hand.SendMessage("Discard", gameObject);
    }

    //card flip helpers
    public void flipOver() { StartCoroutine(flipCoroutine()); } //returns immediately
    public void flipFaceUp() { if (faceDown) flipOver(); } //calls flipOver only if the card is currently face down
    public IEnumerator flipWhenIdle() { yield return waitForIdle(); yield return flipCoroutine(); }

    //main card flip coroutine
    public IEnumerator flipCoroutine()
    {
        Quaternion flipQuaternion = Quaternion.AngleAxis(90, Vector3.up); //rotation to move towards to flip the card at
        faceDown = !faceDown; //flag the flip as complete before it technically even starts to make sure it isn't erroneously triggered again
        yield return StartCoroutine(turnToQuaternion(flipQuaternion)); //turn to the flip position the player doesnt see the back blink in or out of existence
        cardBack.enabled = faceDown; //flip the card
        yield return StartCoroutine(turnToQuaternion(Quaternion.identity)); //turn back to the baseline
        yield break; //done
    }

    //update stats for this wave
    public void updateWaveStats()
    {
        //show the wave message, if there is one, and then blank it out so it only shows once
        if (wave.message != null)
        {
            MessageHandlerScript.ShowNoYield(wave.message);
            wave.message = null;
        }

        //fetch wave stats
        spawnCount = wave.spawnCount;
        totalRemainingHealth = wave.totalRemainingHealth;
    }

    //applies the given effect to the wave, provided it is not a survivor wave
    public void applyWaveEffect(IEffectWave e) { if(wave.isSurvivorWave == false) wave = e.alteredWaveData(wave); } 
}