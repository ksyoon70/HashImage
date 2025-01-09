using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HashImage
{
    class ImageHashMng
    {
        const string DLL_PATH = "ImageHashMng.dll";

        [DllImport(DLL_PATH, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InitEngine(string strHashFilePath);

        [DllImport(DLL_PATH, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GenHash(string strImagePath);

        [DllImport(DLL_PATH, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetKey(string key, StringBuilder value, int keyBufferSize);

        [DllImport(DLL_PATH, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindValue(string key, StringBuilder value, int valueBufferSize);

        [DllImport(DLL_PATH, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseEngine();
    }
}
