using System.Runtime.InteropServices;
using System.Text;

namespace SilkThread.Core.SystemOperations;

internal sealed class NativePowerReadApi : IPowerReadApi
{
    public uint EnumerateScheme(uint index, out Guid schemeId)
    {
        uint size = 16;
        var code = PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 16 /* ACCESS_SCHEME */,
            index, out schemeId, ref size);
        return code == 0 && size != 16 ? 13u : code;
    }

    public uint ReadActiveScheme(out Guid schemeId)
    {
        schemeId = Guid.Empty;
        var code = PowerGetActiveScheme(IntPtr.Zero, out var pointer);
        try
        {
            if (code != 0) return code;
            if (pointer == IntPtr.Zero) return 13;
            schemeId = Marshal.PtrToStructure<Guid>(pointer);
            return 0;
        }
        finally
        {
            if (pointer != IntPtr.Zero) LocalFree(pointer);
        }
    }

    public uint ReadSchemeName(Guid schemeId, out string? name)
    {
        name = null;
        uint size = 0;
        var code = PowerReadFriendlyName(IntPtr.Zero, ref schemeId, IntPtr.Zero, IntPtr.Zero, null, ref size);
        if (code != 0 && code != 234) return code;
        // The UTF-16 buffer is measured in bytes. Retry if the name grows between calls.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            if (size < 2 || size > 1024 * 1024 || size % 2 != 0) return 13;
            var buffer = new byte[(int)size];
            code = PowerReadFriendlyName(IntPtr.Zero, ref schemeId, IntPtr.Zero, IntPtr.Zero, buffer, ref size);
            if (code == 234) continue;
            if (code != 0) return code;
            if (size < 2 || size > buffer.Length || size % 2 != 0) return 13;
            name = Encoding.Unicode.GetString(buffer, 0, (int)size).TrimEnd('\0');
            return 0;
        }
        return code;
    }

    public uint ReadValueIndex(Guid schemeId, Guid subgroupId, Guid settingId, bool ac, out uint value)
        => ac
            ? PowerReadACValueIndex(IntPtr.Zero, ref schemeId, ref subgroupId, ref settingId, out value)
            : PowerReadDCValueIndex(IntPtr.Zero, ref schemeId, ref subgroupId, ref settingId, out value);

    [DllImport("powrprof.dll", ExactSpelling = true)]
    private static extern uint PowerEnumerate(IntPtr root, IntPtr scheme, IntPtr subgroup, uint access,
        uint index, out Guid buffer, ref uint bufferSize);

    [DllImport("powrprof.dll", ExactSpelling = true)]
    private static extern uint PowerGetActiveScheme(IntPtr root, out IntPtr scheme);

    [DllImport("kernel32.dll", ExactSpelling = true)]
    private static extern IntPtr LocalFree(IntPtr memory);

    [DllImport("powrprof.dll", ExactSpelling = true)]
    private static extern uint PowerReadFriendlyName(IntPtr root, ref Guid scheme, IntPtr subgroup,
        IntPtr setting, [Out] byte[]? buffer, ref uint bufferSize);

    [DllImport("powrprof.dll", ExactSpelling = true)]
    private static extern uint PowerReadACValueIndex(IntPtr root, ref Guid scheme, ref Guid subgroup,
        ref Guid setting, out uint value);

    [DllImport("powrprof.dll", ExactSpelling = true)]
    private static extern uint PowerReadDCValueIndex(IntPtr root, ref Guid scheme, ref Guid subgroup,
        ref Guid setting, out uint value);
}
