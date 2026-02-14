using Datadog.Trace;
using DatadogMauiApi.Framework.Models;
using DatadogMauiApi.Framework.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Web.Http;

namespace DatadogMauiApi.Framework.Controllers
{
    [RoutePrefix("")]
    public class DataController : ApiController
    {
        private static readonly ConcurrentBag<DataSubmission> DataStore = new ConcurrentBag<DataSubmission>();
        private readonly SessionManager _sessionManager;

        public DataController()
        {
            _sessionManager = SessionManager.Instance;
        }

        [HttpPost]
        [Route("data")]
        public IHttpActionResult SubmitData([FromBody] DataSubmission submission)
        {
            if (submission == null)
            {
                return BadRequest("Submission data is required");
            }

            // Get the active span created by automatic instrumentation
            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                activeScope.Span.ResourceName = "POST /data";
                activeScope.Span.SetTag("custom.operation.type", "data_submission");
                activeScope.Span.SetTag("custom.correlation.id", submission.CorrelationId);
                activeScope.Span.SetTag("custom.data.session_name", submission.SessionName);
                activeScope.Span.SetTag("custom.data.numeric_value", submission.NumericValue.ToString());

                // Check if user is authenticated
                var authHeader = Request.Headers.Authorization;
                if (authHeader != null && !string.IsNullOrEmpty(authHeader.Parameter))
                {
                    var validation = _sessionManager.ValidateSession(authHeader.Parameter);
                    if (validation.Item1 && validation.Item2 != null)
                    {
                        activeScope.Span.SetTag("custom.user.id", validation.Item2);
                        activeScope.Span.SetTag("custom.authenticated", "true");
                    }
                    else
                    {
                        activeScope.Span.SetTag("custom.authenticated", "false");
                    }
                }
                else
                {
                    activeScope.Span.SetTag("custom.authenticated", "false");
                }

                // Store the submission
                DataStore.Add(submission);

                activeScope.Span.SetTag("custom.data.total_submissions", DataStore.Count.ToString());
                activeScope.Span.SetTag("custom.submission.success", "true");

                // Return response with trace IDs
                return Ok(new
                {
                    isSuccessful = true,
                    message = "Data received successfully",
                    correlationId = submission.CorrelationId,
                    timestamp = DateTime.UtcNow,
                    traceId = activeScope.Span.TraceId.ToString(),
                    spanId = activeScope.Span.SpanId.ToString()
                });
            }

            // Fallback if no active span
            return Ok(new
            {
                isSuccessful = true,
                message = "Data received successfully",
                correlationId = submission.CorrelationId,
                timestamp = DateTime.UtcNow,
                traceId = "0",
                spanId = "0"
            });
        }

        [HttpGet]
        [Route("data")]
        public IHttpActionResult GetAllData()
        {
            // Get the active span created by automatic instrumentation
            var activeScope = Tracer.Instance.ActiveScope;
            if (activeScope != null)
            {
                activeScope.Span.ResourceName = "GET /data";
                activeScope.Span.SetTag("custom.operation.type", "data_retrieval");
                activeScope.Span.SetTag("custom.data.count", DataStore.Count.ToString());

                // Check if user is authenticated
                var authHeader = Request.Headers.Authorization;
                if (authHeader != null && !string.IsNullOrEmpty(authHeader.Parameter))
                {
                    var validation = _sessionManager.ValidateSession(authHeader.Parameter);
                    if (validation.Item1 && validation.Item2 != null)
                    {
                        activeScope.Span.SetTag("custom.user.id", validation.Item2);
                        activeScope.Span.SetTag("custom.authenticated", "true");
                    }
                    else
                    {
                        activeScope.Span.SetTag("custom.authenticated", "false");
                    }
                }
                else
                {
                    activeScope.Span.SetTag("custom.authenticated", "false");
                }
            }

            return Ok(DataStore.ToList());
        }
    }
}
