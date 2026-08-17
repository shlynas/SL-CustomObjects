using System;
using System.Collections.Generic;
using DONT_TOUCH.Enums;
using DONT_TOUCH.Scripts.BlockSerialization;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DONT_TOUCH.Scripts.BlockComponents
{
    [ExecuteInEditMode, SelectionBase]
    public sealed class PrismaticCloudComponent : SchematicBlock
    {
        [Header("ProjectMER")]
        [Min(0.01f), Tooltip("How long the hazard remains active, in seconds.")]
        public float HazardDuration = 90f;

        [Tooltip("Status effect name applied by ProjectMER.")]
        public string EffectName = "Prismatic";

        [Range(1, 255), Tooltip("Status effect intensity.")]
        public byte EffectIntensity = 1;

        [Min(0.01f), Tooltip("How long the status effect remains active, in seconds.")]
        public float EffectDuration = 3f;

        [Tooltip("Whether an explosion can destroy the cloud.")]
        public bool IsDestroyable = true;

        public override BlockType BlockType => BlockType.PrismaticCloud;

        public override void Compile(SchematicBlockData block)
        {
            HazardDuration = Mathf.Max(0.01f, HazardDuration);
            EffectDuration = Mathf.Max(0.01f, EffectDuration);
            EffectIntensity = Math.Max((byte)1, EffectIntensity);
            if (string.IsNullOrWhiteSpace(EffectName))
                EffectName = "Prismatic";

            block.Properties = new Dictionary<string, object>
            {
                { nameof(HazardDuration), HazardDuration },
                { nameof(EffectName), EffectName.Trim() },
                { nameof(EffectIntensity), EffectIntensity },
                { nameof(EffectDuration), EffectDuration },
                { nameof(IsDestroyable), IsDestroyable },
            };

            base.Compile(block);
            Vector3 position = (Vector3)block.Position;
            position.y += SpawnHeightOffset;
            block.Position = position;
        }

        public override void Decompile(ref GameObject gameObject, SchematicBlockData block, Transform parent)
        {
            PrismaticCloudComponent cloud = Create<PrismaticCloudComponent>(
                "Assets/Resources/Blocks/PrismaticCloud.prefab");
            gameObject = cloud.gameObject;

            if (block.Properties != null)
            {
                if (block.Properties.TryGetValue(nameof(HazardDuration), out object hazardDuration))
                    cloud.HazardDuration = Mathf.Max(0.01f, Convert.ToSingle(hazardDuration));

                if (block.Properties.TryGetValue(nameof(EffectName), out object effectName))
                    cloud.EffectName = Convert.ToString(effectName) ?? "Prismatic";

                if (block.Properties.TryGetValue(nameof(EffectIntensity), out object effectIntensity))
                    cloud.EffectIntensity = Math.Max((byte)1, Convert.ToByte(effectIntensity));

                if (block.Properties.TryGetValue(nameof(EffectDuration), out object effectDuration))
                    cloud.EffectDuration = Mathf.Max(0.01f, Convert.ToSingle(effectDuration));

                if (block.Properties.TryGetValue(nameof(IsDestroyable), out object isDestroyable))
                    cloud.IsDestroyable = Convert.ToBoolean(isDestroyable);
            }

            base.Decompile(ref gameObject, block, parent);
            gameObject.transform.localPosition = new Vector3(
                gameObject.transform.localPosition.x,
                gameObject.transform.localPosition.y - SpawnHeightOffset,
                gameObject.transform.localPosition.z);
        }

        private void OnEnable()
        {
            _particleSystem = null;
            _particleRenderer = null;
            EnsureBakedClouds();
        }

        private void OnDisable()
        {
            if (_particleSystem != null)
                _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnValidate()
        {
            HazardDuration = Mathf.Max(0.01f, HazardDuration);
            EffectDuration = Mathf.Max(0.01f, EffectDuration);
            EffectIntensity = Math.Max((byte)1, EffectIntensity);
        }

        private void EnsureBakedClouds()
        {
            if (IsPersistentAsset())
                return;

            Transform visualRoot = EnsureVisualRoot();
            _particleSystem = visualRoot.GetComponent<ParticleSystem>();
            if (_particleSystem == null)
                _particleSystem = visualRoot.gameObject.AddComponent<ParticleSystem>();

            _particleRenderer = visualRoot.GetComponent<ParticleSystemRenderer>();
            if (_particleRenderer == null)
                _particleRenderer = visualRoot.gameObject.AddComponent<ParticleSystemRenderer>();

            BakeClouds();
        }

        private Transform EnsureVisualRoot()
        {
            Transform visualRoot = transform.Find(VisualRootName);
            if (visualRoot == null)
            {
                visualRoot = new GameObject(VisualRootName).transform;
                visualRoot.SetParent(transform, false);
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale = Vector3.one;
            }

            return visualRoot;
        }

        private void BakeClouds()
        {
            if (_particleSystem == null)
                return;

            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = _particleSystem.main;
            main.duration = 1f;
            main.loop = false;
            main.prewarm = false;
            main.playOnAwake = true;
            main.simulationSpeed = 1f;
            main.maxParticles = BakedCloudCount;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startLifetime = BakedParticleLifetime;
            main.startSpeed = 0f;
            main.startSize = BakedCloudSize;
            main.startRotation = 0f;
            main.startColor = Color.white;

            ParticleSystem.EmissionModule emission = _particleSystem.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = _particleSystem.shape;
            shape.enabled = false;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = _particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = false;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = _particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = false;

            if (_particleRenderer != null)
            {
                _particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                _particleRenderer.alignment = ParticleSystemRenderSpace.View;
                _particleRenderer.sortMode = ParticleSystemSortMode.Distance;
                _particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _particleRenderer.receiveShadows = false;
                _particleRenderer.sharedMaterial = GetParticleMaterial();
            }

            _particleSystem.Play(false);

            System.Random random = new(BakedRandomSeed);
            for (int i = 0; i < BakedCloudCount; i++)
            {
                float angle = (float)(random.NextDouble() * Mathf.PI * 2f);
                float radius = Mathf.Sqrt((float)random.NextDouble()) * CloudRadius;
                float height = (CloudBottom + CloudTop) * 0.5f;
                float particleRotation = (float)(random.NextDouble() * 360f);

                ParticleSystem.EmitParams emitParams = new()
                {
                    position = new Vector3(
                        Mathf.Cos(angle) * radius,
                        height,
                        Mathf.Sin(angle) * radius),
                    startColor = CandyColors[i % CandyColors.Length],
                    startSize = BakedCloudSize,
                    startLifetime = BakedParticleLifetime,
                    rotation = particleRotation,
                    randomSeed = (uint)(i + 1),
                };
                _particleSystem.Emit(emitParams, 1);
            }

            _particleSystem.Pause(false);
            main.simulationSpeed = 0f;
        }

        private Material GetParticleMaterial()
        {
            if (_particleMaterial == null)
                _particleMaterial = Resources.Load<Material>(ParticleMaterialPath);

            return _particleMaterial;
        }

        private void OnDrawGizmos()
        {
            float radius = CloudRadius + (BakedCloudSize * 0.5f) + BoundsPadding;
            float bottom = CloudBottom;
            float top = CloudTop;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Color previousColor = Gizmos.color;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = BoundsColor;

            DrawRing(bottom, radius);
            DrawRing(top, radius);
            for (int i = 0; i < BoundsSideCount; i++)
            {
                float angle = i * Mathf.PI * 2f / BoundsSideCount;
                Vector3 point = new(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(new Vector3(point.x, bottom, point.z), new Vector3(point.x, top, point.z));
            }

            Gizmos.matrix = previousMatrix;
            Gizmos.color = previousColor;
        }

        private void DrawRing(float y, float radius)
        {
            Vector3 previous = Vector3.zero;
            for (int i = 0; i <= RingPointCount; i++)
            {
                float angle = i * Mathf.PI * 2f / RingPointCount;
                Vector3 point = new(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
                if (i > 0)
                    Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }

        private bool IsPersistentAsset()
        {
#if UNITY_EDITOR
            return EditorUtility.IsPersistent(gameObject);
#else
            return false;
#endif
        }

        private static readonly Color[] CandyColors =
        {
            new(1.0f, 0.22f, 0.38f), // Red
            new(1.0f, 0.60f, 0.15f), // Orange
            new(1.0f, 0.92f, 0.20f), // Yellow
            new(0.35f, 0.95f, 0.35f), // Green
            new(0.20f, 0.92f, 1.00f), // Cyan
            new(0.30f, 0.70f, 1.00f), // Blue
            new(0.78f, 0.42f, 1.00f), // Violet
            new(1.0f, 0.38f, 0.82f), // Pink
        };

        private const int BakedCloudCount = 128;
        private const int BakedRandomSeed = 1984;
        private const float BakedCloudSize = 1.6f;
        private const float BakedParticleLifetime = 9999999f;
        private const float SpawnHeightOffset = 1.52f;
        private const float CloudRadius = 4.5f;
        private const float CloudBottom = 0f;
        private const float CloudTop = 1.3f;
        private const float BoundsPadding = 0.05f;
        private const int BoundsSideCount = 8;
        private const int RingPointCount = 32;
        private const string VisualRootName = "Cloud Particles";
        private const string ParticleMaterialPath = "Materials/ParticleCloudWhite";
        private static readonly Color BoundsColor = new(1f, 1f, 1f, 0.5f);

        private ParticleSystem _particleSystem;
        private ParticleSystemRenderer _particleRenderer;
        private Material _particleMaterial;
    }
}
