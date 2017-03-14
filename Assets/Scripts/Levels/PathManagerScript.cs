using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// XML representation of a path segment
/// </summary>
[System.Serializable]
public class PathSegment : System.Object
{
    [XmlAttribute][Hide] public float startX;
    [XmlAttribute][Hide] public float startY;
    [XmlAttribute][Hide] public float endX;
    [XmlAttribute][Hide] public float endY;

    [Show][XmlIgnore] public Vector2 startPos { get { return new Vector2(startX, startY); } set { startX = value.x; startY = value.y; } }
    [Show][XmlIgnore] public Vector2   endPos { get { return new Vector2(  endX,   endY); } set {   endX = value.x;   endY = value.y; } }

    //provides a human-friendly string for the inspector
    public override string ToString() { return "{" + startX.ToString("F1") + ", " + startY.ToString("F1") + "} -> {" + endX.ToString("F1") + ", " + endY.ToString("F1") + "}"; }
}

/// <summary>
/// spawns all the path segments for the level and handles pathfinding
/// </summary>
public class PathManagerScript : BaseBehaviour
{
    [Hide] public static PathManagerScript instance;
    public GameObject segmentPrefab;
    public GameObject enemyGoalMarkerPrefab;

    private List<PathSegment> segments;

    public const int MAX_PATH_LENGTH = 100; //throws an error if the path result would be longer than this (to catch paths that loop back forever)

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

    /// <summary>
    /// [COROUTINE] called to reset the manager
    /// </summary>
    private IEnumerator Reset()
    {
        //wait for the level to load
        while (LevelManagerScript.instance.levelLoaded == false)
            yield return null;

        //fetch path segments from the level manager
        segments = LevelManagerScript.instance.Data.pathSegments;

        //spawn the path objects
        SpawnPaths();
    }

    /// <summary>
    /// calculates a path from startPos to a goal
    /// </summary>
    /// <param name="startPos">position to start from</param>
    /// <returns>a list of vectors leading to the destination</returns>
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
            throw new System.InvalidOperationException("There is no path connected to the start point!  Check your spawner positions.");

        //if the result is too long, the path probably loops back on itself.  throw error
        if (result.Count > MAX_PATH_LENGTH)
            throw new System.Exception("Path too long!  Make sure the segments defined for this level do not loop back on themselves.");

        return result;
    }

    /// <summary>
    /// calculates all possible paths from startPos to a goal
    /// </summary>
    /// <param name="startPos">position to start from</param>
    /// <returns>a list of vectors leading to the destination</returns>
    public List<List<Vector2>> CalculateAllPathsFromPos(Vector2 startPos, int searchDepth = 0)
    {
        //error if we have hit the path length limit
        if (searchDepth > MAX_PATH_LENGTH)
            throw new System.Exception("Path too long!  Make sure the segments defined for this level do not loop back on themselves.");

        //find all segments leading away from startPos
        List<Vector2> pathCandidates = new List<Vector2>();
        foreach (PathSegment seg in segments.Where(s => s.startPos == startPos))
            pathCandidates.Add(seg.endPos);

        List<List<Vector2>> result = new List<List<Vector2>>(); 

        //special case: no paths lead from here
        if (pathCandidates.Count == 0)
        {
            //if this was the initial call, then there are no paths to return
            if (searchDepth == 0)
                throw new System.InvalidOperationException("There is no path connected to the start point!  Check your spawner positions.");

            //otherwise, this just means we have reached the end of a path.  Return just the position we ended at.
            List<Vector2> path = new List<Vector2>();
            path.Add(startPos);
            result.Add(path);
        }
        else
        {
            //general case: at least one path leads from here.  Return a path for each one.
            
            foreach (Vector2 pc in pathCandidates)
                foreach (List<Vector2> path in CalculateAllPathsFromPos(pc, searchDepth + 1))
                    result.Add(path);

            //unless we are at the initial call, we need to add this position to the front of each path to record how we got there.  The initial call can skip this since the caller already knows where it wanted to start
            if (searchDepth > 0)
                foreach (List<Vector2> path in result)
                    path.Insert(0, startPos);
        }

        return result;
    }

    /// <summary>
    /// spawns the path objects
    /// </summary>
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
                GameObject m = (GameObject) Instantiate(enemyGoalMarkerPrefab);
                m.transform.SetParent(this.transform);
                m.transform.position = new Vector2(v.endX, v.endY);
            }

        }
    }

    /// <summary>
    /// returns whether or not the paths are loaded
    /// </summary>
    public bool pathsLoaded { get { return ( (segments != null) && (segments.Count > 0) ); } }

    /// <summary>
    /// provides a rect in world space that contains all of the paths in the level
    /// </summary>
    public Rect levelBounds
    {
        get
        {
            Rect bounds = new Rect(0.0f, 0.0f, 0.0f, 0.0f);

            foreach(PathSegment s in segments)
            {
                bounds.min = Vector2.Min(bounds.min, Vector2.Min(s.startPos, s.endPos));
                bounds.max = Vector2.Max(bounds.max, Vector2.Max(s.startPos, s.endPos));
            }

            return bounds;
        }
    }
}