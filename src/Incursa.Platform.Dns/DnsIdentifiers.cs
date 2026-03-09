#pragma warning disable MA0048
namespace Incursa.Platform.Dns;

public readonly record struct DnsZoneId
{
    public DnsZoneId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public readonly record struct DnsRecordId
{
    public DnsRecordId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}

public readonly record struct DnsExternalLinkId
{
    public DnsExternalLinkId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
#pragma warning restore MA0048
