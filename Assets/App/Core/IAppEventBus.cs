using System;

namespace App.Core
{
    public interface IAppEventBus
    {
        void Publish<TEvent>(TEvent appEvent);

        void Subscribe<TEvent>(Action<TEvent> handler);

        void Unsubscribe<TEvent>(Action<TEvent> handler);
    }
}
