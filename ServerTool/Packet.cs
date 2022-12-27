using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ServerTool
{
    public enum PacketType : int
    {
        Login,
        SignUp,
        IdCheck,
        DuplicationCheck
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Packet<T> where T : class
    {
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
    public class ClientPacket : Packet<ClientPacket>
    {
        public PacketType m_packetType;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 30)]
        public string m_id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 100)]
        public string m_pw;
    }


    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class ServerPacket : Packet<ServerPacket>
    {
        public PacketType m_packetType;

        [MarshalAs(UnmanagedType.Bool, SizeConst = 100)]
        public bool m_isSuccess;
    }
}