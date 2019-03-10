using System;

namespace DebugObjectBrowser {
	[Flags]
	public enum DisplayOption {
		Fields = 1,
		Properties = 2
	}

	public static class DisplayOptionUtils {
		public static readonly string[] Names = Enum.GetNames(typeof(DisplayOption));
		
		public static bool IsSet(this DisplayOption flags, DisplayOption value) {
			return (flags & value) != 0;
		}

		public static bool IsSet(this DisplayOption flags, int optionIndex) {
			return flags.IsSet(GetByIndex(optionIndex));
		}

		public static DisplayOption With(this DisplayOption flags, DisplayOption value, bool enabled) {
			if (enabled) return flags | value;
			else return flags & ~value;
		}

		public static DisplayOption With(this DisplayOption flags, int optionIndex, bool enabled) {
			return flags.With(GetByIndex(optionIndex), enabled);
		}

		public static DisplayOption GetByIndex(int optionIndex) {
			return (DisplayOption) (1 << optionIndex);
		}
	}
}