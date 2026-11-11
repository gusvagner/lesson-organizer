using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TaskManagement.Api.Http;

public sealed class EnumNameModelBinderProvider : IModelBinderProvider
{
    private static readonly BindingSource[] NameValueSources = [BindingSource.Query, BindingSource.Path, BindingSource.ModelBinding];

    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        var enumType = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;
        return enumType.IsEnum && BindsFromNameValueSource(context.BindingInfo.BindingSource)
            ? new EnumNameModelBinder(enumType)
            : null;
    }

    private static bool BindsFromNameValueSource(BindingSource? source) =>
        source is null || NameValueSources.Contains(source);
}
