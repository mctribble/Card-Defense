//based on tutorial found here: http://wiki.unity3d.com/index.php?title=Saving_and_Loading_Data:_XmlSerializer

using System.Collections.Generic;
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

    public void Save(string path)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(EnemyTypeCollection));

        using (StreamWriter stream = new StreamWriter(path, false, Encoding.GetEncoding("UTF-8")))
        {
            serializer.Serialize(stream, this);
        }
    }

    public static EnemyTypeCollection Load(string path)
    {
        XmlSerializer serializer = new XmlSerializer(typeof(EnemyTypeCollection));
        using (var stream = new FileStream(path, FileMode.Open))
        {
            return serializer.Deserialize(stream) as EnemyTypeCollection;
        }
    }

    public override string ToString() { return "Enemy Types: (" + enemyTypes.Count + " types)"; }
}

public class EnemyTypeManagerScript : BaseBehaviour
{
    //Manager settings.  only shown in the editor since they dont need editing at runtime
    public bool shouldShowSettings() { return !Application.isPlaying; }
    [VisibleWhen("shouldShowSettings")] public static EnemyTypeManagerScript instance; //singleton instance
    [VisibleWhen("shouldShowSettings")] public string path;                            //path of base game enemies
    [VisibleWhen("shouldShowSettings")] public string modPath;                         //path of modded enemies

    //collection of all enemy types.  Only shown if loaded
    public bool areTypesLoaded() { return (types != null && types.enemyTypes.Count > 0); }
    [VisibleWhen("areTypesLoaded")] public EnemyTypeCollection types;

    //set ALL THREE of these to true to save any debugger enemy data changes back to the XML.  Only shows in-game.
    [VisibleWhen("areTypesLoaded")] public bool saveEnemyChanges;
    [VisibleWhen("saveEnemyChanges")] public bool reallySaveEnemyChanges;
    [VisibleWhen("reallySaveEnemyChanges")] public bool reallyReallySaveEnemyChanges;

    // Use this for initialization
    private void Awake()
    {
        instance = this;
        types = EnemyTypeCollection.Load(Path.Combine(Application.dataPath, path));

        //integrate mod files
        EnemyTypeCollection modTypes;                                                               //temp storage of mod enemy
        DirectoryInfo modDir =  new DirectoryInfo (Path.Combine (Application.dataPath, modPath));   //mod folder
        FileInfo[] modFiles = modDir.GetFiles ("*.xml");                                            //file list

        foreach (FileInfo f in modFiles)
        {
            modTypes = EnemyTypeCollection.Load(f.FullName); //load file

            Debug.Log("Loading enemy file: " + f.Name); //log it
            foreach (EnemyData moddedEnemy in modTypes.enemyTypes)
            {
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
    }

    //called prior to the first frame
    private void Start()
    {
        //parse effects on all enemy types so we throw a warning immediately if one uses an unidentified effect
        foreach (EnemyData i in types.enemyTypes)
            if (i.effectData != null)
                i.effectData.parseEffects();
    }

    // Update is called once per frame
    private void Update()
    {
        //allows making enemy type changes from the editor and saving them back to the xml file
        if (saveEnemyChanges && reallySaveEnemyChanges && reallyReallySaveEnemyChanges)
        {
            types.Save(Path.Combine(Application.dataPath, path));
            saveEnemyChanges = false;
            reallySaveEnemyChanges = false;
            reallyReallySaveEnemyChanges = false;
            Debug.Log("Enemy changes saved.");
        }
    }

    //returns a random enemy type from the database, trying to provide one that does not have a spawnCost higher than maxSpawnCost
    public EnemyData getRandomEnemyType(int maxSpawnCost)
    {
        int maxAttempts = types.enemyTypes.Count * 3; //maximum number of times to select another random enemy type looking for one that is not above the max
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
}