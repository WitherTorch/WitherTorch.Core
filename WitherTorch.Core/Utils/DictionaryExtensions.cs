#if NETSTANDARD2_0
using System.Collections.Generic;

namespace WitherTorch.Core.Utils
{
    internal static class DictionaryExtensions
    {
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> _this, TKey key, TValue value)
        {
            if (_this.ContainsKey(key))
                return false;
            _this.Add(key, value);
            return true;
        }
    }
}
#endif
