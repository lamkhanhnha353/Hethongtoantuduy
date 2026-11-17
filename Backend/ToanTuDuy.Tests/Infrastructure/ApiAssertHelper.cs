using System.Text.Json;

namespace ToanTuDuy.Tests.Infrastructure;

public static class ApiAssertHelper
{
    public static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg))
            {
                return msg.GetString() ?? body;
            }
            if (doc.RootElement.TryGetProperty("title", out var title))
            {
                return title.GetString() ?? body;
            }
        }
        catch (JsonException)
        {
        }

        return body;
    }

    public static async Task EventuallyAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? interval = null)
    {
        var maxWait = timeout ?? TimeSpan.FromSeconds(10);
        var poll = interval ?? TimeSpan.FromMilliseconds(200);
        var deadline = DateTime.UtcNow + maxWait;

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(poll);
        }

        Assert.True(await condition(), $"Condition was not met within {maxWait.TotalSeconds}s.");
    }
}
