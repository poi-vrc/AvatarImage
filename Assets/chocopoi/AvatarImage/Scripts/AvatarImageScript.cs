using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;
using VRC.Core;

public class AvatarImageScript : MonoBehaviour
{
    private VRC_AvatarDescriptor m_AvatarDescriptor;

    // Start is called before the first frame update
    void Start()
    {
        m_AvatarDescriptor = Core.ApiAvatar
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ObtainApiAvatar()
    {
        Core.PipelineManager pm = avatar.GetComponent<Core.PipelineManager>();
        if (pm != null && !string.IsNullOrEmpty(pm.blueprintId))
        {
            if (avatar.apiAvatar == null)
            {
                Core.ApiAvatar av = Core.API.FromCacheOrNew<Core.ApiAvatar>(pm.blueprintId);
                av.Fetch(
                    c => avatar.apiAvatar = c.Model as Core.ApiAvatar,
                    c =>
                    {
                        if (c.Code == 404)
                        {
                            Core.Logger.Log(
                                $"Could not load avatar {pm.blueprintId} because it didn't exist.",
                                Core.DebugLevel.API);
                            Core.ApiCache.Invalidate<Core.ApiWorld>(pm.blueprintId);
                        }
                        else
                            Debug.LogErrorFormat("Could not load avatar {0} because {1}", pm.blueprintId, c.Error);
                    });
                avatar.apiAvatar = av;
            }
        }

        if (avatar.apiAvatar != null)
        {
            Core.ApiAvatar a = (avatar.apiAvatar as Core.ApiAvatar);
            DrawContentInfoForAvatar(a);
            VRCSdkControlPanel.DrawContentPlatformSupport(a);
        }
    }
}
