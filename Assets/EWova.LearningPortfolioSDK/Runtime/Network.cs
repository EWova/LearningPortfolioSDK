using Cysharp.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Threading;

using UnityEngine;

namespace EWova.LearningPortfolio
{
    public sealed class NetServiceRequestHandler : IDisposable
    {
        private readonly struct WorkItem
        {
            public readonly Func<CancellationToken, UniTask> Run;
            public readonly Action Cancel;

            public WorkItem(Func<CancellationToken, UniTask> run, Action cancel)
            {
                Run = run;
                Cancel = cancel;
            }
        }

        private readonly Queue<WorkItem> m_queue = new();
        private bool m_processing;
        private CancellationTokenSource m_cts = new();

        public int PendingCount => m_queue.Count;
        public bool IsAnyNetSerivceRequesting => m_processing;

        internal UniTask<T> EnqueueAsync<T>(Func<CancellationToken, UniTask<T>> run, CancellationToken externalToken)
        {
            var tcs = new UniTaskCompletionSource<T>();

            void CancelTcs() => tcs.TrySetCanceled(externalToken);

            Enqueue(async handlerToken =>
            {
                try
                {
                    using var linked = CancellationTokenSource.CreateLinkedTokenSource(handlerToken, externalToken);
                    var result = await run(linked.Token);
                    tcs.TrySetResult(result);
                }
                catch (OperationCanceledException)
                {
                    CancelTcs();
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            }, onCanceled: CancelTcs);

            return tcs.Task;
        }

        private void Enqueue(Func<CancellationToken, UniTask> run, Action onCanceled)
        {
            m_queue.Enqueue(new WorkItem(run, onCanceled));
            if (!m_processing)
            {
                m_processing = true;
                _ = ProcessAsync();
            }
        }

        private async UniTaskVoid ProcessAsync()
        {
            while (m_queue.Count > 0)
            {
                if (m_cts.IsCancellationRequested)
                {
                    DrainWithCancel();
                    return;
                }

                var item = m_queue.Dequeue();
                await item.Run(m_cts.Token);
            }

            m_processing = false;
        }

        /// <summary>
        /// 取消所有排隊中與執行中的請求。
        /// </summary>
        public void CancelAll()
        {
            var old = m_cts;
            m_cts = new CancellationTokenSource();
            old.Cancel();
            old.Dispose();

            if (!m_processing)
                DrainWithCancel();
        }

        private void DrainWithCancel()
        {
            while (m_queue.Count > 0)
            {
                var item = m_queue.Dequeue();
                try { item.Cancel(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
            m_processing = false;
        }

        /// <summary>
        /// 取消所有請求並釋放資源。
        /// </summary>
        public void Dispose()
        {
            m_cts.Cancel();
            m_cts.Dispose();
            DrainWithCancel();
        }
    }

    public enum AsyncRespondStatus { Success, Failed }

    public readonly struct NetServiceAsyncRespond
    {
        public readonly AsyncRespondStatus Status;
        public readonly LearningPortfolioApiException LearningPortfolioApiException;
        public string ErrorMessage => LearningPortfolioApiException?.Message ?? string.Empty;
        public bool IsSuccess => Status == AsyncRespondStatus.Success;
        public bool IsFailed => Status == AsyncRespondStatus.Failed;

        internal NetServiceAsyncRespond(AsyncRespondStatus status, LearningPortfolioApiException ex)
        {
            Status = status;
            LearningPortfolioApiException = ex;
        }

        /// <summary>
        /// 建立成功結果。
        /// </summary>
        public static NetServiceAsyncRespond ResultSuccess() => new(AsyncRespondStatus.Success, null);
        /// <summary>
        /// 建立失敗結果。
        /// </summary>
        /// <param name="ex">失敗原因。</param>
        public static NetServiceAsyncRespond ResultFailed(LearningPortfolioApiException ex) => new(AsyncRespondStatus.Failed, ex);
    }

    public readonly struct NetServiceAsyncRespond<T>
    {
        public readonly T Data;
        public readonly AsyncRespondStatus Status;
        public readonly LearningPortfolioApiException LearningPortfolioApiException;
        public string ErrorMessage => LearningPortfolioApiException?.Message ?? string.Empty;
        public bool IsSuccess => Status == AsyncRespondStatus.Success;
        public bool IsFailed => Status == AsyncRespondStatus.Failed;

        internal NetServiceAsyncRespond(T data, AsyncRespondStatus status, LearningPortfolioApiException ex)
        {
            Data = data;
            Status = status;
            LearningPortfolioApiException = ex;
        }

        /// <summary>
        /// 建立成功結果。
        /// </summary>
        /// <param name="data">回應資料。</param>
        public static NetServiceAsyncRespond<T> ResultSuccess(T data) => new(data, AsyncRespondStatus.Success, null);
        /// <summary>
        /// 建立失敗結果。
        /// </summary>
        /// <param name="ex">失敗原因。</param>
        public static NetServiceAsyncRespond<T> ResultFailed(LearningPortfolioApiException ex) => new(default, AsyncRespondStatus.Failed, ex);
    }

    public abstract class NetServiceBase
    {
        protected NetServiceBase(NetServiceRequestHandler requestHandler)
        {
            RequestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
        }
        internal readonly NetServiceRequestHandler RequestHandler;
    }

    public class NetServiceCommand<TRequest> : NetServiceBase
    {
        private readonly Func<TRequest, CancellationToken, UniTask> m_func;
        private readonly Func<TRequest, CancellationToken, UniTask> m_onDone;

        /// <summary>
        /// 建立一個無回應資料的網路服務指令。
        /// </summary>
        /// <param name="requestHandler">請求處理器。</param>
        /// <param name="func">實際發送請求的邏輯。</param>
        /// <param name="onRespond">成功後的額外處理（選填）。</param>
        public NetServiceCommand(
            NetServiceRequestHandler requestHandler,
            Func<TRequest, CancellationToken, UniTask> func,
            Func<TRequest, CancellationToken, UniTask> onRespond = null)
            : base(requestHandler)
        {
            m_func = func ?? throw new ArgumentNullException(nameof(func));
            m_onDone = onRespond;
        }

        private async UniTask<NetServiceAsyncRespond> RunAsync(
            TRequest request,
            CancellationToken ct)
        {
            try
            {
                await m_func(request, ct);

                if (m_onDone != null)
                    await m_onDone(request, ct);

                return NetServiceAsyncRespond.ResultSuccess();
            }
            catch (LearningPortfolioApiException ex)
            {
                return NetServiceAsyncRespond.ResultFailed(ex);
            }
        }

        /// <summary>
        /// 以非同步方式送出請求。
        /// </summary>
        /// <param name="request">請求內容。</param>
        /// <param name="cancellationToken">取消權杖。</param>
        public UniTask<NetServiceAsyncRespond> RequestAsync(
            TRequest request,
            CancellationToken cancellationToken = default)
            => RequestHandler.EnqueueAsync(token => RunAsync(request, token), cancellationToken);

        /// <summary>
        /// 以 Callback 方式送出請求。
        /// </summary>
        /// <param name="request">請求內容。</param>
        /// <param name="onSuccess">成功時的回呼。</param>
        /// <param name="onFailure">失敗時的回呼，附帶錯誤訊息。</param>
        /// <param name="onException">
        /// 非預期例外的回呼。API 錯誤與請求取消（<see cref="OperationCanceledException"/>）都會由
        /// <paramref name="onFailure"/> 回報，不會進到這裡；若持續收到 onException，通常代表問題並非出在呼叫端，
        /// 可聯絡 EWova 官方支援協助排查。
        /// </param>
        /// <param name="cancellationToken">取消權杖。</param>
        public void Request(
            TRequest request,
            Action onSuccess,
            Action<string> onFailure,
            Action<Exception> onException = null,
            CancellationToken cancellationToken = default)
        {
            RequestAsync(request, cancellationToken)
                .ContinueWith(result =>
                {
                    if (result.IsSuccess)
                        onSuccess?.Invoke();
                    else
                        onFailure?.Invoke(result.ErrorMessage);
                })
                .Forget(ex =>
                {
                    if (ex is OperationCanceledException)
                        onFailure?.Invoke("Request was canceled.");
                    else
                        onException?.Invoke(ex);
                });
        }
    }

    public class NetService<TRequest, TRespond> : NetServiceBase
    {
        private readonly Func<TRequest, CancellationToken, UniTask<TRespond>> m_func;
        private readonly Func<(TRequest Request, TRespond Respond), CancellationToken, UniTask> m_onRespond;

        /// <summary>
        /// 建立一個有回應資料的網路服務。
        /// </summary>
        /// <param name="requestHandler">請求處理器。</param>
        /// <param name="func">實際發送請求並取得回應資料的邏輯。</param>
        /// <param name="onRespond">成功後的額外處理（選填）。</param>
        public NetService(
            NetServiceRequestHandler requestHandler,
            Func<TRequest, CancellationToken, UniTask<TRespond>> func,
            Func<(TRequest Request, TRespond Respond), CancellationToken, UniTask> onRespond = null)
            : base(requestHandler)
        {
            m_func = func ?? throw new ArgumentNullException(nameof(func));
            m_onRespond = onRespond;
        }

        private async UniTask<NetServiceAsyncRespond<TRespond>> RunAsync(
            TRequest request, 
            CancellationToken ct)
        {
            try
            {
                var respond = await m_func(request, ct);

                if (m_onRespond != null)
                    await m_onRespond((request, respond), ct);

                return NetServiceAsyncRespond<TRespond>.ResultSuccess(respond);
            }
            catch (LearningPortfolioApiException ex)
            {
                return NetServiceAsyncRespond<TRespond>.ResultFailed(ex);
            }
        }

        /// <summary>
        /// 以非同步方式送出請求。
        /// </summary>
        /// <param name="request">請求內容。</param>
        /// <param name="cancellationToken">取消權杖。</param>
        public UniTask<NetServiceAsyncRespond<TRespond>> RequestAsync(
            TRequest request,
            CancellationToken cancellationToken = default)
            => RequestHandler.EnqueueAsync(token => RunAsync(request, token), cancellationToken);

        /// <summary>
        /// 以 Callback 方式送出請求。
        /// </summary>
        /// <param name="request">請求內容。</param>
        /// <param name="onSuccess">成功時的回呼，附帶回應資料。</param>
        /// <param name="onFailure">失敗時的回呼，附帶錯誤訊息。</param>
        /// <param name="onException">
        /// 非預期例外的回呼。API 錯誤與請求取消（<see cref="OperationCanceledException"/>）都會由
        /// <paramref name="onFailure"/> 回報，不會進到這裡；若持續收到 onException，通常代表問題並非出在呼叫端，
        /// 可聯絡 EWova 官方支援協助排查。
        /// </param>
        /// <param name="cancellationToken">取消權杖。</param>
        public void Request(
            TRequest request,
            Action<TRespond> onSuccess,
            Action<string> onFailure,
            Action<Exception> onException = null,
            CancellationToken cancellationToken = default)
        {
            RequestAsync(request, cancellationToken)
                .ContinueWith(result =>
                {
                    if (result.IsSuccess)
                        onSuccess?.Invoke(result.Data);
                    else
                        onFailure?.Invoke(result.ErrorMessage);
                })
                .Forget(ex =>
                {
                    if (ex is OperationCanceledException)
                        onFailure?.Invoke("Request was canceled.");
                    else
                        onException?.Invoke(ex);
                });
        }
    }

    public sealed class NetServiceVoid : NetServiceCommand<AsyncUnit>
    {
        /// <summary>
        /// 建立一個無請求內容、無回應資料的網路服務指令。
        /// </summary>
        /// <param name="handler">請求處理器。</param>
        /// <param name="func">實際發送請求的邏輯。</param>
        /// <param name="onRespond">成功後的額外處理（選填）。</param>
        public NetServiceVoid(NetServiceRequestHandler handler, Func<CancellationToken, UniTask> func, Func<CancellationToken, UniTask> onRespond = null)
            : base(handler,
                (_, ct) => func(ct),
                onRespond == null ? null : (_, ct) => onRespond(ct))
        {
        }

        /// <summary>
        /// 以非同步方式送出請求。
        /// </summary>
        /// <param name="ct">取消權杖。</param>
        public UniTask<NetServiceAsyncRespond> RequestAsync(
            CancellationToken ct = default)
            => base.RequestAsync(AsyncUnit.Default, ct);

        /// <summary>
        /// 以 Callback 方式送出請求。
        /// </summary>
        /// <param name="onSuccess">成功時的回呼。</param>
        /// <param name="onFailure">失敗時的回呼，附帶錯誤訊息。</param>
        /// <param name="onException">
        /// 非預期例外的回呼。API 錯誤與請求取消（<see cref="OperationCanceledException"/>）都會由
        /// <paramref name="onFailure"/> 回報，不會進到這裡；若持續收到 onException，通常代表問題並非出在呼叫端，
        /// 可聯絡 EWova 官方支援協助排查。
        /// </param>
        /// <param name="ct">取消權杖。</param>
        public void Request(
            Action onSuccess,
            Action<string> onFailure,
            Action<Exception> onException = null,
            CancellationToken ct = default)
            => base.Request(AsyncUnit.Default, onSuccess, onFailure, onException, ct);
    }

    public sealed class NetServiceRequest<TRequest> : NetServiceCommand<TRequest>
    {
        /// <summary>
        /// 建立一個有請求內容、無回應資料的網路服務指令。
        /// </summary>
        /// <param name="handler">請求處理器。</param>
        /// <param name="func">實際發送請求的邏輯。</param>
        /// <param name="onRespond">成功後的額外處理（選填）。</param>
        public NetServiceRequest(NetServiceRequestHandler handler, Func<TRequest, CancellationToken, UniTask> func, Func<TRequest, CancellationToken, UniTask> onRespond = null)
            : base(handler, func, onRespond)
        {
        }
    }

    public sealed class NetServiceRespond<TRespond> : NetService<AsyncUnit, TRespond>
    {
        /// <summary>
        /// 建立一個無請求內容、有回應資料的網路服務。
        /// </summary>
        /// <param name="handler">請求處理器。</param>
        /// <param name="func">實際發送請求並取得回應資料的邏輯。</param>
        /// <param name="onRespond">成功後的額外處理（選填）。</param>
        public NetServiceRespond(NetServiceRequestHandler handler, Func<CancellationToken, UniTask<TRespond>> func, Func<TRespond, CancellationToken, UniTask> onRespond = null)
            : base(handler,
                (_, ct) => func(ct),
                onRespond == null ? null : (t, ct) => onRespond(t.Respond, ct))
        {
        }

        /// <summary>
        /// 以非同步方式送出請求。
        /// </summary>
        /// <param name="ct">取消權杖。</param>
        public UniTask<NetServiceAsyncRespond<TRespond>> RequestAsync(CancellationToken ct = default)
            => base.RequestAsync(AsyncUnit.Default, ct);

        /// <summary>
        /// 以 Callback 方式送出請求。
        /// </summary>
        /// <param name="onSuccess">成功時的回呼，附帶回應資料。</param>
        /// <param name="onFailure">失敗時的回呼，附帶錯誤訊息。</param>
        /// <param name="onException">
        /// 非預期例外的回呼。API 錯誤與請求取消（<see cref="OperationCanceledException"/>）都會由
        /// <paramref name="onFailure"/> 回報，不會進到這裡；若持續收到 onException，通常代表問題並非出在呼叫端，
        /// 可聯絡 EWova 官方支援協助排查。
        /// </param>
        /// <param name="ct">取消權杖。</param>
        public void Request(
            Action<TRespond> onSuccess,
            Action<string> onFailure,
            Action<Exception> onException = null,
            CancellationToken ct = default)
            => base.Request(AsyncUnit.Default, onSuccess, onFailure, onException, ct);
    }

    public sealed class NetServiceRequestRespond<TRequest, TRespond> : NetService<TRequest, TRespond>
    {
        /// <summary>
        /// 建立一個有請求內容、也有回應資料的網路服務。
        /// </summary>
        /// <param name="handler">請求處理器。</param>
        /// <param name="func">實際發送請求並取得回應資料的邏輯。</param>
        /// <param name="onRespond">成功後的額外處理（選填）。</param>
        public NetServiceRequestRespond(
            NetServiceRequestHandler handler,
            Func<TRequest, CancellationToken, UniTask<TRespond>> func,
            Func<(TRequest Request, TRespond Respond), CancellationToken, UniTask> onRespond = null)
            : base(handler, func, onRespond)
        {
        }
    }
}