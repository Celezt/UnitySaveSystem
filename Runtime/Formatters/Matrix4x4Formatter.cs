using Sirenix.Serialization;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[assembly: RegisterFormatter(typeof(Matrix4x4Formatter))]
internal class Matrix4x4Formatter : MinimalBaseFormatter<Matrix4x4>
{
    private static readonly Serializer<float> _floatSerializer = Serializer.Get<float>();

    protected override void Read(ref Matrix4x4 value, IDataReader reader)
    {
        value.m00 = _floatSerializer.ReadValue(reader);
        value.m01 = _floatSerializer.ReadValue(reader);
        value.m02 = _floatSerializer.ReadValue(reader);
        value.m03 = _floatSerializer.ReadValue(reader);
        value.m10 = _floatSerializer.ReadValue(reader);
        value.m11 = _floatSerializer.ReadValue(reader);
        value.m12 = _floatSerializer.ReadValue(reader);
        value.m13 = _floatSerializer.ReadValue(reader);
        value.m20 = _floatSerializer.ReadValue(reader);
        value.m21 = _floatSerializer.ReadValue(reader);
        value.m22 = _floatSerializer.ReadValue(reader);
        value.m23 = _floatSerializer.ReadValue(reader);
        value.m30 = _floatSerializer.ReadValue(reader);
        value.m31 = _floatSerializer.ReadValue(reader);
        value.m32 = _floatSerializer.ReadValue(reader);
        value.m33 = _floatSerializer.ReadValue(reader);
    }

    protected override void Write(ref Matrix4x4 value, IDataWriter writer)
    {
        _floatSerializer.WriteValue(value.m00, writer);
        _floatSerializer.WriteValue(value.m01, writer);
        _floatSerializer.WriteValue(value.m02, writer);
        _floatSerializer.WriteValue(value.m03, writer);
        _floatSerializer.WriteValue(value.m10, writer);
        _floatSerializer.WriteValue(value.m11, writer);
        _floatSerializer.WriteValue(value.m12, writer);
        _floatSerializer.WriteValue(value.m13, writer);
        _floatSerializer.WriteValue(value.m20, writer);
        _floatSerializer.WriteValue(value.m21, writer);
        _floatSerializer.WriteValue(value.m22, writer);
        _floatSerializer.WriteValue(value.m23, writer);
        _floatSerializer.WriteValue(value.m30, writer);
        _floatSerializer.WriteValue(value.m31, writer);
        _floatSerializer.WriteValue(value.m32, writer);
        _floatSerializer.WriteValue(value.m33, writer);
    }
}
