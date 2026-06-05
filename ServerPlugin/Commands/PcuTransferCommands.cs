using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using PluginSdk.Commands;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using ServerPlugin.PcuTransfer;
using VRage.Game.ModAPI;
using VRage.Groups;

namespace ServerPlugin.Commands;

public abstract class PcuTransferCommandBase : CommandModule
{
    private static readonly Dictionary<string, DateTime> Confirmations = new Dictionary<string, DateTime>();
    private static readonly PcuTransferCore Core = new PcuTransferCore();
    private const int ConfirmationSeconds = 30;

    protected void TransferToPlayer(string playerName, string gridName, bool pcu, bool ownership, bool force)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Context.Respond("Correct Usage is !transfer <playerName> [gridName]");
            return;
        }

        MyIdentity author = Utilities.GetIdentityByNameOrIds(playerName) as MyIdentity;
        if (author == null)
        {
            Context.Respond("Player not Found!");
            return;
        }

        ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups;
        string confirmGridName = gridName;

        if (string.IsNullOrWhiteSpace(gridName))
        {
            MyCharacter character = GetCallerCharacter("Console has no Character so cannot use this command. Use !transfer <playerName> <gridname> instead!");
            if (character == null)
                return;

            groups = GridGroupFinder.FindLookAtGridGroup(character);
            confirmGridName = "nogrid_" + pcu + "_" + ownership;
        }
        else
        {
            groups = GridGroupFinder.FindGridGroup(gridName);
        }

        string confirmationKey = $"{PlayerKey()}:{confirmGridName}:{author.IdentityId}:{pcu}:{ownership}:{force}";
        if (!CheckConfirmation(confirmationKey, groups, author, pcu, force))
            return;

        try
        {
            if (!Core.TryGetTransferGroup(groups, author, pcu, force, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, out string error))
            {
                Context.Respond(error);
                return;
            }

            Context.Respond(StartMessage(author.DisplayName, pcu, ownership));
            Context.Respond(Core.Transfer(group, author, pcu, ownership));
        }
        catch (Exception ex)
        {
            Plugin.Instance.Log.Error(ex, "Error on transferring ship");
            Context.Respond("Error Transferring Ship!");
        }
    }

    protected void TransferToNobody(string gridName, bool pcu, bool ownership)
    {
        ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups;
        string confirmGridName = gridName;

        if (string.IsNullOrWhiteSpace(gridName))
        {
            MyCharacter character = GetCallerCharacter("Console has no Character so cannot use this command. Use !transfernobody <gridname> instead!");
            if (character == null)
                return;

            groups = GridGroupFinder.FindLookAtGridGroup(character);
            confirmGridName = "nogrid_" + pcu + "_" + ownership;
        }
        else
        {
            groups = GridGroupFinder.FindGridGroup(gridName);
        }

        string confirmationKey = $"{PlayerKey()}:{confirmGridName}:0:{pcu}:{ownership}:false";
        if (!CheckConfirmationNobody(confirmationKey, groups))
            return;

        try
        {
            if (!Core.TryGetNobodyTransferGroup(groups, out MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group group, out string error))
            {
                Context.Respond(error);
                return;
            }

            Context.Respond(StartNobodyMessage(pcu, ownership));
            Context.Respond(Core.TransferNobody(group, pcu, ownership));
        }
        catch (Exception ex)
        {
            Plugin.Instance.Log.Error(ex, "Error on transferring ship");
            Context.Respond("Error Transferring Ship!");
        }
    }

    private bool CheckConfirmation(
        string confirmationKey,
        ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups,
        MyIdentity author,
        bool pcu,
        bool force)
    {
        DateTime now = DateTime.UtcNow;
        if (Confirmations.TryGetValue(confirmationKey, out DateTime expiresAt) && expiresAt >= now)
        {
            Confirmations.Remove(confirmationKey);
            return true;
        }

        if (!Core.TryGetTransferGroup(groups, author, pcu, force, out _, out string error))
        {
            Context.Respond(error);
            return false;
        }

        Confirmations[confirmationKey] = now.AddSeconds(ConfirmationSeconds);
        Context.Respond("Are you sure you want to continue? Enter the command again within 30 seconds to confirm.");
        return false;
    }

    private bool CheckConfirmationNobody(
        string confirmationKey,
        ConcurrentBag<MyGroups<MyCubeGrid, MyGridPhysicalGroupData>.Group> groups)
    {
        DateTime now = DateTime.UtcNow;
        if (Confirmations.TryGetValue(confirmationKey, out DateTime expiresAt) && expiresAt >= now)
        {
            Confirmations.Remove(confirmationKey);
            return true;
        }

        if (!Core.TryGetNobodyTransferGroup(groups, out _, out string error))
        {
            Context.Respond(error);
            return false;
        }

        Confirmations[confirmationKey] = now.AddSeconds(ConfirmationSeconds);
        Context.Respond("Are you sure you want to continue? Enter the command again within 30 seconds to confirm.");
        return false;
    }

    private MyCharacter GetCallerCharacter(string consoleMessage)
    {
        if (Context.Caller.IsConsole || Context.Caller.IdentityId == 0)
        {
            Context.Respond(consoleMessage);
            return null;
        }

        MyPlayer player = Utilities.GetPlayerByIdentityId(Context.Caller.IdentityId);
        if (player?.Character is MyCharacter character)
            return character;

        Context.Respond("You have no Character currently. Make sure to spawn and be out of cockpit!");
        return null;
    }

    private string PlayerKey()
    {
        if (Context.Caller.SteamId != 0)
            return Context.Caller.SteamId.ToString();

        if (Context.Caller.IdentityId != 0)
            return Context.Caller.IdentityId.ToString();

        return "console";
    }

    private static string StartMessage(string playerName, bool pcu, bool ownership)
    {
        if (pcu && ownership)
            return "Start transferring PCU and Ownership to " + playerName + "!";

        if (pcu)
            return "Start transferring PCU to " + playerName + "!";

        return "Start transferring Ownership to " + playerName + "!";
    }

    private static string StartNobodyMessage(bool pcu, bool ownership)
    {
        if (pcu && ownership)
            return "Start transferring PCU and Ownership to nobody!";

        if (pcu)
            return "Start transferring PCU to nobody!";

        return "Start transferring Ownership to nobody!";
    }
}

[CommandRoot("transfer", "PCU Transfer", "Transfer PCU and ownership to a player.")]
public sealed class TransferCommand : PcuTransferCommandBase
{
    [Command("", "Transfers PCU and ownership to a player.")]
    [Permission(MyPromoteLevel.SpaceMaster)]
    public void Run(string playerName = null, string gridName = null)
        => TransferToPlayer(playerName, gridName, pcu: true, ownership: true, force: false);
}

[CommandRoot("forcetransfer", "PCU Transfer", "Transfer PCU and ownership to a player, ignoring limits.")]
public sealed class ForceTransferCommand : PcuTransferCommandBase
{
    [Command("", "Transfers PCU and ownership to a player, ignoring limits.")]
    [Permission(MyPromoteLevel.SpaceMaster)]
    public void Run(string playerName = null, string gridName = null)
        => TransferToPlayer(playerName, gridName, pcu: true, ownership: true, force: true);
}

[CommandRoot("transferpcu", "PCU Transfer", "Transfer PCU to a player.")]
public sealed class TransferPcuCommand : PcuTransferCommandBase
{
    [Command("", "Transfers PCU to a player.")]
    [Permission(MyPromoteLevel.SpaceMaster)]
    public void Run(string playerName = null, string gridName = null)
        => TransferToPlayer(playerName, gridName, pcu: true, ownership: false, force: false);
}

[CommandRoot("forcetransferpcu", "PCU Transfer", "Transfer PCU to a player, ignoring limits.")]
public sealed class ForceTransferPcuCommand : PcuTransferCommandBase
{
    [Command("", "Transfers PCU to a player, ignoring limits.")]
    [Permission(MyPromoteLevel.SpaceMaster)]
    public void Run(string playerName = null, string gridName = null)
        => TransferToPlayer(playerName, gridName, pcu: true, ownership: false, force: true);
}

[CommandRoot("transferowner", "PCU Transfer", "Transfer ownership to a player.")]
public sealed class TransferOwnerCommand : PcuTransferCommandBase
{
    [Command("", "Transfers ownership to a player.")]
    [Permission(MyPromoteLevel.SpaceMaster)]
    public void Run(string playerName = null, string gridName = null)
        => TransferToPlayer(playerName, gridName, pcu: false, ownership: true, force: false);
}

[CommandRoot("transfernobody", "PCU Transfer", "Remove PCU and ownership.")]
public sealed class TransferNobodyCommand : PcuTransferCommandBase
{
    [Command("", "Removes PCU and ownership.")]
    [Permission(MyPromoteLevel.SpaceMaster)]
    public void Run(string gridName = null)
        => TransferToNobody(gridName, pcu: true, ownership: true);
}

[CommandRoot("transferpcunobody", "PCU Transfer", "Remove PCU.")]
public sealed class TransferPcuNobodyCommand : PcuTransferCommandBase
{
    [Command("", "Removes PCU.")]
    [Permission(MyPromoteLevel.SpaceMaster)]
    public void Run(string gridName = null)
        => TransferToNobody(gridName, pcu: true, ownership: false);
}

[CommandRoot("transferownernobody", "PCU Transfer", "Remove ownership.")]
public sealed class TransferOwnerNobodyCommand : PcuTransferCommandBase
{
    [Command("", "Removes ownership.")]
    [Permission(MyPromoteLevel.SpaceMaster)]
    public void Run(string gridName = null)
        => TransferToNobody(gridName, pcu: false, ownership: true);
}
