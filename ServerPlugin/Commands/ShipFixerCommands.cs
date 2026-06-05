using System;
using System.Collections.Generic;
using PluginSdk.Commands;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using ServerPlugin.ShipFixer;
using VRage.Game.ModAPI;

namespace ServerPlugin.Commands;

public abstract class ShipFixerCommandBase : CommandModule
{
    private static readonly Dictionary<string, DateTime> CommandCooldowns = new Dictionary<string, DateTime>();
    private static readonly Dictionary<string, DateTime> Confirmations = new Dictionary<string, DateTime>();
    private static readonly ShipFixerCore Core = new ShipFixerCore();

    protected void FixShipPlayer(string gridName = null)
    {
        if (!Plugin.Instance.Config.ShipFixerPlayerCommandEnabled)
        {
            Context.Respond("This command was disabled for players use!");
            return;
        }

        if (Context.Caller.IsConsole || Context.Caller.IdentityId == 0)
        {
            Context.Respond("Console has no Grids so cannot use this command. Use !fixshipmod <Gridname> instead!");
            return;
        }

        MyPlayer player = Utilities.GetPlayerByIdentityId(Context.Caller.IdentityId);
        if (player == null)
        {
            Context.Respond("Player not found.");
            return;
        }

        if (!CheckCommandCooldown(PlayerKey(), out int remainingSeconds))
        {
            Plugin.Instance.Log.Info($"Cooldown for Player {player.DisplayName} still running! {remainingSeconds} seconds remaining!");
            Context.Respond($"Command is still on cooldown for {remainingSeconds} seconds.");
            return;
        }

        long playerId = Context.Caller.IdentityId;
        MyCharacter character = null;
        if (string.IsNullOrWhiteSpace(gridName))
        {
            if (player.Character is not MyCharacter myCharacter)
            {
                Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
                return;
            }

            character = myCharacter;
            gridName = "nogrid";
        }

        if (!CheckConfirmation(PlayerKey(), playerId, gridName, character, 0))
            return;

        try
        {
            ShipFixerResult result = character != null
                ? Core.FixShip(character, playerId)
                : Core.FixShip(playerId, gridName);

            WriteResponse(result);

            if (result == ShipFixerResult.ShipFixed)
            {
                Plugin.Instance.Log.Info($"Cooldown for Player {player.DisplayName} started!");
                StartCommandCooldown(PlayerKey());
            }
        }
        catch (Exception ex)
        {
            Plugin.Instance.Log.Error(ex, "Error on fixing ship");
            Context.Respond("Error on fixing ship.");
        }
    }

    protected void FixShipModeratorByLookOrName(string gridName = null)
    {
        if (!string.IsNullOrWhiteSpace(gridName))
        {
            FixShipModeratorByName(gridName);
            return;
        }

        if (Context.Caller.IsConsole)
        {
            Context.Respond("Console has no Character so cannot use this command. Use !fixshipmod <Gridname> instead!");
            return;
        }

        MyPlayer player = Utilities.GetPlayerByIdentityId(Context.Caller.IdentityId);
        if (player?.Character is not MyCharacter character)
        {
            Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
            return;
        }

        if (!CheckConfirmation(PlayerKey(), 0, "nogrid", character, 0))
            return;

        try
        {
            WriteResponse(Core.FixShip(character, 0));
        }
        catch (Exception ex)
        {
            Plugin.Instance.Log.Error(ex, "Error on fixing ship");
            Context.Respond("Error on fixing ship.");
        }
    }

    protected void FixShipModeratorById(long gridId)
    {
        if (gridId == 0)
        {
            Context.Respond("Correct Usage is !fixshipmodid EntityID");
            return;
        }

        if (!CheckConfirmation(PlayerKey(), 0, "nogrid", null, gridId))
            return;

        try
        {
            WriteResponse(Core.FixShip(0, gridId));
        }
        catch (Exception ex)
        {
            Plugin.Instance.Log.Error(ex, "Error on fixing ship");
            Context.Respond("Error on fixing ship.");
        }
    }

    private void FixShipModeratorByName(string gridName)
    {
        if (!CheckConfirmation(PlayerKey(), 0, gridName, null, 0))
            return;

        try
        {
            WriteResponse(Core.FixShip(0, gridName));
        }
        catch (Exception ex)
        {
            Plugin.Instance.Log.Error(ex, "Error on fixing ship");
            Context.Respond("Error on fixing ship.");
        }
    }

    private bool CheckConfirmation(string callerKey, long playerId, string gridName, MyCharacter character, long id)
    {
        string confirmationKey = $"{callerKey}:{gridName}:{id}:{playerId}";
        DateTime now = DateTime.UtcNow;

        if (Confirmations.TryGetValue(confirmationKey, out DateTime expiresAt) && expiresAt >= now)
        {
            Confirmations.Remove(confirmationKey);
            return true;
        }

        List<MyCubeGrid> groups = id != 0
            ? ShipFixerCore.FindGridGroupsForPlayer(gridName, playerId, out ShipFixerResult searchResult, id)
            : character == null
                ? ShipFixerCore.FindGridGroupsForPlayer(gridName, playerId, out searchResult)
                : ShipFixerCore.FindLookAtGridGroup(character, playerId, out searchResult);

        if (groups == null || groups.Count == 0 || searchResult != ShipFixerResult.OK)
        {
            WriteResponse(searchResult);
            return false;
        }

        ShipFixerResult checkResult = ShipFixerCore.CheckGroups(groups, out _, playerId, ejectPlayers: false);
        if (checkResult != ShipFixerResult.OK)
        {
            WriteResponse(checkResult);
            return false;
        }

        Confirmations[confirmationKey] = now.AddSeconds(Plugin.Instance.Config.ShipFixerConfirmationInSeconds);
        Context.Respond($"Are you sure you want to continue? Enter the command again within {Plugin.Instance.Config.ShipFixerConfirmationInSeconds} seconds to confirm fixship on {groups[0].DisplayName}.");
        return false;
    }

    private bool CheckCommandCooldown(string callerKey, out int remainingSeconds)
    {
        DateTime now = DateTime.UtcNow;
        if (!CommandCooldowns.TryGetValue(callerKey, out DateTime expiresAt) || expiresAt <= now)
        {
            remainingSeconds = 0;
            return true;
        }

        remainingSeconds = (int)Math.Ceiling((expiresAt - now).TotalSeconds);
        return false;
    }

    private void StartCommandCooldown(string callerKey)
        => CommandCooldowns[callerKey] = DateTime.UtcNow.AddSeconds(Plugin.Instance.Config.ShipFixerCooldownInSeconds);

    private string PlayerKey()
    {
        if (Context.Caller.SteamId != 0)
            return Context.Caller.SteamId.ToString();

        if (Context.Caller.IdentityId != 0)
            return Context.Caller.IdentityId.ToString();

        return "console";
    }

    private void WriteResponse(ShipFixerResult result)
    {
        switch (result)
        {
            case ShipFixerResult.TooFewGrids:
                Context.Respond("Could not find your Grid, Or Check if ownership is correct");
                break;
            case ShipFixerResult.TooManyGrids:
                Context.Respond("Found multiple Grids with same Name. Rename your grid first to something unique.");
                break;
            case ShipFixerResult.UnknownProblem:
                Context.Respond("Could not work with found grid for unknown reason.");
                break;
            case ShipFixerResult.OwnedByDifferentPlayer:
                Context.Respond("Grid seems to be owned by a different player.");
                break;
            case ShipFixerResult.DifferentOwnerOnConnectedGrid:
                Context.Respond("One of the connected grids is owned by a different player.");
                break;
            case ShipFixerResult.GridOccupied:
                Context.Respond("Cockpits or seats are still occupied! Clear them first and try again.");
                break;
            case ShipFixerResult.ShipFixed:
                Context.Respond("Ship was fixed!");
                break;
            case ShipFixerResult.GridNotFound:
                Context.Respond("Grid not found");
                break;
        }
    }
}

[CommandRoot("fixship", "Ship Fixer", "Cut and paste a ship to repair grid bugs.")]
public sealed class FixShipCommand : ShipFixerCommandBase
{
    [Command("", "Cuts and pastes a ship you are looking at or with the given name.")]
    [Permission(MyPromoteLevel.None)]
    public void Run(string gridName = null)
        => FixShipPlayer(gridName);
}

[CommandRoot("fixshipmod", "Ship Fixer", "Moderator ship fixer command.")]
public sealed class FixShipModCommand : ShipFixerCommandBase
{
    [Command("", "Cuts and pastes a ship by look target or grid name.")]
    [Permission(MyPromoteLevel.Moderator)]
    public void Run(string gridName = null)
        => FixShipModeratorByLookOrName(gridName);
}

[CommandRoot("fixshipmodid", "Ship Fixer", "Moderator ship fixer by entity id.")]
public sealed class FixShipModIdCommand : ShipFixerCommandBase
{
    [Command("", "Cuts and pastes a ship by entity id.")]
    [Permission(MyPromoteLevel.Moderator)]
    public void Run(long gridId = 0)
        => FixShipModeratorById(gridId);
}
