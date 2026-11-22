using Orchestra.Api.Extensions;

try
{
    var app = WebApplication.CreateBuilder(args)
        .ConfigureServices()
        .Build();

    app.ConfigureApp();
    app.Run();
}
catch (Exception exception)
{
    // Startup must fail loudly: the message goes to the console and the process exits non-zero.
    Console.Error.WriteLine(exception);
    throw;
}
finally
{
    // Nothing to release here yet: the host owns its own lifetime.
}
