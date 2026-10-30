using System.Reflection;
using InvoiceApp.Api.Auth;
using InvoiceApp.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace InvoiceApp.Tests;

public class AuthAuthorizationTests
{
    [Fact]
    public void ApprovalsController_Uses_Manager_And_Director_Auth_Roles()
    {
        var attribute = Assert.Single(typeof(ApprovalsController).GetCustomAttributes(typeof(RequireRoleAttribute), inherit: true));
        var requireRole = Assert.IsType<RequireRoleAttribute>(attribute);

        var field = typeof(RequireRoleAttribute).GetField("_roles", BindingFlags.Instance | BindingFlags.NonPublic);
        var roles = Assert.IsType<string[]>(field!.GetValue(requireRole));

        Assert.Equal(new[] { "Manager", "Director" }, roles);
    }

    [Theory]
    [InlineData("Manager")]
    [InlineData("Director")]
    public void ApprovalEndpointAuthorization_Allows_Manager_And_Director_Sessions(string role)
    {
        var context = CreateActionContext(role);
        var attribute = GetApprovalsControllerRequireRoleAttribute();

        attribute.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Theory]
    [InlineData("Approver")]
    [InlineData("Admin")]
    [InlineData(null)]
    public void ApprovalEndpointAuthorization_Denies_Legacy_And_Missing_Roles(string? role)
    {
        var context = CreateActionContext(role);
        var attribute = GetApprovalsControllerRequireRoleAttribute();

        attribute.OnActionExecuting(context);

        var result = Assert.IsType<StatusCodeResult>(context.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequests_AreRejected_BySessionMiddleware()
    {
        var middleware = new SessionAuthMiddleware(_ => Task.CompletedTask, new SessionStore());
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/approvals/INV-1/approve";

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public void DemoUsersConfiguration_Includes_Manager_And_Director_Accounts()
    {
        var config = LoadApiConfiguration();
        var users = config.GetSection("Users").Get<List<DemoUser>>() ?? [];

        Assert.Contains(users, user => user.Username == "manager.demo" && user.Role == "Manager");
        Assert.Contains(users, user => user.Username == "director.demo" && user.Role == "Director");
    }

    [Theory]
    [InlineData("manager.demo", "ChangeMe123!", "Manager")]
    [InlineData("director.demo", "ChangeMe123!", "Director")]
    public void AuthController_Login_Returns_Configured_Role_For_Demo_Users(string username, string password, string expectedRole)
    {
        var controller = new AuthController(
            Options.Create(LoadDemoUsersOptions()),
            new SessionStore());

        var actionResult = controller.Login(new AuthController.LoginRequest(username, password));

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var loginResponse = Assert.IsType<AuthController.LoginResponse>(okResult.Value);
        Assert.Equal(expectedRole, loginResponse.Role);
    }

    private static RequireRoleAttribute GetApprovalsControllerRequireRoleAttribute()
    {
        var attribute = Assert.Single(typeof(ApprovalsController).GetCustomAttributes(typeof(RequireRoleAttribute), inherit: true));
        return Assert.IsType<RequireRoleAttribute>(attribute);
    }

    private static DemoUsersOptions LoadDemoUsersOptions()
    {
        var config = LoadApiConfiguration();
        var users = config.GetSection("Users").Get<List<DemoUser>>() ?? [];
        return new DemoUsersOptions { Users = users };
    }

    private static IConfiguration LoadApiConfiguration()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        Assert.True(File.Exists(configPath), $"Expected API appsettings.json to be copied to test output at '{configPath}'.");

        return new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: false)
            .Build();
    }

    private static ActionExecutingContext CreateActionContext(string? role)
    {
        var httpContext = new DefaultHttpContext();
        if (role is not null)
        {
            httpContext.Items[HttpContextExtensions.SessionKey] = new UserSession("session-user", role);
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());
    }
}
