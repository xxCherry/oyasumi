using System.Collections.Concurrent;
using System.Collections.Generic;

namespace oyasumi.Utilities
{
    public class MultiKeyDictionary<P, S, T, V>
    {
        private readonly ConcurrentDictionary<P, V> _primaryDictionary = new ();
        private readonly ConcurrentDictionary<P, S> _gateDictionary = new (); // used to get secondary key from primary
        private readonly ConcurrentDictionary<S, T> _secondGateDictionary = new(); // used to get tertiary key from secondary
        private readonly ConcurrentDictionary<S, V> _secondaryDictionary = new ();
        private readonly ConcurrentDictionary<T, V> _tertiaryDictionary = new ();

        public V this[P primary] => _primaryDictionary.TryGetValue(primary, out var item) ? item : default;
        public V this[S secondary] => _secondaryDictionary.TryGetValue(secondary, out var item) ? item : default;
        public V this[T tertiary, int dummy] => _tertiaryDictionary.TryGetValue(tertiary, out var item) ? item : default;
        public IEnumerable<V> Values => _primaryDictionary.Values;

        public void Add(P primary, S secondary, T tertiary, V value)
        {
            _primaryDictionary.TryAdd(primary, value);
            _secondaryDictionary.TryAdd(secondary, value);
            _tertiaryDictionary.TryAdd(tertiary, value);

            _gateDictionary.TryAdd(primary, secondary);
            _secondGateDictionary.TryAdd(secondary, tertiary);
        }

        public void Remove(P primary)
        {
            if (primary is null)
                return;
            
            _gateDictionary.TryGetValue(primary, out var secondary);;
            _secondGateDictionary.TryGetValue(secondary, out var tertiary);

            _primaryDictionary.TryRemove(primary, out _);
            _secondaryDictionary.TryRemove(secondary, out _);
            _tertiaryDictionary.TryRemove(tertiary, out _);
            _gateDictionary.TryRemove(primary, out _);
        }
    }

    public class TwoKeyDictionary<P, S, V>
    {
        private readonly ConcurrentDictionary<P, V> _primaryDictionary = new();
        private readonly ConcurrentDictionary<P, S> _gateDictionary = new(); // used to get secondary key from primary
        private readonly ConcurrentDictionary<S, V> _secondaryDictionary = new();

        public V this[P primary]
        {

            get => _primaryDictionary.TryGetValue(primary, out var item) ? item : default;
            set
            {
                _gateDictionary.TryGetValue(primary, out var secondary);
                _primaryDictionary.TryUpdate(primary, value,
                    _primaryDictionary.TryGetValue(primary, out var item1) ? item1 : default);
                _secondaryDictionary.TryUpdate(secondary, value,
                    _primaryDictionary.TryGetValue(primary, out var item2) ? item2 : default);
            }
        }

        public V this[S secondary] => _secondaryDictionary.TryGetValue(secondary, out var item) ? item : default;
        public IEnumerable<V> Values => _primaryDictionary.Values;

        public void Add(P primary, S secondary, V value)
        {
            _primaryDictionary.TryAdd(primary, value);
            _secondaryDictionary.TryAdd(secondary, value);

            _gateDictionary.TryAdd(primary, secondary);
        }

        public void Remove(P primary)
        {
            if (primary is null)
                return;
            
            _gateDictionary.TryGetValue(primary, out var secondary);;

            _primaryDictionary.TryRemove(primary, out _);
            _secondaryDictionary.TryRemove(secondary, out _);
            _gateDictionary.TryRemove(primary, out _);
        }
    }
}