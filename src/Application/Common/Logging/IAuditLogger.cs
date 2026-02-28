namespace ServiceTemplate.Application.Common.Logging;

public interface IAuditLogger
{
    void Log(AuditEvent auditEvent);
}
