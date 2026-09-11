using System.Drawing;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;

namespace FFMENU;

public class FFMENU : BasePlugin
{
    public override string ModuleName => "FFMENU - Sayrox Edition";
    public override string ModuleVersion => "1.0.1";
    public override string ModuleAuthor => "Sayrox";

    private int _ffMenuSeconds  = -1;
    private int _ffzSeconds     = -1;
    private int _ffdSeconds     = -1;
    private string _ffdRegion   = "";

    private CounterStrikeSharp.API.Modules.Timers.Timer? _ticker;

    private static readonly List<(string Name, string Classname)> Primaries = new()
    {
        ("AK-47",          "weapon_ak47"),
        ("M4A4",            "weapon_m4a1"),
        ("M4A1-S",          "weapon_m4a1_silencer"),
        ("FAMAS",            "weapon_famas"),
        ("Galil AR",        "weapon_galilar"),
        ("SG 553",         "weapon_sg556"),
        ("AUG",            "weapon_aug"),
        ("AWP",            "weapon_awp"),
        ("SSG 08",          "weapon_ssg08"),
        ("G3SG1",          "weapon_g3sg1"),
        ("SCAR-20",        "weapon_scar20"),
        ("M249",            "weapon_m249"),
        ("Negev",           "weapon_negev"),
        ("XM1014",          "weapon_xm1014"),
        ("Nova",            "weapon_nova"),
        ("Sawed-Off",       "weapon_sawedoff"),
        ("MAG-7",           "weapon_mag7"),
        ("M3 (Double Barrel)","weapon_m3"),
        ("PP-Bizon",        "weapon_bizon"),
        ("MP9",             "weapon_mp9"),
        ("MP7",             "weapon_mp7"),
        ("MP5-SD",          "weapon_mp5sd"),
        ("UMP-45",          "weapon_ump45"),
        ("P90",             "weapon_p90"),
        ("MAC-10",          "weapon_mac10"),
    };

    private static readonly List<(string Name, string Classname)> Secondaries = new()
    {
        ("Glock-18",       "weapon_glock"),
        ("P2000",          "weapon_hkp2000"),
        ("USP-S",          "weapon_usp_silencer"),
        ("Dual Berettas",  "weapon_elite"),
        ("P250",           "weapon_p250"),
        ("Five-SeveN",     "weapon_fiveseven"),
        ("Tec-9",          "weapon_tec9"),
        ("CZ75-Auto",      "weapon_cz75a"),
        ("Desert Eagle",   "weapon_deagle"),
        ("R8 Revolver",    "weapon_revolver"),
    };

    private HashSet<int> _awaitingPrimary   = new();
    private HashSet<int> _awaitingSecondary = new();

    private static string BuildCountdownHtml(string title, string line2, int seconds)
        => $"<br><font color='#00FFDD' class='fontSize-l'>{title}</font><br>" +
           $"<font color='#FFFFFF'>Son : </font><font color='#68a3e5'>{seconds}s</font><br>" +
           (string.IsNullOrEmpty(line2) ? "" : $"<font color='#AAAAAA' class='fontSize-m'>{line2}</font><br>");

    private static readonly HashSet<string> PrimaryClassnames = new(Primaries.Select(p => p.Classname));
    private static readonly HashSet<string> SecondaryClassnames = new(Secondaries.Select(s => s.Classname));

    private static readonly string Prefix = $" {ChatColors.Default}[{ChatColors.Red} FFMENU {ChatColors.Default}]";
    private int _tickCounter = 0;

    private Dictionary<string, FFZoneData> _ffZones = new();
    private Dictionary<int, Vector> _tempP1 = new();
    private Dictionary<int, Vector> _tempP2 = new();
    private Dictionary<int, Vector> _tempArrow = new();
    private Dictionary<int, int> _setupStep = new();
    private string _activeZoneName = "";
    private List<CBeam> _activeBeams = new();
    private CDynamicProp? _activeArrow = null;
    private Vector? _activeArrowBasePos = null;
    private float _rgbHue = 0f;

    private string ZonesPath => Path.Combine(ModuleDirectory, "ff_zones.json");

    public override void Load(bool hotReload)
    {
        if (File.Exists(ZonesPath))
        {
            try { _ffZones = JsonSerializer.Deserialize<Dictionary<string, FFZoneData>>(File.ReadAllText(ZonesPath)) ?? new(); }
            catch { _ffZones = new(); }
        }

        RegisterListener<Listeners.OnMapStart>(mapName =>
        {
            Server.PrecacheModel("models/props/de_nuke/hr_nuke/nuke_signs/nuke_sign_arrow_down.vmdl");
        });

        if (hotReload && !string.IsNullOrEmpty(Server.MapName))
        {
            Server.PrecacheModel("models/props/de_nuke/hr_nuke/nuke_signs/nuke_sign_arrow_down.vmdl");
        }

        _ticker = AddTimer(0.1f, OnTick, TimerFlags.REPEAT);
    }

    [GameEventHandler]
    public HookResult OnBulletImpact(EventBulletImpact @event, GameEventInfo info)
    {
        var p = @event.Userid;
        if (p == null || !_setupStep.TryGetValue(p.Slot, out var step) || step == 0) return HookResult.Continue;

        Vector impactPos = new Vector(@event.X, @event.Y, @event.Z);

        if (step == 1)
        {
            _tempP1[p.Slot] = impactPos;
            _setupStep[p.Slot] = 2;
            p.PrintToChat($"{Prefix} {ChatColors.Lime}P1 Noktası Ayarlandı! {ChatColors.Default}Şimdi P2 noktası için bir yere ateş et.");
        }
        else if (step == 2)
        {
            _tempP2[p.Slot] = impactPos;
            _setupStep[p.Slot] = 3;
            p.PrintToChat($"{Prefix} {ChatColors.Lime}P2 Noktası Ayarlandı! {ChatColors.Default}Lütfen bir bölgeye geçin ve {ChatColors.Gold}!arrowadd {ChatColors.Default}kullanın.");
        }

        return HookResult.Continue;
    }

    private void OnTick()
    {
        _tickCounter++;
        bool isSecondTick = _tickCounter >= 10;
        if (isSecondTick) _tickCounter = 0;

        _rgbHue += 15.0f;
        if (_rgbHue >= 360f) _rgbHue = 0f;
        Color rgbColor = HSLToRGB(_rgbHue, 1.0f, 0.5f);

        foreach (var beam in _activeBeams)
        {
            if (beam is { IsValid: true }) 
            {
                beam.Render = rgbColor;
                Utilities.SetStateChanged(beam, "CBaseModelEntity", "m_clrRender");
            }
        }

        if (_activeArrow != null && _activeArrow.IsValid && _activeArrowBasePos != null)
        {
            float hoverOffset = (float)Math.Sin(Server.CurrentTime * 4.0) * 20.0f;
            _activeArrow.Teleport(_activeArrowBasePos + new Vector(0, 0, hoverOffset), null, null);
        }

        string currentHUD = "";

        if (_ffMenuSeconds > 0)
        {
            currentHUD = BuildCountdownHtml("FF Başlamasına Son", "", _ffMenuSeconds);
            if (isSecondTick)
            {
                _ffMenuSeconds--;
                if (_ffMenuSeconds == 0)
                {
                    _ffMenuSeconds = -1;
                    CloseWeaponMenusForAll();
                    Server.ExecuteCommand("mp_teammates_are_enemies 1");
                    Server.PrintToChatAll($"{Prefix} {ChatColors.Lime}FF Başladı! Friendly Fire AKTİF!");
                }
                else RefreshWeaponMenus();
            }
        }
        else if (_ffzSeconds > 0)
        {
            currentHUD = BuildCountdownHtml("FF Açılmasına Son", "", _ffzSeconds);
            if (isSecondTick)
            {
                _ffzSeconds--;
                if (_ffzSeconds == 0)
                {
                    _ffzSeconds = -1;
                    Server.ExecuteCommand("mp_teammates_are_enemies 1");
                    Server.PrintToChatAll($"{Prefix} {ChatColors.Lime}Friendly Fire AKTİF!");
                }
            }
        }
        else if (_ffdSeconds > 0)
        {
            currentHUD = BuildCountdownHtml("FFD Uygulanmasına Son", $"Bölge : {_activeZoneName}", _ffdSeconds);
            if (isSecondTick)
            {
                _ffdSeconds--;
                if (_ffdSeconds == 0)
                {
                    _ffdSeconds = -1;
                    Server.ExecuteCommand("mp_teammates_are_enemies 0");
                    
                    if (_ffZones.TryGetValue(_activeZoneName, out var zone))
                    {
                        foreach (var p in Utilities.GetPlayers())
                        {
                            if (p.IsValid && p.TeamNum == (int)CsTeam.Terrorist && p.PawnIsAlive && !p.IsBot)
                            {
                                if (!zone.Box.IsInside(p.PlayerPawn.Value!.AbsOrigin!))
                                    p.PlayerPawn.Value.CommitSuicide(false, true);
                            }
                        }
                        Server.PrintToChatAll($"{Prefix} {ChatColors.Red}FF Dondur Bitti Bölgeye Giremeyen Kişiler Slaylendi.");
                    }
                    
                    FreezeTerrorists();
                    ClearBeams();
                }
            }
        }

        if (!string.IsNullOrEmpty(currentHUD)) PrintCenterHtmlToAll(currentHUD);
    }

    private void PrintCenterHtmlToAll(string html)
    {
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && !p.IsBot)
            {
                if (!_awaitingPrimary.Contains(p.Slot) && !_awaitingSecondary.Contains(p.Slot))
                {
                    p.PrintToCenterHtml(html);
                }
            }
        }
    }

    private void RefreshWeaponMenus()
    {
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && !p.IsBot && p.PawnIsAlive)
            {
                if (_awaitingPrimary.Contains(p.Slot)) OpenPrimaryMenu(p);
                else if (_awaitingSecondary.Contains(p.Slot)) OpenSecondaryMenu(p);
            }
        }
    }

    private void CloseWeaponMenusForAll()
    {
        foreach (var p in Utilities.GetPlayers())
        {
            if (p.IsValid && !p.IsBot)
            {
                if (_awaitingPrimary.Contains(p.Slot) || _awaitingSecondary.Contains(p.Slot))
                {
                    MenuManager.CloseActiveMenu(p);
                }
            }
        }
        _awaitingPrimary.Clear();
        _awaitingSecondary.Clear();
    }

    [ConsoleCommand("css_ffmenu", "FF menü geri sayımını başlatır ve silah menüsü açar")]
    public void OnFFMenu(CCSPlayerController? player, CommandInfo info)
    {
        if (!CheckAdmin(player, "!ffmenu")) return;
        if (info.ArgCount < 2 || !int.TryParse(info.GetArg(1), out int sec) || sec < 1 || sec > 30)
        {
            player!.PrintToChat($"{Prefix} {ChatColors.Red}Yanlış Kullanım! Doğrusu : !ffmenu <Sayı> Örnek : !ffmenu 30");
            return;
        }
        _ffMenuSeconds = sec;
        Server.PrintToChatAll($"{Prefix} {ChatColors.Default}FF Menüsü başlatıldı. {ChatColors.Lime}{sec} {ChatColors.Default}saniye boyunca silah seçebilirsiniz!");
        OpenPrimaryMenuForAll();
    }

    [ConsoleCommand("css_ffmenu0", "FF menü timerini durdurur")]
    public void OnFFMenuStop(CCSPlayerController? player, CommandInfo info)
    {
        if (!CheckAdmin(player, "!ffmenu0")) return;
        _ffMenuSeconds = -1;
        CloseWeaponMenusForAll();
        player!.PrintToChat($"{Prefix} {ChatColors.Default}FF Menü timeri durduruldu.");
    }

    [ConsoleCommand("css_ffz", "FF açılma geri sayımını başlatır")]
    public void OnFFZ(CCSPlayerController? player, CommandInfo info)
    {
        if (!CheckAdmin(player, "!ffz")) return;
        if (info.ArgCount < 2 || !int.TryParse(info.GetArg(1), out int sec) || sec < 1)
        {
            player!.PrintToChat($"{Prefix} {ChatColors.Red}Yanlış Kullanım! Doğrusu : !ffz <Sayı> Örnek : !ffz 30");
            return;
        }
        _ffzSeconds = sec;
        Server.PrintToChatAll($"{Prefix} {ChatColors.Default}FF {ChatColors.Lime}{sec} {ChatColors.Default}saniye sonra açılacak!");
    }

    [ConsoleCommand("css_ffz0", "FF açılma timerini durdurur")]
    public void OnFFZStop(CCSPlayerController? player, CommandInfo info)
    {
        if (!CheckAdmin(player, "!ffz0")) return;
        _ffzSeconds = -1;
        Server.ExecuteCommand("mp_teammates_are_enemies 0");
        player!.PrintToChat($"{Prefix} {ChatColors.Default}FFZ timeri durduruldu ve FF kapatıldı.");
    }

    [ConsoleCommand("css_ffd", "FF kapat + T dondurma geri sayımını başlatır")]
    public void OnFFD(CCSPlayerController? player, CommandInfo info)
    {
        if (!CheckAdmin(player, "!ffd")) return;
        if (info.ArgCount < 2 || !int.TryParse(info.GetArg(1), out int sec) || sec < 1)
        {
            player!.PrintToChat($"{Prefix} {ChatColors.Red}Yanlış Kullanım! Doğrusu : !ffd <Saniye> Örnek : !ffd 30");
            return;
        }

        if (_ffZones.Count == 0)
        {
            player!.PrintToChat($"{Prefix} {ChatColors.Red}Kayıtlı bölge bulunamadı! Önce bölge oluşturmalısın.");
            return;
        }

        var menu = new CenterHtmlMenu("FF Dondur Bölgesi Seç", this);
        foreach (var zoneName in _ffZones.Keys)
        {
            menu.AddMenuOption(zoneName, (p, _) => {
                _activeZoneName = zoneName;
                _ffdSeconds = sec;
                DrawRGBBox(_ffZones[zoneName].Box);
                SpawnArrow(_ffZones[zoneName].ArrowPos.ToVector());
                Server.PrintToChatAll($"{Prefix} {ChatColors.Default}T Oyuncuları {ChatColors.Lime}{sec} {ChatColors.Default}Saniye İçinde Dondurulacak\nBölge : {ChatColors.Lime}{zoneName}");
            });
        }
        menu.Open(player!);
    }

    [ConsoleCommand("css_ffd0", "FFD timerini durdurur ve T oyuncularını çözer")]
    public void OnFFDStop(CCSPlayerController? player, CommandInfo info)
    {
        if (!CheckAdmin(player, "!ffd0")) return;
        _ffdSeconds = -1;
        UnfreezeTerrorists();
        ClearBeams();
        player!.PrintToChat($"{Prefix} {ChatColors.Default}FFD timeri durduruldu, T oyuncularının donu çözüldü.");
    }

    [ConsoleCommand("css_ffdzoneayar", "Bölge ayarlamayı başlatır (Ateş ederek)")]
    public void OnZoneAyar(CCSPlayerController? player, CommandInfo info)
    {
        if (!CheckAdmin(player, "!ffdzoneayar")) return;
        _setupStep[player!.Slot] = 1;
        _tempP1.Remove(player.Slot);
        _tempP2.Remove(player.Slot);
        _tempArrow.Remove(player.Slot);
        player.PrintToChat($"{Prefix} {ChatColors.Lime}Bölge Ayarlama Başlatıldı! {ChatColors.Default}Lütfen P1 noktası için bir yere ateş edin.");
    }

    [ConsoleCommand("css_ffdzoneayar0", "Bölge ayarlamayı durdurur")]
    public void OnZoneAyar0(CCSPlayerController? player, CommandInfo info)
    {
        if (!CheckAdmin(player, "!ffdzoneayar0")) return;
        _setupStep[player!.Slot] = 0;
        _tempP1.Remove(player.Slot);
        _tempP2.Remove(player.Slot);
        _tempArrow.Remove(player.Slot);
        player.PrintToChat($"{Prefix} {ChatColors.Red}Bölge ayarlama durduruldu ve noktalar temizlendi.");
    }

    [ConsoleCommand("css_arrowadd", "Durduğun yeri ok işareti olarak kaydeder")]
    public void OnArrowAdd(CCSPlayerController? player, CommandInfo info)
    {
        if (!CheckAdmin(player, "!arrowadd")) return;
        if (!_setupStep.TryGetValue(player!.Slot, out var step) || step < 3)
        {
            player.PrintToChat($"{Prefix} {ChatColors.Red}Önce ateş ederek P1 ve P2 noktalarını belirlemelisin!");
            return;
        }

        _tempArrow[player.Slot] = player.PlayerPawn.Value!.AbsOrigin!;
        _setupStep[player.Slot] = 4;
        player.PrintToChat($"{Prefix} {ChatColors.Lime}Ok İşareti Konumu Ayarlandı! {ChatColors.Default}Bölgeyi kaydetmek için: {ChatColors.Gold}!ffdzonekaydet <isim>");
    }

    [ConsoleCommand("css_ffdzonekaydet", "Bölgeyi kaydeder")]
    public void OnZoneKaydet(CCSPlayerController? player, CommandInfo info)
    {
        if (!CheckAdmin(player, "!ffdzonekaydet")) return;
        string name = info.GetArg(1);
        if (string.IsNullOrEmpty(name)) { player!.PrintToChat($"{Prefix} {ChatColors.Red}İsim girmelisin! !ffdzonekaydet <isim>"); return; }
        
        if (!_setupStep.TryGetValue(player!.Slot, out var step) || step < 4)
        {
            player.PrintToChat($"{Prefix} {ChatColors.Red}Önce P1, P2 ve Ok işaretini ayarlamalısın!");
            return;
        }

        _ffZones[name] = new FFZoneData { 
            Name = name, 
            Box = new ZoneBox { P1 = new Vector3D(_tempP1[player.Slot]), P2 = new Vector3D(_tempP2[player.Slot]) },
            ArrowPos = new Vector3D(_tempArrow[player.Slot])
        };
        SaveZones();
        _setupStep[player.Slot] = 0;
        player.PrintToChat($"{Prefix} {ChatColors.Lime}{name} {ChatColors.Default}bölgesi başarıyla kaydedildi!");
    }

    [ConsoleCommand("css_ffdzonesil", "Bölgeyi siler")]
    public void OnZoneSil(CCSPlayerController? player, CommandInfo info)
    {
        if (!CheckAdmin(player, "!ffdzonesil")) return;
        string name = info.GetArg(1);
        if (string.IsNullOrEmpty(name) || !_ffZones.Remove(name)) { player!.PrintToChat($"{Prefix} {ChatColors.Red}Bölge bulunamadı!"); return; }
        SaveZones();
        player!.PrintToChat($"{Prefix} {ChatColors.Red}{name} {ChatColors.Default}bölgesi silindi.");
    }

    private void SaveZones() => File.WriteAllText(ZonesPath, JsonSerializer.Serialize(_ffZones, new JsonSerializerOptions { WriteIndented = true }));

    private void DrawRGBBox(ZoneBox box)
    {
        ClearBeams();
        var p1 = box.P1.ToVector();
        var p2 = box.P2.ToVector();
        var minX = Math.Min(p1.X, p2.X); var maxX = Math.Max(p1.X, p2.X);
        var minY = Math.Min(p1.Y, p2.Y); var maxY = Math.Max(p1.Y, p2.Y);
        var minZ = Math.Min(p1.Z, p2.Z); var maxZ = Math.Max(p1.Z, p2.Z);
        
        Vector[] v = { 
            new(minX, minY, minZ), new(maxX, minY, minZ), new(maxX, maxY, minZ), new(minX, maxY, minZ),
            new(minX, minY, maxZ), new(maxX, minY, maxZ), new(maxX, maxY, maxZ), new(minX, maxY, maxZ) 
        };
        int[,] edges = { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 0 }, { 4, 5 }, { 5, 6 }, { 6, 7 }, { 7, 4 }, { 0, 4 }, { 1, 5 }, { 2, 6 }, { 3, 7 } };
        
        for (int i = 0; i < 12; i++) DrawBeam(v[edges[i, 0]], v[edges[i, 1]]);
    }

    private void DrawBeam(Vector start, Vector end)
    {
        CBeam? beam = Utilities.CreateEntityByName<CBeam>("beam");
        if (beam == null) return;
        beam.Render = Color.White;
        beam.Width = 2.0f;
        beam.SetModel("materials/sprites/laserbeam.vmat");
        beam.Teleport(start, QAngle.Zero, Vector.Zero);
        beam.EndPos.X = end.X; beam.EndPos.Y = end.Y; beam.EndPos.Z = end.Z;
        beam.DispatchSpawn();
        _activeBeams.Add(beam);
    }

    private void SpawnArrow(Vector pos)
    {
        _activeArrow?.Remove();
        var arrow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (arrow == null) return;

        arrow.SetModel("models/props/de_nuke/hr_nuke/nuke_signs/nuke_sign_arrow_down.vmdl");
        arrow.Teleport(pos + new Vector(0,0,100), new QAngle(0,0,0), Vector.Zero);
        arrow.DispatchSpawn();
        
        _activeArrow = arrow;
        _activeArrowBasePos = pos + new Vector(0,0,100);
    }

    private void ClearBeams() 
    { 
        foreach (var b in _activeBeams) if (b is { IsValid: true }) b.Remove(); 
        _activeBeams.Clear(); 
        if (_activeArrow != null && _activeArrow.IsValid) _activeArrow.Remove();
        _activeArrow = null;
        _activeArrowBasePos = null;
    }

    private static Color HSLToRGB(float h, float s, float l)
    {
        float r, g, b;
        if (s == 0) r = g = b = l;
        else {
            float q = l < 0.5f ? l * (1 + s) : l + s - l * s;
            float p = 2 * l - q;
            r = HueToRGB(p, q, h / 360f + 1f / 3f);
            g = HueToRGB(p, q, h / 360f);
            b = HueToRGB(p, q, h / 360f - 1f / 3f);
        }
        return Color.FromArgb(255, (int)(r * 255), (int)(g * 255), (int)(b * 255));
    }

    private static float HueToRGB(float p, float q, float t)
    {
        if (t < 0) t += 1; if (t > 1) t -= 1;
        if (t < 1f / 6f) return p + (q - p) * 6 * t;
        if (t < 1f / 2f) return q;
        if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6;
        return p;
    }

    private void OpenPrimaryMenuForAll()
    {
        _awaitingPrimary.Clear();
        _awaitingSecondary.Clear();
        foreach (var p in Utilities.GetPlayers())
        {
            if (!p.IsValid || p.IsBot || p.TeamNum != (int)CsTeam.Terrorist || !p.PawnIsAlive)
                continue;

            _awaitingPrimary.Add(p.Slot);
            OpenPrimaryMenu(p);
        }
    }

    private void OpenPrimaryMenu(CCSPlayerController player)
    {
        string title = $"Birincil Silahını Seç ({_ffMenuSeconds}s)";
        var menu = new CenterHtmlMenu(title, this);

        foreach (var (name, cls) in Primaries)
        {
            string capturedCls = cls;
            menu.AddMenuOption(name, (p, _) =>
            {
                if (_ffMenuSeconds <= 0) return;
                GiveWeapon(p, capturedCls, isPrimary: true);
                _awaitingPrimary.Remove(p.Slot);
                _awaitingSecondary.Add(p.Slot);
                OpenSecondaryMenu(p);
            });
        }

        menu.Open(player);
    }

    private void OpenSecondaryMenu(CCSPlayerController player)
    {
        string title = $"İkincil Silahını Seç ({_ffMenuSeconds}s)";
        var menu = new CenterHtmlMenu(title, this);

        foreach (var (name, cls) in Secondaries)
        {
            string capturedCls = cls;
            menu.AddMenuOption(name, (p, _) =>
            {
                if (_ffMenuSeconds <= 0) return;
                GiveWeapon(p, capturedCls, isPrimary: false);
                _awaitingSecondary.Remove(p.Slot);
                MenuManager.CloseActiveMenu(p);
                p.PrintToChat($"{Prefix} {ChatColors.Default}Silah seçimlerin tamamlandı!");
            });
        }

        menu.Open(player);
    }

    private static void GiveWeapon(CCSPlayerController player, string classname, bool isPrimary)
    {
        if (!player.IsValid || !player.PawnIsAlive) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn?.WeaponServices?.MyWeapons == null) return;

        foreach (var handle in pawn.WeaponServices.MyWeapons)
        {
            var w = handle.Value;
            if (w == null) continue;
            string designerName = w.DesignerName;
            bool wIsPrimary   = PrimaryClassnames.Contains(designerName);
            bool wIsSecondary = SecondaryClassnames.Contains(designerName);
            if ((isPrimary && wIsPrimary) || (!isPrimary && wIsSecondary))
            {
                w.Remove();
                break;
            }
        }

        player.GiveNamedItem(classname);
    }

    private void FreezeTerrorists()
    {
        foreach (var p in Utilities.GetPlayers())
        {
            if (!p.IsValid || p.IsBot || p.TeamNum != (int)CsTeam.Terrorist || !p.PawnIsAlive) continue;
            var pawn = p.PlayerPawn.Value;
            if (pawn == null) continue;
            pawn.Teleport(null, null, Vector.Zero);
            ChangeMovetype(pawn, MoveType_t.MOVETYPE_NONE, Color.SkyBlue);
        }
    }

    private void UnfreezeTerrorists()
    {
        foreach (var p in Utilities.GetPlayers())
        {
            if (!p.IsValid || p.IsBot || p.TeamNum != (int)CsTeam.Terrorist || !p.PawnIsAlive) continue;
            var pawn = p.PlayerPawn.Value;
            if (pawn == null) continue;
            pawn.Teleport(null, null, Vector.Zero);
            ChangeMovetype(pawn, MoveType_t.MOVETYPE_WALK, Color.White);
        }
    }

    private static void ChangeMovetype(CBasePlayerPawn pawn, MoveType_t movetype, Color glow)
    {
        pawn.MoveType = movetype;
        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (int)movetype);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        pawn.RenderMode = (RenderMode_t)1;
        pawn.Render     = glow;
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    private static bool CheckAdmin(CCSPlayerController? player, string cmd)
    {
        if (player == null) return false;
        if (!AdminManager.PlayerHasPermissions(player, "@css/generic"))
        {
            player.PrintToChat($"{Prefix} {ChatColors.Red}Yetkiniz Yok! (Gereken: @css/generic)");
            return false;
        }
        return true;
    }
}

public class Vector3D
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public Vector3D() { }
    public Vector3D(Vector v) { X = v.X; Y = v.Y; Z = v.Z; }
    public Vector ToVector() => new Vector(X, Y, Z);
}

public class ZoneBox
{
    public Vector3D P1 { get; set; } = new();
    public Vector3D P2 { get; set; } = new();
    public bool IsInside(Vector pos)
    {
        return pos.X >= Math.Min(P1.X, P2.X) && pos.X <= Math.Max(P1.X, P2.X) &&
               pos.Y >= Math.Min(P1.Y, P2.Y) && pos.Y <= Math.Max(P1.Y, P2.Y) &&
               pos.Z >= Math.Min(P1.Z, P2.Z) && pos.Z <= Math.Max(P1.Z, P2.Z);
    }
}

public class FFZoneData
{
    public string Name { get; set; } = "";
    public ZoneBox Box { get; set; } = new();
    public Vector3D ArrowPos { get; set; } = new();
}
