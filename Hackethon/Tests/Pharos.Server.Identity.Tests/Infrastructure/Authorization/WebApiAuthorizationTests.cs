using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Pharos.Server.Identity.Infrastructure.Authorization;

public class WebApiAuthorizationTests
{
    [Fact]
    public void WebApiControllersAndHubsHaveAuthorizeAttribute()
    {
        var checkedEndpoints = 0;
        foreach (var module in Modules.ModuleList)
        {
            var controllers = module.Assembly.DefinedTypes.Where(t => t.GetCustomAttributes<ControllerAttribute>(true).FirstOrDefault() != null).ToList();
            var hubs = module.Assembly.DefinedTypes.Where(t => t.IsSubclassOf(typeof(Hub))).ToList();
            controllers.AddRange(hubs);

            foreach (var controller in controllers)
            {
                var attrs = controller.GetCustomAttributes<AuthorizeAttribute>();
                if (attrs.Any())
                {
                    checkedEndpoints++;
                    continue;
                }
                var methods = controller.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    attrs = method.GetCustomAttributes<AuthorizeAttribute>();
                    if (!attrs.Any())
                    {
                        Assert.Fail($"The method {controller.FullName}.{method.Name}() must have the [Authorize] attribute to avoid unauthorized access.");
                    }
                    checkedEndpoints++;
                }
            }
        }
        checkedEndpoints.Should().BeGreaterThan(0);
    }

    [Fact]
    public void HttpPostAndPutEndpointsRequireAuthorizeWithRoleOrPolicy()
    {
        // The ignore checks override the default behavior of requiring [Authorize] attributes with Permission/Role. Please note that permission check should be done in the commandHandler.
        var ignoreChecks = new List<(string ControllerName, string MethodName)>
        {
            (nameof(Result.Web.Controllers.TestResultsController), nameof(Result.Web.Controllers.TestResultsController.ResetTestResults)),
            (nameof(Web.UsersController), nameof(Web.UsersController.ChangeCurrentUserPassword)),
            (nameof(Request.Web.Controllers.RequestsController), nameof(Request.Web.Controllers.RequestsController.GetNeededSampleVolume)) //This method is tagged as a POST but is really a GET
        };

        var checkedEndpoints = 0;
        foreach (var module in Modules.ModuleList)
        {
            var controllers = module.Assembly.DefinedTypes.Where(t => t.GetCustomAttributes<ControllerAttribute>(true).FirstOrDefault() != null).ToList();

            foreach (var controller in controllers)
            {
                var attrs = controller.GetCustomAttributes<AuthorizeAttribute>();
                if (attrs.Any(a => !string.IsNullOrWhiteSpace(a.Roles) || !string.IsNullOrWhiteSpace(a.Policy)))
                {
                    checkedEndpoints++;
                    continue;
                }
                var methods = controller.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                    .Where(m => m.GetCustomAttributes<HttpPostAttribute>(true).Any()
                                || m.GetCustomAttributes<HttpPutAttribute>(true).Any()
                                || m.GetCustomAttributes<HttpDeleteAttribute>(true).Any()
                                || m.GetCustomAttributes<HttpPatchAttribute>(true).Any());

                foreach (var method in methods)
                {
                    attrs = method.GetCustomAttributes<AuthorizeAttribute>();

                    if (!attrs.Any(a => !string.IsNullOrWhiteSpace(a.Roles) || !string.IsNullOrWhiteSpace(a.Policy)))
                    {
#if DEBUG
                        if (controller.Name == nameof(AuditTrail.Web.Controllers.AuditTrailController) && method.Name == nameof(AuditTrail.Web.Controllers.AuditTrailController.CreateAuditTrailRecord))
                        {
                            continue;
                        }
#endif
                        if (ignoreChecks.Any(x => x.ControllerName == controller.Name && x.MethodName == method.Name))
                        {
                            continue;
                        }
                        Assert.Fail($"The method {controller.FullName}.{method.Name}() must be covered by a permission because it modifies or exports data from the system.");
                    }
                    checkedEndpoints++;
                }
            }
        }
        checkedEndpoints.Should().BeGreaterThan(0);
    }
}