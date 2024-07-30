using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using SlotGame.Attribute;
using SlotGame.Sound;
using DG.Tweening;
using Spine.Unity;

namespace SlotGame.Machine.S1045
{
    public class SlotMachine1045 : SlotMachine
    {
#pragma warning disable 0649
        [Separator("Extention")]
        [SerializeField] private Animator slotAnimator;
        [SerializeField] private Animator flexibleAnimator;
        [SerializeField] private Animator machineLightAnimator;
        [SerializeField] private Animator extraFireAnimator;
        [SerializeField] private GameObject machineTopObject;
        [SerializeField] private PotAnimationCtrl1045 potControl;
        [SerializeField] private ReelEffect reelEffect;

        [SerializeField] private SymbolPool symbolPool;
        [SerializeField] private SymbolPool extraSymbolPool;

        [SerializeField] private ReelGroup extraReelGroup;
        [SerializeField] private SlotUI1045 slotUI;
        [SerializeField] private MegaBonus1045 megaBonus;
        [SerializeField] private string emptySymbolID;
        [SerializeField] private float freespinHitDelayTime;
        [SerializeField] private float titleIntroDelayTime;
        [SerializeField] private float freeSirenDelayTime;

        [Header("Flexible Time")]
        [SerializeField] private float normalToFreeTime;
        [SerializeField] private float normalToFreeEndTime;
        [SerializeField] private float freeToResultTime;

        [Header("Pot")]
        [SerializeField] private Animator potCoinAnimator;
        [SerializeField] private float potCoinTime; // pot trail 이 들어오는 시간
        [SerializeField] private float potCoinEndTime;  // pot coin time 종료 시간
        [SerializeField] private float potMoveBeforeDelayTime;    // y축 이동 전 딜레이 시간
        [SerializeField] private Animator potAnimator;
        [SerializeField] private Transform potCoinTransform;
        [SerializeField] private float potCoinMoveTime; // pot 코인 y 축 이동 시간
        [SerializeField] private float potCoinFreeMoveTime; // 프리스핀 때 pot 코인 y축 이동 시간
        [SerializeField] private GameObject[] potStepBG;

        [SerializeField] private float potAppearTime;
        [SerializeField] private float potUpgradeTime;
        [SerializeField] private float potUpgradeDelayTime;
        [SerializeField] private float potCompleteTime;
        [SerializeField] private float potResetTime;

        [SerializeField] private float potTrailSpeedRateOfTurbo = 3.0f;

        [Header("Reel Acc")]
        [SerializeField] private Animator[] reelAnimator;
        [SerializeField] private Animator extraReelAnimator;

        [Header("Zoom In/Out")]
        [SerializeField] private Transform[] frameRoots;
        [SerializeField] private float zoomInValue;
        [SerializeField] private Ease zoomInEase;
        [SerializeField] private float zoomOutValue;
        [SerializeField] private Ease zoomOutEase;
        [SerializeField] private float zoomInTime;
        [SerializeField] private float zoomOutTime;

        [Header("Delete Symbol Event Reel")]
        [SerializeField] private float deleteSymbolWaitingTime;
        [SerializeField] private float deleteEventEndTime;

        [Header("Sounds")]
        [SerializeField] private SoundPlayer normalBGM;
        [SerializeField] private SoundPlayer freeBGM;
        [SerializeField] private SoundPlayer longspinSound;
        [SerializeField] private SoundPlayer extraReelStopSound;
        [SerializeField] private SoundPlayer extraNormalLongSpinSound;
        [SerializeField] private SoundPlayer extraFreeLongSpinSound;
        [SerializeField] private SoundPlayer[] extraSymbolHitSound; // [0] ~5 , [1] ~10, [2] ~50
        [SerializeField] private SoundPlayer coinTrailSound;
        [SerializeField] private SoundPlayer freeIntro_potExplo_Sound;

        [SerializeField] private SoundPlayer[] freeIntro_title_Sound;
        [SerializeField] private SoundPlayer freeWinNormalSound;
        [SerializeField] private SoundPlayer freeWinBigSound;
        [SerializeField] private SoundPlayer freeExtraDelMoveSound;
        [SerializeField] private SoundPlayer freeExtraDelSound;
        [SerializeField] private SoundPlayer freeEndFireEffectSound;
        [SerializeField] private SoundPlayer potUpgradeSound;
        [SerializeField] private SoundPlayer nudgeSound;
        [SerializeField] private SoundPlayer potResetSound;
        [SerializeField] private SoundPlayer sirenSound;

        [Header("Nudge_(total 100 rate = up + down + none)")]

        [SerializeField] private int rateNudgeUp;
        [SerializeField] private int rateNudgeDown;

#pragma warning restore 0649

        private List<ReelStopInfo> reelStopInfos = new List<ReelStopInfo>();
        private BaseReel extraReel = null;
        private int currentPotIndex = 0;
        private int[] weights = new int[3];
        private readonly int[] nudgeVector = new int[] { -1, 0, 1 };

        private readonly string rowSymbolID_1 = "10";
        private readonly string rowSymbolID_2 = "11";

        private bool isFreespinEnd = false;

        private enum PotState
        {
            None,
            Appear,
            Upgade,
        }

        private PotState potState = PotState.None;

        private readonly int LAST_REEL_INDEX = 2;

        private readonly int EXTRA_REEL_INDEX = 0;
        private readonly int EXTRA_REEL_MAINSYMBOL_INEX = 1;

        private readonly int EXTRA_VALUE_MAINSYMBOL_COUNT = 3;
        private readonly int EXTRA_INDEX_MULTIPLE_ID = 1;

        private readonly int EXTRA_INDEX_EPIC_STEP = 3; // 0 - 18
        private readonly int EXTRA_INDEX_EPIC_CLEAR_COUNT = 4;
        private readonly int EXTRA_INDEX_POT_STATE = 5;

        // Common
        private readonly string HIT_TRIGGER = "hit";
        private readonly string EMPHASIS_TRIGGER = "emphasis";

        // PotCoin Animator Trigger
        private readonly string POT_COIN_TRAIL_TRIGGER = "trail";

        // PotAnimator Trigger
        //private readonly string POT_UPGRADE_TRIGGER = "upgrade";    // integer
        private readonly string POT_FREESPIN = "freespin";          // bool
        private readonly string POT_COMPLETE = "complete";
        private readonly string POT_RESET = "reset";


        private readonly float[] POT_POS = new float[] { -0.2f, 0.0f, 0.2f, 0.4f, 0.6f, 0.8f };
        private readonly float POT_POS_DEFAULT = -0.4f;

        // Reel Trigger        
        private readonly string REEL_LONG_SPIN_TRIGGER = "longSpin";
        private readonly string REEL_APPEAR_TRIGGER = "appear";

        // Slot Trigger
        private readonly string FREE_COUNT_TRIGGER = "count";
        private readonly string FREE_MULTI_TRIGGER = "multi";
        private readonly string FREE_TO_RESULT_TRIGGER = "freeToResult";
        private readonly string RESULT_TO_NORMAL_TRIGGER = "resultToNormal";
        private readonly string NORMAL_TO_FREE_TRIGGER = "normalToFree";
        private readonly string NORMAL_TO_FREE_LOCK_TRIGGER = "normalToFreeLock";
        private readonly string FORCE_FREESPIN_TRIGGER = "forcefreespin";

        // Machine Light Trigger
        private readonly string MACHINE_LIGHT_FREESPIN = "freespin";
        private readonly string MACHINE_LIGHT_WINGRADE = "winGrade";
        private readonly string MACHINE_LIGHT_COMPLETE = "complete";

        protected override void UpdateEnableReelGroupList()
        {
            base.UpdateEnableReelGroupList();
            enableReelGroupList.Add(extraReelGroup);
        }

        // 슬롯머신 진입시 최초 한번 실행되어야 할 스테이트
        // 이 부분에 슬롯머신 진입 애니메이션 등을 구현해야함.
        protected override IEnumerator EnterState()
        {
            extraReelGroup.PrecedingReelGroup = ReelGroup;
            potCoinTransform.localPosition = new Vector3(0.0f, POT_POS_DEFAULT, 0.0f);

            SetupPotState();
            ChangeReelModeEnv();
            SetNudgeWeights();

            if (Info.IsFreespin)
            {
                FreeSpinSymbolVisualChange(false);
            }

            int epicClearCount = 0;
            int epicStep = 0;

            if (Info.GetExtrasCount() >= EXTRA_INDEX_EPIC_CLEAR_COUNT)
            {
                epicClearCount = (int)Info.GetExtraValue(EXTRA_INDEX_EPIC_CLEAR_COUNT);
                epicStep = (int)Info.GetExtraValue(EXTRA_INDEX_EPIC_STEP);
            }

            megaBonus.OnEnter(epicClearCount, epicStep);

            SetPotStepBG();

            extraReel = extraReelGroup.GetReel(EXTRA_REEL_INDEX);

            ReelGroup.OnStateStart.AddListener(OnReelStateStart);
            extraReelGroup.OnStateStart.AddListener(OnExtraReelStateStart);

            ReelGroup.OnStateEnd.AddListener(OnReelStateEnd);
            extraReelGroup.OnStateEnd.AddListener(OnExtraReelStateEnd);

            paylinesHelper.OnPaylineDrawAllEvent.AddListener(OnPaylineDrawAllEvent);
            paylinesHelper.OnPaylineDrawEvent.AddListener(OnPaylineDrawEvent);

            yield return base.EnterState();
        }

        protected override void OnReelModeChanged(ReelMode reelMode)
        {
            if (reelMode == ReelMode.Free)
            {
                winSoundPlayer.SelectJukeBox(1);
            }
            else if (reelMode == ReelMode.Regular)
            {
                normalBGM.Play();
                winSoundPlayer.SelectJukeBox(0);
            }
        }

        // Reel Start
        private void OnReelStateStart(int reelIndex, ReelState state, BaseReel reel)
        {
            if (state == ReelState.Spin)
            {
                reelAnimator[reelIndex].SetBool(HIT_TRIGGER, false);
                slotAnimator.SetBool(HIT_TRIGGER, false);
            }
        }

        private void OnExtraReelStateStart(int reelIndex, ReelState state, BaseReel reel)
        {
            if (state == ReelState.Spin)
            {
                extraReelAnimator.Rebind();
                slotAnimator.SetBool(HIT_TRIGGER, false);
            }
            if (state == ReelState.Slowdown)
            {


                if (extraReel.NudgeCount != 0)
                {
                    nudgeSound.Play();
                }
            }
        }

        // Reel End
        private void OnReelStateEnd(int reelIndex, ReelState state, BaseReel reel)
        {
            if (state == ReelState.Slowdown)
            {
                // reelstopSound.Play();
                if (reelIndex == 2)
                {
                    if (extraReelGroup.intervalType != ReelGroup.IntervalType.Default
                        && extraReelGroup.intervalType != ReelGroup.IntervalType.Extra6)
                    {
                        if (CurrentReelMode == ReelMode.Free)
                        {
                            extraFreeLongSpinSound.Play();
                            extraFireAnimator.SetBool(REEL_LONG_SPIN_TRIGGER, true);
                        }
                        else
                        {
                            extraNormalLongSpinSound.Play();
                        }

                        SetVisualInExtraLongSpin();

                        extraReelAnimator.SetBool(REEL_LONG_SPIN_TRIGGER, true);
                    }
                }
            }
        }

        private void PlayExtraHitSound()
        {
            // 히트 상황에서 엑스트라 심볼 배수에 걸렸을 경우 사운드
            int index = GetMultipleIndex();
            if (index != -1)
            {
                if (Info.SpinResult.WinCoinsHit > 0)
                {
                    extraSymbolHitSound[index].Play();
                }
            }
        }

        private void OnExtraReelStateEnd(int reelIndex, ReelState state, BaseReel reel)
        {
            if (state == ReelState.Spin)
            {
                SetNudgeCount();
            }
            if (state == ReelState.Slowdown)
            {
                extraReelStopSound.Play();

                var extraReel = extraReelGroup.GetReel(EXTRA_REEL_INDEX);
                var extraSymbol = extraReel.GetMainSymbol(EXTRA_REEL_MAINSYMBOL_INEX);

                if (CurrentReelMode == ReelMode.Free)
                {
                    extraFireAnimator.SetBool(REEL_LONG_SPIN_TRIGGER, false);
                    extraFreeLongSpinSound.Stop();

                    if (extraSymbol.id != emptySymbolID)
                    {
                        if (Info.SpinResult.WinCoinsHit > 0)
                        {
                            // Appear Visual = Etc1
                            extraSymbol.SetVisual(SymbolVisualType.Etc1);
                        }

                        PlayExtraHitSound();
                    }
                }
                else if (CurrentReelMode == ReelMode.Regular)
                {
                    extraNormalLongSpinSound.Stop();

                    if (extraSymbol.id != emptySymbolID)
                    {
                        // Appear Visual = Etc1
                        extraSymbol.SetVisual(SymbolVisualType.Etc1);

                        StartCoroutine(OnPotCoin());

                        PlayExtraHitSound();
                    }
                }

                if (CalculateMultiValueToWinCoin() <= 5)
                {
                    if (CurrentReelMode == ReelMode.Regular)
                    {
                        extraNormalLongSpinSound.Stop();
                    }
                    else if (CurrentReelMode == ReelMode.Free)
                    {
                        extraFreeLongSpinSound.Stop();
                    }
                }

                var symbol = extraReel.GetMainSymbol(EXTRA_REEL_MAINSYMBOL_INEX);
                if (symbol.id != emptySymbolID)
                {
                    if (Info.SpinResult.WinCoinsTotal > 0)
                    {
                        extraReelAnimator.SetBool(REEL_APPEAR_TRIGGER, true);
                    }
                }
            }

            if (state == ReelState.Stop)
            {

            }
        }

        public void OnReelEffectAppear(int presetIndex, int reelIndex, BaseReel reel)
        {
            if (reelIndex == LAST_REEL_INDEX)
            {
                reelAnimator[reelIndex].Rebind();
                reelAnimator[reelIndex].SetBool(REEL_APPEAR_TRIGGER, true);
            }
        }

        public void OnReelEffecDisappearDetail(int presetIndex, int reelIndex, GameObject effectObject)
        {
            if (reelIndex == LAST_REEL_INDEX)
            {
                reelAnimator[reelIndex].SetBool(REEL_APPEAR_TRIGGER, false);
            }


            StartCoroutine(StopLongSpinSound(presetIndex));
        }

        private IEnumerator StopLongSpinSound(int presetIndex)
        {
            var preset = reelEffect.GetPreset(presetIndex);

            yield return WaitForSeconds(preset.hideDelay);

            longspinSound.Stop();
        }


        private void SetVisualInExtraLongSpin()
        {
            ShowDimPanel();

            for (int infoIndex = 0; infoIndex < Info.SpinResult.GetWinInfoCountOfAllMachines(); infoIndex++)
            {
                var winInfo = Info.SpinResult.GetWinInfoFromAllMachines(infoIndex);

                for (int reelIndex = 0; reelIndex < winInfo.hitSymbolPositions.Count; reelIndex++)
                {
                    var symbolList = winInfo.hitSymbolPositions[reelIndex];

                    for (int index = 0; index < symbolList.Count; index++)
                    {
                        int mainSymbolIndex = symbolList[index];

                        var symbol = ReelGroup.GetReel(reelIndex).GetMainSymbol(mainSymbolIndex);
                        symbol.SetVisual(SymbolVisualType.Etc3);
                    }
                }
            }
        }

        private void SetExtraReelStopInterval()
        {
            // 단계) 0초과 ~ 5미만 // ~10미만 // 10이상
            // 추가) 배수 당첨이 아니더라도, 당첨이 있을시 롱스핀 들어갑니다.
            float multiValue = CalculateMultiValueToWinCoin();

            if (multiValue == 0)
            {
                return;
            }

            if (CurrentPlayModeInProgress == PlayMode.Fast)
            {
                extraReelGroup.intervalType = ReelGroup.IntervalType.Extra5;
            }
            else
            {
                if (multiValue < 5.0f)
                {
                    extraReelGroup.intervalType = ReelGroup.IntervalType.Extra1;
                }
                else if (multiValue < 10.0f)
                {
                    extraReelGroup.intervalType = ReelGroup.IntervalType.Extra2;
                }
                else
                {
                    extraReelGroup.intervalType = ReelGroup.IntervalType.Extra3;
                }
            }
        }

        private float CalculateMultiValueToWinCoin()
        {
            long winCoinTotal = Info.SpinResult.WinCoinsTotal;
            long currentBet = Info.CurrentTotalBet;
            
            if (winCoinTotal == 0)
            {
                return 0;
            }

            float result = winCoinTotal / currentBet;

            // 배수 당첨이 아니더라도, 당첨이 있을시 롱스핀 들어갑니다.
            if (result == 0)
            {
                if (winCoinTotal > 0)
                {
                    result = 1;
                }
            }

            return result;
        }

        protected override void SetMachineGroupForSpin(PlayMode playMode)
        {
            base.SetMachineGroupForSpin(playMode);

            if (playMode == PlayMode.Normal)
            {
                reelGroup.SetSkipSpinOfAllReels(false);
                reelGroup.interpolateBetweenSpinStartAndStopTime = true;

                extraReelGroup.SetSkipSpinOfAllReels(false);
                extraReelGroup.interpolateBetweenSpinStartAndStopTime = true;
                extraReelGroup.forceUseIntervalWhenStopSpinOfFirstReel = false;
                extraReelGroup.intervalType = ReelGroup.IntervalType.Default;
                extraReel.SetTimeSpeedRate(DefaultReelTimeRate);

            }
            else if (playMode == PlayMode.Fast)
            {
                reelGroup.SetSkipSpinOfAllReels(false);
                reelGroup.interpolateBetweenSpinStartAndStopTime = false;

                extraReelGroup.SetSkipSpinOfAllReels(false);
                extraReelGroup.interpolateBetweenSpinStartAndStopTime = false;
                extraReelGroup.ignoreInterval = true;
                extraReelGroup.forceUseIntervalWhenStopSpinOfFirstReel = true;
                extraReelGroup.intervalType = ReelGroup.IntervalType.Extra6;
                extraReel.SetTimeSpeedRate(fastAutoSpinReelTimeRate);
                extraReel.keepSpinning = false;
            }
            else if (playMode == PlayMode.Turbo)
            {
                reelGroup.SetSkipSpinOfAllReels(true, PLAYMODE_TURBO_SKIP_SPIN_EXPIRATIONTIME);
                reelGroup.interpolateBetweenSpinStartAndStopTime = false;

                extraReelGroup.SetSkipSpinOfAllReels(true, PLAYMODE_TURBO_SKIP_SPIN_EXPIRATIONTIME);
                extraReelGroup.interpolateBetweenSpinStartAndStopTime = false;
                extraReelGroup.ignoreInterval = true;
                extraReelGroup.forceUseIntervalWhenStopSpinOfFirstReel = false;
                extraReelGroup.intervalType = ReelGroup.IntervalType.Default;
                extraReel.SetTimeSpeedRate(turboAutoSpinReelTimeRate);
                extraReel.keepSpinning = false;
            }
        }

        // 기본 상태.
        // 기본적으로 유저의 입력을 기다리지만 프리스핀, 리스핀, 오토스핀등의 상황에선 자동으로 스핀 상태로 변경.
        protected override IEnumerator IdleState()
        {
            extraReelGroup.intervalType = ReelGroup.IntervalType.Default;

            yield return base.IdleState();
        }

        // 스핀을 시작하는 스테이트.
        protected override IEnumerator SpinStartState()
        {
            HideDimPanel();
            potState = PotState.None;
            machineLightAnimator.SetInteger(MACHINE_LIGHT_WINGRADE, 0);

            if (CurrentReelMode == ReelMode.Free)
            {
                flexibleAnimator.SetBool(FREE_MULTI_TRIGGER, false);
                slotUI.OffFreeMultiple();
            }

            reelStopInfos.Clear();
            extraReelGroup.StartSpin();

            yield return base.SpinStartState();

            SetExtraReelStopInterval();

            if (CurrentReelMode == ReelMode.Free)
            {
                flexibleAnimator.SetTrigger(FREE_COUNT_TRIGGER);
                slotUI.SetCurrentRespin(Info.FreespinCurrentCount);
            }
        }

        public override void StartSpin()
        {
            if (extraReelGroup.IsBusy == true)
            {
                extraReelGroup.ignoreInterval = true;
            }
            base.StartSpin();
        }

        // 스핀을 멈추는 스테이트.
        // 스핀 정지 명령을 내리고 실제 스핀이 멈추는 순간까지 유지된다.
        protected override IEnumerator SpinStopState()
        {
            if (CurrentPlayModeInProgress == PlayMode.Turbo)
            {
                potCoinAnimator.speed = potTrailSpeedRateOfTurbo;
            }
            else
            {
                potCoinAnimator.speed = 1.0f;
            }

            DispatchReelGroupStopSpin();
            machineGroup.StopSpin(Info.SpinResult.ReelStopInfoList);
            ExtraSpinEndState();

            while (machineGroup.IsBusy == true || extraReelGroup.IsBusy == true)
            {
                yield return null;
            }

            NextState(SlotMachineState.SPIN_END);
        }

        // 스핀이 완전히 종료되었을때 호출 되는 스테이트
        // 스핀 종료 후 특별한 연출등을 표현 (ex : 스택드 와일드 합체 등등)
        protected override IEnumerator SpinEndState()
        {
            yield return base.SpinEndState();
        }

        private IEnumerator OnPotCoin()
        {
            int multiSymbolID = (int)Info.GetExtraValue(EXTRA_INDEX_MULTIPLE_ID);

            if (multiSymbolID.ToString() == emptySymbolID)
            {
                potState = PotState.None;
                yield break;
            }

            PotCoinEffect(multiSymbolID);

            int potIndex = (int)Info.GetExtraValue(EXTRA_INDEX_POT_STATE);
            SetCurrentPotState(potIndex);

            yield return WaitForSeconds(potCoinTime);

            slotAnimator.SetTrigger(EMPHASIS_TRIGGER);

            switch (potState)
            {
                case PotState.Upgade:
                    {
                        potCoinTransform.DOLocalMoveY(POT_POS[potIndex], potUpgradeTime);

                        currentPotIndex = potIndex;

                        yield return WaitForSeconds(potUpgradeTime);
                    }
                    break;
                case PotState.Appear:
                    {
                        yield return WaitForSeconds(potCoinEndTime);
                    }
                    break;
            }
        }

        private void SetCurrentPotState(int potIndex)
        {
            potState = PotState.None;

            if (currentPotIndex != potIndex)
            {
                if (Info.IsFreespinStart == false)
                {
                    potState = PotState.Upgade;
                }
            }
            else
            {
                potState = PotState.Appear;
            }
        }

        private void PotCoinEffect(int multiSymbolID)
        {
            string triggerName = GetTriggerName(POT_COIN_TRAIL_TRIGGER, multiSymbolID);

            potCoinAnimator.Rebind();
            potCoinAnimator.SetTrigger(triggerName);
            coinTrailSound.Play();
        }

        private string GetTriggerName(string triggerName, int multiSymbolID)
        {
            string result = triggerName;
            switch (multiSymbolID)
            {
                case 10:
                case 11:
                    {
                        result += "1";
                        break;
                    }
                case 12:
                case 13:
                    {
                        result += "2";
                        break;
                    }
                case 14:
                case 15:
                case 16:
                    {
                        result += "3";
                        break;
                    }
                default:
                    {
                        Debug.Log("GetTriggerName - wrong multiSymbolID : " + multiSymbolID);
                        break;
                    }
            }

            return result;
        }


        private void ExtraSpinEndState()
        {
            List<string> extraIDList = new List<string>();
            for (int i = 0; i < EXTRA_VALUE_MAINSYMBOL_COUNT; i++)
            {
                string symbolID = Info.GetExtraValue(i).ToString();
                extraIDList.Add(symbolID);
            }

            int stopIndex = reelStripHelper.FindStopIndexOnReelStrip(extraReel.ReelStrip, extraIDList);

            var reelStopInfo = new ReelStopInfo()
            {
                reelIndex = EXTRA_REEL_INDEX,
                stopIndexOnReelStrip = stopIndex,
                nudgeCount = 0
            };

            reelStopInfos.Add(reelStopInfo);
            extraReelGroup.StopSpin(reelStopInfos);
        }

        // 스핀이 멈추고 결과를 시작 되는 스테이트
        // 잭팟, 빅윈 등에 분기점
        protected override IEnumerator ResultStartState()
        {
            yield return base.ResultStartState();

        }

        // 결과 종료 스테이트
        // idle 상태로 돌아감
        protected override IEnumerator ResultEndState()
        {
            if (CurrentReelMode == ReelMode.Regular)
            {
                if (CurrentPlayModeInProgress != PlayMode.Turbo)
                {
                    switch (potState)
                    {
                        case PotState.Appear:
                            {
                                yield return WaitForSeconds(potAppearTime);
                            }
                            break;
                        case PotState.Upgade:
                            {
                                yield return WaitForSeconds(potUpgradeDelayTime);
                            }
                            break;
                    }
                }
            }
            else if (CurrentReelMode == ReelMode.Free)
            {
                if (Info.SpinResult.WinCoinsHit > 0 && Info.IsFreespinEnd == false)
                {
                    yield return WaitForSeconds(freespinHitDelayTime, true);
                }
            }

            if (Info.IsFreespinEnd)
            {
                while (isFreespinEnd == false)
                {
                    yield return null;
                }
            }

            yield return base.ResultEndState();
        }

        private void FreeSpinSymbolVisualChange(bool resetToIdle = true)
        {
            string[] symbolIds = symbolPool.GetAllID();
            string[] extraSymbolIds = extraSymbolPool.GetAllID();

            foreach (string id in symbolIds)
            {
                Symbol.AddSubstituteVisualAtGlobal(id, SymbolVisualType.Hit, 0, SymbolVisualType.Etc2, 0);
            }

            foreach (string id in extraSymbolIds)
            {
                Symbol.AddSubstituteVisualAtGlobal(id, SymbolVisualType.Hit, 0, SymbolVisualType.Etc2, 0);
            }

            if (resetToIdle)
            {
                SetAllSymbolVisualIdle();
            }
        }

        // 심볼 히트 스테이트
        protected override IEnumerator HitState()
        {
            // Slot Animator - Hit 애니메이션은 빅윈 이상부터
            if (Info.SpinResult.IsBigWin())
            {
                OnReelStateHit(true);
            }
            else
            {
                extraReelAnimator.SetBool(REEL_APPEAR_TRIGGER, false);
                extraReelAnimator.SetBool(REEL_LONG_SPIN_TRIGGER, false);
            }

            WinGrade winGrade = Info.SpinResult.WinGrade;

            // 머신라이트 On
            machineLightAnimator.SetInteger(MACHINE_LIGHT_WINGRADE, (int)winGrade);

            if (CurrentReelMode == ReelMode.Free)
            {
                flexibleAnimator.SetBool(FREE_MULTI_TRIGGER, true);

                long value = Info.GetExtraValue(EXTRA_INDEX_MULTIPLE_ID);
                slotUI.OnFreeMultiple(value);

                if (winGrade == WinGrade.Normal || winGrade == WinGrade.Mini)
                {
                    freeWinNormalSound.Play();
                }
                else
                {
                    freeWinBigSound.Play();
                }
            }

            StartCoroutine(FreespinIntroSiren());
            yield return WaitForSeconds(hitDelayTime);

            ShowDimPanel();

            if (IsJackpot(Info.SpinResult, enabledJackpotGrade) == true)
            {
                NextState(SlotMachineState.HIT_JACKPOT);
            }
            else
            {
                if (winGrade == WinGrade.Normal || winGrade == WinGrade.Mini)
                {
                    NextState(SlotMachineState.HIT_WIN);
                }
                else
                {
                    NextState(SlotMachineState.HIT_BIGWIN);
                }
            }
        }


        public IEnumerator FreespinIntroSiren()
        {
            yield return WaitForSeconds(freeSirenDelayTime);
            // 직전 스핀에서 히트가 있는 경우에만 연출한다.
            if (Info.IsFreespinStart)
            {
                if (Info.SpinResult.WinCoinsHit > 0)
                {
                    // (추가) 팟 연출 Compltet
                    machineLightAnimator.SetInteger(MACHINE_LIGHT_WINGRADE, 0);
                    machineLightAnimator.SetTrigger(MACHINE_LIGHT_COMPLETE);
                    potAnimator.SetTrigger(POT_COMPLETE);

                    sirenSound.Play();
                }
            }
        }

        private void OnPaylineDrawAllEvent()
        {
            ShowDimPanel();
            var symbol = extraReel.GetMainSymbol(EXTRA_REEL_MAINSYMBOL_INEX);
            if (symbol.id != emptySymbolID)
            {
                symbol.SetVisual(SymbolVisualType.Hit);
            }
        }

        private void OnPaylineDrawEvent(SpinResult.WinInfo winInfo)
        {
            ShowDimPanel();
            var symbol = extraReel.GetMainSymbol(EXTRA_REEL_MAINSYMBOL_INEX);
            if (symbol.id != emptySymbolID)
            {
                symbol.SetVisual(SymbolVisualType.Hit);
            }
        }


        // 일반 히트 스테이트
        protected override IEnumerator HitNormalState()
        {
            yield return base.HitNormalState();
        }

        // 빅윈 히트 스테이트
        protected override IEnumerator HitBigwinState()
        {

            yield return base.HitBigwinState();
        }

        // 잭팟 히트 스테이트
        protected override IEnumerator HitJackpotState()
        {
            yield return base.HitJackpotState();
        }

        // 스캐터 히트 스테이트
        protected override IEnumerator ScatterState()
        {
            yield return WaitForSeconds(scatterHitDelayTime);
            NextState(SlotMachineState.FREESPIN_START);
        }

        // 프리스핀을 시작하는 스테이트
        // 프리스핀 팝업을 보여줌
        protected override IEnumerator FreespinStartState()
        {
            isFreespinEnd = false;

            normalBGM.Stop();
            
            HideDimPanel();

            // 줌 인
            OnZoomIn();

            slotUI.SetTotalRespinCount(Info.FreespinTotalCount, Info.FreespinCurrentCount);
            slotAnimator.SetBool(HIT_TRIGGER, false);

            // 프리스핀 진입 연출
            freeIntro_potExplo_Sound.Play();
            potAnimator.SetBool(POT_FREESPIN, true);
            yield return new WaitForSeconds(potMoveBeforeDelayTime);
            potCoinTransform.DOLocalMoveY(POT_POS[POT_POS.Length - 1], potCompleteTime);
            
            StartCoroutine(FreeSpinSoundPlay());

            machineLightAnimator.SetTrigger(MACHINE_LIGHT_FREESPIN);

            // 프리스핀 전환 연출
            flexibleAnimator.ResetTrigger(RESULT_TO_NORMAL_TRIGGER);
            if (slotUI.IsLock() == true)
            {
                flexibleAnimator.SetTrigger(NORMAL_TO_FREE_LOCK_TRIGGER);
            }
            else
            {
                flexibleAnimator.SetTrigger(NORMAL_TO_FREE_TRIGGER);
            }

            yield return new WaitForSeconds(normalToFreeTime);

            // 에픽게이지 연출
            int epicClearCount = (int)Info.GetExtraValue(EXTRA_INDEX_EPIC_CLEAR_COUNT);
            int epicStepIndex = (int)Info.GetExtraValue(EXTRA_INDEX_EPIC_STEP);

            // 줌 아웃
            OnZoomOut();

            yield return megaBonus.UpdateStep(epicClearCount, epicStepIndex);

            int index = slotUI.GetTitleIndex(Info.FreespinTotalCount);
            freeIntro_title_Sound[index].Play();

            yield return new WaitForSeconds(titleIntroDelayTime);

            // FreeSpin override
            DispatchGameUiRaycast(false);
            if (saveSymbolsBeforeFreespins == true)
            {
                SaveSymbols();
            }

            DispatchWinCoins(0, 0.0f, WinGrade.None);

            SetReelMode(ReelMode.Free);
            DispatchChangeFreespins(Info.FreespinCurrentCount, Info.FreespinTotalCount);

            StopRepeatWinningLines();
            paylinesHelper.Clear();
            SetAllSymbolVisualIdle();

            // 프리스핀 타이틀 화면 끝날때까지 딜레이
            yield return new WaitForSeconds(normalToFreeEndTime);

            // 메가보너스 이상일 때만 심볼 삭제 연출
            if (Info.FreespinTotalCount > 8)
            {
                yield return EventReelControl();
            }

            FreeSpinSymbolVisualChange();
            NextState(SlotMachineState.RESULT_END);
            DispatchGameUiRaycast(true);

            yield return WaitForCheckAutoPlayForFeatureStart();
        }

        private IEnumerator FreeSpinSoundPlay()
        {
            yield return WaitForSeconds(3.0f);
            freeBGM.Play();
        }

        // 프리스핀 종료 스테이트
        // 프리스핀 종료 팝업을 보여줌
        protected override IEnumerator FreespinEndState()
        {
            sirenSound.Play();
            StartCoroutine(slotUI.OnFreeResult(Info.TotalWinCoins));

            flexibleAnimator.ResetTrigger(NORMAL_TO_FREE_LOCK_TRIGGER);
            flexibleAnimator.ResetTrigger(NORMAL_TO_FREE_TRIGGER);
            // 프리스핀 종료 -> 결과 팝업
            flexibleAnimator.SetBool(FREE_MULTI_TRIGGER, false);
            flexibleAnimator.SetTrigger(FREE_TO_RESULT_TRIGGER);

            StopRepeatWinningLines();
            paylinesHelper.Clear();

            Symbol.RemoveAllSubstituteVisualsAtGlobal();
            SetAllSymbolVisualIdle();
            ShowDimPanel();
            OnReelStateHit(false);
            potCoinTransform.DOLocalMoveY(POT_POS_DEFAULT, 0.0f);

            yield return WaitForSeconds(freeToResultTime);

            // 결과 팝업 -> 공용 팝업
            yield return base.FreespinEndState();

            slotUI.OffFreeMultiple();

            // 공용 팝업 종료 -> 노말로 트랜지션
            flexibleAnimator.ResetTrigger(FREE_TO_RESULT_TRIGGER);
            flexibleAnimator.SetTrigger(RESULT_TO_NORMAL_TRIGGER);
            machineLightAnimator.SetInteger(MACHINE_LIGHT_WINGRADE, 0);
            SetPotStepBG();
            HideDimPanel();

            // 메가보너스가 Lock 상태일 때 연출을 하지 않음
            while (machineTopObject.activeInHierarchy == false)
            {
                yield return null;
            }

            megaBonus.RestoreStep();
            freeEndFireEffectSound.Play();

            yield return PlayResetCoins();

            int epicClearCount = (int)Info.GetExtraValue(EXTRA_INDEX_EPIC_CLEAR_COUNT);
            int epicStepIndex = (int)Info.GetExtraValue(EXTRA_INDEX_EPIC_STEP);

            slotUI.RestoreLockFreespinEnd();
            yield return megaBonus.UpgradeStep(epicClearCount, epicStepIndex);

            slotUI.OffFreeResult();
        }

        private IEnumerator PlayResetCoins()
        {
            potAnimator.SetTrigger(POT_RESET);
            potResetSound.Play();
            potCoinTransform.DOLocalMoveY(POT_POS[0], potResetTime);

            yield return WaitForSeconds(potResetTime);

            isFreespinEnd = true;
        }

        private void SetAllSymbolVisualIdle()
        {
            reelGroupHelper.SetVisualAllSymbols(SymbolVisualType.Idle);
            extraReel.SetVisualAllMainSymbols(SymbolVisualType.Idle);
        }
        
        private void ChangeReelModeEnv()
        {
            if (CurrentReelMode == ReelMode.Regular)
            {
                normalBGM.Play();
            }
            else if (CurrentReelMode == ReelMode.Free)
            {
                freeBGM.Play();
                flexibleAnimator.SetTrigger(FORCE_FREESPIN_TRIGGER);
                slotUI.SetTotalRespinCount(Info.FreespinTotalCount, Info.FreespinCurrentCount);
            }
        }

        private void SetPotStepBG()
        {
            int potStepBGInex = megaBonus.GetNexStep();

            for (int i = 0; i < potStepBG.Length; i++)
            {
                if (i == potStepBGInex)
                {
                    potStepBG[i].SetActive(true);
                }
                else
                {
                    potStepBG[i].SetActive(false);
                }
            }
        }

        private void SetupPotState()
        {
            if (Info.GetExtrasCount() > EXTRA_INDEX_POT_STATE)
            {
                currentPotIndex = (int)Info.GetExtraValue(EXTRA_INDEX_POT_STATE);
                potCoinTransform.DOLocalMoveY(POT_POS[currentPotIndex], potCoinMoveTime);
            }
        }

        private void OnReelStateHit(bool enable)
        {
            slotAnimator.SetBool(HIT_TRIGGER, enable);

            for (int i = 0; i < reelAnimator.Length; i++)
            {
                reelAnimator[i].SetBool(HIT_TRIGGER, enable);
            }
            
            if (GetMultipleIndex() != -1)
            {
                extraReelAnimator.SetBool(HIT_TRIGGER, enable);
            }
        }

        public void TestBtnEventRrel()
        {
            StartCoroutine(EventReelControl());
        }

        // 메가 / 에픽 보너스 진입 연출 기능 테스트
        private IEnumerator EventReelControl()
        {
            VerticalReel1045 extraVerticalReel = extraReel.GetComponent<VerticalReel1045>();

            List<string> reelStrips = new List<string>();
            int maxCount = 0;

            ReelStrip reelStrip = extraVerticalReel.ReelStrip;
            for (int reelStripIndex = 0; reelStripIndex < reelStrip.Length; reelStripIndex++)
            {
                string symbolID = reelStrip.GetID(reelStripIndex);
                reelStrips.Add(symbolID);

                if (IsValidEventReelSymbol(symbolID) == true)
                {
                    maxCount++;
                }
            }



            bool OnMoveSound = false;
            int deleteCount = 0;

            while (IsContinueEventMoveReel(ref reelStrips))
            {
                int index = reelStrip.GetValidIndex(reelStrip.CurrentIndex + 1);

                var currentSymbol = extraVerticalReel.GetMainSymbol(1);

                if (IsValidEventReelSymbol(currentSymbol.id) == false)
                {
                    if (OnMoveSound == false)
                    {
                        OnMoveSound = true;
                    }
                    yield return extraVerticalReel.ReelNext();
                }
                else
                {
                    OnMoveSound = false;
                    freeExtraDelSound.Play();
                    yield return extraVerticalReel.MoveSymbolsAt(1);
                    yield return WaitForSeconds(deleteSymbolWaitingTime);

                    if (reelStrips.Count > index)
                    {
                        reelStrips[index] = "0";
                    }
                    deleteCount++;
                }



                if (deleteCount >= maxCount)
                {
                    break;
                }

            }

            yield return WaitForSeconds(deleteEventEndTime);

            // 새로 생성된 릴스트립의 삭제되야할 심볼들 정리
            ChangeExistDeleteSymbol();
        }

        private void ChangeExistDeleteSymbol()
        {
            for (int i = 0; i < extraReel.AllSymbolsLength; i++)
            {
                var symbol = extraReel.GetSymbol(i);
                if (IsValidEventReelSymbol(symbol.id) == true)
                {
                    int randomValue = Random.Range(12, 17);
                    extraReel.ChangeSymbol(i, randomValue.ToString());
                }
            }
        }

        private bool IsContinueEventMoveReel(ref List<string> reelStrips)
        {
            if (Info.FreespinTotalCount == 12)
            {
                if (string.IsNullOrEmpty(reelStrips.Find(x => x == rowSymbolID_1)) == false)
                {
                    return true;
                }
            }
            else if (Info.FreespinTotalCount == 20)
            {
                if (string.IsNullOrEmpty(reelStrips.Find(x => x == rowSymbolID_1)) == false)
                {
                    return true;
                }
                else if (string.IsNullOrEmpty(reelStrips.Find(x => x == rowSymbolID_2)) == false)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsValidEventReelSymbol(string symbolID)
        {
            if (Info.FreespinTotalCount == 12)
            {
                if (symbolID == rowSymbolID_1)
                {
                    return true;
                }
            }
            else if (Info.FreespinTotalCount == 20)
            {
                if (symbolID == rowSymbolID_1 || symbolID == rowSymbolID_2)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnZoomIn()
        {
            for (int i = 0; i < frameRoots.Length; i++)
            {
                frameRoots[i].DOScale(zoomInValue, zoomInTime).SetEase(zoomInEase);
            }
        }

        private void OnZoomOut()
        {
            for (int i = 0; i < frameRoots.Length; i++)
            {
                frameRoots[i].DOScale(zoomOutValue, zoomOutTime).SetEase(zoomOutEase);
                if (frameRoots[i].GetComponentInParent<Canvas>() != null)
                {
                    frameRoots[i].DOLocalMove(Vector3.zero, zoomOutTime).SetEase(zoomOutEase);
                }
            }
        }

        public void OnUnlockButtonClicked(int unlockBetIndex)
        {
            if (ReelGroup.IsBusy)
            {
                return;
            }

            var totalBet = Info.GetTotalBet(unlockBetIndex);

            DispatchTotalBetRequest(totalBet);
        }

        private int GetMultipleIndex()
        {
            // 5배 이하 = 0
            // 10배, 25배 = 1
            // 50배 = 2
            // Empty = -1
            long multiSymbolID = Info.GetExtraValue(EXTRA_INDEX_MULTIPLE_ID);

            if (multiSymbolID >= 10 && multiSymbolID <= 13)
            {
                return 0;
            }
            else if (multiSymbolID <= 15)
            {
                return 1;
            }
            else if (multiSymbolID == 16)
            {
                return 2;
            }

            return -1;
        }

        private void SetNudgeWeights()
        {
            int totalWeight = 100;
            int rateNudgeNone = totalWeight - rateNudgeDown - rateNudgeUp;

            weights[0] = rateNudgeDown;
            weights[1] = rateNudgeNone;
            weights[2] = rateNudgeUp;
        }

        private void SetNudgeCount()
        {
            if (CurrentReelMode != ReelMode.Free)
            {
                return;
            }

            if (Info.SpinResult.WinCoinsHit == 0)
            {
                return;
            }

            int randomIndex = GetRandomValue();

            int nudgeCount = nudgeVector[randomIndex];

            extraReel.SetNudgeCount(nudgeCount);
        }

        // 확률 가중치에 따른 랜덤값
        private int GetRandomValue()
        {
            int total = 0;
            foreach (int weight in weights)
            {
                total += weight;
            }

            var random = Random.Range(0.0f, total);

            for (int i = 0; i < weights.Length; i++)
            {
                if (random < weights[i])
                {
                    return i;
                }
                else
                {
                    random -= weights[i];
                }
            }

            return weights.Length - 1;
        }

    }
}