using UnityEngine;

[DisallowMultipleComponent]
public sealed class WalkAnimationPlayer : MonoBehaviour
{
    [SerializeField] private AnimationClip walkClip;
    [SerializeField] private float playbackSpeed = 1f;
    [SerializeField] private float moveSpeed = 0.65f;

    private float playbackTime;

    public void Configure(AnimationClip clip, float animationSpeed, float forwardSpeed)
    {
        walkClip = clip;
        playbackSpeed = animationSpeed;
        moveSpeed = forwardSpeed;
        playbackTime = 0f;
    }

    private void OnEnable()
    {
        playbackTime = 0f;
    }

    private void Update()
    {
        if (walkClip == null || walkClip.length <= Mathf.Epsilon)
            return;

        var deltaTime = Time.unscaledDeltaTime;
        playbackTime = Mathf.Repeat(playbackTime + deltaTime * playbackSpeed, walkClip.length);
        walkClip.SampleAnimation(gameObject, playbackTime);
        transform.Translate(Vector3.forward * (moveSpeed * deltaTime), Space.World);
    }
}
