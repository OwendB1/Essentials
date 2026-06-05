using System.ComponentModel;

namespace Shared.Config;

public interface IPluginConfig : INotifyPropertyChanged
{
    bool Enabled { get; set; }
    bool DetectCodeChanges { get; set; }

    string Motd { get; set; }
    string NewUserMotd { get; set; }
    string MotdUrl { get; set; }
    bool NewUserMotdUrl { get; set; }
    bool StopShipsOnStart { get; set; }
    bool UtilityShowPosition { get; set; }
    bool MarkerShowPosition { get; set; }
    int BackpackLimit { get; set; }
    bool CutGameTags { get; set; }
    bool UseBlockLimitsPlugin { get; set; }
    int ShipFixerCooldownInSeconds { get; set; }
    int ShipFixerConfirmationInSeconds { get; set; }
    bool ShipFixerRemoveBlueprintsFromProjectors { get; set; }
    bool ShipFixerPlayerCommandEnabled { get; set; }
    bool ShipFixerFactionEnabled { get; set; }
    bool ShipFixerEjectPlayers { get; set; }
    bool ShipFixerInParallel { get; set; }
}
