using System.Collections;
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
        if (Application.isWebPlayer)
            loadEnemyTypesWeb();
        else
            yield return StartCoroutine(loadEnemyTypesPC());
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
    /// loads enemy types.  This version is for Web builds, and does not support mods.
    /// </summary>
    private void loadEnemyTypesWeb()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, path);
        using (FileStream stream = new FileStream(filePath, FileMode.Open))
            types = EnemyTypeCollection.Load(stream, filePath);
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
        List<EnemyData> typesToChooseFrom = types.enemyTypes.FindAll(ed => ed.spawnCost < maxSpawnCost);

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
}