using Newtonsoft.Json.Linq;
using System;
using System.Collections;
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
    // TValue unnecessary, removing.
    public class ReflectionDictionary<TClass> where TClass : class, new() {
        public int Count => _dictionary.Count;
        public string[] Keys { get; private set; }
        public int[] Values { get; private set; }
        private readonly Dictionary<string, int> _dictionary;

        private readonly MemberType _members;

        /// <summary>
        /// Creates a new <see cref="ReflectionDictionary{TKey}"/>.<para></para>
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
                    if (field.FieldType == typeof(int))
                        _dictionary.Add(field.Name, (int)field.GetValue(null)!);
            if (_members.HasFlag(MemberType.Properties))
                foreach (var property in properties)
                    if (property.PropertyType == typeof(int))
                        _dictionary.Add(property.Name, (int)property.GetValue(null)!);
        }

        /// <summary>Attempts to retrieve the value of the given key. If it doesn't exist, returns the default value.</summary>
        /// <param name="key">The key of which to grab the value from.</param>
        /// <returns>The corresponding value.</returns>
        public int? GetValue(string key) {
            // Simply try getting the value, if it fails, return default.
            return _dictionary.TryGetValue(key, out var value) ? value : default;
        }

        /// <summary>Attempts to retrieve the key of the given value. If it doesn't exist, returns the default value.</summary>
        /// <param name="value">The value of which to grab the key from.</param>
        /// <returns>The corresponding key.</returns>
        public string? GetKey(int value) {
            foreach (var kvp in _dictionary) {
                if (EqualityComparer<int>.Default.Equals(value, kvp.Value))
                    return kvp.Key;
            }

            return default;
        }

        public bool ContainsKey(string key) => _dictionary.ContainsKey(key);
        public bool ContainsValue(int value) => _dictionary.ContainsValue(value);

        public int[] ForcefullyInsertRange(Span<string> keys, Span<int> values) {
            if (keys.Length != values.Length)
                throw new ArgumentException("keys and values must be the same length", nameof(keys));

            var result = new int[keys.Length];

            for (var i = 0; i < result.Length; i++) {
                result[i] = ForcefullyInsert(keys[i], values[i]/*, result.Length - 1 == i*/);
            }

            return result;
        }

        public int[] ForcefullyInsertRange(IEnumerable<string> keys, IEnumerable<int> values) {
            var keysList = keys.ToList();
            var valuesList = values.ToList();
            if (keysList.Count != valuesList.Count)
                throw new ArgumentException("keys and values must be the same length", nameof(keys));

            var result = new int[keysList.Count];

            for (var i = 0; i < result.Length; i++) {
                result[i] = ForcefullyInsert(keysList[i], valuesList[i]/*, result.Length - 1 == i*/);
            }

            return result;
        }
        /// <summary>
        /// This overload makes the inserted value the same as the next available space.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int ForcefullyInsert(string key) {
            if (_dictionary.ContainsKey(key))
                return Array.IndexOf(Keys, key);

            _dictionary.Add(key, 0);

            var val = Array.IndexOf(_dictionary.Keys.ToArray(), key);
            _dictionary[key] = val;

            Keys = _dictionary.Keys.ToArray();
            Values = _dictionary.Values.ToArray();
            return val;
        }
        /// <summary>
        /// Forcefully inserts an entry into this <see cref="ReflectionDictionary{TClass}"/>.
        /// <para>Give a <see cref="string"/> key, and the method returns where the value was inserted.</para>
        /// </summary>
        /// <param name="key">The <see cref="string"/> key that should be used to access a given value.</param>
        /// /// <param name="value">The value to insert at this given location.</param>
        /// <returns></returns>
        private int ForcefullyInsert(string key, int value) {
            if (_dictionary.ContainsKey(key))
                return Array.IndexOf(Keys, key);
            if (_dictionary.ContainsValue(value))
                return Array.IndexOf(Values, value);

            _dictionary.Add(key, value);

            //if (!refreshCache) // Only refresh the Keys and Values properties if we are requested to.
                //return Array.IndexOf(Keys, key);

            Keys = _dictionary.Keys.ToArray();
            Values = _dictionary.Values.ToArray();
            return Array.IndexOf(Keys, key);
        }
        public bool TryRemove(int id) {
            if (id > _dictionary.Count)
                return false;
            var element = _dictionary.ElementAt(id);
            _dictionary.Remove(element.Key);
            Keys = _dictionary.Keys.ToArray();
            Values = _dictionary.Values.ToArray();
            return true;
        }
        public bool TryRemove(string name) {
            if (!_dictionary.ContainsKey(name))
                return false;
            _dictionary.Remove(name);
            Keys = _dictionary.Keys.ToArray();
            Values = _dictionary.Values.ToArray();
            return true;
        }
    }
}