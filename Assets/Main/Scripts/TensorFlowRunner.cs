using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;
using TensorFlowLite;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System;
using Unity.XR.CoreUtils;

public class TensorFlowRunner : MonoBehaviour
{
    [SerializeField]
    private ARCameraManager arCameraManager;

    [SerializeField]
    private SSD.Options options;

    [SerializeField]
    private GameObject framePrefab;

    [SerializeField]
    private AspectRatioFitter frameContainer = null;

    [SerializeField, Range(0f, 1f)]
    private float scoreThreshold = 0.5f;

    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    private ARRaycastManager raycastManager;

    [SerializeField]
    private GameObject objectPrefab;

    [SerializeField]
    private TextAsset labelMap = null;
    
    private SSD ssd;
    private GameObject[] frames;
    private GameObject activeObject;
    private Texture2D cameraTexture;
    private string[] labels;

    private NativeArray<byte> cameraImageBytes;

    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

    private Dictionary<int, Color> classColors = new Dictionary<int, Color>();
    private const int totalClasses = 80;

    // 初期化処理
    private void Start()
    {
        ssd = new SSD(options);
        if (ssd == null)
        {
            Debug.LogError("Failed to create SSD");
        }

        if (arCameraManager != null)
        {
            arCameraManager.frameReceived += OnCameraFrameReceived;
        }
        else
        {
            Debug.LogError("ARCameraManager is not set");
        }

        // フレームの初期化
        frames = new GameObject[10];
        Transform parent = frameContainer.transform;
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = Instantiate(framePrefab, Vector3.zero, Quaternion.identity, parent);
            frames[i].transform.localPosition = Vector3.zero;
        }

        // ラベルの初期化
        labels = labelMap.text.Split('\n');
        GenerateClassColors(totalClasses);
    }

    // リソースのクリーンアップ
    private void OnDestroy()
    {
        if (arCameraManager != null)
        {
            arCameraManager.frameReceived -= OnCameraFrameReceived;
        }
        ssd?.Dispose();
    }

    private void GenerateClassColors(int numClasses)
    {
        for (int i = 0; i < numClasses; i++)
        {
            float hue = (float)i / numClasses;
            Color classColor = Color.HSVToRGB(hue, 0.8f, 0.8f);
            classColors[i] = classColor;
        }
    }


    // カメラフレームを取得し、画像を回転させてTensorFlowへ渡す
    private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
    {
        if (arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage cpuImage))
        {
            using (cpuImage)
            {
                var conversionParams = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(0, 0, cpuImage.width, cpuImage.height),
                    outputDimensions = new Vector2Int(cpuImage.width, cpuImage.height),
                    outputFormat = TextureFormat.RGBA32,
                    transformation = XRCpuImage.Transformation.MirrorY
                };

                int size = cpuImage.GetConvertedDataSize(conversionParams);
                if (!cameraImageBytes.IsCreated || cameraImageBytes.Length != size)
                {
                    if (cameraImageBytes.IsCreated)
                    {
                        cameraImageBytes.Dispose();
                    }
                    cameraImageBytes = new NativeArray<byte>(size, Allocator.Persistent);
                }

                cpuImage.Convert(conversionParams, cameraImageBytes);

                // `Texture2D`の初期化と更新
                if (cameraTexture == null || cameraTexture.width != cpuImage.width || cameraTexture.height != cpuImage.height)
                {
                    cameraTexture = new Texture2D(cpuImage.width, cpuImage.height, TextureFormat.RGBA32, false);
                }
                cameraTexture.LoadRawTextureData(cameraImageBytes);
                cameraTexture.Apply();
            }

            // 画像を90度回転させて処理
            Texture2D rotatedTexture = RotateImage(cameraTexture, 90);
            ProcessTensorFlow(rotatedTexture);
        }
    }

    // テクスチャの画像を90度回転
    private static Color32[] RotateSquare(Color32[] sourcePixels, double angleRadians, Texture2D tex)
    {
        int width = tex.width;
        int height = tex.height;
        int centerX = width / 2;
        int centerY = height / 2;
        Color32[] rotatedPixels = new Color32[width * height];

        double sn = Math.Sin(angleRadians);
        double cs = Math.Cos(angleRadians);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int newX = (int)(cs * (x - centerX) + sn * (y - centerY) + centerX);
                int newY = (int)(-sn * (x - centerX) + cs * (y - centerY) + centerY);

                if (newX >= 0 && newX < width && newY >= 0 && newY < height)
                {
                    rotatedPixels[y * width + x] = sourcePixels[newY * width + newX];
                }
                else
                {
                    rotatedPixels[y * width + x] = new Color32(0, 0, 0, 0);
                }
            }
        }
        return rotatedPixels;
    }

    // テクスチャ画像を指定の角度で回転
    public static Texture2D RotateImage(Texture2D tex, int angleDegrees)
    {
        Color32[] sourcePixels = tex.GetPixels32();
        double angleRadians = Math.PI / 180 * angleDegrees;
        Color32[] rotatedPixels = RotateSquare(sourcePixels, angleRadians, tex);

        tex.SetPixels32(rotatedPixels);
        tex.Apply();

        return tex;
    }

    public void ProcessTensorFlow(Texture texture)
    {
        ssd.Run(texture);

        SSD.Result[] results = ssd.GetResults();
        Vector2 size = (frameContainer.transform as RectTransform).rect.size;

        Vector2 ratio;
        if (texture.width >= texture.height)
        {
            ratio = new Vector2(1.0f, texture.height / (float)texture.width);
        }
        else
        {
            ratio = new Vector2(texture.width / (float)texture.height, 1.0f);
        }

        for (int i = 0; i < frames.Length; i++)
        {
            UpdateFrame(frames[i], results[i], size * ratio);
        }
    }

    private void UpdateFrame(GameObject frame, SSD.Result result, Vector2 size)
    {
        // スコアがしきい値未満の場合はラベルを非表示
        if (result.score < scoreThreshold)
        {
            frame.gameObject.SetActive(false);
            return;
        }


        // 子オブジェクトにあるすべてのImageコンポーネントを取得
        Image[] images = frame.GetComponentsInChildren<Image>();
        Text[]  labels = frame.GetComponentsInChildren<Text>();

        // アンカーポジションとサイズを設定
        var frameRect = frame.GetComponent<RectTransform>();
        var boxRect   = images[2].rectTransform;

        frameRect.anchoredPosition = result.rect.position * size - size * 0.5f;
        frameRect.sizeDelta        = result.rect.size * size;
        // boxRect.sizeDelta          = result.rect.size * size;

        // テキストの色を設定
        labels[0].color = Color.white;
        labels[1].color = Color.white;

        // フォントサイズを自動調整
        int originalFontSize = labels[0].fontSize;
        const float padding = 10f;
        while (labels[0].preferredWidth > frameRect.sizeDelta.x - padding * 2 && labels[0].fontSize > 10)
        {
            labels[0].fontSize--;
            labels[1].fontSize--;
        }

        // オブジェクトの配置
        RectTransform leftTopRect = frameRect.GetComponent<RectTransform>();
        RectTransform centerRect = CreateNewTargetRectTransform(frameRect.parent);
        SetRectTransformToCenter(leftTopRect, centerRect);

        if (activeObject != null)
        {
            Destroy(activeObject);
        }

        Vector2 screenPosition = GetScreenPositionFromRectTransform(centerRect);

        // スクリーン座標からオブジェクトを配置
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 10f))
        {
            float distance = Vector3.Distance(arCamera.transform.position, hit.point);
            labels[0].text = $" {GetLabelName(result.classID)} {(int)(result.score * 100)}% {distance*100:F2} cm";
            labels[1].text = $" ";
            // activeObject = Instantiate(objectPrefab, raycastHits[0].pose.position, raycastHits[0].pose.rotation);
        }
        else
        {
            labels[0].text = $" {GetLabelName(result.classID)} : {(int)(result.score * 100)}%";
            labels[1].text = "";
        }


        if (classColors.TryGetValue(result.classID, out Color boxColor))
        {
            labels[0].rectTransform.sizeDelta = new Vector2((result.rect.size * size).x, labels[0].fontSize * 2);
            images[0].rectTransform.sizeDelta = new Vector2((result.rect.size * size).x, labels[0].fontSize * 2);
            images[0].color = boxColor;
            images[1].color = boxColor;
            images[2].color = boxColor;
        }
        // フォントサイズを元に戻す
        labels[0].fontSize = originalFontSize;
        labels[1].fontSize = originalFontSize;

        frame.gameObject.SetActive(true);
    }

    // RectTransformからスクリーン座標を取得
    private Vector2 GetScreenPositionFromRectTransform(RectTransform rectTransform)
    {
        Vector3 worldPosition = rectTransform.TransformPoint(Vector3.zero);
        return RectTransformUtility.WorldToScreenPoint(arCamera, worldPosition);
    }

    // クラスIDからラベル名を取得
    private string GetLabelName(int id)
    {
        if (id < 0 || id >= labels.Length - 1)
        {
            return "?";
        }
        return labels[id + 1];
    }

    private void SetRectTransformToCenter(RectTransform source, RectTransform target)
    {
        Vector2 centerPosition = source.anchoredPosition + new Vector2(source.sizeDelta.x / 2, -source.sizeDelta.y / 2);
        target.anchoredPosition = centerPosition;
        target.sizeDelta = source.sizeDelta;
    }

    private RectTransform CreateNewTargetRectTransform(Transform parent)
    {
        GameObject newGameObject = new GameObject("TargetRectTransform");
        newGameObject.transform.SetParent(parent, false);

        RectTransform newRectTransform = newGameObject.AddComponent<RectTransform>();
        newRectTransform.sizeDelta = new Vector2(100, 100);

        return newRectTransform;
    }
}
