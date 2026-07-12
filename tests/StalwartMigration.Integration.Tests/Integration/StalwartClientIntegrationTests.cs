// <copyright file="StalwartClientIntegrationTests.cs" company="Stalwart Labs">
// Copyright © Stalwart Labs 2024. All rights reserved.
// </copyright>

using System.Net;
using System.Text;
using StalwartMigration.Infrastructure.Stalwart;
using StalwartMigration.Integration.Tests.Fixtures;
using Xunit;

namespace StalwartMigration.Integration.Tests.Integration;

/// <summary>
/// Integration tests for the parts of <see cref="StalwartClient"/> that exercise
/// Stalwart's real REST management API, verified against a live Stalwart v0.16
/// container (see tasks/stalwart-test-server-plan.md and
/// stalwart-test-server-todo.md, Task 4).
///
/// Domain/account/alias CRUD (CreateDomainAsync, CreateAccountAsync,
/// CreateAliasAsync, etc.) are deliberately not covered here: those methods target
/// REST paths (/api/domains, /api/accounts, /api/aliases) that were confirmed via a
/// live container to return 404 on Stalwart v0.16 -- that functionality is exposed
/// through JMAP instead, not this REST API. Testing them here would only document
/// a known-broken code path rather than verify real behavior.
/// </summary>
[Collection(StalwartCollection.Name)]
public class StalwartClientIntegrationTests
{
    private readonly StalwartTestFixture _fixture;

    public StalwartClientIntegrationTests(StalwartTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsAccessToken()
    {
        using var client = _fixture.CreateClient();

        var token = await client.AuthenticateAsync(_fixture.Credentials);

        Assert.False(string.IsNullOrEmpty(token.AccessToken));
        Assert.True(client.IsAuthenticated);
    }

    [Fact]
    public async Task AuthenticateAsync_WithWrongPassword_ThrowsUnauthorized()
    {
        using var client = _fixture.CreateClient();
        var badCredentials = new ApiCredentials
        {
            Username = _fixture.Credentials.Username,
            Password = _fixture.Credentials.Password + "-wrong"
        };

        var ex = await Assert.ThrowsAsync<StalwartClientException>(
            () => client.AuthenticateAsync(badCredentials));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    [Fact]
    public async Task RefreshTokenAsync_AfterAuthenticate_ReturnsNewAccessToken()
    {
        using var client = _fixture.CreateClient();
        await client.AuthenticateAsync(_fixture.Credentials);

        var refreshed = await client.RefreshTokenAsync();

        Assert.False(string.IsNullOrEmpty(refreshed.AccessToken));
    }

    [Fact]
    public async Task GetAccountInfoAsync_WhenAuthenticated_ReturnsEditionAndPermissions()
    {
        var info = await _fixture.StalwartClient.GetAccountInfoAsync();

        Assert.False(string.IsNullOrEmpty(info.Edition));
        Assert.NotNull(info.Permissions);
    }

    [Fact]
    public async Task DiscoverOidcProviderAsync_ReturnsOidcDiscoveryDocument()
    {
        var discovery = await _fixture.StalwartClient.DiscoverOidcProviderAsync(_fixture.Credentials.Username!);

        Assert.True(discovery.ContainsKey("token_endpoint"));
    }

    [Fact]
    public async Task GetSchemaRedirectAsync_ThenGetSchemaAsync_ReturnsSchemaDocument()
    {
        var redirect = await _fixture.StalwartClient.GetSchemaRedirectAsync();
        Assert.StartsWith("/api/schema/", redirect);

        var hash = redirect["/api/schema/".Length..];
        var schemaBytes = await _fixture.StalwartClient.GetSchemaAsync(hash);

        Assert.NotEmpty(schemaBytes);
        var json = Encoding.UTF8.GetString(schemaBytes);
        Assert.Contains("\"objects\"", json);
    }

    [Fact]
    public async Task IssueDeliveryTokenAsync_ReturnsNonEmptyToken()
    {
        var token = await _fixture.StalwartClient.IssueDeliveryTokenAsync();

        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task IssueTracingTokenAsync_OnCommunityEdition_ReturnsEmptyString()
    {
        // The Docker image used for these tests is the community edition, which
        // doesn't support live tracing; the client maps the resulting 404 to "".
        var token = await _fixture.StalwartClient.IssueTracingTokenAsync();

        Assert.Equal(string.Empty, token);
    }

    [Fact]
    public async Task IssueMetricsTokenAsync_OnCommunityEdition_ReturnsEmptyString()
    {
        var token = await _fixture.StalwartClient.IssueMetricsTokenAsync();

        Assert.Equal(string.Empty, token);
    }
}
