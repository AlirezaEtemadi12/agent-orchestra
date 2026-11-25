namespace Orchestra.Api.Common.Paging;

/// <summary>Plan 0010 D3: a row DTO property with this attribute is never mapped, even when the entity
/// has a public scalar property with the same name.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class NotFilterableAttribute : Attribute;
