using DijitalMenu.Application;

namespace DijitalMenu.WebUI.Services;

public sealed class SessionBranchContext(IHttpContextAccessor httpContextAccessor) : IBranchContext
{
    public const string SessionKey = "active_branch_id";

    public int BranchId
    {
        get
        {
            var context = httpContextAccessor.HttpContext;
            if (context is null)
            {
                return 1;
            }

            if (int.TryParse(context.Request.Query["branchId"], out var queryBranchId) && queryBranchId > 0)
            {
                return queryBranchId;
            }

            if (context.Request.HasFormContentType &&
                int.TryParse(context.Request.Form["branchId"], out var formBranchId) &&
                formBranchId > 0)
            {
                return formBranchId;
            }

            return context.Session.GetInt32(SessionKey) ?? 1;
        }
    }
}
