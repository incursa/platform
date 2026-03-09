#pragma warning disable MA0048
namespace Incursa.Platform.CustomDomains;

public readonly record struct CustomDomainId
{
    public CustomDomainId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public readonly record struct CustomDomainExternalLinkId
{
    public CustomDomainExternalLinkId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
#pragma warning restore MA0048
