//based on tutorial found here: http://wiki.unity3d.com/index.php?title=Saving_and_Loading_Data:_XmlSerializer

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;

//maintains the collection of enemy types, including saving/loading to XML
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

    public static EnemyTypeCollection Load(string path)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(EnemyTypeCollection));
        using (var stream = new FileStream(path, FileMode.Open))
        {
            EnemyTypeCollection result = serializer.Deserialize(stream) as EnemyTypeCollection;
            result.filePath = path;
            return result;
        }
    }

    public override string ToString() { return "Enemy Types: (" + enemyTypes.Count + " types)"; }
}

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

    //loads enemy types.  Coroutine because we may have to wait for the dependency manager
    private System.Collections.IEnumerator loadEnemyTypes()
    {
        //wait for the dependency manager to exist before we do this
        while (DependencyManagerScript.instance == null)
            yield return null;

        types = EnemyTypeCollection.Load(Path.Combine(Application.dataPath, path));

        //find the mod files
        DirectoryInfo modDir =  new DirectoryInfo (Path.Combine (Application.dataPath, modPath));   //mod folder
        FileInfo[] modFiles = modDir.GetFiles ("*.xml");                                            //file list

        //load the files
        List<EnemyTypeCollection> modEnemyCollections = new List<EnemyTypeCollection>();
        foreach (FileInfo f in modFiles)
        {
            Debug.Log("found enemy mod file: " + f.Name);
            modEnemyCollections.Add(EnemyTypeCollection.Load(f.FullName));
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
            types.Save(Path.Combine(Application.dataPath, path));
            Debug.Log("Enemy types saved.");
        }  
    }

    //returns a random enemy type from the database, trying to provide one that does not have a spawnCost higher than maxSpawnCost
    public EnemyData getRandomEnemyType(int maxSpawnCost)
    {
        int maxAttempts = types.enemyTypes.Count * 5; //maximum number of times to select another random enemy type looking for one that is not above the max
        int chosenIndex = -1; //index of chosen enemy

        //repeatedly attempt to find an enemy that is within the budget
        for (int i = 0; (chosenIndex == -1) && (i < maxAttempts); i++)
        {
            int candidateIndex = Mathf.RoundToInt(Random.Range(0.0f, types.enemyTypes.Count-1));
            if (types.enemyTypes[candidateIndex].spawnCost <= maxSpawnCost)
                chosenIndex = candidateIndex;
        }

        //we could not find an enemy we could afford, so just pick one at random
        chosenIndex = Mathf.RoundToInt(Random.Range(0.0f, types.enemyTypes.Count-1));

        //return enemy at that index
        return types.enemyTypes[chosenIndex];
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

    //provides a list of enemy type names
    public string[] getEnemyNames()
    {
        List<string> names = new List<string>();

        foreach (EnemyData e in types.enemyTypes)
            names.Add(e.name);

        names.Sort();
        return names.ToArray();
    }
}