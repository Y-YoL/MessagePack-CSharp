﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.Resolvers;
using Newtonsoft.Json;
using ProtoBuf;
using SharedData;
using UnityEngine;
using ZeroFormatter;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace Sandbox
{
    [ZeroFormattable]
    [ProtoBuf.ProtoContract]
    [MessagePackObject]
    public class Person : IEquatable<Person>
    {
        [Index(0)]
        [Key(0)]
        [MsgPack.Serialization.MessagePackMember(0)]
        [ProtoMember(1)]
        public virtual int Age { get; set; }

        [Index(1)]
        [Key(1)]
        [MsgPack.Serialization.MessagePackMember(1)]
        [ProtoMember(2)]
        public virtual string FirstName { get; set; }

        [Index(2)]
        [Key(2)]
        [MsgPack.Serialization.MessagePackMember(2)]
        [ProtoMember(3)]
        public virtual string LastName { get; set; }

        [Index(3)]
        [MsgPack.Serialization.MessagePackMember(3)]
        [Key(3)]
        [ProtoMember(4)]
        public virtual Sex Sex { get; set; }

        public bool Equals(Person other)
        {
            return this.Age == other.Age && this.FirstName == other.FirstName && this.LastName == other.LastName && this.Sex == other.Sex;
        }
    }

    public enum Sex : sbyte
    {
        Unknown,
        Male,
        Female,
    }

    public class TestCollection<T> : ICollection<T>
    {
        public List<T> internalCollection = new List<T>();

        public int Count => this.internalCollection.Count;

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(T item)
        {
            this.internalCollection.Add(item);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.internalCollection.GetEnumerator();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    [MessagePackObject(true)]
    public class Takox
    {
#pragma warning disable SA1300 // Element should begin with upper-case letter
        public int hoga { get; set; }

        public int huga { get; set; }

        public int tako { get; set; }
#pragma warning restore SA1300 // Element should begin with upper-case letter
    }

    [MessagePackObject]
    public class MyClass
    {
        // Key is serialization index, it is important for versioning.
        [Key(0)]
        public int Age { get; set; }

        [Key(1)]
        public string FirstName { get; set; }

        [Key(2)]
        public string LastName { get; set; }

        // public members and does not serialize target, mark IgnoreMemberttribute
        [IgnoreMember]
        public string FullName => this.FirstName + this.LastName;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class Sample1
    {
        [Key(0)]
        public int Foo { get; set; }

        [Key(1)]
        public int Bar { get; set; }
    }

    [MessagePackObject]
    public class Sample2
    {
        [Key("foo")]
        public int Foo { get; set; }

        [Key("bar")]
        public int Bar { get; set; }
    }

    [MessagePackObject]
    public class IntKeySample
    {
        [Key(3)]
        public int A { get; set; }

        [Key(10)]
        public int B { get; set; }
    }

    public class ContractlessSample
    {
        public int MyProperty1 { get; set; }

        public int MyProperty2 { get; set; }
    }

    [MessagePackObject]
    public class SampleCallback : IMessagePackSerializationCallbackReceiver
    {
        [Key(0)]
        public int Key { get; set; }

        public void OnBeforeSerialize()
        {
            Console.WriteLine("OnBefore");
        }

        public void OnAfterDeserialize()
        {
            Console.WriteLine("OnAfter");
        }
    }

    [MessagePackObject]
    public struct Point
    {
        [Key(0)]
        public readonly int X;
        [Key(1)]
        public readonly int Y;

        // can't find matched constructor parameter, parameterType mismatch. type:Point parameterIndex:0 parameterType:ValueTuple`2
        public Point((int, int) p)
        {
            this.X = p.Item1;
            this.Y = p.Item2;
        }

        [SerializationConstructor]
        public Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    }

    // mark inheritance types
    [MessagePack.Union(0, typeof(FooClass))]
    [MessagePack.Union(100, typeof(BarClass))]
    public interface IUnionSample
    {
    }

    [MessagePackObject]
    public class FooClass : IUnionSample
    {
        [Key(0)]
        public int XYZ { get; set; }
    }

    [MessagePackObject]
    public class BarClass : IUnionSample
    {
        [Key(0)]
        public string OPQ { get; set; }
    }

    [MessagePackFormatter(typeof(CustomObjectFormatter))]
    public class CustomObject
    {
        private string internalId;

        public CustomObject()
        {
            this.internalId = Guid.NewGuid().ToString();
        }

        // serialize/deserialize private field.
        internal class CustomObjectFormatter : IMessagePackFormatter<CustomObject>
        {
            public void Serialize(ref MessagePackWriter writer, CustomObject value, MessagePackSerializerOptions options)
            {
                options.Resolver.GetFormatterWithVerify<string>().Serialize(ref writer, value.internalId, options);
            }

            public CustomObject Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                var id = options.Resolver.GetFormatterWithVerify<string>().Deserialize(ref reader, options);
                return new CustomObject { internalId = id };
            }
        }
    }

    public interface IEntity
    {
        string Name { get; }
    }

    public class Event : IEntity
    {
        public Event(string name)
        {
            this.Name = name;
        }

        public string Name { get; }
    }

    public class Holder
    {
        public Holder(IEntity entity)
        {
            this.Entity = entity;
        }

        public IEntity Entity { get; }
    }

    public class Dummy___
    {
        public MethodBase MyProperty { get; set; }
    }

    [MessagePackObject]
    public class Callback1 : IMessagePackSerializationCallbackReceiver
    {
        [Key(0)]
        public int X { get; set; }

        [IgnoreMember]
        public bool CalledBefore { get; private set; }

        [IgnoreMember]
        public bool CalledAfter { get; private set; }

        public Callback1(int x)
        {
        }

        public void OnBeforeSerialize()
        {
            this.CalledBefore = true;
        }

        public void OnAfterDeserialize()
        {
            this.CalledAfter = true;
        }
    }

    [MessagePackObject]
    public class SimpleIntKeyData
    {
        [Key(0)]
        ////[MessagePackFormatter(typeof(OreOreFormatter))]
        public int Prop1 { get; set; }

        [Key(1)]
        public ByteEnum Prop2 { get; set; }

        [Key(2)]
        public string Prop3 { get; set; }

        [Key(3)]
        public SimpleStringKeyData Prop4 { get; set; }

        [Key(4)]
        public SimpleStructIntKeyData Prop5 { get; set; }

        [Key(5)]
        public SimpleStructStringKeyData Prop6 { get; set; }

        [Key(6)]
        public byte[] BytesSpecial { get; set; }

        ////[Key(7)]
        ////[MessagePackFormatter(typeof(OreOreFormatter2), 100, "hogehoge")]
        ////[MessagePackFormatter(typeof(OreOreFormatter))]
        ////public int Prop7 { get; set; }
    }

    [MessagePack.MessagePackObject(true)]
    public class StringKeySerializerTarget2
    {
        public int TotalQuestions { get; set; }

        public int TotalUnanswered { get; set; }

        public int QuestionsPerMinute { get; set; }

        public int AnswersPerMinute { get; set; }

        public int TotalVotes { get; set; }

        public int BadgesPerMinute { get; set; }

        public int NewActiveUsers { get; set; }

        public int ApiRevision { get; set; }

        public int Site { get; set; }
    }

    internal class Program
    {
        private static readonly MessagePackSerializerOptions LZ4Standard = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block);

        private static void Main(string[] args)
        {
            var option = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
            var data = Enumerable.Range(0, 10000).Select(x => new StringKeySerializerTarget2()).ToArray();

            var bin = MessagePackSerializer.Serialize(data, option);
            Console.WriteLine(bin.Length);
        }

        private static void Benchmark<T>(T target)
        {
            const int Iteration = 10000; // 10000

            var jsonSerializer = new JsonSerializer();
            MsgPack.Serialization.SerializationContext msgpack = MsgPack.Serialization.SerializationContext.Default;
            msgpack.GetSerializer<T>().PackSingleObject(target);
            MessagePackSerializer.Serialize(target);
            MessagePackSerializer.Serialize(target, LZ4Standard);
            ZeroFormatter.ZeroFormatterSerializer.Serialize(target);
            ProtoBuf.Serializer.Serialize(new MemoryStream(), target);
            jsonSerializer.Serialize(new JsonTextWriter(new StringWriter()), target);

            Console.WriteLine(typeof(T).Name + " serialization test");
            Console.WriteLine();

            Console.WriteLine("Serialize::");
            byte[] data = null;
            byte[] data0 = null;
            byte[] data1 = null;
            byte[] data2 = null;
            byte[] data3 = null;
            byte[] dataJson = null;
            byte[] dataGzipJson = null;
            using (new Measure("MsgPack-Cli"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data = msgpack.GetSerializer<T>().PackSingleObject(target);
                }
            }

            using (new Measure("MessagePack-CSharp"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data0 = MessagePackSerializer.Serialize(target);
                }
            }

            using (new Measure("MessagePack(LZ4)"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data3 = MessagePackSerializer.Serialize(target, LZ4Standard);
                }
            }

            using (new Measure("ZeroFormatter"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    data1 = ZeroFormatter.ZeroFormatterSerializer.Serialize(target);
                }
            }

            using (new Measure("JsonNet"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    using (var ms = new MemoryStream())
                    using (var sw = new StreamWriter(ms, Encoding.UTF8, 1024, true))
                    using (var jw = new JsonTextWriter(sw))
                    {
                        jsonSerializer.Serialize(jw, target);
                    }
                }
            }

            using (new Measure("JsonNet+Gzip"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    using (var ms = new MemoryStream())
                    using (var gzip = new GZipStream(ms, CompressionLevel.Fastest))
                    using (var sw = new StreamWriter(gzip, Encoding.UTF8, 1024, true))
                    using (var jw = new JsonTextWriter(sw))
                    {
                        jsonSerializer.Serialize(jw, target);
                    }
                }
            }

            using (new Measure("protobuf-net"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    using (var ms = new MemoryStream())
                    {
                        ProtoBuf.Serializer.Serialize(ms, target);
                    }
                }
            }

            using (var ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(ms, target);
                data2 = ms.ToArray();
            }

            using (var ms = new MemoryStream())
            {
                using (var sw = new StreamWriter(ms, Encoding.UTF8, 1024, true))
                using (var jw = new JsonTextWriter(sw))
                {
                    jsonSerializer.Serialize(jw, target);
                }

                dataJson = ms.ToArray();
            }

            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionLevel.Fastest))
                using (var sw = new StreamWriter(gzip, Encoding.UTF8, 1024, true))
                using (var jw = new JsonTextWriter(sw))
                {
                    jsonSerializer.Serialize(jw, target);
                }

                dataGzipJson = ms.ToArray();
            }

            msgpack.GetSerializer<T>().UnpackSingleObject(data);
            MessagePackSerializer.Deserialize<T>(data0);
            ZeroFormatterSerializer.Deserialize<T>(data1);
            ProtoBuf.Serializer.Deserialize<T>(new MemoryStream(data2));
            MessagePackSerializer.Deserialize<T>(data3, LZ4Standard);
            jsonSerializer.Deserialize<T>(new JsonTextReader(new StreamReader(new MemoryStream(dataJson))));

            Console.WriteLine();
            Console.WriteLine("Deserialize::");

            using (new Measure("MsgPack-Cli"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    msgpack.GetSerializer<T>().UnpackSingleObject(data);
                }
            }

            using (new Measure("MessagePack-CSharp"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    MessagePackSerializer.Deserialize<T>(data0);
                }
            }

            using (new Measure("MessagePack(LZ4)"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    MessagePackSerializer.Deserialize<T>(data3, LZ4Standard);
                }
            }

            using (new Measure("ZeroFormatter"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    ZeroFormatterSerializer.Deserialize<T>(data1);
                }
            }

            using (new Measure("JsonNet"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    using (var ms = new MemoryStream(dataJson))
                    using (var sr = new StreamReader(ms, Encoding.UTF8))
                    using (var jr = new JsonTextReader(sr))
                    {
                        jsonSerializer.Deserialize<T>(jr);
                    }
                }
            }

            using (new Measure("JsonNet+Gzip"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    using (var ms = new MemoryStream(dataGzipJson))
                    using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
                    using (var sr = new StreamReader(gzip, Encoding.UTF8))
                    using (var jr = new JsonTextReader(sr))
                    {
                        jsonSerializer.Deserialize<T>(jr);
                    }
                }
            }

            using (new Measure("protobuf-net"))
            {
                for (int i = 0; i < Iteration; i++)
                {
                    using (var ms = new MemoryStream(data2))
                    {
                        ProtoBuf.Serializer.Deserialize<T>(ms);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("FileSize::");
            var label = string.Empty;
            label = "MsgPack-Cli";
            Console.WriteLine($"{label,20}   {data.Length} Byte");
            label = "MessagePack-CSharp";
            Console.WriteLine($"{label,20}   {data0.Length} Byte");
            label = "MessagePack(LZ4)";
            Console.WriteLine($"{label,20}   {data3.Length} Byte");
            label = "ZeroFormatter";
            Console.WriteLine($"{label,20}   {data1.Length} Byte");
            label = "protobuf-net";
            Console.WriteLine($"{label,20}   {data2.Length} Byte");
            label = "JsonNet";
            Console.WriteLine($"{label,20}   {dataJson.Length} Byte");
            label = "JsonNet+GZip";
            Console.WriteLine($"{label,20}   {dataGzipJson.Length} Byte");

            Console.WriteLine();
            Console.WriteLine();
        }

        private static string ToHumanReadableSize(long size)
        {
            return ToHumanReadableSize(new long?(size));
        }

        private static string ToHumanReadableSize(long? size)
        {
            if (size == null)
            {
                return "NULL";
            }

            double bytes = size.Value;

            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " B";
            }

            bytes = bytes / 1024;
            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " KB";
            }

            bytes = bytes / 1024;
            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " MB";
            }

            bytes = bytes / 1024;
            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " GB";
            }

            bytes = bytes / 1024;
            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " TB";
            }

            bytes = bytes / 1024;
            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " PB";
            }

            bytes = bytes / 1024;
            if (bytes <= 1024)
            {
                return bytes.ToString("f2") + " EB";
            }

            bytes = bytes / 1024;
            return bytes + " ZB";
        }
    }

    internal struct Measure : IDisposable
    {
        private string label;
        private Stopwatch sw;

        public Measure(string label)
        {
            this.label = label;
            System.GC.Collect(2, GCCollectionMode.Forced, blocking: true);
            this.sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            this.sw.Stop();
            Console.WriteLine($"{this.label,20}   {this.sw.Elapsed.TotalMilliseconds} ms");

            System.GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        }
    }

    public class SerializerTarget
    {
        public int MyProperty1 { get; set; }

        public int MyProperty2 { get; set; }

        public int MyProperty3 { get; set; }

        public int MyProperty4 { get; set; }

        public int MyProperty5 { get; set; }

        public int MyProperty6 { get; set; }

        public int MyProperty7 { get; set; }

        public int MyProperty8 { get; set; }

        public int MyProperty9 { get; set; }
    }

    // design concept sketch of Union.
    [MessagePack.Union(0, typeof(HogeMoge1))]
    [MessagePack.Union(1, typeof(HogeMoge2))]
    public interface IHogeMoge
    {
    }

    public class HogeMoge1
    {
    }

    public class HogeMoge2
    {
    }

    [MessagePackObject]
    public class TestObject
    {
        [MessagePackObject]
        public class PrimitiveObject
        {
#pragma warning disable SA1310 // Field names should not contain underscore
            [Key(0)]
            public int v_int;

            [Key(1)]
            public string v_str;

            [Key(2)]
            public float v_float;

            [Key(3)]
            public bool v_bool;
#pragma warning restore SA1310 // Field names should not contain underscore

            public PrimitiveObject(int vi, string vs, float vf, bool vb)
            {
                this.v_int = vi;
                this.v_str = vs;
                this.v_float = vf;
                this.v_bool = vb;
            }
        }

        [Key(0)]
        public PrimitiveObject[] objectArray;

        [Key(1)]
        public List<PrimitiveObject> objectList;

        [Key(2)]
        public Dictionary<string, PrimitiveObject> objectMap;

        public void CreateArray(int num)
        {
            this.objectArray = new PrimitiveObject[num];
            for (int i = 0; i < num; i++)
            {
                this.objectArray[i] = new PrimitiveObject(i, i.ToString(), (float)i, i % 2 == 0 ? true : false);
            }
        }

        public void CreateList(int num)
        {
            this.objectList = new List<PrimitiveObject>(num);
            for (int i = 0; i < num; i++)
            {
                this.objectList.Add(new PrimitiveObject(i, i.ToString(), (float)i, i % 2 == 0 ? true : false));
            }
        }

        public void CreateMap(int num)
        {
            this.objectMap = new Dictionary<string, PrimitiveObject>(num);
            for (int i = 0; i < num; i++)
            {
                this.objectMap.Add(i.ToString(), new PrimitiveObject(i, i.ToString(), (float)i, i % 2 == 0 ? true : false));
            }
        }

        // I only tested with array
        public static TestObject TestBuild()
        {
            TestObject to = new TestObject();
            to.CreateArray(1000000);

            return to;
        }
    }

    public class HogeMogeFormatter : IMessagePackFormatter<IHogeMoge>
    {
        // Type to Key...
        private static readonly Dictionary<Type, KeyValuePair<int, int>> Map = new Dictionary<Type, KeyValuePair<int, int>>
        {
            { typeof(HogeMoge1), new KeyValuePair<int, int>(0, 0) },
            { typeof(HogeMoge2), new KeyValuePair<int, int>(1, 1) },
        };

        // If 0~10 don't need it.
        private static readonly Dictionary<int, int> KeyToJumpTable = new Dictionary<int, int>
        {
            { 0, 0 },
            { 1, 1 },
        };

        public void Serialize(ref MessagePackWriter writer, IHogeMoge value, MessagePackSerializerOptions options)
        {
            KeyValuePair<int, int> key;
            if (Map.TryGetValue(value.GetType(), out key))
            {
                writer.WriteArrayHeader(2);
                writer.WriteInt32(key.Key);

                switch (key.Value)
                {
                    case 0:
                        options.Resolver.GetFormatterWithVerify<HogeMoge1>().Serialize(ref writer, (HogeMoge1)value, options);
                        break;
                    case 1:
                        options.Resolver.GetFormatterWithVerify<HogeMoge2>().Serialize(ref writer, (HogeMoge2)value, options);
                        break;
                    default:
                        break;
                }

                return;
            }

            writer.WriteNil();
        }

        public IHogeMoge Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            // TODO:array header...
            var key = reader.ReadInt32();

            switch (key)
            {
                case 0:
                    {
                        HogeMoge1 result = options.Resolver.GetFormatterWithVerify<HogeMoge1>().Deserialize(ref reader, options);
                        return (IHogeMoge)result;
                    }

                case 1:
                    {
                        HogeMoge2 result = options.Resolver.GetFormatterWithVerify<HogeMoge2>().Deserialize(ref reader, options);
                        return (IHogeMoge)result;
                    }

                default:
                    {
                        throw new NotImplementedException();
                    }
            }
        }
    }
}
