public async IAsyncEnumerable<string> SendMessageStreamAsync(
    string userId,
    ChatRequestDto request,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var payload = BuildPayload(request, stream: true);
    var json = JsonSerializer.Serialize(payload);
    using var content = new StringContent(json, Encoding.UTF8, "application/json");

    using var resp = await _httpClient.PostAsync(
        "/openai/v1/chat/completions", content, cancellationToken);

    // لو الطلب فشل، رجّع رسالة واضحة وعرفنا الـ body
    if (!resp.IsSuccessStatusCode)
    {
        var errBody = await resp.Content.ReadAsStringAsync(cancellationToken);
        var msg = $"Groq streaming error: {(int)resp.StatusCode} {resp.StatusCode} - {errBody}";
        Console.WriteLine(msg);

        // رجّع الخطأ كأول (وآخر) chunk للـ client عشان تشوفه في Postman
        yield return msg;
        yield break;
    }

    await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
    using var reader = new StreamReader(stream);

    while (!cancellationToken.IsCancellationRequested)
    {
        var line = await reader.ReadLineAsync();
        if (line is null)
            yield break;

        if (string.IsNullOrWhiteSpace(line))
            continue;

        if (line.StartsWith("data: "))
            line = line["data: ".Length..];

        if (line == "[DONE]")
            yield break;

        string? chunk = null;

        try
        {
            using var evtDoc = JsonDocument.Parse(line);
            var root = evtDoc.RootElement;

            var delta = root
                .GetProperty("choices")[0]
                .GetProperty("delta");

            if (delta.TryGetProperty("content", out var contentProp))
            {
                chunk = contentProp.GetString();
            }
        }
        catch
        {
            // لو في سطر مش JSON (مثلاً تعليق) تجاهله
        }

        if (!string.IsNullOrEmpty(chunk))
        {
            yield return chunk;
        }
    }
}