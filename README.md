# AntiCheat Extension for 7 Days to Die

A server-side anti-cheat mod for 7 Days to Die dedicated servers. It watches what players send to the server and flags things that should not be possible: god mode, teleporting, speed and fly hacks, noclip, spawned admin items, oversized stacks, impossible damage and reach, and level jumps.

Everything runs on the server. Players do not need to install anything, and a cheater cannot switch the checks off from their own machine.

- Game version: V3.3 (b18)
- Mod version: 1.0.0

## Installation

1. Download `AntiCheatExtension-<version>.zip` from the [Releases](https://github.com/CTRLKirk/7DTD-Anti-Cheat/releases) page.
2. Extract it into the server's `Mods` folder so that you end up with `Mods/AntiCheatExtension/ModInfo.xml`.
3. Restart the server. The console log should show:

   ```
   [AntiCheat] Loaded v1.0.0 for game V 3.3.0 (b18).
   ```

The mod only needs to be on the server. It works with EasyAntiCheat enabled.

## What it checks

| Detection     | What triggers it                                                        | Default action |
|---------------|-------------------------------------------------------------------------|----------------|
| `GodMode`     | A player's client reports god mode while player damage is on            | Kick           |
| `Teleport`    | A player moves further than `maxDistance` between two samples (0.5s)    | Kick           |
| `IllegalItem` | Dev or test items (creative menu only) in inventory or dropped          | Kick           |
| `Damage`      | A single hit above `maxDamage`. The hit is also cancelled                | Kick           |
| `Speed`       | Average horizontal speed above `maxSpeed` over `windowSeconds`          | Log            |
| `Fly`         | In the air with no blocks nearby for longer than `maxAirSeconds`        | Log            |
| `NoClip`      | Standing inside solid blocks for `minChecks` samples in a row           | Log            |
| `StackSize`   | A stack bigger than the item's maximum stack size                       | Log            |
| `Reach`       | A melee hit from further away than `maxDistance`                        | Log            |
| `Progression` | Player level goes up by more than `maxLevelGain` in one update          | Log            |

The kick defaults are on checks that are very unlikely to fire for a legitimate player. Movement checks default to logging because lag, vehicles and some game mechanics can produce odd movement. Watch the logs for a while before turning them up to `Kick`.

Admins are skipped. Anyone with a permission level at or below `ExemptPermissionLevel` (default 1) is never checked. Players coming out of a spawn, respawn or server teleport get a short grace period. The illegal item check is off when the server has the creative menu enabled.

## What happens on a detection

1. A line is written to the server console and to `Mods/AntiCheatExtension/Logs/detections-<date>.log`.
2. Online admins (permission level at or below `NotifyPermissionLevel`) get a private chat message.
3. If `DiscordWebhookUrl` is set, the detection is posted to that Discord channel.
4. The configured action runs: `Log` stops here, `Kick` kicks the player, `Ban` bans them.

The same detection for the same player is only reported once per `AlertCooldownSeconds`.

## Configuration

Settings live in `Mods/AntiCheatExtension/Config/settings.xml`. Each check has an `action` (`Off`, `Log`, `Kick` or `Ban`) and its own thresholds:

```xml
<Detection name="Speed" action="Log" maxSpeed="20" windowSeconds="3" />
```

General settings:

| Setting                 | Default | Meaning                                                     |
|-------------------------|---------|-------------------------------------------------------------|
| `ExemptPermissionLevel` | 1       | Players at or below this permission level are not checked   |
| `NotifyPermissionLevel` | 1       | Players at or below this level get in-game alerts           |
| `BanDurationHours`      | 0       | Length of bans from the `Ban` action. 0 means permanent      |
| `AlertCooldownSeconds`  | 15      | Minimum time between repeat alerts for one player and check |
| `DiscordWebhookUrl`     | empty   | Discord webhook for alerts. Leave empty to turn off         |

After editing the file, run `ac reload` in the server console. No restart needed.

## Console commands

Run these from the server console, telnet or the in-game console as an admin.

| Command             | What it does                                     |
|---------------------|--------------------------------------------------|
| `ac status`         | Shows every check, its action and total flags     |
| `ac recent [count]` | Shows the latest detections (default 10)         |
| `ac reload`         | Reloads `settings.xml`                           |

`anticheat` works as a longer alias for `ac`.

## Building from source

You need the .NET SDK (6 or newer) and a copy of the game or the dedicated server for the game DLLs.

```
dotnet build -c Release -p:GamePath="C:\Program Files (x86)\Steam\steamapps\common\7 Days to Die Dedicated Server"
```

The ready-to-install mod folder is written to `dist/AntiCheatExtension`. Add `-p:DeployToGame=true` to copy it straight into `GamePath/Mods` as well. An existing `settings.xml` there is left alone.

## License

MIT. See [LICENSE](LICENSE).
