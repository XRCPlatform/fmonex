using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FreeMarketOne.Search
{
    public class DealType : IDictionary<int, string>
    {
        Dictionary<int, string> dictionary = null;
        public DealType()
        {
            dictionary = new Dictionary<int, string>()
            {
                { 0, "Unspecified" },
                { 1, "Pay 0% before 100% after" },
                { 2, "Pay 10% before 90% after" },
                { 3, "Pay 20% before 80% after" },
                { 4, "Pay 30% before 70% after" },
                { 5, "Pay 40% before 60% after" },
                { 6, "Pay 50% before 50% after" },
                { 7, "Pay 60% before 40% after" },
                { 8, "Pay 70% before 30% after" },
                { 9, "Pay 80% before 20% after" },
                { 10, "Pay 90% before 10% after" },
                { 11, "Pay 100% before 0% after" }
            };
        }
        public string this[int key] { get => dictionary[key]; set => throw new NotImplementedException(); }

        public ICollection<int> Keys => dictionary.Keys;

        public ICollection<string> Values => dictionary.Values;

        public int Count => dictionary.Count;

        public bool IsReadOnly => true;

        public void Add(int key, string value)
        {
            throw new NotImplementedException();
        }

        public void Add(KeyValuePair<int, string> item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<int, string> item)
        {
            return dictionary.Contains(item);
        }

        public bool ContainsKey(int key)
        {
            return dictionary.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<int, string>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<int, string>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public bool Remove(int key)
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<int, string> item)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(int key, [MaybeNullWhen(false)] out string value)
        {
            if (dictionary.ContainsKey(key))
            {
                value = dictionary[key];
                return true;
            }

            value = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }
    }
}