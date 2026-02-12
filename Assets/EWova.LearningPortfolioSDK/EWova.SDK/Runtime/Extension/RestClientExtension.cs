using Cysharp.Threading.Tasks;

using Proyecto26;

using System.Threading;


namespace EWova.NetService
{
    public static class RestClientExtension
    {
        public static UniTask<ResponseHelper> AsUniTask(this RSG.IPromise<ResponseHelper> IPromise, CancellationToken cancellationToken = default)
        {
            var source = new UniTaskCompletionSource<ResponseHelper>();

            cancellationToken.Register(() => { source.TrySetCanceled(); });

            IPromise
            .Then(res =>
            {
                if (!cancellationToken.IsCancellationRequested)
                    source.TrySetResult(res);
            })
            .Catch(ex =>
            {
                if (!cancellationToken.IsCancellationRequested)
                    source.TrySetException(ex);
            });

            return source.Task;
        }
        public static UniTask AsUniTask(this RSG.IPromise IPromise, CancellationToken cancellationToken = default)
        {
            var source = new UniTaskCompletionSource();

            cancellationToken.Register(() => { source.TrySetCanceled(); });

            IPromise
            .Then(() =>
            {
                if (!cancellationToken.IsCancellationRequested)
                    source.TrySetResult();
            })
            .Catch(ex =>
            {
                if (!cancellationToken.IsCancellationRequested)
                    source.TrySetException(ex);
            });

            return source.Task;
        }
        public static UniTask<T> AsUniTask<T>(this RSG.IPromise<T> IPromise, CancellationToken cancellationToken = default)
        {
            var source = new UniTaskCompletionSource<T>();

            cancellationToken.Register(() => { source.TrySetCanceled(); });

            IPromise
            .Then(res =>
            {
                if (!cancellationToken.IsCancellationRequested)
                    source.TrySetResult(res);
            })
            .Catch(ex =>
            {
                if (!cancellationToken.IsCancellationRequested)
                    source.TrySetException(ex);
            });

            return source.Task;
        }
        public static UniTask<ResponseHelper> Request(this RSG.IPromise<ResponseHelper> options, CancellationToken cancellationToken = default)
        {
            var source = new UniTaskCompletionSource<ResponseHelper>();

            cancellationToken.Register(() => { source.TrySetCanceled(); });

            options
            .Then(res =>
            {
                if (!cancellationToken.IsCancellationRequested)
                    source.TrySetResult(res);
            })
            .Catch(ex =>
            {
                if (!cancellationToken.IsCancellationRequested)
                    source.TrySetException(ex);
            });

            return source.Task;
        }
    }
}