using System;

namespace DebugObjectBrowser {
	public static class TypeUtil {
		public static readonly Type[] Primitives = {
			typeof(Boolean), typeof(Byte), typeof(SByte), typeof(Int16), typeof(UInt16), typeof(Int32), typeof(UInt32), typeof(Int64), typeof(UInt64), typeof(IntPtr),
			typeof(UIntPtr), typeof(Char), typeof(Double), typeof(Single)
		};
	}
}