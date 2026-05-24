using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Dme.OrderRouting.Tests.TestSupport;

public class TestWebHostEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Test";

    public string ApplicationName { get; set; } = "Dme.OrderRouting.Tests";

    public string WebRootPath { get; set; } = string.Empty;

    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

    public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}