using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconGenerator : MonoBehaviour
{
    private new Camera camera;
    public const string path = "Textures/Icons";

    public List<GameObject> sceneObject;
    public List<ItemObject> dataItemObject;

    [ContextMenu("Screenshot")]
    public void ProcessScreenshots()
    {
        StartCoroutine(Screenshot());
    }

    private IEnumerator Screenshot()
    {
        for(int i = 0; i < sceneObject.Count; i++)
        {
            GameObject obj = sceneObject[i];
            ItemObject data = dataItemObject[i];
            obj.gameObject.SetActive(true);

            yield return null;

            string fullPath = $"{Application.dataPath}/{path}/icon_{data.name}.png";
            TakeScreenshot(fullPath);

            yield return null;

            obj.gameObject.SetActive(false);
            Sprite s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/{path}/icon_{data.name}.png");
            if (s != null)
            {
                data.icon = s;
                UnityEditor.EditorUtility.SetDirty(data);
            }
            yield return null;
        }
    }

    private void Awake()
    {
        camera = GetComponent<Camera>();
    }
    void TakeScreenshot(string fullPath)
    {
        if (camera == null)
        {
            camera = GetComponent<Camera>();
        }
        int width = 256, height = 256;
        RenderTexture rt = new RenderTexture(width, height, 24); //256,256,24
        camera.targetTexture = rt;
        Texture2D screenshot = new Texture2D(width, height, TextureFormat.RGBA32, false);
        camera.Render();
        RenderTexture.active = rt;
        screenshot.ReadPixels(new Rect(0, 0, width, height), 0 , 0);
        camera.targetTexture = null;
        RenderTexture.active = null;

        if (Application.isEditor)
        {
            DestroyImmediate(rt);
        }
        else
        {
            Destroy(rt);
        }

        byte[] bypes = screenshot.EncodeToPNG();
        System.IO.File.WriteAllBytes(fullPath, bypes);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
