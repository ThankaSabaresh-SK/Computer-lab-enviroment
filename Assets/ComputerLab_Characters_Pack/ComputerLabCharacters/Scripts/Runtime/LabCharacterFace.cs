// Computer Lab Characters - procedural face: random blinking, a friendly smile and lip movement while talking.
// Drives the "Blink", "MouthOpen" and "Smile" blendshapes on Body, Eyelashes and Eyebrows.
using System.Collections.Generic;
using UnityEngine;

namespace ComputerLab.Characters
{
    [DisallowMultipleComponent]
    public class LabCharacterFace : MonoBehaviour
    {
        [Range(0f, 100f)] public float smile = 20f;
        [Tooltip("Seconds between blinks (random range).")]
        public Vector2 blinkInterval = new Vector2(2f, 5.5f);
        public float blinkDuration = 0.14f;
        [Tooltip("Force the talking mouth on. It also turns on automatically from the Animator (Talking or Activity == 3).")]
        public bool talking;
        [Range(0f, 100f)] public float mouthOpenAmount = 35f;

        struct Target { public SkinnedMeshRenderer renderer; public int blink, mouth, smile; }

        static readonly int TalkingHash = Animator.StringToHash("Talking");
        static readonly int ActivityHash = Animator.StringToHash("Activity");

        Target[] targets;
        Animator animator;
        bool hasTalking, hasActivity;
        float nextBlink, blinkTime = -1f, mouthPhase, mouth;

        void Awake()
        {
            animator = GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                foreach (var p in animator.parameters)
                {
                    hasTalking |= p.nameHash == TalkingHash && p.type == AnimatorControllerParameterType.Bool;
                    hasActivity |= p.nameHash == ActivityHash && p.type == AnimatorControllerParameterType.Int;
                }
            }
            var list = new List<Target>();
            foreach (var r in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var m = r.sharedMesh;
                if (m == null) continue;
                var t = new Target
                {
                    renderer = r,
                    blink = m.GetBlendShapeIndex("Blink"),
                    mouth = m.GetBlendShapeIndex("MouthOpen"),
                    smile = m.GetBlendShapeIndex("Smile"),
                };
                if (t.blink >= 0 || t.mouth >= 0 || t.smile >= 0) list.Add(t);
            }
            targets = list.ToArray();
            ScheduleBlink();
        }

        void ScheduleBlink() => nextBlink = Time.time + Random.Range(blinkInterval.x, blinkInterval.y);

        public void BlinkNow() => blinkTime = 0f;

        bool IsTalking()
        {
            if (talking) return true;
            if (animator == null) return false;
            return (hasTalking && animator.GetBool(TalkingHash)) || (hasActivity && animator.GetInteger(ActivityHash) == 3);
        }

        void LateUpdate()
        {
            float blink = 0f;
            if (blinkTime < 0f && Time.time >= nextBlink) blinkTime = 0f;
            if (blinkTime >= 0f)
            {
                blinkTime += Time.deltaTime;
                float u = blinkTime / blinkDuration;
                blink = u < 0.5f ? u * 2f : 2f - u * 2f;
                if (u >= 1f) { blinkTime = -1f; blink = 0f; ScheduleBlink(); }
            }

            float targetMouth = 0f;
            if (IsTalking())
            {
                mouthPhase += Time.deltaTime;
                targetMouth = Mathf.Clamp01(Mathf.PerlinNoise(mouthPhase * 6f, 0.37f) * 1.7f - 0.4f) * mouthOpenAmount;
            }
            mouth = Mathf.Lerp(mouth, targetMouth, 1f - Mathf.Exp(-Time.deltaTime * 25f));

            foreach (var t in targets)
            {
                if (t.blink >= 0) t.renderer.SetBlendShapeWeight(t.blink, blink * 100f);
                if (t.mouth >= 0) t.renderer.SetBlendShapeWeight(t.mouth, mouth);
                if (t.smile >= 0) t.renderer.SetBlendShapeWeight(t.smile, smile);
            }
        }
    }
}
