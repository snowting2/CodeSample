using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SlotGame.Popup;
using SlotGame.Attribute;
using DG.Tweening;
using SlotGame.Sound;

namespace SlotGame.Machine.S1023
{
    public class SlotMachine1023 : SlotMachine
    {
#pragma warning disable 0649
        [Separator("Extensions")]
        [Header("Reel")]
        [SerializeField] private VerticalReel bigReel;
        [SerializeField] private ReelEffect reelEffect;
        [SerializeField] private OverrideSpinData bigReelSpinData;
        [SerializeField] private OverrideSpinData regularReelSpinData;

        [SerializeField] private string bigReelStripDefalut;
        [SerializeField] private string coinSymbolID;
        [SerializeField] private string bigCoinSymbolID;
        [SerializeField] private string scatterSymbolID;
        [SerializeField] private string wildSymbolID;
        [SerializeField] private string wildBigSymbolID;
        [SerializeField] private float scatterHitDelayTimeForFree;
        [SerializeField] private float fadeReelTime;

        [SerializeField] private float freeSpinFirstReelInterval;
        [SerializeField] private float freeSpinMidReelInterval;
        [SerializeField] private float freeSpinLastReelInterval;

        [SerializeField] private float bigReelForceStopSlowdownDuration;

        [Header("Link Feature")]
        [SerializeField] private float featureStartDelayTime;
        [SerializeField] private float popupStartWaitTime;
        [SerializeField] private float popupEndWaitTime;
        [SerializeField] private float winWaitTime;
        [SerializeField] private float featureEndDelayTime;
        [SerializeField] private float popupDisappearTime;
        [SerializeField] private float winDelayTime;

        [Header("Canvas")]
        [SerializeField] private Animator LinkHUDAnimator;
        [SerializeField] private GameObject linkFeaturePopup;
        [SerializeField] private LinkFeature1023 linkFeature;

        [Header("JackpotHUD")]
        [SerializeField] private Transform bindLeftHUD;
        [SerializeField] private Transform bindRightHUD;
        [SerializeField] private float defalutBindYPos;
        [SerializeField] private float moveBindYPos;
        [SerializeField] private float bindHUDmoveTime;
        [SerializeField] private float bindHUDwaitTime;

        [Header("Sounds")]
        [SerializeField] private SoundPlayer scatter_scratch_sound;
        [SerializeField] private SoundPlayer linkFeature_pop_bonus_sound;
        [SerializeField] private SoundPlayer bonus_bgm;
        [SerializeField] private SoundPlayer normal_bgm;
        [SerializeField] private SoundPlayer free_bgm;
        [SerializeField] private SoundPlayer bigReel_stop_sound;
        [SerializeField] private SoundPlayer bigSymbol_appear_sound;
        [SerializeField] private SoundPlayer scatter_appear_sound;
        [SerializeField] private SoundPlayer popup_ending;
        [SerializeField] private SoundPlayer link_win_sound;
        [SerializeField] private SoundPlayer wild_win_sound;

        [Header("Test")]
        [SerializeField] private bool isSkipLinkFeature = false;

#pragma warning restore 0649

        private const int FIXED_REEL_INDEX = 2;
        private const int FIXED_BIG_REEL_MID_SYMBOL_INDEX = 0;

        private readonly string LINK_HUD_TOLINK_TRIGGER = "ToLink";
        private readonly string LINK_HUD_LINK_TOFREE_TRIGGER = "LinkToFree";
        private readonly string LINK_HUD_LINK_TONORMAL_TRIGGER = "LinkToNormal";
        private readonly string LINK_HUD_FREE_TONORMAL_TRIGGER = "FreeToNormal";
        private readonly string LINK_HUD_FREE_TOLINK_TRIGGER = "FreeToLink";
        private readonly string LINK_HUD_NORMAL_TOFREE_TRIGGER = "NormalToFree";

        private SpinData cachedSpinData = null;
        private List<int> reelIndexesByBigReel = new List<int>();
        private ReelGroup.ReelInfo bigReelInfo = new ReelGroup.ReelInfo();
        private List<Symbol> bigReelDelegateSymbols = new List<Symbol>();
        private List<Symbol> scatterDelegateSymbols = new List<Symbol>();
        private CoinManager1023 coinManager = null;
        private float scatterHitDelayTimeForRegular = 0.0f;
        private List<long> cachedJackpotCoins = new List<long>();

        private readonly string STATE_LINK_FEATURE_START = "LinkFeatureStartState";
        private readonly string STATE_LINK_FEATURE_END = "LinkFeatureEndState";
        private readonly int LINK_START_COUNT = 6;


        private bool isStartLinkFeature = false;
        private Coroutine jackpotHUDMovingCoroutine = null;
        private bool linkFeaturePopupBtnEnable = false;
        private Animator linkFeaturePopupAnimator = null;
        private Color coinSymbolChangeColor = Color.white;
        private List<CoinVisual> changedCoinVisuals = new List<CoinVisual>();
        private bool isWildHitSoundPlay = false;

        private float originFirstReelInterval = 0.0f;
        private float originMidReelInterval = 0.0f;
        private float originLastReelInterval = 0.0f;

        private float bigReelSlowdownDuration = 0.0f;

        public enum ReelGroupType
        {
            Main,
            Link_Regular,
            Link_Free
        }

        private ReelGroupType reelGroupType = ReelGroupType.Main;

        private void Awake()
        {
            coinSymbolChangeColor.r *= 0.5f;
            coinSymbolChangeColor.g *= 0.5f;
            coinSymbolChangeColor.b *= 0.5f;

            normal_bgm.Stop();
        }
        private List<string> GetBigSymsList()
        {
            List<string> syms = SlotMachineUtils.ConvertSymsToList(cachedSpinData.syms, ReelGroup.Column, ReelGroup.Row)[FIXED_REEL_INDEX];

            for (int i = 0; i < syms.Count; i++)
            {
                syms[i] += "_1";
            }

            return syms;
        }

        // 릴스트립 ID 빅심볼 ID 로 변환
        private string[] GetBigReelStripIds()
        {
            string[] strip = bigReelStripDefalut.Split(',');
            for (int i = 0; i < strip.Length; i++)
            {
                strip[i] += "_1";
            }
            return strip;
        }

        private void SetupBigReelInfo()
        {
            reelIndexesByBigReel.Clear();
            reelIndexesByBigReel.Add(1);
            reelIndexesByBigReel.Add(2);
            reelIndexesByBigReel.Add(3);

            bigReel.ReelStrip.Setup(GetBigReelStripIds());

            bigReel.Initialize();

            bigReelInfo = new ReelGroup.ReelInfo()
            {
                index = 5,
                startIntervalDefault = 0,
                stopIntervalDefault = freeSpinMidReelInterval,
                reel = bigReel
            };
        }

        private void OnSlotMachineStart()
        {
            SetupBigReelInfo();

            OnFastSpin.AddListener(OnFastSpinHandler);

            coinManager = new CoinManager1023(Info, ReelGroup, bigReel);
            coinManager.SetReelStripStartCoinValue(CurrentReelMode);
        }

        // 슬롯머신 진입시 최초 한번 실행되어야 할 스테이트
        // 이 부분에 슬롯머신 진입 애니메이션 등을 구현해야함.
        protected override IEnumerator EnterState()
        {
            originFirstReelInterval = ReelGroup.GetReelInfo(0).stopIntervalDefault;
            originMidReelInterval = ReelGroup.GetReelInfo(FIXED_BIG_REEL_MID_SYMBOL_INDEX).stopIntervalDefault;
            originLastReelInterval = ReelGroup.GetReelInfo(4).stopIntervalDefault;

            ReelGroup.OnStateEnd.AddListener(OnReelStateEnd);
            scatterHitDelayTimeForRegular = scatterHitDelayTime;
            linkFeaturePopupAnimator = linkFeaturePopup.GetComponent<Animator>();

            ChangeEnvReelMove(CurrentReelMode);
            bigReel.OnStateEnd.AddListener(OnBigReelStateEnd);
            yield return base.EnterState();

            // 프리스핀 종료후 릴셋에 등록된 릴 State Setting ( Idle )
            for (int i = 0; i < reelIndexesByBigReel.Count; i++)
            {
                var reel = ReelGroup.GetReel(reelIndexesByBigReel[i]);
                reel.OnStateStart.AddListener(reelState =>
                {
                    if (CurrentState == SlotMachineState.FREESPIN_END)
                    {
                        reel.GetComponent<VerticalReel>().ForceSpinToIdle();
                    }
                });
            }

            paylinesHelper.OnPaylineDrawAllEvent.AddListener(OnPaylineDrawAllEvent);

            bigReelSlowdownDuration = bigReel.slowdownDuration;
        }

        private void OnPaylineDrawAllEvent()
        {
            if (Info.SpinResult.IsBigWin())
            {
                return;
            }

            if (isWildHitSoundPlay == true)
            {
                return;
            }

            for (int infoIndex = 0; infoIndex < Info.SpinResult.GetWinInfoCountOfAllMachines(); infoIndex++)
            {
                var winInfo = Info.SpinResult.GetWinInfoFromAllMachines(infoIndex);

                for (int reelIndex = 0; reelIndex < winInfo.hitSymbolPositions.Count; reelIndex++)
                {
                    var symbolList = winInfo.hitSymbolPositions[reelIndex];

                    for (int index = 0; index < symbolList.Count; index++)
                    {
                        PlayWildSound(reelIndex, symbolList[index]);
                    }
                }
            }
        }

        private void PlayWildSound(int reelIndex, int mainSymbolIndex)
        {
            if (CurrentReelMode == ReelMode.Free && reelIndexesByBigReel.Contains(reelIndex) == true)
            {
                var bigSymbol = bigReel.GetMainSymbol(FIXED_BIG_REEL_MID_SYMBOL_INDEX);
                if (bigSymbol.id == wildBigSymbolID)
                {
                    wild_win_sound.Play();
                    isWildHitSoundPlay = true;
                }
            }

            var symbol = ReelGroup.GetReel(reelIndex).GetMainSymbol(mainSymbolIndex);
            if (symbol.id == wildSymbolID)
            {
                wild_win_sound.Play();
                isWildHitSoundPlay = true;
            }
        }

        private void OnBigReelStateEnd(ReelState state)
        {
            if (state == ReelState.Slowdown)
            {
                bigReel_stop_sound.Play();

                for (int mainSymbolIndex = 0; mainSymbolIndex < bigReel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = bigReel.GetMainSymbol(mainSymbolIndex);
                    if (symbol.IsScatter)
                    {
                        scatter_appear_sound.Play();
                    }
                }
            }

            if (state == ReelState.Stop)
            {
                for (int mainSymbolIndex = 0; mainSymbolIndex < bigReel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = bigReel.GetMainSymbol(mainSymbolIndex);
                    if (symbol.id == bigCoinSymbolID)
                    {
                        bigSymbol_appear_sound.Play();
                    }
                }
            }
        }

        // 기본 상태.
        // 기본적으로 유저의 입력을 기다리지만 프리스핀, 리스핀, 오토스핀등의 상황에선 자동으로 스핀 상태로 변경.
        protected override IEnumerator IdleState()
        {
            if (CurrentReelMode == ReelMode.Free)
            {
                bigReel.SetVisualAllMainSymbols(SymbolVisualType.Idle);
            }

            yield return base.IdleState();
        }

        private void OnReelStateEnd(int reelIndex, ReelState reelState, BaseReel reel)
        {
            if (reelState != ReelState.Slowdown)
            {
                return;
            }

            // 스캐터 트리거, 릴이펙트 종료후 애니메이션 재생
            bool appearScatter = false;
            for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
            {
                var symbol = reel.GetMainSymbol(mainSymbolIndex);

                if (symbol.id == scatterSymbolID)
                {
                    if (reelEffect.GetTriggeredSymbols(reelIndex).Count == 0)
                    {
                        continue;
                    }

                    appearScatter = true;
                    break;
                }
            }

            Animator reelEffectAnimator = reel.GetComponentInChildren<Animator>();
            if (reelEffectAnimator == null)
            {
                return;
            }

            if (appearScatter == true)
            {
                reelEffectAnimator.SetTrigger("Win");
                scatter_scratch_sound.Play();
            }
            else
            {
                reelEffectAnimator.SetTrigger("Disappear");
            }

        }

        private void OnFastSpinHandler(bool arg0)
        {
            if (reelGroupType == ReelGroupType.Link_Free || reelGroupType == ReelGroupType.Link_Regular)
            {
                linkFeature.fastSpin = FastSpin;
            }
        }

        public void SymolVisualActivationChange(Symbol symbol, SymbolVisualType symbolVisualType, bool activation)
        {
            if (symbol.id == coinSymbolID && symbolVisualType == SymbolVisualType.Disable && activation == true)
            {
                if (IsLinkFeatureConditionInRegular() == false)
                {
                    CoinVisual coinVisual = symbol.GetComponent<CoinVisual>();
                    if (coinVisual != null)
                    {
                        coinVisual.ChangeVisualColor(coinSymbolChangeColor);
                        changedCoinVisuals.Add(coinVisual);
                    }
                }
                else
                {
                    symbol.SetVisual(SymbolVisualType.Idle);
                }
            }
        }

        private void ResetSymbol()
        {
            for (int i = 0; i < changedCoinVisuals.Count; i++)
            {
                changedCoinVisuals[i].ChangeVisualColor(Color.white);
            }

            changedCoinVisuals.Clear();
        }

        public override void StartSpin()
        {
            if (reelGroupType == ReelGroupType.Main)
            {
                base.StartSpin();
            }
            else
            {
                linkFeature.StartSpin();
            }
        }

        public override void StopSpin(SpinData spinData)
        {
            if (reelGroupType == ReelGroupType.Main)
            {
                cachedSpinData = spinData;

                base.StopSpin(spinData);

                if (Info.GetExtrasCount() > 0)
                {
                    coinManager.SetCoinInfo(CurrentReelMode);
                }
            }
            else
            {
                linkFeature.StopSpin();
            }

            SetCachedJackpot();
        }

        private void SetCachedJackpot()
        {
            cachedJackpotCoins.Clear();

            cachedJackpotCoins.AddRange(Info.jackpotCoins);

            var jackpotList = coinManager.GetJackpotCoinInfoList();

            if (jackpotList.Count == 0) return;

            for (int i = 0; i < jackpotList.Count; i++)
            {
                var jackpot = jackpotList[i];
                int jackpotGrade = jackpot.JackpotKind;
                long jackpotValue = jackpot.CoinValueLong;
                if (jackpotValue > cachedJackpotCoins[jackpotGrade])
                {
                    cachedJackpotCoins[jackpotGrade] = jackpotValue;
                }

            }

            DispatchChangeJackpotCoins(cachedJackpotCoins);
        }

        // 스핀을 시작하는 스테이트.
        protected override IEnumerator SpinStartState()
        {
            isWildHitSoundPlay = false;
            ResetSymbol();
            coinManager.SetReelStripStartCoinValue(CurrentReelMode);

            if (CurrentReelMode == ReelMode.Free)
            {
                bigReel.slowdownDuration = bigReelSlowdownDuration;
            }
            yield return base.SpinStartState();
        }

        private IEnumerator BigReelSpinStopState()
        {
            List<string> syms = GetBigSymsList();

            int stopIndex = reelStripHelper.FindStopIndexOnReelStrip(bigReel.ReelStrip, syms);

            List<ReelStopInfo> stopinfos = Info.SpinResult.ReelStopInfoList;

            ReelStopInfo stopInfo = new ReelStopInfo
            {
                reelIndex = 5,
                nudgeCount = 0,
                stopIndexOnReelStrip = stopIndex
            };

            // Stop 순서 5(bigReel)-0(first)-4(last)
            stopinfos.Add(stopInfo);

            coinManager.SetReelStripStopCoinValue(stopInfo);

            ReelGroup.StopSpin(stopinfos);

            while (ReelGroup.IsBusy == true)
            {
                // Big Reel 혼자 Slowdown duration 이 2배 이상 길기에 Stop 버튼이 눌렸을 때 떨어지는 심볼 속도가 다름
                // => 맞춰준다.
                if (ReelGroup.ignoreInterval == true)
                {
                    bigReel.slowdownDuration = bigReelForceStopSlowdownDuration;
                }
                yield return null;
            }

            NextState(SlotMachineState.SPIN_END);
        }

        // 스핀을 멈추는 스테이트.
        // 스핀 정지 명령을 내리고 실제 스핀이 멈추는 순간까지 유지된다.
        protected override IEnumerator SpinStopState()
        {
            coinManager.SetReelStripStartCoinValue(CurrentReelMode);

            if (CurrentReelMode == ReelMode.Free)
            {
                yield return StartCoroutine(BigReelSpinStopState());
            }
            else
            {
                coinManager.SetReelStripStopCoinValue();
                yield return base.SpinStopState();
            }
        }

        // 스핀이 완전히 종료되었을때 호출 되는 스테이트
        // 스핀 종료 후 특별한 연출등을 표현 (ex : 스택드 와일드 합체 등등)
        protected override IEnumerator SpinEndState()
        {
            if (CurrentReelMode == ReelMode.Free)
            {
                SetBigReelSymbolVisualDelegate();
            }

            yield return base.SpinEndState();
        }

        protected override string GetStateBeforeEnd(bool isExtraSpin, bool isFreespinStart, bool isFreespinEnd, bool isRespin, bool isFreeChoiceStart)
        {
            if (isSkipLinkFeature == false)
            {
                if (CurrentReelMode == ReelMode.Regular)
                {
                    if (IsLinkFeatureConditionInRegular() == true)
                    {
                        return STATE_LINK_FEATURE_START;
                    }
                }
                else if (CurrentReelMode == ReelMode.Free)
                {
                    if (IsLinkFeatureConditionInFree() == true)
                    {
                        return STATE_LINK_FEATURE_START;
                    }
                }
            }

            return base.GetStateBeforeEnd(isExtraSpin, isFreespinStart, isFreespinEnd, isRespin, isFreeChoiceStart);
        }

        // 링크진입 조건 (Regular)
        private bool IsLinkFeatureConditionInRegular()
        {
            int count = 0;

            for (int reelIndex = 0; reelIndex < ReelGroup.Column; reelIndex++)
            {
                var reel = ReelGroup.GetReel(reelIndex);

                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);

                    if (symbol.id == coinSymbolID)
                    {
                        count++;
                    }
                }
            }

            if (count >= LINK_START_COUNT)
            {
                return true;
            }

            return false;
        }

        // 링크진입 조건 (Free)
        private bool IsLinkFeatureConditionInFree()
        {
            var symbol = bigReel.GetMainSymbol(FIXED_BIG_REEL_MID_SYMBOL_INDEX);
            if (symbol.id == bigCoinSymbolID)
            {
                return true;
            }

            return false;
        }

        // reelset 심볼들의 bigReel로 visual delegate 연결
        private void SetBigReelSymbolVisualDelegate()
        {
            Symbol midSymbol = bigReel.GetMainSymbol(FIXED_BIG_REEL_MID_SYMBOL_INDEX);

            // Main ReelIndex(2) MainSymbolIndex(1) 와 BigReelMainSymbol의 Visual Delegate 연결
            for (int reelIndex = 0; reelIndex < reelIndexesByBigReel.Count; reelIndex++)
            {
                var reel = ReelGroup.GetReel(reelIndexesByBigReel[reelIndex]);
                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);
                    symbol.SetVisualDelegate(midSymbol);
                    bigReelDelegateSymbols.Add(symbol);
                }
            }

            StartCoroutine(ClearBigReelSymbolVisualDelegate());
        }

        // Visual Delegate 해제
        private IEnumerator ClearBigReelSymbolVisualDelegate()
        {
            while (CurrentState != SlotMachineState.SPIN_START)
            {
                yield return null;
            }

            for (int i = 0; i < bigReelDelegateSymbols.Count; i++)
            {
                bigReelDelegateSymbols[i].ClearVisualDelegate();
                bigReelDelegateSymbols[i].RemoveAllSubstituteVisuals();
            }

            bigReelDelegateSymbols.Clear();
        }

        private void LinkFeatureHit()
        {
            ResetSymbol();

            if (CurrentReelMode == ReelMode.Regular)
            {
                for (int reelIndex = 0; reelIndex < ReelGroup.Column; reelIndex++)
                {
                    var reel = ReelGroup.GetReel(reelIndex);
                    for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                    {
                        var symbol = reel.GetMainSymbol(mainSymbolIndex);
                        if (symbol.id == coinSymbolID)
                        {
                            symbol.SetVisual(SymbolVisualType.Appear);
                        }
                    }
                }
            }
            else if (CurrentReelMode == ReelMode.Free)
            {
                var symbol = bigReel.GetMainSymbol(FIXED_BIG_REEL_MID_SYMBOL_INDEX);
                if (symbol.id == bigCoinSymbolID)
                {
                    symbol.SetVisual(SymbolVisualType.Appear);
                }
            }

            link_win_sound.Play();
        }

        // Link Feature Start
        private IEnumerator LinkFeatureStartState()
        {
            linkFeature.LinkHUDActive(false, false);
            linkFeaturePopupBtnEnable = false;

            StopRepeatWinningLines();
            paylinesHelper.Clear();

            isStartLinkFeature = false;
            DispatchUseSpinUI(true);

            yield return new WaitForSeconds(0.5f);
            LinkFeatureHit();

            yield return new WaitForSeconds(featureStartDelayTime);

            yield return WaitForCheckAutoPlayForFeatureStart();

            linkFeaturePopup.gameObject.SetActive(true);
            linkFeature_pop_bonus_sound.Play();

            if (CurrentReelMode == ReelMode.Regular)
            {
                LinkHUDAnimator.SetTrigger(LINK_HUD_TOLINK_TRIGGER);
            }
            else if (CurrentReelMode == ReelMode.Free)
            {
                LinkHUDAnimator.SetTrigger(LINK_HUD_FREE_TOLINK_TRIGGER);
            }

            // 링크 팝업 시작 애니메이션 타임
            yield return new WaitForSeconds(popupStartWaitTime);

            ChangeReelGroup();
            linkFeature.Initialize(coinManager, GetCurrentSymbolList(), reelGroupType, cachedJackpotCoins);

            linkFeaturePopupBtnEnable = true;

            if (holdType != HoldType.On)
            {
                linkFeaturePopup.GetComponent<PopupAutoCloser>().onTimesUp.AddListener(OnClickedBtn_LinkFeatureStartPopup);
            }

            while (isStartLinkFeature == false)
            {
                yield return null;
            }
            bonus_bgm.Play();
            linkFeature.LinkHUDActive(true, false);

            // 링크 팝업 종료 애니메이션 타임
            yield return new WaitForSeconds(popupEndWaitTime);


            linkFeaturePopup.gameObject.SetActive(false);

            linkFeature.autoSpin = true;
            linkFeature.fastSpin = FastSpin;

            yield return linkFeature.Play();

            NextState(STATE_LINK_FEATURE_END);
        }

        protected override bool IsWinPopupSkip()
        {
            if (CurrentState == STATE_LINK_FEATURE_END)
            {
                return false;
            }

            return base.IsWinPopupSkip();
        }

        // Link Feature End
        private IEnumerator LinkFeatureEndState()
        {
            yield return new WaitForSeconds(winDelayTime);

            DispatchUseSpinUI(false);

            var lastCoinInfo = coinManager.GetLastCoinInfo();

            // 윈코인 
            if (lastCoinInfo.JackpotKind == 0)
            {
                // Grand Jackpot
                var jackpotGrade = lastCoinInfo.JackpotKind;
                var totalWinCoins = Info.TotalWinCoins;

                yield return HitJackpot(jackpotGrade, totalWinCoins, totalWinCoins, false);

                cachedJackpotCoins[jackpotGrade] = Info.finalJackpotCoins[jackpotGrade];
                DispatchChangeJackpotCoins(cachedJackpotCoins);
            }
            else
            {
                // Normal Win
                var winGrade = SlotMachineUtils.GetWinGrade(Info.SpinResult.WinCoinsFeature, Info.CurrentTotalBet);
                var winCoins = Info.SpinResult.WinCoinsFeature;
                long totalWinCoins = Info.TotalWinCoins;

                if (IsBigWin(winCoins))
                {
                    yield return HitBigwin(winGrade, winCoins, totalWinCoins, false);
                }
                else
                {
                    yield return HitNormal(winGrade, winCoins, totalWinCoins, false);
                }
            }

            yield return new WaitForSeconds(winWaitTime);

            linkFeaturePopup.SetActive(true);
            linkFeaturePopupAnimator.SetTrigger("Disappear");
            popup_ending.Play();

            yield return new WaitForSeconds(popupDisappearTime);


            ChangeReelGroup();

            if (CurrentReelMode == ReelMode.Regular)
            {
                LinkHUDAnimator.SetTrigger(LINK_HUD_LINK_TONORMAL_TRIGGER);
            }
            else if (CurrentReelMode == ReelMode.Free)
            {
                LinkHUDAnimator.SetTrigger(LINK_HUD_LINK_TOFREE_TRIGGER);
            }

            yield return new WaitForSeconds(featureEndDelayTime);

            linkFeaturePopup.SetActive(false);
            linkFeature.ResetSymbol();

            // LinkFeatureStateExit
            var nextState = base.GetStateBeforeEnd(Info.IsExtraSpin, Info.IsFreespinStart, Info.IsFreespinEnd, Info.IsRespin, Info.IsFreeChoiceStart);
            NextState(nextState);
        }

        private void ChangeReelGroup()
        {
            if (CurrentState == STATE_LINK_FEATURE_START)
            {
                if (CurrentReelMode == ReelMode.Regular)
                {
                    reelGroupType = ReelGroupType.Link_Regular;
                }
                else if (CurrentReelMode == ReelMode.Free)
                {
                    reelGroupType = ReelGroupType.Link_Free;
                }

                ReelGroup.gameObject.SetActive(false);
                linkFeature.gameObject.SetActive(true);



                for (int i = 1; i < 13; i++)
                {
                    string symbolID = i.ToString();

                    if (symbolID == coinSymbolID)
                    {
                        continue;
                    }

                    Symbol.AddSubstituteVisualAtGlobal(symbolID, SymbolVisualType.Idle, 0, SymbolVisualType.Disable, 0);
                    Symbol.AddSubstituteVisualAtGlobal(symbolID, SymbolVisualType.Appear, 0, SymbolVisualType.Disable, 0);
                }
            }
            else
            {
                reelGroupType = ReelGroupType.Main;
                if (CurrentReelMode == ReelMode.Regular)
                {
                    normal_bgm.Play();
                }
                else if (CurrentReelMode == ReelMode.Free)
                {
                    free_bgm.Play();
                }

                ReelGroup.gameObject.SetActive(true);
                linkFeature.gameObject.SetActive(false);
                Symbol.RemoveAllSubstituteVisualsAtGlobal();
                reelGroupHelper.SetVisualAllSymbols(SymbolVisualType.Idle);
            }
        }

        public void OnClickedBtn_LinkFeatureStartPopup()
        {
            if (linkFeaturePopupBtnEnable == false)
            {
                return;
            }

            if (isStartLinkFeature == false)
            {
                linkFeaturePopupAnimator.SetTrigger("On");
                isStartLinkFeature = true;
                linkFeaturePopupBtnEnable = false;
            }
        }
        
        protected override IEnumerator ScatterState()
        {
            if (CurrentReelMode == ReelMode.Free)
            {
                StartCoroutine(ChangeReelSetSymbol());

                scatterHitDelayTime = scatterHitDelayTimeForFree;
            }
            else if (CurrentReelMode == ReelMode.Regular)
            {
                scatterHitDelayTime = scatterHitDelayTimeForRegular;
            }
            ResetSymbol();

            yield return base.ScatterState();

            for (int i = 0; i < scatterDelegateSymbols.Count; i++)
            {
                scatterDelegateSymbols[i].ClearVisualDelegate();
            }
        }

        // Scatter Hit -> ReelSet에 등록된 심볼을 스캐터로 변환
        private IEnumerator ChangeReelSetSymbol()
        {
            var reel = ReelGroup.GetReel(FIXED_REEL_INDEX);
            var symbol = reel.GetMainSymbol(1);
            scatterDelegateSymbols.Clear();

            string symbolId = symbol.id;

            Symbol bigSymbol = bigReel.GetMainSymbol(FIXED_BIG_REEL_MID_SYMBOL_INDEX);
            // 
            if (symbol.IsScatter == false)
            {
                var changeSymbol = reel.ChangeMainSymbol(1, scatterSymbolID);
                changeSymbol.SetVisualDelegate(bigSymbol);
                scatterDelegateSymbols.Add(changeSymbol);
            }

            scatterHitDelayTime = scatterHitDelayTimeForFree;

            // Scatter State 가 끝나면 원래 심볼로 돌려놓는다.
            while (CurrentState == SlotMachineState.SCATTER)
            {
                yield return null;
            }

            reel.ChangeMainSymbol(1, symbolId);
        }

        // 프리스핀을 시작하는 스테이트
        // 프리스핀 팝업을 보여줌
        protected override IEnumerator FreespinStartState()
        {
            yield return base.FreespinStartState();
            yield return FreeSpinSetup();

        }

        // 프리스핀 종료 스테이트
        // 프리스핀 종료 팝업을 보여줌
        protected override IEnumerator FreespinEndState()
        {
            yield return base.FreespinEndState();
            LinkHUDAnimator.SetTrigger(LINK_HUD_FREE_TONORMAL_TRIGGER);
            ResetSymbol();
            yield return FreeSpinClear();
        }

        private IEnumerator FreeSpinSetup(bool init = false)
        {
            float time = (init == true) ? 0.0f : fadeReelTime;

            LinkHUDAnimator.SetTrigger(LINK_HUD_NORMAL_TOFREE_TRIGGER);
            reelEffect.Disable(0);

            ReelGroup.AddReelSet(reelIndexesByBigReel, bigReelInfo, 5);

            // big reel init
            if (init == false)
            {
                FadeBigReel(0.0f, 0.0f);
            }

            bigReel.gameObject.SetActive(true);

            // big reel fade
            FadeBigReel(1.0f, time);

            // normal reel fade
            FadeReelSet(0.0f, time, true, true);

            yield return new WaitForSeconds(time);

            // active false
            FadeReelSet(1.0f, 0.0f, false, false);

            ChangeSpinData(0, ReelMode.Regular);
            ChangeSpinData(1, ReelMode.Free);
            ChangeSpinData(2, ReelMode.Free);
            ChangeSpinData(3, ReelMode.Free);
            ChangeSpinData(4, ReelMode.Regular);

            var firstReelInfo = ReelGroup.GetReelInfo(0);
            firstReelInfo.stopIntervalDefault = freeSpinFirstReelInterval;

            bigReelInfo.stopIntervalDefault = freeSpinMidReelInterval;

            var lastReelInfo = ReelGroup.GetReelInfo(4);
            lastReelInfo.stopIntervalDefault = freeSpinLastReelInterval;
        }

        private IEnumerator FreeSpinClear(bool init = false)
        {
            float time = (init == true) ? 0.0f : fadeReelTime;
            reelEffect.Enable(0);

            ReelGroup.RemoveReelSet(reelIndexesByBigReel);

            RestoreNormalCoinSymbols();
            // big reel fade
            FadeBigReel(0.0f, time);

            // normal reel fade
            FadeReelSet(1.0f, time, true, false);

            yield return new WaitForSeconds(time);

            // big reel active false
            bigReel.gameObject.SetActive(false);

            for (int reelIndex = 0; reelIndex < ReelGroup.Column; reelIndex++)
            {
                ChangeSpinData(reelIndex, ReelMode.Regular);
            }

            var firstReelInfo = ReelGroup.GetReelInfo(0);
            firstReelInfo.stopIntervalDefault = originFirstReelInterval;

            bigReelInfo.stopIntervalDefault = originMidReelInterval;

            var lastReelInfo = ReelGroup.GetReelInfo(4);
            lastReelInfo.stopIntervalDefault = originLastReelInterval;
        }

        private void FadeBigReel(float alpha, float time)
        {
            foreach (SpriteRenderer renderer in bigReel.GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.DOFade(alpha, time);
            }
            foreach (TextMesh mesh in bigReel.GetComponentsInChildren<TextMesh>())
            {
                mesh.GetComponent<Renderer>().material.DOFade(alpha, time);
            }
        }

        private void FadeReelSet(float alpha, float time, bool isActive, bool isSetIdle)
        {
            for (int i = 0; i < reelIndexesByBigReel.Count; i++)
            {
                int reelIndex = reelIndexesByBigReel[i];
                var reel = ReelGroup.GetReel(reelIndex);

                if (isSetIdle)
                {
                    reel.SetVisualAllMainSymbols(SymbolVisualType.Idle);
                }

                reel.gameObject.SetActive(isActive);

                foreach (SpriteRenderer renderer in reel.GetComponentsInChildren<SpriteRenderer>())
                {
                    renderer.DOFade(alpha, time);
                }
                foreach (TextMesh mesh in reel.GetComponentsInChildren<TextMesh>())
                {
                    mesh.GetComponent<MeshRenderer>().material.DOFade(alpha, time);
                }
            }
        }

        // 프리스핀 복구시 프리스핀 이전 심볼 정보가 없는 경우, 노말스핀으로 돌아왔을 때 coin value 를 세팅
        private void RestoreNormalCoinSymbols()
        {
            for (int reelIndex = 0; reelIndex < ReelGroup.Column; reelIndex++)
            {
                var reel = ReelGroup.GetReel(reelIndex);

                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);

                    if (symbol.id == coinSymbolID && string.IsNullOrEmpty(symbol.Value))
                    {
                        symbol.Value = coinManager.GetRandomCoinValue(false);
                    }
                }
            }
        }

        private void ChangeSpinData(int reelIndex, ReelMode reelMode)
        {
            VerticalReel reel = ReelGroup.GetReel(reelIndex).GetComponent<VerticalReel>();
            if (reel == null)
            {
                return;
            }

            if (reelMode == ReelMode.Free)
            {
                reel.SetOverrideSpinData(bigReelSpinData);
            }
            else if (reelMode == ReelMode.Regular)
            {
                reel.SetOverrideSpinData(regularReelSpinData);
            }
        }

        private void ChangeEnvReelMove(ReelMode reelMode)
        {
            if (reelMode == ReelMode.Free)
            {
                free_bgm.Play();

                StartCoroutine(FreeSpinSetup(true));
                LinkHUDAnimator.SetTrigger(LINK_HUD_NORMAL_TOFREE_TRIGGER);

                if (jackpotHUDMovingCoroutine != null)
                {
                    StopCoroutine(jackpotHUDMovingCoroutine);
                    jackpotHUDMovingCoroutine = null;

                    bindLeftHUD.DOLocalMoveY(defalutBindYPos, bindHUDmoveTime);
                    bindRightHUD.DOLocalMoveY(defalutBindYPos, bindHUDmoveTime);
                }
            }
            else if (reelMode == ReelMode.Regular)
            {
                normal_bgm.Play();
                StartCoroutine(FreeSpinClear(true));

                // HUD Moving
                if (jackpotHUDMovingCoroutine == null)
                {
                    jackpotHUDMovingCoroutine = StartCoroutine(MoveJackpotHUD());
                }
            }
        }

        private IEnumerator MoveJackpotHUD()
        {
            bindLeftHUD.DOLocalMoveY(defalutBindYPos, bindHUDmoveTime);
            bindRightHUD.DOLocalMoveY(defalutBindYPos, bindHUDmoveTime);

            yield return new WaitForSeconds(bindHUDwaitTime);

            bindLeftHUD.DOLocalMoveY(moveBindYPos, bindHUDmoveTime);
            bindRightHUD.DOLocalMoveY(moveBindYPos, bindHUDmoveTime);

            yield return new WaitForSeconds(bindHUDwaitTime);

            StartCoroutine(MoveJackpotHUD());
        }

        // 현재 심볼 리스트를 적재
        private List<string> GetCurrentSymbolList()
        {
            List<string> featureStartSymbolIdList = new List<string>();
            for (int mainSymbolIndex = 2; mainSymbolIndex >= 0; mainSymbolIndex--)
            {
                if (reelGroupType == ReelGroupType.Link_Regular)
                {
                    for (int reelIndex = 0; reelIndex < 5; reelIndex++)
                    {
                        string symbolID = ReelGroup.GetReel(reelIndex).GetMainSymbol(mainSymbolIndex).id;
                        featureStartSymbolIdList.Add(symbolID);
                    }
                }
                else if (reelGroupType == ReelGroupType.Link_Free)
                {
                    for (int reelIndex = 0; reelIndex < 5; reelIndex++)
                    {
                        if (reelIndex == FIXED_REEL_INDEX)
                        {
                            string symbolID = bigReel.GetMainSymbol(FIXED_BIG_REEL_MID_SYMBOL_INDEX).id;
                            featureStartSymbolIdList.Add(symbolID);
                        }
                        else
                        {
                            string symbolID = ReelGroup.GetReel(reelIndex).GetMainSymbol(mainSymbolIndex).id;
                            featureStartSymbolIdList.Add(symbolID);
                        }
                    }
                }
            }

            return featureStartSymbolIdList;
        }
    }
}