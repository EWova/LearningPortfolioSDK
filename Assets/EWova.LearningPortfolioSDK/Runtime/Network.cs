using System;
using System.Collections.Generic;

using UnityEngine;

using EWova.NetService.Model;
using Cysharp.Threading.Tasks;

namespace EWova.LearningPortfolio
{
    [Serializable]
    public class NetServiceRequestHandler
    {
        private readonly object _lock = new();
        private readonly Queue<Func<UniTask>> m_queue = new();
        [SerializeField] private bool m_processing = false;
        [SerializeField] private int m_pendingCount = 0;

        public bool IsAnyNetSerivceRequesting => m_processing;

        internal void Queue(Func<UniTask> request)
        {
            bool startProcessing = false;

            lock (_lock)
            {
                m_queue.Enqueue(request);
                m_pendingCount = m_queue.Count;
                if (!m_processing)
                {
                    m_processing = true;
                    startProcessing = true;
                }
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
                Func<UniTask> req = null;
                lock (_lock)
                {
                    if (m_queue.Count == 0)
                    {
                        m_processing = false;
                        m_pendingCount = 0;
                        return;
                    }
                    req = m_queue.Dequeue();
                    m_pendingCount = m_queue.Count;
                }

                // 這裡直接呼叫 delegate
                await req();
            }
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

        public static NetServiceAsyncRespond ResultFailed(string errorMessage, ErrorHandleException handleEx)
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

        public static NetServiceAsyncRespond<T> ResultFailed(string errorMessage, ErrorHandleException handleEx)
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
        internal readonly Func<UniTask> m_func;
        internal readonly Func<UniTask> m_respondFunc;

        public NetSerivceVoid(NetServiceRequestHandler requestHandler, Func<UniTask> func, Func<UniTask> respondFunc = null)
            : base(requestHandler)
        {
            m_func = func ?? throw new ArgumentNullException(nameof(func));
            m_respondFunc = respondFunc;
        }

        private async UniTask<NetServiceAsyncRespond> RunAsync()
        {
            try
            {
                await m_func();

                if (m_respondFunc != null)
                    await m_respondFunc();

                return NetServiceAsyncRespond.ResultSuccess();
            }
            catch (ErrorHandleException ex)
            {
                return NetServiceAsyncRespond.ResultFailed(ex.ErrorMessage, ex);
            }
            catch (Exception ex)
            {
                return NetServiceAsyncRespond.ResultException(ex);
            }
        }

        public UniTask<NetServiceAsyncRespond> RequestAsync()
        {
            var tcs = new UniTaskCompletionSource<NetServiceAsyncRespond>();
            RequestHandler.Queue(async () =>
            {
                var result = await RunAsync();
                tcs.TrySetResult(result);
            });
            return tcs.Task;
        }

        public void Request(
            Action onSuccess,
            Action<string> onFailure,
            Action<Exception> onException = null)
        {
            RequestHandler.Queue(async () =>
            {
                var result = await RunAsync();

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
        public NetSerivceRequest(NetServiceRequestHandler requestHandler, Func<TRequest, UniTask> func, Func<TRequest, UniTask> newValueFunc) : base(requestHandler)
        {
            m_func = func ?? throw new ArgumentNullException(nameof(func));
            m_newValueFunc = newValueFunc;
        }
        internal readonly Func<TRequest, UniTask> m_func;
        internal readonly Func<TRequest, UniTask> m_newValueFunc;

        private async UniTask<NetServiceAsyncRespond> RunAsync(TRequest value)
        {
            try
            {
                await m_func(value);

                if (m_newValueFunc != null)
                    await m_newValueFunc(value);

                return NetServiceAsyncRespond.ResultSuccess();
            }
            catch (ErrorHandleException ex)
            {
                return NetServiceAsyncRespond.ResultFailed(ex.ErrorMessage, ex);
            }
            catch (Exception ex)
            {
                return NetServiceAsyncRespond.ResultException(ex);
            }
        }

        public UniTask<NetServiceAsyncRespond> RequestAsync(TRequest value)
        {
            var tcs = new UniTaskCompletionSource<NetServiceAsyncRespond>();
            RequestHandler.Queue(async () =>
            {
                var result = await RunAsync(value);
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
            RequestHandler.Queue(async () =>
            {
                var result = await RunAsync(value);

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
        public NetSerivceRespond(NetServiceRequestHandler requestHandler, Func<UniTask<TRespond>> func, Func<TRespond, UniTask> respondFunc) : base(requestHandler)
        {
            m_func = func ?? throw new ArgumentNullException(nameof(func));
            m_respondFunc = respondFunc;
        }
        internal readonly Func<UniTask<TRespond>> m_func;
        internal readonly Func<TRespond, UniTask> m_respondFunc;

        private async UniTask<NetServiceAsyncRespond<TRespond>> RunAsync()
        {
            try
            {
                TRespond respond = await m_func();

                if (m_respondFunc != null)
                    await m_respondFunc(respond);

                return NetServiceAsyncRespond<TRespond>.ResultSuccess(respond);
            }
            catch (ErrorHandleException ex)
            {
                return NetServiceAsyncRespond<TRespond>.ResultFailed(ex.ErrorMessage, ex);
            }
            catch (Exception ex)
            {
                return NetServiceAsyncRespond<TRespond>.ResultException(ex);
            }
        }

        public UniTask<NetServiceAsyncRespond<TRespond>> RequestAsync()
        {
            var tcs = new UniTaskCompletionSource<NetServiceAsyncRespond<TRespond>>();
            RequestHandler.Queue(async () =>
            {
                var result = await RunAsync();
                tcs.TrySetResult(result);
            });
            return tcs.Task;
        }

        public void Request(
            Action<TRespond> onSuccess,
            Action<string> onFailure,
            Action<Exception> onException = null)
        {
            RequestHandler.Queue(async () =>
            {
                var result = await RunAsync();

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
        public NetSerivceRequestRespond(NetServiceRequestHandler requestHandler, Func<TRequest, UniTask<TRespond>> func, Func<(TRequest request, TRespond respond), UniTask> respondAndNewValueFunc) : base(requestHandler)
        {
            m_func = func ?? throw new ArgumentNullException(nameof(func));
            m_respondAndNewValueFunc = respondAndNewValueFunc;
        }
        internal readonly Func<TRequest, UniTask<TRespond>> m_func;
        internal readonly Func<(TRequest request, TRespond respond), UniTask> m_respondAndNewValueFunc;
        private async UniTask<NetServiceAsyncRespond<TRespond>> RunAsync(TRequest value)
        {
            try
            {
                TRespond respond = await m_func(value);

                if (m_respondAndNewValueFunc != null)
                    await m_respondAndNewValueFunc((value, respond));

                return NetServiceAsyncRespond<TRespond>.ResultSuccess(respond);
            }
            catch (ErrorHandleException ex)
            {
                return NetServiceAsyncRespond<TRespond>.ResultFailed(ex.ErrorMessage, ex);
            }
            catch (Exception ex)
            {
                return NetServiceAsyncRespond<TRespond>.ResultException(ex);
            }
        }

        public UniTask<NetServiceAsyncRespond<TRespond>> RequestAsync(TRequest value)
        {
            var tcs = new UniTaskCompletionSource<NetServiceAsyncRespond<TRespond>>();
            RequestHandler.Queue(async () =>
            {
                var result = await RunAsync(value);
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
            RequestHandler.Queue(async () =>
            {
                var result = await RunAsync(value);

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