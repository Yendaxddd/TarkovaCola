using System.Collections.Generic;
using System.Linq;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TarkovaCola.Client
{
    // Pantalla de Research (progreso y equipamiento de perks) construida con uGUI a la manera de EFT:
    // fuente Bender del juego, sonidos de UI del juego y el mismo lenguaje visual que el resto del menu.
    // Todo se dibuja en un lienzo de referencia de 1920x1080 escalado a la resolucion real.
    internal class ResearchScreen : MonoBehaviour
    {
        private const float W = 1920f, H = 1080f;
        private static ResearchScreen _inst;

        internal static bool IsOpen { get { return _inst != null; } }

        internal static void Toggle()
        {
            Dbg.Log("MENU", "ResearchScreen.Toggle (abierta=" + IsOpen + ", enRaid=" + Plugin.InRaidWorld + ")");
            if (_inst != null) { _inst.Close(); return; }
            if (Plugin.InRaidWorld) return;                       // el menu de investigacion es del menu principal
            Open();
        }

        private static void Open()
        {
            GameObject go = null;
            try
            {
                PerkService.EnsureLoaded(force: true);
                Achievements.CheckState();
                Achievements.ClaimPending();
                UiKit.EnsureAssets();
                go = new GameObject("TarkovaCola_Research");
                _inst = go.AddComponent<ResearchScreen>();
                _inst.Build();
                UiKit.Sound(EUISoundType.MenuInspectorWindowOpen);
                Dbg.Log("MENU", "pantalla Research abierta (idioma " + (L.Spanish ? "es" : "en") + ")");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("No se pudo abrir la pantalla Research: " + e);
                _inst = null;
                if (go != null) Destroy(go);
            }
        }

        // ---------------------------------------------------------------- estado
        private string _view = "progress";      // progress | equip
        private string _sel;                     // nodo seleccionado en Progreso
        private bool _animate = true;
        private bool _closing;
        private float _fade;                     // 0..1 (aparicion / desaparicion de toda la pantalla)

        private CanvasGroup _rootGroup;
        private RectTransform _content, _header, _left, _stage;
        private CanvasGroup _toastGroup;
        private TextMeshProUGUI _toastText;
        private float _toastUntil;

        private class Anim { public RectTransform Rt; public CanvasGroup Cg; public float T0, Dur = 0.4f, Dy; public Vector2 Base; }
        private class FillAnim { public Image Img; public float To, T0; }
        private readonly List<Anim> _anims = new List<Anim>();
        private readonly List<FillAnim> _fills = new List<FillAnim>();
        private int _idx;

        // ---------------------------------------------------------------- construccion
        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            gameObject.AddComponent<GraphicRaycaster>();
            _rootGroup = gameObject.AddComponent<CanvasGroup>();
            _rootGroup.alpha = 0f;

            // fondo que tapa y bloquea el menu de debajo
            var dim = UiKit.Img(transform, "Dim", UiKit.C(3, 8, 13, 0.94f), null, raycast: true);
            UiKit.Stretch(dim.rectTransform);
            var grad = UiKit.Img(transform, "Glow", UiKit.C(20, 90, 60, 0.10f));
            grad.rectTransform.anchorMin = new Vector2(0.55f, 0f); grad.rectTransform.anchorMax = Vector2.one;
            grad.rectTransform.offsetMin = grad.rectTransform.offsetMax = Vector2.zero;

            // contenido 1920x1080 centrado y escalado a la pantalla (sin deformar)
            _content = UiKit.Rect(transform, "Content");
            _content.anchorMin = _content.anchorMax = new Vector2(0.5f, 0.5f);
            _content.pivot = new Vector2(0.5f, 0.5f);
            _content.sizeDelta = new Vector2(W, H);
            float s = Mathf.Min(Screen.width / W, Screen.height / H);
            _content.localScale = new Vector3(s, s, 1f);

            _header = UiKit.Rect(_content, "Header"); UiKit.Stretch(_header);
            _left = UiKit.Rect(_content, "Left"); UiKit.Stretch(_left);
            _stage = UiKit.Rect(_content, "Stage");
            UiKit.Place(_stage, 420, 120, 1440, 930);

            var toast = UiKit.Panel(_content, W / 2 - 300, 1010, 600, 40, UiKit.C(58, 18, 16, 0.96f), UiKit.Red);
            _toastGroup = toast.gameObject.AddComponent<CanvasGroup>(); _toastGroup.alpha = 0f;
            _toastText = UiKit.Txt(toast.transform, "", 16, UiKit.C(255, 217, 213), TextAlignmentOptions.Center);
            UiKit.Stretch(_toastText.rectTransform);

            Rebuild(true);
        }

        private void Rebuild(bool animate)
        {
            _animate = animate; _idx = 0;
            _anims.Clear(); _fills.Clear();
            foreach (var r in new[] { _header, _left, _stage })
                for (int i = r.childCount - 1; i >= 0; i--) Destroy(r.GetChild(i).gameObject);

            BuildHeader();
            BuildLeft();
            BuildHead();
            if (_view == "progress") BuildProgress(); else BuildEquip();
        }

        // ---------------------------------------------------------------- utilidades de layout / animacion
        private TextMeshProUGUI T(Transform p, string s, float x, float y, float w, float h, float size, Color c,
            TextAlignmentOptions a = TextAlignmentOptions.TopLeft, bool wrap = false, bool bold = false)
        {
            var t = UiKit.Txt(p, s, size, c, a, wrap, bold);
            UiKit.Place(t.rectTransform, x, y, w, h);
            return t;
        }

        // Aparicion escalonada (fade + subida corta). Solo si la reconstruccion pide animar.
        private void Enter(RectTransform rt, float extraDelay = 0f)
        {
            if (!_animate) { _idx++; return; }
            var cg = rt.gameObject.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            _anims.Add(new Anim { Rt = rt, Cg = cg, T0 = Time.unscaledTime + 0.15f + _idx * 0.045f + extraDelay, Dy = 16f, Base = rt.anchoredPosition });
            _idx++;
        }

        private Image Bar(Transform p, float x, float y, float w, float h, float amount, Color fill)
        {
            var back = UiKit.Img(p, "BarBack", UiKit.C(255, 255, 255, 0.10f)); UiKit.Place(back.rectTransform, x, y, w, h);
            var f = UiKit.Img(back.transform, "BarFill", fill);
            UiKit.Stretch(f.rectTransform);
            f.type = Image.Type.Filled; f.fillMethod = Image.FillMethod.Horizontal; f.fillOrigin = 0;
            amount = Mathf.Clamp01(amount);
            if (_animate) { f.fillAmount = 0f; _fills.Add(new FillAnim { Img = f, To = amount, T0 = Time.unscaledTime + 0.4f }); }
            else f.fillAmount = amount;
            return f;
        }

        private void Toast(string msg)
        {
            _toastText.text = msg;
            _toastUntil = Time.unscaledTime + 2.6f;
        }

        private void Update()
        {
            float now = Time.unscaledTime;

            // aparicion / desaparicion de toda la pantalla
            _fade = Mathf.Clamp01(_fade + (_closing ? -1f : 1f) * Time.unscaledDeltaTime / 0.22f);
            _rootGroup.alpha = _fade;
            if (_closing && _fade <= 0f) { _inst = null; Destroy(gameObject); return; }

            if (Plugin.InRaidWorld) { _inst = null; Destroy(gameObject); return; }
            if (!_closing && Input.GetKeyDown(KeyCode.Escape)) { UiKit.Sound(EUISoundType.MenuEscape); Close(); }

            for (int i = _anims.Count - 1; i >= 0; i--)
            {
                var a = _anims[i];
                if (a.Rt == null) { _anims.RemoveAt(i); continue; }
                float p = Mathf.Clamp01((now - a.T0) / a.Dur), e = 1f - Mathf.Pow(1f - p, 3f);
                a.Cg.alpha = e;
                a.Rt.anchoredPosition = a.Base + new Vector2(0f, -a.Dy * (1f - e));
                if (p >= 1f) _anims.RemoveAt(i);
            }
            for (int i = _fills.Count - 1; i >= 0; i--)
            {
                var f = _fills[i];
                if (f.Img == null) { _fills.RemoveAt(i); continue; }
                float p = Mathf.Clamp01((now - f.T0) / 0.9f), e = 1f - Mathf.Pow(1f - p, 3f);
                f.Img.fillAmount = f.To * e;
                if (p >= 1f) _fills.RemoveAt(i);
            }

            float ta = _toastUntil > now ? 1f : 0f;
            _toastGroup.alpha = Mathf.MoveTowards(_toastGroup.alpha, ta, Time.unscaledDeltaTime * 5f);
        }

        internal void Close()
        {
            if (_closing) return;
            _closing = true;
            UiKit.Sound(EUISoundType.MenuInspectorWindowClose);
            Dbg.Log("MENU", "pantalla Research cerrada");
        }

        // ---------------------------------------------------------------- cabecera y columna izquierda
        private void BuildHeader()
        {
            T(_header, L.T("augments"), 60, 26, 400, 20, 14, UiKit.Dim);
            T(_header, L.T(_view == "progress" ? "title.progress" : "title.equip"), 60, 42, 600, 56, 44, UiKit.White, TextAlignmentOptions.TopLeft, false, true);

            Tab("progress", L.T("nav.progress"), 700);
            Tab("equip", L.T("nav.equip"), 900);

            int lv = PerkService.Level;
            var chip = UiKit.Panel(_header, 1540, 46, 150, 40, UiKit.PanelBg, UiKit.Line);
            var ct = UiKit.Txt(chip.transform, L.T("level") + "  " + lv, 20, UiKit.C(75, 226, 124), TextAlignmentOptions.Center, false, true);
            UiKit.Stretch(ct.rectTransform);

            var close = UiButton.Make(_header, 1730, 46, 40, 40, Close, UiKit.C(18, 34, 50, 0.9f), UiKit.C(120, 30, 30, 0.95f));
            var x = UiKit.Txt(close.transform, "X", 22, UiKit.White, TextAlignmentOptions.Center, false, true);
            UiKit.Stretch(x.rectTransform);
        }

        private void Tab(string view, string label, float x)
        {
            bool on = _view == view;
            var b = UiButton.Make(_header, x, 40, 170, 50, () => { if (_view != view) { _view = view; Rebuild(true); } },
                                  UiKit.C(255, 255, 255, 0f), UiKit.C(255, 255, 255, 0.06f));
            var t = UiKit.Txt(b.transform, label, 22, on ? UiKit.White : UiKit.Dim, TextAlignmentOptions.Center, false, on);
            UiKit.Stretch(t.rectTransform);
            if (on) UiKit.Place(UiKit.Img(b.transform, "Underline", UiKit.Cyan).rectTransform, 15, 46, 140, 3);
        }

        private void BuildLeft()
        {
            var btn = UiKit.Panel(_left, 60, 150, 320, 64, UiKit.C(31, 138, 76, 0.85f), UiKit.C(109, 240, 160, 0.9f));
            if (Hud.IconSprite != null)
            {
                var ic = UiKit.Img(btn.transform, "Icon", Color.white, Hud.IconSprite); UiKit.Place(ic.rectTransform, 12, 12, 40, 40);
            }
            T(btn.transform, L.T("perk.speedcola"), 66, 20, 180, 26, 20, UiKit.White, TextAlignmentOptions.TopLeft, false, true);
            T(btn.transform, PerkService.Percent + "%", 240, 22, 70, 22, 16, UiKit.C(234, 255, 241), TextAlignmentOptions.TopRight);
            if (_animate) { _idx = 0; Enter(btn.rectTransform); }
        }

        // cabecera de la perk: icono, nombre, descripcion y barra de XP (comun a las dos pantallas)
        private void BuildHead()
        {
            int lv, cur, max; PerkService.LevelInfo(PerkService.Xp, out lv, out cur, out max);
            var head = UiKit.Rect(_stage, "Head"); UiKit.Place(head, 0, 0, 1440, 140);
            if (Hud.IconSprite != null)
            {
                UiKit.Panel(head, 0, 0, 100, 100, UiKit.C(0, 0, 0, 0), UiKit.C(120, 190, 225, 0.5f), 2f);
                var ic = UiKit.Img(head, "PerkIcon", Color.white, Hud.IconSprite); UiKit.Place(ic.rectTransform, 5, 5, 90, 90);
            }
            T(head, L.T("perk.speedcola"), 124, -2, 800, 54, 46, UiKit.White, TextAlignmentOptions.TopLeft, false, true);
            T(head, L.T("perk.speedcola.desc"), 124, 54, 1000, 44, 16, UiKit.Text, TextAlignmentOptions.TopLeft, true);
            T(head, L.T("level") + " " + lv, 124, 104, 300, 20, 14, UiKit.Green, TextAlignmentOptions.TopLeft, false, true);
            T(head, PerkService.IsMax ? L.T("xp.max") : cur + " / " + max + " XP", 124, 104, 560, 20, 14, PerkService.IsMax ? UiKit.Gold : UiKit.Dim, TextAlignmentOptions.TopRight, false, PerkService.IsMax);
            Bar(head, 124, 126, 560, 10, cur / (float)max, PerkService.IsMax ? UiKit.Gold : UiKit.C(31, 174, 93));
            Enter(head);
        }

        // ---------------------------------------------------------------- pantalla PROGRESO
        private static string ClassOf(AugDef a)
        {
            return a.Kind == NodeKind.Major ? "am" : a.Kind == NodeKind.Minor ? "an" : "sp";
        }

        private static string State(AugDef a)
        {
            return PerkService.IsDone(a.Id) ? "done" : PerkService.Level >= a.Lvl ? "active" : "locked";
        }

        private void BuildProgress()
        {
            var tree = UiKit.Panel(_stage, 0, 142, 1440, 650, UiKit.PanelBg, UiKit.Line);
            Enter(tree.rectTransform);

            var augs = Data.Augs.OrderBy(a => a.Seq).ToList();
            var centers = new Dictionary<string, Vector2>();
            foreach (var a in augs)
            {
                var p = Data.Snake[a.Seq];
                centers[a.Id] = new Vector2(p[0] * 480 + 240, 12 + p[1] * 212 + 78);   // centro del icono, en coordenadas del panel
            }

            // lineas del camino (detras de los nodos)
            for (int i = 0; i < augs.Count - 1; i++)
            {
                Vector2 a = centers[augs[i].Id], b = centers[augs[i + 1].Id];
                bool lit = PerkService.IsDone(augs[i].Id);
                var line = UiKit.Img(tree.transform, "Line", lit ? UiKit.Green : UiKit.C(120, 190, 225, 0.35f));
                var rt = line.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f); rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(Vector2.Distance(a, b), 3f);
                rt.anchoredPosition = new Vector2((a.x + b.x) / 2f, -(a.y + b.y) / 2f);
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(-(b.y - a.y), b.x - a.x) * Mathf.Rad2Deg);
            }

            foreach (var a in augs) BuildNode(tree.transform, a);
            BuildInfo();
        }

        private void BuildNode(Transform parent, AugDef a)
        {
            var p = Data.Snake[a.Seq];
            float x0 = p[0] * 480 + 40, y0 = 12 + p[1] * 212;
            string st = State(a), cls = ClassOf(a);
            bool sel = _sel == a.Id;
            Color ring = st == "locked" ? UiKit.C(80, 92, 104) : UiKit.ClassColor(cls);
            Color tint = st == "locked" ? UiKit.C(90, 90, 90) : st == "active" ? UiKit.C(215, 215, 215) : Color.white;

            var node = UiKit.Rect(parent, "Node_" + a.Id);
            UiKit.Place(node, x0, y0, 400, 208);

            // tapa transparente = zona de clic de todo el nodo
            var hit = UiButton.Make(node, 0, 0, 400, 208, () => { _sel = a.Id; Rebuild(false); },
                                    UiKit.C(255, 255, 255, 0f), UiKit.C(255, 255, 255, 0.05f));

            T(node, Data.Name(a.Id), 0, 0, 400, 26, 19, sel ? UiKit.Cyan : st == "locked" ? UiKit.Dim : UiKit.White, TextAlignmentOptions.Top, false, true);

            bool hex = a.Kind == NodeKind.Major;
            float size = hex ? 100f : 88f;
            if (st == "done") UiKit.Badge(node, 200, 76, size + 18, hex, UiKit.C((int)(ring.r * 255), (int)(ring.g * 255), (int)(ring.b * 255), 0.22f), null, Color.white);
            if (sel) UiKit.Badge(node, 200, 76, size + 12, hex, Color.white, null, Color.white);
            UiKit.Badge(node, 200, 76, size, hex, ring, a.Kind == NodeKind.Special ? null : Hud.IconSprite, tint, a.Kind == NodeKind.Special ? "+2" : null);

            var dsc = T(node, Data.Desc(a.Id), 30, 140, 340, 40, 14, UiKit.Dim, TextAlignmentOptions.Top, true);
            dsc.lineSpacing = 4f;
            string pair = Data.PairOf(a.Id);
            float yy = 182;
            if (pair != null) { T(node, L.F("unlocks", Data.Name(pair)), 20, yy, 360, 16, 12, UiKit.Red, TextAlignmentOptions.Top); yy += 18; }

            string status = st == "done" ? L.T("status.done") : st == "locked" ? L.F("status.level", a.Lvl)
                          : L.F("status.challenge", Mathf.Min(PerkService.Progress(a.Id), a.Goal), a.Goal);
            Color sc = st == "done" ? UiKit.Green : st == "active" ? UiKit.Gold : UiKit.Dim;
            T(node, status, 0, yy, 400, 16, 12, sc, TextAlignmentOptions.Top, false, true);

            Enter(node);
        }

        private void BuildInfo()
        {
            var info = UiKit.Panel(_stage, 0, 806, 1440, 124, UiKit.Panel2, UiKit.Line);
            Enter(info.rectTransform);
            var a = _sel != null ? Data.Aug(_sel) : null;
            if (a == null)
            {
                T(info.transform, L.T("title.progress"), 24, 14, 600, 18, 12, UiKit.Dim);
                T(info.transform, L.T("info.prompt.title"), 24, 34, 900, 30, 24, UiKit.White, TextAlignmentOptions.TopLeft, false, true);
                T(info.transform, L.T("info.prompt.text"), 24, 68, 1300, 40, 16, UiKit.Text, TextAlignmentOptions.TopLeft, true);
                return;
            }

            string kind = a.Kind == NodeKind.Major ? "kind.major" : a.Kind == NodeKind.Minor ? "kind.minor" : "kind.special";
            string st = State(a);
            T(info.transform, L.F("info.header", L.T(kind), a.Lvl), 24, 12, 900, 18, 12, UiKit.Dim);
            T(info.transform, Data.Name(a.Id), 24, 30, 900, 30, 24, UiKit.White, TextAlignmentOptions.TopLeft, false, true);
            T(info.transform, Data.Desc(a.Id), 24, 62, 1000, 22, 16, UiKit.Text, TextAlignmentOptions.TopLeft, true);
            T(info.transform, st == "locked" ? L.F("info.locked", a.Lvl) : L.F("info.challenge", Data.Chal(a.Id)), 24, 84, 1100, 22, 16, UiKit.Gold);
            string pair = Data.PairOf(a.Id);
            if (pair != null) T(info.transform, L.F("info.drawback", Data.Name(pair), Data.Desc(pair)), 24, 104, 1390, 20, 13, UiKit.Red);
            if (st == "active") Bar(info.transform, 1080, 24, 300, 8, PerkService.Progress(a.Id) / (float)a.Goal, UiKit.Gold);
            if (st == "active") T(info.transform, PerkService.Progress(a.Id) + " / " + a.Goal, 1080, 36, 300, 18, 13, UiKit.Dim, TextAlignmentOptions.TopRight);
        }

        // ---------------------------------------------------------------- pantalla EQUIPAR
        private void BuildEquip()
        {
            var left = UiKit.Panel(_stage, 0, 150, 1080, 760, UiKit.PanelBg, UiKit.Line);
            var right = UiKit.Panel(_stage, 1100, 150, 340, 760, UiKit.PanelBg, UiKit.Line);
            Enter(left.rectTransform); Enter(right.rectTransform);

            // columna de mejoras y de desventajas
            Section(left.transform, 20, 16, 500, "sec.am", "am");
            Tiles(left.transform, 20, 54, Data.AugsOf(NodeKind.Major).Select(a => a.Id).ToList(), "am", 160, 190, 3);
            Section(left.transform, 20, 268, 500, "sec.an", "an");
            Tiles(left.transform, 20, 306, Data.AugsOf(NodeKind.Minor).Select(a => a.Id).ToList(), "an", 245, 172, 2);

            Section(left.transform, 550, 16, 500, "sec.dm", "dm");
            Tiles(left.transform, 550, 54, Data.DrbMajor.ToList(), "dm", 160, 190, 3);
            Section(left.transform, 550, 268, 500, "sec.dn", "dn");
            Tiles(left.transform, 550, 306, Data.DrbMinor.ToList(), "dn", 245, 172, 2);

            BuildSlots(right.transform);
        }

        private void Section(Transform p, float x, float y, float w, string key, string cls)
        {
            Color c = UiKit.ClassColor(cls);
            var sq = UiKit.Img(p, "Sq", c); UiKit.Place(sq.rectTransform, x, y + 5, 10, 10);
            T(p, L.T(key), x + 20, y, w - 20, 20, 15, c, TextAlignmentOptions.TopLeft, false, true);
            UiKit.Place(UiKit.Img(p, "Underline", c).rectTransform, x, y + 26, w, 2);
        }

        private void Tiles(Transform p, float x0, float y0, List<string> ids, string cls, float w, float h, int perRow)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                float x = x0 + (i % perRow) * (w + 10), y = y0 + (i / perRow) * (h + 10);
                Tile(p, x, y, w, h, ids[i], cls);
            }
        }

        private void Tile(Transform p, float x, float y, float w, float h, string id, string cls)
        {
            bool drb = Data.IsDrawback(id);
            bool ok = PerkService.IsUnlocked(id), on = PerkService.IsEquipped(id);
            Color c = UiKit.ClassColor(cls);
            Color bg = on ? UiKit.C((int)(c.r * 60 + 18), (int)(c.g * 60 + 34), (int)(c.b * 60 + 50), 0.97f) : UiKit.Panel2;
            Color border = on ? c : ok ? new Color(c.r, c.g, c.b, 0.5f) : UiKit.Line;

            var tile = UiKit.Panel(p, x, y, w, h, bg, border, on ? 2f : 1f);
            Enter(tile.rectTransform);
            var btn = tile.gameObject.AddComponent<UiButton>();
            tile.raycastTarget = true;
            btn.Target = tile; btn.Normal = bg; btn.Hover = UiKit.C((int)(c.r * 40 + 22), (int)(c.g * 40 + 40), (int)(c.b * 40 + 56), 0.98f);
            btn.OnClick = () => OnTile(id);

            bool hex = cls == "am" || cls == "dm";
            Color ring = ok ? c : UiKit.C(80, 92, 104), tint = ok ? Color.white : UiKit.C(90, 90, 90);
            UiKit.Badge(tile.transform, w / 2f, 58, hex ? 62 : 54, hex, ring, Hud.IconSprite, tint);
            T(tile.transform, Data.Name(id), 6, 98, w - 12, 20, 14, ok ? UiKit.White : UiKit.Dim, TextAlignmentOptions.Top, false, true);

            string body; Color bc = UiKit.Dim;
            if (ok) body = Data.Desc(id);
            else if (drb) { body = L.F("tile.unlocks", Data.Name(Data.AugOf(id))); bc = UiKit.Gold; }
            else { body = L.F("tile.challenge", Data.Aug(id).Lvl); bc = UiKit.Gold; }
            var bt = T(tile.transform, body, 10, 124, w - 20, h - 130, 12, bc, TextAlignmentOptions.Top, true);
            bt.lineSpacing = 6f;

            string tag = on ? L.T("tag.equipped") : ok ? null : L.T(drb ? "tag.locked" : "tag.notunlocked");
            if (tag != null)
            {
                float tw = 8 + tag.Length * 7.5f;
                var tg = UiKit.Img(tile.transform, "Tag", on ? c : UiKit.C(58, 70, 82)); UiKit.Place(tg.rectTransform, w - tw, 0, tw, 16);
                var tt = UiKit.Txt(tg.transform, tag, 10, on ? UiKit.C(17, 17, 17) : UiKit.C(201, 214, 224), TextAlignmentOptions.Center, false, true);
                UiKit.Stretch(tt.rectTransform);
            }
        }

        private void OnTile(string id)
        {
            if (!PerkService.IsUnlocked(id)) { Toast(L.T("toast.locked")); return; }
            string info;
            string err = PerkService.ToggleEquip(id, out info);
            if (err != null) { Toast(err); return; }
            if (info != null) Toast(info);
            Dbg.Log("EQUIP", (PerkService.IsEquipped(id) ? "equipado " : "desequipado ") + id);
            Rebuild(false);
        }

        private void BuildSlots(Transform p)
        {
            T(p, L.T("perk.speedcola"), 20, 16, 300, 30, 26, UiKit.Green, TextAlignmentOptions.TopLeft, false, true);
            var rules = T(p, L.F("equip.rules", PerkService.MinorSlots(true)), 20, 56, 300, 110, 13, UiKit.Dim, TextAlignmentOptions.TopLeft, true);
            rules.lineSpacing = 10f;
            rules.paragraphSpacing = 8f;

            Section(p, 20, 190, 300, "equip.augments", "am");
            var augMinor = PerkService.EquippedMinors(false);
            Slot(p, 20, 230, PerkService.EquippedMajor(false), "am");
            for (int i = 0; i < 2; i++)
                Slot(p, 20 + (i + 1) * 106, 230, i < augMinor.Count ? augMinor[i] : null, "an", off: i >= PerkService.MinorSlots(true));

            Section(p, 20, 384, 300, "equip.drawbacks", "dm");
            var drbMinor = PerkService.EquippedMinors(true);
            Slot(p, 20, 424, PerkService.EquippedMajor(true), "dm");
            Slot(p, 126, 424, drbMinor.Count > 0 ? drbMinor[0] : null, "dn");

            // multiplicador de XP
            var box = UiKit.Panel(p, 20, 580, 300, 110, UiKit.Panel2, UiKit.Line);
            T(box.transform, L.T("xp.mult"), 14, 14, 272, 16, 12, UiKit.Dim, TextAlignmentOptions.TopLeft, false, true);
            T(box.transform, "x" + (1 + PerkService.XpBonus).ToString("0.00"), 14, 34, 272, 40, 34, UiKit.Gold, TextAlignmentOptions.TopLeft, false, true);
            T(box.transform, L.T("xp.note"), 14, 80, 272, 24, 12, UiKit.Dim, TextAlignmentOptions.TopLeft, true);
        }

        private void Slot(Transform p, float x, float y, string id, string cls, bool off = false)
        {
            Color c = UiKit.ClassColor(cls);
            bool full = id != null;
            var slot = UiKit.Panel(p, x, y, 98, 128, full ? UiKit.C((int)(c.r * 40 + 18), (int)(c.g * 40 + 34), (int)(c.b * 40 + 50), 0.97f) : UiKit.C(255, 255, 255, 0.03f),
                                   off ? UiKit.Line : new Color(c.r, c.g, c.b, full ? 1f : 0.55f));
            Enter(slot.rectTransform);
            string lbl = L.T(cls == "am" || cls == "dm" ? "slot.major" : "slot.minor");
            if (off)
            {
                T(slot.transform, L.T("slot.level5"), 0, 52, 98, 20, 12, UiKit.Dim, TextAlignmentOptions.Top, false, true);
                return;
            }
            if (full)
            {
                slot.raycastTarget = true;
                var b = slot.gameObject.AddComponent<UiButton>();
                b.Target = slot; b.Normal = slot.color; b.Hover = UiKit.C((int)(c.r * 60 + 26), (int)(c.g * 60 + 44), (int)(c.b * 60 + 60), 0.98f);
                b.OnClick = () => OnTile(id);
                bool hex = cls == "am" || cls == "dm";
                UiKit.Badge(slot.transform, 49, 38, hex ? 52 : 46, hex, c, Hud.IconSprite, Color.white);
                T(slot.transform, Data.Name(id), 4, 68, 90, 34, 11, UiKit.White, TextAlignmentOptions.Top, true, true);
            }
            else T(slot.transform, "+", 0, 34, 98, 40, 34, new Color(c.r, c.g, c.b, 0.7f), TextAlignmentOptions.Top);
            T(slot.transform, lbl, 0, 108, 98, 16, 10, c, TextAlignmentOptions.Top, false, true);
        }
    }
}
