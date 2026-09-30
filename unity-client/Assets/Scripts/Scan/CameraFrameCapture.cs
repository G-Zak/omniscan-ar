using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace OmniScan.Scan
{
    public static class CameraFrameCapture
    {
        public static byte[] CaptureJpeg(ARCameraManager cameraManager, int quality = 75)
        {
            if (cameraManager == null || !cameraManager.TryAcquireLatestCpuImage(out var image))
            {
                return null;
            }

            using (image)
            {
                var size = new Vector2Int(image.width / 2, image.height / 2);
                var conversion = new XRCpuImage.ConversionParams(image, TextureFormat.RGBA32, XRCpuImage.Transformation.MirrorY)
                {
                    outputDimensions = size,
                };

                var texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
                try
                {
                    image.Convert(conversion, texture.GetRawTextureData<byte>());
                    texture.Apply();
                    return texture.EncodeToJPG(quality);
                }
                finally
                {
                    Object.Destroy(texture);
                }
            }
        }
    }
}
