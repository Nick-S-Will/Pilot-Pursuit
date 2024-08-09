using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace PilotPursuit.Gadgets
{
    public class Rocket : MonoBehaviour
    {
        [SerializeField] private new Rigidbody rigidbody;
        [SerializeField] private new Collider collider;
        [Header("Explosion Settings")]
        [SerializeField][Min(0f)] private float explosionForce = 5000f;
        [SerializeField][Min(0f)] private float explosionRadius = 2f, upwardsModifier = 1f, explosionDuration = .1f;
        [Header("Physics Checks")]
        [SerializeField] private LayerMask explosionMask;
        [Header("Events")]
        public UnityEvent OnExplode;

        private Coroutine explosionRoutine;

        public Rigidbody Rigidbody => rigidbody;
        public Collider Collider => collider;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
        }

        private void OnCollisionEnter(Collision collision)
        {
            Explode();
        }

        private void Explode() => explosionRoutine ??= StartCoroutine(Explosion());

        private IEnumerator Explosion()
        {
            OnExplode.Invoke();

            var collidersInRange = Physics.OverlapSphere(rigidbody.position, explosionRadius, explosionMask);
            var rigidbodiesInRange = collidersInRange.Select(collider => collider.attachedRigidbody).Where(rb => rb != null).Distinct().ToArray();
            if (explosionDuration == 0f) ApplyExplosionForce(rigidbodiesInRange, ForceMode.Impulse);
            else
            {
                var startTime = Time.time;
                while (Time.time < startTime + explosionDuration)
                {
                    ApplyExplosionForce(rigidbodiesInRange);

                    yield return new WaitForFixedUpdate();
                }
            }

            Destroy(gameObject);
            explosionRoutine = null;
        }

        private void ApplyExplosionForce(Rigidbody[] rigidbodies, ForceMode forceMode = ForceMode.Force)
        {
            foreach (var rigidbody in rigidbodies)
            {
                rigidbody.AddExplosionForce(explosionForce, this.rigidbody.position, explosionRadius, upwardsModifier, forceMode);
            }
        }

        #region Debug
        private bool CheckReferences()
        {
            if (rigidbody == null) Debug.LogError($"{nameof(rigidbody)} is not assigned on {name}'s {GetType().Name}");
            if (collider == null) Debug.LogError($"{nameof(collider)} is not assigned on {name}'s {GetType().Name}");
            else return true;

            return false;
        }
        #endregion
    }
}