using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Attribute;
using SlotGame.Popup;
using SlotGame.UI;

using SlotGame.Sound;
using Spine.Unity;

namespace SlotGame.Machine.S1061
{
    public class SlotMachine1061 : SlotMachine
    {
#pragma warning disable 0649
        [Separator("1061 General")]
        [SerializeField] private GameObject normalFeature;
        [SerializeField] private LinkFeature1061 linkFeature;
        [SerializeField] private PotTrail1061 potTrail;
        [SerializeField] private PotProgress1061 potProgress;
        [SerializeField] private Animator machineBG;
        [SerializeField] private Animator animatorBG;
        [SerializeField] private SuperBonus1061 superBonusUI;
        [SerializeField] private GameObject spinPanel;
        [SerializeField] private SkeletonAnimation pandaSpine;

        [Separator("Jackpot HUD")]
        [SerializeField] private BenefitJackpotHUD normalJakcpotHUD;

        [Separator("Link Symbol")]
        [SerializeField] private ReelStripFeed feed;
        [SerializeField] private LinkSymbolManager1061 linkSymbolManager = null;

        [Separator("Time")]
        [SerializeField] private float appearTime = 0.433f;
        [SerializeField] private float jackpotTextAnimTime = 1.5f;
        [SerializeField] private float introAnimTime;
        [SerializeField] private float linkResultCoinIntervalTime = 0.5f;
        [SerializeField] private float pandaHitDelayTime;
        [SerializeField] private float pandaBigHitDelayTime;
        [SerializeField] private float linkWinCoinTime = 0.3f;
        [SerializeField] private float potTrailDelayTime;

        [Separator("Dimmed Symbol")]
        public DimmedSymbolData1061 dimmedSymbolData;

        [Separator("Sounds")]
        [SerializeField] private SoundPlayer normalBGM;
        [SerializeField] private SoundPlayer linkBGM_Blue;
        [SerializeField] private SoundPlayer linkBGM_Purple;
        [SerializeField] private SoundPlayer linkBGM_Red;
        [SerializeField] private SoundPlayer linkBGM_Mix;
        [SerializeField] private SoundPlayer linkBGM_Super;
        [SerializeField] private SoundPlayer linkWinCoinSound;

        [Space]
        [SerializeField] private SoundPlayer[] normalIntroSound;

        [Space]
        [SerializeField] private SoundPlayer potIntroSound;
        [SerializeField] private SoundPlayer introPot_Red;
        [SerializeField] private SoundPlayer introPot_Blue;
        [SerializeField] private SoundPlayer introPot_Purple;
        [SerializeField] private SoundPlayer introMixPot2;
        [SerializeField] private SoundPlayer[] introMixPot3;


#pragma warning restore 0649

        private ExtraInfo1061 extraInfo = null;
        private List<long> cachedJackpotCoins = new List<long>();
        private int currentTotalBetIndex = -1;
        private List<string> currentSymbolList = new List<string>();


        //-------------------------------------------------------------------------------
        // BG Trigger 
        //-------------------------------------------------------------------------------
        private readonly string TRIGGER_NORMAL = "normal";
        private readonly string TRIGGER_LINK = "link";
        private readonly string TRIGGER_SUPER_LINK = "superLink";
        //-------------------------------------------------------------------------------
        // Visual 
        //-------------------------------------------------------------------------------
        private readonly SymbolVisualType appearVisual = SymbolVisualType.Etc1;
        private readonly SymbolVisualType loopVisual = SymbolVisualType.Etc2;
        //-------------------------------------------------------------------------------
        // Popup
        //-------------------------------------------------------------------------------
        private readonly string LINK_START_POPUP = "LinkIntroPopup1061";
        private readonly string JACKPOT_POPUP = "JackpotPopup1061";
        private readonly string LINK_RESULT_POPUP = "LinkResultPopup1061";
        //-------------------------------------------------------------------------------
        // Panda Spine
        //-------------------------------------------------------------------------------
        private readonly string[] PANDA_IDLE = new string[] { "idle", "idle2", "idle3" };
        private readonly string[] PANDA_HIT = new string[] { "hit", "hit2" };
        private readonly string[] PANDA_BIG_HIT = new string[] { "hit_big", "hit_big2" };
        private readonly string[] PANDA_HIT_POT = new string[] { "hit_pot", "hit_pot2", "hit_pot3", "hit_pot4", "hit_pot5", "hit_pot6" };
        //-------------------------------------------------------------------------------
        private long linkResultWinCoin = 0;
        private readonly string SIGNAL_NAME_POT_MIX = "potMix";

        protected override void SetupPopups()
        {
            base.SetupPopups();

            PopupSystem.Instance.AddCachedPopup<LinkStartPopup1061>(slotAssets, LINK_START_POPUP);
            PopupSystem.Instance.AddCachedPopup<LinkResultPopup1061>(slotAssets, LINK_RESULT_POPUP);
            PopupSystem.Instance.AddCachedPopup<JackpotPopup1061>(slotAssets, JACKPOT_POPUP);
        }

        protected override void RemovePopups()
        {
            base.RemovePopups();

            PopupSystem.Instance.RemoveCachedPopup(LINK_START_POPUP);
            PopupSystem.Instance.RemoveCachedPopup(JACKPOT_POPUP);
            PopupSystem.Instance.RemoveCachedPopup(LINK_RESULT_POPUP);
        }
        protected override void PostInitialize(EnterData enterData)
        {
            extraInfo = this.GetComponent<ExtraInfo1061>();
            extraInfo.Initialize();

            linkSymbolManager.Initialize(enterData.direct_pays, extraInfo, Info);
            potProgress.Initialize(extraInfo);
            potTrail.Initialize(extraInfo);

            OnTotalBetChange.AddListener(OnTotalBetChangeHandler);
            OnAverageBetChange.AddListener(OnAverageBetChangeHandler);
            OnTotalBetChange.Invoke(Info.CurrentTotalBet);

            base.PostInitialize(enterData);
        }

        // 슬롯머신 진입시 최초 한번 실행되어야 할 스테이트
        // 이 부분에 슬롯머신 진입 애니메이션 등을 구현해야함.
        protected override IEnumerator EnterState()
        {
            linkFeature.InitializeEnter(extraInfo, linkSymbolManager);

            if (extraInfo.IsSuperLink) SetAverageBet(extraInfo.AverageBet);

            LoadSymbols();
            SetCachedJackpot();

            yield return SetEnterLink();

            superBonusUI.Initialize(extraInfo.SuperBonusLevel, extraInfo.IsSuperLink);
            ReelGroup.OnStateEnd.AddListener(OnReelStateEnd);

            yield return base.EnterState();
        }

        private void OnIdlePanda()
        {
            int randomIndex = UnityEngine.Random.Range(0, PANDA_IDLE.Length);
            if (PANDA_HIT.Contains(pandaSpine.AnimationName) || PANDA_BIG_HIT.Contains(pandaSpine.AnimationName))
            {
                pandaSpine.state.SetAnimation(0, PANDA_IDLE[randomIndex], true);
            }
            else
            {
                pandaSpine.state.AddAnimation(0, PANDA_IDLE[randomIndex], true, 0.0f);
            }
        }

        private IEnumerator OnHitPanda(bool bigwin)
        {
            if (bigwin)
            {
                int randomIndex = UnityEngine.Random.Range(0, PANDA_BIG_HIT.Length);

                yield return WaitForSeconds(pandaBigHitDelayTime);
                pandaSpine.state.SetAnimation(0, PANDA_BIG_HIT[randomIndex], true);
            }
            else
            {
                int randomIndex = UnityEngine.Random.Range(0, PANDA_HIT.Length);

                yield return WaitForSeconds(pandaHitDelayTime);
                pandaSpine.state.SetAnimation(0, PANDA_HIT[randomIndex], true);
            }
        }

        // Conncted Pot Pregrsss Animator
        public void OnHitPotPanda()
        {
            int randomIndex = UnityEngine.Random.Range(0, 2);
            int potHitCount = 0;

            if (extraInfo.IncludeBluePot) potHitCount++;
            if (extraInfo.IncludePurplePot) potHitCount++;
            if (extraInfo.IncludeRedPot) potHitCount++;

            randomIndex = ((potHitCount - 1) * 2) + randomIndex;
            Debug.LogFormat("**pothitcount : {0} , randomindex : {1}", potHitCount, randomIndex);
            pandaSpine.state.SetAnimation(0, PANDA_HIT_POT[randomIndex], true);
        }

        private void InitializeLinkSymbol()
        {
            for (int reelIndex = 0; reelIndex < ReelGroup.Column; reelIndex++)
            {
                var reel = ReelGroup.GetReel(reelIndex);
                for (int symbolIndex = 0; symbolIndex < reel.AllSymbolsLength; symbolIndex++)
                {
                    var symbol = reel.GetSymbol(symbolIndex);
                    if (symbol.IsLink)
                    {
                        linkSymbolManager.OnSymbolAdded(reelIndex, reel, symbol, 0);
                    }
                }
            }
        }

        private IEnumerator SetEnterLink()
        {
            if (Info.IsLinkRespin == true)
            {
                if (Info.IsLinkRespinStart)
                {
                    superBonusUI.gameObject.SetActive(true);
                    NextState(SlotMachineState.LINK_RESPIN_START);
                }
                else
                {
                    ConvertReelStrip();
                    dimmedSymbolData.Enable = false;
                    OnJackpotHUD(true);
                    superBonusUI.gameObject.SetActive(false);

                    yield return LinkStartEnter();
                }
            }
            else
            {
                normalBGM.Play();
                int randIndex = UnityEngine.Random.Range(0, normalIntroSound.Length);
                normalIntroSound[randIndex].Play();

                InitializeLinkSymbol();
                dimmedSymbolData.Enable = true;
                OnJackpotHUD(false);
                superBonusUI.gameObject.SetActive(true);
            }

            yield return null;
        }

        private void OnJackpotHUD(bool isLink)
        {
            if (isLink)
            {
                normalJakcpotHUD.gameObject.SetActive(false);
            }
            else
            {
                normalJakcpotHUD.gameObject.SetActive(true);
                normalJakcpotHUD.UpdateItems(true, false);
            }
        }

        private void OnReelStateEnd(int reelIndex, ReelState state, BaseReel reel)
        {
            if (state == ReelState.Slowdown)
            {
                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);

                    if (symbol.IsLink)
                    {
                        StartCoroutine(SetVisualAppear(reel, symbol, reelIndex, mainSymbolIndex));
                    }
                }
            }
        }

        private IEnumerator SetVisualAppear(BaseReel reel, Symbol symbol, int reelIndex, int mainSymbolIndex)
        {
            symbol.SetVisual(appearVisual);

            yield return WaitForSeconds(appearTime);

            yield return new WaitUntil(() => reel.CurrentState == ReelState.Idle);

            symbol.SetVisual(loopVisual);
        }

        // 기본 상태.
        // 기본적으로 유저의 입력을 기다리지만 프리스핀, 리스핀, 오토스핀등의 상황에선 자동으로 스핀 상태로 변경.
        protected override IEnumerator IdleState()
        {
            if (Info.IsLinkRespin)
            {
                yield return new WaitUntil(() => linkFeature.IsInitializeEnd);
            }

            yield return base.IdleState();
        }

        // 스핀을 시작하는 스테이트.
        protected override IEnumerator SpinStartState()
        {
            if (CurrentReelMode == ReelMode.LinkRespin)
            {
                yield return linkFeature.SpinStart();
            }
            else
            {
                OnIdlePanda();
            }
            yield return base.SpinStartState();
        }

        protected override IEnumerator WaitForSpinSignalEffect()
        {
            if (CurrentReelMode != ReelMode.Regular) yield break;

            if (SignalEffect != null)
            {
                int potCount = 0;
                if (extraInfo.IncludePurplePot) potCount++;
                if (extraInfo.IncludeBluePot) potCount++;
                if (extraInfo.IncludeRedPot) potCount++;

                if (potCount > 1)
                {
                    SignalEffect.ForcePlayOnce(SignalEffect.SignalPosition.Spin, SIGNAL_NAME_POT_MIX);
                    yield return SignalEffect.Play(SignalEffect.SignalPosition.Spin, SIGNAL_NAME_POT_MIX);
                }
                else
                {
                    yield return base.WaitForSpinSignalEffect();
                }
            }
        }

        protected override IEnumerator SpinUpdateState()
        {
            return base.SpinUpdateState();
        }

        protected override IEnumerator ChangeReels()
        {
            if (CurrentReelMode != ReelMode.LinkRespin)
            {
                yield return base.ChangeReels();
            }
        }

        // 스핀 응답을 받고 Info 를 업데이트 한 뒤 실제 릴에게 stop 명령을 내리기 전 스테이트
        protected override IEnumerator SpinPreStopState()
        {
            if (CurrentReelMode == ReelMode.LinkRespin)
            {
                linkFeature.SpinPreStop();
            }

            yield return base.SpinPreStopState();
        }

        public override void StopSpin(SpinData spinData)
        {
            base.StopSpin(spinData);

            extraInfo.OnUpdateInfo();
        }

        // 스핀을 멈추는 스테이트.
        // 스핀 정지 명령을 내리고 실제 스핀이 멈추는 순간까지 유지된다.
        protected override IEnumerator SpinStopState()
        {
            if (CurrentReelMode == ReelMode.LinkRespin)
            {
                linkFeature.SpinStop();
            }

            yield return base.SpinStopState();
        }

        // 스핀이 완전히 종료되었을때 호출 되는 스테이트
        // 스핀 종료 후 특별한 연출등을 표현 (ex : 스택드 와일드 합체 등등)
        protected override IEnumerator SpinEndState()
        {
            if (CurrentReelMode == ReelMode.LinkRespin)
            {
                yield return linkFeature.SpinEnd();
            }
            else
            {
                yield return OnPotTrail();
            }

            yield return base.SpinEndState();
        }

        private IEnumerator OnPotTrail()
        {
            bool isPotTrail = false;

            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                var reel = reelGroup.GetReel(reelIndex);
                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);
                    if (symbol.IsLink)
                    {
                        potTrail.OnPotTrail(symbol);
                        isPotTrail = true;
                    }
                }
            }

            if (isPotTrail && Info.TotalWinCoins == 0)
            {
                yield return WaitForSeconds(potTrailDelayTime);
            }
        }
        
        // 일반 히트 스테이트
        protected override IEnumerator HitNormalState()
        {
            StartCoroutine(OnHitPanda(false));
            yield return base.HitNormalState();
        }

        // 빅윈 히트 스테이트
        protected override IEnumerator HitBigwinState()
        {
            StartCoroutine(OnHitPanda(true));
            yield return base.HitBigwinState();
        }
        
        // 링크 리스핀 히트 스테이트
        protected override IEnumerator LinkRespinScatterState()
        {
            StopRepeatWinningLines();
            machineGroup.ForEachPayline(payline => payline.Clear());
            machineGroup.ForEachGroupHelper(groupHelper => groupHelper.SetVisualAllSymbols(SymbolVisualType.Idle));

            StartCoroutine(LinkStartAction());
            OnPlayPotIntroSound();

            yield return WaitForSeconds(linkHitDelayTime, true);

            NextState(SlotMachineState.LINK_RESPIN_START);
        }

        // Connecter Animator
        public void OnLinkScatterAction()
        {
            StartCoroutine(LinkStartAction());
        }

        private IEnumerator LinkStartAction()
        {
            StartCoroutine(potProgress.OnHitTrigger(extraInfo.CurrentPotFeature));

            yield return superBonusUI.OnTriggerGauge(extraInfo.SuperBonusLevel);
        }

        private void OnPlayPotIntroSound()
        {
            int mixCount = GetPotMixCount();
            if (mixCount == 1)
            {
                if (extraInfo.CurrentPotFeature == PotFeatureType.Purple) introPot_Purple.Play();
                else if (extraInfo.CurrentPotFeature == PotFeatureType.Red) introPot_Red.Play();
                else if (extraInfo.CurrentPotFeature == PotFeatureType.Blue) introPot_Blue.Play();
            }
            else if (mixCount == 2)
            {
                introMixPot2.Play();
            }
            else if (mixCount == 3)
            {
                int randIndex = UnityEngine.Random.Range(0, introMixPot3.Length);
                introMixPot3[randIndex].Play();
            }
        }

        private IEnumerator LinkStartEnter()
        {
            Debug.Log("LinkStartEnter");

            DispatchChangeLinkRespins(Info.LinkRespinCurrentCount, Info.LinkRespinRemainCount);
            SetReelMode(ReelMode.LinkRespin);

            Info.UpdateNextReels();

            linkResultWinCoin = Info.TotalWinCoins;

            if (extraInfo.IsTriplePot)
            {
                machineBG.SetTrigger(TRIGGER_SUPER_LINK);
                animatorBG.SetTrigger(TRIGGER_SUPER_LINK);
            }
            else
            {
                machineBG.SetTrigger(TRIGGER_LINK);
                animatorBG.SetTrigger(TRIGGER_LINK);
            }

            OnPlayLinkBGM();

            yield return linkFeature.Initialize(null);
        }

        // 링크 리스핀 시작
        protected override IEnumerator LinkRespinStartState()
        {
            if (currentSymbolList == null) currentSymbolList = new List<string>();

            currentSymbolList.Clear();
            currentSymbolList = GetCurrentSymbolList();

            DispatchWinCoins(0, 0.0f, WinGrade.None);
            DispatchTotalWinCoins(0, 0.0f);

            HideDimPanel();
            Info.UpdateNextReels();
            reelGroupHelper.SetVisualAllSymbols(SymbolVisualType.Idle);

            linkResultWinCoin = 0;

            yield return OpenLinkStartPopup();

            yield return new WaitUntil(() => linkFeature.IsInitializeEnd);

            yield return base.LinkRespinStartState();

            yield return WaitForSeconds(introAnimTime);
        }

        private List<string> GetCurrentSymbolList()
        {
            List<string> featureStartSymbolIDList = new List<string>();

            for (int mainSymbolIndex = reelGroup.Row - 1; mainSymbolIndex >= 0; mainSymbolIndex--)
            {
                for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
                {
                    string symbolID = ReelGroup.GetReel(reelIndex).GetMainSymbol(mainSymbolIndex, true).id;
                    featureStartSymbolIDList.Add(symbolID);
                }
            }

            return featureStartSymbolIDList;
        }

        // 링크 리스핀 종료
        protected override IEnumerator LinkRespinEndState()
        {
            ClearAverageBet();

            DispatchChangeTotalBet(Info.CurrentTotalBet);

            yield return OpenLinkResultPopup();

            normalBGM.Play();

            // 전용 팝업 이후 빅윈
            var winGrade = SlotMachineUtils.GetWinGrade(linkResultWinCoin, Info.CurrentTotalBet);

            if (IsBigWin(linkResultWinCoin))
            {
                yield return HitBigwin(winGrade, linkResultWinCoin, linkResultWinCoin, false, false);
            }
            else
            {
                yield return HitNormal(winGrade, linkResultWinCoin, linkResultWinCoin, false, false);
            }

            extraInfo.OnResetInfo();

            yield return base.LinkRespinEndState();
        }

        // Connected OpenLinkPopup Animator
        private void LinkInTransition()
        {
            Debug.Log("** LinkInTransition");
            if (extraInfo.IsTriplePot)
            {
                machineBG.SetTrigger(TRIGGER_SUPER_LINK);
                animatorBG.SetTrigger(TRIGGER_SUPER_LINK);
            }
            else
            {
                machineBG.SetTrigger(TRIGGER_LINK);
                animatorBG.SetTrigger(TRIGGER_LINK);
            }

            normalBGM.Stop();

            extraInfo.SetPotFeature();
            superBonusUI.gameObject.SetActive(false);

            DispatchUseSpinUI(true);
            DispatchChangeLinkRespins(Info.LinkRespinCurrentCount, Info.LinkRespinRemainCount);
            SetReelMode(ReelMode.LinkRespin);

            OnJackpotHUD(true);

            if (extraInfo.IsSuperLink)
            {
                SetAverageBet(extraInfo.AverageBet);
            }

            StartCoroutine(linkFeature.Initialize(currentSymbolList));
        }

        // Connected LinkResultPopup Animator
        private void LinkOutTransition()
        {
            Debug.Log("** LinkOutTransition");
            linkFeature.LinkEnd();

            machineBG.SetTrigger(TRIGGER_NORMAL);
            animatorBG.SetTrigger(TRIGGER_NORMAL);

            extraInfo.SetNormalFeature();

            superBonusUI.gameObject.SetActive(true);
            superBonusUI.Initialize(extraInfo.SuperBonusLevel, false);

            var reelMode = CalculateReelMode(Info.IsFreespin, Info.IsLinkRespin, Info.IsRespin);
            SetReelMode(reelMode);

            OnJackpotHUD(false);

            HideDimPanel();

            potProgress.UpdatePotLevel(true);

            Info.UpdateNextReels();
            DispatchUseSpinUI(false);
            DispatchReelStripChanged();
            UpdateMainReelStrips();
            InitializeRegularReels();

            OnStopLinkBGM();
        }

        public IEnumerator SetLinkWinCoin(long coin, bool useInterval = true)
        {
            if (useInterval)
            {
                yield return WaitForSeconds(linkResultCoinIntervalTime);
            }

            linkResultWinCoin += coin;
            Debug.LogFormat("SetLinkWinCoin : {0} - {1}", coin, linkResultWinCoin);
            linkWinCoinSound.Play();
            DispatchTotalWinCoins(linkResultWinCoin, linkWinCoinTime);
        }

        private void OnTotalBetChangeHandler(long totalBet)
        {
            var totalBetIndex = Info.TotalBetIndex;

            OnIdlePanda();

            linkSymbolManager.TotalBetChangeUpdateInfo(totalBet);

            if (totalBetIndex == currentTotalBetIndex)
            {
                return;
            }

            currentTotalBetIndex = totalBetIndex;

            StopRepeatWinningLines();
        }

        private void OnAverageBetChangeHandler(long? averageBet)
        {
            if (averageBet != null)
            {
                linkSymbolManager.TotalBetChangeUpdateInfo(averageBet);
            }
        }

        protected override void OnReelModeChanged(ReelMode reelMode)
        {
            switch (reelMode)
            {
                case ReelMode.LinkRespin:
                    {
                        spinPanel.SetActive(false);

                        dimmedSymbolData.Enable = false;

                        linkSymbolManager.UpdateReelGroup();

                        normalFeature.SetActive(false);
                        linkFeature.gameObject.SetActive(true);
                    }
                    break;
                case ReelMode.Regular:
                    {
                        spinPanel.SetActive(true);
                        dimmedSymbolData.Enable = true;

                        linkSymbolManager.UpdateReelGroup();

                        normalFeature.SetActive(true);
                        linkFeature.gameObject.SetActive(false);

                        OnIdlePanda();
                    }
                    break;
            }
        }

        protected override void ReelStripJumpToStartIndex(EnterData enterData)
        {
            Debug.Log("ReelStripJumpToStartIndex");
            if (Info.IsLinkRespin == true && Info.IsLinkRespinStart == false)
            {
                base.ReelStripJumpToStartIndex(enterData);
            }
            else
            {
                base.ReelStripJumpToStartIndex(enterData);
                InitializeRegularReels();
            }
        }

        private void InitializeRegularReels()
        {
            if (Info.IsLinkRespin) return;

            for (int reelIndex = 0, count = ReelGroup.Column; reelIndex < count; reelIndex++)
            {
                var reel = ReelGroup.GetReel(reelIndex);
                reel.Initialize();
            }

            Symbol.RemoveAllSubstituteVisualsAtGlobal();

            for (int reelIndex = 0, count = ReelGroup.Column; reelIndex < count; reelIndex++)
            {
                var reel = ReelGroup.GetReel(reelIndex);

                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex, true);
                    if (symbol.IsLink)
                    {
                        symbol.AddSubstituteVisual(SymbolVisualType.Idle, 0, loopVisual, 0);
                    }
                }
            }

            reelGroupHelper.SetVisualAllSymbols(SymbolVisualType.Idle);
        }

        private void UpdateMainReelStrips()
        {
            feed.Feed(Info.GetReels());
            for (int reelIndex = 0; reelIndex < ReelGroup.Column; reelIndex++)
            {
                var reelstrip = ReelGroup.GetReel(reelIndex).ReelStrip;
                int startIndex = UnityEngine.Random.Range(1, reelstrip.Length);
                reelstrip.Jump(startIndex);
            }
        }

        private void ConvertReelStrip()
        {
            Debug.Log("ConvertReelStrip");
            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                var reelStrip = reelGroup.GetReel(reelIndex).ReelStrip;

                for (int i = 0; i < reelStrip.Length; i++)
                {
                    var symbolID = int.Parse(reelStrip.GetID(i));
                    if (symbolID > 20 && symbolID < 40)
                    {
                        var randomID = UnityEngine.Random.Range(1, 12);
                        reelStrip.SetID(i, randomID.ToString());
                    }

                }
            }
        }

        public void OnTotalBetRequestMap(int index)
        {
            var totalBet = Info.GetTotalBet(index);
            DispatchTotalBetRequest(totalBet);
        }

        private void OnPlayLinkBGM()
        {
            int mixCount = GetPotMixCount();

            if (mixCount == 1)
            {
                if (extraInfo.CurrentPotFeature == PotFeatureType.Purple) linkBGM_Purple.Play();
                else if (extraInfo.CurrentPotFeature == PotFeatureType.Red) linkBGM_Red.Play();
                else if (extraInfo.CurrentPotFeature == PotFeatureType.Blue) linkBGM_Blue.Play();
            }
            else if (mixCount == 2)
            {
                linkBGM_Mix.Play();
            }

            else if (mixCount == 3)
            {
                linkBGM_Super.Play();
            }
        }

        private int GetPotMixCount()
        {
            int mixCount = 0;

            if (extraInfo.CurrentPotFeature.HasFlag(PotFeatureType.Purple)) mixCount++;
            if (extraInfo.CurrentPotFeature.HasFlag(PotFeatureType.Red)) mixCount++;
            if (extraInfo.CurrentPotFeature.HasFlag(PotFeatureType.Blue)) mixCount++;

            return mixCount;
        }
        private void OnStopLinkBGM()
        {
            linkBGM_Blue.Stop();
            linkBGM_Purple.Stop();
            linkBGM_Red.Stop();
            linkBGM_Mix.Stop();
            linkBGM_Super.Stop();
        }
        //----------------------------------------------------------------------------
        // Jackpot
        //----------------------------------------------------------------------------
        protected override void DispatchChangeJackpotCoins(List<long> jackpotList)
        {
            if (CurrentReelMode == ReelMode.LinkRespin) return;

            cachedJackpotCoins.Clear();

            var customJackpot = extraInfo.GetJackpotList(jackpotList.Count);

            for (int i = 0; i < customJackpot.Length; i++)
            {
                if (customJackpot[i] > jackpotList[i])
                {
                    jackpotList[i] = customJackpot[i];
                }
            }
            cachedJackpotCoins.AddRange(jackpotList);

            Debug.LogFormat("** DispatchJackpot : {0}", cachedJackpotCoins.ToEachString());

            base.DispatchChangeJackpotCoins(jackpotList);
        }

        public long GetCachedJackpotCoins(int jackpotGrade, long jackpotValue = 0)
        {
            int jackpotIndex = Mathf.Abs(jackpotGrade) - 1;
            long winCoins = (jackpotValue == 0) ? cachedJackpotCoins[jackpotIndex] : jackpotValue;

            return winCoins;
        }

        public void SetCachedJackpot()
        {
            DispatchChangeJackpotCoins(Info.GetSuggestJackpotCoins());
        }

        private void OnLinkStartPopupButtonClick()
        {
            Debug.Log("**OnLinkStartPopupButtonClick");
            potIntroSound.Play();
            OnPlayLinkBGM();
        }

        //----------------------------------------------------------------------------
        // Popup
        //----------------------------------------------------------------------------
        private IEnumerator OpenLinkStartPopup()
        {
            yield return PopupSystem.Instance.Open<LinkStartPopup1061>(slotAssets, LINK_START_POPUP)
                                              .OnInitialize(p =>
                                              {
                                                  p.Initialize(extraInfo.CurrentPotFeature, LinkInTransition, OnLinkStartPopupButtonClick, holdType != HoldType.On, useVibrate);
                                                  CheckAutoPlayForFeatureStart();
                                              })
                                              .SetBackgroundAlpha(0.0f)
                                              .Cache()
                                              .WaitForClose();
        }

        public IEnumerator OpenJackpotPopup(int jackpotGrade, long upgradeValue, long jackpotValue = 0)
        {
            int jackpotIndex = Mathf.Abs(jackpotGrade) - 1;
            long winCoins = (jackpotValue == 0) ? cachedJackpotCoins[jackpotIndex] : jackpotValue;

            winCoins += upgradeValue;

            Debug.LogFormat("OpenJackpotPopup : winCoins {0}, index {1}, upgradeValue {2}", winCoins, jackpotIndex, upgradeValue);

            yield return PopupSystem.Instance.Open<JackpotPopup1061>(slotAssets, JACKPOT_POPUP)
                                          .OnInitialize(p =>
                                          {
                                              p.Open(jackpotGrade, winCoins, jackpotTextAnimTime);
                                          })
                                          .Cache()
                                          .WaitForClose();

            if (extraInfo.IsSuperLink == false)
            {
                cachedJackpotCoins[jackpotIndex] = Info.GetSuggestFinalJackpotCoins()[jackpotIndex];
                DispatchChangeJackpotCoins(cachedJackpotCoins, false);
            }
        }

        private IEnumerator OpenLinkResultPopup()
        {
            Action shareCallback = DispatchShare;
            if (useShare == false)
            {
                shareCallback = null;
            }


            yield return PopupSystem.Instance.Open<LinkResultPopup1061>(slotAssets, LINK_RESULT_POPUP)
                                                                    .OnInitialize((LinkResultPopup1061 popup) =>
                                                                    {
                                                                        popup.Initialize(linkResultWinCoin,
                                                                                         holdType != HoldType.On,
                                                                                         LinkOutTransition,
                                                                                         shareCallback);
                                                                    })
                                                                    .SetBackgroundAlpha(0.0f)
                                                                    .Cache()
                                                                    .WaitForClose();
        }
    }
}