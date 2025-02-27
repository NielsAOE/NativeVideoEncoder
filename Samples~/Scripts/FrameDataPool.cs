using System.Collections.Generic;

public static class FrameDataPool {
    private static readonly Stack<RollingBufferRecorder.FrameData> pool = new();

    public static RollingBufferRecorder.FrameData Get() {
        return pool.Count > 0 ? pool.Pop() : new RollingBufferRecorder.FrameData();
    }

    public static void Return(RollingBufferRecorder.FrameData frameData) {
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