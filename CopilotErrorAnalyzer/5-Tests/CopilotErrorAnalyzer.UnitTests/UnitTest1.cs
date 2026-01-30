using System;
using System.Threading;
using System.Threading.Tasks;
using CopilotErrorAnalyzer.Domain.Entities;
using CopilotErrorAnalyzer.Infra.Store;

namespace CopilotErrorAnalyzer.UnitTests;

public class InMemoryErrorStoreTests
{
    [Fact]
    public async Task SaveAsync_WithValidEvent_ShouldReturnIdAndAllowGetById()
    {
        var store = new InMemoryErrorStore();
        var errorEvent = new ErrorEvent
        {
            Source = "application",
            Message = "Sample error",
            Timestamp = DateTimeOffset.UtcNow
        };

        var id = await store.SaveAsync(errorEvent, CancellationToken.None);
        var fetched = await store.GetByIdAsync(id, CancellationToken.None);

        Assert.NotNull(fetched);
        Assert.Equal(id, fetched!.Id);
        Assert.Equal(errorEvent.Message, fetched.Message);
    }
}