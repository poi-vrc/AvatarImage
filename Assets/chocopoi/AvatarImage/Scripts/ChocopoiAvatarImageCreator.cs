using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Core;
using System.Linq;

public class ChocopoiAvatarImageCreator : MonoBehaviour
{

    public VRC_AvatarDescriptor avatar;

    public bool displayVersionText = true;

    public bool displayRandomText = true;

    public bool displayCustomImage = false;

    public bool moveVrcCamToPreviewCameraLocation = true;

    public bool autoTickUploadImageEveryUpload = true;

    public bool autoTickAgreement = true;

    public bool autoUpload = false;

    public int randomTextLength = 8;

    public Texture customImageTexture;

    private Canvas avatarImageCanvas;

    private Camera previewCamera;

    private RawImage customImage;

    private Text versionText;

    private Text randomText;

    private System.Random random;

    private bool hookAttempted = false;
    
    void Awake()
    {
        random = new System.Random();
        avatarImageCanvas = GetComponentInChildren<Canvas>();
        versionText = avatarImageCanvas.transform.Find("VersionText")?.GetComponent<Text>();
        randomText = avatarImageCanvas.transform.Find("RandomText")?.GetComponent<Text>();
        customImage = avatarImageCanvas.transform.Find("CustomImage")?.GetComponent<RawImage>();
        previewCamera = GameObject.Find("AvatarImagePreviewCamera")?.GetComponent<Camera>();

        randomText.text = RandomString(randomTextLength);

        versionText.gameObject.SetActive(displayVersionText);
        randomText.gameObject.SetActive(displayRandomText);
        customImage.gameObject.SetActive(displayCustomImage);
        customImage.texture = customImageTexture;
    }
    
    void Update()
    {
        if (!hookAttempted && Time.timeSinceLevelLoad > 2)
        {
            hookAttempted = true;
            HookIntoCamera();
        }
    }

    private void HookIntoCamera()
    {
        GameObject vrcCamObject = GameObject.Find("VRCCam");
        Camera vrcCam = vrcCamObject?.GetComponent<Camera>();

        if (vrcCamObject == null && vrcCam == null)
        {
            Debug.LogError("[AvatarImage] Could not find VRCCam or it does not contain a camera in the running scene!");
            return;
        }

        vrcCam.cullingMask = LayerMask.NameToLayer("Everything");

        if (moveVrcCamToPreviewCameraLocation)
        {
            vrcCam.transform.position = previewCamera.transform.position;
            vrcCam.transform.rotation = previewCamera.transform.rotation;
            vrcCam.transform.localScale = previewCamera.transform.localScale;
        }

        avatarImageCanvas.worldCamera = vrcCam;

        bool firstTimeUpload = true;

        if (autoTickUploadImageEveryUpload)
        {
            GameObject imageUploadToggleObject = GameObject.Find("ImageUploadToggle");

            if (imageUploadToggleObject == null)
            {
                Debug.LogError("[AvatarImage] Could not find image upload toggle in the running scene!");
                return;
            }
            else
            {
                Toggle toggle = imageUploadToggleObject.GetComponent<Toggle>();

                firstTimeUpload = toggle.isOn;

                toggle.isOn = true;
                toggle.onValueChanged?.Invoke(true);
            }
        }

        if (autoUpload || autoTickAgreement)
        {
            GameObject toggleWarrantObject = GameObject.Find("ToggleWarrant");

            if (toggleWarrantObject == null)
            {
                Debug.LogError("[AvatarImage] Could not find warrant toggle in the running scene!");
                return;
            }
            else
            {
                Toggle toggle = toggleWarrantObject.GetComponent<Toggle>();
                toggle.isOn = true;
                toggle.onValueChanged?.Invoke(true);
            }

            if (autoUpload && !firstTimeUpload)
            {
                GameObject uploadButtonObject = GameObject.Find("UploadButton");

                if (uploadButtonObject == null)
                {
                    Debug.LogError("[AvatarImage] Could not find upload button in the running scene!");
                    return;
                }
                else
                {
                    Button button = uploadButtonObject.GetComponent<Button>();
                    button.onClick?.Invoke();
                }
            }
        }

        ObtainApiAvatar();
    }

    private void ObtainApiAvatar()
    {
        PipelineManager pm = avatar.GetComponent<PipelineManager>();

        if (pm != null && !string.IsNullOrEmpty(pm.blueprintId) && avatar.apiAvatar == null)
        {
            ApiAvatar av = API.FromCacheOrNew<ApiAvatar>(pm.blueprintId);
            av.Fetch(
                c => avatar.apiAvatar = c.Model as ApiAvatar,
                c => {
                    Debug.LogErrorFormat("The avatar with blueprint ID {0} cannot be loaded with error {1}", pm.blueprintId, c.Error);
                }
            );
            avatar.apiAvatar = av;
        }

        if (avatar.apiAvatar != null)
        {
            ApiAvatar a = avatar.apiAvatar as ApiAvatar;
            versionText.text = a.version.ToString();
        } else
        {
            versionText.text = "1";
        }
    }

    public string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
