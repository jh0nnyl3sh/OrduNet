using System.Security.Claims;
using OrduNet.Web.Models.Entities;

namespace OrduNet.Web.Services
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsSuperAdmin(this ClaimsPrincipal user)
        {
            return user.IsInRole("SuperAdmin");
        }

        public static bool HasModulePermission(this ClaimsPrincipal user, string moduleKey)
        {
            if (user.IsSuperAdmin())
            {
                return true;
            }

            return user.HasClaim("ModulePermission", moduleKey);
        }
    }
}
