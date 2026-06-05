using System.Collections.Generic;
using System.Linq;
using System.Text;
using PluginSdk.Commands;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace ServerPlugin.Commands;

[CommandRoot("econ", "Economy", "Economy and credit commands")]
public sealed class EconModule : CommandModule
{
    [Command("give", "Add credits to a player's account. Use '*' to affect all players.")]
    [Permission(MyPromoteLevel.Admin)]
    public void Give(string player, long amount, bool onlyOnline = false, bool excludeNpcs = true)
    {
        if (!TryFindPlayerIdentities(player, onlyOnline, excludeNpcs, out List<long> foundIdentities))
        {
            Context.Respond("Player cannot be found!");
            return;
        }

        int changedIdentities = 0;
        foreach (long identityId in foundIdentities)
        {
            ChangeBalance(identityId, amount);
            changedIdentities++;
        }

        Context.Respond($"{amount:#,##0} credits given to {changedIdentities:#,##0} account(s)");
    }

    [Command("take", "Take credits from a player's account. Use '*' to affect all players.")]
    [Permission(MyPromoteLevel.Admin)]
    public void Take(string player, long amount, bool onlyOnline = false, bool excludeNpcs = true)
    {
        if (!TryFindPlayerIdentities(player, onlyOnline, excludeNpcs, out List<long> foundIdentities))
        {
            Context.Respond("Player cannot be found!");
            return;
        }

        int changedIdentities = 0;
        foreach (long identityId in foundIdentities)
        {
            ChangeBalance(identityId, -amount);
            changedIdentities++;
        }

        Context.Respond($"{amount:#,##0} credits taken from {changedIdentities:#,##0} account(s)");
    }

    [Command("set", "Set a player's account balance. Use '*' to affect all players.")]
    [Permission(MyPromoteLevel.Admin)]
    public void Set(string player, long amount, bool onlyOnline = false, bool excludeNpcs = true)
    {
        if (!TryFindPlayerIdentities(player, onlyOnline, excludeNpcs, out List<long> foundIdentities))
        {
            Context.Respond("Player cannot be found!");
            return;
        }

        int changedIdentities = 0;
        foreach (long identityId in foundIdentities)
        {
            long balance = MyBankingSystem.GetBalance(identityId);
            ChangeBalance(identityId, amount - balance);
            changedIdentities++;
        }

        Context.Respond($"Balance(s) set to {amount:#,##0} on {changedIdentities:#,##0} accounts");
    }

    [Command("reset", "Reset credits in a player's account to 10,000. Use '*' to affect all players.")]
    [Permission(MyPromoteLevel.Admin)]
    public void Reset(string player, bool onlyOnline = false, bool excludeNpcs = true)
        => Set(player, 10_000, onlyOnline, excludeNpcs);

    [Command("top", "Return player balances sorted highest to lowest.")]
    [Permission(MyPromoteLevel.None)]
    public string Top(bool onlyOnline = false, bool excludeNpcs = true)
    {
        TryFindPlayerIdentities("*", onlyOnline, excludeNpcs, out List<long> foundIdentities);

        Dictionary<IMyIdentity, long> balances = new Dictionary<IMyIdentity, long>();
        foreach (long identityId in foundIdentities)
        {
            IMyIdentity identity = MySession.Static.Players.TryGetIdentity(identityId);
            if (identity != null)
                balances[identity] = MyBankingSystem.GetBalance(identityId);
        }

        StringBuilder data = new StringBuilder();
        data.AppendLine("Summary of balances across the server");

        foreach (KeyValuePair<IMyIdentity, long> value in balances.OrderByDescending(x => x.Value).ThenBy(x => x.Key.DisplayName))
            data.AppendLine($"Player: {value.Key.DisplayName} - Balance: {value.Value:#,##0}");

        return data.ToString();
    }

    [Command("check", "Check a player's balance.")]
    [Permission(MyPromoteLevel.None)]
    public string Check(string player)
    {
        IMyIdentity identity = Utilities.GetIdentityByNameOrIds(player);
        if (identity == null)
            return "Player cannot be found!";

        long balance = MyBankingSystem.GetBalance(identity.IdentityId);
        return $"{identity.DisplayName}'s balance is {balance:#,##0} credits";
    }

    [Command("pay", "Pay another online player from your account.")]
    [Permission(MyPromoteLevel.None)]
    public string Pay(string player, long amount)
    {
        if (amount <= 0)
            return "Amount cannot be negative";

        if (Context.Caller.IsConsole || Context.Caller.IdentityId == 0)
            return "Console cannot execute this command";

        IMyPlayer target = Utilities.GetPlayerByNameOrId(player);
        if (target == null)
            return "Player is not online or cannot be found!";

        long fromIdentityId = Context.Caller.IdentityId;
        long toIdentityId = target.Identity.IdentityId;

        if (fromIdentityId == toIdentityId)
            return "You cannot pay yourself!";

        long finalFromBalance = MyBankingSystem.GetBalance(fromIdentityId) - amount;
        if (finalFromBalance < 0)
            return $"Sorry, but you are short {-finalFromBalance:#,##0} credits!";

        long finalToBalance = MyBankingSystem.GetBalance(toIdentityId) + amount;
        MyBankingSystem.RequestTransfer_BroadcastToClients(fromIdentityId, toIdentityId, amount, finalFromBalance, finalToBalance);
        return $"Sent {amount:#,##0} credits to {target.DisplayName}.";
    }

    private static void ChangeBalance(long identityId, long amount)
    {
        long balance = MyBankingSystem.GetBalance(identityId);
        if (balance + amount < 0)
            amount = -balance;

        MyBankingSystem.ChangeBalance(identityId, amount);
    }

    private static bool TryFindPlayerIdentities(string playerName, bool onlyOnline, bool excludeNpcs, out List<long> foundIdentities)
    {
        List<long> relevantIdentities = new List<long>();
        var players = MySession.Static.Players;

        if (playerName != "*")
        {
            IMyIdentity identity = Utilities.GetIdentityByNameOrIds(playerName);
            if (identity == null)
            {
                foundIdentities = relevantIdentities;
                return false;
            }

            relevantIdentities.Add(identity.IdentityId);
        }
        else
        {
            relevantIdentities.AddRange(players.GetAllIdentities().Select(identity => identity.IdentityId));
        }

        IEnumerable<long> identitiesToCheck = relevantIdentities;

        if (onlyOnline)
            identitiesToCheck = identitiesToCheck.Where(identityId => players.IsPlayerOnline(identityId));

        if (excludeNpcs)
            identitiesToCheck = identitiesToCheck.Where(identityId => !players.IdentityIsNpc(identityId));

        foundIdentities = identitiesToCheck.ToList();
        return true;
    }
}
