using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFT.NavyReader
{
    enum Groth { None = -1, G잠재 = 0, G명중, G연사, G어뢰, G대공, G수리, G보수, G기관, G함재, G전투, G폭격 }


    [Flags]
    public enum ProcessAccessFlags : uint
    {
        //All = 0x001F0FFF,
        PROCESS_TERMINATE =    0x00000001,
        PROCESS_CREATE_THREAD =0x00000002,
        PROCESS_VM_OPERATION = 0x00000008,

        PROCESS_VM_READ =       0x00000010,
        PROCESS_VM_WRITE =      0x00000020,
        PROCESS_DUP_HANDLE =    0x00000040,
        PROCESS_CREATE_PROCESS =0x00000080,
        PROCESS_SET_QUOTA =         0x00000100,
        PROCESS_SET_INFORMATION =   0x00000200,
        PROCESS_QUERY_INFORMATION = 0x00000400,
        PROCESS_SUSPEND_RESUME =    0x00000800,
        PROCESS_QUERY_LIMITED_INFORMATION = 0x00001000,
        DELETE =        0x00010000,
        READ_CONTROL =  0x00020000,
        WRITE_DAC =     0x00040000,
        WRITE_OWNER =   0x00080000,
        SYNCHRONIZE =   0x00100000,
    }



    //const int PROCESS_QUERY_INFORMATION = 0x0400;
    //const int MEM_COMMIT = 0x00001000;
    //const int PAGE_READWRITE = 0x04;
    //const int PROCESS_WM_READ = 0x0010;
}
