using System;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using UnityEngine;

namespace TarkovaCola.Client
{
    // ============================================================================================
    //  Perks finales (3.1): Quick Revive, Mule Kick, Electric Cherry, Death Perception, Tombstone Soda y Double Tap.
    //  Un unico punto de entrada desde los ganchos comunes (Tracker, DamageHubPatch, PerkFx.OnKill, disparos, recargas, OnGUI...)
    //  para no repartir llamadas por todo el codigo. Cada perk vive en su propio archivo.
    // ============================================================================================
    internal static class FinalPerks
    {
        // al tener el jugador listo en la raid (Tracker)
        internal static void Reset()
        {
            Safe("reset", () => { DoubleTap.Reset(); ElectricCherry.Reset(); QuickRevive.Reset(); DeathPerception.Reset(); Tombstone.Reset(); MuleKick.Reset(); });
        }

        // cada frame en raid (Tracker)
        internal static void Tick(Player me)
        {
            if (!Perks.AnyActive) return;
            Safe("tick.dt", () => DoubleTap.Tick(me));
            Safe("tick.ec", () => ElectricCherry.Tick(me));
            Safe("tick.qr", () => QuickRevive.Tick(me));
            Safe("tick.dp", () => DeathPerception.Tick(me));
            Safe("tick.ts", () => Tombstone.Tick(me));
            Safe("tick.mk", () => MuleKick.Tick(me));
        }

        // disparo propio (DeadshotShotPatch, fragmento 0)
        internal static void OnShot(Player me)
        {
            Util.LastShot = Time.time;                       // Speed Cola: cerrojo/corredera rapidos (PerkFx.UpdateAnim)
            if (!Perks.AnyActive) return;
            Safe("shot.dt", () => DoubleTap.OnShot(me));
            Safe("shot.qr", () => QuickRevive.OnShot());
            Safe("shot.ts", () => Tombstone.OnShot(me));
            Safe("shot.mk", () => MuleKick.OnShot(me));
        }

        // empieza un cambio de cargador propio (OverpressurePatch)
        internal static void OnReloadStart(Player.FirearmController fc)
        {
            if (!Perks.AnyActive) return;
            Safe("reload.ec", () => ElectricCherry.OnReloadStart(fc));
        }

        // dano que recibes (DamageHubPatch, despues de las reducciones); devuelve el dano final
        internal static float OnTaken(Player me, ActiveHealthController hc, EBodyPart part, float damage, DamageInfo info)
        {
            try
            {
                ElectricCherry.OnDamageTaken();
                damage = QuickRevive.OnTaken(me, hc, part, damage, info);         // Revivir (golpe letal -> Caido)
                Tombstone.OnTaken(me, hc, damage, info);                            // Espectro (bajo el 25%)
                MuleKick.OnTaken(me, damage);                                       // Loose Strap
            }
            catch (Exception e) { Dbg.Throttled("final.taken", 10f, "ERROR", "perks finales (dano recibido): " + e); }
            return damage;
        }

        // dano que haces a otro jugador (DamageHubPatch); devuelve el dano final
        internal static float OnDealt(Player me, Player victim, EBodyPart part, float damage, DamageInfo info)
        {
            try { damage = DoubleTap.OnDealt(me, victim, damage, info); damage = Tombstone.OnDealt(damage, info); damage = MuleKick.OnDealt(damage, info); DeathPerception.OnDealt(victim); }
            catch (Exception e) { Dbg.Throttled("final.dealt", 10f, "ERROR", "perks finales (dano causado): " + e); }
            return damage;
        }

        // baja propia (PerkFx.OnKill)
        internal static void OnKill(Player me, Player victim, DamageInfo info, EBodyPart part, bool boss)
        {
            Safe("kill.dt", () => DoubleTap.OnKill(me, victim, info, boss));
            Safe("kill.ec", () => ElectricCherry.OnKill(me, boss));
            Safe("kill.qr", () => QuickRevive.OnKill(me, victim, boss));
            Safe("kill.dp", () => DeathPerception.OnKill(me, victim, boss));
            Safe("kill.ts", () => Tombstone.OnKill(me, victim, boss));
            Safe("kill.mk", () => MuleKick.OnKill(me, victim, info, boss));
        }

        // el jugador muere (PerkFx.OnKill, victima = tu)
        internal static void OnDeath(Player me)
        {
            Safe("death.ts", () => Tombstone.OnDeath(me));
        }

        // factor de balanceo del arma (PerkFx)
        internal static float SwayFactor(Player me)
        {
            float s = 1f;
            try { s *= ElectricCherry.SwayFactor() * DeathPerception.SwayFactor() * MuleKick.SwayFactor(); }
            catch (Exception e) { Dbg.Throttled("final.sway", 10f, "ERROR", "perks finales (balanceo): " + e.Message); }
            return s;
        }

        // OnGUI del plugin
        internal static void DrawGui()
        {
            if (Event.current.type != EventType.Repaint || Plugin.Me == null || !Perks.AnyActive) return;
            Safe("gui.ec", () => { ElectricCherry.DrawGui(); ElectricVfx.DrawGui(); });
            Safe("gui.dp", () => DeathPerception.DrawGui());
            Safe("gui.ts", () => Tombstone.DrawGui());
            Safe("gui.mk", () => MuleKick.DrawGui());
        }

        // cambio de arma propio (Tracker.OnHandsChanged)
        internal static void OnHandsChanged(Player me, object hc)
        {
            Safe("hands.mk", () => MuleKick.OnHandsChanged(me, hc));
        }

        // granadas de cualquiera (GrenadeInitPatch / GrenadeExplosionPatch)
        internal static void OnGrenadeThrown(Grenade g, string profileId, ThrowWeap w)
        {
            if (!Perks.AnyActive) return;
            Safe("nade.dp", () => DeathPerception.OnGrenadeThrown(g, profileId, w));
            Safe("nade.bot", () => BotPerks.OnGrenadeThrown(g, profileId));
        }

        internal static void OnGrenadeExploded(Vector3 pos, string thrower)
        {
            if (!Perks.AnyActive) return;
            Safe("boom.dp", () => DeathPerception.OnGrenadeExploded(pos, thrower));
        }

        // bots con perks (BotPerks.Apply, 2 Hz)
        internal static void BotTick(Player bot, System.Collections.Generic.ICollection<string> perks)
        {
            if (perks.Contains(QuickRevive.Id)) QuickRevive.BotTick(bot, 0.5f);
            Tombstone.BotTickAll();
            if (perks.Contains(ElectricCherry.Id))
            {
                var fc = bot.HandsController as Player.FirearmController;
                if (fc != null && fc.IsInReloadOperation()) ElectricCherry.BotReloading(bot);
            }
        }

        private static void Safe(string tag, Action a)
        {
            try { a(); }
            catch (Exception e) { Dbg.Throttled("final." + tag, 10f, "ERROR", "perks finales (" + tag + "): " + e); }
        }
    }

    // ---------- utilidades visuales de pantalla (OnGUI) ----------
    internal static class FinalFx
    {
        private static Texture2D _vig;

        // borde de color en la pantalla (mas intenso en las esquinas)
        internal static void Vignette(Color c, float alpha)
        {
            if (alpha <= 0.005f) return;
            if (_vig == null)
            {
                const int n = 128;
                _vig = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.4142f;
                        float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, d));
                        px[y * n + x] = new Color(1f, 1f, 1f, a);
                    }
                _vig.SetPixels(px);
                _vig.Apply();
            }
            var prev = GUI.color;
            GUI.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _vig, ScaleMode.StretchToFill, true);
            GUI.color = prev;
        }

        private static Texture2D _white;
        internal static Texture2D White
        {
            get
            {
                if (_white == null) { _white = new Texture2D(1, 1); _white.SetPixel(0, 0, Color.white); _white.Apply(); }
                return _white;
            }
        }
    }
}
