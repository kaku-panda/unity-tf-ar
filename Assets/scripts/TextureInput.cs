using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class TextureInput :MonoBehaviour
{
    [SerializeField]
    private ARCameraBackground _arCameraBackground;

    [SerializeField]
    private int interval = 1;

    [SerializeField]
    private SsdSample _ssdSample;

    private RenderTexture _cameraTexture;
    DateTime last;

    private void Start()
    {
        last = DateTime.Now;
        _cameraTexture = new RenderTexture(Screen.width, Screen.height, 0);
    }


    void Update()
    {
        DateTime now = DateTime.Now;
        if ((now - last).TotalSeconds >= interval)
        {
            Detect();
            last = now;
        }
    }

    private void Detect()
    {
        //ARのカメラの画像をRenderTextureに渡している
        Graphics.Blit(null, _cameraTexture, _arCameraBackground.material);
        
        Debug.Log("Detect");

        //Textureを解析、認識を行っている。
        _ssdSample.Invoke(_cameraTexture);
    }
}
