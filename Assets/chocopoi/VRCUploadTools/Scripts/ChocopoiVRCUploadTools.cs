using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Core;
using System.Linq;

namespace Chocopoi.VRCUploadTools
{
    public class ChocopoiVRCUploadTools : MonoBehaviour
    {

        public VRC_AvatarDescriptor avatar;

        public bool moveVrcCamToPreviewCameraLocation = true;

        public bool autoTickUploadImageEveryUpload = true;

        public bool autoTickAgreement = true;

        public bool autoUpload = false;

        public int randomTextLength = 8;

        private Canvas avatarImageCanvas;

        private Camera previewCamera;

        private RawImage customImage;

        private Text versionText;

        private Text randomText;

        private System.Random random;

        private bool hookAttempted = false;

        /// <summary>
        /// 
        /// </summary>
        void Awake()
        {
            //Initialize system random
            random = new System.Random();

            //Prepare all UI elements
            avatarImageCanvas = GetComponentInChildren<Canvas>();
            versionText = avatarImageCanvas.transform.Find("VersionText")?.GetComponent<Text>();
            randomText = avatarImageCanvas.transform.Find("RandomText")?.GetComponent<Text>();
            customImage = avatarImageCanvas.transform.Find("CustomImage")?.GetComponent<RawImage>();
            previewCamera = GameObject.Find("VRCUploadToolsPreviewCamera")?.GetComponent<Camera>();

            //Generate random string
            randomText.text = RandomString(randomTextLength);
        }

        /// <summary>
        /// Called on every frame update. Starts the VRCCam hook attempt after the scene has loaded for 5 seconds.
        /// </summary>
        void Update()
        {
            //Wait until VRC upload UI fully loaded before hooking
            if (!hookAttempted && Time.timeSinceLevelLoad > 5)
            {
                hookAttempted = true;
                HookIntoCamera();
            }
        }

        /// <summary>
        /// Perform hooking our Unity UI overlay into VRCCam
        /// </summary>
        private void HookIntoCamera()
        {
            //Find the VRCCam GameObject in the running scene and try hooking it
            GameObject vrcCamObject = GameObject.Find("VRCCam");
            Camera vrcCam = vrcCamObject?.GetComponent<Camera>();

            //Return an error if such camera does not exist in the scene
            if (vrcCamObject == null && vrcCam == null)
            {
                Debug.LogError("[AvatarImage] Could not find VRCCam or it does not contain a camera in the running scene!");
                return;
            }

            //Enables the VRCCam to render all layers (to include our UI layer)
            vrcCam.cullingMask = LayerMask.NameToLayer("Everything");

            //Move VRCCam to our preview camera location if enabled
            if (moveVrcCamToPreviewCameraLocation)
            {
                vrcCam.transform.position = previewCamera.transform.position;
                vrcCam.transform.rotation = previewCamera.transform.rotation;
                vrcCam.transform.localScale = previewCamera.transform.localScale;
            }

            //Assign the canvas's world camera as the VRCCam to render an UI overlay onto the image
            avatarImageCanvas.worldCamera = vrcCam;

            //Requests the (cached) avatar information from VRChat
            ObtainApiAvatar();

            //First time upload can be checked by looking whether the Image Upload toggle is turned on by default or not.
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

                    //Tick the toggle and invoke connected events
                    toggle.isOn = true;
                    toggle.onValueChanged?.Invoke(true);
                }
            }

            //Auto upload
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

                //Disallows auto upload if it's a first-time upload (because there are no avatar details inputted)
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
                        //Find the button and invoke it
                        Button button = uploadButtonObject.GetComponent<Button>();
                        button.onClick?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// Obtains the avatar information from the API and assigns it to our version text.
        /// Code is referenced from the included code within the VRCSDK.
        /// And it is used to obtain the avatar version for generating the image ONLY.
        /// </summary>
        private void ObtainApiAvatar()
        {
            PipelineManager pm = avatar.GetComponent<PipelineManager>();

            //Ask the API if there is a copy/cache of the avatar information
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
                //Assign the avatar version to our version text
                ApiAvatar a = avatar.apiAvatar as ApiAvatar;
                versionText.text = a.version.ToString();
            }
            else
            {
                //Sets the version text as zero as this is a new upload
                versionText.text = "0";
            }
        }

        /// <summary>
        /// A misc function to generate a random alphanumeric string with the provided length.
        /// </summary>
        /// <param name="length">The length of the random string to be generated</param>
        /// <returns>A random alphanumeric string with provided length</returns>
        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

}