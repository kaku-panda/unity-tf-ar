using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.Collections;
using TensorFlowLite;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System;

public class TensorFlowRunner : MonoBehaviour
{
    [SerializeField]
    private ARCameraManager arCameraManager;

    [SerializeField]
    private SSD.Options options;

    [SerializeField]
    private Text framePrefab;

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

    private SSD ssd;
    private Text[] frames;
    private GameObject activeObject;
    private Texture2D cameraTexture;
    private string[] labels;

    [SerializeField]
    private TextAsset labelMap = null;
    private NativeArray<byte> cameraImageBytes;

    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

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
        frames = new Text[10];
        Transform parent = frameContainer.transform;
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = Instantiate(framePrefab, Vector3.zero, Quaternion.identity, parent);
            frames[i].transform.localPosition = Vector3.zero;
        }

        // ラベルの初期化
        labels = labelMap.text.Split('\n');
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

    private void UpdateFrame(Text frame, SSD.Result result, Vector2 size)
    {
        if (result.score < scoreThreshold)
        {
            frame.gameObject.SetActive(false);
            return;
        }

        var rectTransform = frame.transform as RectTransform;
        rectTransform.anchoredPosition = result.rect.position * size - size * 0.5f;
        rectTransform.sizeDelta = result.rect.size * size;

        RectTransform leftTopRect = rectTransform.GetComponent<RectTransform>();
        RectTransform centerRect = CreateNewTargetRectTransform(leftTopRect);
        SetRectTransformToCenter(leftTopRect, centerRect);

        // 既存のオブジェクトがあれば削除
        if (activeObject != null)
        {
            Destroy(activeObject);
        }

        // スクリーン座標を取得
        Vector2 screenPosition = DisplayScreenPosition(centerRect);

        // スクリーン座標からレイキャストしてオブジェクトを配置
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (raycastManager.Raycast(ray, raycastHits, TrackableType.AllTypes))
        {
            float distance = Vector3.Distance(arCamera.transform.position, raycastHits[0].pose.position);
            frame.text = $"{GetLabelName(result.classID)} : {(int)(result.score * 100)}%\nDistance: {distance:F2} meters";
            activeObject = Instantiate(objectPrefab, raycastHits[0].pose.position, raycastHits[0].pose.rotation);
        }
        else
        {
            frame.text = $"{GetLabelName(result.classID)} : {(int)(result.score * 100)}%";
        }

        frame.gameObject.SetActive(true);
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


    private Vector2 GetScreenPositionFromAnchoredPosition(RectTransform rectTransform)
    {
        // アンカー基準のワールド座標を取得
        Vector3 worldPosition = rectTransform.TransformPoint(Vector3.zero);

        // ワールド座標をスクリーン座標に変換
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPosition);

        return screenPosition;
    }

    private Vector2 DisplayScreenPosition(RectTransform canvasPoint)
    {
        // 任意の`RectTransform`のスクリーン座標を取得
        Vector2 screenPosition = GetScreenPositionFromAnchoredPosition(canvasPoint);

        Debug.Log($"Screen Position: X={screenPosition.x}, Y={screenPosition.y}");

        return screenPosition;
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
