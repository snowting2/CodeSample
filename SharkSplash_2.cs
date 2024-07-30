using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Popup;
using SlotGame.UI;

namespace SlotGame.Machine.S1088
{
    public class SlotMachine1088 : SlotMachine
    {
#pragma warning disable 0649
        [SerializeField] private BuyFeatureUI buyFeature;

        [SerializeField] private List<MultiText1088> multiList;
        //[SerializeField] private MultiText1088 multiRef_R;
        [SerializeField] private FlipWinCoins1088 flipWinCoins;
        [SerializeField] private TransitionTimeLine1088 timeline;
        [SerializeField] private WaterJetFx1088 waterJetFx;
        [SerializeField] private ReelEffect reelEffect;
        [SerializeField] private SharkFx1088 sharkFx;
        [SerializeField] private Transform winBox;
        [SerializeField] private GameObject winBoxRoot;

        [Header("Flip Time")]
        [SerializeField] private float flipStartDelayTime;
        [SerializeField] private float turboFlipStartDelayTime;
        [SerializeField] private float flipResultDelayTime;

        [SerializeField] private float freespinStartDelayTime;
        [SerializeField] private float turboWaterJetEndTime;

        [Header("Flip Drop Down")]
        [SerializeField] private float flipDropTime;
        [SerializeField] private float flipDownTime;
        [Space]
        [SerializeField] private float turboFlipDropTime;
        [SerializeField] private float turboFlipDownTime;

        [Header("Dimd Color")]
        [SerializeField] private Color dimdColor;

#pragma warning restore 0649

        private ExtraInfo1088 extraInfo;
        private SlotMachineInfo1088 info1088;

        private GameObjectPool<Transform> winBoxPool = null;
        // 릴셋 정보
        private readonly List<List<int>> reelSetInfos = new List<List<int>>()
        {
            new List<int>{0,5,10,15,20,4,9,14,19,24},
            new List<int>{1,6,11,16,21,3,8,13,18,23},
            new List<int>{2,7,12,17,22}
        };

        private Dictionary<int, Transform> winboxList = new Dictionary<int, Transform>(21);

        private readonly SymbolVisualType appearIdleVisual = SymbolVisualType.Etc1;
        private readonly SymbolVisualType appearVisual = SymbolVisualType.Etc4;
        private readonly SymbolVisualType flipDownVisual = SymbolVisualType.Etc2;
        private readonly SymbolVisualType flipDropVisual = SymbolVisualType.Etc3;
        private readonly SymbolVisualType dimdVisual = SymbolVisualType.Etc5;

        // private int scatterCount = 0;
        private readonly int REEL_EFFECT_TARGET_INDEX = 2;
        private readonly string SCATTER_SYMBOL_ID = "8";
        private readonly int MAIN_SYMBOL_INDEX = 0;

        private bool isFirstTrail = false;
        private int triggerCount = 0;
        private bool IsTurbo { get { return AutoSpin && (Info.AutoPlay.playModeCondition.GetPlayMode() == PlayMode.Turbo) && (!Info.IsFreespin && !Info.IsFreespinEnd); } }
        private bool IsQuick { get { return AutoSpin && (Info.AutoPlay.playModeCondition.GetPlayMode() == PlayMode.Fast); } }

        private bool IsAutoNormal
        {
            get
            {
                return AutoSpin && (Info.AutoPlay.playModeCondition.GetPlayMode() == PlayMode.Normal ||
                                    Info.AutoPlay.playModeCondition.GetPlayMode() == PlayMode.Fast);
            }
        }
        public enum FeatureKind
        {
            Base = 0,
            Free,
            None
        }

        private FeatureKind currentFeature = FeatureKind.None;
        private FeatureKind CurrentFeature
        {
            get { return currentFeature; }
            set
            {
                if (currentFeature == value) return;

                string nextState = value.ToString();

                timeline.SetState((currentFeature == FeatureKind.None) ? string.Empty : currentFeature.ToString(), nextState);

                ReleaseAllMultiText(false);

                if (currentFeature == FeatureKind.None)
                {
                    StartCoroutine(sharkFx.OnIntro(nextState));
                }

                currentFeature = value;
            }
        }

        //-------------------------------------------------------------------------------
        // Popup
        //-------------------------------------------------------------------------------
        private readonly string FREE_START_POPUP = "FreeStartPopup1088";
        private readonly string FREE_END_POPUP = "FreeEndPopup1088";
        //-------------------------------------------------------------------------------

        protected override SlotMachineInfo CreateSlotMachineInfo(int reelColumn, int reelRow, SlotOrientationType slotOrientationType, JackpotWinType jackpotWinType = JackpotWinType.Normal)
        {
            info1088 = new SlotMachineInfo1088(reelColumn, reelRow, slotOrientationType, jackpotWinType);
            return info1088;
        }

        protected override void SetupPopups()
        {
            base.SetupPopups();

            PopupSystem.Instance.AddCachedPopup<FreeSpinStartPopup1088>(slotAssets, FREE_START_POPUP);
            PopupSystem.Instance.AddCachedPopup<FreeSpinEndPopup1088>(slotAssets, FREE_END_POPUP);
        }

        protected override void RemovePopups()
        {
            base.RemovePopups();

            PopupSystem.Instance.RemoveCachedPopup(FREE_START_POPUP);
            PopupSystem.Instance.RemoveCachedPopup(FREE_END_POPUP);
        }

        protected override void PostInitialize(EnterData enterData)
        {
            extraInfo = this.GetComponent<ExtraInfo1088>();
            extraInfo.Initialize();

            base.PostInitialize(enterData);
        }


        // 슬롯머신 진입시 최초 한번 실행되어야 할 스테이트
        // 이 부분에 슬롯머신 진입 애니메이션 등을 구현해야함.
        protected override IEnumerator EnterState()
        {
            winBoxPool = new GameObjectPool<Transform>(winBoxRoot, 21, () => { return Instantiate(winBox); });

            SetupReelSet();

            if (flipWinCoins.gameObject.activeSelf == false) flipWinCoins.gameObject.SetActive(true);

            flipWinCoins.Hide(IsTurbo);

            if (Info.IsRespin)
            {
                Debug.Log("** Enter Respin ...");

                var totalWinCoins = (Info.IsFreespin) ? (extraInfo.FlipTotalWinCoins + Info.TotalWinCoins) : extraInfo.FlipTotalWinCoins;
                info1088.AddTotalWinCoin(totalWinCoins);
                DispatchTotalWinCoins(totalWinCoins, 0.0f);
            }

            reelGroup.OnStateStart.AddListener(OnReelStateStart);
            reelGroup.OnStateEnd.AddListener(OnReelStateEnd);

            yield return SetCurrentState();

            yield return base.EnterState();
        }

        private void OnReelStateEnd(int reelIndex, ReelState state, BaseReel reel)
        {
            if (state == ReelState.Slowdown)
            {
                var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX);
                if (symbol.IsScatter)
                {
                    StartCoroutine(ScatterAddAnimation(symbol));
                }
            }
        }

        private IEnumerator ScatterAddAnimation(Symbol symbol)
        {
            symbol.SetVisual(appearVisual);
            yield return null;
            symbol.SetVisual(appearIdleVisual, 0);
        }

        private void OnReelStateStart(int reelIndex, ReelState state, BaseReel reel)
        {
            if ((AutoSpin && (Info.AutoPlay.playModeCondition.GetPlayMode() == PlayMode.Turbo)) || IsQuick) return;
            // ReelEffect 수동 제어
            if (Info.IsRespin && Info.IsRespinStart == false) return;

            if (IsForceStop) return;

            if (isBuyFetureSpin) return;

            if (reelSetInfos[1].Contains(reelIndex))
            {
                if (state == ReelState.Slowdown)
                {
                    var symbolID = reel.ReelStrip.GetID(reel.StopIndexOnReelStrip);

                    if (symbolID == SCATTER_SYMBOL_ID)
                    {
                        triggerCount += 1;
                    }
                }
            }

            if (triggerCount >= 2)
            {
                reelEffect.ShowWithDuration(0, 17, reelEffect.GetPreset(0).duration);
                triggerCount = 0;


            }
        }
        protected override void ForceStop()
        {
            reelEffect.HideAll();
            base.ForceStop();
        }

        private void SetupReelSet()
        {
            for (int i = 0; i < reelSetInfos.Count; i++)
            {
                reelGroup.AddReelSet(reelSetInfos[i]);
            }
        }

        private IEnumerator SetCurrentState()
        {
            if (Info.IsFreespin)
            {
                CurrentFeature = FeatureKind.Free;
                yield return WaitForSeconds(freespinStartDelayTime);
            }
            else
            {
                CurrentFeature = FeatureKind.Base;
            }
        }

        // 기본 상태.
        // 기본적으로 유저의 입력을 기다리지만 프리스핀, 리스핀, 오토스핀등의 상황에선 자동으로 스핀 상태로 변경.
        protected override IEnumerator IdleState()
        {
            if (Info.IsRespin == false && Info.IsRespinEnd == false && Info.IsRespinStart == false)
            {
                SetVisualAllMainSymbol(true);
            }

            yield return base.IdleState();
        }


        private bool IsAppearLongSpin()
        {
            if (isBuyFetureSpin) return false;

            int scatterCount = 0;
            for (int i = 0; i < reelSetInfos[1].Count; i++)
            {
                int reelIndex = reelSetInfos[1][i];
                string symbolID = SlotMachineUtils.ConvertSymsToList(Info.GetSyms(), reelGroup.Column, reelGroup.Row)[reelIndex][0];
                if (symbolID == SCATTER_SYMBOL_ID)
                {
                    scatterCount++;
                }
            }

            return (scatterCount > 1);
        }

        //--------------------------------------------------------------------------------------------
        // OnConnected ReelEffect Events
        //--------------------------------------------------------------------------------------------
        public void OnReelEffectStart()
        {
            sharkFx.OnStartReelEffect();
        }

        public void OnReelEffectEnd()
        {
            sharkFx.OnIdle(false);
        }
        //--------------------------------------------------------------------------------------------

        public override void StartSpin()
        {
            triggerCount = 0;
            base.StartSpin();
        }
        // 스핀을 시작하는 스테이트.
        protected override IEnumerator SpinStartState()
        {
            // Flip Change Symbol + Showing MultiValue
            if (Info.IsRespin)
            {
                UpdateJackpotCoins();

                if ((Info.IsFreespinStart && Info.IsFreespinRestart == false) || Info.IsStackedTotalWin == false)
                {
                    DispatchTotalWinCoins(0, 0.0f);
                }

                DispatchWinCoins(0, 0.0f, WinGrade.None);

                machineGroup.ForEach(item =>
                {
                    item.reelGroup.ClearExtraStopDelayTimes();
                });

                HideDimPanel();
                StopRepeatWinningLines(false);

                while (CurrentState == SlotMachineState.SPIN_START)
                {
                    yield return null;
                }
            }
            else
            {
                ReleaseAllMultiText(false);

                reelGroupHelper.RemoveAllSubstituteVisualSymbols();

                if (CurrentPlayModeInProgress != PlayMode.Turbo)
                {
                    StopRepeatWinningLines();
                }

                yield return base.SpinStartState();
            }
        }


        private IEnumerator OnFlip()
        {
            if (extraInfo.PrevFlipInfoList.Count == 0) yield break;

            SetVisualAllMainSymbol(false);

            //---------------------------------------------------------------------------
            // WaterJet 이펙트
            //---------------------------------------------------------------------------            
            yield return OnWaterJetEffect();
            //---------------------------------------------------------------------------

            // 플립 연출
            //---------------------------------------------------------------------------
            float delayTime = (IsTurbo) ? (turboFlipDropTime + turboFlipDownTime) : (flipDropTime + flipDownTime);
            yield return WaitForSeconds(delayTime);
            //---------------------------------------------------------------------------

            yield return SetTotalWinCoins();

            if (IsAutoNormal)
            {
                yield return WaitForSeconds(flipResultDelayTime);
            }

            flipWinCoins.Hide(IsTurbo);
        }

        private void OnFlipAction(ExtraInfo1088.FlipInfo info)
        {
            StartCoroutine(OnFlipDropDown(info));
        }

        private IEnumerator OnFlipDropDown(ExtraInfo1088.FlipInfo info)
        {
            float delayTime = (IsTurbo) ? turboFlipStartDelayTime : flipStartDelayTime;

            if (IsTurbo)
            {
                ReleaseAllMultiText(true);

                yield return WaitForSeconds(delayTime);
            }
            else
            {
                yield return WaitForSeconds(delayTime);

                ReleaseAllMultiText(true);
            }

            if (info.mul > 0)
            {
                SetMultiText(info);
            }


            if (IsTurbo == false)
            {
                HideWinBox(info.pos.reelIndex);
            }

            yield return OnFlipDown(info.pos.reelIndex);

            if (IsTurbo)
            {
                HideWinBox(info.pos.reelIndex);
                yield return OnFlipDrop(info.pos.reelIndex, info.reelStopIndex);
            }
        }

        private IEnumerator OnFlipDrop(int reelIndex, int stopIndex)
        {
            var reel = reelGroup.GetReel(reelIndex);

            string symbolID = SlotMachineUtils.ConvertSymsToList(Info.GetSyms(), reelGroup.Column, reelGroup.Row)[reelIndex][MAIN_SYMBOL_INDEX];

            // Change Symbol
            var symbol = reel.ChangeMainSymbol(MAIN_SYMBOL_INDEX, symbolID);

            if (symbol.IsScatter)
            {
                StartCoroutine(ScatterAddAnimation(symbol));
                symbol.AddSubstituteVisual(SymbolVisualType.Idle, 0, SymbolVisualType.Idle, 1);
            }
            else
            {
                symbol.SetVisual(flipDropVisual);
            }

            reel.ReelStrip.Jump(stopIndex);

            if (IsTurbo)
            {
                yield return WaitForSeconds(turboFlipDropTime);
            }
        }

        private IEnumerator OnFlipDown(int reelIndex)
        {
            var reel = reelGroup.GetReel(reelIndex);
            var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX);

            symbol.SetVisual(flipDownVisual);

            float delayTime = (IsTurbo) ? turboFlipDownTime : flipDownTime;

            yield return WaitForSeconds(delayTime);
        }

        private IEnumerator OnWaterJetEffect()
        {
            if (extraInfo.PrevFlipInfoList.Count > 0)
            {
                for (int i = 0; i < extraInfo.PrevFlipInfoList.Count; i++)
                {
                    var info = extraInfo.PrevFlipInfoList[i];
                    var pos = info.pos;

                    if (IsTurbo)
                    {
                        waterJetFx.OnTrailTurbo(pos.reelIndex, () => OnFlipAction(info));
                    }
                    else
                    {
                        yield return waterJetFx.OnTrail(pos.reelIndex, () => OnFlipAction(info));
                    }
                }

                float delay = (IsTurbo) ? turboWaterJetEndTime : waterJetFx.MoveTime;

                if (IsTurbo == false)
                {
                    yield return waterJetFx.OnWaitTrailEnd();
                }

                OnFlipWinCoin();

                yield return WaitForSeconds(delay);
            }

            if (IsTurbo == false)
            {
                flipWinCoins.Hide(IsTurbo);

                if (extraInfo.PrevFlipInfoList.Count > 0)
                {
                    yield return WaitForSeconds(flipDownTime);

                    for (int i = 0; i < extraInfo.PrevFlipInfoList.Count; i++)
                    {
                        var info = extraInfo.PrevFlipInfoList[i];
                        yield return OnFlipDrop(info.pos.reelIndex, info.reelStopIndex);
                    }

                    yield return WaitForSeconds(flipDropTime);
                }
            }

            isFirstTrail = false;
        }



        private IEnumerator SetTotalWinCoins()
        {
            var winCoins = extraInfo.FlipTotalWinCoins - Info.SpinResult.WinCoinsHit;

            if ((Info.IsFreespin || Info.IsFreespinEnd) && Info.IsFreespinStart == false)
            {
                winCoins = Info.TotalWinCoins - Info.SpinResult.WinCoinsHit;
            }

            float winDuration = (IsTurbo) ? turboAutoSpinWinAnimDuration : winCoinDuration;
            DispatchTotalWinCoins(winCoins, winDuration);

            yield return WaitForSeconds(winCoinDuration);
        }

        private void SetMultiText(ExtraInfo1088.FlipInfo info)
        {
            var symbol = reelGroup.GetReel(info.pos.reelIndex).GetMainSymbol(info.pos.mainIndex);

            int index = extraInfo.GetMultiIndex(info.pos.reelIndex);

            var multi = multiList[index];
            multi.gameObject.SetActive(true);
            multi.transform.position = symbol.transform.position;

            bool isHit = IsContainHit(info.pos.reelIndex, info.pos.mainIndex);
            multi.ShowMultiValue(info.mul, isHit, IsTurbo);
        }


        private bool IsContainHit(int reelIndex, int mainSymbolIndex)
        {
            if (reelIndex == -1) return false;

            var spinResult = Info.SpinResult;

            for (int i = 0; i < spinResult.GetWinInfoCount(0); i++)
            {
                var winInfo = spinResult.GetWinInfo(0, i);

                if (winInfo.hitSymbolPositions[reelIndex].Contains(mainSymbolIndex))
                {
                    return true;
                }
            }

            return false;
        }


        protected override void DispatchWinCoins(long coins, float duration, WinGrade winGrade, bool coinInit = true, bool isJackpot = false, bool ignorePlayMode = false)
        {
            //base.DispatchWinCoins(coins, duration, winGrade, coinInit, isJackpot, ignorePlayMode);
        }

        protected override IEnumerator SpinUpdateState()
        {
            yield return OnFlip();

            yield return base.SpinUpdateState();
        }

        // 스핀 응답을 받고 Info 를 업데이트 한 뒤 실제 릴에게 stop 명령을 내리기 전 스테이트
        protected override IEnumerator SpinPreStopState()
        {
            yield return base.SpinPreStopState();

            if (IsAppearLongSpin())
            {
                reelGroup.SetExtraStopDelayTime(REEL_EFFECT_TARGET_INDEX, reelEffect.GetPreset(0).duration);
            }
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
            return base.SpinStopState();
        }

        // 스핀이 완전히 종료되었을때 호출 되는 스테이트
        // 스핀 종료 후 특별한 연출등을 표현 (ex : 스택드 와일드 합체 등등)
        protected override IEnumerator SpinEndState()
        {
            yield return base.SpinEndState();
        }

        protected override IEnumerator RespinStartState()
        {
            return base.RespinStartState();
        }

        protected override IEnumerator RespinEndState()
        {
            extraInfo.ResetPrevInfo();

            float delay = (IsTurbo) ? 0.0f : respinResultDelayForAllSpins;
            yield return new WaitForSeconds(delay);

            var nextState = GetStateBeforeEnd(Info.IsExtraSpin, Info.IsFreespinStart, Info.IsFreespinEnd, Info.IsRespin, Info.IsFreeChoiceStart, Info.IsLinkRespin, Info.IsLinkRespinStart, Info.IsLinkRespinEnd, false, false);
            NextState(nextState);
        }

        // 스핀이 멈추고 결과를 시작 되는 스테이트
        // 잭팟, 빅윈 등에 분기점
        protected override IEnumerator ResultStartState()
        {
            yield return null;

            WinGrade winGrade = Info.SpinResult.WinGrade;
            if (Info.IsFreespin == false && Info.IsFreespinEnd == false)
            {
                if (Info.IsRespinEnd)
                {
                    winGrade = SlotMachineUtils.GetWinGrade(Info.TotalWinCoins, Info.CurrentTotalBet);
                }
            }
            else if (Info.IsFreespin && Info.IsFreespinEnd == false && Info.IsRespinEnd)
            {
                winGrade = SlotMachineUtils.GetWinGrade(extraInfo.FlipTotalWinCoins, Info.CurrentTotalBet);
            }

            if (winGrade == WinGrade.None)
            {
                var nextState = GetStateBeforeEnd(Info.IsExtraSpin, Info.IsFreespinStart, Info.IsFreespinEnd, Info.IsRespin, Info.IsFreeChoiceStart);
                NextState(nextState);
            }
            else
            {
                if (Info.IsRespinEnd)
                {
                    if ((winGrade == WinGrade.Normal) || (winGrade == WinGrade.Mini))
                    {
                        var nextState = GetStateBeforeEnd(Info.IsExtraSpin, Info.IsFreespinStart, Info.IsFreespinEnd, Info.IsRespin, Info.IsFreeChoiceStart);
                        NextState(nextState);
                    }
                    else
                    {
                        StartCoroutine(ReadyToResultSkip(RESULT_SKIP_DELAY));
                        NextState(SlotMachineState.HIT);
                    }
                }
                else
                {
                    StartCoroutine(ReadyToResultSkip(RESULT_SKIP_DELAY));
                    NextState(SlotMachineState.HIT);
                }
            }
        }

        // 결과 종료 스테이트
        // idle 상태로 돌아감
        protected override IEnumerator ResultEndState()
        {
            yield return base.ResultEndState();

            if (Info.IsRespinEnd)
            {
                if (Info.IsFreespinStart == false)
                {
                    RemoveSubstituteSymbol();
                }

                OffFlipWinCoin();
                ReleaseWinBox();
            }

            if (Info.IsRespinEnd)
            {
                sharkFx.OnIdle(Info.IsFreespin);
            }
        }

        private void RemoveSubstituteSymbol()
        {
            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                var reel = reelGroup.GetReel(reelIndex);
                var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX);

                symbol.RemoveAllSubstituteVisuals();
            }
        }

        protected override void HitScatterSymbols()
        {
            OffFlipWinCoin();
            base.HitScatterSymbols();
        }

        // 심볼 히트 스테이트
        protected override IEnumerator HitState()
        {
            if (Info.IsRespinStart)
            {
                yield return WaitForSeconds(hitDelayTime);
            }

            WinGrade winGrade = SlotMachineUtils.GetWinGrade(extraInfo.FlipTotalWinCoins, Info.CurrentTotalBet);

            if (Info.IsRespinEnd)
            {
                if (winGrade >= WinGrade.Big)
                {
                    NextState(SlotMachineState.HIT_BIGWIN);
                }
                else
                {
                    NextState(SlotMachineState.HIT_WIN);
                }
            }
            else
            {
                NextState(SlotMachineState.HIT_WIN);
            }
        }

        // 일반 히트 스테이트
        protected override IEnumerator HitNormalState()
        {
            yield return base.HitNormalState();
        }

        protected override IEnumerator HitNormal(WinGrade winGrade, long winCoins, long totalWinCoins, bool dispatchWinCoins, bool hitSymbols)
        {
            ShowDimPanel();

            float winDuration;

            if (winSoundPlayer != null && winSoundPlayer.enabled == true)
            {
                if (CurrentPlayModeInProgress == PlayMode.Turbo)
                {
                    winSoundInfo = winSoundPlayer.GetInfo(0, GetCurrentTotalBet());
                    winDuration = turboAutoSpinWinAnimDuration;
                }
                else
                {
                    winSoundInfo = winSoundPlayer.GetInfo(winCoins, GetCurrentTotalBet());
                    winDuration = winCoinDuration;
                }
            }
            else
            {
                winSoundInfo = null;

                if (CurrentPlayModeInProgress == PlayMode.Turbo)
                {
                    winDuration = turboAutoSpinWinAnimDuration;
                }
                else
                {
                    winDuration = winCoinDuration;
                }
            }

            if (hitSymbols == true)
            {
                if ((CurrentPlayModeInProgress == PlayMode.Turbo) || IsTurbo)
                {
                    yield return HitSymbols(turboAutoSpinWinAnimDuration);
                }
                else
                {
                    float animDuration = (IsAutoNormal || IsQuick) ? hitAnimationTimeInAuto : hitAnimationTime;
                    yield return HitSymbols(animDuration);
                }
            }
            else if (dispatchWinCoins == true)
            {
                yield return WaitForSeconds(winDuration);
            }
        }

        protected override IEnumerator HitSymbols(float delayTime)
        {
            OnSharkFlip();

            foreach (var mul in multiList)
            {
                if (mul.IsHit) mul.OnHit(IsTurbo);
            }


            if (Info.IsRespinEnd == false)
            {
                SetDimdSymbol();
            }

            DrawWinBox();

            yield return HitSymbosAllBeforeRepeatWinningLines(Info.SpinResult, delayTime, 0);

            RemoveSubstituteSymbol();

            extraInfo.SetCurrentFlipWinCoins();
        }

        protected override IEnumerator HitSymbosAllBeforeRepeatWinningLines(SpinResult spinResult, float delay, int hitVisualIndex = 0)
        {
            bool isHit = spinResult.WinCoinsHit > 0;
            SetVisualAllMainSymbol(true, isHit);

            machineGroup.ForEachWithIndex((item, enabledMachineIndex) =>
            {
                item.ReelGroupHelper.HitAllSymbols(enabledMachineIndex, Info.SpinResult, paylineIgnoreType, SymbolVisualType.Hit, 0, !ignoreStickySymbolsLineWin, (!IsTurbo) && (!Info.IsRespinEnd));
            });

            yield return WaitForSeconds(delay);
        }

        private void HideWinBox(int reelIndex)
        {
            if (winboxList.ContainsKey(reelIndex))
            {
                var winBox = winboxList[reelIndex];
                winBoxPool.Return(winBox);
            }
        }

        private void ReleaseWinBox()
        {
            foreach (var winBox in winboxList)
            {
                winBoxPool.Return(winBox.Value);
            }

            winboxList.Clear();
        }

        private void DrawWinBox()
        {
            ReleaseWinBox();

            for (int winInfoIndex = 0; winInfoIndex < Info.SpinResult.GetWinInfoCount(0); winInfoIndex++)
            {
                SpinResult.WinInfo winInfo = Info.SpinResult.GetWinInfo(0, winInfoIndex);

                for (int reelIndex = 0; reelIndex < winInfo.hitSymbolPositions.Count; reelIndex++)
                {
                    var hitSymbols = winInfo.hitSymbolPositions[reelIndex];
                    for (int symbolIndex = 0; symbolIndex < hitSymbols.Count; symbolIndex++)
                    {
                        var mainSymbolIndex = hitSymbols[symbolIndex];
                        var symbol = reelGroup.GetReel(reelIndex).GetMainSymbol(mainSymbolIndex);

                        var winBox = winBoxPool.Get();

                        winBox.SetParent(winBoxRoot.transform, false);
                        winBox.position = symbol.transform.position;

                        if (winboxList.ContainsKey(reelIndex))
                        {
                            HideWinBox(reelIndex);
                        }

                        winboxList[reelIndex] = winBox;
                    }
                }
            }
        }

        private void SetDimdSymbol()
        {
            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                var reel = reelGroup.GetReel(reelIndex);
                var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX);

                var currentFlip = extraInfo.CurrentFlipInfoList.Find(x => x.EqualPos(reelIndex, MAIN_SYMBOL_INDEX));

                if (string.IsNullOrEmpty(currentFlip.id) == false)
                {
                    string symbolID = SlotMachineUtils.ConvertSymsToList(Info.GetSyms(), reelGroup.Column, reelGroup.Row)[reelIndex][MAIN_SYMBOL_INDEX];

                    symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX);

                    if (IsTurbo == false)
                    {
                        if (symbol.IsScatter == false)
                        {
                            symbol.AddSubstituteVisual(SymbolVisualType.Idle, 0, SymbolVisualType.Disable, 0);
                        }
                    }
                }
                else
                {
                    if (IsTurbo == false)
                    {
                        if (symbol.IsScatter == false)
                        {
                            symbol.AddSubstituteVisual(SymbolVisualType.Idle, 0, SymbolVisualType.Disable, 0);
                        }
                    }
                }
            }
        }

        private void OnFlipWinCoin()
        {
            if (extraInfo.CurrentFlipCoins > 0)
            {
                flipWinCoins.ShowResult(extraInfo.CurrentFlipCoins, false, IsTurbo);
            }
        }

        private void OnSharkFlip()
        {
            if (Info.SpinResult.WinCoinsHit > 0)
            {
                var mul = (float)extraInfo.FlipTotalWinCoins / (float)Info.CurrentTotalBet;
                sharkFx.OnFlip(mul, Info.IsFreespin);
            }
        }

        private void OffFlipWinCoin()
        {
            flipWinCoins.Hide(IsTurbo);
            SetVisualAllMainSymbol(true);
        }

        // 빅윈 히트 스테이트
        protected override IEnumerator HitBigwinState()
        {
            if (Info.IsFreespinStart)
            {
                var nextState = GetStateBeforeEnd(Info.IsExtraSpin, Info.IsFreespinStart, Info.IsFreespinEnd, Info.IsRespin, Info.IsFreeChoiceStart);
                NextState(nextState);
            }
            else
            {
                long winCoins = (Info.IsFreespin && Info.IsRespinEnd) ? (extraInfo.FlipTotalWinCoins) : Info.TotalWinCoins;
                long totalWinCoins = Info.TotalWinCoins;

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

            OffFlipWinCoin();

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
                winSoundInfo = null;

                DispatchWinCoins(winCoins, 0.0f, winGrade);
                DispatchTotalWinCoins(totalWinCoins, 0.0f);
            }

            sharkFx.OnIdle(Info.IsFreespin);
        }

        // 스캐터 히트 스테이트
        protected override IEnumerator ScatterState()
        {
            StopRepeatWinningLines(false);
            SetVisualAllMainSymbol(true);

            yield return WaitForSeconds(scatterHitDelayTime, true);

            ShowDimPanel();
            HitScatterSymbols();

            yield return WaitForSeconds(scatterHitAnimationTime, true);

            if (Info.IsFreeChoiceStart == true)
            {
                NextState(SlotMachineState.CHOICE_START);
            }
            else if (Info.IsExtraSpin == true)
            {
                NextState(SlotMachineState.EXTRA_SPIN);
            }
            else
            {
                NextState(SlotMachineState.FREESPIN_START);
            }
        }

        // 프리스핀을 시작하는 스테이트
        // 프리스핀 팝업을 보여줌
        protected override IEnumerator FreespinStartState()
        {
            DispatchGameUiRaycast(false);
            if (saveSymbolsBeforeFreespins == true)
            {
                SaveSymbols();
            }

            if (Info.IsFreespinRestart == false)
            {
                DispatchWinCoins(0, 0.0f, WinGrade.None);
            }

            yield return OpenFreespinPopup();

            DispatchChangeFreespins(Info.FreespinCurrentCount, Info.FreespinTotalCount);

            NextState(SlotMachineState.RESULT_END);
            DispatchGameUiRaycast(true);
        }


        // 프리스핀 종료 스테이트
        // 프리스핀 종료 팝업을 보여줌
        protected override IEnumerator FreespinEndState()
        {
            DispatchGameUiRaycast(false);
            yield return new WaitForSeconds(freespinResultDelay);

            BeforeOpeningResult();

            if (Info.TotalWinCoins > 0)
            {
                yield return OpenResultWinPopup();

                if (showTotalFreeSpinBigwin)
                {
                    yield return TotalFreespinBigWin();
                }
            }
            else
            {
                yield return OpenResultLosePopup();
            }

            HideDimPanel();

            NextState(SlotMachineState.RESULT_END);
            DispatchGameUiRaycast(true);
        }

        private void SetVisualAllMainSymbol(bool containFlipSyms, bool isHit = false)
        {
            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                var reel = reelGroup.GetReel(reelIndex);
                var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX);

                if (containFlipSyms == false)
                {
                    if (extraInfo.IsContainPrevFlipSyms(reelIndex)) continue;
                }

                int visualIndex = (symbol.IsScatter && Info.IsRespin == false && Info.IsRespinEnd == false) ? 1 : 0;

                if (symbol.IsScatter == false)
                {
                    symbol.SetVisual(SymbolVisualType.Idle, visualIndex);
                }
                else
                {
                    if (IsTurbo == false)
                    {
                        if (isHit)
                        {
                            SetScatterDimd(symbol, true);
                        }
                        else
                        {
                            SetScatterDimd(symbol, false);
                        }
                    }
                }
            }
        }

        private void SetScatterDimd(Symbol symbol, bool enable)
        {
            if (enable)
            {
                symbol.SetVisual(dimdVisual);
            }
            else
            {
                symbol.SetVisual(SymbolVisualType.Idle, 1);
            }
        }
        
        private void ReleaseAllMultiText(bool immediatley)
        {
            if (immediatley && isFirstTrail) return;

            foreach (var mul in multiList)
            {
                mul.StartDisappear(IsTurbo);
            }

            if (immediatley) isFirstTrail = true;
        }

        //---------------------------------------------------------------------------------------------------------------
        // Popup
        //---------------------------------------------------------------------------------------------------------------

        protected override IEnumerator OpenFreespinPopup()
        {
            yield return sharkFx.OnTransition();

            yield return PopupSystem.Instance.Open<FreeSpinStartPopup1088>(slotAssets, FREE_START_POPUP)
                                    .OnInitialize(p => p.Initialize(OnTransitionBaseToFree, Info.FreespinTotalCount, holdType != HoldType.On, useVibrate))
                                    .Cache()
                                    .SetBackgroundAlpha(0.0f)
                                    .WaitForClose();

            yield return sharkFx.OnIntro(FeatureKind.Free.ToString());
        }

        protected override IEnumerator OpenResultWinPopup()
        {
            OffFlipWinCoin();

            yield return sharkFx.OnTransition();

            yield return PopupSystem.Instance.Open<FreeSpinEndPopup1088>(slotAssets, FREE_END_POPUP)
                                           .OnInitialize(p =>
                                           {
                                               p.Initialize(OnTransitionFreeToBase, Info.TotalWinCoins, holdType != HoldType.On);
                                           })
                                           .Cache()
                                           .SetBackgroundAlpha(0.0f)
                                           .WaitForClose();

            yield return sharkFx.OnIntro(FeatureKind.Base.ToString());
        }

        protected override IEnumerator OpenResultLosePopup()
        {
            OffFlipWinCoin();
            yield return sharkFx.OnTransition();
            
            yield return PopupSystem.Instance.Open<ResultLosePopup>(commonAssets, POPUP_NAME_RESULT_LOSE)
                                            .OnInitialize(p =>
                                            {
                                                p.Initialize(Info.TotalWinCoins, holdType != HoldType.On);
                                            })
                                            .OnClose(OnTransitionFreeToBase)
                                            .SetLayer(fullSizeResultLosePopup ? null : DEFAULT_LAYER)
                                            .Cache()
                                            .WaitForClose();

            yield return sharkFx.OnIntro(FeatureKind.Base.ToString());
        }
        //---------------------------------------------------------------------------------------------------------------

        private void OnTransitionFreeToBase()
        {
            CurrentFeature = FeatureKind.Base;

            SetReelMode(ReelMode.Regular);

            if (saveSymbolsBeforeFreespins == true)
            {
                if (saveSymbolsWithReels)
                {
                    RestoreReelsBeforeFreeGames();
                }

                LoadSymbols();
            }

            SetVisualAllMainSymbol(true);

            NextState(SlotMachineState.RESULT_END);
            DispatchGameUiRaycast(true);

        }

        private void OnTransitionBaseToFree()
        {
            buyFeature.UpdateState();
            CurrentFeature = FeatureKind.Free;

            DispatchChangeFreespins(Info.FreespinCurrentCount, Info.FreespinTotalCount);

            SetReelMode(ReelMode.Free);
        }

        protected override (bool, string) CheckEarnCoinValidity(long earnCoin)
        {
            // return base.CheckEarnCoinValidity(earnCoin);

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
    }
}