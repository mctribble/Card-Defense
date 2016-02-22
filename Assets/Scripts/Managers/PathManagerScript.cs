using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

[System.Serializable]
public class PathSegment : System.Object {
	[XmlAttribute] public float startX;
	[XmlAttribute] public float startY;
	[XmlAttribute] public float endX;
	[XmlAttribute] public float endY;
}

public class PathManagerScript : MonoBehaviour {
	
	public static PathManagerScript instance;
	public GameObject segmentPrefab;
	private List<PathSegment> segments;

	// Use this for initialization
	void Awake () {
		instance = this;
	}

	//called after all objects are initialized
	IEnumerator Start () {
		//wait for the level to load
		while (LevelManagerScript.instance.levelLoaded == false)
			yield return null;

		//fetch path segments from the level manager
		segments = LevelManagerScript.instance.Data.pathSegments;

		//spawn the path objects
		SpawnPaths ();
	}

	//calculates a path from position to the player's "base" //TODO: define an actual base, and maybe implement Djikstra or something here
	public List<Vector2> CalculatePathFromPos(Vector2 startPos){

		List<Vector2> result = new List<Vector2> ();

		Vector2 prevPos = startPos;
		foreach (PathSegment segment in segments) {
			if (segment.startX == prevPos.x) {
				if (segment.startY == prevPos.y) {
					prevPos = new Vector2(segment.endX, segment.endY);
					result.Add(prevPos);
				}
			}
		}

		return result;
	}

	// Update is called once per frame
	void Update () {
	
	}

	//spawn the paths
	void SpawnPaths () {
		foreach (PathSegment v in segments) {
			GameObject s = (GameObject) Instantiate(segmentPrefab); //create segment
			s.transform.SetParent(transform); //set this as the parent
			PathSegmentData d = new PathSegmentData( new Vector2 (v.startX, v.startY), new Vector2 (v.endX, v.endY), 0.5f ); //create data struct
			s.SendMessage("Reposition", d); //send data to object
		}
	}
}
