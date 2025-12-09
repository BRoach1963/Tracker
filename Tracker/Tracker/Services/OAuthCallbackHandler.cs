using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tracker.Logging;

namespace Tracker.Services
{
    /// <summary>
    /// Handles OAuth callback by running a local HTTP server to receive the authorization code.
    /// </summary>
    public class OAuthCallbackHandler
    {
        private static readonly Lazy<OAuthCallbackHandler> _instance = new(() => new OAuthCallbackHandler());
        public static OAuthCallbackHandler Instance => _instance.Value;

        private readonly LoggingManager.Logger _logger = new("OAuthCallback", "OAuthCallback");
        private HttpListener? _listener;
        private CancellationTokenSource? _cancellationTokenSource;
        private TaskCompletionSource<string?>? _codeCompletionSource;

        private OAuthCallbackHandler()
        {
        }

        /// <summary>
        /// Starts listening for OAuth callback on localhost.
        /// </summary>
        /// <param name="port">The port to listen on (default: 8080).</param>
        /// <param name="cancellationToken">Cancellation token to stop listening.</param>
        /// <returns>The authorization code received from the callback, or null if cancelled/errored.</returns>
        public async Task<string?> ListenForCallbackAsync(int port = 8080, CancellationToken cancellationToken = default)
        {
            _codeCompletionSource = new TaskCompletionSource<string?>();
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();
                _logger.Info("OAuth callback listener started on port {0}", port);

                // Start listening in background
                _ = Task.Run(async () => await ListenForRequestAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

                // Wait for code or cancellation
                using (_cancellationTokenSource.Token.Register(() => _codeCompletionSource.TrySetCanceled()))
                {
                    return await _codeCompletionSource.Task;
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error starting OAuth callback listener");
                _codeCompletionSource?.TrySetResult(null);
                return null;
            }
        }

        /// <summary>
        /// Listens for incoming HTTP requests and extracts the authorization code.
        /// </summary>
        private async Task ListenForRequestAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    var request = context.Request;
                    var response = context.Response;

                    // Extract code from query string
                    var code = request.QueryString["code"];
                    var error = request.QueryString["error"];

                    if (!string.IsNullOrEmpty(error))
                    {
                        _logger.Error("OAuth error received: {0}", error);
                        SendResponse(response, "Authentication was cancelled or failed. Please try again.", false);
                        _codeCompletionSource?.TrySetResult(null);
                        break;
                    }

                    if (!string.IsNullOrEmpty(code))
                    {
                        _logger.Info("Authorization code received");
                        SendResponse(response, "Authentication successful! You can close this window and return to Tracker.", true);
                        _codeCompletionSource?.TrySetResult(code);
                        break;
                    }
                    else
                    {
                        SendResponse(response, "No authorization code received. Please try again.", false);
                    }
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995) // Operation aborted
                {
                    // Listener was stopped, this is expected
                    break;
                }
                catch (Exception ex)
                {
                    _logger.Exception(ex, "Error handling OAuth callback request");
                    if (_listener?.IsListening == true)
                    {
                        try
                        {
                            var context = await _listener.GetContextAsync();
                            SendResponse(context.Response, "An error occurred. Please try again.", false);
                        }
                        catch { }
                    }
                    _codeCompletionSource?.TrySetResult(null);
                    break;
                }
            }
        }

        /// <summary>
        /// Sends an HTTP response to the browser.
        /// </summary>
        private void SendResponse(HttpListenerResponse response, string message, bool success)
        {
            try
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "text/html";
                response.ContentEncoding = Encoding.UTF8;

                var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{(success ? "Success" : "Error")}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        }}
        .container {{
            background: white;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 10px 40px rgba(0,0,0,0.2);
            text-align: center;
            max-width: 400px;
        }}
        .success {{
            color: #10b981;
            font-size: 48px;
            margin-bottom: 20px;
        }}
        .error {{
            color: #ef4444;
            font-size: 48px;
            margin-bottom: 20px;
        }}
        h1 {{
            margin: 0 0 10px 0;
            color: #1f2937;
        }}
        p {{
            color: #6b7280;
            margin: 0;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""{(success ? "success" : "error")}"">{(success ? "✓" : "✗")}</div>
        <h1>{(success ? "Success!" : "Error")}</h1>
        <p>{message}</p>
    </div>
</body>
</html>";

                var buffer = Encoding.UTF8.GetBytes(html);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error sending OAuth callback response");
            }
        }

        /// <summary>
        /// Stops the callback listener.
        /// </summary>
        public void Stop()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _listener?.Stop();
                _listener?.Close();
                _logger.Info("OAuth callback listener stopped");
            }
            catch (Exception ex)
            {
                _logger.Exception(ex, "Error stopping OAuth callback listener");
            }
        }
    }
}

