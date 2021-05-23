using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Modern.Vice.PdbMonitor.Core.Common
{
    public static class AttributeRetriever
    {
        //private readonly static ImmutableDictionary<Type, ResourceManager> resourceManagerPropertiesCache = ImmutableDictionary<Type, ResourceManager>.Empty;

        public static TAttrib? GetEnumAttribute<TEnum, TAttrib>(TEnum value)
            where TEnum : Enum
            where TAttrib : Attribute
        {
            string? name = Enum.GetName(typeof(TEnum), value);
            if (name is not null)
            {
                FieldInfo? fieldInfo = typeof(TEnum).GetRuntimeField(name);
                if (fieldInfo is not null)
                {
                    var attributes = fieldInfo.GetCustomAttributes(typeof(TAttrib), false);
                    return (TAttrib?)attributes?.FirstOrDefault();
                }
            }
            return null;
        }

        public static TAttrib? GetEnumAttribute<TAttrib>(Type enumType, object value)
            where TAttrib : Attribute
        {
            string? name = Enum.GetName(enumType, value);
            if (name is not null)
            {
                FieldInfo? fieldInfo = enumType.GetRuntimeField(name);
                if (fieldInfo is not null)
                {
                    var attributes = fieldInfo.GetCustomAttributes(typeof(TAttrib), false);
                    return (TAttrib?)attributes?.FirstOrDefault();
                }
            }
            return null;
        }

        //static ResourceManager GetResourceManager(Type type)
        //{
        //    Contract.Requires(type != null);

        //    ResourceManager resourceManager;
        //    if (!resourceManagerPropertiesCache.TryGetValue(type, out resourceManager))
        //    {
        //        PropertyInfo resourceManagerProperty = type.GetTypeInfo().DeclaredProperties
        //            .Where(pi => (pi.GetMethod?.IsStatic ?? false)
        //                && string.Equals(pi.Name, "ResourceManager", StringComparison.OrdinalIgnoreCase)
        //                && pi.PropertyType == typeof(ResourceManager)).SingleOrDefault();
        //        if (resourceManagerProperty != null)
        //        {
        //            resourceManager = resourceManagerProperty.GetValue(null) as ResourceManager;
        //            resourceManagerPropertiesCache.Add(type, resourceManager);
        //        }
        //        else
        //        {
        //            resourceManager = null;
        //        }
        //    }
        //    return resourceManager;
        //}
        public static string GetDisplayName<TEnum>(TEnum value)
            where TEnum : Enum
        {
            DisplayAttribute? descriptionAttribute = GetEnumAttribute<TEnum, DisplayAttribute>(value);
            if (descriptionAttribute is not null)
            {
                if (descriptionAttribute.ResourceType != null)
                {
                    throw new NotImplementedException();
                    //return GetResourceManager(descriptionAttribute.ResourceType)?.GetString(descriptionAttribute.Name);
                }
                else
                {
                    return descriptionAttribute.Name ?? string.Empty;
                }
            }
            else
                return string.Empty;
        }

        public static ImmutableArray<string> GetAllDisplayNames<TEnum>()
            where TEnum : Enum
        {
            List<string> result = new List<string>();
            foreach (TEnum value in Enum.GetValues(typeof(TEnum)))
            {
                result.Add(GetDisplayName(value));
            }
            return result.ToImmutableArray();
        }

        public static T? GetSingleCustomAttribute<T>(MemberInfo mi, bool inherit)
            where T : Attribute
        {
            T[] attributes = (T[])mi.GetCustomAttributes(typeof(T), inherit);
            if (attributes?.Length == 1)
                return attributes[0];
            else
                return null;
        }
    }
}
