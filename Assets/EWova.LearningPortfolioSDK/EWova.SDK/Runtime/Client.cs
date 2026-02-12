using System;
using System.Threading;
using System.Threading.Tasks;

using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;

using EWova.NetService.Model;

namespace EWova.NetService
{
    public partial class Client
    {
        public static readonly Debug Debugger = new("[NetService/Client] ");
        public static Client CreateEmptyUser => new() { m_holdingToken = BearerToken.Empty };

        private BearerToken m_holdingToken = BearerToken.Empty;
        public bool IsLogin => m_holdingToken != BearerToken.Empty;
        public bool IsKeepingAlive => m_aliveKeeperCancellationTokenSource != null && !m_aliveKeeperCancellationTokenSource.IsCancellationRequested;

        private CancellationTokenSource m_aliveKeeperCancellationTokenSource;

        public async UniTask<bool> Login(string account, string password, bool keepAlive = true, CancellationToken cancellationToken = default)
        {
            if (IsLogin)
            {
                Debugger.LogWarning("已登入");
                return false;
            }

            if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
            {
                Debugger.LogWarning("登入失敗，帳號或密碼為空");
                return false;
            }

            ApiLoginModel send = new() { email = account, password = password };
            ApiLoginToken token = await Post<ApiLoginToken>("login", send, cancellationToken: cancellationToken);

            if (token == null || string.IsNullOrWhiteSpace(token.data))
            {
                Debugger.LogWarning("登入失敗");
                return false;
            }

            m_holdingToken = token.data;

            if (keepAlive)
                KeepAlive();
            return true;
        }
        public async UniTask<bool> Login(string token, bool keepAlive = true, CancellationToken cancellationToken = default)
        {
            if (IsLogin)
            {
                Debugger.LogWarning("已登入");
                return false;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                Debugger.LogWarning("登入失敗 Token 不可為空");
                return false;
            }

            m_holdingToken = token;
            if (keepAlive)
                KeepAlive();
            return await CheckAlive(cancellationToken);
        }
        public void Logout()
        {
            if (!IsLogin)
            {
                Debugger.LogWarning("未登入");
                return;
            }

            m_aliveKeeperCancellationTokenSource?.Cancel();
            m_holdingToken = BearerToken.Empty;
            Debugger.Log("已登出");
            //等待 token 過期
        }

        private readonly TimeSpan KeepAliveInterval = TimeSpan.FromMinutes(1);
        public bool KeepAlive()
        {
            if (!IsLogin)
            {
                Debugger.LogWarning("未登入");
                return false;
            }

            if (m_aliveKeeperCancellationTokenSource != null)
            {
                Debugger.LogWarning("已經在保持登入狀態中");
                return true;
            }

            Debugger.Log("保持登入 開");
            m_aliveKeeperCancellationTokenSource = new();
            UniTask.Void(async () =>
            {
                await foreach (var _ in UniTaskAsyncEnumerable.Interval(KeepAliveInterval)
                    .WithCancellation(m_aliveKeeperCancellationTokenSource.Token))
                {
                    if (m_aliveKeeperCancellationTokenSource.IsCancellationRequested)
                        break;

                    bool isSuccess = await Renew(m_aliveKeeperCancellationTokenSource.Token);
                    Debugger.Log($"登入刷新 {KeepAliveInterval}");

                    if (!isSuccess)
                    {
                        if (IsLogin)
                        {
                            Debugger.LogError("保持登入狀態失敗，將會自動登出");
                            Logout();
                        }
                        break;
                    }
                }

                m_aliveKeeperCancellationTokenSource.Dispose();
                m_aliveKeeperCancellationTokenSource = null;
            });

            return true;
        }
        public void StopKeepAlive()
        {
            if (m_aliveKeeperCancellationTokenSource != null)
            {
                m_aliveKeeperCancellationTokenSource.Cancel();
                Debugger.Log("保持登入 關");
            }
            else
            {
                Debugger.LogWarning("並沒有保持登入");
            }
        }
        public async UniTask<bool> Renew(CancellationToken cancellationToken = default)
        {
            if (!IsLogin)
            {
                Debugger.LogWarning("尚未登入");
                return false;
            }

            ApiLoginToken token = await Post<ApiLoginToken>("renew", null, cancellationToken: cancellationToken);
            if (token == null || string.IsNullOrWhiteSpace(token.data))
            {
                Debugger.LogWarning("更新登入狀態失敗");
                return false;
            }

            m_holdingToken = token.data;
            return true;
        }
        public UniTask<bool> CheckAlive(CancellationToken cancellationToken = default)
        {
            if (!IsLogin)
            {
                Debugger.LogWarning("尚未登入");
                return UniTask.FromResult(false);
            }

            return Post<bool>("user/quickcheck", null, cancellationToken: cancellationToken);
        }
        public UniTask<UserProfile> GetProfile(CancellationToken cancellationToken = default)
        {
            if (!IsLogin)
            {
                Debugger.LogWarning("未登入");
                return UniTask.FromResult<UserProfile>(null);
            }

            return Get<UserProfile>("user", cancellationToken: cancellationToken);
        }
        public async UniTask<OrganizationProfile> GetOrganization(CancellationToken cancellationToken = default)
        {
            if (!IsLogin)
            {
                Debugger.LogWarning("未登入");
                return null;
            }

            Guid schoolGuid = (await GetProfile(cancellationToken)).schoolGuid;
            return await GetOrganization(schoolGuid, cancellationToken: cancellationToken);
        }
    }
}
