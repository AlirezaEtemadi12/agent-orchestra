using System.Linq.Expressions;
using System.Reflection;
using Gridify;
using Orchestra.Api.Common.Errors;

namespace Orchestra.Api.Common.Paging;

/// <summary>Plan 0010 D3: the row DTO is the allow-list. A property is mapped only when the entity has a
/// public scalar property with the same name and the same underlying type; a differently named key goes
/// through <see cref="Build"/>'s extra action.</summary>
public static class TableMapper<TEntity, TRow>
{
    private static readonly IGridifyMapper<TEntity> Mapper = Build();

    public static (QueryablePaging<TEntity>? Paging, Error? Error) Query(
        IQueryable<TEntity> source,
        GridifyQuery request,
        string defaultOrderBy,
        string tiebreak,
        IGridifyMapper<TEntity>? mapper = null)
    {
        var effective = mapper ?? Mapper;

        TableQueryDefaults.Apply(request, defaultOrderBy, tiebreak);

        if (!request.IsValid(effective))
        {
            return (null, Error.TableQueryInvalid);
        }

        return (source.GridifyQueryable(request, effective), null);
    }

    public static IGridifyMapper<TEntity> Build(Action<GridifyMapper<TEntity>>? extra = null)
    {
        var mapper = new GridifyMapper<TEntity>(autoGenerateMappings: false);
        var entityProperties = typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .ToDictionary(property => property.Name, StringComparer.Ordinal);

        foreach (var row in typeof(TRow).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (row.GetIndexParameters().Length > 0 || row.IsDefined(typeof(NotFilterableAttribute), inherit: true))
            {
                continue;
            }

            if (!entityProperties.TryGetValue(row.Name, out var entity))
            {
                continue;
            }

            if (!IsScalar(entity.PropertyType) || !IsScalar(row.PropertyType))
            {
                continue;
            }

            if (Underlying(entity.PropertyType) != Underlying(row.PropertyType))
            {
                continue;
            }

            var parameter = Expression.Parameter(typeof(TEntity), "row");
            var body = Expression.Convert(Expression.Property(parameter, entity), typeof(object));
            var expression = Expression.Lambda<Func<TEntity, object?>>(body, parameter);

            mapper.AddMap(Key(row.Name), expression);
        }

        extra?.Invoke(mapper);

        return mapper;
    }

    private static string Key(string name)
    {
        return name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
    }

    private static Type Underlying(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    private static bool IsScalar(Type type)
    {
        var underlying = Underlying(type);

        return underlying == typeof(string)
            || underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(decimal)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateOnly)
            || underlying == typeof(TimeOnly)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Guid);
    }
}
