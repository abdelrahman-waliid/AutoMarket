using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CarMarketplace.API.Swagger;

/// <summary>
/// Adds Bearer auth requirements and security responses to authorized Swagger operations.
/// </summary>
public class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (HasAllowAnonymous(context))
            return;

        var authorizeAttributes = GetAuthorizeAttributes(context);
        if (authorizeAttributes.Count == 0)
            return;

        operation.Responses ??= new OpenApiResponses();

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });

        var requiresRoles = authorizeAttributes.Any(a => !string.IsNullOrWhiteSpace(a.Roles));
        if (requiresRoles)
        {
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
        }

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference(
                    referenceId: "Bearer",
                    hostDocument: context.Document,
                    externalResource: null!),
                new List<string>()
            }
        });
    }

    private static bool HasAllowAnonymous(OperationFilterContext context)
    {
        var methodInfo = context.MethodInfo;
        var controllerType = methodInfo.DeclaringType;

        return methodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
               || (controllerType?.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any() ?? false);
    }

    private static List<AuthorizeAttribute> GetAuthorizeAttributes(OperationFilterContext context)
    {
        var methodInfo = context.MethodInfo;
        var controllerType = methodInfo.DeclaringType;

        var methodAttributes = methodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>();
        var controllerAttributes = controllerType?.GetCustomAttributes(true).OfType<AuthorizeAttribute>()
                                   ?? Enumerable.Empty<AuthorizeAttribute>();

        return methodAttributes.Concat(controllerAttributes).ToList();
    }
}
