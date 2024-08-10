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
        [SerializeField] private Transform[] launchPoints;
        [Header("Launch Settings")]
        [SerializeField][Min(0f)] private float launchForce = 1000f;
        [SerializeField][Min(0f)] private float launchInterval = 3f;
        [SerializeField][Min(1f)] private int clipSize = 3;
        [SerializeField] private bool mustBeInAir = true;
        [Header("Physics Checks")]
        [SerializeField] private LayerMask obstacleMask;
        [Tooltip("Set to (0, 0, 0) to disable obstacle check.")]
        [SerializeField] private Vector3 obstacleCheckExtents = .5f * Vector3.one;
        [Header("Events")]
        public UnityEvent OnLaunchFailed;
        public UnityEvent OnLaunchBlocked, OnLaunch, OnLastRocket, OnReload;
        [Header("Debug")]
        [SerializeField] private bool logEvents;

        private float lastLaunchTime;
        private int rocketsInClip;

        /// <summary>
        /// True if launcher has rockets loaded
        /// </summary>
        public bool IsReadyToLaunch => Time.time >= lastLaunchTime + launchInterval && rocketsInClip > 0;
        /// <summary>
        /// True if none of the <see cref="launchPoints"/> are obstructed
        /// </summary>
        public bool IsClearToLaunch
        {
            get
            {
                if (obstacleCheckExtents == Vector3.zero) return true;

                var colliders = new Collider[1];
                foreach (var point in launchPoints)
                {
                    var overlapCount = Physics.OverlapBoxNonAlloc(point.position, obstacleCheckExtents, colliders, point.rotation, obstacleMask);
                    if (overlapCount > 0) return false;
                }

                return true;
            }
        }

        private void Awake()
        {
            if (!CheckReferences()) enabled = false;
            if (logEvents) AddLogToEvents();
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
            if (!enabled || (mustBeInAir && jumpController.IsOnGround)) return;

            if (!IsReadyToLaunch)
            {
                OnLaunchFailed.Invoke();
                return;
            }

            if (!IsClearToLaunch)
            {
                OnLaunchBlocked.Invoke();
                return;
            }

            lastLaunchTime = Time.time;
            rocketsInClip--;
            OnLaunch.Invoke();

            var jumpCollider = jumpController.GetComponent<Collider>();
            foreach (var launchPoint in launchPoints)
            {
                var rocket = Instantiate(rocketPrefab, launchPoint.position, launchPoint.rotation);
                rocket.Rigidbody.velocity = jumpController.Rigidbody.velocity;
                rocket.Rigidbody.AddRelativeForce(launchForce * Vector3.forward, ForceMode.Impulse);

                if (jumpCollider) Physics.IgnoreCollision(jumpCollider, rocket.Collider);
            }

            if (rocketsInClip == 0) OnLastRocket.Invoke();
        }
        #endregion

        #region Reload
        [ContextMenu("Reload")]
        public void Reload() => Reload(clipSize);

        public void Reload(int rocketCount)
        {
            if (rocketCount <= 0) Debug.LogWarning($"{nameof(rocketCount)} was '{rocketCount}', should be > 0.");

            rocketsInClip = rocketCount == 0 ? clipSize : Mathf.Clamp(rocketsInClip + rocketCount, 0, clipSize);

            OnReload.Invoke();
        }
        #endregion

        #region Debug
        private bool CheckReferences()
        {
            if (jumpController == null) Debug.LogError($"{nameof(jumpController)} is not assigned on {name}'s {GetType().Name}");
            else if (rocketPrefab == null) Debug.LogError($"{nameof(rocketPrefab)} is not assigned on {name}'s {GetType().Name}");
            else if (launchPoints.Length == 0) Debug.LogError($"{nameof(launchPoints)} is empty on {name}'s {GetType().Name}");
            else return true;

            return false;
        }

        private void AddLogToEvents()
        {
            OnLaunchFailed.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnLaunchFailed)));
            OnLaunchBlocked.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnLaunchBlocked)));
            OnLaunch.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnLaunch)));
            OnLastRocket.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnLastRocket)));
            OnReload.AddListener(() => Debug.Log(GetType().Name + ": " + nameof(OnReload)));
        }
        #endregion
    }
}