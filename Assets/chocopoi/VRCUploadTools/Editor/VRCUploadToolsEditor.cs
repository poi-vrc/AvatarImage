using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using VRC.SDKBase;
using System.IO;

namespace Chocopoi.VRCUploadTools
{
    [CustomEditor(typeof(ChocopoiVRCUploadTools))]
    public class VRCUploadToolsEditor : Editor
    {
        private static I18n t = I18n.GetInstance();

        private Texture previewRenderTexture;

        private Camera previewCamera;

        private string toolVersion = null;

        private Texture oldCustomImageTexture = null;

        private int selectedTab = 0;

        private int selectedLang = 0;

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

        void DrawHorizontalLine(int i_height = 1)
        {
            EditorGUILayout.Separator();
            Rect rect = EditorGUILayout.GetControlRect(false, i_height);
            rect.height = i_height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
            EditorGUILayout.Separator();
        }

        private void DrawLanguageSelectorGUI()
        {
            if (GUILayout.Button("Reload translations"))
            {
                t.LoadTranslations(new string[] { "en", "zh", "jp" });
            }
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Language 語言 言語:");
            selectedLang = GUILayout.Toolbar(selectedLang, new string[] { "EN", "中", "JP" });

            if (selectedLang == 0)
            {
                t.SetLocale("en");
            }
            else if (selectedLang == 1)
            {
                t.SetLocale("zh");
            }
            else if (selectedLang == 2)
            {
                t.SetLocale("jp");
            }
            GUILayout.EndHorizontal();
        }

        private void DrawTabSelectorGUI()
        {
            ChocopoiVRCUploadTools instance = (ChocopoiVRCUploadTools)target;

            selectedTab = GUILayout.Toolbar(selectedTab, new string[] { "Image", "Auto-Upload", "Avatar Scaling" });

            EditorGUILayout.Separator();

            if (selectedTab == 0)
            {
                DrawTabImageGUI(instance);
            }
            else if (selectedTab == 1)
            {
                DrawTabAutoUploadGUI(instance);
            }
            else if (selectedTab == 2)
            {
                DrawTabAvatarScalingGUI(instance);
            }
        }

        private void DrawToolHeaderGUI()
        {
            GUIStyle titleLabelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24
            };
            EditorGUILayout.LabelField("VRCUploadTools", titleLabelStyle, GUILayout.ExpandWidth(true), GUILayout.Height(30));
            EditorGUILayout.Separator();

            EditorGUILayout.HelpBox(t._("label_header_tool_description"), MessageType.Info);

            GUIStyle uploadBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16
            };
            GUILayout.Button("✔️ " + t._("button_header_upload_now"), uploadBtnStyle, GUILayout.Height(30));
            
            DrawHorizontalLine();
        }

        private void DrawToolFooterGUI()
        {
            DrawHorizontalLine();

            GUILayout.Label(t._("label_footer_version", toolVersion));
            EditorGUILayout.SelectableLabel("https://github.com/poi-vrc/VRCUploadTools");
        }

        private void DrawTabImageGUI(ChocopoiVRCUploadTools instance)
        {
            // Obtain the scene objects
            Transform avatarImageCanvas = instance.transform.Find("VRCUploadToolsCanvas");
            RawImage customRawImage = avatarImageCanvas?.transform.Find("CustomImage")?.GetComponent<RawImage>();
            Text versionText = avatarImageCanvas?.transform.Find("VersionText").GetComponent<Text>();
            Text randomText = avatarImageCanvas?.transform.Find("RandomText").GetComponent<Text>();

            if (avatarImageCanvas == null || customRawImage == null || versionText == null || randomText == null)
            {
                EditorGUILayout.HelpBox("Error: VRCUploadToolsCanvas/CustomImage/VersionText/RandomText could not be found in the scenes. Do not change their GameObject names or delete them. You will experience run-time errors when you are trying to upload.", MessageType.Error);
            }

            GUILayout.Label("Select the avatar that you are going to generate the image for:", EditorStyles.boldLabel);
            instance.avatar = (VRC_AvatarDescriptor)EditorGUILayout.ObjectField("Avatar", instance.avatar, typeof(VRC_AvatarDescriptor), true);

            EditorGUILayout.Separator();

            // Settings
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            instance.moveVrcCamToPreviewCameraLocation = GUILayout.Toggle(instance.moveVrcCamToPreviewCameraLocation, "Move VRCCam to the current VRCUploadToolsPreviewCamera location");

            EditorGUILayout.Separator();

            // Image Texts
            GUILayout.Label("Image Texts", EditorStyles.boldLabel);

            versionText.gameObject.SetActive(GUILayout.Toggle(versionText.gameObject.activeSelf, "Display the avatar version on the image"));
            randomText.gameObject.SetActive(GUILayout.Toggle(randomText.gameObject.activeSelf, "Display a random text on the image"));
            instance.randomTextLength = EditorGUILayout.IntField("Length of the random text", instance.randomTextLength);

            EditorGUILayout.Separator();

            // Custom Image

            GUILayout.Label("Custom Image", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Target image resolution is 1200x900. This tool can scale it for you by entering the original resolution of your image.", MessageType.Info);

            customRawImage.gameObject.SetActive(GUILayout.Toggle(customRawImage.gameObject.activeSelf, "Use custom image instead of camera"));
            customRawImage.texture = (Texture)EditorGUILayout.ObjectField("Custom Image Texture", customRawImage.texture, typeof(Texture), true);

            EditorGUILayout.Popup("Method to Scale", 0, new string[] { "Fit by width", "Fit by height", "Zoom to Fit", "Stretch (None)" });
            EditorGUILayout.Vector2IntField("Custom Image Resolution", Vector2Int.zero);


            // Update the preview image
            if (oldCustomImageTexture != customRawImage.texture)
            {
                oldCustomImageTexture = customRawImage.texture;
                EditorUtility.SetDirty(target);
            }

            GUILayout.Label("Preview", EditorStyles.boldLabel);
            GUILayout.Box(previewRenderTexture, GUILayout.Width(300), GUILayout.Height(225));
        }

        private void DrawTabAutoUploadGUI(ChocopoiVRCUploadTools instance)
        {
            // Miscellaneous
            EditorGUILayout.TextField("Avatar Name", "");
            EditorGUILayout.LabelField("Avatar Description");
            EditorGUILayout.TextArea("", GUILayout.Height(48));

            EditorGUILayout.Separator();

            GUILayout.Label("Settings", EditorStyles.boldLabel);
            GUILayout.Toggle(false, "Make Public");
            instance.autoTickUploadImageEveryUpload = GUILayout.Toggle(instance.autoTickUploadImageEveryUpload, "Auto tick \"Upload Image\" on every upload");
            instance.autoTickAgreement = GUILayout.Toggle(instance.autoTickAgreement, "Auto tick the avatar upload agreement");
            instance.autoUpload = GUILayout.Toggle(instance.autoUpload, "Auto upload (without clicking the \"Upload\" button manually)");
        }

        private void DrawTabAvatarScalingGUI(ChocopoiVRCUploadTools instance)
        {
            EditorGUILayout.HelpBox("All GameObjects/Avatar that starts with \"vrut_<BaseAvatarName>\" will be deleted automatically upon generation.", MessageType.Info);
            var x = (VRC_AvatarDescriptor)EditorGUILayout.ObjectField("Base Avatar", instance.avatar, typeof(VRC_AvatarDescriptor), true);

            GUIStyle generateBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16
            };
            EditorGUILayout.BeginHorizontal();
            GUILayout.Button("Generate All", generateBtnStyle, GUILayout.Height(30));
            GUILayout.Button("Delete All", generateBtnStyle, GUILayout.Height(30));
            EditorGUILayout.EndHorizontal();

            DrawHorizontalLine();

            GUILayout.Label("Add New Scale", EditorStyles.boldLabel);
            EditorGUILayout.FloatField("Scale", 0);
            GUILayout.Button("Add");

            EditorGUILayout.Separator();

            if (EditorGUILayout.Foldout(true, "Scales"))
            {
                EditorGUI.indentLevel = 1;
                if (EditorGUILayout.Foldout(true, "0.0"))
                {
                    GUILayout.BeginVertical();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.FloatField("Scale", 0);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(instance.avatar, typeof(VRC_AvatarDescriptor), true);
                    EditorGUI.EndDisabledGroup();
                    GUILayout.Button("× Delete");
                    GUILayout.Button("✔️ Upload!");
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.TextField("Blueprint ID", "");
                    GUILayout.EndVertical();
                }
            }
            EditorGUI.indentLevel = 0;
        }

        public override void OnInspectorGUI()
        {
            DrawLanguageSelectorGUI();
            DrawToolHeaderGUI();
            DrawTabSelectorGUI();
            DrawToolFooterGUI();
        }
    }

}
