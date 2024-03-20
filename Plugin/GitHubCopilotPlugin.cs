using Microsoft.DevProxy.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace GitHub.Copilot.Listener.Plugin;

public class GitHubCopilotListenerPlugin : BaseProxyPlugin
{
  public override string Name => nameof(GitHubCopilotListenerPlugin);

  public override void Register(IPluginEvents pluginEvents,
                                IProxyContext context,
                                ISet<UrlToWatch> urlsToWatch,
                                IConfigurationSection? configSection = null)
  {
    base.Register(pluginEvents, context, urlsToWatch, configSection);

    pluginEvents.BeforeRequest += BeforeRequest;
  }

  private Task BeforeRequest(object sender, ProxyRequestArgs e)
  {
    if (_urlsToWatch is null ||
      !e.HasRequestUrlMatch(_urlsToWatch) ||
      !e.Session.HttpClient.Request.Method.Equals("PATCH", StringComparison.CurrentCultureIgnoreCase))
    {
      return Task.CompletedTask;
    }

    try
    {
      var schemaString = e.Session.HttpClient.Request.BodyString;
      Console.WriteLine(schemaString);
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.Message);
      return Task.CompletedTask;
    }

    return Task.CompletedTask;
  }
}