using Microsoft.DevProxy.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using Titanium.Web.Proxy.EventArguments;

namespace GitHub.Copilot.Listener.Plugin;

public class GitHubCopilotListenerPluginConfiguration
{
  public string? LogPath { get; set; }
}

public class GitHubCopilotListenerPlugin : BaseProxyPlugin
{
  public override string Name => nameof(GitHubCopilotListenerPlugin);

  private readonly GitHubCopilotListenerPluginConfiguration _configuration = new();

  /// <summary>
  /// Initializes a new instance of the <see cref="GitHubCopilotListenerPlugin"/> class.
  /// </summary>
  public GitHubCopilotListenerPlugin()
  {
    Console.WriteLine("GitHub Copilot Listener Plugin Registered");
    Console.WriteLine($"Running in: {Environment.CurrentDirectory}");
  }

  /// <summary>
  /// Registers the plugin with the specified plugin events, context, URLs to watch, and configuration section.
  /// </summary>
  /// <param name="pluginEvents">The plugin events to register with.</param>
  /// <param name="context">The proxy context.</param>
  /// <param name="urlsToWatch">The set of URLs to watch.</param>
  /// <param name="configSection">The configuration section.</param>
  public override void Register(IPluginEvents pluginEvents,
                                IProxyContext context,
                                ISet<UrlToWatch> urlsToWatch,
                                IConfigurationSection? configSection = null)
  {
    base.Register(pluginEvents, context, urlsToWatch, configSection);

    configSection?.Bind(_configuration);

    pluginEvents.BeforeRequest += BeforeRequest;
    pluginEvents.AfterResponse += AfterResponse;
  }

  /// <summary>
  /// Handles the BeforeRequest event.
  /// </summary>
  /// <param name="sender">The object that raised the event.</param>
  /// <param name="e">The event arguments.</param>
  /// <returns>A task representing the asynchronous operation.</returns>
  private Task BeforeRequest(object sender, ProxyRequestArgs e)
  {
    if (_urlsToWatch is null ||
      !e.HasRequestUrlMatch(_urlsToWatch))
    {
      Console.WriteLine("No URL to watch.");
      return Task.CompletedTask;
    }

    try
    {
      var body = e.Session.HttpClient.Request.BodyString;

      var uri = e.Session.HttpClient.Request.RequestUri;
      if (uri?.AbsolutePath.Contains("/chat/completions") == true)
      {
        LogRequest(e.Session, body, "chat", "request");
      }
      else if (uri?.AbsolutePath.Contains("/copilot-codex/completions") == true)
      {
        LogRequest(e.Session, body, "inline", "request");
      }
      else
      {
        LogRequest(e.Session, body, "unknown", "request");
      }
    }
    catch (Exception ex)
    {
      LogRequest(e.Session, JsonSerializer.Serialize(ex.Message), "error", "request");
      return Task.CompletedTask;
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Represents an asynchronous operation that can return a value.
  /// </summary>
  /// <returns>A task that represents the asynchronous operation.</returns>
  private Task AfterResponse(object? sender, ProxyResponseArgs e)
  {
    if (_urlsToWatch is null ||
      !e.HasRequestUrlMatch(_urlsToWatch))
    {
      Console.WriteLine("No URL to watch.");
      return Task.CompletedTask;
    }

    try
    {
      var body = e.Session.HttpClient.Response.BodyString;

      var uri = e.Session.HttpClient.Request.RequestUri;
      if (uri?.AbsolutePath.Contains("/chat/completions") == true)
      {
        LogRequest(e.Session, body, "chat", "response");
      }
      else if (uri?.AbsolutePath.Contains("/copilot-codex/completions") == true)
      {
        LogRequest(e.Session, body, "inline", "response");
      }
      else
      {
        LogRequest(e.Session, body, "unknown", "response");
      }
    }
    catch (Exception ex)
    {
      LogRequest(e.Session, JsonSerializer.Serialize(ex.Message), "error", "response");
      return Task.CompletedTask;
    }

    return Task.CompletedTask;
  }

  /// <summary>
  /// Logs the request information to a file.
  /// </summary>
  /// <param name="session">The session event arguments.</param>
  /// <param name="schemaString">The schema string to be logged.</param>
  /// <param name="request">The request string to be logged.</param>
  /// <param name="type">The type of the request.</param>
  private void LogRequest(SessionEventArgs session, string schemaString, string request, string type)
  {
    try
    {
      var filePath = Path.Combine(
        Environment.CurrentDirectory,
        _configuration?.LogPath ?? "",
        type,
        $"copilot-{request}-{session.ClientConnectionId}.json");
      var file = new FileInfo(filePath);
      file.Directory?.Create();
      File.WriteAllText(file.FullName, schemaString);
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.Message);
    }
  }
}