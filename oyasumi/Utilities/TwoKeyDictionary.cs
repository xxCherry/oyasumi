using System.Collections.Concurrent;
using System.Collections.Generic;

namespace oyasumi.Utilities
{
    public class TwoKeyDictionary<P, S, V>
    {
        private readonly ConcurrentDictionary<P, V> _primaryDictionary = new ConcurrentDictionary<P, V>();
        private readonly ConcurrentDictionary<P, S> _gateDictionary = new ConcurrentDictionary<P, S>(); // used to get secondary key from primary
        private readonly ConcurrentDictionary<S, V> _secondaryDictionary = new ConcurrentDictionary<S, V>();

        public V this[P primary] => _primaryDictionary.TryGetValue(primary, out var item) ? item : default;
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
            var secondary = _gateDictionary[primary];

            _primaryDictionary.TryRemove(primary, out _);
            _secondaryDictionary.TryRemove(secondary, out _);
            _gateDictionary.TryRemove(primary, out _);
        }
    }
}