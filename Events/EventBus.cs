using System.Collections.Generic;
using System.Linq;

namespace Utilities.Event_Bus
{
    public class EventBus<T> where T : IEvent
    {
        private static readonly HashSet<IEventBinding<T>> Bindings = new HashSet<IEventBinding<T>>();

        public static void Register(EventBinding<T> binding) => Bindings.Add(binding); 
        public static void Deregister(EventBinding<T> binding) => Bindings.Remove(binding);

        public static void Raise(T @event)
        {
            IEventBinding<T>[] bindings = Bindings.ToArray(); 
            
            foreach (IEventBinding<T> binding in bindings)
            {
                binding.OnEvent.Invoke(@event);
                binding.OnEventNoArgs.Invoke();
            }
        }
    }
}