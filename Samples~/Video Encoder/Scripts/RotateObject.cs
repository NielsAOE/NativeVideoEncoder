using UnityEngine;

namespace NielsOstman.NativeVideoRecorder.Samples {
    public class RotateObject : MonoBehaviour
    {
        [SerializeField] Vector3 rotationSpeed = new(100, 100, 100);

        void FixedUpdate()
        {
            transform.Rotate(rotationSpeed * Time.deltaTime);
        }
    }
}
