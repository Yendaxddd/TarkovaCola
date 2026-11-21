using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ===================================================================================================================
    // Death Perception: ver mejor y marcar lo que ves.
    //   Base: ves mejor en la oscuridad (luz ambiente mas alta cuando la escena esta oscura, sin el tinte verde de las gafas) y los
    //         enemigos que ves a menos de 20 m quedan marcados (contorno naranja, DpOutline) hasta que los pierdes de vista.
    //   Mayores: Sixth Sense (marca a traves de las paredes), Hunter's Eye (sin limite de distancia), Killer Instinct (sin marcas: aviso
    //            cuando te miran por la espalda y pulso UAV de 30 s en cada baja que revela a 15 m a traves de las paredes).
    //   Menores: Persistence (la marca dura 3 s sin verlo), Cat's Eyes (vision mas fuerte, cegadoras x0.5), Shrapnel Sense (trayectoria
    //            y radio de las granadas enemigas), Reading (el color de la marca indica su vida). Especial: Extra Slot.
    //   Desventajas ligadas: Interference, Short Sight, Paranoia | Lag, Glare, Migraine, Overload.
    // ===================================================================================================================
    internal static class DeathPerception
    {
        internal const string Id = "deathperception";
        internal const string ItemTpl = "6a1c00000000000000000c0d";
        internal const float Range = 20f, ShortRange = 12f;
        internal const float PersistSeconds = 3f, ApexPersist = 5f;
        internal const float UavSeconds = 30f, UavRadius = 15f;
        internal const float HunterKillDist = 40f;
        internal const float CatFlash = 0.5f, GlareFlash = 1.3f;
        internal const float MigraineCost = 2f, OverloadSway = 1.15f;
        internal const float DarkLum = 0.30f, DarkBoost = 0.12f, CatBoost = 0.22f;
        internal const float BotVision = 1.3f;
        internal const float ScanEvery = 0.1f, LagEvery = 1f;
        internal const float GrenadeWarnDist = 10f;

        internal class Mark { public Player P; public float LostAt; public bool Visible; public bool Wall; public bool Uav; }
        private class Fake { public Vector3 Pos; public float Until; }
        private class Pulse { public Vector3 Pos; public float Until; }
        private class Nade { public Grenade G; public float Radius; public Vector3 Prev; public Vector3 Vel; public bool HasPrev; }

        private static readonly Dictionary<string, Mark> _marks = new Dictionary<string, Mark>();
        private static readonly List<Fake> _fakes = new List<Fake>();
        private static readonly List<Pulse> _pulses = new List<Pulse>();
        private static readonly List<Nade> _nades = new List<Nade>();
        private static readonly Dictionary<string, int> _hits = new Dictionary<string, int>();
        private static readonly List<float> _survive = new List<float>();
        private static float _nextScan, _nextFake, _warnUntil, _nextWarnCheck;
        internal static bool Dark;
        private static GUIStyle _style, _warnStyle;

        internal static bool Active { get { return Perks.IsActive(Id); } }
        internal static string Notice() { return Effect(Perks.Power(Id)); }
        internal static string Effect(float power) { return L.F("notice.deathperception.sub", Mathf.RoundToInt(Range * power)); }

        // Migrana: 2 de energia por enemigo marcado, como mucho una vez por enemigo cada MigraineCooldown s (antes cobraba cada vez que
        // el enemigo entraba y salia de la vista, p. ej. tras la vegetacion, y vaciaba la energia a tirones)
        private const float MigraineCooldown = 60f;
        private static readonly Dictionary<string, float> _migraineAt = new Dictionary<string, float>();
        private static void Migraine(Player me, Player p)
        {
            if (!Perk.Has("de_d_migraine") || me.ActiveHealthController == null) return;
            float last;
            if (_migraineAt.TryGetValue(p.ProfileId, out last) && Time.time - last < MigraineCooldown) return;
            _migraineAt[p.ProfileId] = Time.time;
            me.ActiveHealthController.ChangeEnergy(-MigraineCost);
        }

        internal static void Reset()
        {
            _migraineAt.Clear();
            _marks.Clear(); _fakes.Clear(); _pulses.Clear(); _nades.Clear(); _hits.Clear(); _survive.Clear();
            _nextScan = 0f; _nextFake = Time.time + 20f; _warnUntil = 0f; _nextWarnCheck = 0f; Dark = false;
            DpOutline.Clear();
        }

        private static bool Instinct { get { return Perk.Has("de_instinct"); } }

        private static float MarkRange
        {
            get
            {
                if (Perk.Has("de_hunter")) return 9999f;
                return (Perk.Has("de_d_short") ? ShortRange : Range) * Mathf.Max(0.5f, Perks.Power(Id));
            }
        }

        private static float Persist { get { return Apex.Has(Id) ? ApexPersist : (Perk.Has("de_persist") ? PersistSeconds : 0f); } }

        internal static int MarkCount { get { return _marks.Count(kv => kv.Value.Visible || Time.time - kv.Value.LostAt <= Persist); } }

        internal static bool IsMarked(Player p)
        {
            Mark m;
            return p != null && _marks.TryGetValue(p.ProfileId, out m) && (m.Visible || Time.time - m.LostAt <= Persist + 0.5f);
        }

        // ---------------------------------------------------------------- cada frame
        internal static void Tick(Player me)
        {
            if (!Active) return;
            float now = Time.time;
            TickNades();
            for (int i = _survive.Count - 1; i >= 0; i--)
                if (now >= _survive[i]) { _survive.RemoveAt(i); if (me.HealthController != null && me.HealthController.IsAlive) PerkService.AddProgress("de_shrapnel"); }
            _pulses.RemoveAll(p => now >= p.Until);
            _fakes.RemoveAll(f => now >= f.Until);

            if (Instinct && now >= _nextWarnCheck) { _nextWarnCheck = now + 0.25f; CheckBehind(me); }
            Outline(now);

            if (now < _nextScan) return;
            _nextScan = now + (Perk.Has("de_d_lag") ? LagEvery : ScanEvery);
            Scan(me, now);

            // Paranoia: de vez en cuando, una marca falsa cerca
            if (Perk.Has("de_d_paranoia") && now >= _nextFake)
            {
                _nextFake = now + UnityEngine.Random.Range(15f, 35f);
                var dir = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f) * Vector3.forward;
                _fakes.Add(new Fake { Pos = me.Position + dir * UnityEngine.Random.Range(8f, 25f) + Vector3.up * 1.6f, Until = now + 3f });
                Dbg.Log("EFECTO", "Paranoia: marca falsa");
            }
        }

        private static Vector3 Head(Player p)
        {
            return p.Position + Vector3.up * (p.MovementContext != null ? 1.4f + 0.6f * Mathf.Clamp01(p.MovementContext.PoseLevel) : 1.8f);
        }

        private static void Scan(Player me, float now)
        {
            var world = Singleton<GameWorld>.Instance;
            var cam = Powerup.Cam();
            if (world == null || cam == null) return;
            var camPos = cam.transform.position;
            float range = MarkRange;
            bool sixth = Perk.Has("de_sixth");
            bool instinct = Instinct;
            var seen = new HashSet<string>();
            foreach (var p in world.AllAlivePlayersList)
            {
                if (p == null || p == me || p.IsYourPlayer || Allies.Is(p)) continue;
                if (p.HealthController == null || !p.HealthController.IsAlive) continue;
                if (IsVehicleCrew(p)) continue;                              // conductor y artillero del BTR: no se ven, salia un PMC invisible
                var head = Head(p);
                float d = Vector3.Distance(camPos, head);
                bool uav = _pulses.Any(u => (u.Pos - p.Position).sqrMagnitude <= UavRadius * UavRadius);
                bool visible = false, wall = false;
                if (uav) { visible = true; wall = true; }
                else if (!instinct && d <= range)
                {
                    bool los = !Physics.Linecast(camPos, head, LayersMaskController.HighPolyWithTerrainMask)
                               || !Physics.Linecast(camPos, p.Position + Vector3.up * 1f, LayersMaskController.HighPolyWithTerrainMask);
                    var vp = cam.WorldToViewportPoint(head);
                    bool inView = vp.z > 0f && vp.x > -0.05f && vp.x < 1.05f && vp.y > -0.05f && vp.y < 1.05f;
                    if (los && inView) visible = true;
                    else if (sixth) { visible = true; wall = !los; }
                }
                if (!visible) continue;
                seen.Add(p.ProfileId);
                Mark m;
                if (!_marks.TryGetValue(p.ProfileId, out m))
                {
                    _marks[p.ProfileId] = m = new Mark { P = p };
                    Migraine(me, p);
                }
                else if (!m.Visible && Time.time - m.LostAt > Persist) Migraine(me, p);      // vuelve a marcarse: cuenta como marca nueva
                m.Visible = true; m.Wall = wall; m.Uav = uav;
            }
            // perdidos de vista: la marca se queda Persist s y luego se borra
            foreach (var kv in _marks.ToList())
            {
                if (seen.Contains(kv.Key)) continue;
                var m = kv.Value;
                if (m.Visible) { m.Visible = false; m.LostAt = now; }
                if (now - m.LostAt > Persist + 0.5f || m.P == null || m.P.HealthController == null || !m.P.HealthController.IsAlive) _marks.Remove(kv.Key);
            }
        }

        private static bool IsVehicleCrew(Player p)
        {
            try { var r = p.Profile.Info.Settings.Role.ToString(); return r.IndexOf("BTR", StringComparison.OrdinalIgnoreCase) >= 0; }
            catch { return false; }
        }

        // contorno de los marcados (color de la marca; mas apagado al perderlos de vista; Interferencia lo hace parpadear)
        private static readonly List<DpOutline.Entry> _outline = new List<DpOutline.Entry>();
        private static void Outline(float now)
        {
            _outline.Clear();
            bool flickerOff = Perk.Has("de_d_interf") && ((int)(now * 4f) % 2 == 1);
            if (!flickerOff)
            {
                bool reading = Perk.Has("de_reading") || Apex.Has(Id);
                foreach (var m in _marks.Values)
                {
                    if (m.P == null || m.P.HealthController == null) continue;
                    if (!m.Visible && now - m.LostAt > Persist) continue;
                    _outline.Add(new DpOutline.Entry { P = m.P, C = MarkColor(m, reading) });
                }
            }
            DpOutline.Set(_outline);
        }

        private static Color MarkColor(Mark m, bool reading)
        {
            float hp = m.P.HealthController.GetBodyPartHealth(EBodyPart.Common).Normalized;
            Color c = reading ? Color.Lerp(new Color(1f, 0.15f, 0.1f), new Color(0.3f, 1f, 0.3f), hp) : new Color(1f, 0.5f, 0.1f);
            if (!m.Visible) c *= 0.55f;
            return c;
        }

        // Killer Instinct: aviso cuando un enemigo que te ve esta a tu espalda
        private static void CheckBehind(Player me)
        {
            var world = Singleton<GameWorld>.Instance;
            var cam = Powerup.Cam();
            if (world == null || cam == null) return;
            var fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
            foreach (var p in world.AllAlivePlayersList)
            {
                if (p == null || p.IsYourPlayer || !p.IsAI) continue;
                var bot = p.AIData != null ? p.AIData.BotOwner : null;
                var ge = bot != null && bot.Memory != null ? bot.Memory.GoalEnemy : null;
                if (ge == null || ge.Person == null || ge.Person.ProfileId != me.ProfileId || !ge.IsVisible) continue;
                var to = p.Position - me.Position; to.y = 0f;
                if (to.sqrMagnitude < 0.01f || Vector3.Angle(fwd, to) < 100f) continue;
                if (Time.time >= _warnUntil) Dbg.Log("EFECTO", "Instinto asesino: " + p.Profile.Nickname + " te mira por la espalda");
                _warnUntil = Time.time + 0.6f;
                return;
            }
        }

        // ---------------------------------------------------------------- granadas enemigas (Shrapnel Sense)
        internal static void OnGrenadeThrown(Grenade g, string profileId, ThrowWeap w)
        {
            if (!Active || g == null || g is SmokeGrenade) return;
            var me = Plugin.Me;
            if (me == null || profileId == me.ProfileId) return;
            var tpl = w != null ? w.Template as ThrowWeapTemplate : null;
            float r = tpl != null ? Mathf.Clamp(tpl.MaxExplosionDistance, 3f, 15f) : 7f;
            _nades.Add(new Nade { G = g, Radius = r });
        }

        internal static void OnGrenadeExploded(Vector3 pos, string thrower)
        {
            if (!Active) return;
            var me = Plugin.Me;
            if (me == null || thrower == me.ProfileId) return;
            if ((me.Position - pos).sqrMagnitude <= GrenadeWarnDist * GrenadeWarnDist) _survive.Add(Time.time + 1f);
        }

        private static void TickNades()
        {
            float dt = Mathf.Max(0.001f, Time.deltaTime);
            for (int i = _nades.Count - 1; i >= 0; i--)
            {
                var n = _nades[i];
                if (n.G == null || !n.G.gameObject.activeInHierarchy) { _nades.RemoveAt(i); continue; }
                var pos = n.G.transform.position;
                if (n.HasPrev) n.Vel = Vector3.Lerp(n.Vel, (pos - n.Prev) / dt, 0.5f);
                n.Prev = pos; n.HasPrev = true;
            }
        }

        // ---------------------------------------------------------------- dano / bajas
        internal static void OnDealt(Player victim)
        {
            if (!Active || victim == null) return;
            int n; _hits.TryGetValue(victim.ProfileId, out n);
            if (Time.frameCount != _lastHitFrame || victim.ProfileId != _lastHitId) _hits[victim.ProfileId] = n + 1;
            _lastHitFrame = Time.frameCount; _lastHitId = victim.ProfileId;
        }
        private static int _lastHitFrame = -1;
        private static string _lastHitId;

        internal static void OnKill(Player me, Player victim, bool boss)
        {
            if (!Active || victim == null) return;
            if (boss) PerkService.AddProgress("de_slot");
            if (Dark) PerkService.AddProgress("de_cat");
            if (Vector3.Distance(me.Position, victim.Position) >= HunterKillDist) PerkService.AddProgress("de_hunter");
            Mark m;
            bool marked = _marks.TryGetValue(victim.ProfileId, out m) && (m.Visible || Time.time - m.LostAt <= Persist + 0.5f);
            if (marked)
            {
                PerkService.AddProgress("de_sixth");
                PerkService.AddProgress("de_persist");
                Apex.AddProgress(Id, 1f);
                if (m.Wall && !m.Uav) PerkService.AddProgress("de_instinct");
                int hits; _hits.TryGetValue(victim.ProfileId, out hits);
                if (hits <= 1) PerkService.AddProgress("de_reading");
            }
            _marks.Remove(victim.ProfileId);
            _hits.Remove(victim.ProfileId);
            if (Instinct)
            {
                _pulses.Add(new Pulse { Pos = victim.Position, Until = Time.time + UavSeconds });
                Dbg.Log("EFECTO", "Instinto asesino: pulso UAV en el cuerpo (" + UavSeconds + " s)");
            }
        }

        internal static float SwayFactor() { return Active && Perk.Has("de_d_overload") && MarkCount >= 3 ? OverloadSway : 1f; }

        internal static float FlashMult()
        {
            if (!Active) return 1f;
            float m = 1f;
            if (Perk.Has("de_cat")) m *= CatFlash;
            if (Perk.Has("de_d_glare")) m *= GlareFlash;
            return m;
        }

        // ---------------------------------------------------------------- vision en la oscuridad (LevelSettings.OnPreCullCallback)
        internal static void BoostAmbient(LevelSettings ls)
        {
            if (!Active || ls.AmbientType != AmbientType.Default) { Dark = false; return; }
            var sky = RenderSettings.ambientSkyColor;
            float lum = (0.2126f * sky.r + 0.7152f * sky.g + 0.0722f * sky.b) * Mathf.Max(0.01f, RenderSettings.ambientIntensity);
            float k = Mathf.Clamp01(Mathf.InverseLerp(DarkLum, DarkLum * 0.25f, lum));
            Dark = k > 0.3f;
            if (k <= 0.001f) return;
            float g = (Perk.Has("de_cat") ? CatBoost : DarkBoost) * k * Mathf.Max(0.5f, Perks.Power(Id));
            var add = new Color(g, g, g * 1.05f, 0f);
            RenderSettings.ambientSkyColor = sky + add;
            RenderSettings.ambientEquatorColor = RenderSettings.ambientEquatorColor + add;
            RenderSettings.ambientGroundColor = RenderSettings.ambientGroundColor + add * 0.8f;
            RenderSettings.ambientLight = RenderSettings.ambientLight + add;
        }

        // ---------------------------------------------------------------- pantalla
        internal static void DrawGui()
        {
            if (!Active) return;
            var cam = Powerup.Cam();
            if (cam == null) return;
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
            float now = Time.time;
            bool flickerOff = Perk.Has("de_d_interf") && ((int)(now * 4f) % 2 == 1);
            bool reading = Perk.Has("de_reading") || Apex.Has(Id);

            if (!flickerOff)
            {
                // los marcados llevan contorno (DpOutline); aqui solo la vida en texto con APEX
                if (Apex.Has(Id))
                    foreach (var m in _marks.Values)
                    {
                        if (m.P == null || m.P.HealthController == null) continue;
                        if (!m.Visible && now - m.LostAt > Persist) continue;
                        float hp = m.P.HealthController.GetBodyPartHealth(EBodyPart.Common).Normalized;
                        DrawLabel(cam, Head(m.P) + Vector3.up * 0.35f, MarkColor(m, reading), Mathf.RoundToInt(hp * 100f) + "%");
                    }
                foreach (var f in _fakes) DrawMark(cam, f.Pos, new Color(0.92f, 0.43f, 0.16f), null, false);
            }
            foreach (var u in _pulses) DrawMark(cam, u.Pos + Vector3.up * 0.4f, new Color(1f, 0.25f, 0.2f, 0.7f), "UAV " + Mathf.CeilToInt(u.Until - now) + "s", false);

            if (Perk.Has("de_shrapnel")) foreach (var n in _nades) DrawNade(cam, n);

            if (now < _warnUntil)
            {
                FinalFx.Vignette(new Color(1f, 0.15f, 0.1f), 0.35f * (0.7f + 0.3f * Mathf.Sin(now * 18f)));
                if (_warnStyle == null) _warnStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 22, fontStyle = FontStyle.Bold };
                var r = new Rect(0f, Screen.height * 0.18f, Screen.width, 40f);
                GUI.color = new Color(0f, 0f, 0f, 0.8f); GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), L.T("de.warn"), _warnStyle);
                GUI.color = new Color(1f, 0.35f, 0.25f); GUI.Label(r, L.T("de.warn"), _warnStyle);
                GUI.color = Color.white;
            }
        }

        private static bool ToScreen(Camera cam, Vector3 world, out Vector2 pt)
        {
            var vp = cam.WorldToViewportPoint(world);
            pt = new Vector2(vp.x * Screen.width, (1f - vp.y) * Screen.height);
            return vp.z > 0f;
        }

        private static void DrawLabel(Camera cam, Vector3 pos, Color c, string label)
        {
            Vector2 pt;
            if (!ToScreen(cam, pos, out pt)) return;
            GUI.color = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(pt.x - 59f, pt.y - 9f, 120f, 20f), label, _style);
            c.a = 1f; GUI.color = c;
            GUI.Label(new Rect(pt.x - 60f, pt.y - 10f, 120f, 20f), label, _style);
            GUI.color = Color.white;
        }

        private static void DrawMark(Camera cam, Vector3 pos, Color c, string label, bool wall)
        {
            Vector2 pt;
            if (!ToScreen(cam, pos, out pt)) return;
            GUI.color = new Color(0f, 0f, 0f, c.a * 0.8f);
            GUI.Label(new Rect(pt.x - 39f, pt.y - 11f, 80f, 22f), wall ? "◇" : "◆", _style);
            GUI.color = c;
            GUI.Label(new Rect(pt.x - 40f, pt.y - 12f, 80f, 22f), wall ? "◇" : "◆", _style);
            if (label != null) GUI.Label(new Rect(pt.x - 60f, pt.y + 6f, 120f, 20f), label, _style);
            GUI.color = Color.white;
        }

        // trayectoria prevista (parabola con la velocidad medida) y circulo de peligro donde caera
        private static void DrawNade(Camera cam, Nade n)
        {
            if (n == null || n.G == null) return;
            var pos = n.G.transform.position;
            var vel = n.Vel;
            Vector3 land = pos;
            Vector2 pt;
            var tex = FinalFx.White;
            GUI.color = new Color(1f, 0.55f, 0.15f, 0.9f);
            if (vel.sqrMagnitude > 0.5f)
            {
                var p = pos;
                for (int i = 0; i < 40; i++)
                {
                    var next = p + vel * 0.05f;
                    vel += Physics.gravity * 0.05f;
                    RaycastHit hit;
                    if (Physics.Linecast(p, next, out hit, LayersMaskController.HighPolyWithTerrainMask)) { land = hit.point; break; }
                    p = next; land = p;
                    if (i % 2 == 0 && ToScreen(cam, p, out pt)) GUI.DrawTexture(new Rect(pt.x - 2f, pt.y - 2f, 4f, 4f), tex);
                }
            }
            // circulo de peligro
            GUI.color = new Color(1f, 0.2f, 0.15f, 0.85f);
            for (int i = 0; i < 32; i++)
            {
                float a = i / 32f * Mathf.PI * 2f;
                var wp = land + new Vector3(Mathf.Cos(a), 0.05f, Mathf.Sin(a)) * n.Radius;
                if (ToScreen(cam, wp, out pt)) GUI.DrawTexture(new Rect(pt.x - 2.5f, pt.y - 2.5f, 5f, 5f), tex);
            }
            if (ToScreen(cam, land + Vector3.up * 0.3f, out pt)) GUI.Label(new Rect(pt.x - 40f, pt.y - 12f, 80f, 22f), "⚠", _style);
            GUI.color = Color.white;
        }
    }

    // ---------- vision en la oscuridad ----------
    [HarmonyPatch(typeof(LevelSettings), nameof(LevelSettings.OnPreCullCallback))]
    internal static class DarkVisionPatch
    {
        [HarmonyPostfix]
        private static void Postfix(LevelSettings __instance)
        {
            if (!DeathPerception.Active) return;
            try { DeathPerception.BoostAmbient(__instance); }
            catch (Exception e) { Dbg.Throttled("darkvision", 10f, "ERROR", "Death Perception (vision): " + e.Message); }
        }
    }

    // ---------- granadas cegadoras: Cat's Eyes (x0.5) y Glare (x1.3) ----------
    [HarmonyPatch(typeof(GrenadeFlashScreenEffect), nameof(GrenadeFlashScreenEffect.Explode))]
    internal static class DpFlashPatch
    {
        [HarmonyPrefix]
        private static void Prefix(ref float flashStrength)
        {
            float m = DeathPerception.FlashMult();
            if (m != 1f) flashStrength *= m;
        }
    }

    // ---------- bots con Death Perception: ven mas lejos a oscuras ----------
    [HarmonyPatch(typeof(BotNightVisionData), nameof(BotNightVisionData.UpdateVision))]
    internal static class BotDarkVisionPatch
    {
        [HarmonyPostfix]
        private static void Postfix(BotNightVisionData __instance, ref float __result)
        {
            if (!BotPerks.Any || __result >= 200f) return;
            var owner = __instance._owner;
            var p = owner != null ? owner.GetPlayer : null;
            if (p != null && BotPerks.Has(p, DeathPerception.Id)) __result *= DeathPerception.BotVision;
        }
    }
}
