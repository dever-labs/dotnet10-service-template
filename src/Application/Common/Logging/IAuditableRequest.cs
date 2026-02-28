namespace ServiceTemplate.Application.Common.Logging;

/// <summary>
/// Marker interface for CQRS requests whose execution should produce an RFC 5424 audit log entry.
/// Apply to commands that mutate state (Create, Update, Delete).
/// </summary>
public interface IAuditableRequest
{
    string EntityType { get; }
}
