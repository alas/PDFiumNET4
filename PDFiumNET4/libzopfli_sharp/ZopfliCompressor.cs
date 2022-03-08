using System;
using System.Runtime.InteropServices;

namespace LibZopfliSharp.Native
{
    public class ZopfliCompressor
    {
        /// <summary>
        /// Compresses according to the given output format and appends the result to the output.
        /// </summary>
        /// <param name="options">Zopfli program options</param>
        /// <param name="output_type">The output format to use</param>
        /// <param name="data">Pointer to the data</param>
        /// <param name="data_size">This is the size of the memory block pointed to by data</param>
        /// <param name="data_out">Pointer to the dynamic output array to which the result is appended</param>
        /// <param name="data_out_size">This is the size of the memory block pointed to by the dynamic output array size</param>
        [DllImport("zopfli.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ZopfliCompress(ref ZopfliOptions options, ZopfliFormat output_type, byte[] data, int data_size, ref IntPtr data_out, ref UIntPtr data_out_size);
    }

    public class ZopfliPNGCompressor
    {
        /// <summary>
        /// Library to recompress and optimize PNG images. Uses Zopfli as the compression backend, chooses optimal PNG color model, and tries out several PNG filter strategies.
        /// </summary>
        /// <param name="datain">Binary array to the PNG data</param>
        /// <param name="datainsize">Size of binary data in.</param>
        /// <param name="dataout">Binary array to which the result is appended</param>
        /// <returns>Returns data size on success, error code otherwise.</returns>
        [DllImport("zopfli.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ZopfliPNGExternalOptimize(byte[] datain, int datainsize, ref IntPtr dataout);
    }
}
