using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Universe.Utility;

namespace NFT.NavyReader
{
    using EPA = ProcessAccessFlags;
    using EMP = MemoryProtectionType;
    using EMA = MemoryAllocationType;
    using SI = SYSTEM_INFO;
    //using MI = MEMORY_BASIC_INFORMATION;
    using MI32 = MEMORY_BASIC_INFORMATION32;
    using MI64 = MEMORY_BASIC_INFORMATION64;
    using MI = MMEMORY_BASIC_INFORMATION;

    [Flags]
    public enum ProcessAccessFlags : uint
    {
        All = 0x001F0FFF,
        PROCESS_TERMINATE = 0x00000001,
        PROCESS_CREATE_THREAD = 0x00000002,
        PROCESS_VM_OPERATION = 0x00000008,

        PROCESS_VM_READ = 0x00000010,
        PROCESS_VM_WRITE = 0x00000020,
        PROCESS_DUP_HANDLE = 0x00000040,
        PROCESS_CREATE_PROCESS = 0x00000080,
        PROCESS_SET_QUOTA = 0x00000100,
        PROCESS_SET_INFORMATION = 0x00000200,
        PROCESS_QUERY_INFORMATION = 0x00000400,
        PROCESS_SUSPEND_RESUME = 0x00000800,
        PROCESS_QUERY_LIMITED_INFORMATION = 0x00001000,
        DELETE = 0x00010000,
        READ_CONTROL = 0x00020000,
        WRITE_DAC = 0x00040000,
        WRITE_OWNER = 0x00080000,
        SYNCHRONIZE = 0x00100000,
    }
    [StructLayout(LayoutKind.Sequential)]
    struct SYSTEM_INFO
    {
        public ushort processorArchitecture;
        ushort reserved;
        public uint pageSize;
        public IntPtr minimumApplicationAddress;  // minimum address
        public IntPtr maximumApplicationAddress;  // maximum address
        public IntPtr activeProcessorMask;
        public uint numberOfProcessors;
        public uint processorType;
        public uint allocationGranularity;
        public ushort processorLevel;
        public ushort processorRevision;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct MEMORY_BASIC_INFORMATION32
    {
        public uint BaseAddress;
        public uint AllocationBase;
        public uint AllocationProtect;
        public uint RegionSize;   // size of the region allocated by the program
        public EMA State;   // check if allocated (MEM_COMMIT)
        public EMP Protect; // page protection (must be PAGE_READWRITE)
        public uint lType;
        public static uint Size = (uint)Marshal.SizeOf<MI32>();
    }
    [StructLayout(LayoutKind.Sequential)]
    struct MEMORY_BASIC_INFORMATION64
    {
        public ulong BaseAddress;
        public ulong AllocationBase;
        public int AllocationProtect;
        public int __alignment1;
        public ulong RegionSize;
        public EMA State;
        public EMP Protect;
        public int Type;
        public int __alignment2;
        public static uint Size = (uint)Marshal.SizeOf<MI64>();
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct MMEMORY_BASIC_INFORMATION
    {
        [FieldOffset(0)] internal MI32 mi32;
        [FieldOffset(0)] internal MI64 mi64;
        public EMA State => Is32 ? mi32.State : mi64.State;
        public EMP Protect => Is32 ? mi32.Protect : mi64.Protect;
        public uint RegionSize => Is32 ? mi32.RegionSize : (uint)mi64.RegionSize;
        public IntPtr BaseAddress => Is32 ? (IntPtr)mi32.BaseAddress : (IntPtr)mi64.BaseAddress;
        public static bool Is32 = IntPtr.Size == 4;
        public static uint Size = Is32 ? MI32.Size : MI64.Size;
    }


    public enum MemoryProtectionType : uint
    {
        PAGE_NOACCESS = 0x01,
        PAGE_READONLY = 0x02,
        PAGE_READWRITE = 0x04,
        PAGE_WRITECOPY = 0x08,
        PAGE_EXECUTE = 0x10,
        PAGE_EXECUTE_READ = 0x20,
        PAGE_EXECUTE_READWRITE = 0x40,
        PAGE_EXECUTE_WRITECOPY = 0x80,

        PAGE_GUARD = 0x100,
        PAGE_NOCACHE = 0x200,
        PAGE_WRITECOMBINE = 0x400
    }
    public enum MemoryAllocationType : uint
    {
        MEM_COMMIT = 0x00001000,
        MEM_RESERVE = 0x00002000,
        MEM_RESET = 0x00080000,
        MEM_RESET_UNDO = 0x01000000,

        MEM_LARGE_PAGES = 0x20000000,// with MEM_RESERVE and MEM_COMMIT.
        MEM_PHYSICAL = 0x00400000, //with MEM_RESERVE 
        MEM_TOP_DOWN = 0x00100000,

    }

    class ProcessLogic : IDisposable
    {
        (IntPtr min, IntPtr max) _appAddress;
        SI _si;

        Process _process;
        public Process Process => _process;
        public IntPtr WindowHandle => _process?.MainWindowHandle ?? IntPtr.Zero;

        IntPtr _handle;
        public ProcessLogic(string processName)
        {
            _si = GetSystemInfo();
            _appAddress = (_si.minimumApplicationAddress, _si.maximumApplicationAddress);

            _process = Process.GetProcessesByName(processName).First();
            _handle = OpenProcess(EPA.All, false, _process.Id);
            //_handle = OpenProcess(EPA.PROCESS_VM_READ | EPA.PROCESS_QUERY_INFORMATION, false, _process.Id);
            _isWow64 = IsWow64();
        }
        public void Dispose()
        {
            if (_handle != null) CloseHandle(_handle);
        }
        bool _isWow64;
        public override string ToString()
            => $"ProcessName= {_process.ProcessName}, ProcessHandle= {_handle}, MainWindowHandle= {_process.MainWindowHandle}";

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(EPA processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, uint dwSize, out int lpNumberOfBytesRead);
        public byte[] ReadProcessMemory(IntPtr address, uint size)
        {
            byte[] buffer = new byte[size];
            var result = ReadProcessMemory(_handle, address, buffer, size, out var numBytes);
            if (!result || numBytes != size)
                throw new Exception($"ReadProcessMemory(0x{address:X}) failed: numBytes={numBytes}, error code={Marshal.GetLastWin32Error()}");
            return buffer;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);


        [DllImport("kernel32.dll")]
        static extern void GetNativeSystemInfo(out SI lpSystemInfo);
        public static SI GetSystemInfo()
        {
            GetNativeSystemInfo(out var si);
            return si;
        }

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "VirtualQueryEx")]
        static extern int VirtualQueryEx32(IntPtr hProcess, IntPtr lpAddress, ref MI32 lpBuffer, uint dwLength);
        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "VirtualQueryEx")]
        static extern int VirtualQueryEx64(IntPtr hProcess, IntPtr lpAddress, ref MI64 lpBuffer, uint dwLength);
        public MI VirtualQuery(IntPtr address)
        {
            var mi = new MI();
            var result = MI.Is32 ? VirtualQueryEx32(_handle, address, ref mi.mi32, MI.Size) : VirtualQueryEx64(_handle, address, ref mi.mi64, MI.Size);
            if (result == 0) throw new Exception($"VirtualQueryEx{(MI.Is32 ? "32" : "64")}() failed: error code={Marshal.GetLastWin32Error()}");
            return mi;
        }
        public List<IntPtr> Search(int value)
        {
            var address = new List<IntPtr>();
            long min = (long)_appAddress.min;
            long max = (long)_appAddress.max;
            while (min < max)
            {
                var mi = VirtualQuery((IntPtr)min);
                if (min % 4 != 0) throw new Exception($"min %4 != 0");
                if (mi.RegionSize % 4 != 0) throw new Exception($"mi.RegionSize %4 != 0");
                if ((mi.Protect == EMP.PAGE_READWRITE) && mi.State == EMA.MEM_COMMIT)//PAGE_EXECUTE_READWRITE
                //if ((mi.Protect == EMP.PAGE_EXECUTE_READWRITE || mi.Protect == EMP.PAGE_READWRITE ) && mi.State == EMA.MEM_COMMIT)//PAGE_EXECUTE_READWRITE
                //if (mi.State == EMA.MEM_COMMIT)
                {
                    var buffer = ReadProcessMemory(mi.BaseAddress, mi.RegionSize);
                    var loop = (int)mi.RegionSize / 4;
                    for (int i = 0; i < loop; i += 4) if (BitConverter.ToInt32(buffer, i) == value) address.Add((IntPtr)(min + i));
                }
                min += mi.RegionSize;
            }
            return address;
        }
        public Dictionary<IntPtr, int> ReadAll()
        {
            var dic = new Dictionary<IntPtr, int>();

            //long min = (long)_appAddress.min;
            long max = _isWow64 ? 0xFFFFFFFF : (long)_appAddress.max;
            long min = 0x0;// (long)_appAddressRange.min;
            //long max = 0xffffffff;// (long)_appAddressRange.max;
            while (min < max)
            {
                var isRW = false;
                var mi = VirtualQuery((IntPtr)min);
                isRW = (mi.Protect == EMP.PAGE_READWRITE || mi.Protect == EMP.PAGE_READONLY) && mi.State == EMA.MEM_COMMIT;

                if (isRW)
                {
                    var buffer = ReadProcessMemory(mi.BaseAddress, mi.RegionSize);
                    var loop = mi.RegionSize / 4;
                    for (int i = 0; i < loop; i += 4)
                    {
                        dic[(IntPtr)(min + i)] = BitConverter.ToInt32(buffer, i);
                        //dic[min + i] = (int)BitConverter.ToUInt32(buffer, i);
                    }
                }
                min += mi.RegionSize;
            }
            return dic;// (address, values);
        }


        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWow64Process([In] IntPtr processHandle, [Out, MarshalAs(UnmanagedType.Bool)] out bool wow64Process);
        bool IsWow64()
        {
            if (!IsWow64Process(_handle, out bool is64)) throw new Exception($"IsWow64Process() failed: error code={Marshal.GetLastWin32Error()}");
            return is64;
        }

    }
}
