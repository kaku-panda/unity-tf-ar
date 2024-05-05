using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.Collections;
using System;
using Unity.Collections.LowLevel.Unsafe;

public class CameraImageExample : MonoBehaviour
{
    private Texture2D tempBuffer = null;
    private Texture2D m_Texture = null;
    public Texture2D CurrentArImage => m_Texture;
    [SerializeField]
    ARCameraManager cameraManager = null;
    private XRCpuImage.ConversionParams currentConversionParam;

    [SerializeField]
    private SsdSample _ssdSample;

    void OnEnable()
    {
        cameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        cameraManager.frameReceived -= OnCameraFrameReceived;
    }

    unsafe void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return;
        

        currentConversionParam.inputRect = new RectInt(0, 0, image.width, image.height);
        currentConversionParam.outputDimensions = new Vector2Int(image.width, image.height);
        currentConversionParam.outputFormat = TextureFormat.RGBA32;
        currentConversionParam.transformation = XRCpuImage.Transformation.MirrorY;
        

        // See how many bytes you need to store the final image.
        int size = image.GetConvertedDataSize(currentConversionParam);

        // Allocate a buffer to store the image.
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        // Extract the image data
        image.Convert(currentConversionParam, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);

        // The image was converted to RGBA32 format and written into the provided buffer
        // so you can dispose of the XRCpuImage. You must do this or it will leak resources.
        image.Dispose();
        

        // At this point, you can process the image, pass it to a computer vision algorithm, etc.
        // In this example, you apply it to a texture to visualize it.

        // You've got the data; let's put it into a texture so you can visualize it.
        ReCreateTexture(ref tempBuffer, currentConversionParam.outputDimensions, currentConversionParam.outputFormat);

        tempBuffer.LoadRawTextureData(buffer);
        tempBuffer.Apply();
        
        if (UnityEngine.Input.deviceOrientation == DeviceOrientation.Portrait)
        {
            Vector2Int dimension = Vector2Int.zero;
            dimension.x = currentConversionParam.outputDimensions.y;
            dimension.y = currentConversionParam.outputDimensions.x;
            ReCreateTexture(ref m_Texture, dimension, currentConversionParam.outputFormat);

            // 90° rotate
            for (int y = 0; y < image.height; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    m_Texture.SetPixel(image.height - y - 1, x,  tempBuffer.GetPixel(x, y));
                }
            }
            m_Texture.Apply();
        }
        else if (UnityEngine.Input.deviceOrientation == DeviceOrientation.LandscapeLeft)
        {
            ReCreateTexture(ref m_Texture, currentConversionParam.outputDimensions, currentConversionParam.outputFormat);

            // 180° rotate
            for (int y = 0; y < image.height; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    m_Texture.SetPixel(image.width - x - 1, image.height - y - 1, tempBuffer.GetPixel(x, y));
                }
            }
            m_Texture.Apply();
        }
        else if (UnityEngine.Input.deviceOrientation == DeviceOrientation.PortraitUpsideDown)
        {
            Vector2Int dimension = Vector2Int.zero;
            dimension.x = currentConversionParam.outputDimensions.y;
            dimension.y = currentConversionParam.outputDimensions.x;
            ReCreateTexture(ref m_Texture, dimension, currentConversionParam.outputFormat);

            // 270° rotate
            for (int y = 0; y < image.height; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    m_Texture.SetPixel(y, image.width - x - 1, tempBuffer.GetPixel(x, y));
                }
            }
            m_Texture.Apply();
        }
        else
        {
            m_Texture = tempBuffer;
        }

        _ssdSample.Invoke(m_Texture);
        Debug.Log($"Screen Resolution: {Screen.width}x{Screen.height}");

        // Done with your temporary data, so you can dispose it.
        buffer.Dispose();
    }

    protected void ReCreateTexture(ref Texture2D tex, in Vector2Int size, TextureFormat foramt)
    {
        // Check necessity
        if (tex != null
            && Mathf.RoundToInt(tex.texelSize.x) == size.x
            && Mathf.RoundToInt(tex.texelSize.y) == size.y)
        {
            return;
        }
        
        if (tex != null )
        {
            var prev = tex;
            tex = null;
            Destroy(prev);
        }
        tex = new Texture2D(
            size.x,
            size.y,
            foramt,
            false);
    }
}
