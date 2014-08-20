using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleCritters.Helpers
{
    public interface IReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
    }
    public class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        const string ReadOnlyExceptionMessage = "This dictionary is read-only";

        IDictionary<TKey, TValue> dictionary;

        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = dictionary;
        }

        #region IDictionary<TKey,TValue> Members

        public void Add(TKey key, TValue value)
        {
            throw new NotSupportedException(ReadOnlyExceptionMessage);
        }

        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return dictionary.Keys; }
        }

        public bool Remove(TKey key)
        {
            throw new NotSupportedException(ReadOnlyExceptionMessage);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        public ICollection<TValue> Values
        {
            get { return dictionary.Values; }
        }

        public TValue this[TKey key]
        {
            get
            {
                return dictionary[key];
            }
            set
            {
                throw new NotSupportedException(ReadOnlyExceptionMessage);
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException(ReadOnlyExceptionMessage);
        }

        public void Clear()
        {
            throw new NotSupportedException(ReadOnlyExceptionMessage);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            dictionary.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotSupportedException(ReadOnlyExceptionMessage);
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (dictionary as System.Collections.IEnumerable).GetEnumerator();
        }

        #endregion
    }

    public static class ReadOnlyDictionaryHelper
    {
        public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> @this)
        {
            return new ReadOnlyDictionary<TKey, TValue>(@this);
        }
    }
}
