using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine;

using System;

using static EWova.LearningPortfolio.LearningPortfolio;
namespace EWova.LearningPortfolio
{
    [SerializeField]
    public class DeviceInfo
    {
        [Obsolete("已棄用從 Client 端 Mapping 裝置類型的方式。請直接使用 Application.platform 或 XRDisplaySubsystem.running 來判斷裝置類型和 XR 狀態。")]
        public readonly UsingDeviceList UsingDeviceId;

        public readonly string Platform;
        public readonly string DeviceModel;
        public readonly bool IsXRActive;

#pragma warning disable CS0618 // 類型或成員已經過時
        public DeviceInfo(UsingDeviceList usingDeviceId, string platform, string deviceModel, bool isXRActive)
        {
            UsingDeviceId = usingDeviceId;
            Platform = platform;
            DeviceModel = deviceModel;
            IsXRActive = isXRActive;
        }
#pragma warning restore CS0618 // 類型或成員已經過時
    }
    internal static class DeviceHelper
    {
        public static DeviceInfo GetDeviceInfo()
        {
            return new DeviceInfo
            (
                platform: Application.platform.ToString(),
                deviceModel: SystemInfo.deviceModel,
                isXRActive: DeviceHelper.IsXRActive(),

#pragma warning disable CS0618 // 類型或成員已經過時
                usingDeviceId: DeviceHelper.GetCurrentDevice()
#pragma warning restore CS0618 // 類型或成員已經過時
            );
        }

        [Obsolete("已棄用從 Client 端 Mapping 裝置類型的方式。請直接使用 Application.platform 或 XRDisplaySubsystem.running 來判斷裝置類型和 XR 狀態。")]
        internal static UsingDeviceList GetCurrentDevice()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.LinuxEditor:
                    return UsingDeviceList.Editor;

                case RuntimePlatform.WebGLPlayer:
                    if (IsXRActive())
                        return UsingDeviceList.Web_VR;
                    else
                        return UsingDeviceList.Web;

                case RuntimePlatform.Android:
                    if (IsXRActive())
                    {
                        var modelName = SystemInfo.deviceModel;
                        if (modelName.Contains("quest", System.StringComparison.OrdinalIgnoreCase)
                            || modelName.Contains("oculus", System.StringComparison.OrdinalIgnoreCase))
                            return UsingDeviceList.AllInOne_Meta_Quest;
                        else if (modelName.Contains("vive", System.StringComparison.OrdinalIgnoreCase)
                            || modelName.Contains("htc", System.StringComparison.OrdinalIgnoreCase))
                            return UsingDeviceList.AllInOne_HTC_VIVE;
                        else
                            return UsingDeviceList.AllInOne;
                    }
                    return UsingDeviceList.Android;

                case RuntimePlatform.OSXPlayer:
                    return UsingDeviceList.macOS;

                case RuntimePlatform.IPhonePlayer:
                    return UsingDeviceList.iOS;

#if UNITY_2023_2_OR_NEWER || UNITY_2022_3
                case RuntimePlatform.VisionOS:
                    return UsingDeviceList.visionOS;
#endif

                case RuntimePlatform.LinuxPlayer:
                    return UsingDeviceList.Linux;

                case RuntimePlatform.WindowsPlayer:
                    if (IsXRActive())
                        return UsingDeviceList.Windows_VR;
                    else
                        return UsingDeviceList.Windows;

                default:
                    return UsingDeviceList.Unknown;
            }
        }

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
