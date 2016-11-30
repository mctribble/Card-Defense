//this is for persisting file operations in WebGL builds.  See http://answers.unity3d.com/questions/1095407/saving-webgl.html for more info
//but the gist is, this is a handler that makes sure changes from regular IO calls get through to the user's hard drive
//(more direct link here: http://amalgamatelabs.com/Blog/2016/Data_Persistence/, but I trust unity3d to hold onto the info more than I do a random blog)

var HandleIO = 
{
    SyncFiles : function()
    {
        FS.syncfs(false,function (err) {
            // handle callback
        });
    }
};

mergeInto(LibraryManager.library, HandleIO);