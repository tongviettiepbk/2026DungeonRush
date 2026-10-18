using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Chụp 1 prefab UI (popup có Canvas riêng) ra PNG 1080x2160 trong preview scene — không cần Play.
// Dùng khi reskin để so ảnh game với mockup. Gọi qua UnityMCP execute_code:
//   UICaptureTool.Capture("Assets/.../UIProfilePopup.prefab", "/tmp/out.png", go => { ... bật/tắt node ... });
public static class UICaptureTool
{
    public const int Width = 1080, Height = 2160;

    public static void Capture(string prefabPath, string outPng, Action<GameObject> setup = null)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        try
        {
            var camGo = new GameObject("CaptureCam");
            SceneManager.MoveGameObjectToScene(camGo, scene);
            var cam = camGo.AddComponent<Camera>();
            cam.scene = scene;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
            cam.orthographic = true;
            cam.targetTexture = rt;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            foreach (var canvas in go.GetComponentsInChildren<Canvas>(true))
            {
                if (!canvas.isRootCanvas) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 10;
            }
            if (setup != null) setup(go);

            // Layout group chỉ tự xếp khi chạy → ép xếp lại cho ảnh chụp giống lúc Play.
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)go.transform);
            Canvas.ForceUpdateCanvases();
            cam.Render();
            Canvas.ForceUpdateCanvases();
            cam.Render();

            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            File.WriteAllBytes(outPng, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
