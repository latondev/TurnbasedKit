#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using GameSystems.Battle;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace GameSystems.Battle.Editor
{
    internal sealed class SkillSequencePreviewController : IDisposable
    {
        private const int PreviewLayer = 30;
        private static readonly Color PreviewBackground = new Color(0.13f, 0.14f, 0.17f, 1f);
        private static readonly Vector3 ActorStartPosition = new Vector3(-1.8f, -0.8f, 0f);
        private static readonly Vector3 PrimaryTargetPosition = new Vector3(1.75f, -0.8f, 0f);
        private static readonly Vector3 PreviewCameraPosition = new Vector3(0.6f, 0.15f, -10f);
        private static readonly Quaternion TargetPreviewRotation = Quaternion.Euler(0f, 180f, 0f);
        private const float TargetPreviewScaleMultiplier = 1.35f;
        private const double MinTickInterval = 1d / 60d;
        private const double SequenceRestartDelay = 0.7d;
        private const double EventPopupDuration = 0.85d;
        private const float PreviewCameraSize = 3.0f;

        private readonly List<GameObject> spawnedVfxObjects = new List<GameObject>();
        private readonly List<EventPopup> eventPopups = new List<EventPopup>();
        private static GUIStyle eventPopupStyle;

        private PreviewRenderUtility previewUtility;
        private GameObject prefabSource;
        private GameObject previewGameObject;
        private GameObject targetPrefabSource;
        private GameObject previewTargetGameObject;
        private SkeletonAnimation skeletonAnimation;
        private SkeletonAnimation targetSkeletonAnimation;
        private AnimationHandle animationHandle;
        private SkillViewSequence sequence;
        private Action repaintCallback;

        private bool isPlaying = true;
        private bool isPaused;
        private bool sequenceFinished;
        private int currentStepIndex = -1;
        private bool currentStepHasDuration;
        private double stepStartedAt;
        private double restartAt;
        private double pausedAt;
        private double lastRenderAt;
        private double lastTickAt;
        private float speed = 1f;
        private Vector3 stepMoveStartPosition;
        private Vector3 stepMoveEndPosition;
        private string statusText = "Idle";
        private bool previewHostReleased;
        private bool showEventPopups;
        private bool loopPlayback;
        private bool targetPreviewRegistered;
        private bool terminalIdleLoopActive;

        public SkillViewSequence Sequence
        {
            get { return sequence; }
        }

        public bool HasSequence
        {
            get { return sequence != null; }
        }

        public bool HasPreviewObject
        {
            get { return previewGameObject != null; }
        }

        public bool HasTargetPreviewObject
        {
            get { return previewTargetGameObject != null; }
        }

        public bool IsPlaying
        {
            get { return isPlaying && !isPaused && !sequenceFinished; }
        }

        public bool IsIdleLoopActive
        {
            get { return terminalIdleLoopActive && !isPaused; }
        }

        public bool HasPendingRestart
        {
            get { return (sequenceFinished || terminalIdleLoopActive) && restartAt > 0d; }
        }

        public int CurrentStepIndex
        {
            get { return currentStepIndex; }
        }

        public string StatusText
        {
            get { return statusText; }
        }

        public bool ShowEventPopups
        {
            get { return showEventPopups; }
            set
            {
                if (showEventPopups == value)
                {
                    return;
                }

                showEventPopups = value;
                if (showEventPopups)
                {
                    BindAnimationEvents();
                }
                else
                {
                    UnbindAnimationEvents();
                }
            }
        }

        public bool LoopPlayback
        {
            get { return loopPlayback; }
            set
            {
                if (loopPlayback == value)
                {
                    return;
                }

                loopPlayback = value;
                if (!loopPlayback)
                {
                    restartAt = -1d;
                }
                else if (sequenceFinished && sequence != null && previewGameObject != null)
                {
                    restartAt = EditorApplication.timeSinceStartup + SequenceRestartDelay;
                }
            }
        }

        public float Speed
        {
            get { return speed; }
            set
            {
                float nextSpeed = Mathf.Max(0.1f, value);
                if (Mathf.Abs(speed - nextSpeed) < 0.0001f)
                {
                    return;
                }

                speed = nextSpeed;
                if (animationHandle != null)
                {
                    animationHandle.SetSpeed(speed);
                }
                RequestRepaint();
            }
        }

        public void SetRepaintCallback(Action callback)
        {
            repaintCallback = callback;
        }

        public void BindPrefab(GameObject sourcePrefab)
        {
            if (prefabSource == sourcePrefab)
            {
                if (sourcePrefab != null && previewGameObject != null)
                {
                    return;
                }

                if (sourcePrefab == null && previewGameObject == null)
                {
                    return;
                }
            }

            prefabSource = sourcePrefab;
            RebuildPreviewObject();
            ResetPlayback(true);
        }

        public void SetTargetPrefab(GameObject sourcePrefab)
        {
            if (targetPrefabSource == sourcePrefab)
            {
                if (sourcePrefab != null && previewTargetGameObject != null)
                {
                    RegisterTargetPreviewObject();
                    return;
                }

                if (sourcePrefab == null && previewTargetGameObject == null)
                {
                    return;
                }
            }

            targetPrefabSource = sourcePrefab;
            RebuildTargetPreviewObject();
        }

        public void SetSequence(SkillViewSequence nextSequence)
        {
            if (sequence == nextSequence)
            {
                return;
            }

            sequence = nextSequence;
            ResetPlayback(!previewHostReleased);
        }

        public void TogglePlayback()
        {
            if (
                sequenceFinished
                || currentStepIndex < 0
                || sequence == null
                || previewGameObject == null
            )
            {
                Restart();
                return;
            }

            if (isPaused)
            {
                Resume();
                return;
            }

            Pause();
        }

        public void Restart()
        {
            if (previewGameObject == null && prefabSource != null)
            {
                RebuildPreviewObject();
            }

            ResetPlayback(true);
        }

        public void Pause()
        {
            if (!isPlaying || isPaused)
            {
                return;
            }

            isPaused = true;
            pausedAt = EditorApplication.timeSinceStartup;
            statusText = "Paused";
            RequestRepaint();
        }

        public void Resume()
        {
            if (!isPaused || sequence == null || previewGameObject == null)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            double pausedDuration = now - pausedAt;
            stepStartedAt += pausedDuration;
            if (restartAt > 0d)
            {
                restartAt += pausedDuration;
            }
            lastRenderAt = now;
            isPaused = false;
            isPlaying = true;
            sequenceFinished = false;
            statusText = BuildStatusText();
            RequestRepaint();
        }

        public void Tick()
        {
            if (sequence == null || sequence.Steps == null || previewGameObject == null || isPaused)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;

            if (terminalIdleLoopActive)
            {
                if (restartAt > 0d && now >= restartAt)
                {
                    restartAt = -1d;
                    ResetPlayback(true);
                    return;
                }

                if (lastTickAt > 0d && now - lastTickAt < MinTickInterval)
                {
                    return;
                }

                lastTickAt = now;
                RequestRepaint();
                return;
            }

            if (sequenceFinished)
            {
                if (restartAt > 0d && now >= restartAt)
                {
                    restartAt = -1d;
                    ResetPlayback(true);
                }

                return;
            }

            if (!isPlaying)
            {
                return;
            }

            if (lastTickAt > 0d && now - lastTickAt < MinTickInterval)
            {
                return;
            }

            lastTickAt = now;

            if (currentStepIndex < 0)
            {
                if (AdvanceToNextStep(now))
                {
                    RequestRepaint();
                }

                if (sequenceFinished)
                {
                    return;
                }
            }

            bool changed = false;
            int safetyCounter = 0;

            while (safetyCounter++ < 256)
            {
                if (
                    sequence == null
                    || currentStepIndex < 0
                    || currentStepIndex >= sequence.Steps.Count
                )
                {
                    FinishPlayback();
                    changed = true;
                    break;
                }

                var currentStep = sequence.Steps[currentStepIndex];
                if (currentStep == null)
                {
                    changed |= AdvanceToNextStep(now);
                    if (sequenceFinished)
                    {
                        changed = true;
                        break;
                    }
                    continue;
                }

                if (UpdateCurrentStep(currentStep, now))
                {
                    changed = true;
                    if (sequenceFinished)
                    {
                        break;
                    }
                    break;
                }

                if (IsImmediateStep(currentStep))
                {
                    changed |= AdvanceToNextStep(now);
                    if (sequenceFinished)
                    {
                        changed = true;
                        break;
                    }
                    continue;
                }

                break;
            }

            if (safetyCounter > 256)
            {
                Debug.LogWarning(
                    "[SkillSequencePreview] Preview tick safety limit reached. Stopping playback to avoid editor stall."
                );
                FinishPlayback();
                changed = true;
            }

            if (changed)
            {
                RequestRepaint();
            }
            else if (showEventPopups && eventPopups.Count > 0)
            {
                RequestRepaint();
            }
        }

        public void DrawPreview(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
            {
                EditorGUI.DrawRect(rect, PreviewBackground);
                return;
            }

            if (previewUtility == null || previewGameObject == null)
            {
                EditorGUI.DrawRect(rect, PreviewBackground);
                GUI.Label(
                    rect,
                    "Select a prefab with SkeletonAnimation",
                    EditorStyles.centeredGreyMiniLabel
                );
                return;
            }

            previewUtility.BeginPreview(rect, GUIStyle.none);
            RenderPreviewScene();
            Texture previewTexture = previewUtility.EndPreview();

            if (previewTexture != null)
            {
                GUI.DrawTexture(rect, previewTexture, ScaleMode.StretchToFill, false);
            }

            DrawEventPopups(rect);
        }

        public void Dispose()
        {
            UnbindAnimationEvents();
            DestroySpawnedVfx();
            ClearEventPopups();

            if (previewGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(previewGameObject);
                previewGameObject = null;
            }

            if (previewTargetGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(previewTargetGameObject);
                previewTargetGameObject = null;
            }

            if (previewUtility != null)
            {
                previewUtility.Cleanup();
                previewUtility = null;
            }

            skeletonAnimation = null;
            targetSkeletonAnimation = null;
            animationHandle = null;
            prefabSource = null;
            targetPrefabSource = null;
            previewHostReleased = false;
            targetPreviewRegistered = false;
        }

        private void RebuildPreviewObject()
        {
            DestroySpawnedVfx();
            UnbindAnimationEvents();
            ClearEventPopups();

            if (previewGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(previewGameObject);
                previewGameObject = null;
            }

            skeletonAnimation = null;
            animationHandle = null;

            if (prefabSource == null)
            {
                statusText = "No prefab selected";
                RequestRepaint();
                return;
            }

            EnsurePreviewUtility();

            try
            {
                SkeletonAnimation sourceSkeleton =
                    prefabSource != null
                        ? prefabSource.GetComponentInChildren<SkeletonAnimation>(true)
                        : null;

                if (sourceSkeleton != null)
                {
                    previewGameObject = new GameObject($"{prefabSource.name}_PreviewHost");
                    previewGameObject.AddComponent<MeshFilter>();
                    previewGameObject.AddComponent<MeshRenderer>();

                    skeletonAnimation = previewGameObject.AddComponent<SkeletonAnimation>();
                    EditorUtility.CopySerialized(sourceSkeleton, skeletonAnimation);
                    skeletonAnimation.Initialize(true);

                    animationHandle = previewGameObject.AddComponent<AnimationHandle>();
                    animationHandle.skeletonAnimation = skeletonAnimation;
                    animationHandle.Initialize();
                    animationHandle.SetSpeed(speed);
                }
                else
                {
                    previewGameObject = PrefabUtility.InstantiatePrefab(prefabSource) as GameObject;
                    if (previewGameObject == null)
                    {
                        previewGameObject = UnityEngine.Object.Instantiate(prefabSource);
                    }

                    StripRuntimeBehaviours(previewGameObject);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[SkillSequencePreview] Failed to instantiate prefab preview: {ex.Message}"
                );
                statusText = "Failed to create preview object";
                RequestRepaint();
                return;
            }

            if (previewGameObject == null)
            {
                statusText = "Failed to create preview object";
                RequestRepaint();
                return;
            }

            previewGameObject.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursively(previewGameObject, PreviewLayer);
            previewGameObject.transform.position = ActorStartPosition;

            if (skeletonAnimation == null)
            {
                skeletonAnimation = previewGameObject.GetComponentInChildren<SkeletonAnimation>(
                    true
                );
            }

            if (animationHandle == null)
            {
                animationHandle = previewGameObject.GetComponentInChildren<AnimationHandle>(true);
            }

            if (animationHandle == null && skeletonAnimation != null)
            {
                animationHandle = previewGameObject.AddComponent<AnimationHandle>();
            }

            if (animationHandle != null)
            {
                animationHandle.skeletonAnimation = skeletonAnimation;
                animationHandle.Initialize();
                animationHandle.SetSpeed(speed);
                BindAnimationEvents();
            }

            if (previewUtility != null)
            {
                previewUtility.AddSingleGO(previewGameObject);
                ConfigurePreviewCamera();
            }

            RegisterTargetPreviewObject();

            previewHostReleased = false;
            statusText =
                skeletonAnimation != null ? BuildStatusText() : "Prefab has no SkeletonAnimation";
            RequestRepaint();
        }

        private void RebuildTargetPreviewObject()
        {
            if (previewTargetGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(previewTargetGameObject);
                previewTargetGameObject = null;
            }

            targetSkeletonAnimation = null;
            targetPreviewRegistered = false;

            if (targetPrefabSource == null)
            {
                RequestRepaint();
                return;
            }

            EnsurePreviewUtility();

            try
            {
                previewTargetGameObject =
                    PrefabUtility.InstantiatePrefab(targetPrefabSource) as GameObject;
                if (previewTargetGameObject == null)
                {
                    previewTargetGameObject = UnityEngine.Object.Instantiate(targetPrefabSource);
                }

                StripRuntimeBehaviours(previewTargetGameObject);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"[SkillSequencePreview] Failed to instantiate target preview: {ex.Message}"
                );
                RequestRepaint();
                return;
            }

            if (previewTargetGameObject == null)
            {
                RequestRepaint();
                return;
            }

            previewTargetGameObject.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursively(previewTargetGameObject, PreviewLayer);
            previewTargetGameObject.transform.position = PrimaryTargetPosition;
            previewTargetGameObject.transform.rotation = TargetPreviewRotation;
            previewTargetGameObject.transform.localScale *= TargetPreviewScaleMultiplier;

            targetSkeletonAnimation =
                previewTargetGameObject.GetComponentInChildren<SkeletonAnimation>(true);
            if (targetSkeletonAnimation != null)
            {
                targetSkeletonAnimation.Initialize(true);
            }

            RegisterTargetPreviewObject();
            RequestRepaint();
        }

        private void StripRuntimeBehaviours(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour is SkeletonAnimation || behaviour is AnimationHandle)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(behaviour);
            }
        }

        private void EnsurePreviewUtility()
        {
            if (previewUtility != null)
            {
                return;
            }

            previewUtility = new PreviewRenderUtility(true);
            ConfigurePreviewCamera();
        }

        private void RegisterTargetPreviewObject()
        {
            if (
                previewUtility == null
                || previewTargetGameObject == null
                || targetPreviewRegistered
            )
            {
                return;
            }

            previewUtility.AddSingleGO(previewTargetGameObject);
            targetPreviewRegistered = true;
            ConfigurePreviewCamera();
        }

        private void ConfigurePreviewCamera()
        {
            if (previewUtility == null)
            {
                return;
            }

            Camera camera = previewUtility.camera;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = PreviewBackground;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 1000f;
            camera.transform.position = PreviewCameraPosition;
            camera.orthographicSize = PreviewCameraSize;

            if (previewUtility.lights != null && previewUtility.lights.Length > 0)
            {
                previewUtility.lights[0].transform.rotation = Quaternion.Euler(25f, 25f, 0f);
                previewUtility.lights[0].intensity = 1.2f;
            }

            if (previewUtility.lights != null && previewUtility.lights.Length > 1)
            {
                previewUtility.lights[1].transform.rotation = Quaternion.Euler(340f, 218f, 177f);
                previewUtility.lights[1].intensity = 1.0f;
            }
        }

        private void ResetPlayback(bool autoPlay)
        {
            isPlaying = autoPlay && sequence != null && previewGameObject != null;
            isPaused = false;
            sequenceFinished = false;
            terminalIdleLoopActive = false;
            currentStepIndex = -1;
            currentStepHasDuration = false;
            stepStartedAt = EditorApplication.timeSinceStartup;
            restartAt = -1d;
            pausedAt = stepStartedAt;
            lastRenderAt = stepStartedAt;
            lastTickAt = stepStartedAt;
            stepMoveStartPosition = ActorStartPosition;
            stepMoveEndPosition = ActorStartPosition;

            if (previewGameObject != null)
            {
                previewGameObject.transform.position = ActorStartPosition;
            }

            if (autoPlay && animationHandle != null)
            {
                animationHandle.ResetAnimationState();
            }

            ClearSpawnedVfx();
            ClearEventPopups();
            statusText = sequence != null ? BuildStatusText() : "Idle";

            if (autoPlay && sequence != null && previewGameObject != null)
            {
                AdvanceToNextStep(stepStartedAt);
            }

            RequestRepaint();
        }

        private bool AdvanceToNextStep(double now)
        {
            if (sequence == null || sequence.Steps == null)
            {
                FinishPlayback();
                return false;
            }

            currentStepIndex++;
            currentStepHasDuration = false;

            if (currentStepIndex >= sequence.Steps.Count)
            {
                FinishPlayback();
                return false;
            }

            var currentStep = sequence.Steps[currentStepIndex];
            if (currentStep == null)
            {
                statusText = BuildStatusText();
                return true;
            }

            stepStartedAt = now;
            statusText = BuildStatusText(currentStep);

            switch (currentStep.StepType)
            {
                case SkillViewStepType.MoveToTarget:
                    PlayStepAnimation(currentStep, 1);
                    PrepareMoveToTarget(currentStep);
                    currentStepHasDuration = currentStep.Duration > 0f;
                    if (!currentStepHasDuration)
                    {
                        if (previewGameObject != null)
                        {
                            previewGameObject.transform.position = stepMoveEndPosition;
                        }
                    }
                    break;

                case SkillViewStepType.MoveBack:
                    PlayStepAnimation(currentStep, 2);
                    PrepareMoveBack(currentStep);
                    currentStepHasDuration = currentStep.Duration > 0f;
                    if (!currentStepHasDuration)
                    {
                        if (previewGameObject != null)
                        {
                            previewGameObject.transform.position = ActorStartPosition;
                        }
                    }
                    break;

                case SkillViewStepType.PlayAnimation:
                    PlayStepAnimation(currentStep, 1);
                    currentStepHasDuration =
                        currentStep.WaitForAnimationEnd && currentStep.Duration > 0f;
                    break;

                case SkillViewStepType.Wait:
                    currentStepHasDuration = currentStep.Duration > 0f;
                    break;

                case SkillViewStepType.SpawnVfx:
                    SpawnVfx(currentStep);
                    break;

                case SkillViewStepType.TriggerHit:
                    statusText = BuildStatusText(currentStep);
                    break;

                case SkillViewStepType.ResetSortingOrder:
                    if (animationHandle != null)
                    {
                        animationHandle.ResetSortingOrder();
                    }
                    break;

                case SkillViewStepType.SetSortingOrder:
                    if (animationHandle != null)
                    {
                        animationHandle.SetSortingOrder(currentStep.SortingOrder, "Unit");
                    }
                    break;

                case SkillViewStepType.SetFlipX:
                    if (animationHandle != null)
                    {
                        animationHandle.SetFlipX(currentStep.FlipX);
                    }
                    break;

                case SkillViewStepType.SetIdleAnimation:
                    currentStepHasDuration = currentStep.Duration > 0f;
                    PlayIdleAnimation(currentStep);
                    break;
            }

            if (!currentStepHasDuration && IsImmediateStep(currentStep))
            {
                return true;
            }

            RequestRepaint();
            return true;
        }

        private bool UpdateCurrentStep(SkillViewStep step, double now)
        {
            if (step == null)
            {
                return false;
            }

            if (step.StepType == SkillViewStepType.MoveToTarget)
            {
                float progress = GetProgress(now, step.Duration);
                if (previewGameObject != null)
                {
                    previewGameObject.transform.position = Vector3.Lerp(
                        stepMoveStartPosition,
                        stepMoveEndPosition,
                        progress
                    );
                }

                if (progress >= 1f)
                {
                    statusText = BuildStatusText(step);
                    return AdvanceToNextStep(now);
                }

                statusText = BuildStatusText(step);
                return true;
            }

            if (step.StepType == SkillViewStepType.MoveBack)
            {
                float progress = GetProgress(now, step.Duration);
                if (previewGameObject != null)
                {
                    previewGameObject.transform.position = Vector3.Lerp(
                        stepMoveStartPosition,
                        ActorStartPosition,
                        progress
                    );
                }

                if (progress >= 1f)
                {
                    statusText = BuildStatusText(step);
                    return AdvanceToNextStep(now);
                }

                statusText = BuildStatusText(step);
                return true;
            }

            if (step.StepType == SkillViewStepType.PlayAnimation && currentStepHasDuration)
            {
                if (GetProgress(now, step.Duration) >= 1f)
                {
                    return AdvanceToNextStep(now);
                }

                return true;
            }

            if (step.StepType == SkillViewStepType.SetIdleAnimation && currentStepHasDuration)
            {
                if (GetProgress(now, step.Duration) >= 1f)
                {
                    return AdvanceToNextStep(now);
                }

                return true;
            }

            if (step.StepType == SkillViewStepType.Wait)
            {
                if (GetProgress(now, step.Duration) >= 1f)
                {
                    return AdvanceToNextStep(now);
                }

                return true;
            }

            return false;
        }

        private float GetProgress(double now, float duration)
        {
            if (duration <= 0f)
            {
                return 1f;
            }

            float elapsed = (float)((now - stepStartedAt) * speed);
            return Mathf.Clamp01(elapsed / duration);
        }

        private bool IsImmediateStep(SkillViewStep step)
        {
            if (step == null)
            {
                return true;
            }

            switch (step.StepType)
            {
                case SkillViewStepType.MoveToTarget:
                case SkillViewStepType.MoveBack:
                    return step.Duration <= 0f;
                case SkillViewStepType.PlayAnimation:
                    return !step.WaitForAnimationEnd || step.Duration <= 0f;
                case SkillViewStepType.Wait:
                    return step.Duration <= 0f;
                case SkillViewStepType.SpawnVfx:
                case SkillViewStepType.TriggerHit:
                case SkillViewStepType.ResetSortingOrder:
                case SkillViewStepType.SetSortingOrder:
                case SkillViewStepType.SetFlipX:
                    return true;
                case SkillViewStepType.SetIdleAnimation:
                    return step.Duration <= 0f;
                default:
                    return true;
            }
        }

        private void PrepareMoveToTarget(SkillViewStep step)
        {
            if (previewGameObject == null)
            {
                return;
            }

            stepMoveStartPosition = previewGameObject.transform.position;
            stepMoveEndPosition = ResolveDestination(step);
        }

        private void PrepareMoveBack(SkillViewStep step)
        {
            if (previewGameObject == null)
            {
                return;
            }

            stepMoveStartPosition = previewGameObject.transform.position;
            stepMoveEndPosition = ActorStartPosition;
        }

        private void PlayStepAnimation(SkillViewStep step, int layer)
        {
            if (animationHandle == null)
            {
                return;
            }

            string primary = ResolveAnimationName(step);
            string fallback = ResolveFallbackAnimationName(step);
            animationHandle.TryPlayAnimation(primary, fallback, 0.1f, layer, step.Loop);
        }

        private void PlayIdleAnimation(SkillViewStep step)
        {
            if (animationHandle == null)
            {
                return;
            }

            animationHandle.ClearTrack(1);
            animationHandle.ClearTrack(2);

            string idleName = !string.IsNullOrWhiteSpace(step != null ? step.AnimationName : null)
                ? step.AnimationName
                : (
                    !string.IsNullOrWhiteSpace(sequence != null ? sequence.IdleAnimationName : null)
                        ? sequence.IdleAnimationName
                        : "idle"
                );

            string fallbackName = !string.IsNullOrWhiteSpace(
                step != null ? step.FallbackAnimationName : null
            )
                ? step.FallbackAnimationName
                : idleName;

            animationHandle.TryPlayAnimation(
                idleName,
                fallbackName,
                step != null ? step.Duration : 0.1f,
                0,
                true
            );
        }

        private string ResolveAnimationName(SkillViewStep step)
        {
            if (step == null)
            {
                return "skill";
            }

            if (!string.IsNullOrWhiteSpace(step.AnimationName))
            {
                return step.AnimationName;
            }

            if (!string.IsNullOrWhiteSpace(sequence != null ? sequence.AnimationName : null))
            {
                return sequence.AnimationName;
            }

            return "skill";
        }

        private string ResolveFallbackAnimationName(SkillViewStep step)
        {
            if (step == null)
            {
                return "skill";
            }

            if (!string.IsNullOrWhiteSpace(step.FallbackAnimationName))
            {
                return step.FallbackAnimationName;
            }

            if (
                !string.IsNullOrWhiteSpace(sequence != null ? sequence.FallbackAnimationName : null)
            )
            {
                return sequence.FallbackAnimationName;
            }

            return ResolveAnimationName(step);
        }

        private Vector3 ResolveDestination(SkillViewStep step)
        {
            if (step == null)
            {
                return ActorStartPosition;
            }

            switch (step.TargetType)
            {
                case SkillViewTargetType.Actor:
                    return ActorStartPosition + step.Offset;
                case SkillViewTargetType.AllTargets:
                    return PrimaryTargetPosition + step.Offset;
                case SkillViewTargetType.WorldPosition:
                    return step.WorldPosition + step.Offset;
                default:
                    return ResolvePrimaryTargetDestination(step);
            }
        }

        private Vector3 ResolvePrimaryTargetDestination(SkillViewStep step)
        {
            Vector3 direction = (PrimaryTargetPosition - ActorStartPosition);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.right;
            }
            else
            {
                direction.Normalize();
            }

            if (step.MoveMode == SkillViewMoveMode.OffsetFromTarget)
            {
                return PrimaryTargetPosition + step.Offset;
            }

            float signedDistance =
                step.MoveMode == SkillViewMoveMode.ThroughTarget
                    ? -Mathf.Abs(step.MoveDistance)
                    : Mathf.Abs(step.MoveDistance);

            return PrimaryTargetPosition - (direction * signedDistance) + step.Offset;
        }

        private void SpawnVfx(SkillViewStep step)
        {
            if (step == null || step.VfxPrefab == null || previewUtility == null)
            {
                return;
            }

            Vector3 spawnPosition = ResolveDestination(step);
            ParticleSystem instance = UnityEngine.Object.Instantiate(
                step.VfxPrefab,
                spawnPosition,
                Quaternion.identity
            );
            if (instance == null)
            {
                return;
            }

            GameObject instanceGO = instance.gameObject;
            instanceGO.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursively(instanceGO, PreviewLayer);
            previewUtility.AddSingleGO(instanceGO);
            instance.Play(true);
            spawnedVfxObjects.Add(instanceGO);
        }

        private void RenderPreviewScene()
        {
            if (previewUtility == null)
            {
                return;
            }

            Camera camera = previewUtility.camera;
            camera.transform.position = PreviewCameraPosition;
            camera.orthographicSize = PreviewCameraSize;

            double now = EditorApplication.timeSinceStartup;
            if (lastRenderAt <= 0d)
            {
                lastRenderAt = now;
            }

            float deltaTime = (float)(now - lastRenderAt);
            if (skeletonAnimation != null)
            {
                if (isPlaying && !isPaused && !sequenceFinished)
                {
                    skeletonAnimation.Update(deltaTime);
                }
                else
                {
                    skeletonAnimation.Update(0f);
                }

                skeletonAnimation.LateUpdate();
            }

            if (targetSkeletonAnimation != null)
            {
                if (isPaused)
                {
                    targetSkeletonAnimation.Update(0f);
                }
                else
                {
                    targetSkeletonAnimation.Update(deltaTime);
                }

                targetSkeletonAnimation.LateUpdate();
            }

            lastRenderAt = now;
            camera.Render();
        }

        private void FinishPlayback()
        {
            if (HasTerminalIdleLoopStep(sequence))
            {
                isPlaying = true;
                isPaused = false;
                sequenceFinished = false;
                terminalIdleLoopActive = true;
                restartAt =
                    loopPlayback
                    && sequence != null
                    && sequence.Steps != null
                    && sequence.Steps.Count > 0
                        ? EditorApplication.timeSinceStartup + SequenceRestartDelay
                        : -1d;
                currentStepIndex =
                    sequence != null && sequence.Steps != null && sequence.Steps.Count > 0
                        ? sequence.Steps.Count - 1
                        : -1;
                statusText = "Idle Loop";
                RequestRepaint();
                return;
            }

            StopPlayback("Finished", false);

            if (
                loopPlayback
                && sequence != null
                && sequence.Steps != null
                && sequence.Steps.Count > 0
            )
            {
                restartAt = EditorApplication.timeSinceStartup + SequenceRestartDelay;
            }
        }

        public void Stop()
        {
            StopPlayback("Stopped", false);
        }

        private void BindAnimationEvents()
        {
            if (animationHandle == null || !showEventPopups)
            {
                return;
            }

            animationHandle.OnEventAnimation -= HandlePreviewEvent;
            animationHandle.OnEventAnimation += HandlePreviewEvent;
        }

        private void UnbindAnimationEvents()
        {
            if (animationHandle == null)
            {
                return;
            }

            animationHandle.OnEventAnimation -= HandlePreviewEvent;
        }

        private void HandlePreviewEvent(string animationName, string eventName)
        {
            if (!showEventPopups || string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            Vector3 popupWorldPosition =
                previewGameObject != null
                    ? previewGameObject.transform.position + new Vector3(1.2f, 0.6f, 0f)
                    : ActorStartPosition + new Vector3(1.2f, 0.6f, 0f);

            eventPopups.Add(
                new EventPopup(eventName, popupWorldPosition, EditorApplication.timeSinceStartup)
            );
            RequestRepaint();
        }

        private void ClearSpawnedVfx()
        {
            for (int i = 0; i < spawnedVfxObjects.Count; i++)
            {
                if (spawnedVfxObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(spawnedVfxObjects[i]);
                }
            }

            spawnedVfxObjects.Clear();
        }

        private void ClearEventPopups()
        {
            eventPopups.Clear();
        }

        private void StopPlayback(string nextStatus, bool releasePreviewHost)
        {
            isPlaying = false;
            isPaused = false;
            sequenceFinished = true;
            terminalIdleLoopActive = false;
            restartAt = -1d;
            currentStepIndex =
                sequence != null && sequence.Steps != null && sequence.Steps.Count > 0
                    ? sequence.Steps.Count - 1
                    : -1;

            ClearSpawnedVfx();
            if (releasePreviewHost)
            {
                ReleasePreviewHost();
            }

            statusText = nextStatus;
            RequestRepaint();
        }

        private void ReleasePreviewHost()
        {
            UnbindAnimationEvents();
            if (previewGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(previewGameObject);
                previewGameObject = null;
            }

            if (previewUtility != null)
            {
                previewUtility.Cleanup();
                previewUtility = null;
            }

            targetPreviewRegistered = false;

            skeletonAnimation = null;
            targetSkeletonAnimation = null;
            animationHandle = null;
            lastRenderAt = 0d;
            lastTickAt = 0d;
            restartAt = -1d;
            previewHostReleased = true;
            terminalIdleLoopActive = false;
            ClearEventPopups();
        }

        private bool HasTerminalIdleLoopStep(SkillViewSequence nextSequence)
        {
            if (nextSequence == null || nextSequence.Steps == null)
            {
                return false;
            }

            for (int i = nextSequence.Steps.Count - 1; i >= 0; i--)
            {
                SkillViewStep step = nextSequence.Steps[i];
                if (step == null)
                {
                    continue;
                }

                return step.StepType == SkillViewStepType.SetIdleAnimation && step.Loop;
            }

            return false;
        }

        private void DestroySpawnedVfx()
        {
            ClearSpawnedVfx();
        }

        private void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.layer = layer;
            for (int i = 0; i < root.transform.childCount; i++)
            {
                SetLayerRecursively(root.transform.GetChild(i).gameObject, layer);
            }
        }

        private string BuildStatusText()
        {
            if (sequence == null)
            {
                return "No sequence selected";
            }

            if (sequence.Steps == null)
            {
                return "No steps";
            }

            if (previewGameObject == null)
            {
                return "No prefab selected";
            }

            if (sequenceFinished)
            {
                return "Finished";
            }

            if (terminalIdleLoopActive)
            {
                return "Idle Loop";
            }

            if (isPaused)
            {
                return "Paused";
            }

            if (!isPlaying)
            {
                return "Stopped";
            }

            if (
                currentStepIndex < 0
                || sequence.Steps == null
                || currentStepIndex >= sequence.Steps.Count
            )
            {
                return "Ready";
            }

            return BuildStatusText(sequence.Steps[currentStepIndex]);
        }

        private string BuildStatusText(SkillViewStep step)
        {
            string sequenceName =
                sequence != null
                    ? (
                        !string.IsNullOrWhiteSpace(sequence.SequenceId)
                            ? sequence.SequenceId
                            : sequence.name
                    )
                    : "No sequence";

            if (step == null)
            {
                return sequenceName + " - step pending";
            }

            return string.Format(
                "{0} - Step {1}: {2}",
                sequenceName,
                currentStepIndex + 1,
                step.StepType
            );
        }

        private void DrawEventPopups(Rect previewRect)
        {
            if (!showEventPopups || eventPopups.Count == 0)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            for (int i = eventPopups.Count - 1; i >= 0; i--)
            {
                EventPopup popup = eventPopups[i];
                double elapsed = now - popup.StartedAt;
                if (elapsed >= EventPopupDuration)
                {
                    eventPopups.RemoveAt(i);
                    continue;
                }

                float t = Mathf.Clamp01((float)(elapsed / EventPopupDuration));
                float alpha = 1f - t;
                float rise = Mathf.Lerp(0f, 28f, t);
                GUIStyle popupStyle = GetEventPopupStyle();
                Vector2 textSize = popupStyle.CalcSize(new GUIContent(popup.Text));
                Vector2 screenPoint = ProjectWorldToPreviewPoint(previewRect, popup.WorldPosition);
                Vector2 center = new Vector2(screenPoint.x, screenPoint.y - rise);
                Rect labelRect = new Rect(
                    center.x - textSize.x * 0.5f,
                    center.y - textSize.y * 0.5f,
                    textSize.x + 16f,
                    textSize.y + 6f
                );

                Color previousColor = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, alpha * 0.65f);
                GUI.Label(
                    new Rect(labelRect.x + 1f, labelRect.y + 1f, labelRect.width, labelRect.height),
                    popup.Text,
                    popupStyle
                );
                GUI.color = new Color(1f, 0.78f, 0.22f, alpha);
                GUI.Label(labelRect, popup.Text, popupStyle);
                GUI.color = previousColor;
            }
        }

        private static GUIStyle GetEventPopupStyle()
        {
            if (eventPopupStyle != null)
            {
                return eventPopupStyle;
            }

            eventPopupStyle = new GUIStyle(GUI.skin != null ? GUI.skin.label : EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };

            return eventPopupStyle;
        }

        private Vector2 ProjectWorldToPreviewPoint(Rect previewRect, Vector3 worldPosition)
        {
            if (previewUtility == null || previewUtility.camera == null)
            {
                return previewRect.center;
            }

            Vector3 screenPoint = previewUtility.camera.WorldToScreenPoint(worldPosition);
            return new Vector2(
                previewRect.x + screenPoint.x,
                previewRect.y + (previewRect.height - screenPoint.y)
            );
        }

        private void RequestRepaint()
        {
            if (repaintCallback != null)
            {
                repaintCallback.Invoke();
            }
        }

        private sealed class EventPopup
        {
            public string Text { get; }
            public Vector3 WorldPosition { get; }
            public double StartedAt { get; }

            public EventPopup(string text, Vector3 worldPosition, double startedAt)
            {
                Text = text;
                WorldPosition = worldPosition;
                StartedAt = startedAt;
            }
        }
    }
}

#endif
