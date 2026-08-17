using System;
using System.Collections.Generic;

using UnityEngine;

using Cysharp.Threading.Tasks;
using System.Threading;

namespace EWova.LearningPortfolio
{
    public class NetServiceRequestHandler : IDisposable
    {
        private readonly Queue<Func<CancellationToken, UniTask>> m_queue = new();
        private bool m_processing = false;
        public int PendingCount { get; private set; } = 0;

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public bool IsAnyNetSerivceRequesting => m_processing;

        internal void Queue(Func<CancellationToken, UniTask> request)
        {
            bool startProcessing = false;

            m_queue.Enqueue(request);
            PendingCount = m_queue.Count;
            if (!m_processing)
            {
                m_processing = true;
                startProcessing = true;
            }

            if (startProcessing)
            {
                _ = ProcessAsync();
            }
        }

        private async UniTaskVoid ProcessAsync()
        {
            while (true)
            {
                Func<CancellationToken, UniTask> req = null;
                CancellationToken token;

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    ClearQueueWithCancel();
                    return;
                }

                if (m_queue.Count == 0)
                {
                    m_processing = false;
                    PendingCount = 0;
                    return;
                }
                req = m_queue.Dequeue();
                PendingCount = m_queue.Count;

                // 取得當前的 Token
                token = _cancellationTokenSource.Token;

                try
                {
                    // 同時使用 SuppressCancellationThrow() 避免拋出 OperationCanceledException 導致 ProcessAsync 異常中斷
                    bool isCanceled = await req(token).SuppressCancellationThrow();
                    await UniTask.Yield();

                    if (isCanceled || token.IsCancellationRequested)
                    {
                        ClearQueueWithCancel();
                        return;
                    }
                }
                catch (Exception ex)
                {
                    // 處理非取消引發的其他未知異常，避免整個佇列卡死
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>
        /// 呼叫此方法來取消當前正在進行與排隊中的所有請求
        /// </summary>
        public void CancelAll()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            // 如果當前沒有在處理中，直接清空佇列
            if (!m_processing)
            {
                m_queue.Clear();
                PendingCount = 0;
            }
        }

        private void ClearQueueWithCancel()
        {
            m_queue.Clear();
            m_processing = false;
            PendingCount = 0;
        }

        public void Dispose()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
            m_queue.Clear();
        }
    }
    public enum AsyncRespondStatus
    {
        Success,
        Failed,
    }
    public readonly struct NetServiceAsyncRespond
    {
        public readonly AsyncRespondStatus Status;
        public readonly LearningPortfolioApiException LearningPortfolioApiException;
        public readonly string ErrorMessage => LearningPortfolioApiException?.Message ?? string.Empty;
        public bool IsSuccess => Status == AsyncRespondStatus.Success;
        public bool IsFailed => Status == AsyncRespondStatus.Failed;

        private NetServiceAsyncRespond(
            AsyncRespondStatus status,
            LearningPortfolioApiException learningPortfolioApiException)
        {
            Status = status;
            LearningPortfolioApiException = learningPortfolioApiException;
        }

        public static NetServiceAsyncRespond ResultSuccess()
            => new(AsyncRespondStatus.Success, null);
        public static NetServiceAsyncRespond ResultFailed(LearningPortfolioApiException learningPortfolioApiException = null)
            => new(AsyncRespondStatus.Failed, learningPortfolioApiException);
    }
    public readonly struct NetServiceAsyncRespond<T> where T : class
    {
        public readonly T Data;
        public readonly AsyncRespondStatus Status;
        public readonly LearningPortfolioApiException LearningPortfolioApiException;
        public readonly string ErrorMessage => LearningPortfolioApiException?.Message ?? string.Empty;

        public bool IsSuccess => Status == AsyncRespondStatus.Success;
        public bool IsFailed => Status == AsyncRespondStatus.Failed;

        private NetServiceAsyncRespond(T data, AsyncRespondStatus status, LearningPortfolioApiException learningPortfolioApiException)
        {
            Data = data;
            Status = status;
            LearningPortfolioApiException = learningPortfolioApiException;
        }

        public static NetServiceAsyncRespond<T> ResultSuccess(T data)
            => new(data, AsyncRespondStatus.Success, null);
        public static NetServiceAsyncRespond<T> ResultFailed(LearningPortfolioApiException learningPortfolioApiException)
            => new(null, AsyncRespondStatus.Failed, learningPortfolioApiException);
    }

    public abstract class NetSerivceBase
    {
        public NetSerivceBase(NetServiceRequestHandler requestHandler)
        {
            RequestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
        }
        internal NetServiceRequestHandler RequestHandler;
    }
    public class NetSerivceVoid : NetSerivceBase
    {
        internal readonly Func<CancellationToken, UniTask> m_func;
        internal readonly Func<CancellationToken, UniTask> m_respondFunc;

        public NetSerivceVoid(NetServiceRequestHandler requestHandler, Func<CancellationToken, UniTask> func, Func<CancellationToken, UniTask> respondFunc = null)
            : base(requestHandler)
        {
            m_func = func ?? throw new ArgumentNullException(nameof(func));
            m_respondFunc = respondFunc;
        }

        private async UniTask<NetServiceAsyncRespond> RunAsync(CancellationToken ct)
        {
            try
            {
                await m_func(ct);

                if (m_respondFunc != null)
                    await m_respondFunc(ct);

                return NetServiceAsyncRespond.ResultSuccess();
            }
            catch (LearningPortfolioApiException ex)
            {
                return NetServiceAsyncRespond.ResultFailed(ex);
            }
        }

        public UniTask<NetServiceAsyncRespond> RequestAsync()
        {
            var tcs = new UniTaskCompletionSource<NetServiceAsyncRespond>();
            RequestHandler.Queue(async (ct) =>
            {
                var result = await RunAsync(ct);
                tcs.TrySetResult(result);
            });
            return tcs.Task;
        }

        public void Request(
            Action onSuccess,
            Action<string> onFailure,
            Action<Exception> onException = null)
        {
            RequestHandler.Queue(async (ct) =>
            {
                try
                {
                    var result = await RunAsync(ct);

                    if (result.IsSuccess)
                        onSuccess?.Invoke();
                    else
                        onFailure?.Invoke(result.ErrorMessage);
                }
                catch (OperationCanceledException)
                {
                    onFailure?.Invoke("Request was canceled.");
                }
                catch (Exception ex)
                {
                    onException?.Invoke(ex);
                }
            });
        }
    }
    public class NetSerivceRequest<TRequest> : NetSerivceBase
    {
        public NetSerivceRequest(NetServiceRequestHandler requestHandler, Func<TRequest, CancellationToken, UniTask> func, Func<TRequest, CancellationToken, UniTask> newValueFunc) : base(requestHandler)
        {
            m_func = func ?? throw new ArgumentNullException(nameof(func));
            m_newValueFunc = newValueFunc;
        }
        internal readonly Func<TRequest, CancellationToken, UniTask> m_func;
        internal readonly Func<TRequest, CancellationToken, UniTask> m_newValueFunc;

        private async UniTask<NetServiceAsyncRespond> RunAsync(TRequest value, CancellationToken ct)
        {
            try
            {
                await m_func(value, ct);

                if (m_newValueFunc != null)
                    await m_newValueFunc(value, ct);

                return NetServiceAsyncRespond.ResultSuccess();
            }
            catch (LearningPortfolioApiException ex)
            {
                return NetServiceAsyncRespond.ResultFailed(ex);
            }
        }

        public UniTask<NetServiceAsyncRespond> RequestAsync(TRequest value)
        {
            var tcs = new UniTaskCompletionSource<NetServiceAsyncRespond>();
            RequestHandler.Queue(async (ct) =>
            {
                var result = await RunAsync(value, ct);
                tcs.TrySetResult(result);
            });
            return tcs.Task;
        }

        public void Request(
            TRequest value,
            Action onSuccess,
            Action<string> onFailure,
            Action<Exception> onException = null)
        {
            RequestHandler.Queue(async (ct) =>
            {
                try
                {
                    var result = await RunAsync(value, ct);

                    if (result.IsSuccess)
                        onSuccess?.Invoke();
                    else
                        onFailure?.Invoke(result.ErrorMessage);
                }
                catch (OperationCanceledException)
                {
                    onFailure?.Invoke("Request was canceled.");
                }
                catch (Exception ex)
                {
                    onException?.Invoke(ex);
                }
            });
        }

    }
    public class NetSerivceRespond<TRespond> : NetSerivceBase where TRespond : class
    {
        public NetSerivceRespond(NetServiceRequestHandler requestHandler, Func<CancellationToken, UniTask<TRespond>> func, Func<TRespond, CancellationToken, UniTask> respondFunc) : base(requestHandler)
        {
            m_func = func ?? throw new ArgumentNullException(nameof(func));
            m_respondFunc = respondFunc;
        }
        internal readonly Func<CancellationToken, UniTask<TRespond>> m_func;
        internal readonly Func<TRespond, CancellationToken, UniTask> m_respondFunc;

        private async UniTask<NetServiceAsyncRespond<TRespond>> RunAsync(CancellationToken ct)
        {
            try
            {
                TRespond respond = await m_func(ct);

                if (m_respondFunc != null)
                    await m_respondFunc(respond, ct);

                return NetServiceAsyncRespond<TRespond>.ResultSuccess(respond);
            }
            catch (LearningPortfolioApiException ex)
            {
                return NetServiceAsyncRespond<TRespond>.ResultFailed(ex);
            }
        }

        public UniTask<NetServiceAsyncRespond<TRespond>> RequestAsync()
        {
            var tcs = new UniTaskCompletionSource<NetServiceAsyncRespond<TRespond>>();
            RequestHandler.Queue(async (ct) =>
            {
                var result = await RunAsync(ct);
                tcs.TrySetResult(result);
            });
            return tcs.Task;
        }

        public void Request(
            Action<TRespond> onSuccess,
            Action<string> onFailure,
            Action<Exception> onException = null)
        {
            RequestHandler.Queue(async (ct) =>
            {
                try
                {
                    var result = await RunAsync(ct);

                    if (result.IsSuccess)
                        onSuccess?.Invoke(result.Data);
                    else
                        onFailure?.Invoke(result.ErrorMessage);
                }
                catch (OperationCanceledException)
                {
                    onFailure?.Invoke("Request was canceled.");
                }
                catch (Exception ex)
                {
                    onException?.Invoke(ex);
                }
            });
        }

    }
    public class NetSerivceRequestRespond<TRequest, TRespond> : NetSerivceBase where TRespond : class
    {
        public NetSerivceRequestRespond(NetServiceRequestHandler requestHandler, Func<TRequest, CancellationToken, UniTask<TRespond>> func, Func<(TRequest request, TRespond respond), CancellationToken, UniTask> respondAndNewValueFunc) : base(requestHandler)
        {
            m_func = func ?? throw new ArgumentNullException(nameof(func));
            m_respondAndNewValueFunc = respondAndNewValueFunc;
        }
        internal readonly Func<TRequest, CancellationToken, UniTask<TRespond>> m_func;
        internal readonly Func<(TRequest request, TRespond respond), CancellationToken, UniTask> m_respondAndNewValueFunc;
        private async UniTask<NetServiceAsyncRespond<TRespond>> RunAsync(TRequest value, CancellationToken ct)
        {
            try
            {
                TRespond respond = await m_func(value, ct);

                if (m_respondAndNewValueFunc != null)
                    await m_respondAndNewValueFunc((value, respond), ct);

                return NetServiceAsyncRespond<TRespond>.ResultSuccess(respond);
            }
            catch (LearningPortfolioApiException ex)
            {
                return NetServiceAsyncRespond<TRespond>.ResultFailed(ex);
            }
        }

        public UniTask<NetServiceAsyncRespond<TRespond>> RequestAsync(TRequest value)
        {
            var tcs = new UniTaskCompletionSource<NetServiceAsyncRespond<TRespond>>();
            RequestHandler.Queue(async (ct) =>
            {
                var result = await RunAsync(value, ct);
                tcs.TrySetResult(result);
            });
            return tcs.Task;
        }

        public void Request(
            TRequest value,
            Action<TRespond> onSuccess,
            Action<string> onFailure,
            Action<Exception> onException = null)
        {
            RequestHandler.Queue(async (ct) =>
            {
                try
                {
                    var result = await RunAsync(value, ct);

                    if (result.IsSuccess)
                        onSuccess?.Invoke(result.Data);
                    else
                        onFailure?.Invoke(result.ErrorMessage);
                }
                catch (OperationCanceledException)
                {
                    onFailure?.Invoke("Request was canceled.");
                }
                catch (Exception ex)
                {
                    onException?.Invoke(ex);
                }
            });
        }
    }
}