using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor; // AssetDatabase in Start()
using StreamBED.Backend.Helper;
using StreamBED.Backend.Models.ProtocolModels;

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
    public RectTransform rectangle; // The rectangle that outlines what area is being taken a photo of
    public GameObject confirmButtonPrefab;
    public GameObject deleteButtonPrefab;
    public GameObject featureTogglePrefab;
    public new Camera camera;

    public GameObject taggedObject;

    public bool pressed = false;

    void Start()
    {
        LastImage.color = new Vector4(0, 0, 0, 0);
        SetRectangle(rectangle);
        DisplayImages(recentImages);
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
        getTaggedObjectInViewFinder();
        StartCoroutine("CaptureMiddle");
    }

    public GameObject getTaggedObjectInViewFinder()
    {
        Vector3[] vfCorners = new Vector3[4];
        rectangle.GetWorldCorners(vfCorners);
        foreach (Vector3 v in vfCorners)
        {
            Debug.Log("Viewfinder corner:" + v.ToString());
        }

        Vector3 taggedObjectCenter = RectTransformUtility.WorldToScreenPoint(camera, taggedObject.transform.position);

        float toW = taggedObject.GetComponent<Renderer>().bounds.size.x;
        float toH = taggedObject.GetComponent<Renderer>().bounds.size.y;

        float vfLeftX = vfCorners[0].x;
        float vfRightX = vfCorners[2].x;
        float vfBottomY = vfCorners[0].y;
        float vfTopY = vfCorners[1].y;

        float toLeftX = taggedObjectCenter.x - (toW / 2);
        float toRightX = taggedObjectCenter.x + (toW / 2);
        float toBottomY = taggedObjectCenter.y - (toH / 2);
        float toTopY = taggedObjectCenter.y + (toH / 2);

        bool isTaggedObjectContained = toLeftX >= vfLeftX && toRightX <= vfRightX && toBottomY >= vfBottomY && toTopY <= vfTopY;

        if (isTaggedObjectContained)
        {
            Debug.Log("TAGGED OBJECT IS CONTAINED!");
            return taggedObject;
        }
        else
        {
            Debug.Log("TAGGED OBJECT IS NOT CONTAINED!");
            return null;
        }
    }

    // Captures the desired part of the screen
    // Desired part based on PERCENT_X and PERCENT_Y
    IEnumerator CaptureMiddle()
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
        Debug.Log("Taking picture of: " + startX + ", " + finishX + ", " + startY + ", " + finishY);
        img.ReadPixels(new Rect(startX, startY, finishX, finishY), 0, 0);
        img.Apply();
        // convert to PNG
        byte[] imageBytes = img.EncodeToPNG();
        // Display the image and allow the person to apply features to the fileName and then 
        string name = TagImage(imageBytes, w + 10, h + 10);
        Destroy(img);
    }

    IEnumerator ConfirmPhoto(object[] parms)
    {
        pressed = true;
        byte[] imageBytes = (byte[]) parms[0];
        Toggle[] features = (Toggle[]) parms[1];
        Keyword[] keywords = (Keyword[]) parms[2];

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
        string pathToSave = Application.dataPath + "/Images/" + fileName;
        int w = (int)(Screen.width * PERCENTW);
        int h = (int)(Screen.height * PERCENTH);
        AddImage(imageValue, w, h);
        DisplayImages(recentImages);
        WWW wait = new WWW(fileName);
        while (!wait.isDone) ;
        yield return wait;
        wait = new WWW(pathToSave);
        System.IO.File.WriteAllBytes(pathToSave, imageBytes);
        while (!wait.isDone) ;
        yield return wait;
        Debug.Log("Confirmed");
        this.PrintAllImages();
    }

    public void DeletePhoto()
    {
        pressed = true;
        Debug.Log("Deleted");
    }

    public string TagImage(byte[] b, int w, int h)
    {
        LastImage.rectTransform.sizeDelta = new Vector2(w, h);

        Texture2D imgTexture = new Texture2D(w, h);
        imgTexture.LoadImage(b);
        LastImage.texture = imgTexture;
        //button.onClick.AddListener(() => KeepPhoto());

        StartCoroutine("PickFeatures", b);
        return "";
    }

    IEnumerator PickFeatures(byte[] img)
    {
        Keyword[] bKey = BankStabilityModel.getKeywords();
        Keyword[] eKey = EpifaunalSubstrateModel.getKeywords();
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
            featureToggles[i].GetComponentInChildren<Text>().text = featureNames[i].GetContent();
        }

        object[] parms = new object[3] { img, featureToggles, featureNames };
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
        imgTexture.LoadImage(img.GetPhoto());
        if (allImages.Count >= 2)
            recentImages[2].texture = recentImages[1].texture;

        if (allImages.Count >= 1)
            recentImages[1].texture = recentImages[0].texture;

        recentImages[0].texture = imgTexture;
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
