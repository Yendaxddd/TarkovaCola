using System;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Quests;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Disparadores de los logros de la 2.0 (las definiciones estan en AchievementsExtra.cs, generadas por make_achievements.py).
    // Cada metodo lo llama el sistema correspondiente cuando pasa algo; los que dependen del estado (niveles, mejoras, sinergias,
    // misiones) se comprueban en CheckState.
    internal static class AchTrack
    {
        // ids de las misiones de la historia (mismo calculo que make_quest_pack.py)
        private static readonly string[] Missions = Enumerable.Range(1, 10)
            .Select(n => n == 1 ? "6a1c00000000000000000e01" : "6a1c" + new string('0', 14) + "51" + n.ToString("x2") + "00").ToArray();

        private static readonly List<float> _drinks = new List<float>();
        private static readonly List<float> _kills = new List<float>();
        private static int _headsRaid;
        private static float _run;

        private static void U(string key) { Achievements.Unlock(key); }

        internal static void OnRaidStart() { _drinks.Clear(); _kills.Clear(); _headsRaid = 0; _run = 0f; }

        // El jugador ya existe (las perks persistentes ya estan activas)
        internal static void OnPlayerReady()
        {
            if (Eternal.Any)
            {
                PerkService.AddStat("eternal_raids", 1);
                if (PerkService.Stat("eternal_raids") >= 5) U("e_raids");
            }
            CheckActive();
        }

        // ---------------------------------------------------------------- perks
        internal static void OnDrink(PerkDef def, bool powerup)
        {
            if (!powerup)
            {
                PerkService.AddStat("drinks", 1);
                int n = PerkService.Stat("drinks");
                if (n >= 50) U("g_drinks50");
                if (n >= 150) U("g_drinks150");
                float now = Time.time;
                _drinks.Add(now);
                _drinks.RemoveAll(t => now - t > 60f);
                if (_drinks.Count >= 3) U("g_rush");
            }
            CheckActive();
        }

        internal static void CheckActive()
        {
            int n = Perks.ActivePerks.Count();
            if (n >= 5) U("g_five");
            if (n >= Perks.Original.Length) U("g_six");
            if (ClassicMode.Enabled)
            {
                var e = ClassicMode.Entries();
                if (e.Count(x => !x.Modifier) >= ClassicMode.MaxMain && e.Any(x => x.Modifier)) U("c_full");
            }
        }

        // ---------------------------------------------------------------- bajas
        // Toda baja del jugador (con o sin perks)
        internal static void OnKillAny(Player me, Player victim, EBodyPart part)
        {
            if (QuestGuards.IsGuard(victim))
            {
                PerkService.AddStat("bottlers", 1);
                if (PerkService.Stat("bottlers") >= 25) U("q_guards");
                if (Perks.All.Any(p => Perks.IsActive(p.Id) && QuestGuards.Has(victim, p.Id))) U("x_mirror");
                if (QuestGuards.IsBoss(victim))
                {
                    U("q_boss");
                    if (!Perks.AnyActive) U("q_sober");
                }
            }
            if (!Perks.AnyActive) return;

            PerkService.AddStat("kills_perk", 1);
            int k = PerkService.Stat("kills_perk");
            if (k >= 100) U("s_kills100");
            if (k >= 500) U("s_kills500");
            if (k >= 1500) U("s_kills1500");
            if (part == EBodyPart.Head)
            {
                PerkService.AddStat("heads_perk", 1);
                if (PerkService.Stat("heads_perk") >= 250) U("s_heads");
                if (Deadshot.Active && ++_headsRaid >= 5) U("dd_secret");
            }
            if (Deadshot.Active)
            {
                if (PerkService.Stat("dd_heads") >= 100) U("dd_heads");
                if (PerkService.Stat("dd_far") >= 10) U("dd_far");
            }

            float now = Time.time;
            int active = Perks.ActivePerks.Count();
            _kills.Add(now);
            _kills.RemoveAll(t => now - t > 10f);
            if (_kills.Count >= 3 && active >= 3) U("g_combo");
            if (PerkFx.IsBoss(victim) && active >= Perks.Original.Length) U("g_bossall");
        }

        // ---------------------------------------------------------------- otros eventos
        internal static void OnRun(float meters)
        {
            _run += meters;
            if (_run < 100f) return;
            int whole = (int)_run; _run -= whole;
            PerkService.AddStat("run_m", whole);
            if (PerkService.Stat("run_m") >= 50000) U("s_run");
        }

        internal static void OnAbsorbed(float damage)
        {
            if (damage <= 0f) return;
            PerkService.AddStat("phd_absorbed", Mathf.CeilToInt(damage));
            if (PerkService.Stat("phd_absorbed") >= 5000) U("s_absorb");
        }

        internal static void OnWebbed()
        {
            if (PerkService.Stat("widow_webbed") >= 250) U("s_webbed");
        }

        internal static void OnEternal(bool viaPowerup)
        {
            U("e_first");
            if (Eternal.Count >= 3) U("e_three");
            if (Eternal.Count >= Perks.Original.Length) U("e_all");
            if (viaPowerup) U("p_eternal");
        }

        internal static void OnEternalReturned() { U("e_back"); }

        internal static void OnPowerupPicked(float timeLeft)
        {
            U("p_first");
            if (timeLeft <= 5f) U("p_late");
        }

        internal static void OnClassicChallenge() { U("c_chal"); }
        internal static void OnClassicSecondAugment() { U("c_second"); }

        // Fin de raid (desde PerkFx.OnRaidEnd)
        internal static void OnRaidEnd(bool alive)
        {
            if (ClassicMode.Enabled)
            {
                if (alive && ClassicMode.Entries().Count > 0)
                {
                    PerkService.AddStat("classic_streak", 1);
                    if (PerkService.Stat("classic_streak") >= 5) U("c_streak");
                }
                else if (!alive && PerkService.State["stats"] is Newtonsoft.Json.Linq.JObject st) { st["classic_streak"] = 0; PerkService.Save(); }
            }
            if (!alive) return;
            if (Synergy.Equipped().Any(id => Synergy.Power(id) > 0f)) U("syn_extract");
            if (Perks.ActivePerks.Count() >= Perks.Original.Length) U("x_extract6");
        }

        // ---------------------------------------------------------------- estado (niveles, mejoras, sinergias, misiones)
        internal static void CheckState()
        {
            if (PerkService.IsMax(Deadshot.Id)) U("dd_lvl5");
            if (Perks.Deadshot.Augs.All(a => PerkService.IsDone(a.Id))) U("dd_all");
            if (Perks.Original.All(p => PerkService.IsMax(p.Id))) U("g_lvl5all");
            var augs = Perks.All.SelectMany(p => p.Augs).ToList();
            int done = augs.Count(a => PerkService.IsDone(a.Id));
            if (done >= 20) U("g_aug20");
            if (done == augs.Count) U("g_augall");
            if (!ClassicMode.Enabled)
            {
                if (Perks.All.Any(p => PerkService.EquippedMajor(p.Id, false) != null && PerkService.EquippedMinors(p.Id, false).Count >= 2)) U("m_loaded");
                if (Perks.Original.All(p => p.Augs.Any(a => PerkService.IsDone(a.Id)))) U("m_spread");
            }
            OnSynergyUnlocked();
            CheckMissions();
        }

        internal static void OnSynergyUnlocked()
        {
            int n = 0;
            foreach (var s in Synergy.All)
                if (Synergy.IsUnlocked(s.Id)) { n++; U(s.Id); }
            if (n >= 5) U("syn_five");
            if (n >= Synergy.All.Length) U("syn_all");
        }

        // Misiones de la historia completadas (estado de las misiones del perfil)
        private static void CheckMissions()
        {
            try
            {
                Profile profile = Plugin.Me != null ? Plugin.Me.Profile : null;
                if (profile == null)
                {
                    var app = Singleton<ClientApplication<IEftSession>>.Instance;
                    var session = app != null ? app.GetClientBackEndSession() : null;
                    profile = session != null ? session.Profile : null;
                }
                if (profile == null || profile.QuestsData == null) return;
                for (int i = 0; i < Missions.Length; i++)
                {
                    var q = profile.QuestsData.FirstOrDefault(x => x.Id == Missions[i]);
                    if (q != null && q.Status == EQuestStatus.Success) U("q_" + (i + 1).ToString("00"));
                }
            }
            catch (Exception e) { Dbg.Log("LOGRO", "no se pudieron leer las misiones: " + e.Message); }
        }
    }
}
