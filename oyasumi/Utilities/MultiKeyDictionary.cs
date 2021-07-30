using System.Linq;
using System.Collections.Concurrent;
using System;
using System.Collections.Generic;

namespace oyasumi.Utilities
{
    public class MultiKeyDictionary<T1, T2, V> : IMultiKeyDictionary
    {
        private readonly ConcurrentDictionary<T1, V> _firstDictionary = new();
        private readonly ConcurrentDictionary<T2, V> _secondDictionary = new();
        private readonly ConcurrentDictionary<T1, T2> _gateDictionary = new();
        private readonly ConcurrentDictionary<T2, T1> _reverseGateDictionary = new();

        public V this[T1 primary]
        {
            get => _firstDictionary.TryGetValue(primary, out var item) ? item : default;
            set
            {
                _gateDictionary.TryGetValue(primary, out var secondary);
                _firstDictionary.TryUpdate(primary, value,
                    _firstDictionary.TryGetValue(primary, out var item1) ? item1 : default);
                _secondDictionary.TryUpdate(secondary, value,
                    _secondDictionary.TryGetValue(secondary, out var item2) ? item2 : default);
            }
        }

        public V this[T2 secondary]
        {
            get => _secondDictionary.TryGetValue(secondary, out var item) ? item : default;
            set
            {
                _reverseGateDictionary.TryGetValue(secondary, out var primary);
                _firstDictionary.TryUpdate(primary, value,
                    _firstDictionary.TryGetValue(primary, out var item1) ? item1 : default);
                _secondDictionary.TryUpdate(secondary, value,
                    _secondDictionary.TryGetValue(secondary, out var item2) ? item2 : default);
            }
        }

        public V[] Values => _firstDictionary.Values.ToArray();
        public void RemoveAll(Predicate<V> match)
        {
            var dictCopy = _firstDictionary;
            foreach (var kvp in dictCopy)
            {
                if (match(kvp.Value))
                    Remove(kvp.Key);
            }
        }

        public void ExecuteWhere(Predicate<V> filter, Func<V, V> action)
        {
            foreach (var kvp in _firstDictionary)
            {
                if (filter(kvp.Value))
                    Modify(kvp.Key, action(kvp.Value));
            }
        }

        public int Count => _firstDictionary.Count;

        public void Add(T1 primary, T2 secondary, V value)
        {
            _firstDictionary.TryAdd(primary, value);
            _secondDictionary.TryAdd(secondary, value);

            _gateDictionary.TryAdd(primary, secondary);
            _reverseGateDictionary.TryAdd(secondary, primary);
        }

        public void Modify(T1 primary, V value)
        {
            _gateDictionary.TryGetValue(primary, out var secondary);

            this[primary] = value;
            this[secondary] = value;
        }

        public void Add(object primaryRaw, object secondaryRaw, object valueRaw)
        {
            var primary = (T1)primaryRaw;
            var secondary = (T2)secondaryRaw;
            var value = (V)valueRaw;

            _firstDictionary.TryAdd(primary, value);
            _secondDictionary.TryAdd(secondary, value);

            _gateDictionary.TryAdd(primary, secondary);
            _reverseGateDictionary.TryAdd(secondary, primary);
        }

        public object ValueAt(int index)
            => _firstDictionary.ElementAt(index).Value;
        
        public void Remove(T1 primary)
        {
            _gateDictionary.TryGetValue(primary, out var secondary);

            _firstDictionary.TryRemove(primary, out _);
            _secondDictionary.TryRemove(secondary, out _);

            _gateDictionary.TryRemove(primary, out _);
            _reverseGateDictionary.TryRemove(secondary, out _);
        }
    }
}