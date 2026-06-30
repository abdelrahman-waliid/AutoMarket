using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CarMarketplace.API.Filters;

/// <summary>
/// Action filter that runs FluentValidation for [FromBody] parameters and returns 400 with validation errors when invalid.
/// Validators are defined in the Application layer; uses reflection to avoid referencing FluentValidation in API.
/// </summary>
public sealed class FluentValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Get open IValidator<> type from a validator in Application (FluentValidation)
        var closedValidatorType = typeof(CarMarketplace.Application.Validators.RegisterRequestDTOValidator)
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition().Name == "IValidator`1");
        if (closedValidatorType == null)
        {
            await next();
            return;
        }
        var validatorOpenType = closedValidatorType.GetGenericTypeDefinition();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument == null)
                continue;

            var type = argument.GetType();
            var validatorType = validatorOpenType.MakeGenericType(type);
            var validator = _serviceProvider.GetService(validatorType);
            if (validator == null)
                continue;

            var validateMethod = validatorType.GetMethod("ValidateAsync", new[] { type, typeof(CancellationToken) });
            if (validateMethod == null)
                continue;

            var task = (Task)validateMethod.Invoke(validator, new[] { argument, CancellationToken.None })!;
            await task.ConfigureAwait(false);

            var result = task.GetType().GetProperty("Result")?.GetValue(task);
            if (result == null)
                continue;

            var isValid = (bool)result.GetType().GetProperty("IsValid")!.GetValue(result)!;
            if (isValid)
                continue;

            var errorsProperty = result.GetType().GetProperty("Errors")!.GetValue(result);
            var errors = (System.Collections.IEnumerable)errorsProperty!;
            var modelState = new ModelStateDictionary();
            foreach (var error in errors)
            {
                var prop = error?.GetType().GetProperty("PropertyName")?.GetValue(error)?.ToString() ?? "";
                var msg = error?.GetType().GetProperty("ErrorMessage")?.GetValue(error)?.ToString() ?? "Validation failed.";
                modelState.AddModelError(prop, msg);
            }

            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(modelState)
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest
            });
            return;
        }

        await next();
    }
}
