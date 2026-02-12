
using Cysharp.Threading.Tasks;

using Proyecto26;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace EWova.NetService
{
    public partial class Client
    {
        public static readonly Debug HttpDebugger = new("[NetService/Client] ");
        public async UniTask<string> Get(string path, CancellationToken cancellationToken = default)
        {
            var req = new RequestHelper
            {
                Uri = Path.Combine(EWova.NetServiceApi, path),
                Headers = !IsLogin ? null : new Dictionary<string, string> { { "Authorization", m_holdingToken.Header.ToString() } },
                Method = "GET",
            };

            HttpDebugger.Log($"[{req.Method}] Request Uri:{req.Uri}");

            ResponseHelper rsp;
            try
            {
                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
            }
            catch (Exception ex)
            {
                HttpDebugger.LogError($"[{req.Method}] Error Uri:{req.Uri} Exception:{ex}");
                return null;
            }

            HttpDebugger.Log($"[{req.Method}] Response Uri:{req.Uri} Content:{rsp.Text}");

            return rsp.Text;
        }
        public async UniTask<T> Get<T>(string path, CancellationToken cancellationToken = default)
        {
            var req = new RequestHelper
            {
                Uri = Path.Combine(EWova.NetServiceApi, path),
                Headers = !IsLogin ? null : new Dictionary<string, string> { { "Authorization", m_holdingToken.Header.ToString() } },
                Method = "GET",
            };

            HttpDebugger.Log($"[{req.Method}] Request Uri:{req.Uri}");

            ResponseHelper rsp;
            try
            {
                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
            }
            catch (Exception ex)
            {
                HttpDebugger.LogError($"[{req.Method}] Error Uri:{req.Uri} Exception:{ex}");
                return default(T);
            }

            T typed;
            if (!string.IsNullOrWhiteSpace(rsp.Text))
            {
                try
                {
                    typed = DeserializeObject<T>(rsp.Text);
                }
                catch (Exception ex)
                {
                    HttpDebugger.LogError($"[{req.Method}] Error Uri:{req.Uri} DeserializeObject({typeof(T)}) Exception:{ex}");
                    return default(T);
                }
            }
            else
            {
                return default(T);
            }

            HttpDebugger.Log($"[{req.Method}] Response Uri:{req.Uri} Content({typeof(T)}):{rsp.Text}");
            return typed;
        }
        public async UniTask<string> Post(string path, object body, CancellationToken cancellationToken = default)
        {
            var req = new RequestHelper
            {
                Uri = Path.Combine(EWova.NetServiceApi, path),
                Headers = !IsLogin ? null : new Dictionary<string, string> { { "Authorization", m_holdingToken.Header.ToString() } },
                Method = "POST",
                Body = body
            };

            HttpDebugger.Log($"[{req.Method}] Request Uri:{req.Uri}");

            ResponseHelper rsp;
            try
            {
                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
            }
            catch (Exception ex)
            {
                HttpDebugger.LogError($"[{req.Method}] Error Uri:{req.Uri} Exception:{ex}");
                return null;
            }

            HttpDebugger.Log($"[{req.Method}] Response Uri:{req.Uri} Content:{rsp.Text}");

            return rsp.Text;
        }
        public async UniTask<T> Post<T>(string path, object body, CancellationToken cancellationToken = default)
        {
            var req = new RequestHelper
            {
                Uri = Path.Combine(EWova.NetServiceApi, path),
                Headers = !IsLogin ? null : new Dictionary<string, string> { { "Authorization", m_holdingToken.Header.ToString() } },
                Method = "POST",
                Body = body
            };

            HttpDebugger.Log($"[{req.Method}] Request Uri:{req.Uri}");

            ResponseHelper rsp;
            try
            {
                rsp = await RestClient.Request(req).AsUniTask(cancellationToken);
            }
            catch (Exception ex)
            {
                HttpDebugger.LogError($"[{req.Method}] Error Uri:{req.Uri} Exception:{ex}");
                return default(T);
            }

            T typed;
            if (!string.IsNullOrWhiteSpace(rsp.Text))
            {
                try
                {
                    typed = DeserializeObject<T>(rsp.Text);
                }
                catch (Exception ex)
                {
                    HttpDebugger.LogError($"[{req.Method}] Error Uri:{req.Uri} DeserializeObject({typeof(T)}) Exception:{ex}");
                    return default(T);
                }
            }
            else
            {
                return default(T);
            }

            HttpDebugger.Log($"[{req.Method}] Response Uri:{req.Uri} Content({typeof(T)}):{rsp.Text}");
            return typed;
        }

        private T DeserializeObject<T>(string text)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text);
        }
    }
}
