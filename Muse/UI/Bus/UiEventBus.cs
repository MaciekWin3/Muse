using System.Collections.Concurrent;

namespace Muse.UI.Bus;

public interface IUiEventBus
{
    void Publish<T>(T message);
    IDisposable Subscribe<T>(Action<T> handler);
    void Unsubscribe<T>(Action<T> handler);
}

public class UiEventBus : IUiEventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> handlers = new();

    public void Publish<T>(T message)
    {
        if (handlers.TryGetValue(typeof(T), out var delegates))
        {
            Delegate[] snapshot;
            lock (delegates)
            {
                snapshot = [.. delegates];
            }

            foreach (var d in snapshot)
            {
                try
                {
                    ((Action<T>)d).Invoke(message);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error in event handler: {ex}");
                }
            }
        }
    }

    public IDisposable Subscribe<T>(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var delegates = handlers.GetOrAdd(typeof(T), _ => []);
        lock (delegates)
        {
            delegates.Add(handler);
        }

        return new Subscription(() => RemoveHandler(typeof(T), delegates, handler));
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        if (handlers.TryGetValue(typeof(T), out var list))
        {
            lock (list)
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    handlers.TryRemove(typeof(T), out _);
                }
            }
        }
    }

    private void RemoveHandler<T>(Type messageType, List<Delegate> list, Action<T> handler)
    {
        lock (list)
        {
            list.Remove(handler);
            if (list.Count == 0)
            {
                handlers.TryRemove(messageType, out _);
            }
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose()
        {
            Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
        }
    }
}
