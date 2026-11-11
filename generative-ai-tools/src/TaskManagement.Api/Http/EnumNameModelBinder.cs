using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TaskManagement.Api.Http;

/// <summary>
/// Binds enum values from the query or route by name only (case-insensitive), so numbers such as <c>?status=2</c>
/// are rejected like they are in JSON bodies.
/// </summary>
public sealed class EnumNameModelBinder(Type enumType) : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var value = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (value == ValueProviderResult.None || string.IsNullOrEmpty(value.FirstValue))
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, value);
        var name = EnumNames.Match(enumType, value.FirstValue);
        if (name is null)
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName, $"The value must be one of {EnumNames.Allowed(enumType)}; got '{value.FirstValue}'.");
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(Enum.Parse(enumType, name));
        return Task.CompletedTask;
    }
}
