using PilotPursuit.Movement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace PilotPursuit.Gadgets
{
    public class RocketJumpController : MonoBehaviour
    {
        [SerializeField] private JumpController jumpController;
        [SerializeField] private Rocket rocketPrefab;
        [SerializeField] private Transform[] rocketSpawnPoints;
        [Header("Launch Settings")]
        [SerializeField][Min(0f)] private float launchForce = 1000f;
        [SerializeField][Min(0f)] private float launchInterval = 3f;
        [SerializeField][Min(1f)] private int clipSize = 3;
        [Header("Events")]
        public UnityEvent OnLaunchFailed;
        public UnityEvent OnLaunch, OnLastRocket, OnReload;

        private float lastLaunchTime;
        private int rocketsInClip;

        public bool CanLaunch => Time.time >= lastLaunchTime + launchInterval && rocketsInClip > 0;

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
        }

        private void Start()
        {
            lastLaunchTime = -launchInterval;
            rocketsInClip = clipSize;
        }

        #region Launch
        public void TryLaunch(InputAction.CallbackContext context)
        {
            if (context.performed) TryLaunch();
        }

        public void TryLaunch()
        {
            if (!CanLaunch)
            {
                OnLaunchFailed.Invoke();
                return;
            }

            OnLaunch.Invoke();

            lastLaunchTime = Time.time;
            rocketsInClip--;

            var jumpCollider = jumpController.GetComponent<Collider>();
            foreach (var spawnPoint in rocketSpawnPoints)
            {
                var rocket = Instantiate(rocketPrefab, spawnPoint.position, spawnPoint.rotation); // TODO: Add check to make sure point isn't in a collider
                rocket.Rigidbody.velocity = jumpController.Rigidbody.velocity;
                rocket.Rigidbody.AddRelativeForce(launchForce * Vector3.forward, ForceMode.Impulse);

                if (jumpCollider) Physics.IgnoreCollision(jumpCollider, rocket.Collider);
            }

            if (rocketsInClip == 0) OnLastRocket.Invoke();
        }
        #endregion

        [ContextMenu("Reload")]
        public void Reload() => Reload(clipSize);

        /// <summary>
        /// Adds <paramref name="rocketCount"/> rockets to <see cref="rocketsInClip"/> up to <see cref="clipSize"/>
        /// </summary>
        public void Reload(int rocketCount)
        {
            if (rocketCount <= 0) Debug.LogWarning($"{nameof(rocketCount)} was '{rocketCount}', should be > 0.");

            rocketsInClip = rocketCount == 0 ? clipSize : Mathf.Clamp(rocketsInClip + rocketCount, 0, clipSize);

            OnReload.Invoke();
        } 

        #region Debug
        private bool CheckReferences()
        {
            if (jumpController == null) Debug.LogError($"{nameof(jumpController)} is not assigned on {name}'s {GetType().Name}");
            if (rocketPrefab == null) Debug.LogError($"{nameof(rocketPrefab)} is not assigned on {name}'s {GetType().Name}");
            if (rocketSpawnPoints.Length == 0) Debug.LogError($"{nameof(rocketSpawnPoints)} is empty on {name}'s {GetType().Name}");
            else return true;

            return false;
        }
        #endregion
    }
}