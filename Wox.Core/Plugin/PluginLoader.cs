﻿using System.Reflection;
using System.Text.Json;
using Wox.Core.Plugin.Host;
using Wox.Core.Plugin.System;
using Wox.Core.Utils;

namespace Wox.Core.Plugin;

public static class PluginLoader
{
    private static List<PluginHostBase> PluginHosts { get; } = new()
    {
        new DotnetHost(),
        new NodejsHost(),
        new PythonHost()
    };

    public static async Task<List<PluginInstance>> LoadPlugins()
    {
        Logger.Debug("Start to load plugins");
        var pluginInstances = new List<PluginInstance>();


        // load system plugin first
        var systemPluginInstances = LoadSystemPlugins();
        if (systemPluginInstances != null) pluginInstances.AddRange(systemPluginInstances);

        // load other plugins
        foreach (var pluginRuntime in PluginRuntime.All)
            try
            {
                var instances = await LoadPluginsByRuntime(pluginRuntime);
                pluginInstances.AddRange(instances);
            }
            catch (Exception e)
            {
                Logger.Error($"[{pluginRuntime} host] load host plugin failed", e);
            }

        return pluginInstances;
    }

    private static List<PluginInstance>? LoadSystemPlugins()
    {
        var pluginInstances = new List<PluginInstance>();

        try
        {
            var systemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(o => typeof(ISystemPlugin).IsAssignableFrom(o) && o.IsClass);
            foreach (var type in systemTypes)
            {
                var rawIPlugin = Activator.CreateInstance(type) as ISystemPlugin;
                if (rawIPlugin == null) return null;
                var pluginInstance = new PluginInstance
                {
                    Metadata = rawIPlugin.GetMetadata(),
                    Plugin = rawIPlugin,
                    API = new PluginPublicAPI(rawIPlugin.GetMetadata()),
                    PluginDirectory = "",
                    Host = new DotnetHost(),
                    IsSystemPlugin = true
                };
                Logger.Debug($"Start to load system plugin: {pluginInstance.Metadata.Name}");
                pluginInstances.Add(pluginInstance);
            }

            return pluginInstances;
        }
        catch (Exception e)
        {
            Logger.Error("Couldn't load system plugins", e);
#if DEBUG
            throw;
#else
            return null;
#endif
        }
    }

    private static async Task<List<PluginInstance>> LoadPluginsByRuntime(string pluginRuntime)
    {
        if (pluginRuntime.ToUpper() == PluginRuntime.Python.ToUpper())
            throw new Exception("Python plugin runtime is not supported yet");

        var pluginInstances = new List<PluginInstance>();

        List<(PluginMetadata, string)> pluginMetas = new();
        var pluginDirectories = DataLocation.PluginDirectories.SelectMany(Directory.GetDirectories);
        foreach (var pluginDirectory in pluginDirectories)
        {
            var configPath = Path.Combine(pluginDirectory, "plugin.json");
            if (!File.Exists(configPath))
            {
                Logger.Error($"Didn't find plugin config file {configPath}");
                continue;
            }

            var metadata = ParsePluginMetadataFromDirectory(pluginDirectory);
            if (metadata == null) continue;

            if (metadata.Runtime.ToUpper() != pluginRuntime.ToUpper()) continue;

            pluginMetas.Add((metadata, pluginDirectory));
        }

        Logger.Debug($"[{pluginRuntime} host] start to load plugins");
        var pluginHost = PluginHosts.FirstOrDefault(o => o.PluginRuntime.ToUpper() == pluginRuntime.ToUpper());
        if (pluginHost == null) throw new Exception($"[{pluginRuntime}] there is no host for {pluginRuntime}");

        Logger.Debug($"[{pluginHost.PluginRuntime} host] starting plugin host");
        await pluginHost.Start();
        foreach (var (metadata, pluginDirectory) in pluginMetas)
        {
            Logger.Debug($"[{metadata.Runtime} host] start to load plugin: {metadata.Name}");
            var plugin = await pluginHost.LoadPlugin(metadata, pluginDirectory);
            if (plugin == null) continue;

            var pluginInstance = new PluginInstance
            {
                Metadata = metadata,
                Plugin = plugin,
                API = new PluginPublicAPI(metadata),
                PluginDirectory = pluginDirectory,
                Host = pluginHost
            };
            pluginInstances.Add(pluginInstance);
        }

        return pluginInstances;
    }

    /// <summary>
    ///     Parse plugin metadata in giving directory
    /// </summary>
    private static PluginMetadata? ParsePluginMetadataFromDirectory(string pluginDirectory)
    {
        Logger.Debug($"Start to parse plugin metadata in {pluginDirectory}");
        var configPath = Path.Combine(pluginDirectory, "plugin.json");
        if (!File.Exists(configPath))
        {
            Logger.Error($"Didn't find plugin config file {configPath}");
            return null;
        }

        try
        {
            var pluginJson = File.ReadAllText(configPath);
            return ParsePluginMetadata(pluginJson);
        }
        catch (Exception e)
        {
            Logger.Error($"Read plugin.json failed {configPath}", e);
            return null;
        }
    }

    public static PluginMetadata? ParsePluginMetadata(string pluginJson)
    {
        PluginMetadata? metadata;
        try
        {
            metadata = JsonSerializer.Deserialize<PluginMetadata>(pluginJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (metadata == null)
            {
                Logger.Error($"Invalid json for plugin config {pluginJson}");
                return null;
            }
        }
        catch (Exception e)
        {
            Logger.Error($"Deserialize plugin config failed {pluginJson}", e);
            return null;
        }

        if (metadata.TriggerKeywords.Count == 0)
        {
            Logger.Error($"Plugin {metadata.Name} didn't register any trigger keyword");
            return null;
        }

        if (!PluginRuntime.IsAllowed(metadata.Runtime))
        {
            Logger.Error($"Invalid runtime {metadata.Runtime} for plugin config");
            return null;
        }

        return metadata;
    }
}