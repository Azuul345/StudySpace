using Microsoft.Extensions.Configuration;

namespace StudySpace.Api.Tests;

[Trait("Req", "REQ-SEC-001")]
public class ListenAddressTests
{
    private static IConfiguration Config(string? urls) =>
        new ConfigurationBuilder().AddInMemoryCollection(urls is null ? [] : new Dictionary<string, string?> { ["Urls"] = urls }).Build();

    [Fact]
    public void Default_IsLoopbackOnly()
    {
        var urls = ListenAddress.Resolve(Config(null));

        Assert.Equal(["http://127.0.0.1:5080"], urls);
    }

    [Theory]
    [InlineData("http://127.0.0.1:6000")]
    [InlineData("http://localhost:6000")]
    [InlineData("http://[::1]:6000")]
    [InlineData("http://127.0.0.1:6000;http://localhost:6001")]
    public void LoopbackAddresses_AreAccepted(string urls)
    {
        Assert.NotEmpty(ListenAddress.Resolve(Config(urls)));
    }

    [Theory]
    [InlineData("http://0.0.0.0:5080")]
    [InlineData("http://*:5080")]
    [InlineData("http://+:5080")]
    [InlineData("http://192.168.1.10:5080")]
    [InlineData("http://example.com:5080")]
    [InlineData("http://127.0.0.1:5080;http://0.0.0.0:5081")]
    [InlineData("not a url")]
    public void NonLoopbackAddresses_AreRefused(string urls)
    {
        Assert.Throws<InvalidOperationException>(() => ListenAddress.Resolve(Config(urls)));
    }
}
