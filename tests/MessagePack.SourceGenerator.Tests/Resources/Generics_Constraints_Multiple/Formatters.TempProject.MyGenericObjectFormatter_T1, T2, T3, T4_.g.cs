﻿// <auto-generated />

#pragma warning disable 618, 612, 414, 168, CS1591, SA1129, SA1309, SA1312, SA1403, SA1649

#pragma warning disable CS8669 // We may leak nullable annotations into generated code.

namespace MessagePack {

using MsgPack = global::MessagePack;

partial class GeneratedMessagePackResolver
{
private partial class TempProject { 
	internal sealed class MyGenericObjectFormatter<T1, T2, T3, T4> : MsgPack::Formatters.IMessagePackFormatter<global::TempProject.MyGenericObject<T1, T2, T3, T4>>
		where T1 : struct
		where T2 : global::System.IDisposable, new()
		where T3 : notnull
		where T4 : unmanaged
	{

		public void Serialize(ref MsgPack::MessagePackWriter writer, global::TempProject.MyGenericObject<T1, T2, T3, T4> value, MsgPack::MessagePackSerializerOptions options)
		{
			if (value == null)
			{
				writer.WriteNil();
				return;
			}

			MsgPack::IFormatterResolver formatterResolver = options.Resolver;
			writer.WriteArrayHeader(4);
			MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<T1>(formatterResolver).Serialize(ref writer, value.Content1, options);
			MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<T2>(formatterResolver).Serialize(ref writer, value.Content2, options);
			MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<T3>(formatterResolver).Serialize(ref writer, value.Content3, options);
			MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<T4>(formatterResolver).Serialize(ref writer, value.Content4, options);
		}

		public global::TempProject.MyGenericObject<T1, T2, T3, T4> Deserialize(ref MsgPack::MessagePackReader reader, MsgPack::MessagePackSerializerOptions options)
		{
			if (reader.TryReadNil())
			{
				return null;
			}

			options.Security.DepthStep(ref reader);
			MsgPack::IFormatterResolver formatterResolver = options.Resolver;
			var length = reader.ReadArrayHeader();
			var ____result = new global::TempProject.MyGenericObject<T1, T2, T3, T4>();

			for (int i = 0; i < length; i++)
			{
				switch (i)
				{
					case 0:
						____result.Content1 = MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<T1>(formatterResolver).Deserialize(ref reader, options);
						break;
					case 1:
						____result.Content2 = MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<T2>(formatterResolver).Deserialize(ref reader, options);
						break;
					case 2:
						____result.Content3 = MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<T3>(formatterResolver).Deserialize(ref reader, options);
						break;
					case 3:
						____result.Content4 = MsgPack::FormatterResolverExtensions.GetFormatterWithVerify<T4>(formatterResolver).Deserialize(ref reader, options);
						break;
					default:
						reader.Skip();
						break;
				}
			}

			reader.Depth--;
			return ____result;
		}
	}

}}

}
