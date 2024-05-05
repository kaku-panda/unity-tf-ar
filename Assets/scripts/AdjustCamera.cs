using UnityEngine;

public class ARCameraAdjuster : MonoBehaviour
{
    void Start()
    {
        AdjustARCamera();
    }

    void AdjustARCamera()
    {
        // カメラを取得
        Camera arCamera = Camera.main;
        if (arCamera == null)
        {
            Debug.LogError("AR Camera not found.");
            return;
        }

        // カメラの表示領域を中央に配置するためのスケーリング
        float scale = 0.5f;

        // `Rect`を計算
        float offsetX = (1 - scale) / 2;
        float offsetY = (1 - scale) / 2;
        arCamera.rect = new Rect(offsetX, offsetY, scale, scale);

        Debug.Log($"Rect = {arCamera.rect}");
    }
}
