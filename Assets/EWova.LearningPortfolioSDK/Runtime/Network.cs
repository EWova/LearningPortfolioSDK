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

        public static NetServiceAsyncRespond ResultSuccess() => new(AsyncRespondStatus.Success, null);
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

        public static NetServiceAsyncRespond<T> ResultSuccess(T data) => new(data, AsyncRespondStatus.Success, null);
        public static NetServiceAsyncRespond<T> ResultFailed(LearningPortfolioApiException ex) => new(default, AsyncRespondStatus.Failed, ex);
    }

    public abstract class NetSerivceBase
    {
        protected NetSerivceBase(NetServiceRequestHandler requestHandler)
        {
            RequestHandler = requestHandler ?? throw new ArgumentNullException(nameof(requestHandler));
        }
        internal readonly NetServiceRequestHandler RequestHandler;
    }

    public class NetServiceCommand<TRequest> : NetSerivceBase
    {
        private readonly Func<TRequest, CancellationToken, UniTask> m_func;
        private readonly Func<TRequest, CancellationToken, UniTask> m_onDone;

        public NetServiceCommand(
            NetServiceRequestHandler requestHandler,
            Func<TRequest, CancellationToken, UniTask> func,
            Func<TRequest, CancellationToken, UniTask> onRespond = null)
            : base(requestHandler)
        {
            m_func = func ?? throw new ArgumentNullException(nameof(func));
            m_onDone = onRespond;
        }

        private async UniTask<NetServiceAsyncRespond> RunAsync(TRequest request, CancellationToken ct)
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

        public UniTask<NetServiceAsyncRespond> RequestAsync(TRequest request, CancellationToken cancellationToken = default)
            => RequestHandler.EnqueueAsync(token => RunAsync(request, token), cancellationToken);

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

    public class NetService<TRequest, TRespond> : NetSerivceBase
    {
        private readonly Func<TRequest, CancellationToken, UniTask<TRespond>> m_func;
        private readonly Func<(TRequest Request, TRespond Respond), CancellationToken, UniTask> m_onRespond;

        public NetService(
            NetServiceRequestHandler requestHandler,
            Func<TRequest, CancellationToken, UniTask<TRespond>> func,
            Func<(TRequest Request, TRespond Respond), CancellationToken, UniTask> onRespond = null)
            : base(requestHandler)
        {
            m_func = func ?? throw new ArgumentNullException(nameof(func));
            m_onRespond = onRespond;
        }

        private async UniTask<NetServiceAsyncRespond<TRespond>> RunAsync(TRequest request, CancellationToken ct)
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

        public UniTask<NetServiceAsyncRespond<TRespond>> RequestAsync(TRequest request, CancellationToken cancellationToken = default)
            => RequestHandler.EnqueueAsync(token => RunAsync(request, token), cancellationToken);

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
        public NetServiceVoid(NetServiceRequestHandler handler, Func<CancellationToken, UniTask> func, Func<CancellationToken, UniTask> onRespond = null)
            : base(handler,
                (_, ct) => func(ct),
                onRespond == null ? null : (_, ct) => onRespond(ct))
        {
        }

        public UniTask<NetServiceAsyncRespond> RequestAsync(CancellationToken ct = default)
            => RequestAsync(AsyncUnit.Default, ct);

        public void Request(Action onSuccess, Action<string> onFailure, Action<Exception> onException = null, CancellationToken ct = default)
            => Request(AsyncUnit.Default, onSuccess, onFailure, onException, ct);
    }

    public sealed class NetServiceRequest<TRequest> : NetServiceCommand<TRequest>
    {
        public NetServiceRequest(NetServiceRequestHandler handler, Func<TRequest, CancellationToken, UniTask> func, Func<TRequest, CancellationToken, UniTask> onRespond = null)
            : base(handler, func, onRespond)
        {
        }
    }

    public sealed class NetServiceRespond<TRespond> : NetService<AsyncUnit, TRespond>
    {
        public NetServiceRespond(NetServiceRequestHandler handler, Func<CancellationToken, UniTask<TRespond>> func, Func<TRespond, CancellationToken, UniTask> onRespond = null)
            : base(handler,
                (_, ct) => func(ct),
                onRespond == null ? null : (t, ct) => onRespond(t.Respond, ct))
        {
        }

        public UniTask<NetServiceAsyncRespond<TRespond>> RequestAsync(CancellationToken ct = default)
            => RequestAsync(AsyncUnit.Default, ct);

        public void Request(Action<TRespond> onSuccess, Action<string> onFailure, Action<Exception> onException = null, CancellationToken ct = default)
            => Request(AsyncUnit.Default, onSuccess, onFailure, onException, ct);
    }

    public sealed class NetServiceRequestRespond<TRequest, TRespond> : NetService<TRequest, TRespond>
    {
        public NetServiceRequestRespond(
            NetServiceRequestHandler handler,
            Func<TRequest, CancellationToken, UniTask<TRespond>> func,
            Func<(TRequest Request, TRespond Respond), CancellationToken, UniTask> onRespond = null)
            : base(handler, func, onRespond)
        {
        }
    }
}