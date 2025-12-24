using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Core.Data.Helper;

public static class PropertyBuilderExtensions
{
    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> propertyBuilder) where T : class, new()
    {
        JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = false };

        // 1. Define how to convert the data for the DB
        ValueConverter<T, string> converter = new(
            v => JsonSerializer.Serialize(v, options),
            v => JsonSerializer.Deserialize<T>(v, options) ?? new T()
        );

        // 2. Define how to compare the data for Change Tracking
        ValueComparer<T> comparer = new ValueComparer<T>(
            (l, r) => JsonSerializer.Serialize(l, options) == JsonSerializer.Serialize(r, options),
            v => v == null ? 0 : JsonSerializer.Serialize(v, options).GetHashCode(),
            v => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(v, options), options)!
        );

        propertyBuilder.HasConversion(converter);
        propertyBuilder.Metadata.SetValueComparer(comparer);
        // propertyBuilder.HasColumnType("TEXT"); // Use "jsonb" for Postgres

        return propertyBuilder;
    }
}