using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using DraftService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ArticleService.Tests;

public class DraftServiceTests : IClassFixture<DraftApiFactory>
{
    private readonly HttpClient _client;
    public DraftServiceTests(DraftApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Supports_draft_crud()
    {
        var create = await _client.PostAsJsonAsync("/drafts", new { title = "Draft", body = "First version", author = "publisher" });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var draft = await create.Content.ReadFromJsonAsync<Draft>();
        Assert.NotNull(draft);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/drafts/{draft!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"/drafts/{draft.Id}", new { title = "Updated", body = "Second version", author = "publisher" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/drafts/{draft.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/drafts/{draft.Id}")).StatusCode);
    }
}

public sealed class DraftApiFactory : WebApplicationFactory<DraftServiceProgram>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDraftRepository>();
            services.AddSingleton<IDraftRepository, InMemoryDraftRepository>();
        });
    }
}

public sealed class InMemoryDraftRepository : IDraftRepository
{
    private readonly ConcurrentDictionary<Guid, Draft> _drafts = new();
    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<IReadOnlyList<Draft>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<Draft>>(_drafts.Values.OrderByDescending(draft => draft.CreatedAt).ToList());
    public Task<Draft?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_drafts.GetValueOrDefault(id));
    public Task<Draft> CreateAsync(Draft draft, CancellationToken cancellationToken) { _drafts[draft.Id] = draft; return Task.FromResult(draft); }
    public Task<bool> UpdateAsync(Draft draft, CancellationToken cancellationToken) { _drafts[draft.Id] = draft; return Task.FromResult(true); }
    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult(_drafts.TryRemove(id, out _));
}
