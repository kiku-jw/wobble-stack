using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace WobbleStack.Runtime
{
    internal static class PortraitCapture
    {
        private const int Width = 1179;
        private const int Height = 2556;

        public static void Write(Camera camera, Canvas canvas, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            RenderTexture renderTexture = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            RenderMode previousRenderMode = canvas.renderMode;
            Camera previousCanvasCamera = canvas.worldCamera;
            float previousPlaneDistance = canvas.planeDistance;

            try
            {
                camera.targetTexture = renderTexture;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                Canvas.ForceUpdateCanvases();
                RenderTexture.active = renderTexture;
                camera.Render();

                Texture2D image = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.Destroy(image);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                canvas.renderMode = previousRenderMode;
                canvas.worldCamera = previousCanvasCamera;
                canvas.planeDistance = previousPlaneDistance;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }
    }
}
