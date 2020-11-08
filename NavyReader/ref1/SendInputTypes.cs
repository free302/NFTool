using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NFT.NavyReader.ref1
{
    /*
     * Struct MOUSEINPUT
     * Mouse internal input struct
     * See : https://msdn.microsoft.com/en-us/library/windows/desktop/ms646273(v=vs.85).aspx
     */
    internal struct MOUSEINPUT
    {
        public Int32 X;
        public Int32 Y;
        public UInt32 MouseData;
        public UInt32 Flags;
        public UInt32 Time;
        public IntPtr ExtraInfo;
    }

    /*
     * Struct HARDWAREINPUT
     * Hardware internal input struct
     * See : https://msdn.microsoft.com/en-us/library/windows/desktop/ms646269(v=vs.85).aspx
     */
    internal struct HARDWAREINPUT
    {
        public UInt32 Msg;
        public UInt16 ParamL;
        public UInt16 ParamH;
    }

    /*
     * Struct KEYBDINPUT
     * Keyboard internal input struct (Yes, actually only this one is used, but we need the 2 others to properly send inputs)
     * See : https://msdn.microsoft.com/en-us/library/windows/desktop/ms646271(v=vs.85).aspx
     */
    internal struct KEYBDINPUT
    {
        public UInt16 KeyCode; //The keycode of the triggered key. See https://msdn.microsoft.com/en-us/library/windows/desktop/dd375731(v=vs.85).aspx
        public UInt16 Scan; //Unicode character in some keys (when flags are saying "hey, this is unicode"). Ununsed in our case.
        public UInt32 Flags; //Type of action (keyup or keydown). Specifies too if the key is a "special" key.
        public UInt32 Time; //Timestamp of the event. Ununsed in our case.
        public IntPtr ExtraInfo; //Extra information (yeah, it wasn't that hard to guess). Ununsed in our case.
    }

    /*
     * Struct MOUSEKEYBDHARDWAREINPUT
     * Union struct for key sending 
     * See : https://msdn.microsoft.com/en-us/library/windows/desktop/ms646270%28v=vs.85%29.aspx
     */

    [StructLayout(LayoutKind.Explicit)]
    internal struct MOUSEKEYBDHARDWAREINPUT
    {
        [FieldOffset(0)]
        public MOUSEINPUT Mouse;

        [FieldOffset(0)]
        public KEYBDINPUT Keyboard;

        [FieldOffset(0)]
        public HARDWAREINPUT Hardware;
    }

    /*
     * Struct INPUT
     * Input internal struct for key sending 
     * See : https://msdn.microsoft.com/en-us/library/windows/desktop/ms646270%28v=vs.85%29.aspx
     */

    internal struct INPUT
    {
        public UInt32 Type; //Type of the input (0 = Mouse, 1 = Keyboard, 2 = Hardware)
        public MOUSEKEYBDHARDWAREINPUT Data; //The union of "Mouse/Keyboard/Hardware". Only one is read, depending of the type.
        public static int Size = Marshal.SizeOf(typeof(INPUT));
    }
}
