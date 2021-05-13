using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Core;
using System.Linq;

public class AvatarImageScript : MonoBehaviour
{

    public VRC_AvatarDescriptor avatar;

    private Canvas avatarImageCanvas;

    private Text versionText;

    private Text randomText;

    private System.Random random;

    private bool hookAttempted = false;
    
    void Awake()
    {
        random = new System.Random();
        avatarImageCanvas = GetComponentInChildren<Canvas>();

        versionText = avatarImageCanvas.transform.Find("VersionText").GetComponent<Text>();
        randomText = avatarImageCanvas.transform.Find("RandomText").GetComponent<Text>();

        randomText.text = RandomString(8);
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

        avatarImageCanvas.worldCamera = vrcCam;

        GameObject imageUploadToggleObject = GameObject.Find("ImageUploadToggle");

        if (imageUploadToggleObject == null)
        {
            Debug.LogError("[AvatarImage] Could not find image upload toggle in the running scene!");
            return;
        }
        else
        {
            Toggle toggle = imageUploadToggleObject.GetComponent<Toggle>();
            toggle.isOn = true;
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
