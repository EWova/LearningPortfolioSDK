using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine;

namespace EWova.LearningPortfolio
{
    [SerializeField]
    public partial class DeviceInfo
    {
        public readonly string Platform;
        public readonly string DeviceModel;
        public readonly bool IsXRActive;
        public DeviceInfo(string platform, string deviceModel, bool isXRActive)
        {
            Platform = platform;
            DeviceModel = deviceModel;
            IsXRActive = isXRActive;
        }

    }
    internal static partial class DeviceHelper
    {
        internal static bool IsXRActive()
        {
            List<XRDisplaySubsystem> displaySubsystems = new();
            SubsystemManager.GetSubsystems(displaySubsystems);
            foreach (var d in displaySubsystems)
            {
                if (d.running)
                    return true;
            }
            return false;
        }
    }
}
