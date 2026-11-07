using System.ComponentModel;
using System.IO;
using Microsoft.Win32;
using RoutePlanner.Core;

namespace RoutePlanner.App;

internal static class EpicLauncher
{
    public static GameLaunchResult RequestFortnite()
    {
        try
        {
            // Read only the OS protocol registration; never discover or inspect game processes/files.
            using var protocol = Registry.ClassesRoot.OpenSubKey("com.epicgames.launcher", writable:false);
            if (protocol?.GetValue("URL Protocol") is null)
                return new(false,"Epic Games Launcher nicht registriert. Epic installieren/öffnen oder die Startoption ausschalten.");
            ExternalLinks.Open(EpicLaunchContract.FortniteUri);
            return new(true,"Epic-Start angefragt · tatsächlicher Spielstart nicht überprüft");
        }
        catch (Exception ex) when (ex is Win32Exception or IOException or InvalidOperationException or UnauthorizedAccessException or System.Security.SecurityException)
        { return new(false,"Epic-Start nicht angefragt: "+ex.Message); }
    }
}
