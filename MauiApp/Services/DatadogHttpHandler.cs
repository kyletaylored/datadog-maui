using System.Diagnostics;
using Microsoft.Extensions.Logging;

#if ANDROID
using Datadog.Android.Rum;
using Datadog.Android.Trace;
#endif

namespace DatadogMauiApp.Services;

/// <summary>
/// HTTP message handler that tracks HTTP requests in Datadog RUM and APM
/// </summary>
public class DatadogHttpHandler : DelegatingHandler
{
    private readonly ILogger<DatadogHttpHandler>? _logger;

    public DatadogHttpHandler(ILogger<DatadogHttpHandler>? logger = null) : base(new HttpClientHandler())
    {
        _logger = logger;
    }

    public DatadogHttpHandler(HttpMessageHandler innerHandler, ILogger<DatadogHttpHandler>? logger = null) : base(innerHandler)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
#if ANDROID
        var url = request.RequestUri?.ToString() ?? "unknown";
        var method = request.Method.ToString();
        var resourceKey = $"{method} {url}";

        _logger?.LogInformation("Tracking request: {ResourceKey}", resourceKey);

        // Map HTTP method to RumResourceMethod enum
        var rumMethod = request.Method.Method.ToUpperInvariant() switch
        {
            "GET" => RumResourceMethod.Get!,
            "POST" => RumResourceMethod.Post!,
            "PUT" => RumResourceMethod.Put!,
            "DELETE" => RumResourceMethod.Delete!,
            "HEAD" => RumResourceMethod.Head!,
            "PATCH" => RumResourceMethod.Patch!,
            _ => RumResourceMethod.Get! // Default to GET for unknown methods
        };

        // Start RUM resource tracking with RumResourceMethod enum
        // Include trace context for RUM-to-APM correlation
        var resourceAttributes = new Dictionary<string, Java.Lang.Object>();

        GlobalRumMonitor.Get()?.StartResource(
            resourceKey,
            rumMethod,
            url,
            resourceAttributes
        );

        _logger?.LogInformation("RUM resource tracking started");

        // Log request details for debugging
        _logger?.LogInformation("Request details: Method={Method}, URL={Url}", method, url);
        if (request.Content != null)
        {
            var contentLength = request.Content.Headers?.ContentLength;
            var contentType = request.Content.Headers?.ContentType?.ToString();
            _logger?.LogInformation("Request content: ContentLength={ContentLength} bytes, ContentType={ContentType}",
                contentLength ?? 0, contentType ?? "unknown");
        }

        // Generate and inject Datadog trace headers for distributed tracing
        // Datadog expects trace IDs as 64-bit unsigned integers
        ulong traceId = 0;
        ulong spanId = 0;
        try
        {
            traceId = GenerateTraceId();
            spanId = GenerateTraceId();

            // Add Datadog distributed tracing headers
            request.Headers.TryAddWithoutValidation("x-datadog-trace-id", traceId.ToString());
            request.Headers.TryAddWithoutValidation("x-datadog-parent-id", spanId.ToString());
            request.Headers.TryAddWithoutValidation("x-datadog-sampling-priority", "1");
            request.Headers.TryAddWithoutValidation("x-datadog-origin", "rum");

            // Also add W3C TraceContext header for better .NET compatibility
            // W3C traceparent format: version-traceId-spanId-flags
            // Convert 64-bit IDs to 32-character hex (128-bit with padding)
            var traceIdHex = traceId.ToString("x16").PadLeft(32, '0');
            var spanIdHex = spanId.ToString("x16");
            var traceparent = $"00-{traceIdHex}-{spanIdHex}-01";
            request.Headers.TryAddWithoutValidation("traceparent", traceparent);

            _logger?.LogInformation("Injected trace headers: TraceId={TraceId}, ParentId={ParentId}, Traceparent={Traceparent}",
                traceId, spanId, traceparent);

            // Log all headers being sent
            if (_logger?.IsEnabled(LogLevel.Debug) == true)
            {
                foreach (var header in request.Headers)
                {
                    _logger.LogDebug("Request header: {HeaderKey}={HeaderValue}",
                        header.Key, string.Join(", ", header.Value));
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to inject trace headers");
        }

        var stopwatch = Stopwatch.StartNew();
        HttpResponseMessage? response = null;
        Exception? exception = null;

        try
        {
            // Execute the request
            response = await base.SendAsync(request, cancellationToken);
            return response;
        }
        catch (Exception ex)
        {
            exception = ex;
            _logger?.LogError(ex, "Request failed: {ErrorMessage}", ex.Message);
            throw;
        }
        finally
        {
            stopwatch.Stop();

            try
            {
                // Report to RUM
                if (response != null)
                {
                    var statusCode = (int)response.StatusCode;

                    // Get actual content length from response
                    long contentLength = 0;
                    if (response.Content != null)
                    {
                        // Try to get declared content length first
                        contentLength = response.Content.Headers?.ContentLength ?? 0;

                        // If no content-length header, try to read the actual size
                        if (contentLength == 0)
                        {
                            try
                            {
                                // This will load the content but preserve it for later use
                                await response.Content.LoadIntoBufferAsync();
                                contentLength = response.Content.Headers?.ContentLength ?? 0;
                            }
                            catch
                            {
                                // If we can't load the buffer, leave it as 0
                            }
                        }
                    }

                    _logger?.LogInformation("Response received: Status={StatusCode}, ContentLength={ContentLength} bytes, Duration={Duration}ms",
                        statusCode, contentLength, stopwatch.ElapsedMilliseconds);

                    // Use boxed primitives instead of deprecated wrapper constructors
                    var statusCodeObj = Java.Lang.Integer.ValueOf(statusCode);
                    var contentLengthObj = Java.Lang.Long.ValueOf(contentLength);

                    // Add trace context to RUM resource for correlation
                    var stopAttributes = new Dictionary<string, Java.Lang.Object>();
                    if (traceId > 0)
                    {
                        stopAttributes["_dd.trace_id"] = new Java.Lang.String(traceId.ToString());
                        stopAttributes["_dd.span_id"] = new Java.Lang.String(spanId.ToString());
                        _logger?.LogInformation("Adding trace context to RUM resource: TraceId={TraceId}, SpanId={SpanId}",
                            traceId, spanId);
                    }

                    GlobalRumMonitor.Get()?.StopResource(
                        resourceKey,
                        statusCodeObj,
                        contentLengthObj,
                        RumResourceKind.Native!, // Using Native for mobile HTTP client calls
                        stopAttributes
                    );

                    _logger?.LogInformation("RUM resource stopped with Native kind");
                }
                else if (exception != null)
                {
                    _logger?.LogWarning("Reporting RUM error for failed request");

                    // Create Java throwable from .NET exception
                    var javaException = new Java.Lang.Exception(exception.Message);

                    // Report as error
                    GlobalRumMonitor.Get()?.AddError(
                        exception.Message,
                        RumErrorSource.Network!,
                        javaException,
                        new Dictionary<string, Java.Lang.Object>()
                    );

                    // Still stop the resource
                    var zeroLong = Java.Lang.Long.ValueOf(0L);
                    GlobalRumMonitor.Get()?.StopResource(
                        resourceKey,
                        null, // no status code for failed requests
                        zeroLong,
                        RumResourceKind.Native!,
                        new Dictionary<string, Java.Lang.Object>()
                    );
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error reporting to Datadog");
            }
        }
#else
        return await base.SendAsync(request, cancellationToken);
#endif
    }

#if ANDROID
    /// <summary>
    /// Generate a random 64-bit unsigned integer for Datadog trace/span IDs
    /// </summary>
    private static ulong GenerateTraceId()
    {
        var bytes = new byte[8];
        Random.Shared.NextBytes(bytes);
        return BitConverter.ToUInt64(bytes, 0);
    }
#endif
}
