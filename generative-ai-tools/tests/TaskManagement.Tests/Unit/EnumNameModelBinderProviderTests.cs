using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskManagement.Api.Http;
using TaskManagement.Api.Tasks;

namespace TaskManagement.Tests.Unit;

public sealed class EnumNameModelBinderProviderTests
{
    private readonly EnumNameModelBinderProvider _provider = new();

    public static TheoryData<string> NameBindingSources => new() { "None", "Query", "Path", "ModelBinding" };

    public static TheoryData<string> OtherBindingSources => new() { "Header", "Body", "Form", "Services" };

    [Theory]
    [MemberData(nameof(NameBindingSources))]
    public void GetBinder_EnumFromQueryRouteOrUnspecifiedSource_ReturnsEnumNameBinder(string bindingSourceId)
    {
        var binder = _provider.GetBinder(new EnumBindingProviderContext(typeof(TaskItemStatus?), SourceById(bindingSourceId)));

        Assert.IsType<EnumNameModelBinder>(binder);
    }

    [Theory]
    [MemberData(nameof(OtherBindingSources))]
    public void GetBinder_EnumFromOtherSource_LeavesItToTheFrameworkBinders(string bindingSourceId)
    {
        var binder = _provider.GetBinder(new EnumBindingProviderContext(typeof(TaskItemStatus), SourceById(bindingSourceId)));

        Assert.Null(binder);
    }

    [Fact]
    public void GetBinder_NonEnumType_ReturnsNull()
    {
        Assert.Null(_provider.GetBinder(new EnumBindingProviderContext(typeof(int), BindingSource.Query)));
    }

    private static BindingSource? SourceById(string id) => id switch
    {
        "None" => null,
        "Query" => BindingSource.Query,
        "Path" => BindingSource.Path,
        "ModelBinding" => BindingSource.ModelBinding,
        "Header" => BindingSource.Header,
        "Body" => BindingSource.Body,
        "Form" => BindingSource.Form,
        "Services" => BindingSource.Services,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Expected a known BindingSource id."),
    };

    private sealed class EnumBindingProviderContext(Type modelType, BindingSource? bindingSource) : ModelBinderProviderContext
    {
        private readonly EmptyModelMetadataProvider _metadataProvider = new();

        public override BindingInfo BindingInfo { get; } = new() { BindingSource = bindingSource };

        public override ModelMetadata Metadata => _metadataProvider.GetMetadataForType(modelType);

        public override IModelMetadataProvider MetadataProvider => _metadataProvider;

        public override IModelBinder CreateBinder(ModelMetadata metadata) =>
            throw new NotSupportedException($"CreateBinder({metadata.ModelType.Name}) is not used by EnumNameModelBinderProvider.");
    }
}
