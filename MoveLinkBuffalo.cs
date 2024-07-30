using System.Collections;
using UnityEngine;
using DG.Tweening;
using SlotGame.Sound;

namespace SlotGame.Machine.S1021
{
    public class GoldenCollect1021 : MonoBehaviour
    {
        [SerializeField] private Animator pannelAnimator;
        [SerializeField] private Animator circleAnimator;
        [SerializeField] private float circleOnTime;
        [SerializeField] private float pannelOnTime;
        [SerializeField] private float holeFadeTime;
        [SerializeField] private CanvasGroup holeCanvasGroup;
        [SerializeField] private Transform[] circleHole;

        [Header("Sound")]
        [SerializeField] private SoundPlayer free_gauge_sound;
        [SerializeField] private SoundPlayer free_gauge_buffalo_sound;

        private readonly string PANNEL_TRIGGER_OPEN = "Open";
        private readonly string CIRCLE_TRIGGER = "On";
        private readonly string PANNEL_TRIGGER_CHANGE = "Change";
        private readonly int MAX_COUNT = 4;


        private int circleCount = 0;
        private int pannelCount = 0;
        public int PannelCount { get { return pannelCount; } }
        private bool isCollected = false;

        public bool IsCollected
        {
            get { return isCollected; }
            set
            {
                Collecting_Change();
                isCollected = value;
            }
        }

        public void Initialize()
        {
            circleCount = 0;
            pannelCount = 0;
            isCollected = false;
            pannelAnimator.Rebind();
            circleAnimator.Rebind();

            holeCanvasGroup.alpha = 1.0f;
        }

        public Vector3 GetCircleTargetPos()
        {
            var pos = circleHole[circleCount].position;
            pos.z = -10.0f;

            return pos;
        }

        public IEnumerator Collecting_Open()
        {
            circleCount += 1;
            circleAnimator.SetInteger(CIRCLE_TRIGGER, circleCount);

            yield return new WaitForSeconds(circleOnTime);

            if (circleCount >= MAX_COUNT)
            {
                circleCount = 0;
                circleAnimator.SetInteger(CIRCLE_TRIGGER, circleCount);

                pannelCount += 1;
                pannelAnimator.SetInteger(PANNEL_TRIGGER_OPEN, pannelCount);
                isCollected = true;
                free_gauge_sound.Play();

                yield return new WaitForSeconds(pannelOnTime / 2);
                free_gauge_buffalo_sound.Play();

                yield return new WaitForSeconds(pannelOnTime / 2);
            }
        }

        private void Collecting_Change()
        {
            if (pannelCount >= MAX_COUNT)
            {
                holeCanvasGroup.DOFade(0.0f, holeFadeTime);
            }

            pannelAnimator.SetInteger(PANNEL_TRIGGER_CHANGE, pannelCount);
        }


        public void RestoreCollect(int collectCount)
        {
            circleCount = collectCount % 4;
            pannelCount = collectCount / 4;

            string circleTriggerName = CIRCLE_TRIGGER + circleCount;
            circleAnimator.SetTrigger(circleTriggerName);

            string pannelTriggerName = PANNEL_TRIGGER_OPEN + pannelCount;
            pannelAnimator.SetTrigger(pannelTriggerName);
        }
    }
}