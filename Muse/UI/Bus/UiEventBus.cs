using System.Collections.Concurrent;

namespace Muse.UI.Bus;

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

    public void Subscribe<T>(Action<T> handler)
    {
        var delegates = handlers.GetOrAdd(typeof(T), _ => []);
        lock (delegates)
        {
            delegates.Add(handler);
        }
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        if (handlers.TryGetValue(typeof(T), out var list))
        {
            lock (list)
            {
                list.Remove(handler);
            }
        }
    }
}
