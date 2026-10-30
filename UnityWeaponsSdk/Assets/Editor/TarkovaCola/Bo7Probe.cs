using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TarkovaCola
{
    // Vuelca, de cada FBX de Assets/Models/bo7, los tag_* y la caja de sus mallas (en el espacio de la raiz del FBX)
    public static class Bo7Probe
    {
        public static void Run()
        {
            AssetDatabase.Refresh();
            var sb = new StringBuilder();
            foreach (var dir in Directory.GetDirectories("Assets/Models/bo7"))
            {
                string n = Path.GetFileName(dir);
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(dir.Replace('\\', '/') + "/" + n + ".fbx");
                if (fbx == null) { sb.AppendLine(n + ": sin fbx"); continue; }
                var go = Object.Instantiate(fbx);
                var rs = go.GetComponentsInChildren<Renderer>();
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                sb.AppendLine("== " + n + "  root scale " + go.transform.localScale.ToString("F3") + "  bounds c" + b.center.ToString("F3") + " s" + b.size.ToString("F3") + "  renderers " + rs.Length);
                foreach (var t in go.GetComponentsInChildren<Transform>())
                    if (t.name.StartsWith("tag_") || t.name == "j_gun" || t.name.StartsWith("j_mag"))
                        sb.AppendLine("   " + t.name + " " + t.position.ToString("F4") + " r" + t.rotation.eulerAngles.ToString("F0"));
                Object.DestroyImmediate(go);
            }
            Directory.CreateDirectory("Preview");
            File.WriteAllText("Preview/bo7_probe.txt", sb.ToString());
        }
    }
}
