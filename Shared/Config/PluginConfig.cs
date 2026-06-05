#if !TORCH

using PluginSdk.Config;

namespace Shared.Config;

[Tab("general", caption: "General")]
[Tab("motd", caption: "MOTD")]
[Tab("cleanup", caption: "Cleanup")]
[Tab("pcu", caption: "PCU Tools")]
[Tab("shipfixer", caption: "Ship Fixer")]
[Section("core", "general", "Core")]
[Section("grid", "general", "Grid Utilities")]
[Section("motd-main", "motd", "Messages")]
[Section("motd-url", "motd", "URL")]
[Section("cleanup-bags", "cleanup", "Backpacks")]
[Section("pcu-core", "pcu", "Limits")]
[Section("shipfixer-core", "shipfixer", "Core")]
public class PluginConfig : PluginSdk.Config.PluginConfig, IPluginConfig
{
    [BoolOption("Enable Essentials plugin runtime.", Parent = "core")]
    public bool Enabled { get; set => SetField(ref field, value); } = true;

    [BoolOption("Verify patched game methods before applying Harmony patches.", Parent = "core")]
    public bool DetectCodeChanges { get; set => SetField(ref field, value); } = true;

    [BoolOption("Cut mods and block limits from matchmaking server info.", Parent = "core")]
    public bool CutGameTags { get; set => SetField(ref field, value); }

    [StringOption(description: "Message displayed to players when they connect.", Parent = "motd-main")]
    public string Motd { get; set => SetField(ref field, value); } = "";

    [StringOption(description: "Message displayed only to first-time players.", Parent = "motd-main")]
    public string NewUserMotd { get; set => SetField(ref field, value); } = "";

    [StringOption(description: "URL opened in the Steam overlay on player connect.", Parent = "motd-url")]
    public string MotdUrl { get; set => SetField(ref field, value); } = "";

    [BoolOption("Open MOTD URL only for first-time players.", Parent = "motd-url")]
    public bool NewUserMotdUrl { get; set => SetField(ref field, value); }

    [BoolOption("Stop all entities when the server starts.", Parent = "core")]
    public bool StopShipsOnStart { get; set => SetField(ref field, value); }

    [BoolOption("Show positions in owned-grid list output.", Parent = "grid")]
    public bool UtilityShowPosition { get; set => SetField(ref field, value); }

    [BoolOption("Show GPS markers for owned-grid list output.", Parent = "grid")]
    public bool MarkerShowPosition { get; set => SetField(ref field, value); }

    [IntOption(-1, int.MaxValue, "Maximum empty backpacks per player. Set -1 for no limit.", Parent = "cleanup-bags")]
    public int BackpackLimit { get; set => SetField(ref field, value); } = 1;

    [BoolOption("Use BlockLimits Plugin when validating PCU transfer limits. Ignored when BlockLimits is detected and enabled.", Parent = "pcu-core")]
    public bool UseBlockLimitsPlugin { get; set => SetField(ref field, value); }

    [IntOption(0, int.MaxValue, "Player fixship cooldown in seconds.", Parent = "shipfixer-core")]
    public int ShipFixerCooldownInSeconds { get; set => SetField(ref field, value); } = 5 * 60;

    [IntOption(0, int.MaxValue, "Fixship confirmation window in seconds.", Parent = "shipfixer-core")]
    public int ShipFixerConfirmationInSeconds { get; set => SetField(ref field, value); } = 30;

    [BoolOption("Remove blueprints from projectors during fixship.", Parent = "shipfixer-core")]
    public bool ShipFixerRemoveBlueprintsFromProjectors { get; set => SetField(ref field, value); }

    [BoolOption("Allow player fixship command.", Parent = "shipfixer-core")]
    public bool ShipFixerPlayerCommandEnabled { get; set => SetField(ref field, value); } = true;

    [BoolOption("Allow faction ownership to satisfy player fixship ownership checks.", Parent = "shipfixer-core")]
    public bool ShipFixerFactionEnabled { get; set => SetField(ref field, value); }

    [BoolOption("Allow fixship to eject seated players.", Parent = "shipfixer-core")]
    public bool ShipFixerEjectPlayers { get; set => SetField(ref field, value); }

    [BoolOption("Process fixship grids in parallel.", Parent = "shipfixer-core")]
    public bool ShipFixerInParallel { get; set => SetField(ref field, value); } = true;
}

#endif
