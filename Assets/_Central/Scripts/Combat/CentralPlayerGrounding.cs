using UnityEngine;

namespace DontDiePlease.Central.Combat
{
    [DisallowMultipleComponent]
    public sealed class CentralPlayerGrounding : MonoBehaviour
    {
        private CharacterController controller;
        private Vector3 safePosition;
        private int startFrame;
        private float nextSampleAt;

        public void Configure(Vector3 spawn)
        {
            controller = GetComponent<CharacterController>();
            safePosition = spawn;
            startFrame = Time.frameCount;
            nextSampleAt = Time.time + 0.5f;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            safePosition = transform.position;
            startFrame = Time.frameCount;
        }

        private void LateUpdate()
        {
            if (Time.frameCount - startFrame < 10)
                return;

            if (!TryGetGroundHeight(transform.position, out var groundHeight))
            {
                if (transform.position.y < -60f)
                    MoveTo(safePosition);

                return;
            }

            if (transform.position.y < groundHeight - 3f)
            {
                var recovery = safePosition;

                if (!TryGetGroundHeight(recovery, out var safeGroundHeight))
                {
                    recovery.x = transform.position.x;
                    recovery.z = transform.position.z;
                    safeGroundHeight = groundHeight;
                }

                recovery.y = safeGroundHeight + 0.12f;
                MoveTo(recovery);
                return;
            }

            if (Time.time < nextSampleAt || controller == null || !controller.isGrounded)
                return;

            safePosition = transform.position;
            safePosition.y = groundHeight + 0.12f;
            nextSampleAt = Time.time + 0.5f;
        }

        private void MoveTo(Vector3 position)
        {
            var wasEnabled = controller != null && controller.enabled;

            if (wasEnabled)
                controller.enabled = false;

            transform.position = position;
            Physics.SyncTransforms();

            if (wasEnabled)
                controller.enabled = true;
        }

        private bool TryGetGroundHeight(Vector3 position, out float height)
        {
            foreach (var terrain in Terrain.activeTerrains)
            {
                if (terrain == null || terrain.terrainData == null)
                    continue;

                var origin = terrain.transform.position;
                var size = terrain.terrainData.size;

                if (position.x < origin.x || position.x > origin.x + size.x ||
                    position.z < origin.z || position.z > origin.z + size.z)
                {
                    continue;
                }

                height = terrain.SampleHeight(position) + origin.y;
                return true;
            }

            var mask = Physics.DefaultRaycastLayers;

            if (gameObject.layer >= 0)
                mask &= ~(1 << gameObject.layer);

            var rayOrigin = position + Vector3.up * 20f;

            if (Physics.Raycast(
                    rayOrigin,
                    Vector3.down,
                    out var hit,
                    220f,
                    mask,
                    QueryTriggerInteraction.Ignore))
            {
                height = hit.point.y;
                return true;
            }

            height = 0f;
            return false;
        }
    }
}
