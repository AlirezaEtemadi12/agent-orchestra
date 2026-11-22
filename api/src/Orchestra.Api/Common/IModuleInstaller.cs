namespace Orchestra.Api.Common;

/// <summary>One per module: Data 10, Projects 20, Import 30, Runs 40, Usage 50.</summary>
public interface IModuleInstaller
{
    int Order { get; }

    void Install(IServiceCollection services, IConfiguration configuration);
}
