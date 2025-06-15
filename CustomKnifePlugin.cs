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
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Your Name";
    public override string ModuleDescription => "Permet aux joueurs de changer leur couteau via une commande chat";

    // Dictionnaire pour stocker le choix de couteau de chaque joueur (SteamID -> KnifeID)
    private readonly Dictionary<string, int> _playerKnifeChoices = new();

    private readonly Dictionary<string, int> _availableKnives = new()
    {
        { "karambit", 507 },
        { "m9", 508 },
        { "butterfly", 515 },
        { "bayonet", 500 },
        { "flip", 505 },
        { "gut", 506 },
        { "classic", 503 },
        { "skeleton", 525 },
        { "nomad", 521 },
        { "talon", 523 },
        { "stiletto", 522 },
        { "ursus", 519 },
        { "paracord", 517 },
        { "survival", 518 },
        { "huntsman", 509 },
        { "falchion", 512 },
        { "bowie", 514 },
        { "daggers", 516 },
        { "navaja", 520 },
        { "kukri", 526 }
    };

    public override void Load(bool hotReload)
    {
        AddCommand("css_knife", "Changer votre couteau", CommandKnife);
        RegisterListener<Listeners.OnMapStart>(OnMapStart);

        // Créer un timer qui vérifie les couteaux toutes les 0.1 secondes
        AddTimer(0.1f, CheckKnives, TimerFlags.REPEAT);
    }

    private void CheckKnives()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || player.IsBot || !player.PlayerPawn.IsValid)
                continue;

            string steamId = player.SteamID.ToString();
            if (!_playerKnifeChoices.TryGetValue(steamId, out int knifeId))
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null) continue;

            foreach (var weapon in pawn.WeaponServices.MyWeapons)
            {
                if (weapon?.Value == null) continue;
                var weaponName = weapon.Value.DesignerName;
                if (weaponName == null) continue;

                if (weaponName.Contains("knife") || weaponName.Contains("bayonet"))
                {
                    // Réappliquer le skin du couteau
                    Server.ExecuteCommand($"subclass_create {knifeId}");
                    Server.ExecuteCommand($"subclass_change {knifeId} weapon_knife");
                    break;
                }
            }
        }
    }

    private void OnMapStart(string mapName)
    {
        // Activer les commandes nécessaires au démarrage de la map
        Server.ExecuteCommand("sv_cheats 1");
        Server.ExecuteCommand("mp_drop_knife_enable 1");
    }

    private void ApplyKnife(CCSPlayerController player, int knifeId)
    {
        if (player == null || !player.IsValid || !player.PlayerPawn.IsValid) return;

        AddTimer(0.1f, () => {
            if (player.IsValid && player.PlayerPawn.IsValid)
            {
                Server.ExecuteCommand($"subclass_create {knifeId}");
                Server.ExecuteCommand($"subclass_change {knifeId} weapon_knife");
            }
        });
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        
        if (player == null || !player.IsValid || player.IsBot || !player.PlayerPawn.IsValid)
            return HookResult.Continue;

        string steamId = player.SteamID.ToString();
        if (_playerKnifeChoices.TryGetValue(steamId, out int knifeId))
        {
            ApplyKnife(player, knifeId);
        }

        return HookResult.Continue;
    }

    private void CommandKnife(CCSPlayerController? player, CommandInfo command)
    {
        if (player == null || !player.IsValid || player.IsBot || !player.PlayerPawn.IsValid)
        {
            return;
        }

        if (command.ArgCount < 2)
        {
            player.PrintToChat($" \x04[Knife] \x01Usage: !knife <{string.Join("/", _availableKnives.Keys)}>");
            return;
        }

        string requestedKnife = command.ArgByIndex(1).ToLower();

        if (!_availableKnives.ContainsKey(requestedKnife))
        {
            player.PrintToChat($" \x04[Knife] \x01Couteau non valide. Choix disponibles: {string.Join(", ", _availableKnives.Keys)}");
            return;
        }

        // Sauvegarder le choix du joueur
        int knifeId = _availableKnives[requestedKnife];
        _playerKnifeChoices[player.SteamID.ToString()] = knifeId;

        // Appliquer le changement de couteau
        ApplyKnife(player, knifeId);

        // Forcer le joueur à lâcher son couteau actuel
        var pawn = player.PlayerPawn.Value;
        if (pawn != null)
        {
            foreach (var weapon in pawn.WeaponServices.MyWeapons)
            {
                if (weapon?.Value == null) continue;
                var weaponName = weapon.Value.DesignerName;
                if (weaponName == null) continue;

                if (weaponName.Contains("knife") || weaponName.Contains("bayonet"))
                {
                    Server.ExecuteCommand($"sm_slay #{player.UserId}");
                    AddTimer(0.1f, () => {
                        if (player.IsValid)
                        {
                            player.PrintToChat($" \x04[Knife] \x01Votre couteau a été changé en {requestedKnife}! Ce choix sera conservé après votre mort.");
                        }
                    });
                    break;
                }
            }
        }
    }
}
