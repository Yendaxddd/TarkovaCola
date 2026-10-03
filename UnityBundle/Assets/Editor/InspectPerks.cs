using System.Text;
using UnityEditor;
using UnityEngine;

namespace TarkovaCola
{
    public static class InspectPerks
    {
        public static void Run()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/perks/perks.fbx");
            var sb = new StringBuilder();
            Walk(model.transform, 0, sb);
            System.IO.File.WriteAllText("perks_hierarchy.txt", sb.ToString());
            Debug.Log("[TarkovaCola] jerarquia escrita");
        }

        private static void Walk(Transform t, int depth, StringBuilder sb)
        {
            var mf = t.GetComponent<MeshFilter>();
            var smr = t.GetComponent<SkinnedMeshRenderer>();
            var mesh = mf != null ? mf.sharedMesh : smr != null ? smr.sharedMesh : null;
            var r = t.GetComponent<Renderer>();
            sb.Append(new string(' ', depth * 2)).Append(t.name)
              .Append("  pos=").Append(t.localPosition.ToString("F3")).Append(" scl=").Append(t.localScale.ToString("F3")).Append(" rot=").Append(t.localEulerAngles.ToString("F0"));
            if (mesh != null) sb.Append("  mesh=").Append(mesh.name).Append(" v=").Append(mesh.vertexCount).Append(" size=").Append(mesh.bounds.size.ToString("F3")).Append(" sub=").Append(mesh.subMeshCount);
            if (r != null) { sb.Append(" mats="); foreach (var m in r.sharedMaterials) sb.Append(m != null ? m.name : "null").Append(","); }
            sb.AppendLine();
            foreach (Transform c in t) Walk(c, depth + 1, sb);
        }
    }
}
