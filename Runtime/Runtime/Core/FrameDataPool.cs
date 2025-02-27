using System.Collections.Generic;

namespace NielsOstman.NativeVideoRecorder.Core {
    public static class FrameDataPool {
        private static readonly Stack<FrameData> pool = new();

        public static FrameData Get() {
            return pool.Count > 0 ? pool.Pop() : new FrameData();
        }

        public static void Return(FrameData frameData) {
            if (frameData == null)
                return;

            if (frameData.RawData != null) {
                ByteArrayPool.Return(frameData.RawData);
                frameData.RawData = null;
            }

            frameData.FrameIndex = 0;
            pool.Push(frameData);
        }
    }
}