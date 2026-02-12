//using Cysharp.Threading.Tasks;

//using Proyecto26;

//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Threading;

//using UnityEngine;
//using UnityEngine.Networking;

//namespace EWova.NetService
//{
//    public partial class Client
//    {
//        public static readonly Dictionary<string, AudioType> ExtensionToAudioType = new(StringComparer.OrdinalIgnoreCase)
//        {
//            { ".wav", AudioType.WAV },
//            { ".mp3", AudioType.MPEG },
//            { ".ogg", AudioType.OGGVORBIS }
//        };

//        public async UniTask<Texture2D> GetTexture2D(string uri, bool readable = false, bool mipmap = true, CancellationToken cancellationToken = default)
//        {
//            DownloadedTextureParams downloadedTextureParams = DownloadedTextureParams.Default;
//            downloadedTextureParams.readable = readable;
//            downloadedTextureParams.mipmapChain = mipmap;

//            var req = new RequestHelper
//            {
//                Uri = uri,
//                Headers = !IsLogin ? null : new Dictionary<string, string> { { "Authorization", m_holdingToken.Header.ToString() } },
//                Method = "GET",
//                DownloadHandler = new DownloadHandlerTexture(downloadedTextureParams)
//            };

//            HttpDebugger.Log($"[{req.Method}](Texture2D) Request Uri:{req.Uri}");

//            ResponseHelper rsp;
//            try
//            {
//                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
//            }
//            catch (Exception ex)
//            {
//                HttpDebugger.LogError($"[{req.Method}](Texture2D) Error Uri:{req.Uri} Exception:{ex}");
//                return null;
//            }

//            Texture2D result = ((DownloadHandlerTexture)rsp.Request.downloadHandler)?.texture;

//            if (result != null)
//            {
//                string memory = result.CalcUnityObjectNativeSize().RuntimeEstimateSize.ToHumanReadableSize();
//                HttpDebugger.Log($"[{req.Method}](Texture2D) Response Uri:{req.Uri} Content:w-{result.width},h-{result.height} Size:{rsp.Request.downloadedBytes.ToHumanReadableSize()} Memory:{memory}");
//                result.name = $"#{DateTime.Now:HHmmssfff}({memory})-{uri}";
//                return result;
//            }
//            else
//            {
//                HttpDebugger.LogWarning($"[{req.Method}](Texture2D) Error Uri:{req.Uri}");
//                return null;
//            }
//        }

//        public async UniTask<AudioClip> GetAudioClip(string uri, AudioType audioType, CancellationToken cancellationToken = default)
//        {
//            if (audioType is AudioType.UNKNOWN)
//            {
//                var ex = Path.GetExtension(uri);
//                if (string.IsNullOrEmpty(ex) || !ExtensionToAudioType.TryGetValue(ex, out var foundType))
//                {
//                    HttpDebugger.LogWarning($"(AudioClip) Error 不支援格式 {ex} Uri:{uri}");
//                    return null;
//                }
//                audioType = foundType;
//            }

//            var req = new RequestHelper
//            {
//                Uri = uri,
//                Headers = !IsLogin ? null : new Dictionary<string, string> { { "Authorization", m_holdingToken.Header.ToString() } },
//                Method = "GET",
//                DownloadHandler = new DownloadHandlerAudioClip(uri, audioType)
//            };

//            HttpDebugger.Log($"[{req.Method}](AudioClip) Request Uri:{req.Uri}");

//            ResponseHelper rsp;
//            try
//            {
//                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
//            }
//            catch (Exception ex)
//            {
//                HttpDebugger.LogError($"[{req.Method}](AudioClip) Error Uri:{req.Uri} Exception:{ex}");
//                return null;
//            }

//            AudioClip result = ((DownloadHandlerAudioClip)rsp.Request.downloadHandler).audioClip;

//            if (result != null)
//            {
//                string memory = result.CalcUnityObjectNativeSize().ToHumanReadableSize();
//                HttpDebugger.Log($"[{req.Method}](AudioClip) Response Uri:{req.Uri} Content:freq-{result.frequency},len-{result.length},ch-{result.channels} Size:{rsp.Request.downloadedBytes.ToHumanReadableSize()} Memory:{memory}");
//                result.name = $"#{DateTime.Now:HHmmssfff}({memory})-{uri}";
//                return result;
//            }
//            else
//            {
//                HttpDebugger.LogWarning($"[{req.Method}](AudioClip) Error Uri:{req.Uri}");
//                return null;
//            }
//        }

//    }
//}
