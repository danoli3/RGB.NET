﻿using System.Collections.Generic;
using System.Linq;

namespace RGB.NET.Devices.Corsair
{
    internal static class DictionaryExtension
    {
        public static Dictionary<TValue, TKey> SwapKeyValue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary) => dictionary.ToDictionary(x => x.Value, x => x.Key);
    }
}
