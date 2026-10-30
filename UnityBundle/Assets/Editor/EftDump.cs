using System.IO;
using System.Text;
using UnityEngine;

namespace TarkovaCola
{
    // Vuelca la jerarquia y componentes de los prefabs de un bundle de EFT (EFT_DUMP = ruta como en Windows.json; salida en Preview/)
    public static class EftDump
    {
        public static void Run()
        {
            string rel = System.Environment.GetEnvironmentVariable("EFT_DUMP");
            var ab = EftBundles.Load(rel);
            var sb = new StringBuilder();
            foreach (var name in ab.GetAllAssetNames())
            {
                sb.AppendLine("### " + name);
                var go = ab.LoadAsset<GameObject>(name);
                if (go != null) Walk(go.transform, 0, sb);
            }
            Directory.CreateDirectory("Preview");
            File.WriteAllText(Path.Combine("Preview", Path.GetFileNameWithoutExtension(rel) + ".txt"), sb.ToString());
            EftBundles.UnloadAll();
        }

        private static void Walk(Transform t, int d, StringBuilder sb)
        {
            sb.Append(' ', d * 2).Append(t.name);
            foreach (var c in t.GetComponents<Component>())
            {
                if (c is Transform) continue;
                sb.Append(" [").Append(c == null ? "MISSING" : c.GetType().Name);
                if (c is MeshFilter mf && mf.sharedMesh != null) sb.Append(":").Append(mf.sharedMesh.name).Append(" v").Append(mf.sharedMesh.vertexCount);
                sb.Append("]");
            }
            sb.AppendLine(" p" + t.localPosition.ToString("F3"));
            for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), d + 1, sb);
        }
    }
}
