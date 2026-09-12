using System.Net;
using System.Net.Http;
using PwM.Mobile.Services;
using PwMLib;

namespace PwM.Tests.HIPB;

public class BreachResponseTests
{
    private const string Password = "regression test password";
    private static string Suffix => Sha1Converter.Hash(Password)[5..];

    [Theory]
    [InlineData("0", false)]
    [InlineData("12", true)]
    public async Task Padded_entries_with_zero_occurrences_are_not_breaches(string count, bool expected)
    {
        using var client = new HttpClient(new Handler(request =>
        {
            Assert.Equal("https://api.pwnedpasswords.com/range/" + Sha1Converter.Hash(Password)[..5],
                request.RequestUri!.AbsoluteUri);
            Assert.Equal("true", Assert.Single(request.Headers.GetValues("Add-Padding")));
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{Suffix}:{count}\r\n") };
        }));

        Assert.Equal(expected, await new HibpService(client).IsBreachedAsync(Password));
    }

    [Theory]
    [InlineData(503, "unavailable")]
    [InlineData(200, "<html>proxy error</html>")]
    [InlineData(200, "")]
    [InlineData(302, "redirect")]
    public async Task Failed_checks_are_unknown_not_clean(int status, string body)
    {
        using var client = new HttpClient(new Handler(_ => new HttpResponseMessage((HttpStatusCode)status)
        {
            Content = new StringContent(body)
        }));

        Assert.Null(await new HibpService(client).IsBreachedAsync(Password));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("not-a-count")]
    [InlineData("9999999999999999999999999")]
    public void Invalid_counts_are_rejected(string count) =>
        Assert.Throws<InvalidDataException>(() => HIBP.GetBreachCount($"{Suffix}:{count}", Suffix));

    [Fact]
    public async Task Later_checks_retry_instead_of_retaining_password_hashes_or_failed_results()
    {
        int requests = 0;
        using var client = new HttpClient(new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent($"{Suffix}:{requests++}")
        }));
        var service = new HibpService(client);

        Assert.False(await service.IsBreachedAsync(Password));
        Assert.True(await service.IsBreachedAsync(Password));
        Assert.Equal(2, requests);
    }

    [Fact]
    public async Task Cancellation_does_not_start_a_request()
    {
        using var client = new HttpClient(new Handler(_ => throw new InvalidOperationException("Unexpected request")));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new HibpService(client).IsBreachedAsync(Password, cancellation.Token));
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) =>
            Task.FromResult(respond(request));
    }
}
