using Orchestra.Api.Extensions;

try
{
    OrchestraHost.Create(args).Run();
}
catch (Exception exception)
{
    // Startup must fail loudly: the message goes to the console and the process exits non-zero.
    Console.Error.WriteLine(exception);
    throw;
}
