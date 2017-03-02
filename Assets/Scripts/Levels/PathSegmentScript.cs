using UnityEngine;
using Vexe.Runtime.Types;

/// <summary>
/// data used to create a path segment
/// </summary>
public struct PathSegmentData
{
    public Vector2 pathStart;
    public Vector2 pathEnd;
    public float   pathWidth;

    public PathSegmentData(Vector2 start, Vector2 end, float width)
    {
        pathStart = start;
        pathEnd   = end;
        pathWidth = width;
    }
}

/// <summary>
/// represents a path segment that has been spawned into the world
/// </summary>
public class PathSegmentScript : BaseBehaviour
{
    public Vector2 pathStart;
    public Vector2 pathEnd;
    public float   pathWidth;

    // Use this for initialization
    private void Start()
    {
        //this does nothing because the path manager must first set the values
    }

    /// <summary>
    /// sets the variables for this object and re-initializes
    /// </summary>
    private void Reposition(PathSegmentData d)
    {
        pathStart = d.pathStart;
        pathEnd = d.pathEnd;
        pathWidth = d.pathWidth;

        Init();
    }

    /// <summary>
    /// positions, rotates, and scales the path to render it from point A to point B
    /// </summary>
    private void Init()
    {
        //calculate middle, length, and angle
        Vector2 midpoint = Vector2.Lerp (pathStart, pathEnd, 0.5f);
        float segmentLength = Vector2.Distance (pathStart, pathEnd) + 0.5f; //small buffer so there are no gaps at the corners
        Vector2 pathVec = pathStart - pathEnd;
        float angle = Vector2.Angle (pathVec, Vector2.right);

        //Vector2.Angle returns closest angle instead of angle in the clockwise direction
        //use the cross product to detect when we have a "counter-clockwise angle" and update the value accordingly
        //(see http://answers.unity3d.com/questions/162177/vector2angles-direction.html)
        if (Vector3.Cross(pathVec, Vector3.right).z > 0.0)
            angle = 360 - angle;

        //clip a little bit off the end to avoid blue arrows pointing into space
        Vector2 clippedMidpoint = Vector2.MoveTowards(midpoint, pathStart, 0.25f);
        float   clippedSegmentLength = segmentLength - 0.5f;

        //position appropriately
        RectTransform rTrans = GetComponent<RectTransform> ();
        rTrans.position = clippedMidpoint;
        rTrans.sizeDelta = new Vector2(clippedSegmentLength, pathWidth);
        rTrans.Rotate(0.0f, 0.0f, angle);

        //update texture UV coords
        Rect temp = this.GetComponent<UnityEngine.UI.RawImage>().uvRect;
        temp.Set(midpoint.x, midpoint.y, clippedSegmentLength, pathWidth);
        this.GetComponent<UnityEngine.UI.RawImage>().uvRect = temp;

        //also scale collider
        BoxCollider2D collider = gameObject.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(segmentLength, pathWidth);
    }
}