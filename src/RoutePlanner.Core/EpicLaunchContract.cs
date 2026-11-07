namespace RoutePlanner.Core;

public sealed record GameLaunchResult(bool Requested, string Message);

public static class EpicLaunchContract
{
    // Epic Games Protocol Activation, section "Launch Game". Public catalog identity, no credential.
    public const string FortniteUri = "com.epicgames.launcher://apps/fn%3A4fe75bbc5a674f4f9b356b5c90567da5%3AFortnite?action=launch&silent=true";
}
