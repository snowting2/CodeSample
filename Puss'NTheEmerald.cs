
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Spine.Unity;
using SlotGame.Attribute;
using SlotGame.Sound;

namespace SlotGame.Machine.S1052
{
    [RequireComponent(typeof(Symbol)), DisallowMultipleComponent]
    public class LinkReelSymbol1052 : LinkSymbol1052
    {
#pragma warning disable 0649         
        [Separator("LinkReel Symbol")]
        [SerializeField] private VerticalReel1052 reel;
        [SerializeField] private float moveTime = 1.0f;
        [SerializeField] private AnimationCurve slowdownAnimationCurve = AnimationCurve.Linear(0, 0, 1, 1);
        [SerializeField] private string reelStrip = string.Empty;

        [SerializeField] private SpriteMask reelMask;
        [SerializeField] private SkeletonAnimation spineFrame;
        [SerializeField] private GameObject wheel;
        [SerializeField] private Animator wheelAnimator;


        [Header("SortingGroup")]
        [SerializeField] private SortingGroup reelBackSortingGroup;     // 0
        [SerializeField] private SortingGroup childReelSortingGroup;    // 1
        [SerializeField] private SortingGroup frameSortingGroup;        // 2
        [SerializeField] private int defaultOrder = 0;
        [SerializeField] private int maskOrder = 6;

        [Header("Time")]
        [SerializeField] private float spinDuration;
        [SerializeField] private float stopIntervalTime;
        [SerializeField] private float visualChangeTime;
        [SerializeField] private float wheelStartTime = 0.6f;
        [SerializeField] private float wheelAppearDelayTime = 0.3f;

        [Header("Sound")]
        [SerializeField] private SoundPlayer wheelSpinSound;
        [SerializeField] private SoundPlayer resultSpinSound;


#pragma warning restore 0649

        private SlotMachine1052.FeatureKind currentFeature = SlotMachine1052.FeatureKind.Normal;
        private ReelStripHelper reelStripHelper = new ReelStripHelper();

        //17,14,16,16,21,12,14,12,18,12,11,13,19,15,13,11,20,14,15,12,21,15,13,11
        private readonly List<string> DirectPaySymbols = new List<string> { "11", "12", "13", "14", "15", "16" };

        private LinkSymbolManager1052 linkSymbolManager = null;

        private Coroutine wheelCoroutine = null;
        private StringUtils.KMBOption kmbOption = new StringUtils.KMBOption();

        private readonly string SPINE_ANIM_APPEAR = "Appear";
        private readonly string SPINE_ANIM_LOOP = "Loop";
        private readonly string SPINE_ANIM_RESULT = "Result";
        private readonly string SPINE_ANIM_HIT = "Hit";
        private readonly string SPINE_ANIM_WILD_HIT = "WildHit";

        private readonly string SPINE_ANIM_WHEEL_START = "WheelStart";
        private readonly string SPINE_ANIM_WHEEL_LOOP = "WheelLoop";
        private readonly string SPINE_ANIM_WHEEL_END = "WheelEnd";

        private readonly SymbolVisualType wheelStartVisual = SymbolVisualType.Etc7;
        private readonly SymbolVisualType wheelLoopVisual = SymbolVisualType.Etc8;
        private readonly SymbolVisualType wheelEndVisual = SymbolVisualType.Etc9;

        private bool isInitialize = false;

        private int currentReelStripIndex = 0;

        protected override void OnEnable()
        {
            base.OnEnable();

            if (string.IsNullOrEmpty(Symbol.id) == false)
            {
                SetBadgeCount(0, false, true);
            }

            SetDefaultOrder();
            wheel.SetActive(false);

            reel.SetPosition();
        }

        public void OnChangeFeatureKind(SlotMachine1052.FeatureKind kind)
        {
            if (currentFeature != kind)
            {
                currentFeature = kind;
            }
        }

        protected override void onVisualActivationChangeHandler(Symbol symbol, SymbolVisualType visualType, bool activation)
        {
            if (activation == false)
            {
                return;
            }

            SetSpineFrameAnimation(visualType);

            if (visualType == SymbolVisualType.Etc5)
            {
                if (isInitialize == false)
                {
                    Initialize();
                }
            }

            if (visualType == SymbolVisualType.Spin)
            {
                if (wheel.activeSelf == true || wheelCoroutine != null)
                {
                    StopWheelReel();
                }
            }

            base.onVisualActivationChangeHandler(symbol, visualType, activation);
        }

        public override void OnAppear()
        {
            OnWheelActive();
            StartCoroutine(OnWheelActiveDelay());
        }

        private IEnumerator OnWheelActiveDelay()
        {
            yield return new WaitForSeconds(wheelAppearDelayTime);

            wheel.SetActive(true);
        }

        private void OnWheelActive()
        {
            int mainSymbolIndex = reel.AllSymbolsLength - reel.SubSymbolsHalfLength - 1;
            for (int symbolIndex = 0; symbolIndex < reel.AllSymbolsLength; symbolIndex++)
            {
                var symbol = reel.GetSymbol(symbolIndex);
                if (symbolIndex != mainSymbolIndex)
                {
                    symbol.gameObject.SetActive(false);
                }
            }
        }

        private void SetSpineFrameAnimation(SymbolVisualType visualType)
        {
            switch (visualType)
            {
                // Hit
                case SymbolVisualType.Hit:
                    spineFrame.state.SetAnimation(0, SPINE_ANIM_HIT, true);
                    break;
                // Loop
                case SymbolVisualType.Etc1:
                    spineFrame.state.SetAnimation(0, SPINE_ANIM_LOOP, true);
                    break;
                // Result
                case SymbolVisualType.Etc2:
                    spineFrame.state.SetAnimation(0, SPINE_ANIM_RESULT, false);
                    break;
                // WildHit
                case SymbolVisualType.Etc4:
                    spineFrame.state.SetAnimation(0, SPINE_ANIM_WILD_HIT, true);
                    break;
                // Appear
                case SymbolVisualType.Etc5:
                    spineFrame.state.SetAnimation(0, SPINE_ANIM_APPEAR, false);
                    break;
                case SymbolVisualType.Etc7:
                    spineFrame.state.SetAnimation(0, SPINE_ANIM_WHEEL_START, false);
                    wheelAnimator.SetTrigger(SPINE_ANIM_WHEEL_START);
                    break;
                case SymbolVisualType.Etc8:
                    spineFrame.state.SetAnimation(0, SPINE_ANIM_WHEEL_LOOP, false);
                    wheelAnimator.SetTrigger(SPINE_ANIM_WHEEL_LOOP);
                    break;
                case SymbolVisualType.Etc9:
                    spineFrame.state.SetAnimation(0, SPINE_ANIM_WHEEL_END, false);
                    wheelAnimator.SetTrigger(SPINE_ANIM_WHEEL_END);
                    break;
            }
        }

        public void OnWheelStart()
        {
            if (this.gameObject.activeSelf == false)
            {
                Debug.Log("OnWheelStart active false");
                return;
            }

            SetMaskWheelOrder();

            if (isInitialize == false)
            {
                Initialize();
            }

            reel.OnInitializeSymbolPosition();

            wheel.SetActive(true);

            wheelCoroutine = StartCoroutine(StartWheelReel());
        }

        private void ResetSymbolOrder()
        {
            var startSymbolIndex = reel.ReelStrip.CurrentIndex - reel.SubSymbolsHalfLength;

            for (int i = 0; i < reel.AllSymbolsLength; i++)
            {
                var info = reel.ReelStrip.GetInfo(startSymbolIndex);
                var symbol = reel.ChangeSymbol(i, info.id);

                if (DirectPaySymbols.Contains(info.id))
                {
                    long value = linkSymbolManager.GetValue(info.id);
                    string kbmValue = StringUtils.ToKMB(SlotMachineUtils.CurrencyExchange(value), kmbOption);

                    symbol.Value = kbmValue;
                }

                startSymbolIndex++;
            }
        }


        private new void OnDisable()
        {
            StopWheelReel();
        }

        protected override void Awake()
        {
            base.Awake();

            isInitialize = false;

            kmbOption = StringUtils.DefaultKMBOption();
            kmbOption.minLength = 3;
            kmbOption.ignorZeroDecimalPoint = true;
        }

        private void Initialize()
        {
            if (linkSymbolManager == null)
            {
                linkSymbolManager = this.GetComponentInParent<LinkSymbolManager1052>();
            }

            SetupReelStrip();

            isInitialize = true;
        }

        private void SetupReelStrip()
        {
            string[] splitReelStrip = reelStrip.Split(',');
            reel.ReelStrip.Setup(splitReelStrip);
            SetupValue();

            JumpReelStrip();

            reel.Initialize();
        }

        private void JumpReelStrip()
        {
            currentReelStripIndex = Random.Range(0, reel.ReelStrip.Length);
            reel.ReelStrip.Jump(currentReelStripIndex);
        }

        private void SetupValue()
        {
            for (int i = 0; i < reel.ReelStrip.Length; i++)
            {
                string id = reel.ReelStrip.GetID(i);

                if (DirectPaySymbols.Contains(id))
                {
                    long value = linkSymbolManager.GetValue(id);
                    string kbmValue = StringUtils.ToKMB(SlotMachineUtils.CurrencyExchange(value), kmbOption);

                    reel.ReelStrip.SetValue(i, kbmValue);
                }
            }
        }

        private void SetDefaultOrder()
        {
            reelBackSortingGroup.sortingOrder = defaultOrder + 1;
            childReelSortingGroup.sortingOrder = defaultOrder + 2;
            frameSortingGroup.sortingOrder = defaultOrder + 3;
        }

        private void SetMaskWheelOrder()
        {
            var findSortingGroup = this.GetComponentInParent<SortingGroup>();
            int order = (findSortingGroup == null) ? maskOrder : findSortingGroup.sortingOrder;

            reelBackSortingGroup.sortingOrder = order;
            childReelSortingGroup.sortingOrder = order + 1;
            frameSortingGroup.sortingOrder = order + 2;
        }

        public override void Show(long value)
        {
        }

        protected override void ShowDP(string value)
        {
        }

        public override void SetSkin(string skinName)
        {
            if (spineFrame.skeleton != null)
            {
                spineFrame.skeleton.SetSkin(skinName);
            }
        }

        // JackPot Wheel 

        public void StopWheelReel(bool wheelActive = false)
        {
            if (wheelCoroutine != null)
            {
                Debug.Log("StopWheelReel");
                wheelSpinSound.Stop();

                StopCoroutine(wheelCoroutine);
                wheelCoroutine = null;

                reel.OnInitializeSymbolPosition();
            }

            wheel.SetActive(wheelActive);
        }

        private IEnumerator StartWheelReel()
        {
            yield return new WaitForEndOfFrame();

            wheelSpinSound.Play();

            while (true)
            {
                yield return ReelNext();
            }
        }

        public IEnumerator ReelNext()
        {
            var movePos = reel.SymbolSize.y * -1.0f;

            yield return reel.MoveReel(movePos, moveTime, slowdownAnimationCurve);
        }

        public override IEnumerator OnSpinResult(string symbolID)
        {
            StopWheelReel(true);

            Symbol.SetVisual(wheelStartVisual);
            resultSpinSound.Play();

            yield return new WaitForSeconds(wheelStartTime);

            reel.StartSpin();
            Symbol.SetVisual(wheelLoopVisual);

            yield return new WaitForSeconds(spinDuration);

            List<string> stopSyms = new List<string>
            {
                symbolID
            };

            var stopIndex = reelStripHelper.FindStopIndexOnReelStrip(reel.ReelStrip, stopSyms);
            reel.StopSpin(stopIndex, 0);

            Symbol.SetVisual(wheelEndVisual);

            yield return new WaitForSeconds(stopIntervalTime);
        }

        public void SetReelStripIndex(int index)
        {
            if (isInitialize == false)
            {
                Initialize();
            }

            reel.ReelStrip.Jump(index);

            ResetSymbolOrder();
        }

        public int GetReelStripIndex()
        {
            return reel.ReelStrip.CurrentIndex;
        }


        public override void SetActiveSwitchBadge(bool active)
        {
            base.SetActiveSwitchBadge(active);
        }
    }
}