using System;
using System.Reflection;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace TarkovaCola.Client
{
    // Estilos de cuerpo a cuerpo (base de Melee Macchiato). EFT solo tiene dos golpes por arma (tajo y estocada); aqui se encadenan
    // en combos y se giran por codigo para que parezcan golpes distintos:
    //   ligeras (cuchillos):   1 tajo horizontal -> 2 tajo vertical (el mismo tajo girado) -> 3 estocada
    //   medias (machetes...):  1 horizontal -> 2 horizontal de reves (girado 180) -> 3 vertical descendente
    //   pesadas (hachas...):   un unico tajo horizontal, mas lento y con peso (sacudida al golpear)
    // El dano no depende de la animacion (el juego mira lo que hay delante), asi que girar el golpe es solo visual.
    internal static class MeleeStyle
    {
        internal enum Kind { Light, Medium, Heavy }
        internal const float LightMax = 0.3f, MediumMax = 1.0f;       // kg
        private const float ComboWindow = 0.55f;                       // s tras acabar un golpe para encadenar el siguiente
        private const float BlendIn = 0.07f;                           // s para girar el arma al empezar el golpe

        private static int _combo = -1;
        private static float _lastEnd = -10f;
        private static float _swingStart;
        private static bool _swinging;
        internal static bool Swinging { get { return _swinging; } }               // (APEX de Melee Macchiato)
        private static float _roll;                                    // giro (grados) del golpe actual alrededor del eje de la camara
        private static Kind _kind;
        private static float _rollNow;                                 // giro actual (suavizado) que se aplica al arma
        private const float RollSpeed = 900f;                          // grados por segundo con que entra y sale el giro
        private static float _shake;                                   // pesadas: sacudida al impactar

        // el controlador del cuchillo es el que tiene ahora el jugador local
        internal static bool Mine(Player.KnifeController ctrl) { var me = Plugin.Me; return me != null && ReferenceEquals(me.HandsController, ctrl); }

        internal static Kind Classify(Item item)
        {
            float w = item != null ? item.Template.Weight : 0.2f;
            return w < LightMax ? Kind.Light : (w < MediumMax ? Kind.Medium : Kind.Heavy);
        }

        // ---------------------------------------------------------------- golpes
        // Antes de un tajo (desde Idling): decide el paso del combo. Devuelve true si el tajo debe convertirse en estocada.
        internal static bool BeforeSlash(Player.KnifeController ctrl)
        {
            if (!Plugin.CfgMeleeStyles.Value) return false;
            _kind = Classify(ctrl.Item);
            bool chained = Time.time - _lastEnd <= ComboWindow * ComboMult;
            _combo = chained ? (_combo + 1) % 3 : 0;
            if (_kind == Kind.Heavy) _combo = 0;
            _roll = 0f;
            bool stab = false;
            switch (_kind)
            {
                case Kind.Light:
                    if (_combo == 1) _roll = 80f;          // tajo vertical
                    else if (_combo == 2) stab = true;     // estocada
                    break;
                case Kind.Medium:
                    if (_combo == 1) _roll = 180f;         // de reves
                    else if (_combo == 2) _roll = 75f;     // vertical descendente
                    break;
            }
            _weight = ctrl.Item != null ? ctrl.Item.Template.Weight : 0f;
            _chained = chained;
            Ev((chained ? "encadena" : "nuevo combo") + " -> golpe " + (_combo + 1) + (stab ? " (estocada)" : " giro " + _roll));
            Dbg.Log("MELEE", _kind + " golpe " + (_combo + 1) + (stab ? " (estocada)" : " giro " + _roll));
            _stab = stab;
            return stab;
        }

        internal static void OnKickStart(Player.KnifeController ctrl)
        {
            float speed = MeleeMacchiato.SwingSpeed(_stab);
            _stab = false;
            try { MeleeMacchiato.OnSwing(Plugin.Me); } catch (Exception e) { Dbg.Throttled("mmswing", 10f, "ERROR", "Macchiato (golpe): " + e.Message); }
            if (!Plugin.CfgMeleeStyles.Value)
            {
                if (speed > 1.01f) ctrl.SetMeleeSpeed(Plugin.Me.Physical.MeleeSpeed * speed);      // sin estilos, Macchiato sigue acelerando
                return;
            }
            _swinging = true;
            _swingStart = Time.time;
            if (_kind == Kind.Heavy) speed *= 0.85f;                 // pesadas: mas lentas, se siente el peso
            _speed = speed;
            Ev("inicio del golpe (velocidad x" + speed.ToString("0.00") + ")");
            if (Mathf.Abs(speed - 1f) > 0.01f) ctrl.SetMeleeSpeed(Plugin.Me.Physical.MeleeSpeed * speed);
        }

        internal static void OnImpact()
        {
            Ev("impacto (" + (Time.time - _swingStart).ToString("0.00") + " s)");
            if (Plugin.CfgMeleeStyles.Value && _kind == Kind.Heavy) _shake = 1f;
        }

        internal static void OnKickEnd()
        {
            if (_swinging) Ev("fin del golpe (" + (Time.time - _swingStart).ToString("0.00") + " s)");
            _swinging = false;
            _lastEnd = Time.time;
        }

        // ---------------------------------------------------------------- depuracion en pantalla
        private static float _weight, _speed = 1f;
        private static bool _stab;                                     // el golpe que empieza es una estocada (Flash Strike solo acelera el tajo)
        private static bool _chained;
        private static readonly System.Collections.Generic.List<string> _events = new System.Collections.Generic.List<string>();
        private static void Ev(string s)
        {
            if (!Plugin.CfgMeleeDebug.Value) return;
            _events.Add(Time.time.ToString("0.00") + "  " + s);
            if (_events.Count > 12) _events.RemoveAt(0);
        }

        // una linea con el estado del golpe (la usa la grabacion de clips para escribirla en datos.txt)
        internal static string Snapshot()
        {
            var me = Plugin.Me;
            if (me == null || !(me.HandsController is Player.KnifeController)) return "-";
            return _kind + " | golpe " + (_combo + 1) + "/3 | giro " + _roll.ToString("0") + " | vel x" + _speed.ToString("0.00")
                   + " | " + (_swinging ? "golpeando " + (Time.time - _swingStart).ToString("0.00") + "s" : "quieto")
                   + " | ventana " + Mathf.Max(0f, ComboWindow * ComboMult - (Time.time - _lastEnd)).ToString("0.00") + "s";
        }

        internal static void DrawDebug()
        {
            if (!Plugin.CfgMeleeDebug.Value || Event.current.type != EventType.Repaint) return;
            var me = Plugin.Me;
            if (me == null || !(me.HandsController is Player.KnifeController)) return;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("CUERPO A CUERPO  (estilos " + (Plugin.CfgMeleeStyles.Value ? "si" : "no") + ", Macchiato " + (MeleeMacchiato.Active ? "si" : "no") + ")");
            sb.AppendLine("arma: " + _kind + "  " + _weight.ToString("0.00") + " kg");
            sb.AppendLine("combo: golpe " + (_combo + 1) + "/3" + (_chained ? " (encadenado)" : "") + "  giro " + _roll.ToString("0") + "°  velocidad x" + _speed.ToString("0.00"));
            sb.AppendLine("golpeando: " + (_swinging ? (Time.time - _swingStart).ToString("0.00") + " s" : "no")
                          + "  ventana combo: " + Mathf.Max(0f, ComboWindow * ComboMult - (Time.time - _lastEnd)).ToString("0.00") + " s");
            foreach (var e in _events) sb.AppendLine(e);
            var r = new Rect(20f, Screen.height * 0.25f, 520f, 300f);
            GUI.color = new Color(0f, 0f, 0f, 0.6f); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white;
            GUI.Label(new Rect(r.x + 8f, r.y + 6f, r.width - 16f, r.height - 12f), sb.ToString());
        }

        // Melee Macchiato acelera los golpes (y mas adelante alargara la ventana del combo)
        internal static float SpeedMult { get { return MeleeMacchiato.SpeedFactor; } }
        internal static float ComboMult { get { return 1f; } }

        // ---------------------------------------------------------------- giro visual (despues de que EFT coloca las manos)
        internal static void LateTransform(Player me)
        {
            if (!(me.HandsController is Player.KnifeController)) { _swinging = false; return; }
            var lift = Plugin.CfgMeleeLift.Value;
            if (!Plugin.CfgMeleeStyles.Value)
            {
                var r0 = me.PlayerBones != null ? me.PlayerBones.Weapon_Root_Anim : null; var c0 = Powerup.Cam();
                if (r0 != null && c0 != null) r0.position += c0.transform.up * lift - c0.transform.right * Plugin.CfgMeleeSide.Value;
                _swinging = false; return;
            }
            var root = me.PlayerBones != null ? me.PlayerBones.Weapon_Root_Anim : null;
            var cam = Powerup.Cam();
            if (root == null || cam == null) return;

            // el giro entra y sale suave (antes volvia de golpe al terminar el golpe): se acerca al valor del golpe actual (o a 0)
            float target = _swinging ? _roll : 0f;
            _rollNow = Mathf.MoveTowards(_rollNow, target, RollSpeed * Mathf.Max(Time.deltaTime, 1f / 240f));
            Quaternion extra = Mathf.Abs(_rollNow) > 0.1f ? Quaternion.AngleAxis(_rollNow, cam.transform.forward) : Quaternion.identity;
            if (_shake > 0f)
            {
                float a = _shake * 2.5f;
                extra = extra * Quaternion.Euler(UnityEngine.Random.Range(-a, a), UnityEngine.Random.Range(-a, a), 0f);
                _shake = Mathf.Max(0f, _shake - Time.deltaTime * 4f);
            }
            // ligeras: el arma algo mas al centro de la pantalla
            Vector3 center = (_kind == Kind.Light && Plugin.CfgMeleeCenter.Value ? -cam.transform.right * 0.035f : Vector3.zero) + cam.transform.up * lift - cam.transform.right * Plugin.CfgMeleeSide.Value;
            if (extra == Quaternion.identity && center == Vector3.zero) return;

            // el arma gira SOBRE SI MISMA (antes giraba alrededor de la camara y 80 grados la mandaban al borde superior de la pantalla)
            root.position += center;
            root.rotation = extra * root.rotation;
        }
    }

    // ---------------------------------------------------------------- parches
    [HarmonyPatch]
    internal static class MeleeSlashPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            var idling = AccessTools.Inner(typeof(Player.KnifeController), "Idling");
            return AccessTools.Method(idling, "MakeKnifeKick");
        }

        [HarmonyPrefix]
        private static bool Prefix(object __instance, ref bool __result)
        {
            try
            {
                var ctrl = Traverse.Create(__instance).Field("Controller").GetValue() as Player.KnifeController;
                if (ctrl == null || !MeleeStyle.Mine(ctrl)) return true;
                if (!MeleeStyle.BeforeSlash(ctrl)) return true;
                __result = (bool)AccessTools.Method(__instance.GetType(), "MakeAlternativeKick").Invoke(__instance, null);   // 3.er golpe: estocada
                return false;
            }
            catch (Exception e) { Plugin.Log.LogError("cuerpo a cuerpo (combo): " + e.Message); return true; }
        }
    }

    [HarmonyPatch]
    internal static class MeleeKickStartPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            var kick = AccessTools.Inner(typeof(Player.KnifeController), "Kick");
            return AccessTools.Method(kick, "Start");
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            try
            {
                var ctrl = Traverse.Create(__instance).Field("Controller").GetValue() as Player.KnifeController;
                if (ctrl != null && MeleeStyle.Mine(ctrl)) MeleeStyle.OnKickStart(ctrl);
            }
            catch (Exception e) { Plugin.Log.LogError("cuerpo a cuerpo (golpe): " + e.Message); }
        }
    }

    [HarmonyPatch]
    internal static class MeleeKickFirePatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            var kick = AccessTools.Inner(typeof(Player.KnifeController), "Kick");
            return AccessTools.Method(kick, "OnFire");
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            var ctrl = Traverse.Create(__instance).Field("Controller").GetValue() as Player.KnifeController;
            if (ctrl != null && MeleeStyle.Mine(ctrl)) MeleeStyle.OnImpact();
        }
    }

    [HarmonyPatch]
    internal static class MeleeKickEndPatch
    {
        [HarmonyTargetMethod]
        private static MethodBase TargetMethod()
        {
            var kick = AccessTools.Inner(typeof(Player.KnifeController), "Kick");
            return AccessTools.Method(kick, "OnFireEnd");
        }

        [HarmonyPostfix]
        private static void Postfix(object __instance)
        {
            var ctrl = Traverse.Create(__instance).Field("Controller").GetValue() as Player.KnifeController;
            if (ctrl != null && MeleeStyle.Mine(ctrl)) MeleeStyle.OnKickEnd();
        }
    }

    [HarmonyPatch(typeof(ProceduralWeaponAnimation), nameof(ProceduralWeaponAnimation.LateTransformations))]
    internal static class MeleeLatePatch
    {
        [HarmonyPostfix]
        private static void Postfix(ProceduralWeaponAnimation __instance)
        {
            try
            {
                var me = Plugin.Me;
                if (me == null || !ReferenceEquals(me.ProceduralWeaponAnimation, __instance)) return;
                MeleeStyle.LateTransform(me);
            }
            catch (Exception e) { Dbg.Throttled("melee_late", 10f, "ERROR", "cuerpo a cuerpo (giro): " + e.Message); }
            try
            {
                var me = Plugin.Me;
                if (me != null && ReferenceEquals(me.ProceduralWeaponAnimation, __instance)) CodDrink.LateTransform(me);     // animacion de beber de CoD
            }
            catch (Exception e) { Dbg.Throttled("cod_drink", 10f, "ERROR", "animacion de beber de CoD: " + e); }
        }
    }
}
