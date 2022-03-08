using System;
using System.Runtime.InteropServices;

namespace LibZopfliSharp
{
    /// <summary>
    /// Access to native lib from c#
    /// </summary>
    public static class NativeUtilities
    {
        /// <summary>
        /// Get data from unmanaged memory back to managed memory
        /// </summary>
        /// <param name="source">A Pointer where the data lifes</param>
        /// <param name="length">How many bytes you want to copy</param>
        /// <returns></returns>
        public static byte[] GetDataFromUnmanagedMemory(IntPtr source, int length)
        {
            // Initialize managed memory to hold the array
            byte[] data = new byte[length];
            // Copy the array back to managed memory
            Marshal.Copy(source, data, 0, length);
            return data;
        }
    }
}
