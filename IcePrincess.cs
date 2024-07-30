using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Popup;
using SlotGame.Sound;
using SlotGame.UI;

namespace SlotGame.Machine.S1079
{
    public class SlotMachine1079 : SlotMachine
    {
#pragma warning disable 0649
        [SerializeField] private VerticalTumblingReel[] reelList;

        [SerializeField] private BuyFeatureUI buyFeature;

        [SerializeField] private float tumbleWinDuration;
        [SerializeField] private Animator bgAnim;
        [SerializeField] private float startTumbleDelayTime;
        [SerializeField] private float transtionDelayTime;
        [SerializeField] private MultipleUI1079 multipleUI;
        [SerializeField] private float impactTime;
        [SerializeField] private float multiUIOpenDelayTime;
        [SerializeField] private float transitionTime;
        [SerializeField] private float reelEffectSoundStopDelayTime;
        [SerializeField] private float dispatchWinCoinsDelayOnTurboMode = 0.3f;

        [Header("TotalBet Time")]
        [SerializeField] private float TbUnderTime = 1.0f;
        [SerializeField] private float TbUpperTime = 2.0f;
        [SerializeField] private float TbTurboTime = 0.5f;

        [Header("Sounds")]
        [SerializeField] private SoundPlayer freeBGM;

        [SerializeField] private SoundPlayer tumbleSound1;
        [SerializeField] private SoundPlayer tumbleSound2;
        [SerializeField] private SoundPlayer tumbleSound1Fast;
        [SerializeField] private SoundPlayer tumbleSound2Fast;
        [SerializeField] private SoundPlayer tumbleSound1Turbo;
        [SerializeField] private SoundPlayer tumbleSound2Turbo;
        [SerializeField] private SoundPlayer iceBreakSound;
        [SerializeField] private SoundPlayer stopSound;
        [SerializeField] private SoundPlayer reelEffectSound;
        [SerializeField] private SoundPlayer introSound;

        [Header("Value")]
        [SerializeField] private float normalDropFillDuration;
        [SerializeField] private float quickDropFillDuration;
        [SerializeField] private float turboDropFillDuration;
        [SerializeField] private float normalNewFillDelay;
        [SerializeField] private float quickNewFillDelay;
        [SerializeField] private float turboNewFillDelay;
#pragma warning restore 0649

        private ExtraInfo1079 extraInfo;
        private SlotMachineInfo1079 info1079;
        private TumblingReelController tumblingReelController;

        private readonly int PAY_SOUND_NORMAL_INDEX = 0;
        private readonly int PAY_SOUND_FREE_INDEX = 1;

        private readonly string SYMBOL_ID_OVER = "9";

        private readonly string ANIM_TRIGGER_BASE = "Base";
        private readonly string ANIM_TRIGGER_FREE = "Free";
        private readonly string ANIM_TRIGGER_TRANSITION = "{0}To{1}";

        //-------------------------------------------------------------------------------
        // Popup
        //-------------------------------------------------------------------------------
        private readonly string FREE_START_POPUP = "FreeStartPopup1079";
        private readonly string FREE_END_POPUP = "FreeEndPopup1079";
        private readonly string RETRIGGER_POPUP = "FreeRetriggerPopup1079";
        //-------------------------------------------------------------------------------

        private SymbolVisualType appearVisual = SymbolVisualType.Etc2;
        private SymbolVisualType impactVisual = SymbolVisualType.Etc3;

        private bool isEnterRespin = false;

        protected override SlotMachineInfo CreateSlotMachineInfo(int reelColumn, int reelRow, SlotOrientationType slotOrientationType, JackpotWinType jackpotWinType = JackpotWinType.Normal)
        {
            info1079 = new SlotMachineInfo1079(reelColumn, reelRow, slotOrientationType, jackpotWinType);
            return info1079;
        }

        protected override void PostInitialize(EnterData enterData)
        {
            extraInfo = this.GetComponent<ExtraInfo1079>();
            extraInfo.Initialize();

            base.PostInitialize(enterData);
        }

        protected override void SetupPopups()
        {
            base.SetupPopups();

            PopupSystem.Instance.AddCachedPopup<FreeSpinStartPopup1079>(slotAssets, FREE_START_POPUP);
            PopupSystem.Instance.AddCachedPopup<FreeEndPopup1079>(slotAssets, FREE_END_POPUP);
            PopupSystem.Instance.AddCachedPopup<ExtraSpinPopup>(slotAssets, RETRIGGER_POPUP);
        }

        protected override void RemovePopups()
        {
            base.RemovePopups();

            PopupSystem.Instance.RemoveCachedPopup(FREE_START_POPUP);
            PopupSystem.Instance.RemoveCachedPopup(FREE_END_POPUP);
            PopupSystem.Instance.RemoveCachedPopup(RETRIGGER_POPUP);
        }

        // 슬롯머신 진입시 최초 한번 실행되어야 할 스테이트
        // 이 부분에 슬롯머신 진입 애니메이션 등을 구현해야함.
        protected override IEnumerator EnterState()
        {
            reelGroup.OnStateEnd.AddListener(OnReelStateEnd);

            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                var reel = reelGroup.GetReel(reelIndex).GetComponent<VerticalTumblingReel>();
                reel.onDropStart.AddListener(OnDropStart);
            }

            tumblingReelController = tumblingReelControllerDic[reelGroup];

            SetEnterState();

            yield return SetRemainTumbleRespin();

            yield return base.EnterState();
        }

        private void OnDropStart(VerticalTumblingReel.TumblingState state)
        {
            if (state == VerticalTumblingReel.TumblingState.DropFill)
            {
                if (CurrentPlayModeInProgress == PlayMode.Turbo)
                {
                    tumbleSound1Turbo.Play();
                }
                else if (CurrentPlayModeInProgress == PlayMode.Fast)
                {
                    tumbleSound1Fast.Play();
                }
                else
                {
                    tumbleSound1.Play();
                }
            }
            else if (state == VerticalTumblingReel.TumblingState.DropNewFill)
            {
                if (CurrentPlayModeInProgress == PlayMode.Turbo)
                {
                    tumbleSound2Turbo.Play();
                }
                else if (CurrentPlayModeInProgress == PlayMode.Fast)
                {
                    tumbleSound2Fast.Play();
                }
                else
                {
                    tumbleSound2.Play();
                }
            }
        }

        public void OnStopReelEffectSound()
        {
            StartCoroutine(OnStopReelEffectSoundWithDelay());
        }

        private IEnumerator OnStopReelEffectSoundWithDelay()
        {
            yield return WaitForSeconds(reelEffectSoundStopDelayTime);
            reelEffectSound.Stop();
        }

        private void SetEnterState()
        {
            if (Info.IsRespin && extraInfo.BaseTumbleSymbolPos.Count > 0)
            {
                isEnterRespin = true;
            }

            if (extraInfo.TotalWinCoins > 0)
            {
                DispatchTotalWinCoins(extraInfo.TotalWinCoins, 0.0f);
            }

            if (Info.IsFreespin)
            {
                winSoundPlayer.SelectJukeBox(PAY_SOUND_FREE_INDEX);
                multipleUI.OnInitialize(extraInfo.TumbleMulValue);
                bgAnim.SetTrigger(ANIM_TRIGGER_FREE);

                freeBGM.Play();

                info1079.SetRemainTumble(true);
            }
            else
            {
                winSoundPlayer.SelectJukeBox(PAY_SOUND_NORMAL_INDEX);
                bgAnim.SetTrigger(ANIM_TRIGGER_BASE);
                if (isEnterRespin == false)
                {
                    introSound.Play();
                }
            }
        }

        private void OnReelStateEnd(int reelIndex, ReelState state, BaseReel reel)
        {
            if (state == ReelState.Slowdown)
            {
                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    // 오버사이즈 심볼 오더 변경
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);
                    if (symbol.id == SYMBOL_ID_OVER)
                    {
                        var addOrder = (reelGroup.Row - mainSymbolIndex - 1) + reelIndex;
                        symbol.SetAdditionalSortingOrder(addOrder);
                        symbol.GetVisualElement().Execute();
                    }

                    if (symbol.IsScatter)
                    {
                        if (symbol.GetVisual().visualType == appearVisual) continue;

                        symbol.AddSubstituteVisual(SymbolVisualType.Idle, 0, SymbolVisualType.Idle, 1);
                        symbol.SetVisual(SymbolVisualType.Idle);
                    }
                }

                var tumbleReel = reel.GetComponent<VerticalTumblingReel>();
                if (tumbleReel.GetIsSymbolHitTumbling() == false)
                {
                    stopSound.Play();
                }
            }
        }

        // 기본 상태.
        // 기본적으로 유저의 입력을 기다리지만 프리스핀, 리스핀, 오토스핀등의 상황에선 자동으로 스핀 상태로 변경.
        protected override IEnumerator IdleState()
        {
            yield return base.IdleState();

            MachineUI.ignoreReelModeEvent = false;
        }

        private IEnumerator SetRemainTumbleRespin()
        {
            if (Info.IsFreespin)
            {
                if (extraInfo.FreeSymbolList != null && extraInfo.FreeSymbolList.Count > 0)
                {
                    Debug.Log("SetFreeTumbleRespin");
                    SetRestoreSymbol(true);

                    yield return WaitForSeconds(startTumbleDelayTime);
                }
            }
            else
            {
                if (extraInfo.BaseSymbolList != null && extraInfo.BaseSymbolList.Count > 0)
                {
                    Debug.Log("SetBaseTumbleRespin");
                    SetRestoreSymbol();

                    yield return WaitForSeconds(startTumbleDelayTime);
                }
            }
        }

        private void SetDropFillDuration(float duration)
        {
            for (int i = 0; i < reelList.Length; i++)
            {
                VerticalTumblingReel reel = reelList[i];
                reel.dropFillDuration = duration;
            }
        }

        private void SetNewFillDelay(float delay)
        {
            for (int i = 0; i < reelList.Length; i++)
            {
                VerticalTumblingReel reel = reelList[i];
                reel.dropNewFillDelay = delay;
            }
        }

        // 스핀을 시작하는 스테이트.
        protected override IEnumerator SpinStartState()
        {
            FreespinMultiReset();

            if (CurrentPlayModeInProgress == PlayMode.Normal)
            {
                SetNewFillDelay(normalNewFillDelay);
                SetDropFillDuration(normalDropFillDuration);
            }
            else if (CurrentPlayModeInProgress == PlayMode.Fast)
            {
                SetNewFillDelay(quickNewFillDelay);
                SetDropFillDuration(quickDropFillDuration);
            }
            else if (CurrentPlayModeInProgress == PlayMode.Turbo)
            {
                SetNewFillDelay(turboNewFillDelay);
                SetDropFillDuration(turboDropFillDuration);
            }

            yield return base.SpinStartState();
        }

        private void FreespinMultiReset(bool isBigwin = false)
        {
            if (Info.IsFreespin == false) return;

            if (isBigwin)
            {
                multipleUI.OnReset();
            }
            else
            {
                if (tumblingReelController.GetTumbling() == false)
                {
                    multipleUI.OnReset();
                }
            }
        }

        public override void StartSpin()
        {
            base.StartSpin();
        }

        public override void StopSpin(SpinData spinData)
        {
            base.StopSpin(spinData);

            extraInfo.OnUpdateInfo();
        }

        // 스핀 응답을 받고 Info 를 업데이트 한 뒤 실제 릴에게 stop 명령을 내리기 전 스테이트
        protected override IEnumerator SpinPreStopState()
        {
            // 리스핀 발동 (텀블 히트) 시 히트 연출등을 스킵하지 않기 위해 PlayMode값을 Normal로 변경함
            if (Info.IsRespinStart)
            {
                CurrentPlayModeInProgress = PlayMode.Normal;
            }

            yield return base.SpinPreStopState();
        }
        
        // 스핀이 멈추고 결과를 시작 되는 스테이트
        // 잭팟, 빅윈 등에 분기점
        protected override IEnumerator ResultStartState()
        {
            //---------------------------------------------------------------------------------------------------------------------
            // Result Start
            //---------------------------------------------------------------------------------------------------------------------
            yield return null;

            WinGrade winGrade = Info.SpinResult.WinGrade;
            if (Info.IsFreespin == false && Info.IsFreespinEnd == false)
            {
                if (Info.IsRespinEnd)
                {
                    winGrade = SlotMachineUtils.GetWinGrade(extraInfo.TotalWinCoins, Info.CurrentTotalBet);
                }
            }
            else if (Info.IsFreespin && Info.IsFreespinEnd == false && Info.IsRespinEnd)
            {
                winGrade = SlotMachineUtils.GetWinGrade(extraInfo.TumbleWinCoins, Info.CurrentTotalBet);
            }

            if (winGrade == WinGrade.None)
            {
                if (extraInfo.IsScatterTumble)
                {
                    NextState(SlotMachineState.FREESPIN_END);
                }
                else
                {
                    var nextState = GetStateBeforeEnd(Info.IsExtraSpin, Info.IsFreespinStart, Info.IsFreespinEnd, Info.IsRespin, Info.IsFreeChoiceStart);
                    NextState(nextState);
                }
            }
            else
            {
                StartCoroutine(ReadyToResultSkip(RESULT_SKIP_DELAY));

                NextState(SlotMachineState.HIT);
            }
            //---------------------------------------------------------------------------------------------------------------------

            Debug.LogFormat("** ResultStartState : [FreeStart] {0} [Free] {1} [FreeEnd] {2} [Extra] {3} [RespinStart] {4}, [Respin] {5} , [RespinEnd] {6}",
                Info.IsFreespinStart, Info.IsFreespin, Info.IsFreespinEnd, Info.IsExtraSpin, Info.IsRespinStart, Info.IsRespin, Info.IsRespinEnd);

            float winDuration = winCoinDuration;

            if (Info.IsRespinEnd)
            {
                if (Info.IsFreespinStart || Info.IsExtraSpin)
                {
                    var winCoins = extraInfo.TotalWinCoins;
                    info1079.AddTotalWinCoin(winCoins);
                    winDuration = (winCoins < Info.CurrentTotalBet) ? TbUnderTime : TbUpperTime;
                    DispatchTotalWinCoins(winCoins, winDuration);
                }
                else
                {
                    if (Info.IsFreespin || Info.IsFreespinEnd)
                    {
                        var winCoins = extraInfo.TotalWinCoins;
                        info1079.AddTotalWinCoin(winCoins);

                        if (Info.SpinResult.WinCoinsHit > 0)
                        {
                            winDuration = (Info.SpinResult.WinCoinsHit < Info.CurrentTotalBet) ? TbUnderTime : TbUpperTime;
                            DispatchWinCoins(Info.SpinResult.WinCoinsHit, winDuration, WinGrade.None);
                        }

                        DispatchTotalWinCoins(winCoins, winDuration);
                    }
                    else
                    {
                        var winCoins = extraInfo.TotalWinCoins;
                        info1079.SetTotalWinCoin(winCoins);

                        winDuration = (winCoins < Info.CurrentTotalBet) ? TbUnderTime : TbUpperTime;

                        if (IsSkipSymbolHitOnTumblingReel())
                        {
                            StartCoroutine(DispatchTotalWinCoinsWithDelay(dispatchWinCoinsDelayOnTurboMode, winCoins, TbTurboTime, true));
                        }
                        else
                        {
                            DispatchTotalWinCoins(winCoins, winDuration);
                        }

                        info1079.SetRemainTumble(false);
                    }
                }
            }
            // IsRespin
            else
            {
                if (Info.IsFreespinStart || Info.IsExtraSpin)
                {
                    var winCoins = extraInfo.TotalWinCoins;
                    info1079.AddTotalWinCoin(winCoins);

                    winDuration = (winCoins < Info.CurrentTotalBet) ? TbUnderTime : TbUpperTime;

                    if (Info.IsFreespin || Info.IsFreespinEnd)
                    {
                        if (Info.SpinResult.WinCoinsHit > 0)
                        {
                            winDuration = (Info.SpinResult.WinCoinsHit < Info.CurrentTotalBet) ? TbUnderTime : TbUpperTime;
                            DispatchWinCoins(Info.SpinResult.WinCoinsHit, winDuration, WinGrade.None);
                        }
                    }

                    DispatchTotalWinCoins(winCoins, winDuration);
                }
                else
                {
                    var winCoins = extraInfo.TotalWinCoins;
                    info1079.AddTotalWinCoin(winCoins);
                    winDuration = (winCoins < Info.CurrentTotalBet) ? TbUnderTime : TbUpperTime;

                    if (Info.IsFreespin || Info.IsFreespinEnd)
                    {
                        if (Info.SpinResult.WinCoinsHit > 0)
                        {
                            winDuration = (Info.SpinResult.WinCoinsHit < Info.CurrentTotalBet) ? TbUnderTime : TbUpperTime;
                            DispatchWinCoins(Info.SpinResult.WinCoinsHit, winDuration, WinGrade.None);
                        }
                    }

                    if (IsSkipSymbolHitOnTumblingReel())
                    {
                        StartCoroutine(DispatchTotalWinCoinsWithDelay(dispatchWinCoinsDelayOnTurboMode, winCoins, TbTurboTime, true));
                    }
                    else
                    {
                        DispatchTotalWinCoins(winCoins, winDuration);
                    }
                }
            }
        }
        
        protected override IEnumerator RespinEndState()
        {
            yield return null;

            var nextState = GetStateBeforeEnd(Info.IsExtraSpin, Info.IsFreespinStart, Info.IsFreespinEnd, Info.IsRespin, Info.IsFreeChoiceStart, Info.IsLinkRespin, Info.IsLinkRespinStart, Info.IsLinkRespinEnd, false, false);
            NextState(nextState);
        }

        // 결과 종료 스테이트
        // idle 상태로 돌아감
        protected override IEnumerator ResultEndState()
        {
            if (Info.IsStackedTotalWin == false)
            {
                EarnCoins(Info.TotalWinCoins);
                EarnPearls(Info.TotalWinPearls);
                EarnTickets(Info.TotalWinTickets);
            }

            yield return WaitForSeconds(GetResultEndDelay());

            if (updateNextReelType == UpdateNextReelType.ResultEnd)
            {
                Info.UpdateNextReels();
            }

            if (Info.IsReelChanged)
            {
                DispatchReelStripChanged();
            }

            yield return null;

            NextState(SlotMachineState.IDLE);
        }

        protected override float GetResultEndDelay()
        {
            if (Info.IsRespin || Info.IsRespinEnd || Info.IsFreespin || Info.IsFreespinEnd)
            {
                return 0.0f;
            }
            else
            {
                return base.GetResultEndDelay();
            }
        }

        private IEnumerator HitTumbleSymbols(float duration)
        {
            if (Info.IsFreespin && Info.IsFreespinEnd == false)
            {
                foreach (var pos in extraInfo.FreeTumbleSymbolPos)
                {
                    var symbol = reelGroup.GetReel(pos.reelIndex).GetMainSymbol(pos.mainIndex);
                    symbol.SetVisual(SymbolVisualType.Hit);
                }
            }
            else
            {
                foreach (var pos in extraInfo.BaseTumbleSymbolPos)
                {
                    var symbol = reelGroup.GetReel(pos.reelIndex).GetMainSymbol(pos.mainIndex);
                    symbol.SetVisual(SymbolVisualType.Hit);
                }
            }

            yield return WaitForSeconds(duration);
        }

        protected override void DispatchTotalWinCoins(long coins, float duration, bool ignorePlayMode = false)
        {
            if (Info.IsFreespinStart || Info.IsExtraSpin) return;
            base.DispatchTotalWinCoins(coins, duration, ignorePlayMode);
        }

        private IEnumerator DispatchTotalWinCoinsWithDelay(float delay, long coins, float duration, bool ignorePlayMode = false)
        {
            yield return new WaitForSeconds(delay);

            DispatchTotalWinCoins(coins, duration, ignorePlayMode);
        }

        protected override IEnumerator HitSymbols(float delayTime)
        {
            if (IsSkipSymbolHitOnTumblingReel())
            {
                yield break;
            }

            if (Info.SpinResult.GetWinInfoCount(0) > 0)
            {
                yield return HitSymbosAllBeforeRepeatWinningLines(Info.SpinResult, delayTime, 0);
            }
            else
            {
                yield return HitTumbleSymbols(delayTime);
            }
        }
        protected override IEnumerator PrepareTumblingState()
        {
            Debug.LogFormat("PrepareTumblingState : isEnterRespin {0}, IsFreespinEnd {1}, isExtraSpin {2}",
                isEnterRespin, Info.IsFreespinEnd, Info.IsExtraSpin);
            if (isEnterRespin || Info.IsFreespinEnd || Info.IsExtraSpin)
            {
                yield return CustomTumblingState();
            }
            else
            {
                if (IsTumblingSpin())
                {
                    Dictionary<MachineItem, List<List<int>>> hitSymbolPositionDic = new Dictionary<MachineItem, List<List<int>>>();

                    // 심볼 히트 정보 데이터 저장 및 심볼 비주얼 변경
                    machineGroup.ForEach(item =>
                    {
                        List<List<int>> hitSymbolPositionsList = new List<List<int>>();

                        for (int column = 0; column < reelGroup.Column; column++)
                        {
                            hitSymbolPositionsList.Add(new List<int>());
                        }

                        for (int winInfoIdx = 0; winInfoIdx < Info.SpinResult.GetWinInfoCount(item.index); winInfoIdx++)
                        {
                            SpinResult.WinInfo winInfo = Info.SpinResult.GetWinInfo(item.index, winInfoIdx);

                            for (int reelIdx = 0; reelIdx < winInfo.hitSymbolPositions.Count; reelIdx++)
                            {
                                BaseReel reel = reelGroup.GetReel(reelIdx);
                                List<int> position = winInfo.hitSymbolPositions[reelIdx];

                                if (position != null)
                                {
                                    hitSymbolPositionsList[reelIdx].AddRange(position);
                                    hitSymbolPositionsList = hitSymbolPositionsList.Distinct().ToList();
                                }
                            }
                        }

                        hitSymbolPositionDic.Add(item, hitSymbolPositionsList);

                        // 심볼 비주얼 변경
                        TumblingReelController tumblingController = tumblingReelControllerDic[item.reelGroup];

                        if (tumblingController == null)
                        {
                            Debug.LogError("[SlotMachineFSM] TumblingReelController is NULL.");
                        }
                        else
                        {
                            tumblingController.RemoveSymbol(hitSymbolPositionsList);
                            tumblingController.SetTumbling(true);
                            SetIgnoreInterval(true);

                            iceBreakSound.Play();

                            if (Info.IsFreespin || Info.IsFreespinEnd)
                            {
                                multipleUI.UpgradeNext();
                            }
                        }
                    });

                    yield return new WaitForSeconds(removeSymboDelayTime);

                    if (tumblingReelController.GetTumbling())
                    {
                        var hitPos = hitSymbolPositionDic[machineGroup.GetItem(0)];
                        yield return OnTumbleAppear(hitPos);
                    }
                }
                else
                {
                    machineGroup.ForEach(item =>
                    {
                        TumblingReelController tumblingController = tumblingReelControllerDic[item.reelGroup];

                        if (tumblingController != null)
                        {
                            tumblingController.SetTumbling(false);
                        }
                    });
                }

                yield return null;

                if (tumblingReelController.GetTumbling() == false)
                {
                    reelGroupHelper.RemoveAllSubstituteVisualSymbols();
                    reelGroupHelper.SetVisualAllMainSymbols(SymbolVisualType.Idle);
                }

                NextState(SlotMachineState.SPIN_START);
            }
        }

        private IEnumerator CustomTumblingState()
        {
            // 스핀을 받지않고 이전 결과로 텀블 진행후 다음 스핀으로 넘어간다
            Debug.LogFormat("CustomPrepareTumblingState -- respinSTart {0}, respin {1}", Info.IsRespinStart, Info.IsRespin);

            if (Info.IsRespinStart == true || Info.IsRespin == true)
            {
                Dictionary<MachineItem, List<List<int>>> hitSymbolPositionDic = new Dictionary<MachineItem, List<List<int>>>();

                // 심볼 히트 정보 데이터 저장 및 심볼 비주얼 변경
                machineGroup.ForEach(item =>
                {
                    List<List<int>> hitSymbolPositionsList = new List<List<int>>();

                    for (int column = 0; column < reelGroup.Column; column++)
                    {
                        hitSymbolPositionsList.Add(new List<int>());
                    }

                    if (Info.IsFreespin && extraInfo.IsScatterTumble == false)
                    {
                        foreach (var posInfo in extraInfo.FreeTumbleSymbolPos)
                        {
                            hitSymbolPositionsList[posInfo.reelIndex].Add(posInfo.mainIndex);
                        }
                    }
                    else
                    {
                        foreach (var posInfo in extraInfo.BaseTumbleSymbolPos)
                        {
                            hitSymbolPositionsList[posInfo.reelIndex].Add(posInfo.mainIndex);
                        }
                    }


                    hitSymbolPositionsList = hitSymbolPositionsList.Distinct().ToList();

                    hitSymbolPositionDic.Add(item, hitSymbolPositionsList);

                    // 심볼 비주얼 변경
                    TumblingReelController tumblingController = tumblingReelControllerDic[item.reelGroup];

                    if (tumblingController == null)
                    {
                        Debug.LogError("[SlotMachineFSM] TumblingReelController is NULL.");
                    }
                    else
                    {
                        tumblingController.RemoveSymbol(hitSymbolPositionsList);
                        tumblingController.SetTumbling(true);
                        SetIgnoreInterval(true);

                        iceBreakSound.Play();

                        if (Info.IsFreespin || Info.IsFreespinEnd)
                        {
                            multipleUI.UpgradeNext();
                        }
                    }
                });

                yield return new WaitForSeconds(removeSymboDelayTime);

                if (tumblingReelController.GetTumbling())
                {
                    var hitPos = hitSymbolPositionDic[machineGroup.GetItem(0)];

                    yield return OnTumbleAppear(hitPos);
                }
            }
            else
            {
                machineGroup.ForEach(item =>
                {
                    TumblingReelController tumblingController = tumblingReelControllerDic[item.reelGroup];

                    if (tumblingController != null)
                    {
                        tumblingController.SetTumbling(false);
                    }
                });
            }

            yield return null;

            if (tumblingReelController.GetTumbling() == false)
            {
                reelGroupHelper.RemoveAllSubstituteVisualSymbols();
                reelGroupHelper.SetVisualAllMainSymbols(SymbolVisualType.Idle);
            }

            info1079.ResetFreespin();
            isEnterRespin = false;

            NextState(SlotMachineState.SPIN_START);
        }

        private void SetIgnoreInterval(bool ignore)
        {
            reelGroup.ignoreInterval = ignore;
        }
        // 심볼 히트 스테이트
        protected override IEnumerator HitState()
        {
            yield return WaitForSeconds(hitDelayTime);

            WinGrade winGrade = WinGrade.None;

            if (Info.IsFreespin)
            {
                winGrade = SlotMachineUtils.GetWinGrade(extraInfo.TumbleWinCoins, Info.CurrentTotalBet);
            }
            else
            {
                winGrade = SlotMachineUtils.GetWinGrade(extraInfo.TotalWinCoins, Info.CurrentTotalBet);
            }

            if (winGrade == WinGrade.Normal || winGrade == WinGrade.Mini)
            {
                NextState(SlotMachineState.HIT_WIN);
            }
            else
            {
                NextState(SlotMachineState.HIT_BIGWIN);
            }
        }

        // 일반 히트 스테이트
        protected override IEnumerator HitNormalState()
        {
            WinGrade winGrade = SlotMachineUtils.GetWinGrade(extraInfo.TotalWinCoins, Info.CurrentTotalBet);
            if (Info.IsFreespin && Info.IsFreespinEnd == false && Info.IsRespinEnd)
            {
                winGrade = SlotMachineUtils.GetWinGrade(extraInfo.TumbleWinCoins, Info.CurrentTotalBet);
            }

            long winCoins = Info.SpinResult.WinCoinsHit;
            long totalWinCoins = extraInfo.TotalWinCoins;

            yield return HitNormal(winGrade, winCoins, totalWinCoins);

            var nextState = GetStateBeforeEnd(Info.IsExtraSpin, Info.IsFreespinStart, Info.IsFreespinEnd, Info.IsRespin, Info.IsFreeChoiceStart);
            NextState(nextState);
        }

        protected override IEnumerator HitNormal(WinGrade winGrade, long winCoins, long totalWinCoins, bool hitSymbols = true)
        {
            hitSymbols = !Info.IsRespinEnd;

            yield return HitNormal(winGrade, winCoins, totalWinCoins, true, hitSymbols);
        }

        protected override IEnumerator HitNormal(WinGrade winGrade, long winCoins, long totalWinCoins, bool dispatchWinCoins, bool hitSymbols)
        {
            Debug.LogFormat("**HitNormal : winGrade {0}, dispatchWinCoins {1}, hitSymbol {2}", winGrade, dispatchWinCoins, hitSymbols);

            if (Info.IsRespinEnd)
            {
                dispatchWinCoins = false;
            }

            ShowDimPanel();

            float winDuration;
            if (CurrentPlayModeInProgress == PlayMode.Turbo)
            {
                int index = UnityEngine.Random.Range(0, 3);
                winSoundInfo = winSoundPlayer.GetInfo(index);
                winDuration = PLAYMODE_TURBO_WIN_DURATION;
            }
            else
            {
                if (Info.IsFreespin)
                {
                    winSoundInfo = winSoundPlayer.GetInfo(multipleUI.CurrentIndex + 1);
                }
                else
                {
                    int index = UnityEngine.Random.Range(0, 3);
                    winSoundInfo = winSoundPlayer.GetInfo(index);
                }

                winDuration = (winCoins < Info.CurrentTotalBet) ? TbUnderTime : TbUpperTime;
            }

            if (hitSymbols == true)
            {
                if (CurrentPlayModeInProgress == PlayMode.Turbo)
                {
                    winSoundInfo.duration = turboAutoSpinWinAnimDuration;
                    winSoundPlayer.Play(winSoundInfo);

                    yield return HitSymbols(turboAutoSpinWinAnimDuration);
                }
                else
                {
                    if (Info.IsFreespin)
                    {
                        yield return HitSymbols(winDuration);
                    }
                    else
                    {
                        winSoundInfo.duration = winDuration;
                        winSoundPlayer.Play(winSoundInfo);

                        yield return HitSymbols(winDuration);
                    }
                }
            }
            else if (dispatchWinCoins == true)
            {
                yield return WaitForSeconds(winDuration);
            }

            if (dispatchWinCoins)
            {
                if (Info.IsFreespin || Info.IsFreespinEnd)
                {
                    DispatchWinCoins(Info.SpinResult.WinCoinsHit, 0.0f, winGrade);
                    DispatchTotalWinCoins(extraInfo.TotalWinCoins, 0.0f);
                }
                else
                {
                    if (IsSkipSymbolHitOnTumblingReel())
                    {
                        StartCoroutine(DispatchTotalWinCoinsWithDelay(dispatchWinCoinsDelayOnTurboMode, extraInfo.TotalWinCoins, TbTurboTime, true));
                    }
                    else
                    {
                        DispatchTotalWinCoins(extraInfo.TotalWinCoins, 0.0f);
                    }
                }
            }
        }

        protected override void DispatchWinCoins(long coins, float duration, WinGrade winGrade, bool coinInit = true, bool isJackpot = false, bool ignorePlayMode = false)
        {
            Debug.LogFormat("** dispatchWincoins - {0} , {1}", multipleUI.CurrentIndex, extraInfo.TumbleMulValue);
            winSoundInfo = winSoundPlayer.GetInfo(multipleUI.CurrentIndex + 1);

            base.DispatchWinCoins(coins, duration, winGrade, coinInit, isJackpot, ignorePlayMode);
        }

        // 빅윈 히트 스테이트
        protected override IEnumerator HitBigwinState()
        {
            // 텀블링 중이거나 스캐터 페이에는 라인 페이 빅윈 처리 X
            if (Info.IsRespinStart || Info.IsRespin)
            {
                NextState(SlotMachineState.HIT_WIN);
            }
            else if (Info.IsFreespinStart || Info.IsExtraSpin)
            {
                var nextState = GetStateBeforeEnd(Info.IsExtraSpin, Info.IsFreespinStart, Info.IsFreespinEnd, Info.IsRespin, Info.IsFreeChoiceStart);
                NextState(nextState);
            }
            else
            {
                long winCoins = (Info.IsFreespin && Info.IsRespinEnd) ? extraInfo.TumbleWinCoins : extraInfo.TotalWinCoins;
                long totalWinCoins = (Info.IsFreespin && Info.IsRespinEnd) ? extraInfo.TumbleWinCoins : extraInfo.TotalWinCoins;

                WinGrade winGrade = SlotMachineUtils.GetWinGrade(winCoins, Info.CurrentTotalBet);

                yield return WaitForSeconds(bigwinDelayTime);
                yield return WaitForBigwinSignalEffect();
                yield return HitBigwin(winGrade, winCoins, totalWinCoins);

                skipResult = false;

                var nextState = GetStateBeforeEnd(Info.IsExtraSpin, Info.IsFreespinStart, Info.IsFreespinEnd, Info.IsRespin, Info.IsFreeChoiceStart);
                NextState(nextState);
            }
        }

        protected override IEnumerator HitBigwin(WinGrade winGrade, long winCoins, long totalWinCoins, bool dispatchWinCoins, bool hitSymbols = true)
        {
            ShowDimPanel();

            yield return WaitForSeconds(bigwinHitDelayTime);

            Action shareCallback = DispatchShare;
            if (useShare == false)
            {
                shareCallback = null;
            }

            bool vibrate = useVibrate && winGrade >= WinGrade.Huge;
            string popupName = POPUP_NAME_BIGWIN;

            if (winGrade == WinGrade.Huge)
            {
                popupName = POPUP_NAME_HUGEWIN;
            }
            else if (winGrade == WinGrade.Mega)
            {
                popupName = POPUP_NAME_MEGAWIN;
            }
            else if (winGrade == WinGrade.Epic)
            {
                popupName = POPUP_NAME_EPICWIN;
            }

            popupName = GetPopupNameForOrientation(popupName);

            DispatchHitBigwinOpened(winCoins, winGrade);

            PopupObject<BigWinPopup> popupObject = PopupSystem.Instance.Open<BigWinPopup>(commonAssets, popupName)
                                                                       .OnInitialize(p =>
                                                                       {
                                                                           p.Initialize(winCoins,
                                                                                        holdType != HoldType.On,
                                                                                        shareCallback,
                                                                                        vibrate,
                                                                                        IsWinPopupSkip());
                                                                       })
                                                                       .SetLayer(fullSizeBigwinPopup ? null : DEFAULT_LAYER)
                                                                       .Cache();

            yield return popupObject.WaitForClose();

            DispatchHitBigwinClosed(winCoins, winGrade);

            if (dispatchWinCoins)
            {
                if (CurrentPlayModeInProgress != PlayMode.Turbo)
                {
                    float winDuration = (winCoins < Info.CurrentTotalBet) ? TbUnderTime : TbUpperTime;
                    yield return WaitForSeconds(winDuration);
                }
            }

            FreespinMultiReset(true);
        }
        
        // 프리스핀을 시작하는 스테이트
        // 프리스핀 팝업을 보여줌
        protected override IEnumerator FreespinStartState()
        {
            DispatchGameUiRaycast(false);

            yield return OpenFreespinPopup();


            multipleUI.OnInitialize();

            winSoundPlayer.SelectJukeBox(PAY_SOUND_FREE_INDEX);

            SetReelMode(ReelMode.Free);
            DispatchChangeFreespins(Info.FreespinCurrentCount, Info.FreespinTotalCount);

            yield return WaitForSeconds(multiUIOpenDelayTime);

            info1079.SetRemainTumble(true);

            yield return new WaitForSeconds(transitionTime);

            NextState(SlotMachineState.RESULT_END);
            DispatchGameUiRaycast(true);
        }

        // 프리스핀 종료 스테이트
        // 프리스핀 종료 팝업을 보여줌
        protected override IEnumerator FreespinEndState()
        {
            multipleUI.OnEnd();

            yield return base.FreespinEndState();

            winSoundPlayer.SelectJukeBox(PAY_SOUND_NORMAL_INDEX);

            yield return new WaitForSeconds(transitionTime);

            MachineUI.ignoreReelModeEvent = true;
        }

        // 엑스트라 스핀 스테이트
        // 엑스트라 스핀 팝업을 보여줌
        protected override IEnumerator ExtraspinState()
        {
            DispatchGameUiRaycast(false);
            DispatchChangeFreespins(Info.FreespinCurrentCount, Info.FreespinTotalCount);

            yield return OpenExtraSpinPopup();

            NextState(SlotMachineState.RESULT_END);
            DispatchGameUiRaycast(true);
        }

        protected override (bool, string) CheckEarnCoinValidity(long earnCoin)
        {
            if (Info.IsRespin)
            {
                return (true, string.Empty);
            }

            string message = null;

            if (Info.LatestSlotAction == SlotMachineInfo.SlotActionType.Spin)
            {
                var userCoin = Info.coins + earnCoin;
                var spinData = Info.SpinResult.GetData();

                if (userCoin != spinData.coin)
                {
                    message = $"유저 머니 불일치\n\n클라 : {StringUtils.ToComma(userCoin)}\n서버 : {StringUtils.ToComma(spinData.coin)}";
                    Debug.LogWarning(message);
                }
            }

            bool isValid = string.IsNullOrEmpty(message);
            return (isValid, message);
        }

        // Popup

        protected override IEnumerator OpenFreespinPopup()
        {
            freeBGM.Play();

            yield return PopupSystem.Instance.Open<FreeSpinStartPopup1079>(slotAssets, FREE_START_POPUP)
                                    .OnInitialize(p => p.Initialize(OnTransitionBaseToFree, Info.FreespinTotalCount, holdType != HoldType.On, useVibrate))
                                    .Cache()
                                    .SetBackgroundAlpha(0.0f)
                                    .WaitForClose();
        }

        //---------------------------------------------------------------------------------------------------------------
        // Connected BG Transition Animation
        //---------------------------------------------------------------------------------------------------------------
        public void OnTransitionBaseToFree()
        {
            Debug.Log("OnTransitioBaseToFree");

            buyFeature.UpdateState();

            bgAnim.ResetAllTriggers();
            bgAnim.SetTrigger(string.Format(ANIM_TRIGGER_TRANSITION, ANIM_TRIGGER_BASE, ANIM_TRIGGER_FREE));
            //baseToFreeSound.Play();


        }

        public void OnTransitionFreeToBase()
        {
            Debug.Log("OnTransitionFreeToBase");

            bgAnim.SetTrigger(string.Format(ANIM_TRIGGER_TRANSITION, ANIM_TRIGGER_FREE, ANIM_TRIGGER_BASE));
            SetReelMode(ReelMode.Regular);
            SetRestoreSymbol();
        }
        //---------------------------------------------------------------------------------------------------------------

        private void SetRestoreSymbol(bool isFree = false)
        {
            Debug.Log("SetRestoreSymbol");
            var restoreBaseList = (isFree) ? extraInfo.FreeSymbolList : extraInfo.BaseSymbolList;

            if (isFree)
            {
                for (int i = 0; i < extraInfo.FreeReelStopList.Count; i++)
                {
                    var reel = reelGroup.GetReel(i);

                    reel.ReelStrip.Jump(extraInfo.FreeReelStopList[i]);
                }
            }
            else
            {
                for (int i = 0; i < extraInfo.BaseReelStopList.Count; i++)
                {
                    var reel = reelGroup.GetReel(i);
                    reel.ReelStrip.Jump(extraInfo.BaseReelStopList[i]);
                }
            }


            foreach (var info in restoreBaseList)
            {
                var reel = reelGroup.GetReel(info.pos.reelIndex);
                var symbol = reel.ChangeMainSymbol(info.pos.mainIndex, info.id);
                symbol.SetVisual(SymbolVisualType.Idle);
            }
        }

        protected override IEnumerator OpenResultWinPopup()
        {
            yield return PopupSystem.Instance.Open<FreeEndPopup1079>(slotAssets, FREE_END_POPUP)
                                           .OnInitialize(p =>
                                           {
                                               p.Initialize(OnTransitionFreeToBase, extraInfo.TotalWinCoins, holdType != HoldType.On);
                                           })
                                           .Cache()
                                           .SetBackgroundAlpha(0.0f)
                                           .WaitForClose();
        }

        protected override IEnumerator OpenResultLosePopup()
        {
            return base.OpenResultLosePopup();
        }

        protected override IEnumerator OpenExtraSpinPopup()
        {
            yield return PopupSystem.Instance.Open<ExtraSpinPopup>(commonAssets, RETRIGGER_POPUP)
                                            .OnInitialize(p =>
                                            {
                                                p.Initialize(Info.ExtraSpinCount, muteBgmWhenExtraPopupOpens);
                                            })
                                            .SetLayer(fullSizeExtraSpinPopup ? null : DEFAULT_LAYER)
                                            .Cache()
                                            .WaitForClose();
        }

        // Connect Event - ReelEffect Component
        public void OnSymbolAppeared(ReelEffect.TriggeredInfo info)
        {
            // 텀블 중에는 appear 하지 않음
            if (tumblingReelController.GetTumbling()) return;

            if (info.presetIndex == 0)
            {
                int reelIndex = info.symbolInfo.column;
                int mainSymbolIndex = info.symbolInfo.row;

                var reel = reelGroup.GetReel(reelIndex);
                var symbol = reel.GetMainSymbol(mainSymbolIndex);
                if (symbol.IsScatter)
                {
                    symbol.SetVisual(appearVisual, 0);
                    symbol.AddSubstituteVisual(SymbolVisualType.Idle, 0, SymbolVisualType.Idle, 1);
                }
            }
        }

        private IEnumerator OnTumbleAppear(List<List<int>> hitPos)
        {
            List<Symbol> appearScatter = new List<Symbol>();

            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                var reel = reelGroup.GetReel(reelIndex);

                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength - hitPos[reelIndex].Count; mainSymbolIndex++)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);
                    if (symbol.IsScatter)
                    {
                        appearScatter.Add(symbol);
                    }
                }
            }

            if (appearScatter.Count > 1)
            {
                foreach (var symbol in appearScatter)
                {
                    symbol.SetVisual(impactVisual, 0);

                }

                yield return WaitForSeconds(impactTime);
            }
        }

        protected override PlayMode ConvertPlayModeInFeature(PlayMode currentPlayMode)
        {
            if (Info.IsFreespin)
            {
                return PlayMode.Normal;
            }

            return currentPlayMode;
        }
    }
}