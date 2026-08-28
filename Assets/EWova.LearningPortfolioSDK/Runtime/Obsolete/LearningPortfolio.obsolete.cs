using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using static EWova.LearningPortfolio.LearningPortfolio;

namespace EWova.LearningPortfolio
{
    public partial class DeviceInfo
    {
        [Obsolete("已棄用從 Client 端 Mapping 裝置類型的方式。請直接使用 Application.platform 或 XRDisplaySubsystem.running 來判斷裝置類型和 XR 狀態。")]
        public readonly UsingDeviceList UsingDeviceId;

        [Obsolete("已棄用從 Client 端 Mapping 裝置類型的方式。請直接使用 Application.platform 或 XRDisplaySubsystem.running 來判斷裝置類型和 XR 狀態。")]
        public DeviceInfo(UsingDeviceList usingDeviceId, string platform, string deviceModel, bool isXRActive)
        {
            UsingDeviceId = usingDeviceId;
            Platform = platform;
            DeviceModel = deviceModel;
            IsXRActive = isXRActive;
        }
    }
    public partial class Api
    {
        public partial class SetProjectUsageRecordRequest
        {
            [Obsolete("已棄用從 Client 端 Mapping 裝置類型的方式")]
            public int UsingDeviceId;
        }
    }
    public partial class LearningPortfolio
    {
        [Obsolete("已棄用，請使用 CreateUserProjectSheetShower")]
        public static ProjectRecordShower CreateUserProjectRecordShower(RectTransform rectTransform) => LearningPortfolio.CreateUserProjectSheetShower(rectTransform);
        [Obsolete("現在的 ConnectAsync 已經包含了認證檢查，請直接使用 IsConnected 屬性就可以知道是否已連線。")]
        public static bool IsLoggedIn => IsConnected;

        public partial class UserProjectRecordSheet
        {

            [Obsolete("已棄用，請使用 ProgressAllCompleteMarkedDic")]
            public IReadOnlyList<string> ProgressCompletions => AllMarkedProgressDic.Keys.ToList();
            [Obsolete("已棄用，請使用 ProgressAllCompleteMarkedDic")]
            public IReadOnlyList<DateTime> ProgressCompletionsLocalDateTime => AllMarkedProgressDic.Values.ToList();
        }
        public partial class ProgressNode
        {
            [Obsolete("已棄用，請使用 SetMark")]
            public NetServiceVoid SetComplete => SetMark;
            [Obsolete("已棄用，請使用 IsMarked")]
            public bool IsCompletedSelf => RootSheet.AllMarkedProgressDic.ContainsKey(Path);
            [Obsolete("已棄用，請使用 MarkedTime")]
            public DateTime? CompleteTime => RootSheet.AllMarkedProgressDic.TryGetValue(Path, out var result) ? result : (DateTime?)null;
        }
    }

    internal static partial class DeviceHelper
    {
        public static DeviceInfo GetDeviceInfo()
        {
#pragma warning disable CS0618 // 類型或成員已經過時
            return new DeviceInfo
            (
                platform: Application.platform.ToString(),
                deviceModel: SystemInfo.deviceModel,
                isXRActive: DeviceHelper.IsXRActive(),
                usingDeviceId: DeviceHelper.GetCurrentDevice()
            );
#pragma warning restore CS0618 // 類型或成員已經過時
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
    }
}