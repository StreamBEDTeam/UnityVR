using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StreamBED.Backend.Helper;
using StreamBED.Backend.Models.ProtocolModels;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;

public class TakeScreenshot : MonoBehaviour
{

    public class Image
    {
        private byte[] pixels;
        private string pathName;
        private int width;
        private int height;
        List<string> features;

        public Image(string path, byte[] pix, int w, int h)
        {
            this.pixels = pix;
            this.pathName = path;
            this.height = h;
            this.width = w;
            this.features = new List<string>();
        }

        public string getPath()
        {
            return pathName;
        }

        public byte[] getPixels()
        {
            return pixels;
        }

        public int getW()
        {
            return width;
        }

        public int getH()
        {
            return height;
        }

        public string[] getFeatures()
        {
            return (string[])features.ToArray();
        }
    }

    public static float PERCENTW = .5f; // Percent of the width of the screen that the rectangle occupies
    public static float PERCENTH = .5f; // Percent of the height of the screen that the rectangle occupies
    public RawImage[] recentImages = new RawImage[3]; // The last 3 images which are shown
    public List<ImageWithMetadata> allImages = new List<ImageWithMetadata>(); // All of the images taken, stored in a list
    public RawImage LastImage; // The last image that was taken, which displays immediately after taking the screenshot

    public RectTransform screen; // Canvas
    public string currScene;
    public RectTransform rectangle; // The rectangle that outlines what area is being taken a photo of
    public GameObject confirmButtonPrefab;
    public GameObject deleteButtonPrefab;
    public GameObject featureTogglePrefab;
    public new Camera camera;

    public ImageSerialization imgSer;

    public GameObject[] areasOfInterest;

    public bool pressed = false;

    void Start()
    {
        imgSer = new ImageSerialization();
        Scene currentScene = SceneManager.GetActiveScene();
        currScene = currentScene.name;

        string dataPath = Application.dataPath + "/" + currScene;
        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
        }

        areasOfInterest = defineAreasOfInterest();

        Debug.Log("In Scene " + currScene);
        LastImage.color = new Vector4(0, 0, 0, 0);
        SetRectangle(rectangle);
        DisplayImages(recentImages);
    }

    private GameObject[] defineAreasOfInterest()
    {
        if (currScene == "Scene 1")
        {
            // TODO: Define areasOfInterest for Scene 1
            return new GameObject[0];
        }
        else if (currScene == "Scence 2")
        {
            // TODO: Define areasOfInterest for Scene 2
            return new GameObject[0];
        }
        else
        {
            return new GameObject[0]; // Default is just an empty array
        }
    }

    private void SetRectangle(RectTransform r)
    {
        r.localScale = new Vector2(PERCENTW, PERCENTH);
        //transform.localScale = new Vector2(PERCENT_X, PERCENT_Y);
    }

    public void ChangeWidthSize(RectTransform r)
    {
        if (PERCENTW == .5f)
        {
            PERCENTW = .2f;
        }
        else if (PERCENTW == .2f)
        {
            PERCENTW = .3f;
        }
        else
        {
            PERCENTW = .5f;
        }

        SetRectangle(r);
    }

    public void ChangeHeightSize(RectTransform r)
    {
        if (PERCENTH == .5f)
        {
            PERCENTH = .2f;
        }
        else if (PERCENTH == .2f)
        {
            PERCENTH = .3f;
        }
        else
        {
            PERCENTH = .5f;
        }

        SetRectangle(r);
    }

    public void TakeAShot()
    {
        List<int> ret = new List<int>();
        int currArea = 1;
        foreach (GameObject obj in areasOfInterest) {
            if (IsObjectInViewFinder(obj)) {
                ret.Add(currArea);
                Debug.Log("Object " + obj.name + " is in viewfinder!");
            }
            currArea++;
        }

        Debug.Log(ret.Count);
        StartCoroutine("CaptureMiddle", ret);
    }

    public bool IsObjectInViewFinder(GameObject obj) {
        Vector3[] vfCorners = new Vector3[4];
        rectangle.GetWorldCorners(vfCorners);

        Vector3 taggedObjectCenter = RectTransformUtility.WorldToScreenPoint(
            camera, obj.transform.position);

        float toW = obj.GetComponent<Renderer>().bounds.size.x;
        float toH = obj.GetComponent<Renderer>().bounds.size.y;

        float vfLeftX = vfCorners[0].x;
        float vfRightX = vfCorners[2].x;
        float vfBottomY = vfCorners[0].y;
        float vfTopY = vfCorners[1].y;

        float toLeftX = taggedObjectCenter.x - (toW / 2);
        float toRightX = taggedObjectCenter.x + (toW / 2);
        float toBottomY = taggedObjectCenter.y - (toH / 2);
        float toTopY = taggedObjectCenter.y + (toH / 2);

        bool isObjectContained = toLeftX >= vfLeftX && toRightX <= vfRightX &&
            toBottomY >= vfBottomY && toTopY <= vfTopY;

        return isObjectContained;
    }

    // Captures the desired part of the screen
    // Desired part based on PERCENT_X and PERCENT_Y
    IEnumerator CaptureMiddle(List<int> areaOfInt)
    {
        yield return new WaitForEndOfFrame();
        int w = (int)(Screen.width * PERCENTW);
        int h = (int)(Screen.height * PERCENTH);
        float startX, startY, finishX, finishY;

        if (PERCENTW > 0f && PERCENTW < 1f)
        {
            startX = (.5f - (PERCENTW * .5f)) * Screen.width;
            finishX = w + startX;
        }
        else
        {
            startX = 0;
            finishX = Screen.width;
        }

        if (PERCENTH > 0f && PERCENTH < 1f)
        {
            startY = (.5f - (PERCENTH * .5f)) * Screen.height;
            finishY = h + startY;
        }
        else
        {
            startY = 0;
            finishY = Screen.height;
        }

        Texture2D img = new Texture2D(w, h, TextureFormat.RGB24, false);

        img.ReadPixels(new Rect(startX, startY, finishX, finishY), 0, 0);
        img.Apply();
        // convert to PNG
        byte[] imageBytes = img.EncodeToPNG();
        // Display the image and allow the person to apply features to the fileName and then
        TagImage(imageBytes, w + 10, h + 10, areaOfInt.ToArray());
        Destroy(img);
    }

    IEnumerator ConfirmPhoto(object[] parms)
    {
        pressed = true;
        byte[] imageBytes = (byte[]) parms[0];
        Toggle[] features = (Toggle[]) parms[1];
        Keyword[] keywords = (Keyword[]) parms[2];
        int[] areasOfInterest = (int[]) parms[3];

        string name = "";
        ImageWithMetadata imageValue = new ImageWithMetadata(imageBytes);
        for (int i = 0; i < features.Length; i++)
        {
            if (features[i].isOn)
            {
                string f = features[i].GetComponentInChildren<Text>().text;
                name = name + f + "-";
                imageValue.AddKeyword(keywords[i]);
            }
        }
        string timestamp = System.DateTime.Now.ToString("MM-dd-yyy-HH-mm-ss");
        string fileName = name + timestamp + ".png";
        string pathToSave = Application.dataPath + "/" + currScene;
        int w = (int)(Screen.width * PERCENTW);
        int h = (int)(Screen.height * PERCENTH);
        AddImage(imageValue, w, h);
        DisplayImages(recentImages);
        WWW wait = new WWW(fileName);
        while (!wait.isDone) ;
        yield return wait;
        wait = new WWW(pathToSave);
        Debug.Log(areasOfInterest.Length);
        if (areasOfInterest.Length == 0)
        {
            string path = Application.dataPath + "/" + currScene + "/Other";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (imageValue.Keywords.Count == 0)
            {
                string newPath = path + "/None";
                Debug.Log(newPath);
                if (!Directory.Exists(newPath))
                {
                    Directory.CreateDirectory(newPath);
                }
                newPath += "/" + fileName;
                File.WriteAllBytes(newPath, imageBytes);
            }
            else
            {
                for (int i = 0; i < imageValue.Keywords.Count; i++)
                {
                    string newPath = path + "/" + imageValue.Keywords[i].Content;
                    Debug.Log(newPath);
                    if (!Directory.Exists(newPath))
                    {
                        Directory.CreateDirectory(newPath);
                    }
                    newPath += "/" + fileName;
                    File.WriteAllBytes(newPath, imageBytes);
                }
            }
        }
        else
        {
            foreach (int j in areasOfInterest)
            {
                string path = Application.dataPath + "/" + currScene + "/Area" + j;
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                if (imageValue.Keywords.Count == 0)
                {
                    string newPath = path + "/None";
                    Debug.Log(newPath);
                    if (!Directory.Exists(newPath))
                    {
                        Directory.CreateDirectory(newPath);
                    }
                    newPath += "/" + fileName;
                    File.WriteAllBytes(newPath, imageBytes);
                }
                else
                {
                    for (int i = 0; i < imageValue.Keywords.Count; i++)
                    {
                        string newPath = path + "/" + imageValue.Keywords[i].Content;
                        Debug.Log(newPath);
                        if (!Directory.Exists(newPath))
                        {
                            Directory.CreateDirectory(newPath);
                        }
                        newPath += "/" + fileName;
                        File.WriteAllBytes(newPath, imageBytes);
                    }
                }
            }
        }
        while (!wait.isDone);
        yield return wait;
        pathToSave = Application.dataPath + "/Images/" + fileName;
        wait = new WWW(pathToSave);
        File.WriteAllBytes(pathToSave, imageBytes);
        Debug.Log("Confirmed");
        this.PrintAllImages();
    }

    public void DeletePhoto()
    {
        pressed = true;
        Debug.Log("Deleted");
    }

    public void TagImage(byte[] b, int w, int h, int[] areasOfInterest)
    {
        Debug.Log(areasOfInterest);
        LastImage.rectTransform.sizeDelta = new Vector2(w, h);

        Texture2D imgTexture = new Texture2D(w, h);
        imgTexture.LoadImage(b);
        LastImage.texture = imgTexture;
        //button.onClick.AddListener(() => KeepPhoto());
        object[] param = new object[2][]{ b.Cast<object>().ToArray(), areasOfInterest.Cast<object>().ToArray()};
        StartCoroutine("PickFeatures", param);
    }

    IEnumerator PickFeatures(object[][] param)
    {
        object[] var = param[0];
        byte[] img = var.Cast<byte>().ToArray();
        var = param[1];
        int[] areasOfInterest = var.Cast<int>().ToArray();

        Keyword[] bKey = BankStabilityModel.GetKeywords();
        Keyword[] eKey = EpifaunalSubstrateModel.GetKeywords();
        Keyword[] featureNames = new Keyword[bKey.Length + eKey.Length];
        bKey.CopyTo(featureNames, 0);
        eKey.CopyTo(featureNames, bKey.Length);
        // Add confirm button
        GameObject goButton = Instantiate(confirmButtonPrefab);
        goButton.transform.SetParent(screen, false);
        goButton.transform.localScale = new Vector3(1, 1, 1);
        Button confirmButton = goButton.GetComponent<Button>();

        // Add delete button
        GameObject goButton2 = Instantiate(deleteButtonPrefab);
        goButton2.transform.SetParent(screen, false);
        goButton2.transform.localScale = new Vector3(1, 1, 1);
        Button deleteButton = goButton2.GetComponent<Button>();
        deleteButton.onClick.AddListener(() => DeletePhoto());

        // Add the features
        int len = featureNames.Length;
        GameObject[] featureObjs = new GameObject[len];
        Toggle[] featureToggles = new Toggle[len];
        float distBetween = -1 * (Screen.height / len);
        for (int i = 0; i < len; i++)
        {
            featureObjs[i] = Instantiate(featureTogglePrefab);
            featureObjs[i].transform.SetParent(screen, false);
            featureObjs[i].transform.localScale = new Vector3(1, 1, 1);
            featureObjs[i].transform.localPosition = featureObjs[i].transform.localPosition + new Vector3(0, distBetween * (i + (float).5), 0);
            featureToggles[i] = featureObjs[i].GetComponent<Toggle>();
            featureToggles[i].GetComponentInChildren<Text>().text = featureNames[i].Content;
        }

        object[] parms = new object[4] { img, featureToggles, featureNames, areasOfInterest };
        confirmButton.onClick.AddListener(() => StartCoroutine("ConfirmPhoto", parms));

        LastImage.color = new Vector4(255, 255, 255, 255); // Make visible
        yield return new WaitUntil(() => pressed);
        pressed = false;
        Destroy(confirmButton);
        Destroy(goButton);
        Destroy(deleteButton);
        Destroy(goButton2);
        for (int i = 0; i < len; i++)
        {
            Destroy(featureObjs[i]);
            Destroy(featureToggles[i]);
        }

        LastImage.color = new Vector4(0, 0, 0, 0); // make transparent
    }

    IEnumerator DisplayFeature()
    {
        yield return new WaitForEndOfFrame();
    }

    // Auto populate the list of pictures
    public void AddImage(ImageWithMetadata img, int w, int h)//Image img)
    {
        Texture2D imgTexture = new Texture2D(w, h);
        imgTexture.LoadImage(img.Data);
        if (allImages.Count >= 2)
            recentImages[2].texture = recentImages[1].texture;

        if (allImages.Count >= 1)
            recentImages[1].texture = recentImages[0].texture;

        recentImages[0].texture = imgTexture;
        imgSer.AddImage(img);
        //imgSer.SerializeImage();
        allImages.Add(img);
    }

    public void DisplayImages(RawImage[] recentImages)
    {
        if (allImages.Count > 0)
            recentImages[0].color = new Vector4(255, 255, 255, 255);
        else
            recentImages[0].color = new Vector4(0, 0, 0, 0); // Make transparent if no image

        if (allImages.Count > 1)
            recentImages[1].color = new Vector4(255, 255, 255, 255);
        else
            recentImages[1].color = new Vector4(0, 0, 0, 0); // Make transparent if no image

        if (allImages.Count > 2)
            recentImages[2].color = new Vector4(255, 255, 255, 255);
        else
            recentImages[2].color = new Vector4(0, 0, 0, 0); // Make transparent if no image
    }

    public void PrintAllImages()
    {
        // TODO: Print all images (test to see if allImages is working properly
    }
}
