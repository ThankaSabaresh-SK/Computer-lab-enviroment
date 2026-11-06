// Computer Lab Characters - sit at / stand up from a computer desk and use the keyboard or mouse.
//
// Layout the clips are authored for (metres, relative to the CHAIR seat centre, chair tucked in at the desk):
//   seat height 0.46, desk top 0.75, desk front edge 0.26 in front of the seat centre,
//   keyboard centre 0.41 in front of the seat centre, mouse 0.30 to the right of the keyboard centre.
// Optional keyboard / mouse targets make the hands follow your real props with Animator IK.
using System.Collections;
using UnityEngine;

namespace ComputerLab.Characters
{
    public enum LabActivity { None = 0, Typing = 1, Mouse = 2, Talking = 3 }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class LabSeatController : MonoBehaviour
    {
        [Header("Chair")]
        [Tooltip("Chair transform. Its forward (+Z) must point the way a seated person looks (towards the desk).")]
        public Transform chair;
        [Tooltip("Height of the chair pivot above the floor (0 if the pivot is on the floor).")]
        public float chairPivotHeight = 0f;
        [Tooltip("Offset from the chair pivot to the seat centre, in chair space.")]
        public Vector3 seatCenterOffset = Vector3.zero;
        [Tooltip("The character stands this far in front of the seat centre before sitting.")]
        public float standOffset = 0.28f;
        [Tooltip("How far the chair (with the seated character) is pulled out from / pushed in to the desk.")]
        public float scootDistance = 0.25f;
        public float scootTime = 0.6f;
        public bool moveChair = true;
        public bool startSeated;

        [Header("Props (optional)")]
        [Tooltip("Hidden while seated, e.g. the student's backpack. Auto-filled with a child named *_Backpack.")]
        public GameObject[] hideWhileSeated;
        [Tooltip("Keyboard transform (centre of the keys, +Z away from the user). Enables hand IK while typing.")]
        public Transform keyboard;
        [Tooltip("Mouse transform. Enables right hand IK in the Mouse activity.")]
        public Transform mouse;
        [Range(0f, 1f)] public float handIKWeight = 1f;

        public bool IsSeated { get; private set; }
        public bool IsBusy { get; private set; }

        static readonly int SeatedHash = Animator.StringToHash("Seated");
        static readonly int ActivityHash = Animator.StringToHash("Activity");
        static readonly int SpeedHash = Animator.StringToHash("Speed");

        Animator anim;
        bool chairPulledOut;
        float ikLeft, ikRight;

        void Awake()
        {
            anim = GetComponent<Animator>();
            if (hideWhileSeated == null || hideWhileSeated.Length == 0)
            {
                foreach (var t in GetComponentsInChildren<Transform>(true))
                    if (t.name.EndsWith("_Backpack")) hideWhileSeated = new[] { t.gameObject };
            }
        }

        void Start()
        {
            if (startSeated && chair != null) SitImmediately();
        }

        Vector3 SeatCenter => chair.TransformPoint(seatCenterOffset);

        Vector3 ChairForward
        {
            get { var f = chair.forward; f.y = 0f; return f.normalized; }
        }

        /// <summary>Floor point where the character must stand (facing ChairForward) before SitDown.</summary>
        public Vector3 StandPoint
        {
            get
            {
                var p = SeatCenter + ChairForward * standOffset;
                p.y = chair.position.y - chairPivotHeight;
                return p;
            }
        }

        public void SetActivity(LabActivity activity) => anim.SetInteger(ActivityHash, (int)activity);

        public void SitDown()
        {
            if (!IsBusy && !IsSeated && chair != null) StartCoroutine(SitRoutine());
        }

        public void StandUp()
        {
            if (!IsBusy && IsSeated) StartCoroutine(StandRoutine());
        }

        IEnumerator SitRoutine()
        {
            IsBusy = true;
            anim.SetFloat(SpeedHash, 0f);
            if (!chairPulledOut) yield return Scoot(-1, false);       // pull the chair out from the desk
            transform.SetPositionAndRotation(StandPoint, Quaternion.LookRotation(ChairForward, Vector3.up));
            anim.applyRootMotion = false;
            ShowProps(false);
            anim.SetBool(SeatedHash, true);
            yield return WaitForState("Sit_Idle");
            yield return Scoot(+1, true);                             // shuffle in to the desk
            IsSeated = true;
            IsBusy = false;
        }

        IEnumerator StandRoutine()
        {
            IsBusy = true;
            SetActivity(LabActivity.None);
            yield return WaitForState("Sit_Idle");
            yield return Scoot(-1, true);
            anim.SetBool(SeatedHash, false);
            yield return WaitForState("Idle");
            ShowProps(true);
            anim.applyRootMotion = true;
            IsSeated = false;
            IsBusy = false;
        }

        void SitImmediately()
        {
            transform.SetPositionAndRotation(StandPoint, Quaternion.LookRotation(ChairForward, Vector3.up));
            anim.applyRootMotion = false;
            anim.SetBool(SeatedHash, true);
            anim.Play("Sit_Idle", 0, 0f);
            chairPulledOut = false;      // chair stays tucked in, character sits at the desk
            ShowProps(false);
            IsSeated = true;
        }

        IEnumerator Scoot(int direction, bool withCharacter)
        {
            Vector3 delta = ChairForward * scootDistance * direction;
            Vector3 p0 = transform.position, c0 = chair.position;
            for (float t = 0f; t < scootTime; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / scootTime);
                if (withCharacter) transform.position = p0 + delta * u;
                if (moveChair) chair.position = c0 + delta * u;
                yield return null;
            }
            if (withCharacter) transform.position = p0 + delta;
            if (moveChair) chair.position = c0 + delta;
            chairPulledOut = direction < 0;
        }

        IEnumerator WaitForState(string state)
        {
            for (float timeout = 6f; timeout > 0f; timeout -= Time.deltaTime)
            {
                if (anim.GetCurrentAnimatorStateInfo(0).IsName(state) && !anim.IsInTransition(0)) yield break;
                yield return null;
            }
        }

        void ShowProps(bool visible)
        {
            if (hideWhileSeated == null) return;
            foreach (var g in hideWhileSeated)
                if (g != null) g.SetActive(visible);
        }

        void OnAnimatorIK(int layerIndex)
        {
            var st = anim.GetCurrentAnimatorStateInfo(0);
            bool typing = keyboard != null && st.IsName("Typing");
            bool mousing = st.IsName("Mouse");
            float k = 1f - Mathf.Exp(-Time.deltaTime * 6f);
            ikLeft = Mathf.Lerp(ikLeft, (typing || (mousing && keyboard != null)) ? handIKWeight : 0f, k);
            ikRight = Mathf.Lerp(ikRight, (typing || (mousing && mouse != null)) ? handIKWeight : 0f, k);

            if (keyboard != null)
            {
                Vector3 toUser = -keyboard.forward;
                Vector3 left = keyboard.position - keyboard.right * 0.115f + toUser * 0.05f + Vector3.up * 0.035f;
                // keep the authored hand motion, only move its centre onto the real keyboard
                Vector3 animLeft = anim.GetIKPosition(AvatarIKGoal.LeftHand);
                anim.SetIKPosition(AvatarIKGoal.LeftHand, Vector3.Lerp(animLeft, left, 0.85f));
                anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, ikLeft);
                if (typing)
                {
                    Vector3 right = keyboard.position + keyboard.right * 0.115f + toUser * 0.05f + Vector3.up * 0.035f;
                    Vector3 animRight = anim.GetIKPosition(AvatarIKGoal.RightHand);
                    anim.SetIKPosition(AvatarIKGoal.RightHand, Vector3.Lerp(animRight, right, 0.85f));
                }
            }
            if (mousing && mouse != null)
                anim.SetIKPosition(AvatarIKGoal.RightHand, mouse.position + Vector3.up * 0.03f - mouse.forward * 0.06f);
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, ikRight);
        }
    }
}
