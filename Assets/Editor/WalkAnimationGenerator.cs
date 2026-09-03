using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WalkAnimationGenerator
{
    private const string ModelPath = "Assets/Models/Character.fbx";
    private const string AnimationFolder = "Assets/Animations";
    private const string ClipPath = AnimationFolder + "/Walk.anim";
    private const string ControllerPath = AnimationFolder + "/Character.controller";

    [MenuItem("Tools/Create Walk Animation")]
    public static void CreateWalkAnimation()
    {
        EnsureFolder();

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = "Walk", frameRate = 30f };
            AssetDatabase.CreateAsset(clip, ClipPath);
        }

        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
            AnimationUtility.SetEditorCurve(clip, binding, null);

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
            throw new System.InvalidOperationException("Character model was not found at " + ModelPath);

        var times = new[] { 0f, 0.125f, 0.25f, 0.375f, 0.5f, 0.625f, 0.75f, 0.875f, 1f };
        BuildBoneWalkCurves(clip, model, times);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlend = true;
        settings.keepOriginalPositionY = true;
        settings.keepOriginalPositionXZ = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        var stateMachine = controller.layers[0].stateMachine;
        var walkState = stateMachine.states.Select(item => item.state).FirstOrDefault(state => state.name == "Walk");
        if (walkState == null)
            walkState = stateMachine.AddState("Walk");

        walkState.motion = clip;
        walkState.speed = 1f;
        stateMachine.defaultState = walkState;
        EditorUtility.SetDirty(controller);

        var instance = GameObject.Find("Character_Walking");
        if (instance == null)
        {
            instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
                throw new System.InvalidOperationException("Could not instantiate the character model.");

            instance.name = "Character_Walking";
            instance.transform.position = new Vector3(2f, 0f, 0f);
            Undo.RegisterCreatedObjectUndo(instance, "Create walking character");
        }

        var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.enabled = false;

        var player = instance.GetComponent<WalkAnimationPlayer>() ?? instance.AddComponent<WalkAnimationPlayer>();
        player.Configure(clip, 1f, 0.65f);

        NormalizeCharacter(instance, 1.8f);
        instance.transform.position = new Vector3(2f, instance.transform.position.y, 0f);
        EditorUtility.SetDirty(instance);

        var camera = Camera.main;
        if (camera != null)
        {
            camera.fieldOfView = 50f;
            camera.nearClipPlane = 0.05f;
            var follow = camera.GetComponent<SimpleCameraFollow>() ?? camera.gameObject.AddComponent<SimpleCameraFollow>();
            follow.Configure(instance.transform, new Vector3(0f, 1.35f, -3.2f), 0.9f);
            EditorUtility.SetDirty(camera.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(instance.scene);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = instance;

        Debug.Log($"Created looping walk animation with {AnimationUtility.GetCurveBindings(clip).Length} bone curves.");
    }

    private static void BuildBoneWalkCurves(AnimationClip clip, GameObject model, float[] times)
    {
        var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (renderer == null)
            throw new System.InvalidOperationException("The character has no SkinnedMeshRenderer.");

        // Contact, down, passing, up, opposite contact, down, passing, up, loop.
        var leftLeg = new[] { 34f, 20f, 2f, -18f, -32f, -18f, 4f, 24f, 34f };
        var rightLeg = new[] { -32f, -18f, 4f, 24f, 34f, 20f, 2f, -18f, -32f };
        var leftKnee = new[] { 5f, 10f, 6f, 10f, 24f, 52f, 64f, 34f, 5f };
        var rightKnee = new[] { 24f, 52f, 64f, 34f, 5f, 10f, 6f, 10f, 24f };
        var leftFoot = new[] { 12f, 3f, -5f, -10f, -20f, 18f, 12f, 6f, 12f };
        var rightFoot = new[] { -20f, 18f, 12f, 6f, 12f, 3f, -5f, -10f, -20f };
        var leftArm = new[] { 32f, 22f, 5f, -19f, -33f, -22f, -5f, 19f, 32f };
        var rightArm = new[] { -33f, -22f, -5f, 19f, 32f, 22f, 5f, -19f, -33f };
        var leftElbow = new[] { 24f, 20f, 18f, 22f, 34f, 38f, 34f, 28f, 24f };
        var rightElbow = new[] { 34f, 38f, 34f, 28f, 24f, 20f, 18f, 22f, 34f };
        var spine = new[] { -4f, -3f, 0f, 3f, 4f, 3f, 0f, -3f, -4f };
        var torsoLean = new[] { -4f, -5f, -4f, -3f, -4f, -5f, -4f, -3f, -4f };
        var hipLift = new[] { -1f, -2f, 2.5f, 0f, -1f, -2f, 2.5f, 0f, -1f };
        var hipShift = new[] { 0f, 1.2f, 2f, 1.2f, 0f, -1.2f, -2f, -1.2f, 0f };

        AddBoneRotationCurves(clip, model.transform, FindBone(renderer, "thigh.L"), times, leftLeg, Vector3.right);
        AddBoneRotationCurves(clip, model.transform, FindBone(renderer, "thigh.R"), times, rightLeg, Vector3.right);
        AddBoneRotationCurves(clip, model.transform, FindBone(renderer, "shin.L"), times, leftKnee, Vector3.right);
        AddBoneRotationCurves(clip, model.transform, FindBone(renderer, "shin.R"), times, rightKnee, Vector3.right);
        AddBoneRotationCurves(clip, model.transform, FindBone(renderer, "foot.L"), times, leftFoot, Vector3.right);
        AddBoneRotationCurves(clip, model.transform, FindBone(renderer, "foot.R"), times, rightFoot, Vector3.right);
        AddArmRotationCurves(clip, model.transform, FindBone(renderer, "upper_arm.L"), times, leftArm, 72f);
        AddArmRotationCurves(clip, model.transform, FindBone(renderer, "upper_arm.R"), times, rightArm, -72f);
        AddBoneRotationCurves(clip, model.transform, FindBone(renderer, "forearm.L"), times, leftElbow, Vector3.right);
        AddBoneRotationCurves(clip, model.transform, FindBone(renderer, "forearm.R"), times, rightElbow, Vector3.right);
        AddBoneRotationCurves(clip, model.transform, FindBone(renderer, "spine.002"), times, spine, Vector3.forward);
        AddBoneRotationCurves(clip, model.transform, FindBone(renderer, "spine.001"), times, torsoLean, Vector3.right);
        AddBonePositionCurves(clip, model.transform, FindBone(renderer, "spine"), times, hipLift, hipShift);
    }

    private static Transform FindBone(SkinnedMeshRenderer renderer, string boneName)
    {
        var bone = renderer.bones.FirstOrDefault(item => item.name == boneName);
        if (bone == null)
            throw new System.InvalidOperationException("Required bone was not found: " + boneName);
        return bone;
    }

    private static void AddBoneRotationCurves(
        AnimationClip clip,
        Transform modelRoot,
        Transform bone,
        float[] times,
        float[] angles,
        Vector3 axis)
    {
        var path = AnimationUtility.CalculateTransformPath(bone, modelRoot);
        var xKeys = new Keyframe[times.Length];
        var yKeys = new Keyframe[times.Length];
        var zKeys = new Keyframe[times.Length];
        var wKeys = new Keyframe[times.Length];
        var previous = bone.localRotation;

        for (var i = 0; i < times.Length; i++)
        {
            var rotation = bone.localRotation * Quaternion.AngleAxis(angles[i], axis);
            if (Quaternion.Dot(previous, rotation) < 0f)
                rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);

            previous = rotation;
            xKeys[i] = new Keyframe(times[i], rotation.x);
            yKeys[i] = new Keyframe(times[i], rotation.y);
            zKeys[i] = new Keyframe(times[i], rotation.z);
            wKeys[i] = new Keyframe(times[i], rotation.w);
        }

        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x"), new AnimationCurve(xKeys));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.y"), new AnimationCurve(yKeys));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.z"), new AnimationCurve(zKeys));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.w"), new AnimationCurve(wKeys));
    }

    private static void AddArmRotationCurves(
        AnimationClip clip,
        Transform modelRoot,
        Transform bone,
        float[] times,
        float[] swingAngles,
        float lowerAngle)
    {
        var path = AnimationUtility.CalculateTransformPath(bone, modelRoot);
        var xKeys = new Keyframe[times.Length];
        var yKeys = new Keyframe[times.Length];
        var zKeys = new Keyframe[times.Length];
        var wKeys = new Keyframe[times.Length];
        var forwardAxis = bone.parent.InverseTransformDirection(modelRoot.forward).normalized;
        var rightAxis = bone.parent.InverseTransformDirection(modelRoot.right).normalized;
        var previous = bone.localRotation;

        for (var i = 0; i < times.Length; i++)
        {
            var lower = Quaternion.AngleAxis(lowerAngle, forwardAxis);
            var swing = Quaternion.AngleAxis(swingAngles[i], rightAxis);
            var rotation = swing * lower * bone.localRotation;
            if (Quaternion.Dot(previous, rotation) < 0f)
                rotation = new Quaternion(-rotation.x, -rotation.y, -rotation.z, -rotation.w);

            previous = rotation;
            xKeys[i] = new Keyframe(times[i], rotation.x);
            yKeys[i] = new Keyframe(times[i], rotation.y);
            zKeys[i] = new Keyframe(times[i], rotation.z);
            wKeys[i] = new Keyframe(times[i], rotation.w);
        }

        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.x"), new AnimationCurve(xKeys));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.y"), new AnimationCurve(yKeys));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.z"), new AnimationCurve(zKeys));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation.w"), new AnimationCurve(wKeys));
    }

    private static void AddBonePositionCurves(
        AnimationClip clip,
        Transform modelRoot,
        Transform bone,
        float[] times,
        float[] verticalOffsets,
        float[] lateralOffsets)
    {
        var path = AnimationUtility.CalculateTransformPath(bone, modelRoot);
        var xKeys = new Keyframe[times.Length];
        var yKeys = new Keyframe[times.Length];
        var zKeys = new Keyframe[times.Length];
        var upAxis = bone.parent.InverseTransformDirection(modelRoot.up).normalized;
        var rightAxis = bone.parent.InverseTransformDirection(modelRoot.right).normalized;

        for (var i = 0; i < times.Length; i++)
        {
            var position = bone.localPosition + upAxis * verticalOffsets[i] + rightAxis * lateralOffsets[i];
            xKeys[i] = new Keyframe(times[i], position.x);
            yKeys[i] = new Keyframe(times[i], position.y);
            zKeys[i] = new Keyframe(times[i], position.z);
        }

        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.x"), new AnimationCurve(xKeys));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y"), new AnimationCurve(yKeys));
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.z"), new AnimationCurve(zKeys));
    }

    private static void NormalizeCharacter(GameObject instance, float targetHeight)
    {
        instance.transform.localScale = Vector3.one;
        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        if (bounds.size.y <= Mathf.Epsilon)
            return;

        var scale = targetHeight / bounds.size.y;
        instance.transform.localScale = Vector3.one * scale;

        bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        instance.transform.position += Vector3.up * -bounds.min.y;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(AnimationFolder))
            AssetDatabase.CreateFolder("Assets", "Animations");
    }
}
