using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using VRC.SDKBase;
using System.IO;

[CustomEditor(typeof(ChocopoiVRCUploadTools))]
public class VRCUploadToolsEditor : Editor
{
    private Texture previewRenderTexture;

    private Camera previewCamera;

    private string toolVersion = null;

    private bool oldVersionTextEnabled = true;

    private bool oldRandomTextEnabled = true;

    private bool oldCustomImageEnabled = false;

    private Texture oldCustomImageTexture = null;

    void Awake()
    {
        string objectName = Application.isPlaying ?
            "VRCCam" :
            "VRCUploadToolsPreviewCamera";

        GameObject previewCameraObject = GameObject.Find(objectName);

        if (previewCameraObject == null)
        {
            Debug.LogErrorFormat("[AvatarImage] Could not find camera \"{0}\" in the running scenes! Cannot initialize preview in inspector!.");
            return;
        }

        previewCamera = previewCameraObject.GetComponent<Camera>();
        previewRenderTexture = previewCamera.targetTexture;

        if (toolVersion == null)
        {
            toolVersion = GetToolVersionFromFile();
        }
    }

    private string GetToolVersionFromFile()
    {
        StreamReader reader = new StreamReader("Assets/chocopoi/VRCUploadTools/version.txt");
        string str = reader.ReadToEnd();
        reader.Close();
        return str;
    }

    public override void OnInspectorGUI()
    {
        ChocopoiVRCUploadTools creator = (ChocopoiVRCUploadTools)target;

        EditorGUILayout.HelpBox("This avatar image editor script helps you to generate an new image with a version code and timestamp every time you upload your avatar.", MessageType.Info);

        // Obtain the scene objects
        Transform avatarImageCanvas = creator.transform.Find("VRCUploadToolsCanvas");
        RawImage customRawImage = avatarImageCanvas?.transform.Find("CustomImage")?.GetComponent<RawImage>();
        Text versionText = avatarImageCanvas?.transform.Find("VersionText").GetComponent<Text>();
        Text randomText = avatarImageCanvas?.transform.Find("RandomText").GetComponent<Text>();

        if (avatarImageCanvas == null || customRawImage == null || versionText == null || randomText == null)
        {
            EditorGUILayout.HelpBox("Error: VRCUploadToolsCanvas/CustomImage/VersionText/RandomText could not be found in the scenes. Do not change their GameObject names or delete them. You will experience run-time errors when you are trying to upload.", MessageType.Error);
        }

        GUILayout.Label("Select the avatar that you are going to generate the image for:", EditorStyles.boldLabel);
        creator.avatar = (VRC_AvatarDescriptor)EditorGUILayout.ObjectField("Avatar", creator.avatar, typeof(VRC_AvatarDescriptor), true);

        // Settings
        GUILayout.Label("Settings", EditorStyles.boldLabel);

        creator.moveVrcCamToPreviewCameraLocation = GUILayout.Toggle(creator.moveVrcCamToPreviewCameraLocation, "Move VRCCam to the current AvatarImagePreviewCamera location");
        creator.displayVersionText = GUILayout.Toggle(creator.displayVersionText, "Display the avatar version on the image");
        creator.displayRandomText = GUILayout.Toggle(creator.displayRandomText, "Display a random text on the image");
        creator.randomTextLength = EditorGUILayout.IntField("Length of the random text", creator.randomTextLength);
        creator.displayCustomImage = GUILayout.Toggle(creator.displayCustomImage, "Display custom image");
        creator.customImageTexture = (Texture)EditorGUILayout.ObjectField("Custom Image Texture", creator.customImageTexture, typeof(Texture), true);

        // Miscellaneous
        GUILayout.Label("Miscellaneous", EditorStyles.boldLabel);
        creator.autoTickUploadImageEveryUpload = GUILayout.Toggle(creator.autoTickUploadImageEveryUpload, "Auto tick \"Upload Image\" on every upload");
        creator.autoTickAgreement = GUILayout.Toggle(creator.autoTickAgreement, "Auto tick the avatar upload agreement");
        creator.autoUpload = GUILayout.Toggle(creator.autoUpload, "Auto upload (without clicking the \"Upload\" button manually)");

        versionText.gameObject.SetActive(creator.displayVersionText);
        randomText.gameObject.SetActive(creator.displayRandomText);
        customRawImage.gameObject.SetActive(creator.displayCustomImage);
        customRawImage.texture = creator.customImageTexture;

        // Update the preview image
        if (oldCustomImageEnabled != creator.displayCustomImage || 
            oldVersionTextEnabled != creator.displayVersionText || 
            oldRandomTextEnabled != creator.displayRandomText ||
            oldCustomImageTexture != creator.customImageTexture)
        {
            EditorUtility.SetDirty(target);
        }

        GUILayout.Label("Preview", EditorStyles.boldLabel);
        GUILayout.Box(previewRenderTexture,
            GUILayout.Height(256), GUILayout.MaxHeight(256));

        EditorGUILayout.Separator();
        GUILayout.Label("Tool Version: " + toolVersion);
        EditorGUILayout.SelectableLabel("https://github.com/poi-vrc/AvatarImage");

        oldCustomImageEnabled = creator.displayCustomImage;
        oldVersionTextEnabled = creator.displayVersionText;
        oldRandomTextEnabled = creator.displayRandomText;
        oldCustomImageTexture = creator.customImageTexture;
    }
}
