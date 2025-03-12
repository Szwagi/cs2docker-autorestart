using System;
using System.IO;
using System.Threading;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace AutoRestart;

public class AutoRestartPlugin : BasePlugin
{
    public override string ModuleName => "Auto Restart";

    public override string ModuleVersion => "1.0.0";

    private string _buildVersion = null!;

    private Timer _timer = null!;
    
    private bool _restartNeeded = false;

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnMapEnd>(OnMapEnd);

        _buildVersion = Environment.GetEnvironmentVariable("build_ver")?.Trim()
            ?? throw new Exception("Environment variable 'build_ver' was not found, this plugin is meant to be used with cs2docker!");

        _timer = new Timer(OnTimerCallback, null, 0, 10000);
    }

    public override void Unload(bool hotReload)
    {
        _timer.Dispose();
    }
    
    private bool IsServerOutOfDate()
    {
        var latestBuildVersion = File.ReadAllText("/watchdog/cs2/latest.txt").Trim();
        return _buildVersion != latestBuildVersion;
    }

    private void OnTimerCallback(object? state)
    {
        if (IsServerOutOfDate())
        {
            Server.NextWorldUpdate(() =>
            {
                var numPlayers = Utilities.GetPlayers().Where(x => !x.IsBot).Count();
                if (numPlayers == 0)
                {
                    Server.ExecuteCommand("quit");
                }
                else if (!_restartNeeded)
                {
                    _restartNeeded = true;
                    Server.PrintToChatAll("The server will restart at the next opportunity!");
                }
            });
        }
    }

    public void OnMapEnd()
    {
        if (_restartNeeded || IsServerOutOfDate())
        {
            // Doesn't seem to be exiting cleanly without NextWorldUpdate?
            Server.NextWorldUpdate(() => Server.ExecuteCommand("quit"));
        }
    }
}
