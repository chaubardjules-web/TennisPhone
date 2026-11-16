using System.IO;
using UnityEditor;
using UnityEngine;

namespace TennisPhone.EditorTools
{
    /// <summary>
    /// Captures d'écran de contrôle, à une taille choisie, sans dépendre de la taille de la
    /// fenêtre Game. Les images vont dans Temp/Captures (non versionné).
    /// </summary>
    public static class CaptureTool
    {
        /// <summary>Rend la caméra principale dans une image de cette taille. Le HUD (IMGUI) n'y figure pas.</summary>
        public static string CaptureCamera(string name, int width, int height)
        {
            Camera view = Camera.main;
            if (view == null)
            {
                return "Pas de caméra principale.";
            }

            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            RenderTexture previousTarget = view.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            view.targetTexture = target;
            view.Render();
            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            image.Apply();

            view.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            string path = Save(name, image.EncodeToPNG());
            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
            return path;
        }

        /// <summary>Capture la fenêtre Game telle qu'elle est, HUD compris. À appeler en mode Play.</summary>
        [MenuItem("TennisPhone/Capture d'ecran du jeu")]
        public static void CaptureGameViewMenu()
        {
            string path = Path.Combine(Folder(), "capture_" + System.DateTime.Now.ToString("HHmmss") + ".png");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[TennisPhone] Capture : " + path);
        }

        static string Save(string name, byte[] png)
        {
            string path = Path.Combine(Folder(), name + ".png");
            File.WriteAllBytes(path, png);
            return path;
        }

        static string Folder()
        {
            string folder = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "Captures");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
