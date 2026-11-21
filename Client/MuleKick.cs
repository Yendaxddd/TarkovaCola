using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Comfort.Common;
using Diz.LanguageExtensions;
using EFT;
using EFT.Ballistics;
using EFT.InputSystem;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ===================================================================================================================
    // Mule Kick: una 3.a ranura de arma ("MuleKick", acepta armas principales).
    //   Base: la ranura aparece en el inventario solo con la perk (o si aun tiene un arma dentro, para poder sacarla; entonces no
    //         admite nada nuevo). El arma se saca con la rueda del raton (principal -> secundaria -> pistola -> Mule Kick) y al
    //         sacarla se ve el icono de la perk 1 s a la derecha de la pantalla. Cambiar a/desde ella es x1.5 mas rapido.
    //   Mayores: Mule's Kick (primer disparo en 2 s tras cambiar: dano x1.5), Mule Bandolier (se recarga sola guardada: 1 bala
    //            cada 1.5 s de la municion suelta), Quick Change (cambiar con el cargador vacio es instantaneo).
    //   Menores: Light Strap (pesa x0.5), Chamber Ready (sale con bala en la recamara), Balance (balanceo x0.85 2 s al sacarla),
    //            Holster (las otras armas tambien x1.3). Especial: Extra Slot.
    //   Desventajas ligadas: Mule Load, Loose Strap, Clumsy Swap | Empty Chamber, Safety On, Clatter, Tangled.
    //   Compatible con la funda izquierda de DualWieldLab: los indices de las ranuras se asignan por nombre.
    // ===================================================================================================================
    internal static class MuleKick
    {
        internal const string Id = "mulekick";
        internal const string ItemTpl = "6a1c00000000000000000c0b";
        internal const string SlotName = "MuleKick";
        internal const float SwapBoost = 1.5f, ClumsyDraw = 0.7f, HolsterDraw = 1.3f, QuickChange = 4f;
        internal const float KickWindow = 2f, KickMult = 1.5f;
        internal const float BandoInterval = 1.5f;
        internal const float StrapLight = 0.5f, StrapHeavy = 1.5f;
        internal const float BalanceSway = 0.85f, BalanceSeconds = 2f;
        internal const float DropMinDamage = 30f, DropChance = 0.05f;
        internal const float SafetyDelay = 0.3f, ClatterRadius = 15f;
        internal const int ApexRounds = 5;
        internal const float ApexCooldown = 10f;            // por arma: evita municion infinita cambiando sin parar
        internal const float IconSeconds = 1f;

        // indice de la ranura en el equipo (lo asigna el constructor de InventoryEquipment al leer el nombre "MuleKick")
        internal static int SlotIndex = -1;
        internal static EquipmentSlot SlotEnum { get { return (EquipmentSlot)SlotIndex; } }

        private static Item _cur;                            // arma en las manos (la anterior, mientras el juego crea la nueva)
        private static float _swapAt = -99f, _muleDrawAt = -99f, _iconAt = -99f, _nextBando, _kickUntil, _safetyAt = -1f;
        private static bool _kickArmed, _kickUsed, _safetyDone = true, _safetyHeld;
        private static float _meters; private static Vector3 _lastPos; private static bool _hasPos;
        private static readonly Dictionary<string, float> _apexAt = new Dictionary<string, float>();

        internal static bool Active { get { return Perks.IsActive(Id); } }
        internal static string Notice() { return Effect(Perks.Power(Id)); }
        internal static string Effect(float power) { return L.T("perk.mulekick.desc"); }

        internal static void Reset()
        {
            _cur = null; _swapAt = -99f; _muleDrawAt = -99f; _iconAt = -99f; _nextBando = 0f; _kickUntil = 0f; _safetyAt = -1f;
            _kickArmed = false; _kickUsed = true; _safetyDone = true; _safetyHeld = false;
            _meters = 0f; _hasPos = false; _apexAt.Clear();
        }

        // ---------------------------------------------------------------- ranura
        internal static Slot SlotOf(InventoryEquipment eq)
        {
            if (eq == null || SlotIndex < 0 || eq.Slots == null || eq.Slots.Length <= SlotIndex) return null;
            var s = eq.GetSlot(SlotEnum);
            return s != null && s.ID == SlotName ? s : null;
        }

        internal static bool IsMule(Item it)
        {
            // ojo: Item.Parent lanza si el objeto no esta en ningun sitio (la RGD-5 de la explosion de PHD/APEX): se mira CurrentAddress
            var addr = it != null ? it.CurrentAddress : null;
            var s = addr != null ? addr.Container as Slot : null;
            return s != null && s.ID == SlotName;
        }

        internal static Weapon MuleWeapon(Player me)
        {
            var s = me != null ? SlotOf(me.Profile.Inventory.Equipment) : null;
            return s != null ? s.ContainedItem as Weapon : null;
        }

        // se pueden meter armas: en raid, con la perk activa; fuera, si la tienes guardada (Eterna o Classic)
        private static bool _avail; private static float _availAt = -99f;
        internal static bool Available
        {
            get
            {
                if (Plugin.InRaidWorld) return Active;
                if (Time.realtimeSinceStartup < _availAt) return _avail;
                _availAt = Time.realtimeSinceStartup + 1f;
                try { _avail = ClassicMode.Enabled ? ClassicMode.Find(Id) != null : Eternal.Stored().Contains(Id); }
                catch { _avail = false; }
                return _avail;
            }
        }

        // el equipo es el del jugador (en raid o el PMC fuera de ella)
        internal static bool IsOwn(InventoryEquipment eq)
        {
            if (eq == null) return false;
            var me = Plugin.AnyMe;
            if (me != null && me.Profile != null && me.Profile.Inventory.Equipment != null && me.Profile.Inventory.Equipment.Id == eq.Id) return true;
            try
            {
                var app = Singleton<ClientApplication<IEftSession>>.Instance;
                var session = app != null ? app.GetClientBackEndSession() : null;
                var prof = session != null ? session.Profile : null;
                return prof != null && prof.Inventory != null && prof.Inventory.Equipment != null && prof.Inventory.Equipment.Id == eq.Id;
            }
            catch { return false; }
        }

        private static int Rounds(Weapon w)
        {
            if (w == null) return 0;
            var mag = w.GetCurrentMagazine();
            return (mag != null ? mag.Count : 0) + w.ChamberAmmoCount;
        }

        // ---------------------------------------------------------------- velocidad de cambio (Perk.DrawMult)
        internal static float DrawMult(Player me)
        {
            if (me == null || !Active) return 1f;
            var now = (me.HandsController as Player.FirearmController)?.Item;
            bool mule = IsMule(now) || IsMule(_cur);
            float m;
            if (mule) m = Perk.Has("mk_d_clumsy") ? ClumsyDraw : 1f + (SwapBoost - 1f) * Perks.Power(Id);
            else m = Perk.Has("mk_holster") ? HolsterDraw : 1f;
            if (Perk.Has("mk_change") && !ReferenceEquals(now, _cur) && Rounds(_cur as Weapon) == 0 && _cur is Weapon) m *= QuickChange;
            return m;
        }

        // ---------------------------------------------------------------- antes de sacar un arma (Player.TryProceed)
        internal static void BeforeDraw(Player me, Weapon w)
        {
            if (!Active || w == null) return;
            var ctrl = me.InventoryController;
            if (Apex.Has(Id))
            {
                float at;
                if (!_apexAt.TryGetValue(w.Id, out at) || Time.time - at >= ApexCooldown)
                {
                    _apexAt[w.Id] = Time.time;
                    AddRounds(w, ApexRounds);
                    Chamber(w, ctrl);
                }
            }
            if (!IsMule(w)) return;
            if (Perk.Has("mk_chamber")) Chamber(w, ctrl);
            else if (Perk.Has("mk_d_empty")) Unchamber(w, ctrl);
        }

        private static void Chamber(Weapon w, InventoryController ctrl)
        {
            try
            {
                if (w.Chambers == null || w.Chambers.Length == 0 || w.Chambers[0].ContainedItem != null) return;
                var mag = w.GetCurrentMagazine();
                if (mag == null || mag.Count <= 0 || mag is CylinderMagazine) return;
                var op = mag.Cartridges.PopTo(ctrl, w.Chambers[0].CreateItemAddress());
                if (op.Failed) { Dbg.Log("EFECTO", "Mule Kick: no se pudo meter bala en la recamara: " + op.Error); return; }
                ctrl.TryRunNetworkTransaction(op);
                Dbg.Log("EFECTO", "Mule Kick: bala en la recamara de " + w.ShortName);
            }
            catch (Exception e) { Dbg.Throttled("mkchamber", 10f, "ERROR", "Mule Kick (recamara): " + e.Message); }
        }

        private static void Unchamber(Weapon w, InventoryController ctrl)
        {
            try
            {
                if (w.Chambers == null || w.Chambers.Length == 0) return;
                var round = w.Chambers[0].ContainedItem as Ammo;
                var mag = w.GetCurrentMagazine();
                if (round == null || round.IsUsed || mag == null || mag.Count >= mag.MaxCount || mag is CylinderMagazine) return;
                var op = ItemManipulator.Move(round, mag.Cartridges.CreateItemAddress(), ctrl);
                if (op.Failed) return;
                ctrl.TryRunNetworkTransaction(op);
                Dbg.Log("EFECTO", "Mule Kick (Empty Chamber): recamara vacia en " + w.ShortName);
            }
            catch (Exception e) { Dbg.Throttled("mkunchamber", 10f, "ERROR", "Mule Kick (recamara vacia): " + e.Message); }
        }

        private static void AddRounds(Weapon w, int n)
        {
            try
            {
                var mag = w.GetCurrentMagazine();
                if (mag == null || mag is CylinderMagazine) return;
                n = Math.Min(n, mag.MaxCount - mag.Count);
                var tpl = w.CurrentAmmoTemplate;
                if (n <= 0 || tpl == null) return;
                var top = mag.Cartridges.Last;
                if (top != null && top.TemplateId == tpl._id && top.StackObjectsCount + n <= top.StackMaxSize) top.StackObjectsCount += n;
                else
                {
                    var ammo = Singleton<ItemFactory>.Instance.CreateItem(Guid.NewGuid().ToString("N").Substring(0, 24), tpl._id, null);
                    if (ammo == null) return;
                    ammo.StackObjectsCount = n;
                    mag.Cartridges.Add(ammo, false);
                }
                Dbg.Log("EFECTO", "Mule Kick APEX: +" + n + " balas en " + w.ShortName);
            }
            catch (Exception e) { Dbg.Throttled("mkapex", 10f, "ERROR", "Mule Kick (APEX): " + e.Message); }
        }

        // ---------------------------------------------------------------- cambio de arma (Tracker.OnHandsChanged)
        internal static void OnHandsChanged(Player me, object hc)
        {
            var prev = _cur;
            var item = (hc as Player.FirearmController)?.Item;
            _cur = item;
            if (!Active || item == null) return;
            _swapAt = Time.time;
            _kickArmed = false; _kickUsed = false;
            if (prev != null && !ReferenceEquals(prev, item))
            {
                PerkService.AddProgress("mk_holster");
                if (prev is Weapon pw && Rounds(pw) == 0) PerkService.AddProgress("mk_change");
            }
            if (!IsMule(item)) return;
            _muleDrawAt = Time.time; _iconAt = Time.time;
            _safetyDone = !Perk.Has("mk_d_safety"); _safetyAt = -1f; _safetyHeld = false;
            PerkService.AddProgress("mk_chamber");
            if (Perk.Has("mk_d_clatter")) Singleton<GlobalEventDispatcher>.Instance.PlaySound(me, me.Position, ClatterRadius, AISoundType.gun);
            Dbg.Log("EFECTO", "Mule Kick: arma de la 3.a ranura en las manos (" + item.ShortName + ")");
        }

        // ---------------------------------------------------------------- cada frame
        internal static void Tick(Player me)
        {
            if (!Active) return;
            var w = MuleWeapon(me);
            if (w == null) { _hasPos = false; return; }
            float now = Time.time;
            bool inHands = me.HandsController != null && ReferenceEquals(me.HandsController.Item, w);

            // Light Strap (desafio): distancia con un arma en la ranura
            var pos = me.Position;
            if (_hasPos)
            {
                var d = pos - _lastPos; d.y = 0f;
                float m = d.magnitude;
                if (m < 5f) { _meters += m; if (_meters >= 10f) { _meters -= 10f; PerkService.AddProgress("mk_strap", 10); } }
            }
            _lastPos = pos; _hasPos = true;

            // Mule Bandolier: se recarga sola mientras esta guardada
            if (!inHands && Perk.Has("mk_bando") && now >= _nextBando)
            {
                _nextBando = now + BandoInterval;
                var mag = w.GetCurrentMagazine();
                if (mag != null && !(mag is CylinderMagazine) && mag.Count < mag.MaxCount)
                {
                    var all = me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment).ToList();
                    var ammo = NewPerkUtil.LooseAmmoFor(all, mag);
                    if (ammo != null && NewPerkUtil.MoveRounds(me, ammo, mag, 1))
                        Dbg.Log("EFECTO", "Mule Bandolier: +1 " + ammo.ShortName + " en " + w.ShortName + " (" + (mag.Count + 1) + "/" + mag.MaxCount + ")");
                }
            }

            // Safety On: el primer disparo se retrasa; si sigues apretando, dispara al acabar el retraso
            if (!_safetyDone && _safetyAt >= 0f && now - _safetyAt >= SafetyDelay)
            {
                _safetyDone = true;
                var fc = me.HandsController as Player.FirearmController;
                if (_safetyHeld && fc != null && ReferenceEquals(fc.Item, w)) fc.SetTriggerPressed(true);
            }
        }

        // gatillo del arma de Mule Kick (SetTriggerPressed): devuelve false si hay que bloquearlo
        internal static bool AllowTrigger(Player.FirearmController fc, bool pressed)
        {
            if (_safetyDone || !IsMule(fc.Item)) return true;
            if (!pressed) { _safetyHeld = false; return true; }
            _safetyHeld = true;
            if (_safetyAt < 0f) _safetyAt = Time.time;
            return Time.time - _safetyAt >= SafetyDelay;
        }

        // ---------------------------------------------------------------- combate
        internal static void OnShot(Player me)
        {
            if (!Active || _kickUsed || !Perk.Has("mk_kick")) return;
            _kickUsed = true;
            if (Time.time - _swapAt <= KickWindow) { _kickArmed = true; _kickUntil = Time.time + 0.6f; }
        }

        internal static float OnDealt(float damage, DamageInfo info)
        {
            if (!_kickArmed) return damage;
            _kickArmed = false;
            if (Time.time > _kickUntil) return damage;
            Dbg.Log("EFECTO", "Mule's Kick: primer disparo tras cambiar de arma x" + KickMult);
            return damage * KickMult;
        }

        internal static void OnTaken(Player me, float damage)
        {
            if (!Active || damage < DropMinDamage || !Perk.Has("mk_d_strap") || UnityEngine.Random.value >= DropChance) return;
            var w = MuleWeapon(me);
            if (w == null || (me.HandsController != null && ReferenceEquals(me.HandsController.Item, w))) return;
            var r = me.InventoryController.TryThrowItem(w);
            if (r.Failed) return;
            Hud.Notify(L.T("perk.mulekick"), L.T("mule.dropped"), Id);
            Dbg.Log("EFECTO", "Loose Strap: el arma de Mule Kick (" + w.ShortName + ") cae al suelo");
        }

        internal static void OnKill(Player me, Player victim, DamageInfo info, bool boss)
        {
            if (!Active) return;
            if (boss) PerkService.AddProgress("mk_slot");
            if (Time.time - _swapAt <= KickWindow) PerkService.AddProgress("mk_kick");
            if (!IsMule(info.Weapon)) return;
            PerkService.AddProgress("mk_bando");
            Apex.AddProgress(Id, 1f);
            var pwa = me.ProceduralWeaponAnimation;
            if (pwa != null && pwa.IsAiming) PerkService.AddProgress("mk_balance");
        }

        internal static float SwayFactor()
        {
            return Active && Perk.Has("mk_balance") && Time.time - _muleDrawAt <= BalanceSeconds ? BalanceSway : 1f;
        }

        // peso: diferencia que se suma al peso total (Light Strap / Mule Load)
        internal static float WeightDelta(Player me)
        {
            if (!Active) return 0f;
            float m = 1f;
            if (Perk.Has("mk_strap")) m *= StrapLight;
            if (Perk.Has("mk_d_load")) m *= StrapHeavy;
            if (m == 1f) return 0f;
            var w = MuleWeapon(me);
            return w != null ? w.TotalWeight * (m - 1f) : 0f;
        }

        // Tangled: no se recarga esprintando
        internal static bool BlockReload(Player me, Item item)
        {
            return Active && Perk.Has("mk_d_tangled") && IsMule(item) && me.IsSprintEnabled;
        }

        // ---------------------------------------------------------------- rueda del raton
        private static readonly EquipmentSlot[] Order = { EquipmentSlot.FirstPrimaryWeapon, EquipmentSlot.SecondPrimaryWeapon, EquipmentSlot.Holster };

        // devuelve true si la rueda se ha usado para cambiar de arma (si no, el juego la usa para la velocidad al andar)
        internal static bool Cycle(Player me, int dir)
        {
            if (!Active || Plugin.CfgMuleWheel == null || !Plugin.CfgMuleWheel.Value) return false;
            var eq = me.Profile.Inventory.Equipment;
            var mule = SlotOf(eq);
            if (mule == null || !(mule.ContainedItem is Weapon)) return false;
            if (me.InventoryController.IsChangingWeapon || me.HandsController == null) return false;
            if (me.HandsController is Player.GrenadeHandsController) return false;
            var pwa = me.ProceduralWeaponAnimation;
            if (pwa != null && pwa.IsAiming) return false;

            var list = Order.Select(s => eq.GetSlot(s).ContainedItem).Concat(new[] { mule.ContainedItem }).ToList();
            int cur = list.FindIndex(i => i != null && ReferenceEquals(i, me.HandsController.Item));
            int n = list.Count;
            int start = cur < 0 ? (dir > 0 ? -1 : 0) : cur;
            for (int k = 1; k <= n; k++)
            {
                int i = ((start + dir * k) % n + n) % n;
                var it = list[i] as Weapon;
                if (it == null || ReferenceEquals(it, me.HandsController.Item)) continue;
                if (!it.CheckAction(null).Succeeded) continue;
                me.TryProceed(it, r => { }, true);
                return true;
            }
            return true;                                      // solo hay un arma: no cambia la velocidad por sorpresa
        }

        // ---------------------------------------------------------------- icono al sacarla
        internal static void DrawGui()
        {
            float t = Time.time - _iconAt;
            if (t < 0f || t > IconSeconds) return;
            float a = t < IconSeconds * 0.6f ? 1f : 1f - (t - IconSeconds * 0.6f) / (IconSeconds * 0.4f);
            float size = Mathf.Clamp(Screen.height * 0.08f, 48f, 128f);
            var r = new Rect(Screen.width - size - Screen.width * 0.04f, Screen.height * 0.5f - size * 0.5f, size, size);
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            var tex = Hud.IconTex(Id);
            if (tex != null) GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
            else
            {
                var st = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(size * 0.22f) };
                st.normal.textColor = new Color(0.47f, 0.78f, 0.35f, a);
                GUI.Label(new Rect(r.x - size, r.y, size * 2f, size), L.T("mule.slot"), st);
            }
            GUI.color = prev;
        }

        // ---------------------------------------------------------------- bots: llevan una 3.a arma (botin)
        private static readonly string[] BotGuns =
        {
            "57dc2fa62459775949412633",   // AKS-74U
            "5926bb2186f7744b1c6c6e60",   // MP5
            "576165642459773c7a400233",   // Saiga-12
            "5ae08f0a5acfc408fb1398a1",   // Mosin
            "56dee2bdd2720bc8328b4567",   // MP-153
            "59984ab886f7743e98271174",   // PP-19-01
            "57d14d2524597714373db789",   // PP-91 Kedr
        };

        internal static void GiveBotWeapon(Player bot)
        {
            try
            {
                var slot = SlotOf(bot.Profile.Inventory.Equipment);
                if (slot == null || slot.ContainedItem != null) return;
                var tpl = BotGuns[UnityEngine.Random.Range(0, BotGuns.Length)];
                var w = Singleton<ItemFactory>.Instance.GetPresetItem(tpl) as Weapon;
                if (w == null) return;
                var mag = w.GetCurrentMagazine();
                var ammoTpl = w.Template.DefAmmoTemplate;
                if (mag != null && ammoTpl != null && !(mag is CylinderMagazine) && mag.Count < mag.MaxCount)
                {
                    var ammo = Singleton<ItemFactory>.Instance.CreateItem(Guid.NewGuid().ToString("N").Substring(0, 24), ammoTpl._id, null);
                    if (ammo != null) { ammo.StackObjectsCount = mag.MaxCount - mag.Count; mag.Cartridges.Add(ammo, false); }
                }
                var r = slot.AddWithoutRestrictions(w);
                if (r.Failed) { Dbg.Log("BOTS", "Mule Kick: no cabe la 3.a arma: " + r.Error); return; }
                Dbg.Log("BOTS", bot.Profile.Nickname + " lleva una 3.a arma (" + w.ShortName + ")");
            }
            catch (Exception e) { Dbg.Throttled("mkbot", 10f, "ERROR", "Mule Kick (bot): " + e.Message); }
        }

        // ---------------------------------------------------------------- slot por nombre (constructor del equipo)
        [ThreadStatic] internal static int CtorCount;

        public static object ParseSlot(Type type, string value)
        {
            if (type == typeof(EquipmentSlot))
            {
                if (value == SlotName)
                {
                    int i = Math.Max(CtorCount - 1, 15);
                    SlotIndex = i;
                    return (EquipmentSlot)i;
                }
                if (value == "HolsterLeft") return (EquipmentSlot)15;            // DualWieldLab (misma asignacion que su parche)
            }
            return Enum.Parse(type, value);
        }
    }

    // ===================================================================================================================
    //  Parches de la ranura
    // ===================================================================================================================

    // El constructor del equipo convierte el nombre de cada ranura en un indice con Enum.Parse, que falla con "MuleKick".
    // Prioridad ultima: DualWieldLab cambia antes Enum.Parse por el suyo, y luego el nuestro sustituye al que haya.
    [HarmonyPatch]
    internal static class MuleSlotCtorPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Constructor(typeof(InventoryEquipment), new[] { typeof(string), typeof(InventoryEquipmentTemplate) });
        }

        [HarmonyPrefix]
        private static void Prefix(InventoryEquipmentTemplate template)
        {
            MuleKick.CtorCount = template != null && template.Slots != null ? template.Slots.Length : 0;
        }

        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var parse = AccessTools.Method(typeof(Enum), "Parse", new[] { typeof(Type), typeof(string) });
            var ours = AccessTools.Method(typeof(MuleKick), nameof(MuleKick.ParseSlot));
            int n = 0;
            foreach (var ins in instructions)
            {
                var m = ins.operand as MethodInfo;
                if ((ins.opcode == OpCodes.Call || ins.opcode == OpCodes.Callvirt) && m != null
                    && (m == parse || (m.Name == "ParseSlot" && m.ReturnType == typeof(object) && m.GetParameters().Length == 2 && m.DeclaringType != typeof(MuleKick))))
                {
                    ins.opcode = OpCodes.Call;
                    ins.operand = ours;
                    n++;
                }
                yield return ins;
            }
            if (n != 1) Plugin.Log.LogWarning("[Mule Kick] el constructor del equipo ha cambiado: " + n + " llamadas sustituidas (esperaba 1)");
        }
    }

    // la ranura cuenta para el peso y el resto de listas de ranuras del equipo
    [HarmonyPatch(typeof(InventoryEquipmentExtension), nameof(InventoryEquipmentExtension.GetAllSlots))]
    internal static class MuleAllSlotsPatch
    {
        [HarmonyPostfix]
        private static void Postfix(InventoryEquipment equipment, ref IEnumerable<Slot> __result)
        {
            var s = MuleKick.SlotOf(equipment);
            if (s != null) __result = __result.Concat(new[] { s });
        }
    }

    // el arma de la ranura se puede poner en las manos (solo con la perk activa)
    [HarmonyPatch(typeof(InventoryController), nameof(InventoryController.IsAtReachablePlace))]
    internal static class MuleReachablePatch
    {
        [HarmonyPostfix]
        private static void Postfix(InventoryController __instance, Item item, ref bool __result)
        {
            if (__result || !(item is Weapon) || !MuleKick.IsMule(item) || !MuleKick.Active) return;
            var slot = MuleKick.SlotOf(__instance.Inventory.Equipment);
            if (slot == null || !ReferenceEquals(item.Parent.Container, slot)) return;
            var ci = item as CompoundItem;
            if (ci != null && ci.MissingVitalParts.Any()) return;
            __result = __instance.Examined(item);
        }
    }

    // sin la perk (o en el equipo de otro), la ranura no admite nada nuevo
    [HarmonyPatch(typeof(Slot), nameof(Slot.CheckConditions))]
    internal static class MuleAcceptPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Slot __instance, Item item, bool ignoreRestrictions, ref Option<bool> __result)
        {
            if (ignoreRestrictions || __result.Failed || __instance.ID != MuleKick.SlotName) return;
            var eq = __instance.ParentItem as InventoryEquipment;
            if (MuleKick.IsOwn(eq) && MuleKick.Available) return;
            __result = new Slot.ItemFiltersWontAllowError(item, __instance);
        }
    }

    // antes de sacar un arma: Chamber Ready / Empty Chamber / APEX
    [HarmonyPatch(typeof(Player), nameof(Player.TryProceed))]
    internal static class MuleDrawPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Player __instance, Item item)
        {
            if (!Perks.AnyActive || !(item is Weapon) || !Util.IsMe(__instance)) return;
            try { MuleKick.BeforeDraw(__instance, (Weapon)item); }
            catch (Exception e) { Dbg.Throttled("mkdraw", 10f, "ERROR", "Mule Kick (sacar): " + e.Message); }
        }
    }

    // Safety On
    [HarmonyPatch(typeof(Player.FirearmController), nameof(Player.FirearmController.SetTriggerPressed))]
    internal static class MuleSafetyPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Player.FirearmController __instance, ref bool pressed)
        {
            if (!MuleKick.Active || !Util.IsMe(__instance._player)) return;
            if (!MuleKick.AllowTrigger(__instance, pressed)) pressed = false;
        }
    }

    // Tangled
    [HarmonyPatch(typeof(Player.FirearmController), nameof(Player.FirearmController.CanStartReload))]
    internal static class MuleTangledPatch
    {
        [HarmonyPostfix]
        private static void Postfix(Player.FirearmController __instance, ref bool __result)
        {
            if (__result && MuleKick.Active && Util.IsMe(__instance._player) && MuleKick.BlockReload(__instance._player, __instance.Item)) __result = false;
        }
    }

    // rueda del raton: con un arma en la ranura de Mule Kick, cambia de arma en vez de la velocidad al andar
    [HarmonyPatch(typeof(MoveInputTranslator), nameof(MoveInputTranslator.TranslateCommand))]
    internal static class MuleWheelPatch
    {
        [HarmonyPrefix]
        private static bool Prefix(MoveInputTranslator __instance, ECommand command, ref InputNode.ETranslateResult __result)
        {
            if (command != ECommand.ScrollNext && command != ECommand.ScrollPrevious) return true;
            if (!MuleKick.Active) return true;
            var me = __instance._controller;
            if (!Util.IsMe(me)) return true;
            try
            {
                if (!MuleKick.Cycle(me, command == ECommand.ScrollNext ? 1 : -1)) return true;
            }
            catch (Exception e) { Dbg.Throttled("mkwheel", 10f, "ERROR", "Mule Kick (rueda): " + e.Message); return true; }
            __result = InputNode.ETranslateResult.Ignore;
            return false;
        }
    }

    // ===================================================================================================================
    //  Inventario: una 3.a fila de arma larga bajo las otras dos (clon de la ranura "a la espalda")
    // ===================================================================================================================
    [HarmonyPatch(typeof(EquipmentTab), nameof(EquipmentTab.Show))]
    internal static class MuleTabPatch
    {
        private const string CloneName = "TarkovaCola_MuleKickSlot";
        private static readonly Dictionary<RectTransform, KeyValuePair<Vector2, Vector2>> Native = new Dictionary<RectTransform, KeyValuePair<Vector2, Vector2>>();

        [HarmonyPrefix]
        private static void Prefix(EquipmentTab __instance, InventoryEquipment equipment, Dictionary<EquipmentSlot, SlotView> ____slotViews)
        {
            try
            {
                var views = ____slotViews;
                var sec = __instance._seconaryWeaponSlot;
                var pri = __instance._primaryWeaponSlot;
                if (views == null || sec == null || pri == null || MuleKick.SlotIndex < 0) return;
                var key = MuleKick.SlotEnum;
                var slot = MuleKick.SlotOf(equipment);
                bool show = slot != null && (slot.ContainedItem != null || (MuleKick.IsOwn(equipment) && MuleKick.Available));

                var tr = sec.transform.parent != null ? sec.transform.parent.Find(CloneName) : null;
                SlotView view = tr != null ? tr.GetComponent<SlotView>() : null;
                var priRt = pri.transform as RectTransform;
                var secRt = sec.transform as RectTransform;
                Remember(priRt); Remember(secRt);
                Restore(priRt); Restore(secRt);

                if (!show)
                {
                    views.Remove(key);
                    if (view != null) view.gameObject.SetActive(false);
                    return;
                }
                if (view == null)
                {
                    var go = UnityEngine.Object.Instantiate(sec.gameObject, sec.transform.parent, false);
                    go.name = CloneName;
                    view = go.GetComponent<SlotView>();
                    Relabel(go);
                }
                view.gameObject.SetActive(true);
                views[key] = view;
                Stack(priRt, secRt, view.transform as RectTransform);
            }
            catch (Exception e) { Dbg.Throttled("mktab", 10f, "ERROR", "Mule Kick (inventario): " + e); }
        }

        [HarmonyPostfix]
        private static void Postfix(EquipmentTab __instance)
        {
            try
            {
                var sec = __instance._seconaryWeaponSlot;
                var tr = sec != null && sec.transform.parent != null ? sec.transform.parent.Find(CloneName) : null;
                if (tr != null && tr.gameObject.activeSelf) Relabel(tr.gameObject);
            }
            catch (Exception e) { Dbg.Throttled("mktab2", 10f, "ERROR", "Mule Kick (inventario): " + e.Message); }
        }

        // las dos ranuras largas y la nueva se reparten el mismo alto, una debajo de otra
        private static void Stack(RectTransform a, RectTransform b, RectTransform mule)
        {
            if (a == null || b == null || mule == null) return;
            var ra = WorldRect(a); var rb = WorldRect(b);
            var upper = ra.yMax >= rb.yMax ? a : b; var lower = ReferenceEquals(upper, a) ? b : a;
            var ru = WorldRect(upper); var rl = WorldRect(lower);
            float top = ru.yMax, bottom = rl.yMin;
            float gap = Mathf.Max(0f, ru.yMin - rl.yMax);
            float h = (top - bottom - 2f * gap) / 3f;
            if (h <= 1f) return;
            mule.anchorMin = lower.anchorMin; mule.anchorMax = lower.anchorMax; mule.pivot = lower.pivot;
            mule.sizeDelta = lower.sizeDelta; mule.anchoredPosition = lower.anchoredPosition;
            SetSpanY(upper, top - h, top);
            SetSpanY(lower, top - 2f * h - gap, top - h - gap);
            SetSpanY(mule, bottom, bottom + h);
        }

        private static void SetSpanY(RectTransform view, float yMin, float yMax)
        {
            float s = Mathf.Max(0.0001f, view.parent.lossyScale.y);
            view.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (yMax - yMin) / s);
            var r = WorldRect(view);
            view.anchoredPosition += new Vector2(0f, (yMin - r.yMin) / s);
        }

        private static Rect WorldRect(RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x, c[0].y, c[2].x, c[2].y);
        }

        private static void Remember(RectTransform rt)
        {
            if (rt == null || Native.ContainsKey(rt)) return;
            foreach (var dead in Native.Keys.Where(k => k == null).ToList()) Native.Remove(dead);
            Native[rt] = new KeyValuePair<Vector2, Vector2>(rt.sizeDelta, rt.anchoredPosition);
        }

        private static void Restore(RectTransform rt)
        {
            KeyValuePair<Vector2, Vector2> v;
            if (rt != null && Native.TryGetValue(rt, out v)) { rt.sizeDelta = v.Key; rt.anchoredPosition = v.Value; }
        }

        // el titulo de la ranura clonada: los textos traducidos por el juego pasan a decir "MULE KICK"
        private static void Relabel(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<TMP_Text>(true))
            {
                var loc = t.GetComponents<MonoBehaviour>().Where(c => c != null && c.GetType().Name.StartsWith("Localized")).ToList();
                if (loc.Count == 0) continue;
                if (t.GetComponentInParent<ItemView>() != null) continue;      // no tocar el nombre del arma que haya dentro
                foreach (var c in loc) c.enabled = false;
                t.text = L.T("mule.slot");
            }
        }
    }
}
