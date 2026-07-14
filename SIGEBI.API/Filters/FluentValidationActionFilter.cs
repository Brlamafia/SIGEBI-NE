using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SIGEBI.API.Filters;

public sealed class FluentValidationActionFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values.Where(value => value is not null))
        {
            var argumentType = argument!.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);

            if (serviceProvider.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContextType = typeof(ValidationContext<>).MakeGenericType(argumentType);
            var validationContext = (IValidationContext)Activator.CreateInstance(
                validationContextType,
                argument)!;
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            foreach (var error in result.Errors)
                context.ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(
                new ValidationProblemDetails(context.ModelState));
            return;
        }

        await next();
    }
}
