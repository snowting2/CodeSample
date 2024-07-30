using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SlotGame.Popup;
using SlotGame.Sound;
using SlotGame.Attribute;
using DG.Tweening;

namespace SlotGame.Machine.S1055
{
    public class SlotMachine1055 : SlotMachine
    {
#pragma warning disable 0649
        [Separator("Custom")]
        [SerializeField] private ReelGroup extraReelGroup;
        [SerializeField] private TopDollarFeature1055 topDollarFeature;
        [SerializeField] private Animator machineAnim;
        [SerializeField, Range(0, 1)] private float longSpinRate = 0.6f;

        [Separator("Time")]
        [SerializeField, Tooltip("탑달러 진입 전 대기 시간")] private float topDollarStartInterval;
        [SerializeField, Tooltip("탑달러 종료 전 대기 시간")] private float topDollarEndInterval;
        [SerializeField, Tooltip("SlotIntro 대기 시간")] private float introDelayTime;
        [SerializeField, Tooltip("탑달러 종료 후 대기 시간")] private float topDollarEndDelayTime = 1.0f;

        [Separator("Sounds")]
        [SerializeField] private SoundPlayer bgmRegular;
        [SerializeField] private SoundPlayer bgmOffer;

        [SerializeField] private SoundPlayer extraReelEffectSound;
        [SerializeField] private SoundPlayer jackpotHitSound;
        [SerializeField] private SoundPlayer scatterHitSound;
        [SerializeField] private SoundPlayer scatterAppearSound;

        [SerializeField] private SoundPlayer extraHitSound_10;
        [SerializeField] private SoundPlayer extraHitSound_25;
        [SerializeField] private SoundPlayer extraHitSound_50;

        [SerializeField] private SoundPlayer[] bonusIntroSound;
        [SerializeField] private SoundPlayer slotIntroSound;

#pragma warning restore 0649

        private readonly int EXTRA_REEL_INDEX = 0;
        private readonly int EXTRA_MAIN_SYMBOL_INDEX = 1;
        private readonly int EXTRA_MAINSYMBOL_COUNT = 3;
        private readonly int EXTRA_COUNT_TOP_DOLLAR = 4;
        // 탑달라 획득 총 금액
        private readonly int EXTRA_INDEX_TOP_WINCOINS = 19;

        private readonly string FEATURE_TOP_DOLLAR_START = "TopDollarFeatureStart";
        private readonly string FEATURE_TOP_DOLLAR_END = "TopDollarFeatureEnd";

        private const string SPECIAL_REQUEST_TAKE = "take";
        private const string SPECIAL_REQUEST_TRY = "try";

        private readonly string CUSTOM_POPUP_JACKPOT = "JackpotPopup1055";

        // Machine Animator Trigger
        private readonly string TRIGGER_IDLE = "Idle";
        private readonly string TRIGGER_SPIN = "Spin";
        private readonly string TRIGGER_HIT = "Hit";
        private readonly string TRIGGER_JACKPOT = "Jackpot";
        private readonly string TRIGGER_LONG_EXTRA = "LongSpin_Extra";
        private readonly string TRIGGER_BONUS = "Bonus";
        private readonly string TRIGGER_BONUS_RESULT = "BonusResult";
        private readonly string TRIGGER_SLOT_INTRO = "SlotIntro";
        private readonly string TRIGGER_SCATTER = "Scatter";

        // Symbol Visual Settings
        private readonly SymbolVisualType VISUAL_TYPE_WAIT = SymbolVisualType.Etc1;
        private readonly SymbolVisualType VISUAL_TYPE_APPEAR = SymbolVisualType.Etc2;

        // Symbol ID
        private readonly List<string> EXTRA_SYMBOL_ID_UNDER_10 = new List<string>() { "10", "11", "12", "13", "14" };

        private readonly string EXTRA_SYMBOL_ID_25 = "15";
        private readonly string EXTRA_SYMBOL_ID_50 = "16";

        private readonly string SYMBOL_ID_BLANK = "9";

        private BaseReel extraReel = null;
        private List<ReelStopInfo> reelStopInfos = new List<ReelStopInfo>();
        private List<string> extraIDList = new List<string>();
        private long topDollarWinCoins = 0;

        private ReelGroupHelper extraReelGroupHelper = null;

        private float timeOutSec = 5.0f;
        private Coroutine timeOutCoroutine = null;
        protected override void SetupPopups()
        {
            base.SetupPopups();

            PopupSystem.Instance.AddCachedPopup<JackpotPopup>(slotAssets, CUSTOM_POPUP_JACKPOT);
        }

        protected override void RemovePopups()
        {
            base.RemovePopups();

            PopupSystem.Instance.RemoveCachedPopup(CUSTOM_POPUP_JACKPOT);
        }

        protected override void UpdateEnableReelGroupList()
        {
            base.UpdateEnableReelGroupList();
            enableReelGroupList.Add(extraReelGroup);
        }

        // 슬롯머신 진입시 최초 한번 실행되어야 할 스테이트
        // 이 부분에 슬롯머신 진입 애니메이션 등을 구현해야함.
        protected override IEnumerator EnterState()
        {
            extraReelGroupHelper = new ReelGroupHelper(extraReelGroup);

            extraReelGroup.PrecedingReelGroup = ReelGroup;

            extraReel = extraReelGroup.GetReel(EXTRA_REEL_INDEX);

            ReelGroup.OnStateEnd.AddListener(OnReelStateEnd);
            extraReelGroup.OnStateEnd.AddListener(OnExtraReelStateEnd);

            // Special Request & Response
            topDollarFeature.OnRequestTakeOffer.AddListener(OnRequestTakeOffer);
            topDollarFeature.OnRequestTryAgain.AddListener(OnRequestTryAgain);

            OnSpecialResponse.AddListener(OnResponseTryAgain);

            paylinesHelper.OnPaylineDrawAllEvent.AddListener(OnPaylineDrawAllEvent);
            paylinesHelper.OnPaylineDrawEvent.AddListener(OnPaylineDrawEvent);

            // 피쳐 진입 상태이다
            if (IsTopDollarFeature() == true)
            {
                bgmOffer.Play();
                NextState(FEATURE_TOP_DOLLAR_START);
            }
            else
            {
                bgmRegular.Play();

                DispatchEnableSpinButton(false);
                slotIntroSound.Play();
                machineAnim.SetTrigger(TRIGGER_SLOT_INTRO);
                topDollarFeature.OnTriggerSlotIntro();

                yield return WaitForSeconds(introDelayTime);
            }

            yield return base.EnterState();
        }

        protected override void SetMachineGroupForSpin(PlayMode playMode)
        {
            base.SetMachineGroupForSpin(playMode);

            if (playMode == PlayMode.Normal)
            {
                reelGroup.interpolateBetweenSpinStartAndStopTime = true;
                extraReelGroup.interpolateBetweenSpinStartAndStopTime = true;
                extraReelGroup.intervalType = ReelGroup.IntervalType.Default;
                extraReelGroup.forceUseIntervalWhenStopSpinOfFirstReel = false;
                extraReel.SetTimeSpeedRate(DefaultReelTimeRate);
                extraReel.skipSpin = false;

            }
            else if (playMode == PlayMode.Fast)
            {
                reelGroup.interpolateBetweenSpinStartAndStopTime = false;
                extraReelGroup.interpolateBetweenSpinStartAndStopTime = false;
                extraReelGroup.ignoreInterval = true;
                extraReelGroup.forceUseIntervalWhenStopSpinOfFirstReel = true;
                extraReelGroup.intervalType = ReelGroup.IntervalType.Extra6;
                extraReel.SetTimeSpeedRate(fastAutoSpinReelTimeRate);
                extraReel.keepSpinning = false;
                extraReel.skipSpin = false;
            }
            else if (playMode == PlayMode.Turbo)
            {
                reelGroup.interpolateBetweenSpinStartAndStopTime = false;
                extraReelGroup.interpolateBetweenSpinStartAndStopTime = false;
                extraReelGroup.ignoreInterval = true;
                extraReelGroup.forceUseIntervalWhenStopSpinOfFirstReel = false;
                extraReelGroup.intervalType = ReelGroup.IntervalType.Extra6;
                extraReel.SetTimeSpeedRate(turboAutoSpinReelTimeRate);
                extraReel.keepSpinning = false;
                extraReel.skipSpin = true;
                extraReel.expirationTimeOfSkipSpin = PLAYMODE_TURBO_SKIP_SPIN_EXPIRATIONTIME;
            }
        }

        // Reel End
        private void OnReelStateEnd(int reelIndex, ReelState state, BaseReel reel)
        {
            if (state == ReelState.Slowdown)
            {
                if (reelIndex == 2)
                {
                    if (extraReelGroup.intervalType != ReelGroup.IntervalType.Default
                       && extraReelGroup.intervalType != ReelGroup.IntervalType.Extra6)
                    {
                        extraReelEffectSound.Play();
                        machineAnim.SetTrigger(TRIGGER_LONG_EXTRA);
                        SetVisualInExtraLongSpin();
                    }
                }
            }
        }

        private void OnExtraReelStateEnd(int reelIndex, ReelState state, BaseReel reel)
        {
            if (state == ReelState.Slowdown)
            {
                extraReelEffectSound.Stop();

                var extraReel = extraReelGroup.GetReel(EXTRA_REEL_INDEX);
                var extraSymbol = extraReel.GetMainSymbol(EXTRA_MAIN_SYMBOL_INDEX);

                if (extraSymbol.IsBlank == false)
                {
                    if (extraSymbol.IsScatter == true)
                    {
                        extraSymbol.SetVisual(VISUAL_TYPE_APPEAR);
                    }
                    else if (Info.SpinResult.WinCoinsHit > 0)
                    {
                        extraSymbol.SetVisual(VISUAL_TYPE_APPEAR);
                    }
                }

                if (extraSymbol.IsScatter == true)
                {
                    extraSymbol.SetVisual(VISUAL_TYPE_APPEAR);
                    scatterAppearSound.Play();

                    topDollarFeature.SetInfoUIScatterHit();
                }

                if (Info.SpinResult.GetWinInfoCountOfAllMachines() > 0)
                {
                    // 엑스트라 10, 25, 50 배수 심볼 등장 사운드
                    if (EXTRA_SYMBOL_ID_UNDER_10.Contains(extraSymbol.id))
                    {
                        extraHitSound_10.Play();
                    }
                    else if (extraSymbol.id == EXTRA_SYMBOL_ID_25)
                    {
                        extraHitSound_25.Play();
                    }
                    else if (extraSymbol.id == EXTRA_SYMBOL_ID_50)
                    {
                        extraHitSound_50.Play();
                    }
                }
            }
        }

        private void OnPaylineDrawAllEvent()
        {
            ShowDimPanel();
            var symbol = extraReel.GetMainSymbol(EXTRA_MAIN_SYMBOL_INDEX);
            if (symbol.IsBlank == false && symbol.IsScatter == false)
            {
                symbol.SetVisual(SymbolVisualType.Hit);
            }
        }

        private void OnPaylineDrawEvent(SpinResult.WinInfo winInfo)
        {
            ShowDimPanel();
            var symbol = extraReel.GetMainSymbol(EXTRA_MAIN_SYMBOL_INDEX);
            if (symbol.IsBlank == false && symbol.IsScatter == false)
            {
                symbol.SetVisual(SymbolVisualType.Hit);
            }
        }


        // 기본 상태.
        // 기본적으로 유저의 입력을 기다리지만 프리스핀, 리스핀, 오토스핀등의 상황에선 자동으로 스핀 상태로 변경.
        protected override IEnumerator IdleState()
        {
            extraReelGroup.intervalType = ReelGroup.IntervalType.Default;
            machineAnim.SetTrigger(TRIGGER_IDLE);

            yield return base.IdleState();
        }

        // 스핀을 시작하는 스테이트.
        protected override IEnumerator SpinStartState()
        {
            // extraReelGroupHelper.SetTimeRateAll(DefaultReelTimeRate);

            machineAnim.SetTrigger(TRIGGER_SPIN);
            extraReelGroup.StartSpin();

            yield return base.SpinStartState();

            if (CurrentPlayModeInProgress != PlayMode.Turbo)
            {
                SetExtraReelStopInterval();
            }
        }

        private void SetExtraReelStopInterval()
        {
            // 엑스트라 릴 고조연출 조건
            // 1. 엑스트라 릴 배수 심볼 당첨
            // 2. 스캐터 당첨
            // 3. LongSpinRate 확률에 따름

            long winCoinTotal = Info.SpinResult.WinCoinsTotal;

            string symbolID = Info.GetExtraValue(EXTRA_MAIN_SYMBOL_INDEX).ToString();

            if (winCoinTotal > 0)
            {
                extraReelGroup.intervalType = ReelGroup.IntervalType.Extra2;

                if (symbolID != SYMBOL_ID_BLANK)
                {
                    if (CalculateZoomRate() == true)
                    {
                        extraReelGroup.intervalType = ReelGroup.IntervalType.Extra1;
                    }
                }
            }
        }

        private bool CalculateZoomRate()
        {
            float rate = Random.Range(0.0f, 1.0f);

            if (rate <= longSpinRate)
            {
                return true;
            }

            return false;
        }

        private void SetVisualInExtraLongSpin()
        {
            for (int infoIndex = 0; infoIndex < Info.SpinResult.GetWinInfoCountOfAllMachines(); infoIndex++)
            {
                var winInfo = Info.SpinResult.GetWinInfoFromAllMachines(infoIndex);

                for (int reelIndex = 0; reelIndex < winInfo.hitSymbolPositions.Count; reelIndex++)
                {
                    var symbolList = winInfo.hitSymbolPositions[reelIndex];

                    for (int index = 0; index < symbolList.Count; index++)
                    {
                        int mainSymbolIndex = symbolList[index];

                        var symbol = reelGroup.GetReel(reelIndex).GetMainSymbol(mainSymbolIndex);
                        symbol.SetVisual(VISUAL_TYPE_WAIT);
                    }
                }
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

        protected override void ForceStop()
        {
            base.ForceStop();

            extraReel.SetTimeSpeedRate(ForceStopReelTimeRate);
        }
        
        // 스핀을 멈추는 스테이트.
        // 스핀 정지 명령을 내리고 실제 스핀이 멈추는 순간까지 유지된다.
        protected override IEnumerator SpinStopState()
        {
            if (CurrentPlayModeInProgress == PlayMode.Turbo)
            {
                HideDimPanel();
                StopRepeatWinningLines();
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

        private void ExtraSpinEndState()
        {
            reelStopInfos.Clear();
            extraIDList.Clear();

            for (int i = 0; i < EXTRA_MAINSYMBOL_COUNT; i++)
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

        // 스핀이 완전히 종료되었을때 호출 되는 스테이트
        // 스핀 종료 후 특별한 연출등을 표현 (ex : 스택드 와일드 합체 등등)
        protected override IEnumerator SpinEndState()
        {
            if (Info.TotalWinCoins == 0)
            {
                reelGroupHelper.SetVisualAllSymbols(SymbolVisualType.Idle);
            }

            yield return base.SpinEndState();
        }
        
        protected override string GetStateBeforeEnd(bool isExtraSpin, bool isFreespinStart, bool isFreespinEnd, bool isRespin, bool isFreeChoiceStart)
        {
            if (IsTopDollarFeature())
            {
                return FEATURE_TOP_DOLLAR_START;
            }

            return base.GetStateBeforeEnd(isExtraSpin, isFreespinStart, isFreespinEnd, isRespin, isFreeChoiceStart);
        }

        private bool IsTopDollarFeature()
        {
            bool result = false;
            if (Info.GetExtrasCount() > EXTRA_COUNT_TOP_DOLLAR)
            {
                for (int i = 0; i < Info.GetExtrasCount(); i++)
                {
                    if (Info.GetExtraValue(i) != 0)
                    {
                        result = true;
                        break;
                    }
                }
            }

            return result;
        }

        private IEnumerator EarnCoinBeforeTopDollarFeature()
        {
            // 시작전 코인 획득
            if (Info.IsStackedTotalWin == false)
            {
                EarnCoins(Info.TotalWinCoins);
                EarnPearls(Info.TotalWinPearls);
                EarnTickets(Info.TotalWinTickets);
            }

            yield return WaitForSeconds(GetResultEndDelay());
        }

        private IEnumerator TopDollarFeatureStart()
        {
            if (Info.TotalWinCoins > 0)
            {
                yield return EarnCoinBeforeTopDollarFeature();
            }

            MachineUI.GetSpinButton().onClick.AddListener(DisableSpinButton);

            DispatchWinCoins(0, 0.0f, WinGrade.None);
            DispatchTotalWinCoins(0, 0.0f);
            StopRepeatWinningLines();

            paylinesHelper.Clear();
            reelGroupHelper.SetVisualAllSymbols(SymbolVisualType.Idle);
            extraReelGroupHelper.SetVisualAllSymbols(SymbolVisualType.Idle);

            Debug.Log("TopDollarFeatureStart");

            skipResult = false;
            DispatchEnableSpinButton(false);

            topDollarWinCoins = 0;

            yield return WaitForSeconds(scatterHitDelayTime);

            // 스캐터 심볼 연출
            var symbol = extraReel.GetMainSymbol(EXTRA_MAIN_SYMBOL_INDEX);
            if (symbol.IsScatter)
            {
                scatterHitSound.Play();
                symbol.SetVisual(SymbolVisualType.Hit);
                machineAnim.SetTrigger(TRIGGER_SCATTER);

                yield return WaitForSeconds(scatterHitAnimationTime);
            }

            yield return WaitForCheckAutoPlayForFeatureStart();

            symbol.SetVisual(SymbolVisualType.Idle);

            machineAnim.SetTrigger(TRIGGER_BONUS);

            int randIndex = Random.Range(0, bonusIntroSound.Length);
            bonusIntroSound[randIndex].Play();

            bgmRegular.Stop();
            if (bgmOffer.IsInPlay() == false)
            {
                bgmOffer.Play();
            }

            MachineUI.GetSpinButton().onClick.RemoveAllListeners();

            topDollarFeature.Initialize(Info, holdType == HoldType.Off);

            yield return new WaitUntil(() => topDollarFeature.ResultEnd);

            NextState(FEATURE_TOP_DOLLAR_END);
        }

        private void DisableSpinButton()
        {
            bool spinBtnState = MachineUI.GetSpinButtonInteractable();
            if (spinBtnState == true)
            {
                MachineUI.GetSpinButton().interactable = false;
            }
        }

        private IEnumerator TopDollarFeatureEnd()
        {
            Debug.Log("TopDollarFeatureEnd");

            skipResult = false;
            DispatchEnableSpinButton(false);

            yield return WaitForSeconds(topDollarEndInterval);

            var winGrade = SlotMachineUtils.GetWinGrade(topDollarWinCoins, Info.CurrentTotalBet);
            if (winGrade == WinGrade.Normal || winGrade == WinGrade.Mini)
            {
                yield return HitNormal(winGrade, topDollarWinCoins, topDollarWinCoins, true);
            }
            else
            {
                yield return WaitForSeconds(bigwinDelayTime);
                yield return WaitForBigwinSignalEffect();
                yield return HitBigwin(winGrade, topDollarWinCoins, topDollarWinCoins);
            }

            EarnCoins(topDollarWinCoins);

            machineAnim.SetTrigger(TRIGGER_BONUS_RESULT);
            topDollarFeature.RestoreCreditValue();

            yield return WaitForSeconds(GetResultEndDelay());

            if (updateNextReelType == UpdateNextReelType.ResultEnd)
            {
                Info.UpdateNextReels();
            }

            if (Info.IsReelChanged)
            {
                DispatchReelStripChanged();
            }

            bgmRegular.Play();
            bgmOffer.Stop();

            yield return WaitForSeconds(topDollarEndDelayTime);

            NextState(SlotMachineState.IDLE);
        }

        // 심볼 히트 스테이트
        protected override IEnumerator HitState()
        {
            machineAnim.SetTrigger(TRIGGER_HIT);
            yield return base.HitState();
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
            jackpotHitSound.Play();
            topDollarFeature.JackpotHit(true);
            machineAnim.SetTrigger(TRIGGER_JACKPOT);

            long winCoins = Info.SpinResult.WinCoinsJackpot;

            yield return base.HitJackpotState();

            topDollarFeature.JackpotHit(false);
        }

        protected override IEnumerator HitJackpot(int jackpotGrade, long winCoins, long totalWinCoins, bool dispatchWinCoins, bool hitSymbols = true)
        {
            if (hitSymbols == true)
            {
                StartCoroutine(HitSymbols(hitAnimationTime));
            }

            yield return WaitForSeconds(jackpotHitDelayTime);

            yield return PopupSystem.Instance.Open<JackpotPopup>(slotAssets, CUSTOM_POPUP_JACKPOT)
                                          .OnInitialize(p =>
                                          {
                                              p.Initialize(winCoins, true, null, useVibrate, IsWinPopupSkip());
                                          })
                                          .Cache()
                                          .WaitForClose();


            DispatchJackpotClosed(winCoins, jackpotGrade);

            if (dispatchWinCoins)
            {
                DispatchWinCoins(winCoins, winCoinDuration, WinGrade.None);
                DispatchTotalWinCoins(totalWinCoins, winCoinDuration);

                yield return WaitForSeconds(winCoinDuration);

                DispatchWinCoins(winCoins, 0.0f, WinGrade.None);
                DispatchTotalWinCoins(totalWinCoins, 0.0f);
            }
        }

        public void OnRequestTakeOffer()
        {
            DispatchSpecialRequest(SPECIAL_REQUEST_TAKE);

            timeOutCoroutine = StartCoroutine(OnResponseWait());
        }

        public void OnRequestTryAgain()
        {
            DispatchSpecialRequest(SPECIAL_REQUEST_TRY);

            timeOutCoroutine = StartCoroutine(OnResponseWait());
        }

        public void OnResponseTryAgain(SpecialData data)
        {
            Debug.Log("OnSpecialResponseTry");
            if (timeOutCoroutine != null)
            {
                StopCoroutine(timeOutCoroutine);
                timeOutCoroutine = null;
            }

            if (data.act == SPECIAL_REQUEST_TAKE)
            {
                topDollarFeature.OnResult();
                topDollarWinCoins = data.extras[EXTRA_INDEX_TOP_WINCOINS];

                if (Info.SpinResult.GetData() != null)
                {
                    Info.SpinResult.GetData().coin += topDollarWinCoins;
                }

            }
            else if (data.act == SPECIAL_REQUEST_TRY)
            {
                topDollarFeature.OnUpdateInfo(Info, true);
            }
        }

        // Timeout 발생시 방어코드
        private IEnumerator OnResponseWait()
        {
            yield return null;

            yield return WaitForSeconds(timeOutSec);

            topDollarFeature.OnTimeoutUpdate();

        }
    }
}