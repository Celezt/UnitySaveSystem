using Sirenix.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[assembly: RegisterFormatter(typeof(ValueTuple2Formatter<,>))]
internal class ValueTuple2Formatter<TItem1, TItem2> : BaseFormatter<ValueTuple<TItem1, TItem2>>
{
    private static readonly Serializer<TItem1> _item1Serializer = Serializer.Get<TItem1>();
    private static readonly Serializer<TItem2> _item2Serializer = Serializer.Get<TItem2>();

    protected override void DeserializeImplementation(ref ValueTuple<TItem1, TItem2> value, IDataReader reader)
    {
        value = (_item1Serializer.ReadValue(reader), _item2Serializer.ReadValue(reader));
    }

    protected override void SerializeImplementation(ref ValueTuple<TItem1, TItem2> value, IDataWriter writer)
    {
        _item1Serializer.WriteValue(value.Item1, writer);
        _item2Serializer.WriteValue(value.Item2, writer);
    }
}
