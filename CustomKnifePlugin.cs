using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;

namespace CustomKnifePlugin;

[MinimumApiVersion(100)]
public class CustomKnifePlugin : BasePlugin
{
    public override string ModuleName => "Custom Knife Plugin";
    public override string ModuleVersion => "2.0.0";
    public override string ModuleAuthor => "Your Name";
    public override string ModuleDescription => "Permet aux joueurs de changer leur couteau via une commande chat";

    private readonly Dictionary<string, string> _playerKnifeChoices = new();

    private readonly Dictionary<string, string> _availableKnives = new()
    {
        { "karambit", "weapon_knife_karambit" },
        { "m9", "weapon_knife_m9_bayonet" },
        { "butterfly", "weapon_knife_butterfly" },
        { "bayonet", "weapon_bayonet" },
        { "flip", "weapon_knife_flip" },
        { "gut", "weapon_knife_gut" },
        { "classic", "weapon_knife_css" },
        { "skeleton", "weapon_knife_skeleton" },
        { "nomad", "weapon_knife_outdoor" },
        { "talon", "weapon_knife_widowmaker" },
        { "stiletto", "weapon_knife_stiletto" },
        { "ursus", "weapon_knife_ursus" },
        { "paracord", "weapon_knife_cord" },
        { "survival", "weapon_knife_canis" },
        { "huntsman", "weapon_knife_tactical" },
        { "falchion", "weapon_knife_falchion" },
        { "bowie", "weapon_knife_survival_bowie" },
        { "daggers", "weapon_knife_push" },
        { "navaja", "weapon_knife_gypsy_jackknife" },
        { "default", "weapon_knife" }
    };

    public override void Load(bool hotReload)
    {
        AddCommand("css_knife", "Changer votre couteau", CommandKnife);
        AddCommand("css_knives", "Afficher la liste des couteaux disponibles", CommandKnifeList);
        
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerConnectFull>(OnPlayerConnectFull);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
    }

    private void OnMapStart(string mapName)
    {
        AddTimer(1.0f, () => {
            Server.ExecuteCommand("mp_drop_knife_enable 1");
        });
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid || player.IsBot)
            return HookResult.Continue;

        string steamId = player.SteamID.ToString();
        if (!_playerKnifeChoices.ContainsKey(steamId))
        {
            _playerKnifeChoices[steamId] = "default";
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (player == null || !player.IsValid || player.IsBot || !player.PlayerPawn.IsValid)
            return HookResult.Continue;

        string steamId = player.SteamID.ToString();
        
        AddTimer(0.5f, () => {
            ApplyPlayerKnife(player);
        });

        return HookResult.Continue;
    }

    private void ApplyPlayerKnife(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || player.IsBot || !player.PlayerPawn.IsValid)
            return;

        string steamId = player.SteamID.ToString();
        if (!_playerKnifeChoices.TryGetValue(steamId, out string? knifeChoice) || knifeChoice == "default")
            return;

        if (!_availableKnives.TryGetValue(knifeChoice, out string? knifeEntity))
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn?.WeaponServices == null)
            return;

        foreach (var weapon in pawn.WeaponServices.MyWeapons)
        {
            if (weapon?.Value == null) continue;
            var weaponName = weapon.Value.DesignerName;
            if (weaponName == null) continue;

            if (weaponName.Contains("knife") || weaponName.Contains("bayonet"))
            {
                weapon.Value.Remove();
                break;
            }
        }

        AddTimer(0.1f, () => {
            if (player.IsValid && player.PlayerPawn.IsValid)
            {
                player.GiveNamedItem(knifeEntity);
            }
        });
    }

    private void CommandKnifeList(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot)
            return;

        var knivesList = string.Join(", ", _availableKnives.Keys.Where(k => k != "default"));
        player.PrintToChat($" \x04[Knife] \x01Couteaux disponibles: {knivesList}");
        player.PrintToChat($" \x04[Knife] \x01Usage: !knife <nom_du_couteau>");
    }

    private void CommandKnife(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot || !player.PlayerPawn.IsValid)
            return;

        if (command.ArgCount < 2)
        {
            var knivesList = string.Join(", ", _availableKnives.Keys.Where(k => k != "default"));
            player.PrintToChat($" \x04[Knife] \x01Usage: !knife <{knivesList}>");
            player.PrintToChat($" \x04[Knife] \x01Ou tapez !knives pour voir la liste complète");
            return;
        }

        string requestedKnife = command.ArgByIndex(1).ToLower();

        if (requestedKnife == "reset" || requestedKnife == "default")
        {
            _playerKnifeChoices[player.SteamID.ToString()] = "default";
            player.PrintToChat($" \x04[Knife] \x01Votre couteau a été remis par défaut!");
            
            RestartPlayer(player);
            return;
        }

        if (!_availableKnives.ContainsKey(requestedKnife) || requestedKnife == "default")
        {
            var knivesList = string.Join(", ", _availableKnives.Keys.Where(k => k != "default"));
            player.PrintToChat($" \x04[Knife] \x01Couteau non valide. Choix disponibles: {knivesList}");
            return;
        }

        string steamId = player.SteamID.ToString();
        _playerKnifeChoices[steamId] = requestedKnife;

        player.PrintToChat($" \x04[Knife] \x01Votre couteau a été changé en {requestedKnife}!");
        player.PrintToChat($" \x04[Knife] \x01Vous respawnerez avec ce couteau à chaque round.");
        
        RestartPlayer(player);
    }

    private void RestartPlayer(CCSPlayerController player)
    {
        if (player == null || !player.IsValid || !player.PlayerPawn.IsValid)
            return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        var position = pawn.CBodyComponent?.SceneNode?.AbsOrigin;
        var angles = pawn.CBodyComponent?.SceneNode?.AbsRotation;
        var team = player.TeamNum;

        pawn.Health = 0;
        pawn.TakesDamage = false;
        
        AddTimer(0.1f, () => {
            if (player.IsValid)
            {
                player.Respawn();
                
                AddTimer(0.5f, () => {
                    ApplyPlayerKnife(player);
                });
            }
        });
    }
}
