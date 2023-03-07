using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TanksRebirth.Internals.Common.Framework.Collections
{
    [Flags]
    public enum MemberType
    {
        Fields,
        Properties
    }
    /// <summary>Creates a dictionary of member names to </summary>
    /// <typeparam name="TClass"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class ReflectionDictionary<TClass, TValue> where TClass : class, new()
    {
        public int Count => _dictionary.Count;
        public IEnumerable<string> Keys => _dictionary.Keys.AsEnumerable();
        public IEnumerable<TValue> Values => _dictionary.Values.AsEnumerable();
        private Dictionary<string, TValue> _dictionary;

        private MemberType _members;
        /// <summary>
        /// Creates a new <see cref="ReflectionDictionary{TType, TKey, TValue}"/>.<para></para>
        /// Using bitwise flags can be used for using multiple member types.
        /// </summary>
        /// <param name="members">The kind(s) of members to allow for insertion into the dictionary.</param>
        public ReflectionDictionary(MemberType members)
        {
            _dictionary = new();
            _members = members;
            Initialize();
        }
        private void Initialize()
        {
            if (_members.HasFlag(MemberType.Fields))
                foreach (var field in typeof(TClass).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    if (field.FieldType == typeof(TValue))
                        _dictionary.Add(field.Name, (TValue)field.GetValue(null));
            if (_members.HasFlag(MemberType.Properties))
                foreach (var property in typeof(TClass).GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    if (property.PropertyType == typeof(TValue))
                        _dictionary.Add(property.Name, (TValue)property.GetValue(null));
        }
        /// <summary>Attempts to retrieve the value of the given key. If it doesn't exist, returns the default value.</summary>
        /// <param name="key">The key of which to grab the value from.</param>
        /// <returns>The corresponding value.</returns>
        public TValue GetValue(string key)
        {
            if (ContainsKey(key))
                return _dictionary[key];
            return default;
        }
        /// <summary>Attempts to retrieve the key of the given value. If it doesn't exist, returns the default value.</summary>
        /// <param name="value">The value of which to grab the key from.</param>
        /// <returns>The corresponding key.</returns>
        public string GetKey(TValue value)
        {
            if (ContainsValue(value))
                return _dictionary.FirstOrDefault(x => x.Value.Equals(value)).Key;
            return default;
        }

        public bool ContainsKey(string key) => _dictionary.ContainsKey(key);
        public bool ContainsValue(TValue value) => _dictionary.ContainsValue(value);

        public Dictionary<string, TValue> GetContents() => _dictionary;

        public int ForcefullyInsert(string key, TValue value) {
            if (_dictionary.ContainsKey(key))
                return _dictionary.Keys.ToList().IndexOf(key);

            if (_dictionary.ContainsValue(value))
                return _dictionary.Values.ToList().IndexOf(value);

            _dictionary.Add(key, value);
            return _dictionary.Keys.ToList().IndexOf(key);
        }
    }
}
