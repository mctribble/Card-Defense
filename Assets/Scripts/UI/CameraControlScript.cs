using UnityEngine;
using System.Collections;
using Vexe.Runtime.Types;
using System.IO;

/// <summary>
/// handles user input to control the camera
/// </summary>
public class CameraControlScript : BaseBehaviour
{
    public Canvas UICanvas; //reference to the UI canvas so we can hide it for screenshots

    public float cameraSpeed;  //speed of the camera
    public Rect  cameraBounds; //region the camera is allowed to move in

    public float zoomSpeed; //camera zoom speed
    public float minSize;   //min size of the camera
    public float maxSize;   //max size of the camera

    public float cameraPadding;  //how much padding to add when adjusting the camera to see the entire level
    public string thumbnailPath; //where to save level thumbnails
    public string levelPath;     //where to find level files for timestamp comparison

    private Camera cameraRef;

    //init
    private void Start()
    {
        cameraRef = this.GetComponent<Camera>();
        LevelManagerScript.instance.LevelLoadedEvent += levelLoadedHandler; //register the level loaded event so that we can move the camera as needed to see everything when it loads
    }

    //allows player to move the camera
	private void Update()
    {
        Vector3 newPos = transform.position; //fetch current position

        //update it from keyboard input
        newPos.x += Input.GetAxis("Horizontal") * Time.unscaledDeltaTime * cameraSpeed * cameraRef.orthographicSize;
        newPos.y += Input.GetAxis("Vertical")   * Time.unscaledDeltaTime * cameraSpeed * cameraRef.orthographicSize;

        //clamp it to the bounds
        newPos.x = Mathf.Clamp(newPos.x, cameraBounds.xMin, cameraBounds.xMax);
        newPos.y = Mathf.Clamp(newPos.y, cameraBounds.yMin, cameraBounds.yMax);

        transform.position = newPos; //update the transform

        //zoom controls
        cameraRef.orthographicSize -= Input.GetAxis("Zoom") * Time.unscaledDeltaTime * zoomSpeed;
        cameraRef.orthographicSize  = Mathf.Clamp(cameraRef.orthographicSize, minSize, maxSize);
    }
	
    /// <summary>
    /// called when a level is loaded.  Recenters the camera to show the whole level and also updates the level thumbnail in case something has changed since the last load
    /// TODO: consider removing the thumbnail update when the game releases
    /// </summary>
    private void levelLoadedHandler()
    {
        showEntireLevel();
        StartCoroutine(saveLevelThumbnail());
    }

    ///<summary>
    ///moves/zooms the camera to show the entire level
    ///if there is no level loaded, logs a warning instead
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
            float desiredScreenHeight = minHeight + cameraPadding;                 //but we'll also give it a little padding so it doesnt look cramped
            cameraRef.orthographicSize = desiredScreenHeight / 2.0f;      //and the orthographic size we set is half of that
        }
        else
        {
            Debug.LogWarning("could not showEntireLevel() because there is no level to show or the level has no paths.");
        }
    }

    /// <summary>
    /// forces an update of the level screenshot by deleting the existing one and calling saveLevelThumbnail()
    /// </summary>
    [Show] public void forceUpdateLevelThumbnail()
    {
        //skip if the platform does not support it
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.LogWarning("Could not force update thumbnail: on WebGLPlayer.");
            return;
        }

        //skip if no level is loaded
        if (LevelManagerScript.instance.levelLoaded == false)
        {
            Debug.LogWarning("Could not force update thumbnail: no level loaded.");
            return;
        }

        //update screenshot
        showEntireLevel();
        StartCoroutine(saveLevelThumbnail());
    }

    /// <summary>
    /// takes a screenshot of the level and saves it as a .png with the same name as the level to StreamingAssets\Level Thumbnails
    /// if there is no level loaded, logs a warning instead
    /// </summary>
    public IEnumerator saveLevelThumbnail()
    {
        //skip if the platform does not support it
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            yield break;

        if ( LevelManagerScript.instance.levelLoaded )
        {
            string screenshotName = Path.Combine(Application.streamingAssetsPath, thumbnailPath); //find the folder we're saving to
            screenshotName = Path.Combine(screenshotName, Path.GetFileNameWithoutExtension(LevelManagerScript.instance.data.fileName)); //add the file name of the level
            screenshotName += ".png"; //add the extension

            string levelName = Path.Combine(Path.Combine(Application.streamingAssetsPath, levelPath), LevelManagerScript.instance.data.fileName); //find the level file

            //if the screenshot file already exists, and it is newer than the level file, dont bother taking a screenshot
            if (File.Exists(screenshotName))
            {
                FileInfo screenshotFile = new FileInfo(screenshotName);
                FileInfo levelFile = new FileInfo(levelName);

                if (screenshotFile.LastWriteTime > levelFile.LastWriteTime)
                    yield break;
            }

            //take the screenshot
            UICanvas.enabled = false;
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();
            Application.CaptureScreenshot(screenshotName);
            yield return null;
            UICanvas.enabled = true;
            Debug.Log("level thumbnail updated.");
        }
        else
        {
            Debug.LogWarning("could not saveLevelThumbnail() because there is no level loaded.");
        }
    }
}

