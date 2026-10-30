using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Perks de los companeros de PIT Fireteam: al abrir su inventario (darle o cambiarle equipo) aparece abajo un panel con 4 casillas.
    //  Clic en una casilla vacia: eliges una de las perks que llevas encima y se le equipa (la botella se gasta al cerrar el inventario).
    //  Clic en una ocupada: se la quitas y la botella vuelve a tu equipo. Las perks se guardan por companero (pit_perks.json) y se
    //  pierden si muere. BotPerks aplica sus efectos.
    //  Los cambios de botellas se hacen despues de cerrar el inventario de PIT: mientras esta abierto trabaja sobre una copia del
    //  equipo y no se debe tocar el de verdad.
    // ============================================================================================
    internal class PitPerks : MonoBehaviour
    {
        private const float Slot = 64f, Gap = 10f, Pad = 14f;

        private class Saved { public string Name; public List<string> Perks = new List<string>(); }
        private static Dictionary<string, Saved> _store;
        private static string File_ { get { return Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location), "pit_perks.json"); } }

        // botellas pendientes: + = gastar una tuya (se equipo), - = devolvertela (se quito)
        private static readonly List<KeyValuePair<string, string>> _take = new List<KeyValuePair<string, string>>();   // (companero, perk)
        private static readonly List<string> _giveBack = new List<string>();
        private static float _applyAt;

        private static PitPerks _inst;
        private static PropertyInfo _current;
        private static FieldInfo _bot, _cmd;
        private static bool _looked;

        private RectTransform _root;
        private string _sig, _follower, _picking;

        // ---------------------------------------------------------------- datos
        private static Dictionary<string, Saved> Store
        {
            get
            {
                if (_store != null) return _store;
                try { _store = File.Exists(File_) ? JsonConvert.DeserializeObject<Dictionary<string, Saved>>(File.ReadAllText(File_)) : null; }
                catch (Exception e) { Plugin.Log.LogWarning("Perks de PIT: no se pudo leer pit_perks.json (" + e.Message + ")"); }
                return _store ?? (_store = new Dictionary<string, Saved>());
            }
        }

        private static void Save()
        {
            try { File.WriteAllText(File_, JsonConvert.SerializeObject(Store, Formatting.Indented)); }
            catch (Exception e) { Plugin.Log.LogWarning("Perks de PIT: no se pudo guardar (" + e.Message + ")"); }
        }

        // perks equipadas de un companero (vacio si no tiene)
        internal static IList<string> Of(string profileId)
        {
            Saved s;
            return profileId != null && Store.TryGetValue(profileId, out s) ? (IList<string>)s.Perks : new string[0];
        }

        // muere: pierde sus perks
        internal static void OnDeath(Player p)
        {
            Saved s;
            if (p == null || !Store.TryGetValue(p.ProfileId, out s) || s.Perks.Count == 0) return;
            Dbg.Log("BOTPERKS", "companero muerto, pierde sus perks: " + s.Name + " (" + string.Join(", ", s.Perks.ToArray()) + ")");
            Hud.Notify(L.T("pit.title").ToUpperInvariant(), L.F("pit.lost", s.Name), s.Perks[0]);
            Store.Remove(p.ProfileId); Save();
        }

        // ---------------------------------------------------------------- inventario de PIT abierto (por reflexion)
        private static BotOwner OpenFor()
        {
            if (!_looked)
            {
                _looked = true;
                var t = AccessTools.TypeByName("pitTeam.Modules.TeammateGearSwap");
                if (t != null) { _current = AccessTools.Property(t, "Current"); _bot = AccessTools.Field(t, "Bot"); _cmd = AccessTools.Field(t, "CommandLoot"); }
            }
            if (_current == null || _bot == null) return null;
            try
            {
                var cur = _current.GetValue(null, null);
                if (cur == null || (_cmd != null && (bool)_cmd.GetValue(cur))) return null;
                return _bot.GetValue(cur) as BotOwner;
            }
            catch { return null; }
        }

        // Plugin.Update
        internal static void Tick()
        {
            if (!Plugin.InRaidWorld || !Plugin.CfgBotPerks.Value) { if (_inst != null) { Destroy(_inst.gameObject); _inst = null; } return; }
            var bot = OpenFor();
            if (bot != null && bot.Profile != null)
            {
                if (_inst == null) { UiKit.EnsureAssets(); _inst = new GameObject("TarkovaCola_PitPerks").AddComponent<PitPerks>(); _inst.Init(); }
                _inst.Show(bot);
                _applyAt = Time.unscaledTime + 1f;
            }
            else
            {
                if (_inst != null) _inst.Hide();
                if ((_take.Count > 0 || _giveBack.Count > 0) && Time.unscaledTime >= _applyAt) ApplyBottles();
            }
        }

        // ya cerrado el inventario de PIT: se gastan o devuelven las botellas
        private static void ApplyBottles()
        {
            var me = Plugin.Me;
            if (me == null) return;
            foreach (var kv in _take)
            {
                var def = Perks.Get(kv.Value);
                var it = def == null ? null : me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment).FirstOrDefault(i => i != null && i.StringTemplateId == def.ItemTpl);
                if (it != null)
                {
                    var op = ItemManipulator.Remove(it, me.InventoryController, true);
                    if (op.Succeeded) { me.InventoryController.TryRunNetworkTransaction(op); continue; }
                    Plugin.Log.LogWarning("Perks de PIT: no se pudo gastar la botella (" + op.Error + ")");
                }
                // la botella ya no esta (se la diste en el inventario): se le quita la perk
                Saved s;
                if (Store.TryGetValue(kv.Key, out s)) s.Perks.Remove(kv.Value);
                Hud.Notify(L.T("pit.title").ToUpperInvariant(), L.F("pit.nobottle", def != null ? def.Name : kv.Value), kv.Value);
            }
            foreach (var id in _giveBack)
            {
                var def = Perks.Get(id);
                if (def != null && !NewPerkUtil.Give(me.Profile, def.ItemTpl)) Hud.Notify(L.T("pit.title").ToUpperInvariant(), L.F("pit.nospace", def.Name), id);
            }
            _take.Clear(); _giveBack.Clear();
            Save();
        }

        // ---------------------------------------------------------------- panel
        private void Init()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 29500;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            gameObject.AddComponent<GraphicRaycaster>();
            _root = UiKit.Rect(transform, "Root");
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0f);
            _root.pivot = new Vector2(0.5f, 0f);
            float s = Screen.height / 1080f;
            _root.localScale = new Vector3(s, s, 1f);
            _root.anchoredPosition = new Vector2(0f, 16f * s);
        }

        private void Hide()
        {
            if (!gameObject.activeSelf) return;
            _picking = null; _sig = null;
            gameObject.SetActive(false);
        }

        private void Show(BotOwner bot)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            if (_follower != bot.ProfileId) { _follower = bot.ProfileId; _picking = null; }
            string name = bot.Profile.Nickname;
            var mine = Of(_follower);
            var have = Bottles();
            string sig = _follower + "|" + string.Join(",", mine.ToArray()) + "|" + _picking + "|" + string.Join(",", have.Select(x => x.Key + x.Value).ToArray());
            if (sig == _sig) return;
            _sig = sig;
            foreach (Transform c in _root) Destroy(c.gameObject);
            Build(name, mine, have);
        }

        // perks que llevas encima (descontando las ya prometidas)
        private static List<KeyValuePair<string, int>> Bottles()
        {
            var r = new List<KeyValuePair<string, int>>();
            var me = Plugin.Me;
            if (me == null) return r;
            var count = new Dictionary<string, int>();
            foreach (var it in me.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment))
            {
                var def = it == null ? null : Perks.ByItem(it.StringTemplateId);
                if (def == null) continue;
                int n; count.TryGetValue(def.Id, out n); count[def.Id] = n + it.StackObjectsCount;
            }
            foreach (var kv in _take) { int n; if (count.TryGetValue(kv.Value, out n)) count[kv.Value] = n - 1; }
            foreach (var kv in count) if (kv.Value > 0) r.Add(kv);
            return r;
        }

        private void Build(string name, IList<string> mine, List<KeyValuePair<string, int>> have)
        {
            int max = BotPerks.MaxFollowerPerks;
            float w = Pad * 2 + max * Slot + (max - 1) * Gap, h = 40 + Slot + Pad + 22;
            float pickH = 0f;
            var choices = have.Where(x => !mine.Contains(x.Key)).ToList();
            if (_picking != null) pickH = 12 + Slot * 0.8f + 34;
            float pw = Mathf.Max(w, Pad * 2 + Mathf.Max(1, choices.Count) * (Slot * 0.8f + Gap) - Gap);
            _root.sizeDelta = new Vector2(Mathf.Max(w, pw), h + pickH);

            // lista para elegir (encima de las casillas)
            if (_picking != null)
            {
                var pp = UiKit.Panel(_root, (_root.sizeDelta.x - pw) / 2f, 0, pw, pickH - 8, UiKit.Panel2, UiKit.Gold, 1f);
                Txt(pp.transform, choices.Count > 0 ? L.T("pit.pick") : L.T("pit.none"), Pad, 8, pw - Pad * 2, 20, 15, UiKit.Gold, true);
                for (int i = 0; i < choices.Count; i++)
                {
                    var c = choices[i]; float x = Pad + i * (Slot * 0.8f + Gap), sz = Slot * 0.8f;
                    var cell = UiKit.Panel(pp.transform, x, 32, sz, sz, UiKit.C(10, 20, 30, 0.9f), UiKit.PerkColor(c.Key), 1f);
                    var ic = Hud.IconSprite(c.Key);
                    if (ic != null) UiKit.Place(UiKit.Img(cell.transform, "Icon", Color.white, ic).rectTransform, 6, 6, sz - 12, sz - 12);
                    if (c.Value > 1) Txt(cell.transform, "x" + c.Value, 0, sz - 18, sz - 4, 16, 13, UiKit.White, true, TextAlignmentOptions.TopRight);
                    string perk = c.Key;
                    UiButton.Make(pp.transform, x, 32, sz, sz, () => Equip(perk), UiKit.C(255, 255, 255, 0f), UiKit.C(255, 255, 255, 0.12f));
                }
            }

            float top = pickH, left = (_root.sizeDelta.x - w) / 2f;
            var panel = UiKit.Panel(_root, left, top, w, h, UiKit.PanelBg, UiKit.Line, 1f);
            Txt(panel.transform, L.F("pit.header", name).ToUpperInvariant(), Pad, 10, w - Pad * 2, 22, 17, UiKit.White, true);
            for (int i = 0; i < max; i++)
            {
                float x = Pad + i * (Slot + Gap), y = 40;
                string perk = i < mine.Count ? mine[i] : null;
                var cell = UiKit.Panel(panel.transform, x, y, Slot, Slot, UiKit.C(18, 34, 50, 0.9f), perk != null ? UiKit.PerkColor(perk) : UiKit.Line, perk != null ? 2f : 1f);
                if (perk != null)
                {
                    var ic = Hud.IconSprite(perk);
                    if (ic != null) UiKit.Place(UiKit.Img(cell.transform, "Icon", Color.white, ic).rectTransform, 7, 7, Slot - 14, Slot - 14);
                    UiButton.Make(panel.transform, x, y, Slot, Slot, () => Unequip(perk), UiKit.C(255, 255, 255, 0f), UiKit.C(255, 90, 90, 0.18f));
                }
                else
                {
                    Txt(cell.transform, "+", 0, 12, Slot, 40, 32, _picking != null ? UiKit.Gold : UiKit.Dim, true, TextAlignmentOptions.Top);
                    UiButton.Make(panel.transform, x, y, Slot, Slot, () => { _picking = _picking == null ? "x" : null; _sig = null; }, UiKit.C(255, 255, 255, 0f), UiKit.C(255, 255, 255, 0.10f));
                }
            }
            Txt(panel.transform, L.T("pit.hint"), Pad, 40 + Slot + 6, w - Pad * 2, 18, 12, UiKit.Dim, false);
        }

        private void Equip(string perk)
        {
            Saved s;
            if (!Store.TryGetValue(_follower, out s)) Store[_follower] = s = new Saved();
            s.Name = OpenFor()?.Profile?.Nickname ?? s.Name;
            if (s.Perks.Count >= BotPerks.MaxFollowerPerks || s.Perks.Contains(perk)) return;
            s.Perks.Add(perk);
            _take.Add(new KeyValuePair<string, string>(_follower, perk));
            _picking = null; _sig = null;
            UiKit.Sound(EUISoundType.ButtonClick);
            BotPerks.ScanSoon(_follower);
            Save();
        }

        private void Unequip(string perk)
        {
            Saved s;
            if (!Store.TryGetValue(_follower, out s) || !s.Perks.Remove(perk)) return;
            // si la botella aun no se habia gastado, basta con no gastarla
            int i = _take.FindIndex(kv => kv.Key == _follower && kv.Value == perk);
            if (i >= 0) _take.RemoveAt(i); else _giveBack.Add(perk);
            _sig = null;
            UiKit.Sound(EUISoundType.ButtonClick);
            BotPerks.ScanSoon(_follower);
            Save();
        }

        private static TextMeshProUGUI Txt(Transform p, string s, float x, float y, float w, float h, float size, Color c, bool bold, TextAlignmentOptions a = TextAlignmentOptions.TopLeft)
        {
            var t = UiKit.Txt(p, s, size, c, a, false, bold); UiKit.Place(t.rectTransform, x, y, w, h); return t;
        }
    }
}
