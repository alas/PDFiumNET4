using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CppSharp.Runtime
{
    //from: https://github.com/mono/CppSharp/

    public unsafe static class CppSharp
    {
        public static string GetString(Encoding encoding, IntPtr str)
        {
            if (str == IntPtr.Zero)
                return null;

            int byteCount = 0;

            if (encoding == Encoding.UTF32)
            {
                var str32 = (int*)str;
                while (*(str32++) != 0) byteCount += sizeof(int);
            }
            else if (encoding == Encoding.Unicode || encoding == Encoding.BigEndianUnicode)
            {
                var str16 = (short*)str;
                while (*(str16++) != 0) byteCount += sizeof(short);
            }
            else
            {
                var str8 = (byte*)str;
                while (*(str8++) != 0) byteCount += sizeof(byte);
            }

            var arr = new byte[byteCount];
            Marshal.Copy(str, arr, 0, byteCount);

            return encoding.GetString(arr, 0, byteCount);
        }
    }

    // HACK: .NET Standard 2.0 which we use in auto-building to support .NET Framework, lacks UnmanagedType.LPUTF8Str
    public class UTF8Marshaller : ICustomMarshaler
    {
        public void CleanUpManagedData(object ManagedObj)
        {
        }

        public void CleanUpNativeData(IntPtr pNativeData) => Marshal.FreeHGlobal(pNativeData);

        public int GetNativeDataSize() => -1;

        public IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null)
                return IntPtr.Zero;
            if (!(managedObj is string))
                throw new MarshalDirectiveException(
                    "UTF8Marshaler must be used on a string.");

            // not null terminated
            byte[] strbuf = Encoding.UTF8.GetBytes((string)managedObj);
            IntPtr buffer = Marshal.AllocHGlobal(strbuf.Length + 1);
            Marshal.Copy(strbuf, 0, buffer, strbuf.Length);

            // write the terminating null
            Marshal.WriteByte(buffer + strbuf.Length, 0);
            return buffer;
        }

        public unsafe object MarshalNativeToManaged(IntPtr str)
        {
            if (str == IntPtr.Zero)
                return null;

            int byteCount = 0;
            var str8 = (byte*)str;
            while (*(str8++) != 0) byteCount += sizeof(byte);

            var arr = new byte[byteCount];
            Marshal.Copy(str, arr, 0, byteCount);

            return Encoding.UTF8.GetString(arr, 0, byteCount);
        }

        public static ICustomMarshaler GetInstance(string pstrCookie)
        {
            if (marshaler == null)
                marshaler = new UTF8Marshaller();
            return marshaler;
        }

        private static UTF8Marshaller marshaler;
    }
}