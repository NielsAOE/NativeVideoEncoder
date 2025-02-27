using System;
using System.Collections;
using NielsOstman.NativeVideoRecorder.Core;
using UnityEngine;

namespace NielsOstman.NativeVideoRecorder.Recording {
    public class RollingBufferRecorder : VideoRecorderBase {
        [Header("Rolling Buffer Settings")]
        [SerializeField] private float bufferDurationSeconds = 10f;
        [SerializeField] private bool resumeAfterSave = false;
        [SerializeField] private string outputFileNameBase = "encoded_video";

        private int maxFrameCount;
        private CircularBuffer<FrameData> frameBuffer;
        private int videoSequenceNumber = 0;
        private string OutputFileName => $"{outputFileNameBase}_{videoSequenceNumber}.webm";

        public override void StartRecording() {
            maxFrameCount = Mathf.CeilToInt(bufferDurationSeconds * captureFrameRate);
            frameBuffer = new CircularBuffer<FrameData>(maxFrameCount, OnFrameBufferOverwrite);
            if (InitializeEncoder(OutputFileName)) {
                base.StartRecording();
            }
        }

        // Override StopRecording to automatically trigger saving.
        public override void StopRecording() {
            base.StopRecording();
            SaveBufferAsWebM();
        }

        protected override void HandleCapturedFrame(FrameData frameData) {
            frameBuffer.Add(frameData);
        }

        public void SaveBufferAsWebM() {
            StartCoroutine(SaveBufferAsWebMCoroutine());
        }

        private IEnumerator SaveBufferAsWebMCoroutine() {
            // Wait for any pending GPU readbacks to finish.
            yield return new WaitUntil(() => pendingReadbacks == 0);

            FrameData[] frames = frameBuffer.ToArray();
            if (frames == null || frames.Length == 0) {
                Debug.LogError("No frames available for encoding.");
                yield break;
            }

            // Validate frames.
            for (int i = 0; i < frames.Length; i++) {
                if (frames[i] == null || frames[i].RawData == null) {
                    Debug.LogError($"Frame at index {i} is null or missing raw data.");
                    yield break;
                }
            }

            // Sort frames from oldest to newest.
            Array.Sort(frames, (a, b) => a.FrameIndex.CompareTo(b.FrameIndex));

            foreach (var frame in frames) {
                if (!EncodeFrame(frame)) {
                    Debug.LogError($"Failed to encode frame {frame.FrameIndex}");
                }
            }

            if (!FinalizeEncoder()) {
                Debug.LogError("Failed to finalize video encoder.");
            } else {
                videoSequenceNumber++;
            }

            DestroyEncoder();
            frameBuffer.Clear();

            if (resumeAfterSave) {
                StartRecording();
            }
        }

        private void OnFrameBufferOverwrite(FrameData frame) {
            if (frame?.RawData != null) {
                FrameDataPool.Return(frame);
            }
        }
    }
}
