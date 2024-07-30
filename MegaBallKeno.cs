using System.Collections;
using UnityEngine;
using SlotGame.Attribute;
using System.Collections.Generic;
using SlotGame.Popup;
using SlotGame.Sound;

namespace SlotGame.Machine.S1058
{
    public class SlotMachine1058 : Keno
    {
#pragma warning disable 0649
        [Separator("Extensions")]
        [SerializeField] private KenoUI1058 kenoUI;
        [SerializeField] private KenoBallPot1058 kenoBallPot;
        [SerializeField] private RollingMessage1058 rollingMsg;
        [SerializeField] private RectTransform targetBallPos;
        [SerializeField] private MultiBlock1058 multiBlock;
        [SerializeField] private GameObject bottomTarget;

        [Separator("Time")]
        [SerializeField] private float resultOpenTime;

        [SerializeField] private float spinEndWaitTime;
        [SerializeField] private float spinEndHitWaitTime;

        [SerializeField] private float multiPopupOpenTime;

        [Space(10)]
        [SerializeField] private float hitWaitTime;
        [SerializeField] private float normalSpinInterval;

        [Separator("Sounds")]
        [SerializeField] private SoundPlayer bgmBase;

        [Space(10)]
        [SerializeField] private SoundPlayer normalMatchSound;
        [SerializeField] private SoundPlayer userPickSound;
        [SerializeField] private SoundPlayer serverPickSound;

#pragma warning restore 0649

        private readonly int EXTRA_INDEX_RESULT = 13;
        private readonly int EXTRA_COUNT_RESULT = 20;

        private readonly int EXTRA_INDEX_HIT_COUNT = 33;
        private readonly int EXTRA_INDEX_MULTI_VALUE = 35;

        private readonly int EXTRA_INDEX_HOT = 36;
        private readonly int EXTRA_COUNT_HOT = 10;

        private readonly int EXTRA_INDEX_COLD = 46;
        private readonly int EXTRA_COUNT_COLD = 10;

        private readonly int EXTRA_INDEX_SUPER = 56;
        private readonly int EXTRA_COUNT_SUPER = 10;

        //---------------------------------------------------------------------

        private readonly string POPUP_MULTIPLE = "MultiBlockPopup1058";

        //---------------------------------------------------------------------

        // 슬롯머신 진입시 최초 한번 실행되어야 할 스테이트
        // 이 부분에 슬롯머신 진입 애니메이션 등을 구현해야함.
        protected override IEnumerator EnterState()
        {
            kenoUI.SetTotalBet(Info.CurrentTotalBet);
            multiBlock.Initialize();

            yield return base.EnterState();
        }

        protected override void SetupPopups()
        {
            base.SetupPopups();

            PopupSystem.Instance.AddCachedPopup<KenoMultiplePopup1058>(slotAssets, POPUP_MULTIPLE);
        }

        protected override void RemovePopups()
        {
            base.RemovePopups();

            PopupSystem.Instance.RemoveCachedPopup(GetPopupNameForOrientation(POPUP_MULTIPLE));
        }

        protected override void UpdateJackpotCoins()
        {
            if (Info.GetExtrasCount() < (EXTRA_INDEX_COLD + EXTRA_COUNT_COLD))
            {
                return;
            }

            List<string> hotValues = new List<string>();
            for (int i = EXTRA_INDEX_HOT; i < EXTRA_INDEX_HOT + EXTRA_COUNT_HOT; i++)
            {
                var value = Info.GetExtraValue(i);
                hotValues.Add(value.ToString());
            }

            List<string> coldValues = new List<string>();
            for (int i = EXTRA_INDEX_COLD; i < EXTRA_INDEX_COLD + EXTRA_COUNT_COLD; i++)
            {
                var value = Info.GetExtraValue(i);
                coldValues.Add(value.ToString());
            }

            List<string> superValues = new List<string>();

            if (EXTRA_INDEX_SUPER < Info.GetExtrasCount())
            {
                for (int i = EXTRA_INDEX_SUPER; i < EXTRA_INDEX_SUPER + EXTRA_COUNT_SUPER; i++)
                {
                    var value = Info.GetExtraValue(i);
                    superValues.Add(value.ToString());
                }
            }

            kenoUI.SetHotColdNumber(hotValues, coldValues, superValues);

            kenoUI.OnChangeJackpotCoins(Info.finalJackpotCoins);

            base.UpdateJackpotCoins();
        }



        // 기본 상태.
        // 기본적으로 유저의 입력을 기다리지만 프리스핀, 리스핀, 오토스핀등의 상황에선 자동으로 스핀 상태로 변경.
        protected override IEnumerator IdleState()
        {
            rollingMsg.OnIdle();
            yield return base.IdleState();
        }
        public override void StartSpin()
        {
            if (KenoManager.OnEnableSpinState() == false)
            {
                rollingMsg.PlayAnimNotEnoughPick();
                return;
            }

            base.StartSpin();
        }

        // 스핀을 시작하는 스테이트.
        protected override IEnumerator SpinStartState()
        {
            HideHitBar();

            StartCoroutine(multiBlock.OnStop());

            kenoBallPot.SetFallOutOpen();

            yield return base.SpinStartState();

            rollingMsg.OnSpinEndHitWaiting();

            yield return kenoBallPot.Release();
        }
        

        // 스핀이 완전히 종료되었을때 호출 되는 스테이트
        // 스핀 종료 후 특별한 연출등을 표현 (ex : 스택드 와일드 합체 등등)
        protected override IEnumerator SpinEndState()
        {
            int startIndex = EXTRA_INDEX_RESULT;
            int endIndex = EXTRA_INDEX_RESULT + EXTRA_COUNT_RESULT;

            int lastIndex = (endIndex - 1);

            for (int i = startIndex; i < endIndex; i++)
            {
                var resultNum = (int)Info.GetExtraValue(i);

                bool isLast = false;

                if (lastIndex <= i)
                {
                    isLast = true;
                }

                rollingMsg.OnHitCountPrevUpdate();

                if (isLast)
                {
                    var block = KenoManager.GetBlock(resultNum);
                    Vector2 startPos = new Vector2(block.transform.position.x, targetBallPos.position.y);
                    Vector2 bouncePos = block.transform.position;
                    bool isRight = (lastIndex == i) ? true : false;

                    yield return kenoBallPot.ShowLastBall(resultNum, startPos, bouncePos, isRight);
                }
                else
                {
                    bool isHit = KenoManager.GetResultBlock(resultNum);
                    kenoBallPot.ShowBall(resultNum, isHit);
                }

                yield return WaitForSeconds(resultOpenTime);
            }

            long multiple = Info.GetExtraValue(EXTRA_INDEX_MULTI_VALUE);
            if (multiple > 0)
            {
                yield return WaitForSeconds(spinEndHitWaitTime);
            }
            else
            {
                yield return WaitForSeconds(spinEndWaitTime);
            }

            yield return base.SpinEndState();
        }

        // 스핀이 멈추고 결과를 시작 되는 스테이트
        // 잭팟, 빅윈 등에 분기점
        protected override IEnumerator ResultStartState()
        {
            yield return null;

            WinGrade winGrade = Info.SpinResult.WinGrade;
            if (winGrade == WinGrade.None)
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
        
        protected override float GetResultEndDelay()
        {
            if (Info.IsFreespin)
            {
                return FreeSpinInterval;
            }
            else
            {
                if (AutoSpin)
                {
                    if (CurrentPlayModeInProgress == PlayMode.Turbo)
                    {
                        return turboAutoSpinInterval;
                    }
                    else
                    {
                        return AutoSpinInterval;
                    }
                }
                else
                {
                    return normalSpinInterval;
                }
            }
        }
        
        // 일반 히트 스테이트
        protected override IEnumerator HitNormalState()
        {
            WinGrade winGrade = Info.SpinResult.WinGrade;

            long winCoinsOriginal = Info.SpinResult.WinCoinsHitOriginal; // 윈코인 원 금액
            long winCoins = Info.SpinResult.WinCoinsHit;                 // 윈코인 배수 적용 금액
            long totalWinCoinsOriginal = Info.TotalWinCoins - winCoins + winCoinsOriginal;  // 토탈 윈코인 원금액 합산
            long totalWinCoins = Info.TotalWinCoins;                                        // 토탈 윈코인 배수 적용 합산 금액

            long hitCount = Info.SpinResult.IsJackpot() ? Info.SpinResult.JackpotGrade : Info.GetExtraValue(EXTRA_INDEX_HIT_COUNT);
            long multiple = Info.GetExtraValue(EXTRA_INDEX_MULTI_VALUE);

            var winType = GetWinType(Info.SpinResult.IsJackpot(), hitCount);

            // 슈퍼볼 히트 갯수에 따른 히트 대기 시간 추가
            if (multiple > 1)
            {
                yield return WaitForSeconds(hitWaitTime);
            }

            if (IsBigWin(winCoins) == false)
            {
                yield return HitNormal(winGrade, winCoinsOriginal, winCoins, totalWinCoinsOriginal, totalWinCoins, multiple);
            }
            else
            {
                var hitBlocks = KenoManager.GetHitBlocks();
                KenoWinPopupBall[] ballRefs = new KenoWinPopupBall[hitBlocks.Count];

                for (int i = 0; i < hitBlocks.Count; i++)
                {
                    var block = hitBlocks[i];

                    var popupBall = popupBallOP.Get();
                    bool isLastHitBall = kenoBallPot.IsLastHitBall(block.Number);

                    popupBall.Initialize(block.Number.ToString(), isLastHitBall);
                    ballRefs[i] = popupBall;
                }

                if (Info.SpinResult.IsJackpot())
                {
                    yield return HitJackpot(winType, ballRefs, winCoins, totalWinCoins);
                }
                else
                {
                    yield return HitNormal(winType, ballRefs, winCoinsOriginal, winCoins, totalWinCoinsOriginal, totalWinCoins, multiple);
                }

                ballRefs.Initialize();
            }

            var nextState = GetStateBeforeEnd(Info.IsExtraSpin, Info.IsFreespinStart, Info.IsFreespinEnd, Info.IsRespin, Info.IsFreeChoiceStart);
            NextState(nextState);
        }

        protected override IEnumerator HitNormal(KenoWinPopup.WinType winType, KenoWinPopupBall[] ballRefs, long winCoinsOriginal, long winCoins, long totalWinCoinsOriginal, long totalWinCoins, long multiple)
        {
            float winDuration;
            if (winSoundPlayer != null && winSoundPlayer.enabled == true)
            {
                winSoundInfo = winSoundPlayer.GetInfo(winCoinsOriginal, GetCurrentTotalBet());
                winDuration = winSoundInfo.duration;
            }
            else
            {
                winSoundInfo = null;
                winDuration = winCoinDuration;
            }

            DispatchWinCoins(winCoinsOriginal, winDuration, WinGrade.None);
            DispatchTotalWinCoins(totalWinCoinsOriginal, winDuration);

            yield return HitSymbols(winDuration);

            if (multiple > 1)
            {
                DispatchWinCoins(winCoinsOriginal, 0.0f, WinGrade.None);
                DispatchTotalWinCoins(totalWinCoinsOriginal, 0.0f);

                skipResult = false;

                yield return OpenMultiplierPopup(multiple);

                winSoundInfo = null;
            }

            WinGrade winGrade = SlotMachineUtils.GetWinGrade(winCoins, Info.CurrentTotalBet);
            bool vibrate = useVibrate && winGrade >= WinGrade.Huge;

            PopupObject<KenoWinPopup> popupObject = PopupSystem.Instance.Open<KenoWinPopup>(commonAssets, POPUP_NAME_KENO_WIN)
                                                                        .OnInitialize(p =>
                                                                        {
                                                                            p.Initialize(winCoins,
                                                                                         winType,
                                                                                         ballRefs,
                                                                                         vibrate,
                                                                                         IsWinPopupSkip());
                                                                        })
                                                                        .SetLayer(fullSizeBigwinPopup ? null : DEFAULT_LAYER)
                                                                        .Cache()
                                                                        .OnClose(() =>
                                                                        {
                                                                            foreach (var ball in ballRefs)
                                                                            {
                                                                                popupBallOP.Return(ball);
                                                                            }
                                                                        });

            yield return null;
            
            DispatchTotalWinCoins(totalWinCoins, popupObject.GetPopup().TextDuration);

            yield return popupObject.WaitForClose();

            DispatchWinCoins(winCoins, 0.0f, WinGrade.None);
            DispatchTotalWinCoins(totalWinCoins, 0.0f);
        }

        protected override void HitKenoBlock()
        {
            base.HitKenoBlock();
        }

        protected override IEnumerator OpenMultiplierPopup(long multiple)
        {
            yield return WaitForSeconds(multiPopupOpenTime);

            rollingMsg.FadeMultiText();

            var popupObject = PopupSystem.Instance.Open<KenoMultiplePopup1058>(slotAssets, POPUP_MULTIPLE)
                                            .OnInitialize(p =>
                                            {
                                                p.Initialize(multiple, multiBlock.transform.position, bottomTarget.transform.position);
                                            })
                                            .Cache();

            yield return null;

            yield return new WaitUntil(() => popupObject.GetPopup().IsCloseStart);
        }

        protected override void DispatchErase()
        {
            HideHitBar();
            SetKenoDimd(false);

            multiBlock.OnStopImmediatly();
            KenoManager.OnErase();
        }

        protected override void DispatchQuickPick()
        {
            HideHitBar();
            SetKenoDimd(false);

            KenoManager.OnQuickPick();
        }

        protected override void DispatchChangeTotalBet(long totalBet)
        {
            kenoUI.OnChangeTotalBet(totalBet);

            base.DispatchChangeTotalBet(totalBet);

            KenoManager.SetSpinState();
        }

        protected override void OnHitCountUpdate(int count)
        {
            kenoUI.SetHitCount(count);
            if (count > 0)
            {
                rollingMsg.OnHitCountUpdate(count);
            }
        }

        protected override void OnMarkCountUpdate(int count)
        {
            kenoUI.SetMarkCount(count);
            rollingMsg.OnPickCountUpdate(count);

            base.OnMarkCountUpdate(count);
        }

        protected override void OnHitState(int count)
        {
            kenoUI.ShowHitBar(count);

            if (Info.SpinResult.IsJackpot() == true)
            {
                rollingMsg.OnJackpot(Info.SpinResult.JackpotGrade);
            }
        }

        public void OnMegaBallBounce(int num)
        {
            int hitCount = KenoManager.GetHitCount();

            rollingMsg.OnMultiplier(hitCount);
            OnMultiBlock(num);
        }

        protected override void OnSpecialBallHit(int num)
        {
            if (Info.SpinResult.WinCoinsHit == 0)
            {
                Debug.Log("No Hit - Super Ball passed");
                return;
            }

            int hitCount = KenoManager.GetHitCount();

            if (hitCount < 2) return;

            rollingMsg.OnMegaMultiplier(hitCount);

            OnMultiBlock(num);
        }

        private void OnMultiBlock(int blockNum)
        {
            var block = KenoManager.GetBlock(blockNum);

            if (Info.SpinResult.IsJackpot())
            {
                int jackpotIndex = Info.SpinResult.JackpotGrade;
                multiBlock.OnPlayJackpot(jackpotIndex, block.transform.position);
            }
            else
            {
                long multiple = Info.GetExtraValue(EXTRA_INDEX_MULTI_VALUE);
                if (multiple > 1)
                {
                    multiBlock.OnPlayMultiple((int)multiple, block.transform.position);
                }
            }
        }

        private void HideHitBar()
        {
            kenoUI.HideHitBar();
        }

        protected override void OnNormalMatch()
        {
            normalMatchSound.Play();
        }

        protected override void OnUserPickBlock()
        {
            multiBlock.OnStopImmediatly();
            userPickSound.Play();
        }

        protected override void OnServerPickBlock()
        {
            serverPickSound.Play();
        }
    }
}