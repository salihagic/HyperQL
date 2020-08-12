using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;

namespace HyperQL
{
    public static class Extensions
    {
        public static IQueryable<T> WhereBySearchRequest<T>(this IQueryable<T> query, object searchRequest)
        {
            if (searchRequest == null)
                return query;

            searchRequest.GetPropertiesWithSetValue()?.ForEach(x =>
            {
                query = query.Where(BuildCondition(x));
            });

            return query;
        }

        public static IQueryable<T> IncludeBySearchRequest<T>(this IQueryable<T> query, object searchRequest) where T : class
        {
            if (searchRequest == null)
                return query;

            searchRequest.GetPropsForInclude()?.ForEach(x =>
            {
                query = query.Include(x);
            });

            return query;
        }

        public static IQueryable<T> OrderByExtension<T>(this IQueryable<T> query, List<OrderField> orderFields) where T : class
        {
            if (orderFields == null || orderFields.Count == 0)
                return query;

            orderFields?.ForEach(orderField =>
            {
                query = query.OrderBy($"{orderField.Field} {orderField.Direction}");
            });

            return query;
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        private static string BuildCondition(PropInfo prop)
        {
            if(prop.Type == typeof(string))
            {
                switch (prop.CompareType)
                {
                    case CompareType.Equals:
                        return $"{prop.Name} = \"{prop.Value}\"";
                    case CompareType.StartsWith:
                        if (prop.IsCaseSensitive ?? false)
                            return $"{prop.Name}.StartsWith(\"{prop.Value}\")";
                        else
                            return $"{prop.Name}.ToLower().StartsWith({((string)prop.Value).ToLower()})";
                    case CompareType.Contains:
                        if (prop.IsCaseSensitive ?? false)
                            return $"{prop.Name}.Contains(\"{prop.Value}\")";
                        else
                            return $"{prop.Name}.ToLower().Contains(\"{((string)prop.Value).ToLower()}\")";
                    case CompareType.EndsWith:
                        if (prop.IsCaseSensitive ?? false)
                            return $"{prop.Name}.EndsWith(\"{prop.Value}\")";
                        else
                            return $"{prop.Name}.ToLower().EndsWith(\"{((string)prop.Value).ToLower()}\")";
                    default:
                        if (prop.IsCaseSensitive ?? false)
                            return $"{prop.Name}.StartsWith(\"{prop.Value}\")";
                        else
                            return $"{prop.Name}.ToLower().StartsWith(\"{((string)prop.Value).ToLower()}\")";
                }
            }
            else
            {
                switch (prop.CompareType)
                {
                    case CompareType.Equals:
                        return $"{prop.Name} = {prop.Value}";
                    case CompareType.GreaterThan:
                        return $"{prop.Name} > {prop.Value}";
                    case CompareType.GreaterThanOrEqual:
                        return $"{prop.Name} >= {prop.Value}";
                    case CompareType.LessThan:
                        return $"{prop.Name} < {prop.Value}";
                    case CompareType.LessThanOrEqual:
                        return $"{prop.Name} <= {prop.Value}";
                    default:
                        return $"{prop.Name} = {prop.Value}";
                }
            }
        }

        private static List<string> GetPropsForInclude(this object searchRequest, string parentName = "")
        {
            if (searchRequest == null)
                return null;

            var propsToInclude = new List<string>();

            var props = searchRequest.GetType().GetProperties();

            foreach (var prop in props)
            {
                var propValue = prop.GetValue(searchRequest);

                if (propValue != null)
                {
                    if (prop.ContainsIncludeAttribute() && propValue.Equals(true))
                    {
                        var propName = string.IsNullOrEmpty(parentName) ? prop.GetEntityToInclude() : $"{parentName}.{prop.GetEntityToInclude()}";
                        propsToInclude.Add(propName);
                    }
                    else if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                    {
                        var propName = string.IsNullOrEmpty(parentName) ? prop.Name : $"{parentName}.{prop.Name}";

                        var subClassProps = propValue.GetPropsForInclude(propName);
                        if (subClassProps != null && subClassProps.Count > 0)
                        {
                            propsToInclude.AddRange(subClassProps);
                        }
                    }
                }
            }

            return propsToInclude;
        }

        private static List<PropInfo> GetPropertiesWithSetValue(this object searchRequest, string parentName = "")
        {
            if (searchRequest == null)
                return null;

            var propsToInclude = new List<PropInfo>();
            var props = searchRequest.GetType().GetProperties().ToList();

            foreach (var prop in props)
            {
                var propValue = prop.GetValue(searchRequest);

                if (!prop.ShouldCompareAlways() && ((propValue?.Equals(prop.PropertyType.GetDefaultTypeValue()) ?? true) || prop.ShouldIgnoreForCompare()))
                    continue;

                string propName = parentName != "" ? $"{parentName}.{prop.Name}" : prop.Name;

                if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                {
                    var subClassProps = propValue.GetPropertiesWithSetValue(propName);
                    if (subClassProps != null && subClassProps.Count > 0)
                    {
                        propsToInclude.AddRange(subClassProps);
                    }
                    continue;
                }

                propsToInclude.Add(new PropInfo
                {
                    Name = propName,
                    Type = prop.PropertyType,
                    Value = propValue,
                    CompareType = prop.GetCompareType(),
                    IsCaseSensitive = prop.IsCaseSensitive(),
                });
            }

            return propsToInclude;
        }

        private static bool ContainsIgnoreAttribute(this PropertyInfo prop)
        {
            return prop.GetCustomAttributes(true).Any(x => x.GetType() == typeof(IgnoreAttribute));
        }

        private static bool ContainsIncludeAttribute(this PropertyInfo prop)
        {
            return prop.GetCustomAttributes(true).Any(x => x.GetType() == typeof(IncludeAttribute));
        }

        private static string GetEntityToInclude(this PropertyInfo prop)
        {
            var entityToInclude = ((IncludeAttribute)prop.GetCustomAttributes(true).Where(x => x.GetType() == typeof(IncludeAttribute))?.FirstOrDefault())?.EntityToInclude;
            return !string.IsNullOrEmpty(entityToInclude) ? entityToInclude : prop.Name.Replace("Include", "");
        }

        private static bool ContainsCompareAttribute(this PropertyInfo prop)
        {
            return prop.GetCustomAttributes(true).Any(x => x.GetType() == typeof(CompareAttribute));
        }

        private static CompareType? GetCompareType(this PropertyInfo prop)
        {
            return ((CompareAttribute)prop.GetCustomAttributes(true).Where(x => x.GetType() == typeof(CompareAttribute))?.FirstOrDefault())?.CompareType;
        }

        private static bool? IsCaseSensitive(this PropertyInfo prop)
        {
            return ((CompareAttribute)prop.GetCustomAttributes(true).Where(x => x.GetType() == typeof(CompareAttribute))?.FirstOrDefault())?.IsCaseSensitive;
        }

        private static bool ShouldCompareAlways(this PropertyInfo prop)
        {
            return ((CompareAttribute)prop.GetCustomAttributes(true).Where(x => x.GetType() == typeof(CompareAttribute))?.FirstOrDefault())?.CompareAlways ?? false;
        }

        private static bool ShouldIgnoreForCompare(this PropertyInfo prop)
        {
            return prop.ContainsIgnoreAttribute() || prop.ContainsIncludeAttribute();
        }

        private static object GetDefaultTypeValue(this Type type) => (type.IsValueType && Nullable.GetUnderlyingType(type) == null) ? Activator.CreateInstance(type) : null;
    }
}
