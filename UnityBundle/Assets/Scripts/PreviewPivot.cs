using System;
using UnityEngine;

// STUB: copia exacta de los campos serializados de PreviewPivot del juego (Assembly-CSharp).
// Al llamarse igual y vivir en Assembly-CSharp, el bundle la enlaza con la clase real de Tarkov,
// que exige este componente para poder generar el icono de inventario.
public class PreviewPivot : MonoBehaviour
{
    [Serializable]
    public class IconSettings
    {
        public Vector3 position;
        public bool hasOffset;
        public Quaternion rotation;
        public float boundsScale = 1f;
        public float perspective = 15f;
        public bool orthographic;
        public float orthographicSize = 10f;
        public Sprite overrideIcon;
    }

    public Vector3 pivotPosition = Vector3.zero;
    public Quaternion pivotRotation = Quaternion.identity;
    public Vector3 scale = Vector3.one;
    public Vector3 SpawnPosition = Vector3.zero;
    public IconSettings Icon = new IconSettings();
}
