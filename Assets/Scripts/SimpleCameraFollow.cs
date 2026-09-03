using UnityEngine;

[DisallowMultipleComponent]
public sealed class SimpleCameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.35f, -3.2f);
    [SerializeField] private float lookHeight = 0.9f;
    [SerializeField] private float smoothing = 10f;

    public void Configure(Transform followTarget, Vector3 cameraOffset, float targetHeight)
    {
        target = followTarget;
        offset = cameraOffset;
        lookHeight = targetHeight;
        SnapToTarget();
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        transform.position = target.position + offset;
        transform.rotation = Quaternion.LookRotation(target.position + Vector3.up * lookHeight - transform.position);
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        var blend = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
        var desiredPosition = target.position + offset;
        var desiredRotation = Quaternion.LookRotation(target.position + Vector3.up * lookHeight - desiredPosition);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, blend);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
    }
}
