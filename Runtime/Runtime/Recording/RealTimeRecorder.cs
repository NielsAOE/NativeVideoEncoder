using System.Collections;
using NielsOstman.NativeVideoRecorder.Core;
using UnityEngine;

namespace NielsOstman.NativeVideoRecorder.Recording {
    public class RealTimeRecorder : VideoRecorderBase {
        [Header("Real-Time Recorder Settings")]
        [SerializeField] private string outputFileNameBase = "real_time_video";
        private int videoSequenceNumber = 0;
        private string OutputFileName => $"{outputFileNameBase}_{videoSequenceNumber}.webm";

        public override void StartRecording() {
            // Initialize the encoder with the current output file name.
            if (InitializeEncoder(OutputFileName)) {
                base.StartRecording();
            }
        }

        protected override void HandleCapturedFrame(FrameData frameData) {
            // Immediately encode the frame as soon as it is captured.
            if (!EncodeFrame(frameData)) {
                Debug.LogError($"Failed to encode frame {frameData.FrameIndex}");
            }
            // Return the frame data to the pool since we're done with it.
            FrameDataPool.Return(frameData);
        }

        public override void StopRecording() {
            base.StopRecording();
            StartCoroutine(StopRecordingRoutine());
        }

        private IEnumerator StopRecordingRoutine() {
            // Wait until all pending GPU readbacks are complete.
            yield return new WaitUntil(() => pendingReadbacks == 0);

            // Finalize and destroy the encoder.
            if (!FinalizeEncoder()) {
                Debug.LogError("Failed to finalize video encoder.");
            } else {
                videoSequenceNumber++;
            }
            DestroyEncoder();
        }
    }
}