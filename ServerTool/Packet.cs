using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace client_test
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Packet<T> where T : class
    {
        public Packet()
        {

        }

        public byte[] Serialize()
        {
            var size = Marshal.SizeOf(typeof(T));
            var array = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, array, 0, size);
            Marshal.FreeHGlobal(ptr);
            return array;
        }

        public static T Deserialize(byte[] array)
        {
            var size = Marshal.SizeOf(typeof(T));
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(array, 0, ptr, size);
            var s = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return s;
        }
    }


    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class LoginPacket : Packet<LoginPacket>
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string m_id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string m_pw;

        public LoginPacket()
        {

        }
    }
}