using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Perks en los demas: los enemigos que llevan una perk encima (en el inventario) tienen su efecto, y los companeros de
    //  PIT Fireteam las que les equipes en sus 4 casillas (PitPerks; las pierden si mueren). F12: "Efectos de perks en
    //  enemigos y companeros".
    //   Juggernog: reciben la mitad de dano | Speed Cola: disparan un 30% mas rapido | Stamin-Up: estamina infinita e inmunes al dolor
    //   Widow's Wine: sus disparos frenan a quien aciertan | PHD Flopper: inmunes a explosiones y caidas
    //   Bandolier: aparecen con municion extra | Vulture Aid: aparecen con un objeto valioso | Melee Macchiato: estamina infinita y
    //   golpes cuerpo a cuerpo x2 | Deadshot: saben donde estan sus enemigos aunque haya paredes (hasta 80 m)
    //  (Los Embotelladores de las misiones tienen sus propias reglas en QuestGuards.)
    // ============================================================================================
    internal static class BotPerks
    {
        internal const float JugTaken = 0.5f;
        internal const float ReloadMult = 1.5f;          // Speed Cola: recarga x1.5
        internal const float Penetration = 1.3f;         // Deadshot: +30% de penetracion
        internal const float WebRadius = 8f, WebLeft2 = 0.4f, WebSecs2 = 4f, WebContusion = 3f;   // granadas de bots con Widow's Wine
        internal const float CherryRadius = 3f, CherryDamage = 25f, CherryContusion = 3f, CherryCooldown = 10f;
        internal const float TombFuse = 3f;              // Tombstone: granada al morir (F-1), explota a los 3 s
        internal const string TombGrenade = "5710c24ad2720bc3458b45a3";
        internal const float WebLeft = 0.6f, WebSeconds = 3f, WebCooldown = 4f;
        internal const float MeleeMult = 2f;
        internal const float DeadshotRange = 80f, DeadshotEvery = 2f;
        internal const int MaxFollowerPerks = 4;
        private const float Rescan = 15f;

        // objetos valiosos con los que puede aparecer un enemigo con Vulture Aid
        private static readonly string[] Valuables =
        {
            "5c0530ee86f774697952d952" /* LEDX */, "57347ca924597744596b4e71" /* tarjeta grafica */, "59faff1d86f7746c51718c9c" /* bitcoin */,
            "5bc9bc53d4351e00367fbcee" /* gallo de oro */, "5c94bbff86f7747ee735c08f" /* tarjeta de Labs */,
        };

        private class Info
        {
            public Player P; public HashSet<string> Perks = new HashSet<string>(); public float NextScan; public bool Bonus; public bool Follower; public bool Dead;
            public bool Guard; public bool DeathDone; public bool WasReloading; public float NextCherry; public string AmmoTpl;
            public HashSet<string> AmmoAtSpawn; public HashSet<string> Provokers = new HashSet<string>(); public float NextHunt; public bool Exposed;
        }
        private static readonly Dictionary<object, Info> _byObj = new Dictionary<object, Info>();          // SkillManager / Physical -> perks
        internal static bool HasObj(object o, string perk) { Info i; return o != null && _byObj.TryGetValue(o, out i) && i.Perks.Contains(perk); }
        private static readonly Dictionary<string, Info> _byId = new Dictionary<string, Info>();
        private static readonly Dictionary<object, Info> _byInv = new Dictionary<object, Info>();
        private static readonly Dictionary<string, float> _nextWeb = new Dictionary<string, float>();
        private static float _next, _nextSight;
        private static string _raidKey;

        internal static bool Any { get { return _byId.Count > 0; } }
        internal static bool Has(Player p, string perk) { Info i; return p != null && _byId.TryGetValue(p.ProfileId, out i) && i.Perks.Contains(perk); }
        internal static bool Has(string profileId, string perk) { Info i; return profileId != null && _byId.TryGetValue(profileId, out i) && i.Perks.Contains(perk); }

        // ---------------------------------------------------------------- PIT Fireteam (opcional: se busca por reflexion)
        private static bool _pitLooked;
        private static MethodInfo _isFollower;
        internal static bool IsFollower(Player p)
        {
            if (!_pitLooked)
            {
                _pitLooked = true;
                try { _isFollower = AccessTools.Method(AccessTools.TypeByName("pitTeam.Modules.BossPlayers"), "IsFollowerProfileId"); } catch { }
                Plugin.Log.LogInfo("PIT Fireteam " + (_isFollower != null ? "encontrado: sus companeros pueden llevar perks" : "no esta instalado"));
            }
            if (_isFollower == null) return false;
            try { return (bool)_isFollower.Invoke(null, new object[] { p.ProfileId }); } catch { return false; }
        }

        // ---------------------------------------------------------------- 2 veces por segundo (Plugin.Update)
        internal static void Tick()
        {
            bool all = Plugin.CfgBotPerks.Value;
            if (!Plugin.InRaid || ArenaCompat.InArena || (!all && !QuestGuards.Any)) { if (_byId.Count > 0) Clear(); return; }
            float now = Time.time;
            if (now < _next) return;
            _next = now + 0.5f;
            var me = Plugin.Me;
            var world = Singleton<GameWorld>.Instance;
            if (me == null || world == null || world.AllAlivePlayersList == null) return;
            string key = world.LocationId + "|" + me.ProfileId + "|" + world.GetHashCode();
            if (key != _raidKey) { _raidKey = key; Clear(); }

            // muertos: los companeros pierden sus perks; los enemigos sueltan lo suyo (Tombstone, Bandolier...)
            foreach (var i in _byId.Values)
            {
                if (i.P == null || i.P.HealthController == null || i.P.HealthController.IsAlive) continue;
                if (!i.Dead && i.Follower) { i.Dead = true; PitPerks.OnDeath(i.P); }
                if (!i.DeathDone && !i.Follower && i.Perks.Count > 0) { i.DeathDone = true; try { OnBotDeath(i); } catch (Exception e) { Dbg.Throttled("botdeath", 10f, "ERROR", "perks en bots (muerte): " + e.Message); } }
            }
            TickGrenades(me, now);
            BotRevive.Tick(me);

            bool sight = now >= _nextSight;
            if (sight) _nextSight = now + DeadshotEvery;
            foreach (var p in world.AllAlivePlayersList)
            {
                if (p == null || p.IsYourPlayer || p.HealthController == null || !p.HealthController.IsAlive) continue;
                if (!all && !QuestGuards.IsGuard(p)) continue;
                Info info;
                if (!_byId.TryGetValue(p.ProfileId, out info)) { info = new Info { P = p }; _byId[p.ProfileId] = info; }
                if (now >= info.NextScan) { info.NextScan = now + Rescan; Scan(info); }
                if (info.Perks.Count == 0) continue;
                try { Apply(info, sight); }
                catch (Exception e) { Dbg.Throttled("botperks", 10f, "ERROR", "perks en bots: " + e.Message); }
            }
        }

        private static void Clear() { BotRevive.Clear(); _byId.Clear(); _byInv.Clear(); _byObj.Clear(); _nextWeb.Clear(); _grenades.Clear(); _bombs.Clear(); }

        // al cambiar sus casillas: se vuelve a mirar en el siguiente tick
        internal static void ScanSoon(string profileId)
        {
            Info i;
            if (profileId != null && _byId.TryGetValue(profileId, out i)) i.NextScan = 0f;
        }

        // Que perks lleva (los companeros: las de sus casillas)
        private static void Scan(Info info)
        {
            var p = info.P;
            info.Perks.Clear();
            var slots = PitPerks.Of(p.ProfileId);
            info.Follower = slots.Count > 0 || IsFollower(p);
            if (info.Follower) foreach (var id in slots.Take(MaxFollowerPerks)) info.Perks.Add(id);
            info.Guard = QuestGuards.IsGuard(p);
            foreach (var id in QuestGuards.PerksOf(p.ProfileId)) info.Perks.Add(id);         // los Embotelladores: sus perks de mision
            if (BotRevive.IsRevived(p.ProfileId)) info.Perks.Add(DeathPerception.Id);
            if (!info.Follower) try
            {
                foreach (var it in p.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment))
                {
                    var def = Perks.ByItem(it?.TemplateId.ToString());
                    if (def != null) info.Perks.Add(def.Id);
                }
            }
            catch { }
            if (BotRevive.IsRevived(p.ProfileId)) info.Perks.Remove(QuickRevive.Id);
            if (info.Perks.Count > 0)
            {
                if (p.InventoryController != null) _byInv[p.InventoryController] = info;
                if (p.Skills != null) _byObj[p.Skills] = info;
                if (p.Physical != null) _byObj[p.Physical] = info;
                Dbg.Log("BOTPERKS", (info.Follower ? "companero " : "") + p.Profile?.Info?.Settings?.Role + " " + p.Profile?.Nickname + ": " + string.Join(", ", info.Perks.ToArray()));
            }
            // al aparecer: municion extra (Bandolier) y un objeto valioso (Vulture Aid). Solo enemigos: los companeros conservan su equipo entre raids.
            if (!info.Bonus && info.Perks.Count > 0 && !info.Follower)
            {
                info.Bonus = true;
                if (info.Perks.Contains(Bandolier.Id)) ExtraAmmo(info);
                if (info.Perks.Contains(MuleKick.Id)) MuleKick.GiveBotWeapon(p);
                if (info.Perks.Contains(VultureAid.Id)) VultureGear(p);
            }
        }

        // Vulture Aid: mejor equipo. 2 objetos valiosos y, si no lleva, un 70% de llevar una 2.a arma (preset completo, con cargador lleno)
        private static readonly string[] VultureGuns =
        {
            "5447a9cd4bdc2dbd208b4567" /* M4A1 */, "5bb2475ed4351e00853264e3" /* HK 416A5 */, "5ac66d2e5acfc43b321d4b53" /* AK-103 */,
            "5a367e5dc4a282000e49738f" /* RSASS */, "5fc22d7c187fea44d52eda44" /* MK-18 */, "6183afd850224f204c1da514" /* SCAR-H */,
        };
        private static void VultureGear(Player p)
        {
            for (int i = 0; i < 2; i++) NewPerkUtil.Give(p.Profile, Valuables[UnityEngine.Random.Range(0, Valuables.Length)]);
            if (UnityEngine.Random.value > 0.7f) return;
            try
            {
                var slot = p.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.SecondPrimaryWeapon);
                if (slot == null || slot.ContainedItem != null) return;
                var w = Singleton<ItemFactory>.Instance.GetPresetItem(VultureGuns[UnityEngine.Random.Range(0, VultureGuns.Length)]) as Weapon;
                if (w == null) return;
                var mag = w.GetCurrentMagazine();
                var ammoTpl = w.Template.DefAmmoTemplate;
                if (mag != null && ammoTpl != null && !(mag is CylinderMagazine) && mag.Count < mag.MaxCount)
                {
                    var ammo = Singleton<ItemFactory>.Instance.CreateItem(Guid.NewGuid().ToString("N").Substring(0, 24), ammoTpl._id, null);
                    if (ammo != null) { ammo.StackObjectsCount = mag.MaxCount - mag.Count; mag.Cartridges.Add(ammo, false); }
                }
                if (slot.AddWithoutRestrictions(w).Succeeded) Dbg.Log("BOTS", p.Profile.Nickname + " (Vulture Aid): 2.a arma " + w.ShortName.Localized());
            }
            catch (Exception e) { Dbg.Throttled("vulturebot", 10f, "ERROR", "Vulture Aid (bot): " + e.Message); }
        }

        // Bandolier: municion infinita mientras vive (se le repone la de reserva); al morir se le quita lo repuesto, solo se lootea lo suyo
        private static void ExtraAmmo(Info info)
        {
            var p = info.P;
            var w = p.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem as Weapon
                    ?? p.Profile.Inventory.Equipment.GetSlot(EquipmentSlot.Holster).ContainedItem as Weapon;
            var top = w != null && w.GetCurrentMagazine() != null ? w.GetCurrentMagazine().Cartridges.Last : null;
            if (top == null) return;
            info.AmmoTpl = top.TemplateId.ToString();
            info.AmmoAtSpawn = new HashSet<string>(LooseAmmo(p, info.AmmoTpl).Select(a => a.Id));
        }

        private static IEnumerable<Ammo> LooseAmmo(Player p, string tpl)
        {
            return p.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment).OfType<Ammo>()
                .Where(a => a.TemplateId.ToString() == tpl && a.Parent != null && a.Parent.Container != null && !(a.Parent.Container.ParentItem is Magazine) && !(a.Parent.Container.ParentItem is Weapon));
        }

        private static void TopUpAmmo(Info info)
        {
            if (info.AmmoTpl == null) return;
            int spare = LooseAmmo(info.P, info.AmmoTpl).Sum(a => a.StackObjectsCount);
            if (spare < 60) NewPerkUtil.Give(info.P.Profile, info.AmmoTpl, 60);
        }

        // ---------------------------------------------------------------- al morir un bot con perks
        private class Bomb { public Vector3 Pos; public float At; public string Owner; }
        private static readonly List<Bomb> _bombs = new List<Bomb>();

        private static void OnBotDeath(Info info)
        {
            var p = info.P;
            // Bandolier: se le quita la municion repuesta (la que no llevaba al aparecer)
            if (info.AmmoTpl != null && info.AmmoAtSpawn != null)
                foreach (var a in LooseAmmo(p, info.AmmoTpl).Where(a => !info.AmmoAtSpawn.Contains(a.Id)).ToList())
                {
                    foreach (var slot in new[] { EquipmentSlot.Backpack, EquipmentSlot.TacticalVest, EquipmentSlot.Pockets })
                    {
                        var c = p.Profile.Inventory.Equipment.GetSlot(slot).ContainedItem as CompoundItem;
                        if (c == null) continue;
                        foreach (var grid in c.Grids) if (grid.Items.Contains(a)) grid.RemoveWithoutRestrictions(a);
                    }
                }
            // Tombstone: suelta una granada al caer (explota a los 3 s: se puede escapar)
            if (info.Perks.Contains(Tombstone.Id))
            {
                _bombs.Add(new Bomb { Pos = p.Position + Vector3.up * 0.2f, At = Time.time + TombFuse, Owner = p.ProfileId });
                var me = Plugin.Me;
                if (me != null && (me.Position - p.Position).sqrMagnitude < 15f * 15f) Hud.Notify(L.T("perk.tombstone").ToUpperInvariant(), L.T("notice.bottomb"), Tombstone.Id);
                Dbg.Log("BOTS", p.Profile.Nickname + " (Tombstone): granada al morir");
            }
        }

        private static void Explode(Bomb b)
        {
            try
            {
                var w = Singleton<ItemFactory>.Instance.CreateItem(Guid.NewGuid().ToString("N").Substring(0, 24), TombGrenade, null) as ThrowWeap;
                if (w == null) return;
                if (!string.IsNullOrEmpty(w.ExplosionEffectType)) Singleton<Systems.Effects.Effects>.Instance.EmitGrenade(w.ExplosionEffectType, b.Pos, Vector3.up);
                var calc = Singleton<IGameLevel>.Instance != null ? Singleton<IGameLevel>.Instance.CreateBallisticCalculator(0) : null;
                Grenade.Explosion(null, w, b.Pos, b.Owner, calc, w, new Vector3(0f, 0.08f, 0f));
            }
            catch (Exception e) { Dbg.Throttled("tombbot", 10f, "ERROR", "Tombstone (granada de bot): " + e.Message); }
        }

        // ---------------------------------------------------------------- granadas: Tombstone y Widow's Wine de los bots
        private static readonly Dictionary<Grenade, bool> _grenades = new Dictionary<Grenade, bool>();    // granada de bot con Widow's Wine -> ya te atrapo
        internal static void OnGrenadeThrown(Grenade g, string profileId)
        {
            Info i;
            if (g == null || g is SmokeGrenade || profileId == null || !_byId.TryGetValue(profileId, out i) || !i.Perks.Contains(WidowsWine.Id)) return;
            _grenades[g] = false;
        }

        private static void TickGrenades(Player me, float now)
        {
            for (int k = _bombs.Count - 1; k >= 0; k--)
                if (now >= _bombs[k].At) { var b = _bombs[k]; _bombs.RemoveAt(k); Explode(b); }
            if (_grenades.Count == 0) return;
            foreach (var g in _grenades.Keys.ToList())
            {
                if (g == null) { _grenades.Remove(g); continue; }
                if (_grenades[g] || (g.transform.position - me.Position).sqrMagnitude > WebRadius * WebRadius) continue;
                _grenades[g] = true;
                // como la del jugador: te frena y te aturde, salvo que tengas PHD Flopper o Widow's Wine
                if (Perks.IsActive(PhdFlopper.Id) || Perks.IsActive(WidowsWine.Id)) { Dbg.Log("BOTS", "telarana de un bot: inmune (PHD/Widow's)"); continue; }
                var mc = me.MovementContext;
                if (mc != null) { mc.ChangeSpeedLimit(WebLeft2 * mc.MaxSpeed, Player.ESpeedLimit.Swamp, WebSecs2); WidowsWine.Slow(mc, WebLeft2, WebSecs2); }
                Contuse(me, WebContusion);
                WebVfx.OnVictim(me, WebSecs2);
                Hud.Notify(L.T("perk.widowswine").ToUpperInvariant(), L.T("notice.botweb"), WidowsWine.Id);
            }
        }

        internal static void Contuse(Player p, float secs)
        {
            try { p.ActiveHealthController?.AddEffect<ActiveHealthController.Contusion>(EBodyPart.Head, 0f, secs, 0.5f, 1f); }
            catch (Exception e) { Dbg.Throttled("contusion", 10f, "ERROR", "contusion: " + e.Message); }
        }

        private static void Apply(Info info, bool sight)
        {
            var p = info.P;
            var phys = p.Physical;
            FinalPerks.BotTick(p, info.Perks);                       // perks finales (Quick Revive...)
            if (info.Perks.Contains(Bandolier.Id)) TopUpAmmo(info);
            // Electric Cherry: al empezar a recargar, descarga a menos de 3 m (dano y contusion; una vez cada 10 s)
            var fcr = p.HandsController as Player.FirearmController;
            bool reloading = fcr != null && fcr.IsInReloadOperation();
            if (reloading && !info.WasReloading && info.Perks.Contains(ElectricCherry.Id) && Time.time >= info.NextCherry) Cherry(info);
            info.WasReloading = reloading;
            // Stamin-Up y Melee Macchiato: estamina infinita; Stamin-Up ademas inmune al dolor
            if ((info.Perks.Contains(StaminUp.Id) || info.Perks.Contains(MeleeMacchiato.Id)) && phys != null && phys.Stamina != null)
                phys.Stamina.Current = phys.Stamina.TotalCapacity;
            if (info.Perks.Contains(StaminUp.Id) && p.ActiveHealthController != null && !p.ActiveHealthController.FindActiveEffects<ActiveHealthController.PainKiller>(EBodyPart.Head).Any())
                p.ActiveHealthController.AddEffect<ActiveHealthController.PainKiller>(EBodyPart.Head, 0f, 3600f, 0f);

            var bot = p.AIData != null ? p.AIData.BotOwner : null;
            if (info.Perks.Contains(DeathPerception.Id) && bot != null && bot.BotsGroup != null) Hunt(info, bot);
            // Deadshot: sabe donde estan sus enemigos cercanos aunque no los vea
            if (sight && info.Perks.Contains(Deadshot.Id) && bot != null && bot.BotsGroup != null)
            {
                var world = Singleton<GameWorld>.Instance;
                foreach (var t in world.AllAlivePlayersList)
                {
                    if (t == null || t == p || (t.Position - p.Position).sqrMagnitude > DeadshotRange * DeadshotRange || !bot.BotsGroup.IsEnemy(t)) continue;
                    bot.BotsGroup.SetEnemyPos(t, t.Position, t.WeaponRoot != null ? t.WeaponRoot.position : t.Position, EEnemyPartVisibleType.Sence);
                }
            }
        }

        private static void Cherry(Info info)
        {
            info.NextCherry = Time.time + CherryCooldown;
            var me = Plugin.Me;
            ElectricVfx.Burst(info.P.Position, CherryRadius);
            if (me == null || (me.Position - info.P.Position).sqrMagnitude > CherryRadius * CherryRadius) return;
            var hc = me.ActiveHealthController;
            if (hc == null) return;
            hc.ChangeHealth(EBodyPart.Chest, -CherryDamage * 0.5f, DamageHelper.UndefinedDamage);
            hc.ChangeHealth(EBodyPart.Stomach, -CherryDamage * 0.5f, DamageHelper.UndefinedDamage);
            Contuse(me, CherryContusion);
            Hud.Notify(L.T("perk.electriccherry").ToUpperInvariant(), L.T("notice.botcherry"), ElectricCherry.Id);
            Dbg.Log("BOTS", info.P.Profile.Nickname + " (Electric Cherry): descarga al recargar");
        }

        // Death Perception: solo van a por ti; a otros enemigos solo se defienden si les atacan primero
        private static void Hunt(Info info, BotOwner bot)
        {
            var me = Plugin.Me;
            if (me == null) return;
            foreach (var e in bot.BotsGroup.Enemies.Keys.ToList())
                if (e != null && !e.IsYourPlayer && !info.Provokers.Contains(e.ProfileId)) bot.BotsGroup.RemoveEnemy(e);
            if (!bot.BotsGroup.IsEnemy(me)) bot.BotsGroup.AddEnemy(me, EBotEnemyCause.initial);
            if (Time.time >= info.NextHunt)
            {
                info.NextHunt = Time.time + 10f;                    // cada 10 s sabe mas o menos por donde andas
                var guess = me.Position + new Vector3(UnityEngine.Random.Range(-8f, 8f), 0f, UnityEngine.Random.Range(-8f, 8f));
                bot.BotsGroup.SetEnemyPos(me, guess, guess + Vector3.up * 1.5f, EEnemyPartVisibleType.Sence);
            }
        }

        // ---------------------------------------------------------------- iconos de sus perks sobre la cabeza (como los Embotelladores)
        // Solo a la vista (sin paredes en medio) y a menos de 60 m. Los Embotelladores los dibuja QuestGuards; los companeros no llevan.
        private const float IconRange = 60f;
        internal static void DrawIcons(float scale)
        {
            if (_byId.Count == 0 || Plugin.CfgBotPerkIcons == null || !Plugin.CfgBotPerkIcons.Value) return;
            var cam = Powerup.Cam();
            if (cam == null) return;
            var camPos = cam.transform.position;
            foreach (var info in _byId.Values)
            {
                var p = info.P;
                if (p == null || info.Follower || info.Guard || info.Perks.Count == 0 || p.HealthController == null || !p.HealthController.IsAlive) continue;
                var head = p.Position + Vector3.up * (p.MovementContext != null ? 1.4f + 0.6f * Mathf.Clamp01(p.MovementContext.PoseLevel) : 2f);
                float dist = Vector3.Distance(camPos, head);
                if (dist > IconRange || Vector3.Dot(cam.transform.forward, head - camPos) <= 0f) continue;
                if (Physics.Linecast(camPos, head, LayersMaskController.HighPolyWithTerrainMask)) continue;
                var vp = cam.WorldToViewportPoint(head + Vector3.up * 0.25f);
                if (vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f) continue;
                float size = Mathf.Lerp(24f, 14f, dist / IconRange) * scale;
                int n = info.Perks.Count;
                float total = n * size + (n - 1) * 2f * scale;
                float x = vp.x * Screen.width - total / 2f, y = (1f - vp.y) * Screen.height - size;
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(1.2f - dist / IconRange));
                int i = 0;
                foreach (var id in info.Perks)
                {
                    var tex = Hud.IconTex(id);
                    if (tex != null) GUI.DrawTexture(new Rect(x + i * (size + 2f * scale), y, size, size), tex, ScaleMode.ScaleToFit, true);
                    i++;
                }
            }
            GUI.color = Color.white;
        }

        // ---------------------------------------------------------------- dano (desde DamageHubPatch, para cualquier victima)
        internal static float OnDamage(Player victim, EBodyPart part, float damage, DamageInfo info)
        {
            BotRevive.NoteCombat(victim.Position);
            // la victima
            Info v;
            if (_byId.TryGetValue(victim.ProfileId, out v) && v.Perks.Count > 0)
            {
                var t = info.DamageType;
                if (v.Perks.Contains(PhdFlopper.Id) && (t & (PerkFx.Blast | EDamageType.Fall)) != 0) return 0f;
                var att = info.Player != null ? info.Player.iPlayer : null;
                // Embotelladores: inmunes a tu cuerpo a cuerpo salvo que tengas Melee Macchiato
                if (v.Guard && (t & EDamageType.Melee) != 0 && att != null && att.IsYourPlayer && !Perks.IsActive(MeleeMacchiato.Id)) return 0f;
                if (att != null && !att.IsYourPlayer) v.Provokers.Add(att.ProfileId);           // Death Perception: se defiende de quien le ataca
                if (v.Perks.Contains(Juggernog.Id) && Juggernog.Reduces(t)) damage *= JugTaken;
                if (v.Perks.Contains(MeleeMacchiato.Id) && !v.Guard) damage = MacchiatoTaken(v, part, damage, (t & EDamageType.Melee) != 0 && att != null && att.IsYourPlayer);
                if (v.Perks.Contains(QuickRevive.Id) && (t & PerkFx.Hits) != 0) QuickRevive.BotDamaged(victim);
                if (v.Perks.Contains(QuickRevive.Id) || BotRevive.IsDowned(victim.ProfileId)) damage = BotRevive.OnDamage(victim, part, damage);   // caido en vez de muerto
            }
            // quien ataca
            var by = info.Player != null ? info.Player.iPlayer : null;
            Info a;
            if (by == null || !_byId.TryGetValue(by.ProfileId, out a) || a.Perks.Count == 0) return damage;
            if ((info.DamageType & EDamageType.Melee) != 0 && a.Perks.Contains(MeleeMacchiato.Id)) damage *= MeleeMult;
            return damage;
        }

        // Melee Macchiato (bot normal): -80% de dano hasta que le pegues cuerpo a cuerpo; y la cabeza / el torax no lo matan
        // de golpe: hay que vaciarle la vida del resto del cuerpo antes
        internal const float MacchiatoArmor = 0.2f;
        private static float MacchiatoTaken(Info v, EBodyPart part, float damage, bool myMelee)
        {
            if (myMelee && !v.Exposed)
            {
                v.Exposed = true;
                Hud.Notify(L.T("perk.meleemacchiato").ToUpperInvariant(), L.T("notice.botexposed"), MeleeMacchiato.Id);
                Dbg.Log("BOTS", v.P.Profile.Nickname + " (Melee Macchiato): sin proteccion");
            }
            if (!v.Exposed) damage *= MacchiatoArmor;
            if (part != EBodyPart.Head && part != EBodyPart.Chest) return damage;
            var hc = v.P.ActiveHealthController;
            if (hc == null) return damage;
            float cur = hc.GetBodyPartHealth(part).Current;
            if (damage < cur) return damage;
            // lo que sobra va al resto del cuerpo; solo muere cuando ya no queda vida en otra parte
            float extra = damage - Mathf.Max(0f, cur - 1f);
            foreach (var other in new[] { EBodyPart.Stomach, EBodyPart.LeftArm, EBodyPart.RightArm, EBodyPart.LeftLeg, EBodyPart.RightLeg, EBodyPart.Chest, EBodyPart.Head })
            {
                if (other == part || extra <= 0f || hc.IsBodyPartDestroyed(other)) continue;
                float h = hc.GetBodyPartHealth(other).Current - (other == EBodyPart.Head || other == EBodyPart.Chest ? 1f : 0f);
                if (h <= 0f) continue;
                float take = Mathf.Min(h, extra);
                hc.ChangeHealth(other, -take, DamageHelper.UndefinedDamage);
                extra -= take;
            }
            return extra > 0f ? damage : Mathf.Max(0f, cur - 1f);
        }

        private static void Web(Player victim)
        {
            float until;
            if (_nextWeb.TryGetValue(victim.ProfileId, out until) && Time.time < until) return;
            _nextWeb[victim.ProfileId] = Time.time + WebCooldown;
            var mc = victim.MovementContext;
            if (mc != null) mc.ChangeSpeedLimit(WebLeft * mc.MaxSpeed, Player.ESpeedLimit.Swamp, WebSeconds);
            WebVfx.OnVictim(victim, WebSeconds);
            if (victim.IsYourPlayer) Hud.Notify(L.T("perk.widowswine").ToUpperInvariant(), L.T("notice.botweb"), WidowsWine.Id);
        }

        // Speed Cola: cadencia de las armas de quien la lleva
        internal static float FireRateMult(Weapon w)
        {
            Info i;
            if (w == null || w.Owner == null || !_byInv.TryGetValue(w.Owner, out i)) return 1f;
            float m = i.Perks.Contains(DoubleTap.Id) ? DoubleTap.BotRate : 1f;     // Double Tap: +25%
            return m;
        }
    }

    [HarmonyPatch]
    internal static class BotFireRatePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertyGetter(typeof(Weapon), nameof(Weapon.FireRate));
            yield return AccessTools.PropertyGetter(typeof(Weapon), nameof(Weapon.SingleFireRate));
        }

        [HarmonyPostfix]
        private static void Postfix(Weapon __instance, ref int __result)
        {
            if (!BotPerks.Any) return;
            float m = BotPerks.FireRateMult(__instance);
            if (m != 1f) __result = Mathf.RoundToInt(__result * m);
        }
    }
}
