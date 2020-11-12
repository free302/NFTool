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
    using MI = MEMORY_BASIC_INFORMATION64;

    [Flags]
    public enum ProcessAccessFlags : uint
    {
        //All = 0x001F0FFF,
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
    public struct SYSTEM_INFO
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
    public struct MEMORY_BASIC_INFORMATION
    {
        public int BaseAddress;
        public int AllocationBase;
        public int AllocationProtect;
        public int RegionSize;   // size of the region allocated by the program
        public EMA State;   // check if allocated (MEM_COMMIT)
        public EMP Protect; // page protection (must be PAGE_READWRITE)
        public int lType;
        public static uint Size = (uint)Marshal.SizeOf<MEMORY_BASIC_INFORMATION>();
    }
    public struct MEMORY_BASIC_INFORMATION64
    {
        public long BaseAddress;
        public long AllocationBase;
        public int AllocationProtect;
        public int __alignment1;
        public long RegionSize;
        public EMA State;
        public EMP Protect;
        public int Type;
        public int __alignment2;
        public static uint Size = (uint)Marshal.SizeOf<MEMORY_BASIC_INFORMATION64>();
    }


    public enum MemoryProtectionType : int
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
    public enum MemoryAllocationType : int
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
        (IntPtr min, IntPtr max) _appAddressRange;
        SI _si;

        Process _process;
        public Process Process => _process;
        public IntPtr WindowHandle => _process?.MainWindowHandle ?? IntPtr.Zero;

        IntPtr _handle;
        public ProcessLogic(string processName)
        {
            _si = GetSystemInfo();
            _appAddressRange = (_si.minimumApplicationAddress, _si.maximumApplicationAddress);

            _process = Process.GetProcessesByName(processName).First();
            _handle = OpenProcess(EPA.PROCESS_VM_READ | EPA.PROCESS_QUERY_INFORMATION, false, _process.Id);
        }
        public void Dispose()
        {
            if (_handle != null) CloseHandle(_handle);
        }

        public override string ToString()
            => $"ProcessName= {_process.ProcessName}, ProcessHandle= {_handle}, MainWindowHandle= {_process.MainWindowHandle}";

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(EPA processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);
        public byte[] ReadProcessMemory(long address, int size)
        {
            byte[] buffer = new byte[size];
            var ipAddress = new IntPtr(address);
            var result = ReadProcessMemory(_handle, ipAddress, buffer, size, out var numBytes);
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
        static extern void GetSystemInfo(out SI lpSystemInfo);
        public static SI GetSystemInfo()
        {
            GetSystemInfo(out var si);
            return si;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, ref MI lpBuffer, uint dwLength);
        public MI VirtualQuery(long address)
        {
            var mi = new MI();
            var result = VirtualQueryEx(_handle, new IntPtr(address), ref mi, MI.Size);
            if (result == 0) throw new Exception($"VirtualQueryEx() failed: error code={Marshal.GetLastWin32Error()}"); ;
            return mi;
        }

        public List<long> Search(int value)
        {
            var address = new List<long>();
            long min = (long)_appAddressRange.min;
            long max = (long)_appAddressRange.max;
            while (min < max)
            {
                var mi = VirtualQuery(min);
                if (mi.RegionSize % 4 != 0) throw new Exception($"mi.RegionSize %4 != 0");
                if (mi.Protect == EMP.PAGE_READWRITE && mi.State == EMA.MEM_COMMIT)
                {
                    var buffer = ReadProcessMemory(mi.BaseAddress, (int)mi.RegionSize);
                    for (int i = 0; i < mi.RegionSize / 4; i += 4) if (BitConverter.ToInt32(buffer, i) == value) address.Add(min + i);
                }
                min += mi.RegionSize;
            }
            return address;
        }
    }
}
