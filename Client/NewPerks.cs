using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.Interactive;
using EFT.InventoryLogic;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Utilidades comunes de las perks nuevas
    internal static class NewPerkUtil
    {
        internal const string GpCoin = "5d235b4d86f7742e017bc88a";

        internal static bool IsPmc(Player p)
        {
            var side = p != null && p.Profile != null && p.Profile.Info != null ? p.Profile.Info.Side : EPlayerSide.Savage;
            return side == EPlayerSide.Usec || side == EPlayerSide.Bear;
        }

        internal static bool InsideAny(Item item, ICollection<Item> roots)
        {
            var a = item.Parent;
            for (int depth = 0; depth < 12 && a != null && a.Container != null; depth++)      // tope: la raiz del equipo puede apuntarse a si misma
            {
                var parent = a.Container.ParentItem;
                if (parent == null || ReferenceEquals(parent, item)) return false;
                if (roots.Contains(parent)) return true;
                a = parent.Parent;
            }
            return false;
        }

        // Crea un objeto nuevo y lo mete en el equipo de un jugador (vivo o cuerpo): mochila, chaleco o bolsillos, donde quepa
        internal static bool Give(Profile profile, string tpl, int count = 1)
        {
            try
            {
                var item = Singleton<ItemFactory>.Instance.CreateItem(Guid.NewGuid().ToString("N").Substring(0, 24), tpl, null);
                if (item == null) return false;
                if (count > 1) item.StackObjectsCount = Math.Min(count, item.StackMaxSize);
                var equipment = profile.Inventory.Equipment;
                foreach (var slot in new[] { EquipmentSlot.Backpack, EquipmentSlot.TacticalVest, EquipmentSlot.Pockets })
                {
                    var container = equipment.GetSlot(slot).ContainedItem as CompoundItem;
                    if (container == null) continue;
                    foreach (var grid in container.Grids)
                    {
                        var address = grid.FindLocationForItem(item);
                        if (address == null) continue;
                        if (grid.AddItemWithoutRestrictions(item, address.LocationInGrid).Succeeded) return true;
                    }
                }
            }
            catch (Exception e) { Plugin.Log.LogWarning("No se pudo crear " + tpl + ": " + e.Message); }
            return false;
        }

        // Pasa 'n' balas de un monton suelto a un cargador sin la animacion de cargar (una operacion de inventario normal)
        internal static bool MoveRounds(Player me, Ammo ammo, Magazine mag, int n)
        {
            var ctrl = me.InventoryController;
            n = Math.Min(n, Math.Min(ammo.StackObjectsCount, mag.MaxCount - mag.Count));
            if (n <= 0) return false;
            var top = mag.Cartridges.Last as Ammo;
            if (top != null && top.TemplateId == ammo.TemplateId && top.StackObjectsCount + n <= top.StackMaxSize)
            {
                var op = ItemManipulator.TransferExact(ammo, n, top, ctrl, true);
                if (op.Failed) return false;
                ctrl.TryRunNetworkTransaction(op);
                return true;
            }
            var split = ItemManipulator.SplitExact(ammo, n, mag.Cartridges.CreateItemAddress(), ctrl, ctrl, true);
            if (split.Failed) return false;
            ctrl.TryRunNetworkTransaction(split);
            return true;
        }

        // municion suelta (no metida en un cargador ni en un arma) que encaje en el cargador
        internal static Ammo LooseAmmoFor(IEnumerable<Item> items, Magazine mag)
        {
            return items.OfType<Ammo>().FirstOrDefault(a => a.Parent != null && a.Parent.Container != null
                && !(a.Parent.Container.ParentItem is Magazine) && !(a.Parent.Container.ParentItem is Weapon)
                && mag.Cartridges.CheckCompatibility(a));
        }

        internal static int Price(Item it) { return it.Template.CreditsPrice * Math.Max(1, it.StackObjectsCount); }

        internal static int ProfileValue(Player p)
        {
            int v = 0;
            foreach (var it in p.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment)) v += Price(it);
            return v;
        }

        // Mientras la estamina de brazos sube, se le quita una parte de lo recuperado (Strap Burn, Weak Grip)
        private static float _prevHands = -1f;
        internal static void SlowArmRecovery(Player me, float keep)
        {
            var hs = me.Physical != null ? me.Physical.HandsStamina : null;
            if (hs == null) return;
            if (keep < 1f && _prevHands >= 0f && hs.Current > _prevHands) hs.Current = _prevHands + (hs.Current - _prevHands) * keep;
            _prevHands = hs.Current;
        }
        internal static void Reset() { _prevHands = -1f; }
    }

    // ===================================================================================================================
    // Bandolier Bandit: municion.
    //   Base: los cargadores del chaleco y los bolsillos se rellenan solos (1 bala cada 2 s) con la municion suelta, mientras no disparas.
    //   Mayores: Sleek (20%: al vaciar el cargador se rellena solo), Discipline (dolor y brazos heridos no afectan al control),
    //            Bring it on! (municion y cargadores no pesan).
    //   Menores: Extra Pouch (10% de granada al matar), Cold Barrel (el arma se calienta un 30% menos), Ammo Leech (matar de un tiro
    //            a la cabeza devuelve 2 balas), Padded Strap (las armas equipadas pesan la mitad). Especial: Extra Slot.
    //   Desventajas ligadas: Loose Rounds, Numb, Gear Clatter | Itchy Pin, Hot Brass, Fumble, Strap Burn.
    // ===================================================================================================================
    internal static class Bandolier
    {
        internal const string Id = "bandolier";
        internal const string ItemTpl = "6a1c00000000000000000c07";
        internal const float Interval = 2f;              // s entre balas
        internal const float QuietAfterShot = 1.5f;      // s sin disparar antes de empezar a rellenar
        internal const float SleekChance = 0.20f;
        internal const float PouchChance = 0.10f;
        internal const string PouchGrenade = "5710c24ad2720bc3458b45a3";    // F-1
        internal const float ColdHeat = 0.70f;           // Cold Barrel
        internal const float HotWear = 1.20f;            // Hot Brass
        internal const int LeechRounds = 2;
        internal const float StrapWeight = 0.5f;         // Padded Strap
        internal const float LooseChance = 0.10f;        // Loose Rounds
        internal const float NumbCut = 0.30f;            // Numb: los analgesicos duran un 30% menos
        internal const float ClatterRadius = 20f;        // Gear Clatter
        internal const float PinCut = 0.5f;              // Itchy Pin: s menos de mecha
        internal const float FumbleSpeed = 0.8f;         // Fumble
        internal const float BurnKeep = 0.8f;            // Strap Burn
        internal const float HeavyKg = 50f;              // desafio de Bring it on!

        private static float _next, _lastTrigger;
        private static int _killsNoReload, _lastReloadSeen;
        private static bool _wasGrounded = true, _armsZero;
        private static readonly Dictionary<ThrowWeapTemplate, float> _fuses = new Dictionary<ThrowWeapTemplate, float>();
        private static float _strapDist;
        private static Vector3 _lastPos;

        internal static bool Active { get { return Perks.IsActive(Id); } }
        internal static string Notice() { return Effect(Perks.Power(Id)); }
        internal static string Effect(float power) { return L.F("notice.bandolier.sub", (Interval / Mathf.Max(0.01f, power)).ToString("0.#")); }

        internal static void Reset()
        {
            _next = _lastTrigger = 0f; _killsNoReload = 0; _lastReloadSeen = 0; _wasGrounded = true; _armsZero = false; _strapDist = 0f; _lastPos = Vector3.zero;
            RestoreFuses();
        }

        internal static void Tick(Player me)
        {
            if (!Active) { RestoreFuses(); return; }
            var fc = me.HandsController as Player.FirearmController;
            if (fc != null && fc.IsTriggerPressed) _lastTrigger = Time.time;

            // Loose Rounds: tras cada recarga, 10% de que se caiga una bala del cargador nuevo
            if (Perk.ReloadCount != _lastReloadSeen)
            {
                _lastReloadSeen = Perk.ReloadCount;
                _killsNoReload = 0;
                if (Perk.Has("bb_d_loose") && UnityEngine.Random.value < LooseChance) DropOneRound(me, fc);
            }

            // Gear Clatter: al saltar y al aterrizar haces ruido
            var mc = me.MovementContext;
            if (mc != null)
            {
                bool g = mc.IsGrounded;
                if (g != _wasGrounded && Perk.Has("bb_d_clatter"))
                    Singleton<GlobalEventDispatcher>.Instance.PlaySound(me, me.Position, ClatterRadius, AISoundType.step);
                _wasGrounded = g;
            }

            // Fumble: mas lento mientras recargas
            if (fc != null && Perk.Has("bb_d_fumble") && fc.IsInReloadOperation() && mc != null)
                mc.ChangeSpeedLimit(FumbleSpeed * mc.MaxSpeed, Player.ESpeedLimit.Swamp, 0.3f);

            // Itchy Pin: con una granada en la mano, la mecha es mas corta
            if (Perk.Has("bb_d_pin") && me.HandsController is Player.GrenadeHandsController gh) ShortenFuse(gh.Item as ThrowWeap);
            else if (!(me.HandsController is Player.GrenadeHandsController)) RestoreFuses();

            // Strap Burn: los brazos se recuperan mas lento
            NewPerkUtil.SlowArmRecovery(me, Perk.Has("bb_d_burn") ? BurnKeep : 1f);

            // Padded Strap (desafio): recorre 3 km con dos armas principales
            var eq = me.Profile.Inventory.Equipment;
            if (eq.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem != null && eq.GetSlot(EquipmentSlot.SecondPrimaryWeapon).ContainedItem != null)
            {
                if (_lastPos != Vector3.zero)
                {
                    float d = Vector3.Distance(_lastPos, me.Position);
                    if (d < 5f) _strapDist += d;
                    if (_strapDist >= 10f) { PerkService.AddProgress("bb_strap", (int)_strapDist); _strapDist -= (int)_strapDist; }
                }
                _lastPos = me.Position;
            }
            else _lastPos = Vector3.zero;

            var hc = me.ActiveHealthController;

            // Numb: los analgesicos se cortan al 70% de su duracion
            if (Perk.Has("bb_d_numb"))
                foreach (var pk in hc.FindActiveEffects<ActiveHealthController.PainKiller>(EBodyPart.Common).ToList())
                    if (pk.State == EEffectState.Started && pk.WorkStateTime > 0f && pk.CurStateTimeLeft <= pk.WorkStateTime * NumbCut) pk.ForceResidue();

            Refill(me);
        }

        // ---- base: 1 bala cada 2 s en un cargador del chaleco o los bolsillos (sin la animacion de cargar) ----
        private static void Refill(Player me)
        {
            if (Time.time < _next || Time.time - _lastTrigger < QuietAfterShot) return;
            _next = Time.time + Interval / Mathf.Max(0.01f, Perks.Power(Id));
            var equipment = me.Profile.Inventory.Equipment;
            var rigs = new HashSet<Item>(new[] { EquipmentSlot.TacticalVest, EquipmentSlot.Pockets }
                .Select(s => equipment.GetSlot(s).ContainedItem).Where(i => i != null));
            var all = me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment).ToList();
            foreach (var mag in all.OfType<Magazine>())
            {
                if (mag.Count >= mag.MaxCount || mag.Parent.Container.ParentItem is Weapon || !NewPerkUtil.InsideAny(mag, rigs)) continue;
                var ammo = NewPerkUtil.LooseAmmoFor(all, mag);
                if (ammo == null) continue;
                if (NewPerkUtil.MoveRounds(me, ammo, mag, 1))
                    Dbg.Log("EFECTO", "Bandolier: +1 " + ammo.ShortName + " en " + mag.ShortName + " (" + mag.Count + "/" + mag.MaxCount + ")");
                return;
            }
        }

        // ---- cada disparo propio (desde el gancho de CreateShot) ----
        internal static void OnShot(Player me)
        {
            if (!Active) return;
            PerkService.AddProgress("bb_cold");
            var w = (me.HandsController as Player.FirearmController)?.Item as Weapon;
            var mag = w != null ? w.GetCurrentMagazine() : null;
            // Sleek: al vaciar el cargador, 20% de que se llene solo con tu municion suelta
            if (mag != null && mag.Count == 0 && Perk.Has("bb_sleek") && UnityEngine.Random.value < SleekChance * Perks.Power(Id))
            {
                var all = me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment).ToList();
                int moved = 0;
                for (int i = 0; i < 6 && mag.Count < mag.MaxCount; i++)
                {
                    var ammo = NewPerkUtil.LooseAmmoFor(all, mag);
                    if (ammo == null || !NewPerkUtil.MoveRounds(me, ammo, mag, mag.MaxCount - mag.Count)) break;
                    moved++;
                }
                if (moved > 0) { Hud.Enqueue(L.T("bb_sleek.n"), L.T("notice.bb_sleek")); Dbg.Log("EFECTO", "Sleek: cargador rellenado"); }
            }
        }

        private static void DropOneRound(Player me, Player.FirearmController fc)
        {
            var w = fc != null ? fc.Item as Weapon : null;
            var mag = w != null ? w.GetCurrentMagazine() : null;
            var top = mag != null ? mag.Cartridges.Last : null;
            if (top == null) return;
            var ctrl = me.InventoryController;
            var op = top.StackObjectsCount > 1 ? (object)ItemManipulator.SplitToNowhere(top, 1, ctrl, ctrl, true) : ItemManipulator.Remove(top, ctrl, true);
            if (op is Diz.LanguageExtensions.OperationResult<SplitToNowhereResult> s) { if (s.Succeeded) ctrl.TryRunNetworkTransaction(s); }
            else if (op is Diz.LanguageExtensions.OperationResult<RemoveResult> r) { if (r.Succeeded) ctrl.TryRunNetworkTransaction(r); }
            Dbg.Log("EFECTO", "Loose Rounds: se cae una bala del cargador");
        }

        private static void ShortenFuse(ThrowWeap g)
        {
            var t = g != null ? g.Template as ThrowWeapTemplate : null;
            if (t == null || _fuses.ContainsKey(t)) return;
            _fuses[t] = t.ExplDelay;
            t.ExplDelay = Mathf.Max(0.5f, t.ExplDelay - PinCut);
        }

        private static void RestoreFuses()
        {
            if (_fuses.Count == 0) return;
            foreach (var kv in _fuses) kv.Key.ExplDelay = kv.Value;
            _fuses.Clear();
        }

        // ---- bajas propias ----
        internal static void OnKill(Player me, Player victim, DamageInfo info, EBodyPart part, bool boss)
        {
            if (!Active) return;
            if (boss) PerkService.AddProgress("bb_slot");
            if (++_killsNoReload >= 3) { _killsNoReload = 0; PerkService.AddProgress("bb_sleek"); }
            if (me.Physical != null && CarriedKg(me) >= HeavyKg) PerkService.AddProgress("bb_bring");
            if ((info.DamageType & PerkFx.Blast) != 0) PerkService.AddProgress("bb_pouch");
            if (Rev.FullMags >= Rev.BeltMags) PerkService.AddProgress("bb_disc");     // Cinturon de balas (desafio): bajas llevando 6 cargadores llenos
            if (part == EBodyPart.Head)
            {
                PerkService.AddProgress("bb_leech");
                if (Perk.Has("bb_leech")) LeechRounds2(me);
            }
            if (Perk.Has("bb_pouch") && UnityEngine.Random.value < PouchChance && NewPerkUtil.Give(me.Profile, PouchGrenade))
                Hud.Enqueue(L.T("bb_pouch.n"), L.T("notice.bb_pouch"));
        }

        private static void LeechRounds2(Player me)
        {
            var w = (me.HandsController as Player.FirearmController)?.Item as Weapon;
            var mag = w != null ? w.GetCurrentMagazine() : null;
            var top = mag != null ? mag.Cartridges.Last as Ammo : null;
            if (top == null || mag.Count >= mag.MaxCount) return;
            int add = Math.Min(LeechRounds, mag.MaxCount - mag.Count);
            top.StackObjectsCount += add;           // la bala de arriba del cargador gana 'add' copias
            Dbg.Log("EFECTO", "Ammo Leech: +" + add + " balas");
        }

        internal static float CarriedKg(Player me)
        {
            try { return me.Inventory.TotalWeight.Value; } catch { return 0f; }
        }

        // ---- peso: Bring it on! (municion y cargadores no pesan) y Padded Strap (armas equipadas a la mitad) ----
        private static float _weightCut, _weightAt = -10f;
        internal static float WeightCut(Player me)
        {
            if (!Active || (!Perk.Has("bb_bring") && !Perk.Has("bb_strap"))) return 0f;
            if (Time.time - _weightAt < 1f) return _weightCut;
            _weightAt = Time.time;
            float cut = 0f;
            var eq = me.Profile.Inventory.Equipment;
            if (Perk.Has("bb_bring"))
                foreach (var it in me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment))
                {
                    if (it is Ammo && !(it.Parent?.Container?.ParentItem is Magazine)) cut += it.Template.Weight * it.StackObjectsCount;
                    else if (it is Magazine m) cut += m.TotalWeight;
                }
            if (Perk.Has("bb_strap"))
                foreach (var s in new[] { EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon, EquipmentSlot.Holster })
                {
                    var w = eq.GetSlot(s).ContainedItem;
                    if (w != null) cut += w.TotalWeight * StrapWeight;
                }
            _weightCut = cut;
            return cut;
        }
    }

    // ===================================================================================================================
    // Vulture Aid: saqueo.
    //   Base: los contenedores y cuerpos sin registrar con objetos de valor se ven a traves de las paredes a menos de 15 m.
    //   Mayores: Scent of Money (aviso y marca de PMC que llevan mas de 250k), Carrion Bonus (botin extra en tus bajas),
    //            Re-roll (vuelve a tirar el botin de un contenedor; 10 puntos por raid).
    //   Menores: Sharp Eyes (30 m), Leftovers (municion de tu calibre en tus bajas), Field Kit (botiquin en tus bajas),
    //            Lucky Find (GP Coin al registrar). Especial: Extra Slot.
    //   Desventajas ligadas: Stench, Greedy Stomach, Hunched | Dry Beak, Myopia, Clumsy Talons, Weak Grip.
    // ===================================================================================================================
    internal static class VultureAid
    {
        internal const string Id = "vultureaid";
        internal const string ItemTpl = "6a1c00000000000000000c08";
        internal const float Range = 15f, SharpRange = 30f;
        internal const int MinValue = 50000;              // valor (precio base) para que un contenedor "valga la pena"
        internal const int RichPmc = 250000;              // Scent of Money
        internal const float ScentRange = 80f, ScentShow = 4f, ScentCooldown = 60f;
        internal const float LeftoverChance = 0.25f, FieldKitChance = 0.15f, LuckyChance = 0.10f;
        internal const string FieldKit = "5755356824597772cb798962";   // AI-2
        internal const float StenchRadius = 15f;
        internal const float MyopiaAim = 0.10f;
        internal const float ClumsyDraw = 0.90f;
        internal const float WeakGripKeep = 0.85f;
        private const float Rescan = 2f;

        // botin extra (Carrion Bonus): PMC y jefes 15% en total; Scavs solo GP Coin al 5%
        private static readonly (string Tpl, float Chance)[] BonusPmc =
        {
            (NewPerkUtil.GpCoin, 0.10f), ("6656560053eaaa7a23349c86", 0.01f) /* LEGA */, ("5c94bbff86f7747ee735c08f", 0.025f) /* Labs */,
            ("679b9819a2f2dd4da9023512", 0.015f) /* Labrys */,
        };
        internal const float BonusScav = 0.05f;

        private struct Mark { public Vector3 Pos; public int Value; public int Kind; public float Until; }   // Kind: 0 contenedor, 1 cuerpo, 2 PMC rico
        private static readonly List<Mark> _marks = new List<Mark>();
        private static readonly List<Mark> _rich = new List<Mark>();
        private static LootableContainer[] _containers;
        private static readonly HashSet<string> _searched = new HashSet<string>();
        private static readonly HashSet<string> _scented = new HashSet<string>();
        private static float _nextScan, _nextScent;
        private static int _lastCount = -1;

        internal static bool Active { get { return Perks.IsActive(Id); } }
        internal static string Notice() { return Effect(Perks.Power(Id)); }
        internal static string Effect(float power) { return L.F("notice.vultureaid.sub", Mathf.RoundToInt(Range * power)); }
        internal static float MarkRange { get { return (Perk.Has("va_sharp") ? SharpRange : Range) * Perks.Power(Id); } }

        internal static void Reset()
        {
            _containers = null; _marks.Clear(); _rich.Clear(); _searched.Clear(); _scented.Clear(); _nextScan = _nextScent = 0f; _lastCount = -1;
            Reroll.Reset();
        }

        internal static void Tick(Player me)
        {
            if (!Active) return;
            Reroll.Tick(me);
            if (Time.time < _nextScan) return;
            _nextScan = Time.time + Rescan;
            float range = MarkRange;
            if (_containers == null) _containers = UnityEngine.Object.FindObjectsOfType<LootableContainer>();
            _marks.Clear();
            var pos = me.Position;
            foreach (var c in _containers)
            {
                if (c == null) continue;
                var root = c.ItemOwner != null ? c.ItemOwner.RootItem : null;
                if (root == null) continue;
                float d2 = (c.transform.position - pos).sqrMagnitude;
                if (d2 > Mathf.Max(range, 2f) * Mathf.Max(range, 2f)) continue;
                if (Searched(root, d2, false)) continue;
                int v = Value(root);
                if (v >= MinValue) _marks.Add(new Mark { Pos = c.transform.position + Vector3.up * 0.4f, Value = v });
            }
            var world = Singleton<GameWorld>.Instance;
            if (world != null)
                foreach (var p in world.AllPlayersEverExisted)
                {
                    if (p == null || p == me || p.HealthController == null) continue;
                    float d2 = (p.Position - pos).sqrMagnitude;
                    if (p.HealthController.IsAlive) { Scent(me, p, d2); continue; }
                    if (d2 > Mathf.Max(range, 2f) * Mathf.Max(range, 2f)) continue;
                    if (Searched(p.Profile.Inventory.Equipment, d2, true, NewPerkUtil.IsPmc(p))) continue;
                    int v = NewPerkUtil.ProfileValue(p);
                    if (v >= MinValue) _marks.Add(new Mark { Pos = p.Position + Vector3.up * 0.3f, Value = v, Kind = 1 });
                }
            _rich.RemoveAll(m => Time.time > m.Until);
            if (_marks.Count != _lastCount) { _lastCount = _marks.Count; Dbg.Log("EFECTO", "Vulture: " + _marks.Count + " marcas a menos de " + range.ToString("0") + " m"); }
        }

        // Scent of Money: un PMC con mucho valor encima se marca unos segundos (una vez por PMC, como mucho uno cada 60 s)
        private static void Scent(Player me, Player p, float d2)
        {
            if (!Perk.Has("va_scent") || !NewPerkUtil.IsPmc(p) || d2 > ScentRange * ScentRange || Time.time < _nextScent || _scented.Contains(p.ProfileId)) return;
            int v = NewPerkUtil.ProfileValue(p);
            if (v < RichPmc) return;
            _scented.Add(p.ProfileId);
            _nextScent = Time.time + ScentCooldown;
            _rich.Add(new Mark { Pos = p.Position + Vector3.up * 1.0f, Value = v, Kind = 2, Until = Time.time + ScentShow });
            Hud.Enqueue(L.T("va_scent.n"), L.F("notice.va_scent", Mathf.RoundToInt(Mathf.Sqrt(d2)), v / 1000));
        }

        // Un contenedor se da por registrado cuando el juego lo marca, o tras quedarte a menos de 1.5 m (ahi cuentan los desafios)
        private static bool Searched(Item root, float d2, bool body, bool pmc = false)
        {
            if (_searched.Contains(root.Id)) return true;
            var s = root as SearchableItem;
            if ((s != null && s.IsSearched) || d2 < 1.5f * 1.5f)
            {
                _searched.Add(root.Id);
                OnSearched(root, body, pmc);
                return true;
            }
            return false;
        }

        private static void OnSearched(Item root, bool body, bool pmc)
        {
            if (body) { PerkService.AddProgress("va_lucky"); if (pmc) { PerkService.AddProgress("va_scent"); Rev.OnLootedPmc(); Apex.OnLootedPmc(); } }
            else
            {
                PerkService.AddProgress("va_sharp");
                PerkService.AddProgress("va_reroll");
                // Lucky Find: al registrar un contenedor, 10% de una GP Coin dentro
                if (Perk.Has("va_lucky") && UnityEngine.Random.value < LuckyChance && PutInto(root as CompoundItem, NewPerkUtil.GpCoin))
                    Hud.Enqueue(L.T("va_lucky.n"), L.T("notice.va_lucky"));
            }
        }

        internal static bool PutInto(CompoundItem c, string tpl, int count = 1)
        {
            if (c == null) return false;
            var item = Singleton<ItemFactory>.Instance.CreateItem(Guid.NewGuid().ToString("N").Substring(0, 24), tpl, null);
            if (item == null) return false;
            if (count > 1) item.StackObjectsCount = Math.Min(count, item.StackMaxSize);
            foreach (var grid in c.Grids)
            {
                var address = grid.FindLocationForItem(item);
                if (address != null && grid.AddItemWithoutRestrictions(item, address.LocationInGrid).Succeeded) return true;
            }
            return false;
        }

        internal static int Value(Item root)
        {
            int v = 0;
            var c = root as CompoundItem;
            if (c == null) return 0;
            foreach (var grid in c.Grids)
                foreach (var it in grid.Items) v += NewPerkUtil.Price(it) + Value(it);
            return v;
        }

        // ---- bajas propias: Carrion Bonus, Leftovers, Field Kit ----
        internal static void OnKill(Player me, Player victim, bool boss)
        {
            if (!Active) return;
            if (boss) PerkService.AddProgress("va_slot");
            PerkService.AddProgress("va_bonus");
            PerkService.AddProgress("va_leftover");
            if (Vector3.Distance(me.Position, victim.Position) <= 15f) PerkService.AddProgress("va_kit");
            bool pmc = NewPerkUtil.IsPmc(victim);
            if (Perk.Has("va_bonus"))
            {
                if (pmc || boss)
                {
                    float r = UnityEngine.Random.value;
                    foreach (var b in BonusPmc)
                    {
                        if (r < b.Chance) { Drop(victim, b.Tpl, "Carrion Bonus"); break; }
                        r -= b.Chance;
                    }
                }
                else if (UnityEngine.Random.value < BonusScav) Drop(victim, NewPerkUtil.GpCoin, "Carrion Bonus");
            }
            if (Perk.Has("va_leftover") && UnityEngine.Random.value < LeftoverChance)
            {
                var w = (me.HandsController as Player.FirearmController)?.Item as Weapon;
                var top = w != null && w.GetCurrentMagazine() != null ? w.GetCurrentMagazine().Cartridges.Last : null;
                if (top != null) Drop(victim, top.TemplateId, "Leftovers", UnityEngine.Random.Range(15, 41));
            }
            if (Perk.Has("va_kit") && UnityEngine.Random.value < FieldKitChance) Drop(victim, FieldKit, "Field Kit");
        }

        private static void Drop(Player victim, string tpl, string why, int count = 1)
        {
            if (NewPerkUtil.Give(victim.Profile, tpl, count))
            {
                Dbg.Log("EFECTO", why + ": " + tpl + " x" + count + " en el cuerpo de " + victim.Profile.Nickname);
                Hud.Enqueue(L.T("perk.vultureaid"), L.T("notice.va_drop"));
            }
        }

        private static GUIStyle _style;
        internal static void DrawGui()
        {
            if (!Active || Event.current.type != EventType.Repaint) return;
            Reroll.DrawGui();
            if (_marks.Count == 0 && _rich.Count == 0) return;
            var cam = Camera.main;
            if (cam == null) return;
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold };
            float scale = Screen.height / (float)cam.pixelHeight;
            foreach (var m in _marks.Concat(_rich))
            {
                var sp = cam.WorldToScreenPoint(m.Pos);
                if (sp.z <= 0f) continue;
                float x = sp.x * scale, y = Screen.height - sp.y * scale;
                float a = m.Kind == 2 ? 1f : Mathf.Lerp(1f, 0.5f, sp.z / Mathf.Max(1f, MarkRange));
                Color col = m.Kind == 0 ? new Color(0.55f, 1f, 0.4f) : (m.Kind == 1 ? new Color(1f, 0.55f, 0.25f) : new Color(1f, 0.85f, 0.2f));
                col.a = a;
                GUI.color = new Color(0f, 0f, 0f, a * 0.8f);
                GUI.Label(new Rect(x - 59f, y - 21f, 120f, 20f), "◆", _style);
                GUI.Label(new Rect(x - 59f, y - 5f, 120f, 20f), (m.Kind == 2 ? "PMC " : "") + (m.Value / 1000) + "k ₽", _style);
                GUI.color = col;
                GUI.Label(new Rect(x - 60f, y - 22f, 120f, 20f), "◆", _style);
                GUI.Label(new Rect(x - 60f, y - 6f, 120f, 20f), (m.Kind == 2 ? "PMC " : "") + (m.Value / 1000) + "k ₽", _style);
            }
            GUI.color = Color.white;
        }
    }

    // ===================================================================================================================
    // Melee Macchiato: cuerpo a cuerpo. Los estilos (combos por peso del arma) estan en MeleeStyle.
    //   Base: dano cuerpo a cuerpo x2 y golpes un 30% mas rapidos.
    //   Mayores: Charge (corriendo con el cuchillo, -40% de dano recibido), Frenzy (cada golpe seguido al mismo objetivo x1.2),
    //            Iron Skull (con el cuchillo en la mano no mueres por un tiro en la cabeza).
    //   Menores: Quick Draw (sacar el cuchillo es instantaneo), Whetstone (el cuchillo se repara al acertar), Flash Strike (el ataque
    //            rapido es instantaneo), Second Wind (cada golpe da estamina). Especial: Extra Slot.
    //   Desventajas ligadas: Glass Jaw, Caffeine Jitters, Caffeine Crash | Butterfingers, Bitter Brew, Loud Steps, Sugar Crash.
    // ===================================================================================================================
    internal static class MeleeMacchiato
    {
        internal const string Id = "meleemacchiato";
        internal const string ItemTpl = "6a1c00000000000000000c09";
        internal const float DamageMult = 2f;
        internal const float Speed = 1.3f;
        internal const float ChargeTaken = 0.60f;        // Charge: -40%
        internal const float FrenzyStep = 1.2f, FrenzyMax = 3f, FrenzyWindow = 3f;
        internal const float QuickDraw = 8f;             // Quick Draw: x8 (practicamente instantaneo)
        internal const float FlashSpeed = 2.5f;          // Flash Strike: el tajo rapido x2.5
        internal const float HoneRepair = 2f;            // Whetstone: puntos de durabilidad por golpe acertado
        internal const float WindSwing = 5f, WindHit = 15f;   // Second Wind
        internal const float GlassTaken = 1.10f;         // Glass Jaw
        internal const float JitterSway = 1.20f;         // Caffeine Jitters
        internal const float CrashDrain = 0.15f;         // Caffeine Crash
        internal const float ButterDraw = 0.85f;         // Butterfingers
        internal const float LoudRadius = 12f;           // Loud Steps

        private static string _target;
        private static int _streak;
        private static float _lastHit;
        private static float _sprintDist;

        internal static bool Active { get { return Perks.IsActive(Id); } }
        internal static string Notice() { return Effect(Perks.Power(Id)); }
        internal static string Effect(float power)
        {
            return L.F("notice.meleemacchiato.sub", (1f + (DamageMult - 1f) * power).ToString("0.#"), Mathf.RoundToInt((Speed - 1f) * 100f * power));
        }

        internal static float DamageFactor { get { float p = Perks.Power(Id); return p <= 0f ? 1f : 1f + (DamageMult - 1f) * p; } }
        internal static float SpeedFactor { get { float p = Perks.Power(Id); return p <= 0f ? 1f : 1f + (Speed - 1f) * p; } }
        internal static bool KnifeOut(Player me) { return me != null && me.HandsController is Player.KnifeController; }

        internal static void Reset() { _target = null; _streak = 0; _lastHit = 0f; _sprintDist = 0f; }

        internal static void Tick(Player me, float dt)
        {
            if (!Active) return;
            // Charge (desafio): corre 2 km con el arma cuerpo a cuerpo en la mano
            if (KnifeOut(me) && me.Physical != null && me.Physical.Sprinting)
            {
                _sprintDist += PerkFx.Speed * dt;
                if (_sprintDist >= 10f) { PerkService.AddProgress("mm_charge", (int)_sprintDist); _sprintDist -= (int)_sprintDist; }
            }
        }

        // velocidad del golpe que empieza (Flash Strike acelera el tajo rapido)
        internal static float SwingSpeed(bool alternative)
        {
            float s = SpeedFactor;
            if (!alternative && Perk.Has("mm_flash")) s *= FlashSpeed;
            return s;
        }

        // ---- al empezar un golpe ----
        internal static void OnSwing(Player me)
        {
            if (!Active) return;
            PerkService.AddProgress("mm_flash");
            Rev.OnSwing();                                   // Golpe en falso
            if (Perk.Has("mm_wind")) AddStamina(me, WindSwing);
            var st = me.Physical != null ? me.Physical.Stamina : null;
            if (st != null && st.TotalCapacity.Value > 0f && st.Current < st.TotalCapacity.Value * 0.3f) PerkService.AddProgress("mm_wind");
        }

        // ---- dano que haces cuerpo a cuerpo (multiplicador final) ----
        internal static float OnHit(Player me, Player victim, float damage)
        {
            float m = DamageFactor;
            if (!Active) return damage * m;
            PerkService.AddProgress("mm_hone");
            Rev.OnMeleeHit();                                // Golpe en falso, Sed de sangre
            if (Time.time - _lastHit > FrenzyWindow || victim.ProfileId != _target) { _target = victim.ProfileId; _streak = 0; }
            _streak++;
            _lastHit = Time.time;
            if (_streak == 3) PerkService.AddProgress("mm_frenzy");
            if (Perk.Has("mm_frenzy") && _streak > 1) m *= Mathf.Min(FrenzyMax, Mathf.Pow(FrenzyStep, _streak - 1));
            if (Perk.Has("mm_wind")) AddStamina(me, WindHit);
            if (Perk.Has("mm_hone"))
            {
                var knife = (me.HandsController as Player.KnifeController)?.Item;
                RepairableComponent rep;
                if (knife != null && knife.TryGetItemComponent(out rep) && rep.Durability < rep.MaxDurability)
                    rep.Durability = Mathf.Min(rep.MaxDurability, rep.Durability + HoneRepair);
            }
            return damage * m;
        }

        private static void AddStamina(Player me, float v)
        {
            var st = me.Physical != null ? me.Physical.Stamina : null;
            if (st != null) st.Current = Mathf.Min(st.TotalCapacity.Value, st.Current + v);
        }

        // ---- dano que recibes ----
        internal static float OnDamageTaken(Player me, ActiveHealthController hc, EBodyPart part, float damage)
        {
            if (!Active) return damage;
            bool knife = KnifeOut(me);
            if (knife && Perk.Has("mm_charge") && me.Physical != null && me.Physical.Sprinting) damage *= ChargeTaken;
            if (!knife && me.HandsController is Player.FirearmController && Perk.Has("mm_d_glass")) damage *= GlassTaken;
            // Iron Skull: con el cuchillo en la mano, un golpe en la cabeza no te mata (te deja a 1)
            if (knife && part == EBodyPart.Head && Perk.Has("mm_iron"))
            {
                float cur = hc.GetBodyPartHealth(EBodyPart.Head).Current;
                if (damage >= cur - 1f) { damage = Mathf.Max(0f, cur - 1f); Dbg.Log("EFECTO", "Iron Skull: tiro en la cabeza absorbido"); }
            }
            return damage;
        }

        // ---- bajas propias ----
        internal static void OnKill(Player me, DamageInfo info, bool boss)
        {
            if (!Active || (info.DamageType & EDamageType.Melee) == 0) return;
            PerkService.AddProgress("mm_iron");
            if (boss) PerkService.AddProgress("mm_slot");
        }

        // ---- sacar el arma: Quick Draw (cuchillo) y Butterfingers (armas de fuego) ----
        internal static float DrawMult(Player me)
        {
            if (!Active || me == null) return 1f;
            if (KnifeOut(me)) return Perk.Has("mm_quick") ? QuickDraw : 1f;
            return Perk.Has("mm_d_butter") ? ButterDraw : 1f;
        }

        internal static void OnKnifeDrawn()
        {
            if (Active) PerkService.AddProgress("mm_quick");
        }
    }

    // ---------- Cold Barrel (el arma se calienta menos) y Hot Brass (se desgasta mas): solo tus armas ----------
    [HarmonyLib.HarmonyPatch(typeof(Weapon), "get_HeatFactorByShot")]
    internal static class ColdBarrelPatch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void Postfix(Weapon __instance, ref float __result)
        {
            if (!Bandolier.Active || !Perk.Has("bb_cold")) return;
            var me = Plugin.Me;
            if (me != null && ReferenceEquals(__instance.Owner, me.InventoryController)) __result *= Bandolier.ColdHeat;
        }
    }

    [HarmonyLib.HarmonyPatch(typeof(Weapon), "get_DurabilityBurnRatio")]
    internal static class HotBrassPatch
    {
        [HarmonyLib.HarmonyPostfix]
        private static void Postfix(Weapon __instance, ref float __result)
        {
            if (!Bandolier.Active || !Perk.Has("bb_d_brass")) return;
            var me = Plugin.Me;
            if (me != null && ReferenceEquals(__instance.Owner, me.InventoryController)) __result *= Bandolier.HotWear;
        }
    }
}
