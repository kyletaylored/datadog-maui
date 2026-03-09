using Datadog.Trace;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace DatadogMauiApi.Framework.Controllers
{
    [RoutePrefix("proxy")]
    public class ProxyController : ApiController
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public class ProxyRequestBody
        {
            public string Url { get; set; }
            public string Method { get; set; }
        }

        [HttpPost]
        [Route("")]
        public async Task<IHttpActionResult> ProxyRequest([FromBody] ProxyRequestBody request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Url))
                return BadRequest("URL is required");

            Uri uri;
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out uri) ||
                (uri.Scheme != "http" && uri.Scheme != "https"))
                return BadRequest("Invalid URL. Must be an absolute http/https URL.");

            var method = string.IsNullOrWhiteSpace(request.Method) ? "GET" : request.Method.ToUpperInvariant();
            if (method != "GET" && method != "POST" && method != "HEAD")
                return BadRequest("Unsupported method. Use GET, POST, or HEAD.");

            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                activeScope.Span.ResourceName = method + " /proxy";
                activeScope.Span.SetTag("custom.operation.type", "outbound_proxy");
                activeScope.Span.SetTag("custom.proxy.target_url", request.Url);
                activeScope.Span.SetTag("custom.proxy.method", method);
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var httpRequest = new HttpRequestMessage(new HttpMethod(method), uri);
                var response = await _httpClient.SendAsync(httpRequest);
                stopwatch.Stop();

                var contentType = response.Content.Headers.ContentType?.ToString() ?? "unknown";
                var bodyBytes = await response.Content.ReadAsByteArrayAsync();

                string body;
                if (bodyBytes.Length > 4096)
                    body = Encoding.UTF8.GetString(bodyBytes, 0, 4096) + "\n... [truncated]";
                else
                    body = Encoding.UTF8.GetString(bodyBytes);

                if (activeScope != null)
                {
                    activeScope.Span.SetTag("custom.proxy.status_code", ((int)response.StatusCode).ToString());
                    activeScope.Span.SetTag("custom.proxy.elapsed_ms", stopwatch.ElapsedMilliseconds.ToString());
                    activeScope.Span.SetTag("custom.proxy.content_type", contentType);
                    activeScope.Span.SetTag("custom.proxy.response_size", bodyBytes.Length.ToString());
                }

                return Ok(new
                {
                    statusCode = (int)response.StatusCode,
                    statusText = response.StatusCode.ToString(),
                    contentType,
                    elapsed = stopwatch.ElapsedMilliseconds,
                    url = request.Url,
                    body
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                if (activeScope != null)
                {
                    activeScope.Span.Error = true;
                    activeScope.Span.SetTag("custom.proxy.error", ex.Message);
                }

                return Ok(new
                {
                    statusCode = 0,
                    statusText = "Error",
                    contentType = "text/plain",
                    elapsed = stopwatch.ElapsedMilliseconds,
                    url = request.Url,
                    body = "Error: " + ex.Message
                });
            }
        }
    }
}
