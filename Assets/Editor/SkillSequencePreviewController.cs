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
        private static readonly Vector3 ActorStartPosition = new Vector3(-0.5f, 0f, 0f);
        private static readonly Vector3 PrimaryTargetPosition = new Vector3(1.75f, 0f, 0f);
        private static readonly Vector3 PreviewCameraPosition = new Vector3(0.6f, 0.15f, -10f);
        private const double MinTickInterval = 1d / 30d;
        private const float PreviewCameraSize = 3.0f;

        private readonly List<GameObject> spawnedVfxObjects = new List<GameObject>();

        private PreviewRenderUtility previewUtility;
        private GameObject prefabSource;
        private GameObject previewGameObject;
        private SkeletonAnimation skeletonAnimation;
        private AnimationHandle animationHandle;
        private SkillViewSequence sequence;
        private Action repaintCallback;

        private bool isPlaying = true;
        private bool isPaused;
        private bool sequenceFinished;
        private int currentStepIndex = -1;
        private bool currentStepHasDuration;
        private double stepStartedAt;
        private double pausedAt;
        private double lastRenderAt;
        private double lastTickAt;
        private float speed = 1f;
        private Vector3 stepMoveStartPosition;
        private Vector3 stepMoveEndPosition;
        private string statusText = "Idle";
        private bool previewHostReleased;

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

        public bool IsPlaying
        {
            get { return isPlaying && !isPaused && !sequenceFinished; }
        }

        public int CurrentStepIndex
        {
            get { return currentStepIndex; }
        }

        public string StatusText
        {
            get { return statusText; }
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

                if (sourcePrefab != null && previewHostReleased)
                {
                    return;
                }
            }

            prefabSource = sourcePrefab;
            RebuildPreviewObject();
            ResetPlayback(true);
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
            if (sequenceFinished || currentStepIndex < 0 || sequence == null || previewGameObject == null)
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
            lastRenderAt = now;
            isPaused = false;
            isPlaying = true;
            sequenceFinished = false;
            statusText = BuildStatusText();
            RequestRepaint();
        }

        public void Tick()
        {
            if (sequence == null || sequence.Steps == null || previewGameObject == null || !isPlaying || isPaused || sequenceFinished)
            {
                return;
            }

            double now = EditorApplication.timeSinceStartup;
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
            }

            bool changed = false;
            int safetyCounter = 0;

            while (safetyCounter++ < 256)
            {
                if (sequence == null || currentStepIndex < 0 || currentStepIndex >= sequence.Steps.Count)
                {
                    FinishPlayback();
                    changed = true;
                    break;
                }

                var currentStep = sequence.Steps[currentStepIndex];
                if (currentStep == null)
                {
                    changed |= AdvanceToNextStep(now);
                    continue;
                }

                if (UpdateCurrentStep(currentStep, now))
                {
                    changed = true;
                    break;
                }

                if (IsImmediateStep(currentStep))
                {
                    changed |= AdvanceToNextStep(now);
                    continue;
                }

                break;
            }

            if (safetyCounter > 256)
            {
                Debug.LogWarning("[SkillSequencePreview] Preview tick safety limit reached. Stopping playback to avoid editor stall.");
                FinishPlayback();
                changed = true;
            }

            if (changed)
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
                GUI.Label(rect, "Select a prefab with SkeletonAnimation", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            previewUtility.BeginPreview(rect, GUIStyle.none);
            RenderPreviewScene();
            Texture previewTexture = previewUtility.EndPreview();

            if (previewTexture != null)
            {
                GUI.DrawTexture(rect, previewTexture, ScaleMode.StretchToFill, false);
            }
        }

        public void Dispose()
        {
            DestroySpawnedVfx();

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

            skeletonAnimation = null;
            animationHandle = null;
            prefabSource = null;
            previewHostReleased = false;
        }

        private void RebuildPreviewObject()
        {
            DestroySpawnedVfx();

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
                SkeletonAnimation sourceSkeleton = prefabSource != null
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
                Debug.LogWarning($"[SkillSequencePreview] Failed to instantiate prefab preview: {ex.Message}");
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
                skeletonAnimation = previewGameObject.GetComponentInChildren<SkeletonAnimation>(true);
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
            }

            if (previewUtility != null)
            {
                previewUtility.AddSingleGO(previewGameObject);
                ConfigurePreviewCamera();
            }

            previewHostReleased = false;
            statusText = skeletonAnimation != null ? BuildStatusText() : "Prefab has no SkeletonAnimation";
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
            currentStepIndex = -1;
            currentStepHasDuration = false;
            stepStartedAt = EditorApplication.timeSinceStartup;
            pausedAt = stepStartedAt;
            lastRenderAt = stepStartedAt;
            lastTickAt = stepStartedAt;
            stepMoveStartPosition = ActorStartPosition;
            stepMoveEndPosition = ActorStartPosition;

            if (previewGameObject != null)
            {
                previewGameObject.transform.position = ActorStartPosition;
            }

            ClearSpawnedVfx();
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
                    PlayAnimation(currentStep);
                    currentStepHasDuration = currentStep.WaitForAnimationEnd && currentStep.Duration > 0f;
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
                    previewGameObject.transform.position = Vector3.Lerp(stepMoveStartPosition, stepMoveEndPosition, progress);
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
                    previewGameObject.transform.position = Vector3.Lerp(stepMoveStartPosition, ActorStartPosition, progress);
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
                case SkillViewStepType.SetIdleAnimation:
                    return true;
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

        private void PlayAnimation(SkillViewStep step)
        {
            if (animationHandle == null)
            {
                return;
            }

            string primary = ResolveAnimationName(step);
            string fallback = ResolveFallbackAnimationName(step);
            animationHandle.TryPlayAnimation(primary, fallback, 0.1f, 0, step.Loop);
        }

        private void PlayIdleAnimation(SkillViewStep step)
        {
            if (animationHandle == null)
            {
                return;
            }

            string idleName = string.IsNullOrWhiteSpace(sequence != null ? sequence.IdleAnimationName : null)
                ? "idle"
                : sequence.IdleAnimationName;

            animationHandle.TryPlayAnimation(idleName, idleName, 0.1f, 0, true);
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

            if (!string.IsNullOrWhiteSpace(sequence != null ? sequence.FallbackAnimationName : null))
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

            float signedDistance = step.MoveMode == SkillViewMoveMode.ThroughTarget
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
            ParticleSystem instance = UnityEngine.Object.Instantiate(step.VfxPrefab, spawnPosition, Quaternion.identity);
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
            if (skeletonAnimation != null)
            {
                if (lastRenderAt <= 0d)
                {
                    lastRenderAt = now;
                }

                float deltaTime = (float)(now - lastRenderAt);
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

            lastRenderAt = now;
            camera.Render();
        }

        private void FinishPlayback()
        {
            StopPlayback("Finished", true);
        }

        public void Stop()
        {
            StopPlayback("Stopped", false);
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

        private void StopPlayback(string nextStatus, bool releasePreviewHost)
        {
            isPlaying = false;
            isPaused = false;
            sequenceFinished = true;
            currentStepIndex = sequence != null && sequence.Steps != null && sequence.Steps.Count > 0
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

            skeletonAnimation = null;
            animationHandle = null;
            lastRenderAt = 0d;
            lastTickAt = 0d;
            previewHostReleased = true;
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

            if (isPaused)
            {
                return "Paused";
            }

            if (!isPlaying)
            {
                return "Stopped";
            }

            if (currentStepIndex < 0 || sequence.Steps == null || currentStepIndex >= sequence.Steps.Count)
            {
                return "Ready";
            }

            return BuildStatusText(sequence.Steps[currentStepIndex]);
        }

        private string BuildStatusText(SkillViewStep step)
        {
            string sequenceName = sequence != null
                ? (!string.IsNullOrWhiteSpace(sequence.SequenceId) ? sequence.SequenceId : sequence.name)
                : "No sequence";

            if (step == null)
            {
                return sequenceName + " - step pending";
            }

            return string.Format(
                "{0} - Step {1}: {2}",
                sequenceName,
                currentStepIndex + 1,
                step.StepType);
        }

        private void RequestRepaint()
        {
            if (repaintCallback != null)
            {
                repaintCallback.Invoke();
            }
        }
    }
}

#endif
