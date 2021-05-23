using System;
using System.Collections.Immutable;
using System.Linq;

namespace Modern.Vice.PdbMonitor.Core.Common
{
    public abstract class EnumMapper<T>
    {
        ImmutableDictionary<Type, ImmutableDictionary<Enum, T>> cache;
        public EnumMapper()
        {
            cache = ImmutableDictionary<Type, ImmutableDictionary<Enum, T>>.Empty;
        }

        protected ImmutableDictionary<Enum, T> GetFromCache(Type enumType, Func<ImmutableDictionary<Enum, T>> populate)
        {
            if (!cache.TryGetValue(enumType, out var data))
            {
                data = populate();
                cache.Add(enumType, data);
            }
            return data;
        }

        protected abstract T Map(Type enumType, Enum value);

        public ImmutableDictionary<Enum, T> GetMapEnum(Type enumType)
        {
            return GetFromCache(enumType, () =>
            {
                var query = from v in Enum.GetValues(enumType).Cast<Enum>()
                            select new { Key = v, Value = Map(enumType, v) };
                return query.ToImmutableDictionary(p => p.Key, p => p.Value);
            });
        }
    }
}
