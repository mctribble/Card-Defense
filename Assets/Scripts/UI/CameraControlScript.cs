using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;

/// <summary>
/// handles user input to control the camera
/// </summary>
public class CameraControlScript : BaseBehaviour
{
    public float cameraSpeed;  //speed of the camera
    public Rect  cameraBounds; //region the camera is allowed to move in

    public float zoomSpeed; //camera zoom speed
    public float minSize;   //min size of the camera
    public float maxSize;   //max size of the camera

    private Camera cameraRef;

    //init
    private void Start()
    {
        cameraRef = this.GetComponent<Camera>();
        LevelManagerScript.instance.LevelLoadedEvent += showEntireLevel; //register the level loaded event so that we can move the camera as needed to see everything when it loads
    }

    //allows player to move the camera
	private void Update()
    {
        Vector3 newPos = transform.position; //fetch current position

        //update it from keyboard input
        newPos.x += Input.GetAxis("Horizontal") * Time.deltaTime * cameraSpeed * cameraRef.orthographicSize;
        newPos.y += Input.GetAxis("Vertical")   * Time.deltaTime * cameraSpeed * cameraRef.orthographicSize;

        //clamp it to the bounds
        newPos.x = Mathf.Clamp(newPos.x, cameraBounds.xMin, cameraBounds.xMax);
        newPos.y = Mathf.Clamp(newPos.y, cameraBounds.yMin, cameraBounds.yMax);

        transform.position = newPos; //update the transform

        //zoom controls
        cameraRef.orthographicSize -= Input.GetAxis("Zoom") * Time.deltaTime * zoomSpeed;
        cameraRef.orthographicSize  = Mathf.Clamp(cameraRef.orthographicSize, minSize, maxSize);
    }
	
    ///<summary>
    ///moves/zooms the camera to show the entire level
    ///this is called automatically when a level is loaded.
    ///</summary>
    [Show] public void showEntireLevel()
    {
        if ( (PathManagerScript.instance != null) &&
             (PathManagerScript.instance.pathsLoaded) )
        {
            //get the bounds of the level
            Rect levelBounds = PathManagerScript.instance.levelBounds;

            //center on it
            transform.position = new Vector3(levelBounds.center.x, levelBounds.center.y, transform.position.z);

            //the camera size is set as orthographicSize, which is half the height of the viewing area.  We have to do some math to figure out what size we need

            float WidthAspect = levelBounds.width / cameraRef.aspect;     //this is how high the screen would have to be to contain the entire width of the level
            float minHeight = Mathf.Max(levelBounds.height, WidthAspect); //so the screen has to be at least this big
            float desiredScreenHeight = minHeight + 3.0f;                 //but we'll also give it a little padding so it doesnt look cramped
            cameraRef.orthographicSize = desiredScreenHeight / 2.0f;      //and the orthographic size we set is half of that
        }
    }
}
