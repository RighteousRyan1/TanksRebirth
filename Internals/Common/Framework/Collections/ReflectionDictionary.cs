using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TanksRebirth.Internals.Common.Framework.Collections {
    [Flags]
    public enum MemberType {
        Fields,
        Properties
    }

    /// <summary>Creates a dictionary of member names within a given type.</summary>
    /// <typeparam name="TClass"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ReflectionDictionary<TClass, TValue> where TClass : class, new() {
        public int Count => _dictionary.Count;
        public string[] Keys { get; private set; }
        public TValue[] Values { get; private set; }
        private readonly Dictionary<string, TValue> _dictionary;

        private readonly MemberType _members;

        /// <summary>
        /// Creates a new <see cref="ReflectionDictionary{TType, TKey, TValue}"/>.<para></para>
        /// Using bitwise flags can be used for using multiple member types.
        /// </summary>
        /// <param name="members">The kind(s) of members to allow for insertion into the dictionary.</param>
        public ReflectionDictionary(MemberType members) {
            var fields = typeof(TClass).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var properties =
                typeof(TClass).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            _dictionary = new(fields.Length);
            _members = members;
            Initialize(fields, properties);
            Keys = _dictionary.Keys.ToArray();
            Values = _dictionary.Values.ToArray();
        }

        private void Initialize(in FieldInfo[] fields, in PropertyInfo[] properties) {
            if (_members.HasFlag(MemberType.Fields))
                foreach (var field in fields)
                    if (field.FieldType == typeof(TValue))
                        _dictionary.Add(field.Name, (TValue)field.GetValue(null));
            if (_members.HasFlag(MemberType.Properties))
                foreach (var property in properties)
                    if (property.PropertyType == typeof(TValue))
                        _dictionary.Add(property.Name, (TValue)property.GetValue(null));
        }

        /// <summary>Attempts to retrieve the value of the given key. If it doesn't exist, returns the default value.</summary>
        /// <param name="key">The key of which to grab the value from.</param>
        /// <returns>The corresponding value.</returns>
        public TValue? GetValue(string key) {
            // Simply try getting the value, if it fails, return default.
            return _dictionary.TryGetValue(key, out var value) ? value : default;
        }

        /// <summary>Attempts to retrieve the key of the given value. If it doesn't exist, returns the default value.</summary>
        /// <param name="value">The value of which to grab the key from.</param>
        /// <returns>The corresponding key.</returns>
        public string? GetKey(TValue value) {
            foreach (var kvp in _dictionary) {
                if (EqualityComparer<TValue>.Default.Equals(value, kvp.Value))
                    return kvp.Key;
            }

            return default;
        }

        public bool ContainsKey(string key) => _dictionary.ContainsKey(key);
        public bool ContainsValue(TValue value) => _dictionary.ContainsValue(value);

        public int[] ForcefullyInsertRange(Span<string> keys, Span<TValue> values) {
            if (keys.Length != values.Length)
                throw new ArgumentException("keys and values must be the same length", nameof(keys));

            var result = new int[keys.Length];

            for (var i = 0; i < result.Length; i++) {
                result[i] = ForcefullyInsert(keys[i], values[i], result.Length - 1 == i);
            }

            return result;
        }

        public int[] ForcefullyInsertRange(IEnumerable<string> keys, IEnumerable<TValue> values) {
            var keysList = keys.ToList();
            var valuesList = values.ToList();
            if (keysList.Count != valuesList.Count)
                throw new ArgumentException("keys and values must be the same length", nameof(keys));

            var result = new int[keysList.Count];

            for (var i = 0; i < result.Length; i++) {
                result[i] = ForcefullyInsert(keysList[i], valuesList[i], result.Length - 1 == i);
            }

            return result;
        }
        /// <summary>
        /// Forcefully inserts an entry into this <see cref="ReflectionDictionary{TClass, TValue}"/>. 
        /// <para>Give a <see cref="string"/> key, and the method returns where the value was inserted.</para>
        /// </summary>
        /// <param name="key">The <see cref="string"/> key that should be used to access a given value.</param>
        /// /// <param name="value">The value to insert at this given location.</param>
        /// <returns></returns>
        public int ForcefullyInsert(string key, TValue value) {
            return ForcefullyInsert(key, value, true);
        }

        private int ForcefullyInsert(string key, TValue value, bool refreshCache) {
            if (_dictionary.ContainsKey(key))
                return Array.IndexOf(Keys, key);
            if (_dictionary.ContainsValue(value))
                return Array.IndexOf(Values, value);

            _dictionary.Add(key, value);

            if (!refreshCache) // Only refresh the Keys and Values properties if we are requested to.
                return Array.IndexOf(Keys, key);

            Keys = _dictionary.Keys.ToArray();
            Values = _dictionary.Values.ToArray();
            return Array.IndexOf(Keys, key);
        }
    }
}