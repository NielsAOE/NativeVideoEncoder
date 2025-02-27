using System;
using System.Runtime.InteropServices;

namespace NielsOstman.NativeVideoRecorder.Encoding {
    /// <summary>
    /// Represents the configuration for the video encoder.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct EncoderConfigC {
        /// <summary>
        /// Width of the video frames.
        /// </summary>
        public int width;

        /// <summary>
        /// Height of the video frames.
        /// </summary>
        public int height;

        /// <summary>
        /// Frames per second.
        /// </summary>
        public int fps;

        /// <summary>
        /// Target bitrate in kilobits per second.
        /// </summary>
        public int bitrate;

        /// <summary>
        /// Codec option (1 for VP8, 2 for VP9).
        /// </summary>
        public int codecOption;

        /// <summary>
        /// Output directory for the encoded video.
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)] public string outputDirectory;

        /// <summary>
        /// Output filename.
        /// </summary>
        [MarshalAs(UnmanagedType.LPStr)] public string outputFilename;
    }

    /// <summary>
    /// Provides a managed interface to the native video encoder library.
    /// </summary>
    public static class VideoEncoderPlugin {
        private const string LIB_NAME =
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            "libUnityNativeVideoEncoder.dll";
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            "libUnityNativeVideoEncoder";
#else
            "libUnityNativeVideoEncoder.dll"; // Fallback (may need customization)
#endif
        
        /// <summary>
        /// Creates a new video encoder instance.
        /// </summary>
        /// <returns>
        /// An IntPtr handle to the video encoder instance.
        /// This handle must be passed to subsequent API calls.
        /// </returns>
        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CreateVideoEncoder();

        /// <summary>
        /// Initializes the video encoder with the specified configuration.
        /// </summary>
        /// <param name="handle">
        /// The video encoder handle obtained from CreateVideoEncoder.
        /// </param>
        /// <param name="config">
        /// The encoder configuration parameters.
        /// </param>
        /// <returns>
        /// True if the encoder is successfully initialized; otherwise, false.
        /// </returns>
        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool InitializeVideoEncoder(IntPtr handle, ref EncoderConfigC config);

        /// <summary>
        /// Encodes a single video frame.
        /// </summary>
        /// <param name="handle">
        /// The video encoder handle.
        /// </param>
        /// <param name="frameData">
        /// A byte array containing the raw RGB frame data.
        /// </param>
        /// <param name="frameIndex">
        /// The index of the frame to encode (used for timestamping).
        /// </param>
        /// <returns>
        /// True if the frame is encoded successfully; otherwise, false.
        /// </returns>
        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool EncodeVideoFrame(IntPtr handle, byte[] frameData, int frameIndex);

        /// <summary>
        /// Finalizes the video encoding process.
        /// </summary>
        /// <param name="handle">
        /// The video encoder handle.
        /// </param>
        /// <returns>
        /// True if the encoder finalizes successfully; otherwise, false.
        /// </returns>
        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool FinalizeVideoEncoder(IntPtr handle);

        /// <summary>
        /// Destroys the video encoder instance and frees any allocated resources.
        /// </summary>
        /// <param name="handle">
        /// The video encoder handle.
        /// </param>
        [DllImport(LIB_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DestroyVideoEncoder(IntPtr handle);
    }
}