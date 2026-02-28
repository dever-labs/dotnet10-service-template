using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Sinks.Syslog;
using ServiceTemplate.Application.Common.Logging;

namespace ServiceTemplate.Infrastructure.Logging;

/// <summary>
/// Writes audit events as RFC 5424 syslog messages via UDP (production).
///
/// Each message carries STRUCTURED-DATA with machine-readable audit fields:
///   [audit@32473 AuditAction="..." EntityType="..." EntityId="..." Outcome="..." ErrorCode="..."]
///
/// The MSGID field (RFC 5424 §6.2.7) is set to the audit action name for log-server filtering.
///
/// When syslog is disabled (development default), falls back to standard ILogger which routes
/// through the OpenTelemetry pipeline — no extra dependencies required.
/// </summary>
public sealed class SyslogAuditLogger : IAuditLogger, IDisposable
{
    // Private Enterprise Number used as the SD-ID namespace per RFC 5424 §7.2.2.
    // 32473 is the IANA-designated "example" PEN; replace with your own for production.
    private const string SdId = "audit@32473";

    private readonly ILogger<SyslogAuditLogger>? _fallback;
    private readonly Serilog.ILogger? _syslog;

    public SyslogAuditLogger(IOptions<AuditLogOptions> options, ILogger<SyslogAuditLogger> fallback)
    {
        var opts = options.Value;

        if (opts.UseSyslog)
        {
            _syslog = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.UdpSyslog(
                    host: opts.SyslogHost,
                    port: opts.SyslogPort,
                    appName: opts.AppName,
                    facility: Facility.Auth,        // facility 4 — security/authorization messages
                    format: SyslogFormat.RFC5424,
                    messageIdPropertyName: "AuditAction")
                .CreateLogger();
        }
        else
        {
            _fallback = fallback;
        }
    }

    public void Log(AuditEvent auditEvent)
    {
        if (_syslog is not null)
        {
            // RFC 5424: context properties → STRUCTURED-DATA block
            // [audit@32473 AuditAction="..." EntityType="..." EntityId="..." Outcome="..." ErrorCode="..."]
            _syslog
                .ForContext("AuditAction", auditEvent.Action)
                .ForContext("EntityType", auditEvent.EntityType)
                .ForContext("EntityId", auditEvent.EntityId ?? "-")
                .ForContext("Outcome", auditEvent.Outcome.ToString())
                .ForContext("ErrorCode", auditEvent.ErrorCode ?? "-")
                .Information(
                    "[{SdId}] {AuditAction} {Outcome} | {EntityType} {EntityId}",
                    SdId,
                    auditEvent.Action,
                    auditEvent.Outcome,
                    auditEvent.EntityType,
                    auditEvent.EntityId ?? "-");
        }
        else
        {
            _fallback!.LogInformation(
                "[audit] {AuditAction} {Outcome} | {EntityType} {EntityId} | ErrorCode={ErrorCode}",
                auditEvent.Action,
                auditEvent.Outcome,
                auditEvent.EntityType,
                auditEvent.EntityId ?? "-",
                auditEvent.ErrorCode ?? "-");
        }
    }

    public void Dispose() => (_syslog as IDisposable)?.Dispose();
}
