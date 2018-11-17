using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using StreamBED.Backend.Helper;

public class TakeScreenshot : MonoBehaviour {

    public class Image {
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
            return (string[]) features.ToArray();
        }
    }

    public RectTransform screen;
    public static float PERCENTW = .5f; // Percent of the width of the screen that the rectangle occupies
    public static float PERCENTH = .5f; // Percent of the height of the screen that the rectangle occupies
    public RectTransform rectangle; // The rectangle that outlines what area is being taken a photo of
    public RawImage[] recentImages = new RawImage[3]; // The last 3 images which are shown
    public List<ImageWithMetadata> allImages = new List<ImageWithMetadata>(); // All of the images taken, stored in a list
    public RawImage LastImage; // The last image that was taken, which displays immediately after taking the screenshot

    public GameObject confirmButtonPrefab;
    public GameObject deleteButtonPrefab;
    public GameObject featureTogglePrefab;

    public bool pressed = false;
    
    public void Start()
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
        StartCoroutine("CaptureMiddle");
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
        img.ReadPixels(new Rect(startX, startY, finishX, finishY), 0, 0);
        img.Apply();
        // convert to PNG
        byte[] imageBytes = img.EncodeToPNG();
        // Display the image and allow the person to apply features to the fileName and then 
        string name = TagImage(imageBytes, w + 10, h + 10);
        Object.Destroy(img);
    }

    IEnumerator ConfirmPhoto(object[] parms)
    {
        pressed = true;
        byte[] imageBytes = (byte[]) parms[0];
        Toggle[] features = (Toggle[]) parms[1];
        string name = "";
        ImageWithMetadata imageValue = new ImageWithMetadata(imageBytes);
        for (int i = 0; i < features.Length; i++)
        {
            if (features[i].isOn)
            {
                string f = features[i].GetComponentInChildren<Text>().text;
                name = name + f + "-";
                //Keyword key = new Keyword(f);
                //imageValue.Keywords.Add(key);
            }
        }

        string timestamp = System.DateTime.Now.ToString("MM-dd-yyy-HH-mm-ss");
        string fileName = name + timestamp + ".png";
        string pathToSave = Application.dataPath + "/Images/" + fileName;     
        int w = (int)(Screen.width * PERCENTW);
        int h = (int)(Screen.height * PERCENTH);
        AddImage(imageValue, imageBytes, w, h);
        DisplayImages(recentImages);
        WWW wait = new WWW(fileName);
        while (!wait.isDone) ;
        yield return wait;
        wait = new WWW(pathToSave);
        System.IO.File.WriteAllBytes(pathToSave, imageBytes);
        while (!wait.isDone) ;
        yield return wait;
        Debug.Log("Confirm!");
    }

    public void DeletePhoto()
    {
        pressed = true;
        Debug.Log("Delete!");
    }

    public string TagImage(byte[] b, int w, int h)
    {
        LastImage.rectTransform.sizeDelta = new Vector2(w, h);

        Texture2D imgTexture = new Texture2D(w, h);
        imgTexture.LoadImage(b);
        LastImage.texture = imgTexture;
        //button.onClick.AddListener(() => KeepPhoto());
        Debug.Log("About to add a button");

        StartCoroutine("PickFeatures", b);
        return "";
    }

    IEnumerator PickFeatures(byte[] img)
    {
        string[] featureNames = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J" };
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

        //GameObject feature = Instantiate(featureTogglePrefab);
        //feature.transform.SetParent(screen, false);
        //feature.transform.localScale = new Vector3(1, 1, 1);
        //Toggle featureToggle = feature.GetComponent<Toggle>();

        GameObject[] featureObjs = new GameObject[10];
        Toggle[] featureToggles = new Toggle[10];
        float distBetween = -1 * (Screen.height / 10);
        for (int i = 0; i < 10; i++)
        {
            featureObjs[i] = Instantiate(featureTogglePrefab);
            featureObjs[i].transform.SetParent(screen, false);
            featureObjs[i].transform.localScale = new Vector3(1, 1, 1);
            featureObjs[i].transform.localPosition = featureObjs[i].transform.localPosition + new Vector3(0, distBetween*(i + (float).5), 0);
            featureToggles[i] = featureObjs[i].GetComponent<Toggle>();
            featureToggles[i].GetComponentInChildren<Text>().text = featureNames[i];
        }

        // TODO: Display and add features
        object[] parms = new object[2] { img, featureToggles }; // TODO: Change features to actual keywords
        confirmButton.onClick.AddListener(() => StartCoroutine("ConfirmPhoto", parms));

        LastImage.color = new Vector4(255, 255, 255, 255); // Make visible
        yield return new WaitUntil(() => pressed);
        pressed = false;
        Destroy(confirmButton);
        Destroy(goButton);
        Destroy(deleteButton);
        Destroy(goButton2);
        for (int i = 0; i < 10; i++)
        {
            Destroy(featureObjs[i]);
            Destroy(featureToggles[i]);
        }

        //Destroy(feature);
        LastImage.color = new Vector4(0, 0, 0, 0); // make transparent
    }

    IEnumerator DisplayFeature()
    {
        yield return new WaitForEndOfFrame();
    }

    // Auto populate the list of pictures
    public void AddImage(ImageWithMetadata img, byte[] b, int w, int h)//Image img)
    {
        Texture2D imgTexture = new Texture2D(w, h);
        imgTexture.LoadImage(b);
        //LastImage.texture = imgTexture;
        if (allImages.Count >= 2)
            recentImages[2].texture = recentImages[1].texture;

        if (allImages.Count >= 1)
            recentImages[1].texture = recentImages[0].texture;

        recentImages[0].texture = imgTexture;
        //LastImage.texture = imgTexture;
        //LastImage.color = new Vector4(255, 255, 255, 255);
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
            recentImages[1].color = new Vector4(0,0,0, 0); // Make transparent if no image

        if (allImages.Count > 2)
            recentImages[2].color = new Vector4(255, 255, 255, 255);
        else
            recentImages[2].color = new Vector4(0,0,0, 0); // Make transparent if no image
    }
}
