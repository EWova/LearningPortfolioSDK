using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine;

using static EWova.LearningPortfolio.LearningPortfolio;

namespace EWova.LearningPortfolio
{
    internal static class DeviceHelper
    {
        internal static UsingDeviceList GetCurrentDevice()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    return UsingDeviceList.Editor;

                case RuntimePlatform.WebGLPlayer:
                    if (IsXRRunning())
                        return UsingDeviceList.Web_VR;
                    else
                        return UsingDeviceList.Web;

                case RuntimePlatform.Android:
                    if (IsXRRunning())
                    {
                        var modelName = SystemInfo.deviceModel.ToLower();
                        if (modelName.Contains("quest") || modelName.Contains("oculus"))
                            return UsingDeviceList.AllInOne_Meta_Quest;
                        else if (modelName.Contains("vive") || modelName.Contains("htc"))
                            return UsingDeviceList.AllInOne_HTC_VIVE;
                        else
                            return UsingDeviceList.AllInOne;
                    }
                    else
                        return UsingDeviceList.Android;

                case RuntimePlatform.OSXPlayer:
                    return UsingDeviceList.macOS;

                case RuntimePlatform.IPhonePlayer:
                    return UsingDeviceList.iOS;

#if UNITY_2023_2_OR_NEWER || UNITY_2022_3
                case RuntimePlatform.VisionOS:
                    return UsingDeviceList.visionOS;
                    break;
#endif

                case RuntimePlatform.LinuxPlayer:
                    return UsingDeviceList.Linux;

                case RuntimePlatform.WindowsPlayer:
                    if (IsXRRunning())
                        return UsingDeviceList.Windows_VR;
                    else
                        return UsingDeviceList.Windows;

                default:
                    return UsingDeviceList.Unknown;
            }
        }

        private static bool IsXRRunning()
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
