﻿using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// maintains the collection of enemy types, including saving/loading to XML
/// </summary>
[XmlRoot("EnemyTypes")]
[System.Serializable]
public class EnemyTypeCollection
{
    //list of different enemy types
    [XmlArray("Enemies")]
    [XmlArrayItem("Enemy")]
    [Display(Seq.GuiBox | Seq.PerItemDuplicate | Seq.PerItemRemove)]
    public List<EnemyData> enemyTypes = new List<EnemyData>();

    //the file name this collection was populated from.  For use in error reporting
    [XmlIgnore] public string filePath { get; set; }
    [XmlIgnore] public string fileName { get { return Path.GetFileNameWithoutExtension(filePath); } }

    //comma separated list of enemy mod files that this enemy file is dependent on
    [XmlAttribute("enemyFileDependencies")][DefaultValue("")][Hide] public string dependencies;

    /// <summary>
    /// saves this collection to the given location.  Modded enemies are not included, since they are likely defined elsewhere
    /// </summary>
    public void Save(string path)
    {
        //temporarily remove modded enemies
        List<EnemyData> temp = new List<EnemyData>(enemyTypes);
        enemyTypes.RemoveAll(ed => ed.isModded);

        XmlSerializer serializer = new XmlSerializer(typeof(EnemyTypeCollection));

        using (StreamWriter stream = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
        {
            serializer.Serialize(stream, this);
        }

        //restore the list to normal
        enemyTypes = temp;
    }

    /// <summary>
    /// returns a new EnemyTypeCollection created from the given XML file.
    /// note that the stream is NOT disposed!
    /// filePath is stored in the resulting object
    /// </summary>
    public static EnemyTypeCollection Load(Stream stream, string filePath)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(EnemyTypeCollection));
        EnemyTypeCollection result = serializer.Deserialize(stream) as EnemyTypeCollection;
        result.filePath = filePath;
        return result;
    }

    public override string ToString() { return "Enemy Types: (" + enemyTypes.Count + " types)"; }
}

/// <summary>
/// handles saving/loading of enemy types and provides several utility functions for finding enemy types
/// </summary>
public class EnemyTypeManagerScript : BaseBehaviour
{
    //Manager settings.  only shown in the editor since they dont need editing at runtime
    private bool shouldShowSettings() { return !Application.isPlaying; }
    [VisibleWhen("shouldShowSettings")] public static EnemyTypeManagerScript instance; //singleton instance
    [VisibleWhen("shouldShowSettings")] public string path;                            //path of base game enemies
    [VisibleWhen("shouldShowSettings")] public string modPath;                         //path of modded enemies

    //collection of all enemy types.  Only shown if loaded
    public bool areTypesLoaded() { return (types != null && types.enemyTypes.Count > 0); }
    [VisibleWhen("areTypesLoaded")] public EnemyTypeCollection types;

    //private dictionary of enemy sprites so we only have to load them once
    private Dictionary<string, Sprite> enemySprites = new Dictionary<string, Sprite>();

    // Use this for initialization
    private void Awake()
    {
        instance = this;
        StartCoroutine(loadEnemyTypes());
    }

    /// <summary>
    /// [COROUTINE] loads enemy types.  Coroutine because we may have to wait for the dependency manager.  Works for any supported platform.
    /// </summary>
    /// <returns></returns>
    private IEnumerator loadEnemyTypes()
    {
        //delegate to load types based on blatforms
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            yield return StartCoroutine(loadEnemyTypesWeb());
        else
            yield return StartCoroutine(loadEnemyTypesPC());

        //load-time processing of enemy types
        foreach (EnemyData ed in types.enemyTypes)
        {
            //remove any effects forbidden in this context, and throw warnings about them
            if (ed.effectData != null)
                ed.effectData.removeForbiddenEffects(EffectContext.enemyCard, true);

            //load the sprites for these enemy types
            yield return StartCoroutine(loadEnemySprite(ed.spriteName));
        }
    }

    /// <summary>
    /// [COROUTINE] loads enemy types.  Coroutine because we may have to wait for the dependency manager.  This version is for PC builds.
    /// </summary>
    private IEnumerator loadEnemyTypesPC()
    {
        //wait for the dependency manager to exist before we do this
        while (DependencyManagerScript.instance == null)
            yield return null;

        string filePath = Path.Combine(Application.streamingAssetsPath, path);
        using (FileStream stream = new FileStream(filePath, FileMode.Open))
            types = EnemyTypeCollection.Load(stream, filePath);

        //find the mod files
        DirectoryInfo modDir =  new DirectoryInfo (Path.Combine (Application.streamingAssetsPath, modPath));   //mod folder
        FileInfo[] modFiles = modDir.GetFiles ("*.xml");                                            //file list

        //load the files
        List<EnemyTypeCollection> modEnemyCollections = new List<EnemyTypeCollection>();
        foreach (FileInfo f in modFiles)
        {
            Debug.Log("found enemy mod file: " + f.Name);
            using (FileStream stream = new FileStream(f.FullName, FileMode.Open))
                modEnemyCollections.Add(EnemyTypeCollection.Load(stream, f.FullName));
        }

        //get the dependency manager to sort/cull the list
        modEnemyCollections = DependencyManagerScript.instance.handleEnemyFileDependencies(modEnemyCollections);

        foreach (EnemyTypeCollection modTypes in modEnemyCollections)
        {
            foreach (EnemyData moddedEnemy in modTypes.enemyTypes)
            {
                //mark the definition as modded
                moddedEnemy.isModded = true;

                //find the existing version of this enemy
                EnemyData existingEnemy = null;
                foreach (EnemyData baseEnemy in types.enemyTypes)
                {
                    if (baseEnemy.name == moddedEnemy.name)
                    {
                        existingEnemy = baseEnemy;
                        break;
                    }
                }

                //replace the enemy if it exists already, and add it if it doesnt
                if (existingEnemy != null)
                {
                    types.enemyTypes.Remove(existingEnemy);
                    types.enemyTypes.Add(moddedEnemy);
                    Debug.Log("Overwriting enemy: " + existingEnemy.name);
                }
                else {
                    types.enemyTypes.Add(moddedEnemy);
                }
            }
        }

        yield break;
    }

    /// <summary>
    /// [COROUTINE] loads enemy types.  This version is for Web builds, and does not support mods.
    /// </summary>
    private IEnumerator loadEnemyTypesWeb()
    {
        //form the web request
        string filePath = Application.streamingAssetsPath + '/' + path;
        WWW request = new WWW(filePath);

        //wait for the request to load
        yield return request;

        //show error if there was one
        if (request.error != null)
        {
            Debug.LogError("Error loading enemy types:\n" + request.error);
            yield break;
        }

        //or, if we were successful, create a new stream and fill it with the contents of the web request:
        using (MemoryStream enemyTypesStream = new MemoryStream())    //create the stream
        {
            StreamWriter writer = new StreamWriter(enemyTypesStream); //used to write to it
            writer.Write(request.text);                               //write contents of the request
            writer.Flush();                                           //make sure it gets processed
            enemyTypesStream.Position = 0;                            //send the stream back to the start

            //now we can finally load the enemy types
            types = EnemyTypeCollection.Load(enemyTypesStream, filePath);
        }

        Debug.Log(types.enemyTypes.Count + " enemy types loaded.");
    }

    //called prior to the first frame
    private void Start()
    {
        //parse effects on all enemy types so we throw a warning immediately if one uses an unidentified effect
        foreach (EnemyData i in types.enemyTypes)
            if (i.effectData != null)
                i.effectData.parseEffects();
    }

    //DEV: provides a button in the inspector to save enemy type definitions
    [Show][VisibleWhen("areTypesLoaded")] private System.Collections.IEnumerator saveEnemyChanges()
    {
        yield return StartCoroutine(MessageHandlerScript.PromptYesNo("Are you sure you want to overwrite the enemy definitions?"));
        if (MessageHandlerScript.responseToLastPrompt == "Yes")
        {
            types.Save(Path.Combine(Application.streamingAssetsPath, path));
            Debug.Log("Enemy types saved.");
        }  
    }

    /// <summary>
    /// returns a random enemy type from the database, trying to provide one that does not have a spawnCost higher than maxSpawnCost
    /// if no enemy type that is cheap enough is found after many attempts, returns any random enemy type
    /// </summary>
    public EnemyData getRandomEnemyType(int maxSpawnCost)
    {
        //choose only enemies we can afford
        List<EnemyData> typesToChooseFrom = types.enemyTypes.FindAll(ed => ed.baseSpawnCost < maxSpawnCost);

        //if that list is empty, then search all of them so we can at least return something meaningful
        if (typesToChooseFrom.Count == 0)
            typesToChooseFrom = types.enemyTypes;

        //pick a random item on the list
        int index = Mathf.RoundToInt(Random.Range(0.0f, typesToChooseFrom.Count-1));

        //return that enemy type
        return typesToChooseFrom[index];
    }

    //returns the enemy type with the given name
    public EnemyData getEnemyTypeByName(string nameToFind)
    {
        foreach (EnemyData t in types.enemyTypes)
        {
            if (string.Equals(nameToFind, t.name, System.StringComparison.OrdinalIgnoreCase))
            {
                return t;
            }
        }

        throw new KeyNotFoundException("Enemy type not found: " + nameToFind);
    }

    /// <summary>
    /// provides a sorted array of all valid enemy type names
    /// </summary>
    public string[] getEnemyNames()
    {
        List<string> names = new List<string>();

        foreach (EnemyData e in types.enemyTypes)
            names.Add(e.name);

        names.Sort();
        return names.ToArray();
    }

    /// <summary>
    /// if a sprite with the given name has not been loaded yet, loads that sprite.  
    /// This should be called at least once for each sprite before calling getEnemySprite()
    /// loadEnemyTypes() handles calls this once for each enemy in the collection
    /// </summary>
    /// <param name="spriteName"></param>
    /// <returns></returns>
    private IEnumerator loadEnemySprite(string spriteName)
    {
        //bail if it is already loaded
        if (enemySprites.ContainsKey(spriteName))
            yield break;

        //yes, I know its awkward, but we're setting the sprite with WWW, even on PC
        Sprite enemySprite;
        string spritePath = "";
        if (Application.platform != RuntimePlatform.WebGLPlayer)
            spritePath = "file:///";
        spritePath += Application.streamingAssetsPath + "/Art/Sprites/" + spriteName;
        WWW www = new WWW (spritePath);
        yield return www;

        if (www.error == null)
        {
            //load the texture, but force mipmapping
            Texture2D rawTex = www.texture;
            Texture2D newTex = new Texture2D(rawTex.width, rawTex.height, rawTex.format, true);
            www.LoadImageIntoTexture(newTex);

            //use it to make a sprite   
            enemySprite = Sprite.Create(newTex, new Rect(0, 0, newTex.width, newTex.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogWarning("Failed to load enemy sprite " + spriteName + ": " + www.error);
            enemySprite = Resources.Load<Sprite>("Sprites/Error");
        }

        //store the sprite
        enemySprites.Add(spriteName, enemySprite);
    }

    /// <summary>
    /// returns the sprite to be used for this type of enemy.  Maintains an internal list so all instances of the enemy can share one sprite
    /// if that sprite has not been loaded yet, returns the error sprite.
    /// </summary>
    /// <param name="enemyName"></param>
    /// <returns></returns>
    public Sprite getEnemySprite(string spriteName)
    {
        //attempt to fetch the sprite
        Sprite enemySprite = null;
        bool   spriteFound = enemySprites.TryGetValue(spriteName, out enemySprite);

        //bail if the sprite has not been loaded yet
        if (spriteFound == false)
        {
            Debug.LogWarning("Attempted to get an enemy sprite that hasnt been loaded yet! (" + spriteName + ")");
            return Resources.Load<Sprite>("Sprites/Error");
        }
        else
            return enemySprite;
    }
}