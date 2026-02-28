namespace ServiceTemplate.Infrastructure.Logging;

public sealed class AuditLogOptions
{
    public const string SectionName = "AuditLog";

    /// <summary>Send audit events to a syslog server over UDP (RFC 5424).</summary>
    public bool UseSyslog { get; init; } = false;

    public string SyslogHost { get; init; } = "localhost";

    public int SyslogPort { get; init; } = 514;

    /// <summary>APP-NAME field in RFC 5424 header.</summary>
    public string AppName { get; init; } = "service-template";
}
