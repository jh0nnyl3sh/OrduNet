using OrduNet.Web.Data;
using OrduNet.Web.Models.Entities;

namespace OrduNet.Web.Services
{
    public interface IAuditService
    {
        void LogAudit(string userName, string action, string entityName, string? entityId, string details, string? ipAddress);
    }
}
