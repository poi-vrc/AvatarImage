using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AvatarImageScript))]
public class AvatarImageEditor : Editor
{
    private Texture previewRenderTexture;

    private Camera previewCamera;

    void Awake()
    {
        GameObject previewCameraObject = GameObject.Find("AvatarImagePreviewCamera");
        previewCamera = previewCameraObject.GetComponent<Camera>();

        previewRenderTexture = previewCamera.targetTexture;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        GUILayout.Box(previewRenderTexture, 
            GUILayout.Height(256), GUILayout.MaxHeight(256));
    }
}
