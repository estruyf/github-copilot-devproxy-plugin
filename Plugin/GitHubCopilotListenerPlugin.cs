using Microsoft.DevProxy.Abstractions;
using Microsoft.Extensions.Configuration;
using Titanium.Web.Proxy.Http;
using System.Text.Json;

namespace GitHub.Copilot.Listener.Plugin;

public class GitHubCopilotListenerPluginConfiguration
{
  public string? LogPath { get; set; }
}

public class GitHubCopilotListenerPlugin : BaseProxyPlugin
{
  public override string Name => nameof(GitHubCopilotListenerPlugin);

  private readonly GitHubCopilotListenerPluginConfiguration _configuration = new();

  public GitHubCopilotListenerPlugin()
  {
    Console.WriteLine("GitHub Copilot Listener Plugin Registered");
    Console.WriteLine($"Runnin in: {Environment.CurrentDirectory}");
  }

  public override void Register(IPluginEvents pluginEvents,
                                IProxyContext context,
                                ISet<UrlToWatch> urlsToWatch,
                                IConfigurationSection? configSection = null)
  {
    base.Register(pluginEvents, context, urlsToWatch, configSection);
    Console.WriteLine($"URLs: {urlsToWatch}");

    configSection?.Bind(_configuration);

    pluginEvents.BeforeRequest += BeforeRequest;
    pluginEvents.AfterResponse += AfterResponse;
  }

  private Task BeforeRequest(object sender, ProxyRequestArgs e)
  {
    var filePath = Path.Combine(Environment.CurrentDirectory, _configuration?.LogPath ?? "", $"copilot-{DateTime.Now.ToUniversalTime().ToString("yyyyMMdd-HHmm")}.txt");

    _logger?.LogRequest(
      [
        filePath
      ],
      MessageType.Normal, new LoggingContext(e.Session)
    );
    File.WriteAllText(filePath, "Nothing to log");

    if (_urlsToWatch is null ||
      !e.HasRequestUrlMatch(_urlsToWatch))
    {
      File.WriteAllText(filePath, "Nothing to log");
      return Task.CompletedTask;
    }

    try
    {
      var schemaString = e.Session.HttpClient.Request.BodyString;
      File.WriteAllText(filePath, schemaString);
    }
    catch (Exception ex)
    {
      File.WriteAllText(filePath, ex.Message);
      return Task.CompletedTask;
    }

    return Task.CompletedTask;
  }

  private Task AfterResponse(object? sender, ProxyResponseArgs e)
  {
    Request request = e.Session.HttpClient.Request;
    if (_urlsToWatch is not null &&
        e.HasRequestUrlMatch(_urlsToWatch))
    {

    }
    Console.WriteLine("AfterResponse");
    return Task.CompletedTask;
  }
}