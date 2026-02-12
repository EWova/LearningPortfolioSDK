using System;

public static class SafeEventInvoker
{
    public static void InvokeSafely<T>(
        this Action<T> handlers,
        T arg,
        Action<Exception> @throw = null)
    {
        if (handlers == null) return;

        foreach (var handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action<T>)handler)(arg);
            }
            catch (Exception ex)
            {
                @throw?.Invoke(ex);
            }
        }
    }
    public static void InvokeSafely(
    this Action handlers,
    Action<Exception> @throw = null)
    {
        if (handlers == null) return;

        foreach (var handler in handlers.GetInvocationList())
        {
            try
            {
                ((Action)handler)();
            }
            catch (Exception ex)
            {
                @throw?.Invoke(ex);
            }
        }
    }
}