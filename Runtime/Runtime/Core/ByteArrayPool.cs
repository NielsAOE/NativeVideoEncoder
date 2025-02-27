using System.Collections.Generic;

namespace NielsOstman.NativeVideoRecorder.Core {
    public static class ByteArrayPool {
        private static Stack<byte[]> pool = new();

        public static byte[] Get(int size) {
            if (pool.Count > 0) {
                var array = pool.Pop();
                if (array.Length >= size)
                    return array;
            }

            return new byte[size];
        }

        public static void Return(byte[] array) {
            pool.Push(array);
        }
    }
}