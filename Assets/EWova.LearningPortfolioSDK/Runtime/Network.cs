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
    public readonly struct NetServiceAsyncRespond
    {
        public string ErrorMessage { get; }
        public Exception Exception { get; }
        public bool IsSuccess => Status == StatusType.Success;
        public bool IsFailed => Status == StatusType.Failed;
        public bool IsException => Status == StatusType.Exception;
        private StatusType Status { get; }
        private enum StatusType
        {
            Success,
            Failed,
            Exception
        }
        private NetServiceAsyncRespond(string errorMessage, Exception exception, StatusType status)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
            Status = status;
        }

        public static NetServiceAsyncRespond ResultSuccess()
            => new NetServiceAsyncRespond(null, null, StatusType.Success);

        public static NetServiceAsyncRespond ResultFailed(string errorMessage, Exception handleEx)
            => new NetServiceAsyncRespond(errorMessage, handleEx, StatusType.Failed);

        public static NetServiceAsyncRespond ResultException(Exception ex)
            => new NetServiceAsyncRespond(null, ex, StatusType.Exception);
    }
    public readonly struct NetServiceAsyncRespond<T>
    {
        public T Data { get; }
        public string ErrorMessage { get; }
        public Exception Exception { get; }

        public bool IsSuccess => Status == StatusType.Success;
        public bool IsFailed => Status == StatusType.Failed;
        public bool IsException => Status == StatusType.Exception;

        private StatusType Status { get; }

        private enum StatusType
        {
            Success,
            Failed,
            Exception
        }

        private NetServiceAsyncRespond(T data, string errorMessage, Exception exception, StatusType status)
        {
            Data = data;
            ErrorMessage = errorMessage;
            Exception = exception;
            Status = status;
        }

        public static NetServiceAsyncRespond<T> ResultSuccess(T data)
            => new NetServiceAsyncRespond<T>(data, null, null, StatusType.Success);

        public static NetServiceAsyncRespond<T> ResultFailed(string errorMessage, Exception handleEx)
            => new NetServiceAsyncRespond<T>(default, errorMessage, handleEx, StatusType.Failed);

        public static NetServiceAsyncRespond<T> ResultException(Exception ex)
            => new NetServiceAsyncRespond<T>(default, null, ex, StatusType.Exception);
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
                return NetServiceAsyncRespond.ResultFailed(ex.Message, ex);
            }
            catch (OperationCanceledException ex)
            {
                return NetServiceAsyncRespond.ResultFailed("Operation was canceled.", ex);
            }
            catch (Exception ex)
            {
                return NetServiceAsyncRespond.ResultException(ex);
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
                var result = await RunAsync(ct);

                if (result.IsSuccess)
                    onSuccess?.Invoke();
                else if (result.IsFailed)
                    onFailure?.Invoke(result.ErrorMessage);
                else if (result.IsException)
                    onException?.Invoke(result.Exception);
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
                return NetServiceAsyncRespond.ResultFailed(ex.Message, ex);
            }
            catch (OperationCanceledException ex)
            {
                return NetServiceAsyncRespond.ResultFailed("Operation was canceled.", ex);
            }
            catch (Exception ex)
            {
                return NetServiceAsyncRespond.ResultException(ex);
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
                var result = await RunAsync(value, ct);

                if (result.IsSuccess)
                    onSuccess?.Invoke();
                else if (result.IsFailed)
                    onFailure?.Invoke(result.ErrorMessage);
                else if (result.IsException)
                    onException?.Invoke(result.Exception);
            });
        }

    }
    public class NetSerivceRespond<TRespond> : NetSerivceBase
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
                return NetServiceAsyncRespond<TRespond>.ResultFailed(ex.Message, ex);
            }
            catch (OperationCanceledException ex)
            {
                return NetServiceAsyncRespond<TRespond>.ResultFailed("Operation was canceled.", ex);
            }
            catch (Exception ex)
            {
                return NetServiceAsyncRespond<TRespond>.ResultException(ex);
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
                var result = await RunAsync(ct);

                if (result.IsSuccess)
                    onSuccess?.Invoke(result.Data);
                else if (result.IsFailed)
                    onFailure?.Invoke(result.ErrorMessage);
                else if (result.IsException)
                    onException?.Invoke(result.Exception);
            });
        }

    }
    public class NetSerivceRequestRespond<TRequest, TRespond> : NetSerivceBase
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
                return NetServiceAsyncRespond<TRespond>.ResultFailed(ex.Message, ex);
            }
            catch (OperationCanceledException ex)
            {
                return NetServiceAsyncRespond<TRespond>.ResultFailed("Operation was canceled.", ex);
            }
            catch (Exception ex)
            {
                return NetServiceAsyncRespond<TRespond>.ResultException(ex);
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
                var result = await RunAsync(value, ct);

                if (result.IsSuccess)
                    onSuccess?.Invoke(result.Data);
                else if (result.IsFailed)
                    onFailure?.Invoke(result.ErrorMessage);
                else if (result.IsException)
                    onException?.Invoke(result.Exception);
            });
        }
    }
}