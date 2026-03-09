namespace Incursa.Integrations.WorkOS.Abstractions.Capabilities;

public sealed class CapabilityNotSupportedException : InvalidOperationException
{
    public CapabilityNotSupportedException()
    {
    }

    public CapabilityNotSupportedException(string? message)
        : base(message)
    {
    }

    public CapabilityNotSupportedException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public CapabilityNotSupportedException(Type capabilityType)
        : base($"Capability '{capabilityType.FullName}' is not supported by this integration.")
    {
        CapabilityType = capabilityType;
    }

    public Type? CapabilityType { get; }
}
