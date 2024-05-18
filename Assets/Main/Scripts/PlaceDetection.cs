using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class PlaneDetectionWithScreenPosition : MonoBehaviour
{
    ARRaycastManager raycastManager;
    [SerializeField] GameObject sphere;
    [SerializeField] GameObject labelPrefab;
    [SerializeField] Camera arCamera;

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        if (Input.touchCount == 0 || Input.GetTouch(0).phase != TouchPhase.Ended || sphere == null || labelPrefab == null)
        {
            return;
        }

        var hits = new List<ARRaycastHit>();
        if (raycastManager.Raycast(Input.GetTouch(0).position, hits, TrackableType.AllTypes))
        {
            var hitPose = hits[0].pose;
            GameObject placedObject = Instantiate(sphere, hitPose.position, hitPose.rotation);

            // デバイスからオブジェクトまでの距離を計算
            float distance = Vector3.Distance(arCamera.transform.position, placedObject.transform.position);

            // 3Dラベルの基準位置をオブジェクトの上にオフセットして表示
            Vector3 labelPosition = hitPose.position + new Vector3(0, 0.03f, 0);
            GameObject labelObject = Instantiate(labelPrefab, labelPosition, Quaternion.identity);

            // ワールド座標からスクリーン座標に変換
            Vector3 screenPosition = arCamera.WorldToScreenPoint(hitPose.position);

            // TextMeshProでのテキスト内容を更新
            TMP_Text tmpText = labelObject.GetComponent<TMP_Text>();

            if (tmpText != null)
            {
                tmpText.text = $"Screen Position: {screenPosition.x:F2}, {screenPosition.y:F2}\nDistance: {distance:F2} m";
            }
            else
            {
                Debug.LogError("TMP_Text not found on the label prefab");
            }

            // ラベルが常にカメラを向くようにする
            labelObject.transform.LookAt(arCamera.transform);
            labelObject.transform.Rotate(0, 180, 0);
        }
    }
}
