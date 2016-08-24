using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;

[System.Serializable]
public class PathSegment : System.Object
{
    [XmlAttribute] public float startX;
    [XmlAttribute] public float startY;
    [XmlAttribute] public float endX;
    [XmlAttribute] public float endY;
}

public class PathManagerScript : BaseBehaviour
{
    [Hide] public static PathManagerScript instance;
    public GameObject segmentPrefab;
    public GameObject enemyGoalMakerPrefab;

    private List<PathSegment> segments;

    public const int MAX_PATH_LENGTH = 30; //throws an error if the path result would be longer than this (to catch paths that loop back forever)

    // Use this for initialization
    private void Awake()
    {
        instance = this;
    }

    //called after all objects are initialized
    private IEnumerator Start()
    {
        //wait for the level to load
        while (LevelManagerScript.instance.levelLoaded == false)
            yield return null;

        //fetch path segments from the level manager
        segments = LevelManagerScript.instance.Data.pathSegments;

        //spawn the path objects
        SpawnPaths();
    }

    //calculates a path from position to the player's "base" //TODO: define an actual base, and maybe implement Djikstra or something here
    public List<Vector2> CalculatePathFromPos(Vector2 startPos)
    {
        List<Vector2> result = new List<Vector2> ();
        Vector2 prevPos = startPos;

        //return a random valid path
        List<Vector2> pathCandidates = new List<Vector2> ();
        do
        {
            //find all valid segments leading away from current position
            pathCandidates.Clear();
            foreach (PathSegment segment in segments)
                if (segment.startX == prevPos.x)
                    if (segment.startY == prevPos.y)
                        pathCandidates.Add(new Vector2(segment.endX, segment.endY));

            //if the list is empty, we are done
            if (pathCandidates.Count == 0)
                break;

            //pick an option at random and take it
            int i = Random.Range(0, pathCandidates.Count);
            result.Add(pathCandidates[i]);
            prevPos = pathCandidates[i];
        } while (result.Count <= MAX_PATH_LENGTH);

        //if the result is empty, there was no path connected to the start.  throw error
        if (result.Count == 0)
            throw new System.Exception("There is no path connected to the start point!  Check your spawner positions.");

        //if the result is too long, the path probably loops back on itself.  throw error
        if (result.Count > MAX_PATH_LENGTH)
            throw new System.Exception("Path too long!  Make sure the segments defined for this level do not loop back on themselves.");

        return result;
    }

    //spawn the paths
    private void SpawnPaths()
    {
        foreach (PathSegment v in segments)
        {
            GameObject s = (GameObject) Instantiate(segmentPrefab); //create segment
            s.transform.SetParent(transform); //set this as the parent
            PathSegmentData d = new PathSegmentData( new Vector2 (v.startX, v.startY), new Vector2 (v.endX, v.endY), 0.5f ); //create data struct
            s.SendMessage("Reposition", d); //send data to object

            //if there is no segment that begins at the same place this segment ends, spawn a marker
            if (segments.Any(x => (x.startX == v.endX) && (x.startY == v.endY)) == false)
            {
                GameObject m = (GameObject) Instantiate(enemyGoalMakerPrefab);
                m.transform.SetParent(this.transform);
                m.transform.position = new Vector2(v.endX, v.endY);
            }

        }
    }
}