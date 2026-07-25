using Akila.FPSFramework;
using UnityEngine;

namespace DontDiePlease.Narrative.Runtime
{
    public sealed class NarrativeCombatMonitor : MonoBehaviour
    {
        private const float LookupInterval = 1f;
        private NarrativeDirector director;
        private Firearm firearm;
        private float nextLookupAt;

        public void Configure(NarrativeDirector narrativeDirector)
        {
            director = narrativeDirector;
            ResolveFirearm();
        }

        private void Update()
        {
            if (director == null)
            {
                return;
            }

            if (firearm == null || !firearm.isActiveAndEnabled || !firearm.gameObject.activeInHierarchy)
            {
                if (Time.unscaledTime < nextLookupAt)
                {
                    return;
                }

                nextLookupAt = Time.unscaledTime + LookupInterval;
                ResolveFirearm();
            }

            if (firearm == null ||
                firearm.itemInput == null ||
                !firearm.itemInput.SingleFire ||
                firearm.remainingAmmoCount > 0 ||
                firearm.ammoProfile?.count > 0)
            {
                return;
            }

            director.RaiseStoryEvent("REACT_NO_AMMO");
        }

        private void ResolveFirearm()
        {
            firearm = null;
            var firearms = FindObjectsByType<Firearm>(FindObjectsInactive.Include);

            foreach (var candidate in firearms)
            {
                if (candidate != null && candidate.isActiveAndEnabled && candidate.gameObject.activeInHierarchy)
                {
                    firearm = candidate;
                    return;
                }
            }
        }
    }
}
