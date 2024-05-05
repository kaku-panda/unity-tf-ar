using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;
using TensorFlowLite;
using TextureSource;
using UnityEngine.XR.ARSubsystems;

public class SsdSample : MonoBehaviour
{
    [SerializeField]
    private SSD.Options options = default;

    [SerializeField]
    private AspectRatioFitter frameContainer = null;

    [SerializeField]
    private Text framePrefab = null;

    [SerializeField, Range(0f, 1f)]
    private float scoreThreshold = 0.5f;

    [SerializeField]
    private TextAsset labelMap = null;

    [SerializeField]
    private Camera arCamera;

    [SerializeField]
    private ARRaycastManager raycastManager;

    [SerializeField]
    private GameObject highlightPrefab; // ハイライト用のプレハブ

    private GameObject highlightInstance; // ハイライトインスタンス
    private SSD ssd;
    private Text[] frames;
    private string[] labels;
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

    private void Start()
    {
        ssd = new SSD(options);

        // ハイライト用インスタンスを生成し、デフォルトで非表示に
        highlightInstance = Instantiate(highlightPrefab);
        highlightInstance.SetActive(false);

        // Init frames
        frames = new Text[10];
        Transform parent = frameContainer.transform;
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = Instantiate(framePrefab, Vector3.zero, Quaternion.identity, parent);
            frames[i].transform.localPosition = Vector3.zero;
        }

        // Labels
        labels = labelMap.text.Split('\n');

        if (TryGetComponent(out VirtualTextureSource source))
        {
            source.OnTexture.AddListener(Invoke);
        }
    }

    private void OnDestroy()
    {
        if (TryGetComponent(out VirtualTextureSource source))
        {
            source.OnTexture.RemoveListener(Invoke);
        }
        ssd?.Dispose();
    }

    public void Invoke(Texture texture)
    {
        ssd.Run(texture);

        SSD.Result[] results = ssd.GetResults();
        Vector2 size = (frameContainer.transform as RectTransform).rect.size;

        Vector2 ratio;
        if (texture.width >= texture.height)
        {
            ratio = new Vector2(1.0f, (float)texture.height / (float)texture.width);
        }
        else
        {
            ratio = new Vector2((float)texture.width / (float)texture.height, 1.0f);
        }

        for (int i = 0; i < 10; i++)
        {
            SetFrame(frames[i], results[i], size * ratio);
        }
    }

    [SerializeField] private Image centerMarker; 

    private void SetFrame(Text frame, SSD.Result result, Vector2 size)
    {
        if (result.score < scoreThreshold)
        {
            frame.gameObject.SetActive(false);
            return;
        }

        // バウンディングボックスの配置
        var rt = frame.transform as RectTransform;
        rt.anchoredPosition = result.rect.position * size - size * 0.5f;
        rt.sizeDelta = result.rect.size * size;

        // バウンディングボックスの中心座標をCanvas上で計算
        Vector2 boxCenterCanvas = rt.anchoredPosition + new Vector2(rt.sizeDelta.x / 2, -rt.sizeDelta.y / 2);

        // Canvas上の座標をスクリーン座標に変換
        //Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint// // arCamera, rt.TransformPoint(boxCenterCanvas));
        
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        Vector2 screenPosition = new Vector2(0, 0);
        
        // `Image`の位置をスクリーン座標に更新
        RectTransform markerRect = centerMarker.GetComponent<RectTransform>();
        markerRect.position = screenPosition;

        // バウンディングボックス自体の表示を有効にする
        frame.gameObject.SetActive(true);
    }

    private string GetLabelName(int id)
    {
        if (id < 0 || id >= labels.Length - 1)
        {
            return "?";
        }
        return labels[id + 1];
    }

    private Vector2 CanvasToScreenPosition(RectTransform canvasRect, Vector2 canvasPosition)
    {
        // ワールド座標に変換
        Vector3 worldPosition = canvasRect.TransformPoint(canvasPosition);

        // ワールド座標をスクリーン座標に変換
        Vector2 screenPosition = RectTransformUtility.WorldToScreenPoint(arCamera, worldPosition);

        return screenPosition;
    }
}
