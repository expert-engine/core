using System;
using System.Threading.Tasks;
using System.Net.Http;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

using Codidact.WebUI.Middlewares;
using Codidact.Infrastructure.Common;

namespace Codidact.WebUI.IntegrationTests.Middlewares
{
    public class UrlSchemaMiddlewareTests
    {
        private async Task DoTest(string original, string expected, Action<CodidactOptions> configure)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<CodidactOptions>(configure);
                })
                .Configure(app =>
                {
                    app.UseMiddleware<UrlSchemaMiddleware>();

                    app.Use(async (context, next) =>
                    {
                        Assert.Equal(expected, context.Request.GetEncodedUrl());
                        await next();
                    });
                });

            var server = new TestServer(builder);

            HttpClient client = server.CreateClient();
            await client.GetAsync(original);
        }

        [Theory]
        [InlineData("https://codidact.com/community/foo/meta", "https://codidact.com/community/foo/meta")]
        [InlineData("https://bar.codidact.com/meta", "https://codidact.com/community/bar/meta")]
        [InlineData("https://codidact.com/admin", "https://codidact.com/admin")]
        [InlineData("https://foo.bar.codidact.com", "https://codidact.com/error/404")]
        [InlineData("https://127.0.0.1", "https://codidact.com/")]
        [InlineData("https://foo.codidact.com:8080", "https://codidact.com/community/foo/")]
        public async Task Test_WithSubdomainSchema(string original, string expected)
        {
            await DoTest(original, expected, options =>
            {
                options.UseSubdomainSchema = true;
                options.Hostname = "codidact.com";
                options.CommunitySeparator = "community";
            });
        }

        [Theory]
        [InlineData("https://bar.example.com/meta", "https://example.com/meta")]
        [InlineData("https://127.0.0.1", "https://example.com/")]
        [InlineData("https://example.com:8080", "https://example.com/")]
        public async Task Test_WithoutSubdomainSchema(string original, string expected)
        {
            await DoTest(original, expected, options =>
            {
                options.UseSubdomainSchema = false;
                options.Hostname = "example.com";
                options.CommunitySeparator = "community";
            });
        }

        [Theory]
        [InlineData("https://another.co.uk/comunidad/foo", "https://another.co.uk/community/foo")]
        [InlineData("https://another.co.uk/community/bar/meta", "https://another.co.uk/community/bar/meta")]
        private async Task Test_CommunitySeperator(string original, string expected)
        {
            await DoTest(original, expected, options =>
            {
                options.UseSubdomainSchema = false;
                options.Hostname = "another.co.uk";
                options.CommunitySeparator = "comunidad";
            });
        }
    }
}
