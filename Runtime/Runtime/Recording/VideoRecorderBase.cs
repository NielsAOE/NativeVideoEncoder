using System;
using System.Collections;
using System.IO;
using NielsOstman.NativeVideoRecorder.Core;
using NielsOstman.NativeVideoRecorder.Encoding;
using UnityEngine;
using UnityEngine.Rendering;

namespace NielsOstman.NativeVideoRecorder.Recording {
    public abstract class VideoRecorderBase : MonoBehaviour {
        [Header("Capture Settings")]
        [SerializeField] protected int captureFrameRate = 30;
        [SerializeField] protected int desiredOutputHeight = 720;
        [SerializeField] protected int bitrate = 2000;
        [SerializeField] protected string outputDirectory = "Rolling Buffer";

        protected string OutputFolderPath => Path.Combine(Application.persistentDataPath, outputDirectory);

        protected int captureWidth;
        protected int captureHeight;
        protected Texture2D captureTexture;
        protected RenderTexture captureRenderTexture;
        protected RenderTexture fullResolutionRenderTexture;

        protected int frameCounter;
        protected int pendingReadbacks = 0;
        protected bool isRecording;
        protected Coroutine captureCoroutine;

        // --- Video Encoder Fields ---
        protected IntPtr encoderHandle = IntPtr.Zero;

        protected virtual void Start() {
            InitializeCaptureSettings();
        }

        protected void InitializeCaptureSettings() {
            float heightRatio = Screen.height / (float)desiredOutputHeight;
            captureWidth = Mathf.RoundToInt(Screen.width / heightRatio);
            captureHeight = desiredOutputHeight;
            captureTexture = new Texture2D(captureWidth, captureHeight, TextureFormat.RGB24, false);
            captureRenderTexture = new RenderTexture(captureWidth, captureHeight, 24, RenderTextureFormat.ARGB32);
            fullResolutionRenderTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        }

        protected virtual void OnDestroy() {
            ReleaseResources();
        }

        protected void ReleaseResources() {
            if (captureRenderTexture != null) {
                captureRenderTexture.Release();
                Destroy(captureRenderTexture);
            }
            if (fullResolutionRenderTexture != null) {
                fullResolutionRenderTexture.Release();
                Destroy(fullResolutionRenderTexture);
            }
            if (captureTexture != null) {
                Destroy(captureTexture);
            }
            if (encoderHandle != IntPtr.Zero) {
                FinalizeEncoder(); // Optionally finalize if not already done.
                DestroyEncoder();
            }
        }

        protected IEnumerator CaptureFramesCoroutine() {
            float captureInterval = 1f / captureFrameRate;
            while (isRecording) {
                yield return new WaitForEndOfFrame();
                int currentFrameIndex = frameCounter++;

                // Capture the screen.
                ScreenCapture.CaptureScreenshotIntoRenderTexture(fullResolutionRenderTexture);
                Graphics.Blit(fullResolutionRenderTexture, captureRenderTexture);

                pendingReadbacks++;
                AsyncGPUReadback.Request(captureRenderTexture, 0, TextureFormat.RGB24, request => {
                    OnCompleteReadback(request, currentFrameIndex);
                    pendingReadbacks--;
                });

                yield return new WaitForSeconds(captureInterval);
            }
        }

        protected void OnCompleteReadback(AsyncGPUReadbackRequest request, int frameIndex) {
            if (request.hasError) {
                Debug.LogError($"GPU readback error on frame {frameIndex}");
                return;
            }
            var data = request.GetData<byte>();
            int dataLength = data.Length;
            byte[] rawData = ByteArrayPool.Get(dataLength);
            data.CopyTo(rawData);

            FrameData frameData = CreateFrameData();
            frameData.FrameIndex = frameIndex;
            frameData.RawData = rawData;

            HandleCapturedFrame(frameData);
        }

        // Factory method for creating frame data.
        protected virtual FrameData CreateFrameData() {
            return new FrameData();
        }

        // Each mode must implement how to handle a captured frame.
        protected abstract void HandleCapturedFrame(FrameData frameData);

        public virtual void StartRecording() {
            if (isRecording) return;
            isRecording = true;
            frameCounter = 0;
            captureCoroutine = StartCoroutine(CaptureFramesCoroutine());
        }

        public virtual void StopRecording() {
            if (!isRecording) return;
            isRecording = false;
            if (captureCoroutine != null) StopCoroutine(captureCoroutine);
        }

        // --- Video Encoder Helper Methods ---

        protected bool InitializeEncoder(string outputFilename) {
            encoderHandle = VideoEncoderPlugin.CreateVideoEncoder();
            EncoderConfigC config = new EncoderConfigC {
                width = captureWidth,
                height = captureHeight,
                fps = captureFrameRate,
                bitrate = bitrate,       // in kilobits per second
                codecOption = 1,      // 1 for VP8 (use 2 for VP9)
                outputDirectory = OutputFolderPath,
                outputFilename = outputFilename
            };
            bool initSuccess = VideoEncoderPlugin.InitializeVideoEncoder(encoderHandle, ref config);
            if (!initSuccess) {
                Debug.LogError("Failed to initialize video encoder.");
            } else {
                Debug.Log($"Initialized video encoder. Output: {outputFilename}");
            }
            return initSuccess;
        }

        protected bool EncodeFrame(FrameData frameData) {
            if (encoderHandle == IntPtr.Zero) {
                Debug.LogError("Encoder is not initialized.");
                return false;
            }
            return VideoEncoderPlugin.EncodeVideoFrame(encoderHandle, frameData.RawData, frameData.FrameIndex);
        }

        protected bool FinalizeEncoder() {
            if (encoderHandle == IntPtr.Zero) {
                Debug.LogError("Encoder is not initialized.");
                return false;
            }
            return VideoEncoderPlugin.FinalizeVideoEncoder(encoderHandle);
        }

        protected void DestroyEncoder() {
            if (encoderHandle != IntPtr.Zero) {
                VideoEncoderPlugin.DestroyVideoEncoder(encoderHandle);
                encoderHandle = IntPtr.Zero;
            }
        }
    }
}
