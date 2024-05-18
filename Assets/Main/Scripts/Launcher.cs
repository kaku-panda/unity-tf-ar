using UnityEngine;

public class Thrower : MonoBehaviour
{
    public ThrowingObject throwingObjectPrefab;
    public float forceFactorExtra = 1.0f;
    public float torqueFactorExtra = 1.0f;
    public float torqueAngleExtra = 0.0f;

    private Vector2 touchStartPos;
    private bool isTouching = false;

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
#endif
        {
#if UNITY_EDITOR
            touchStartPos = Input.mousePosition;
#else
            touchStartPos = Input.GetTouch(0).position;
#endif
            isTouching = true;
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonUp(0) && isTouching)
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended && isTouching)
#endif
        {
#if UNITY_EDITOR
            Vector2 touchEndPos = Input.mousePosition;
#else
            Vector2 touchEndPos = Input.GetTouch(0).position;
#endif
            Vector2 inputSensitivity = new Vector2(1.0f, 1.0f);
            int screenHeight = Screen.height;

            // オブジェクトを生成して投げる
            var pos = Camera.main.transform.position;
            var forw = Camera.main.transform.forward;
            var thing = Instantiate(throwingObjectPrefab, pos + (forw * 0.4f), Quaternion.identity);

            thing.ThrowBase(
                touchStartPos,
                touchEndPos,
                inputSensitivity,
                Camera.main.transform,
                screenHeight,
                forceFactorExtra,
                torqueFactorExtra,
                torqueAngleExtra);

            isTouching = false;
        }
    }
}
