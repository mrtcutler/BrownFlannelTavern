namespace BrownFlannelTavernStore.Tests.TestHelpers;

public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        if (request.Content != null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }
        return _responder(request);
    }
}
