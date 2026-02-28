namespace ServiceTemplate.Application.Common.Logging;

public enum AuditOutcome { Success, Failure }

/// <summary>
/// Represents a single audit trail event. Logged as an RFC 5424 syslog message
/// with structured-data elements for machine-readable correlation.
/// </summary>
public sealed record AuditEvent(
    string Action,
    string EntityType,
    string? EntityId,
    AuditOutcome Outcome,
    string? ErrorCode = null);
