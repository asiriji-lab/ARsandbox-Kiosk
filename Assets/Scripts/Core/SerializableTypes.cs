using UnityEngine;
using System;

namespace ARSandbox.Core
{
    /// <summary>
    /// JSON-friendly wrapper for Unity's Matrix4x4.
    /// Unity's Matrix4x4 can serialize to JSON, but this provides explicit control.
    /// </summary>
    [Serializable]
    public struct SerializableMatrix4x4
    {
        public float m00, m01, m02, m03;
        public float m10, m11, m12, m13;
        public float m20, m21, m22, m23;
        public float m30, m31, m32, m33;

        public SerializableMatrix4x4(Matrix4x4 m)
        {
            m00 = m.m00; m01 = m.m01; m02 = m.m02; m03 = m.m03;
            m10 = m.m10; m11 = m.m11; m12 = m.m12; m13 = m.m13;
            m20 = m.m20; m21 = m.m21; m22 = m.m22; m23 = m.m23;
            m30 = m.m30; m31 = m.m31; m32 = m.m32; m33 = m.m33;
        }

        public Matrix4x4 ToMatrix()
        {
            Matrix4x4 m = new Matrix4x4();
            m.m00 = m00; m.m01 = m01; m.m02 = m02; m.m03 = m03;
            m.m10 = m10; m.m11 = m11; m.m12 = m12; m.m13 = m13;
            m.m20 = m20; m.m21 = m21; m.m22 = m22; m.m23 = m23;
            m.m30 = m30; m.m31 = m31; m.m32 = m32; m.m33 = m33;
            return m;
        }

        public static SerializableMatrix4x4 Identity => new SerializableMatrix4x4(Matrix4x4.identity);
    }
}
