using OrduNet.Web.Data;
using OrduNet.Web.Models.Entities;

namespace OrduNet.Web.Services
{
    public class AuditService : IAuditService
    {
        private readonly OrduNetDbContext _context;
        private readonly ILogger<AuditService> _logger;

        public AuditService(OrduNetDbContext context, ILogger<AuditService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public void LogAudit(string userName, string action, string entityName, string? entityId, string details, string? ipAddress)
        {
            try
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserName = userName,
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    Details = details,
                    IpAddress = ipAddress
                });
                // We assume SaveChanges is called by the controller that triggers this, 
                // OR we can call it here. Usually it's better to let the caller save to maintain transaction, 
                // but since it's fire-and-forget in AdminController, let's just add it to context.
                // It will be saved when caller does SaveChangesAsync.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audit log yazılamadı. Action: {Action}, Entity: {Entity}", action, entityName);
            }
        }
    }
}
