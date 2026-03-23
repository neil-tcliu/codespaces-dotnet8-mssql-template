using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MssqlCrudBackend.Features.Authors;

namespace MssqlCrudBackend.Tests;

internal sealed class MssqlCrudBackendFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAuthorRepository>();
            services.AddSingleton<IAuthorRepository, InMemoryAuthorRepository>();
        });
    }
}