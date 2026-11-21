using System;
using System.Collections.Generic;
using System.Linq;
using EFT.Interactive;
using UnityEngine;
using UnityEngine.AI;

namespace TarkovaCola.Client
{
    // ===================================================================================================================
    // Colocacion automatica de las maquinas (para no tener que colocarlas a mano en cada mapa con el editor).
    //   Si un mapa no tiene piezas puestas a mano, al empezar la raid se buscan sitios validos alrededor de los contenedores de
    //   botin (estan en interiores y en zonas que se recorren):
    //     - suelo andable (NavMesh) y plano, con una pared detras a menos de 1.2 m: la maquina se pega a ella de espaldas
    //     - hueco libre para la maquina (caja de 1 x 2.1 x 0.85 m), techo por encima y paso libre delante (1.5 m)
    //   De los sitios validos se eligen los mas separados entre si (muestreo del punto mas lejano, con algo de azar) y se reparten:
    //   6 maquinas de perk, Der Wunderfizz, 3 puntos de la caja misteriosa, 2 de TEDD y el interruptor de la luz.
    //   Modo "Fixed" (por defecto): se guardan en machines.json marcadas "auto" y se repiten en cada raid (se pueden retocar con el
    //   editor: lo que se coloca a mano manda). "EveryRaid": sitios nuevos cada raid, sin guardar. "Off": solo las puestas a mano.
    //   F12 > Maquinas > "Rehacer las maquinas automaticas": en la siguiente raid se tiran las automaticas de ese mapa y se buscan otras.
    // ===================================================================================================================
    internal static class MachineAuto
    {
        internal enum Mode { Off, Fixed, EveryRaid }

        private const float FromContainerMin = 1.3f, FromContainerMax = 2.4f;
        private const float WallReach = 1.2f, WallGap = 0.5f;          // la maquina mide ~0.85 de fondo: su centro a 0.5 m de la pared
        private const float MinSpacing = 10f;
        private const int MaxContainers = 450, MaxCandidates = 400;
        private static readonly Vector3 Half = new Vector3(0.5f, 1.0f, 0.4f);   // media caja de la maquina (ancho, alto, fondo)

        private static readonly int RayMask = LayerMask.GetMask("HighPolyCollider", "LowPolyCollider", "Terrain", "Default");

        private struct Cand { public Vector3 Pos; public float Yaw; }

        // piezas en orden de prioridad (si hay pocos sitios, se quedan las primeras)
        private static List<string> Pieces()
        {
            var l = new List<string> { Machines.Wunderfizz, "speedcola", "juggernog", "staminup", "phdflopper", "widowswine", "deadshot",
                                       Machines.Box, Machines.Box, Machines.Tedd, Machines.Box, Machines.Tedd, Machines.Power };
            return l;
        }

        internal static List<Machines.Spot> Generate(string map, Vector3 near)
        {
            var t0 = Time.realtimeSinceStartup;
            var anchors = UnityEngine.Object.FindObjectsOfType<LootableContainer>()
                .Where(c => c != null && c.gameObject.activeInHierarchy).Select(c => c.transform.position).ToList();
            // mapas con pocos contenedores: puntos andables al azar alrededor del jugador
            for (int i = 0; anchors.Count < 60 && i < 400; i++)
            {
                var r = UnityEngine.Random.insideUnitCircle * 250f;
                NavMeshHit h;
                if (NavMesh.SamplePosition(near + new Vector3(r.x, 0f, r.y), out h, 20f, NavMesh.AllAreas)) anchors.Add(h.position);
            }
            Shuffle(anchors);
            if (anchors.Count > MaxContainers) anchors.RemoveRange(MaxContainers, anchors.Count - MaxContainers);

            var cands = new List<Cand>();
            foreach (var a in anchors)
            {
                if (cands.Count >= MaxCandidates) break;
                float start = UnityEngine.Random.value * 360f;
                for (int k = 0; k < 8; k++)
                {
                    var dir = Quaternion.Euler(0f, start + k * 45f, 0f) * Vector3.forward;
                    Cand c;
                    if (TrySpot(a + dir * UnityEngine.Random.Range(FromContainerMin, FromContainerMax), a.y, out c)) { cands.Add(c); break; }   // uno por contenedor
                }
            }

            var pieces = Pieces();
            var chosen = new List<Cand>();
            if (cands.Count > 0)
            {
                chosen.Add(cands[UnityEngine.Random.Range(0, cands.Count)]);
                while (chosen.Count < pieces.Count)
                {
                    // los 6 mas alejados de todo lo elegido; de ellos uno al azar (para que no salga siempre igual)
                    var best = cands.Select(c => new { c, d = chosen.Min(x => (x.Pos - c.Pos).sqrMagnitude) })
                                    .Where(x => x.d >= MinSpacing * MinSpacing).OrderByDescending(x => x.d).Take(6).ToList();
                    if (best.Count == 0) break;
                    chosen.Add(best[UnityEngine.Random.Range(0, best.Count)].c);
                }
            }
            // reparto al azar entre los sitios elegidos (si sobran piezas, se quedan fuera las ultimas de la lista)
            pieces = pieces.Take(chosen.Count).ToList();
            Shuffle(chosen);
            var res = new List<Machines.Spot>();
            for (int i = 0; i < pieces.Count; i++)
                res.Add(new Machines.Spot { map = map, perk = pieces[i], x = R(chosen[i].Pos.x), y = R(chosen[i].Pos.y), z = R(chosen[i].Pos.z), rot = R(chosen[i].Yaw), auto = true });
            Dbg.Log("MAQUINAS", "automaticas en " + map + ": " + res.Count + " piezas de " + cands.Count + " sitios validos (" + anchors.Count + " puntos de partida, " +
                ((Time.realtimeSinceStartup - t0) * 1000f).ToString("0") + " ms)");
            return res;
        }

        // un sitio junto a una pared: devuelve la posicion (base de la maquina) y el giro (+Z hacia la pared, el frente -Z mira a la sala)
        private static bool TrySpot(Vector3 p, float anchorY, out Cand c)
        {
            c = default(Cand);
            NavMeshHit nh;
            if (!NavMesh.SamplePosition(p, out nh, 0.8f, NavMesh.AllAreas)) return false;
            var g = nh.position;
            if (Mathf.Abs(g.y - anchorY) > 1.3f) return false;
            RaycastHit fh;
            if (!Physics.Raycast(g + Vector3.up * 0.6f, Vector3.down, out fh, 1.5f, RayMask, QueryTriggerInteraction.Ignore) || fh.normal.y < 0.9f) return false;
            g.y = fh.point.y;

            // la pared mas cercana (8 direcciones a 1 m de altura)
            Vector3 eye = g + Vector3.up * 1.0f;
            float bestD = WallReach; Vector3 wallN = Vector3.zero, wallP = Vector3.zero;
            for (int k = 0; k < 8; k++)
            {
                var d = Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward;
                RaycastHit wh;
                if (!Physics.Raycast(eye, d, out wh, WallReach, RayMask, QueryTriggerInteraction.Ignore)) continue;
                if (Mathf.Abs(wh.normal.y) > 0.25f || wh.distance >= bestD) continue;
                bestD = wh.distance; wallN = new Vector3(wh.normal.x, 0f, wh.normal.z).normalized; wallP = wh.point;
            }
            if (wallN == Vector3.zero) return false;
            // pared ancha: dos rayos paralelos a los lados tambien la tocan
            var side = Vector3.Cross(Vector3.up, wallN);
            foreach (float s in new[] { -0.45f, 0.45f })
                if (!Physics.Raycast(eye + side * s + wallN * 0.3f, -wallN, bestD + 0.6f, RayMask, QueryTriggerInteraction.Ignore)) return false;

            var pos = new Vector3(wallP.x + wallN.x * WallGap, g.y, wallP.z + wallN.z * WallGap);
            if (!Physics.Raycast(pos + Vector3.up * 0.6f, Vector3.down, out fh, 1.0f, RayMask, QueryTriggerInteraction.Ignore) || fh.normal.y < 0.9f || Mathf.Abs(fh.point.y - g.y) > 0.2f) return false;
            pos.y = fh.point.y;
            // suelo bajo las cuatro esquinas
            foreach (var o in new[] { side * 0.45f + wallN * 0.35f, -side * 0.45f + wallN * 0.35f, side * 0.45f - wallN * 0.35f, -side * 0.45f - wallN * 0.35f })
                if (!Physics.Raycast(pos + o + Vector3.up * 0.4f, Vector3.down, out fh, 0.7f, RayMask, QueryTriggerInteraction.Ignore) || Mathf.Abs(fh.point.y - pos.y) > 0.15f) return false;

            float yaw = Mathf.Atan2(-wallN.x, -wallN.z) * Mathf.Rad2Deg;           // +Z hacia la pared
            var rot = Quaternion.Euler(0f, yaw, 0f);
            if (Physics.CheckBox(pos + Vector3.up * (Half.y + 0.12f), Half, rot, RayMask, QueryTriggerInteraction.Ignore)) return false;   // hueco libre
            if (Physics.Raycast(pos + Vector3.up * 0.3f, Vector3.up, 2.4f, RayMask, QueryTriggerInteraction.Ignore)) return false;          // techo alto
            if (Physics.Raycast(pos + Vector3.up * 1.0f, wallN, 1.6f, RayMask, QueryTriggerInteraction.Ignore)) return false;             // paso libre delante
            if (!NavMesh.SamplePosition(pos + wallN * 1.1f, out nh, 0.4f, NavMesh.AllAreas)) return false;                                  // y se puede llegar
            c = new Cand { Pos = pos, Yaw = (yaw + 360f) % 360f };
            return true;
        }

        private static void Shuffle<T>(List<T> l)
        {
            for (int i = l.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); var t = l[i]; l[i] = l[j]; l[j] = t; }
        }

        private static float R(float v) { return (float)Math.Round(v, 2); }
    }
}
