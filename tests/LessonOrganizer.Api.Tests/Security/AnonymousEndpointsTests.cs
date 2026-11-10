using System.Reflection;
using LessonOrganizer.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace LessonOrganizer.Api.Tests.Security;

public class AnonymousEndpointsTests
{
    [Fact]
    public void AnonymousActions_AreOnlyCsrfRegisterLogin()
    {
        var anonymous = typeof(AuthController).Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(ControllerBase)))
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => method.GetCustomAttributes<HttpMethodAttribute>().Any())
                .Where(method => IsAnonymous(type, method))
                .Select(method => $"{type.Name}.{method.Name}"))
            .Order();
        Assert.Equal(["AuthController.Csrf", "AuthController.Login", "AuthController.Register"], anonymous);
    }

    private static bool IsAnonymous(Type controller, MethodInfo action) =>
        controller.GetCustomAttribute<AllowAnonymousAttribute>() is not null
        || action.GetCustomAttribute<AllowAnonymousAttribute>() is not null;
}
