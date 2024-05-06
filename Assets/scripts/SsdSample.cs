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
    private GameObject objectPrefab;

    private SSD ssd;
    private Text[] frames;
    private string[] labels;
    private GameObject activeObject; // 現在アクティブなオブジェクト
    private List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

    private void Start()
    {
        ssd = new SSD(options);

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

        // RectTransformからターゲットを生成し、中心に配置
        RectTransform sourceRect = rt.GetComponent<RectTransform>();
        RectTransform targetRect = CreateNewTargetRectTransform(sourceRect);
        SetRectTransformToCenter(sourceRect, targetRect);

        // 既存のオブジェクトを削除
        if (activeObject != null)
        {
            Destroy(activeObject);
        }

        // Canvas上の座標をスクリーン座標に変換
        Vector2 screenPosition = DisplayScreenPosition(targetRect);

        // スクリーン座標でレイキャストを実行
        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        if (raycastManager.Raycast(ray, raycastHits, TrackableType.AllTypes))
        {
            // ヒットした位置でオブジェクトを配置
            float distance = Vector3.Distance(arCamera.transform.position, raycastHits[0].pose.position);

            frame.text = $"{GetLabelName(result.classID)} : {(int)(result.score * 100)}%\nDistance: {distance:F2} meters";
            activeObject = Instantiate(objectPrefab, raycastHits[0].pose.position, raycastHits[0].pose.rotation);
        }
        else
        {
            frame.text = $"{GetLabelName(result.classID)} : {(int)(result.score * 100)}%";
        }

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
        // ピボットとサイズから中心座標を計算
        Vector2 centerPosition = source.anchoredPosition + new Vector2(source.sizeDelta.x / 2, -source.sizeDelta.y / 2);

        // 中心座標をターゲットの`anchoredPosition`に設定
        target.anchoredPosition = centerPosition;

        // 必要に応じて、ターゲットのサイズを調整
        target.sizeDelta = source.sizeDelta;
    }

    private RectTransform CreateNewTargetRectTransform(Transform parent)
    {
        // 新しい`GameObject`を生成し、`RectTransform`を追加
        GameObject newGameObject = new GameObject("TargetRectTransform");
        newGameObject.transform.SetParent(parent, false);

        // 新しい`RectTransform`を取得
        RectTransform newRectTransform = newGameObject.AddComponent<RectTransform>();

        // 初期サイズの設定
        newRectTransform.sizeDelta = new Vector2(100, 100); // 適切な初期サイズを設定してください

        return newRectTransform;
    }
}
