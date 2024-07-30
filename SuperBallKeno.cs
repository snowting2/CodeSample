using System.Collections;
using UnityEngine;
using SlotGame.Attribute;
using System.Collections.Generic;
using SlotGame.Popup;
using SlotGame.Sound;

namespace SlotGame.Machine.S1059
{
    public class SlotMachine1059 : Keno
    {
#pragma warning disable 0649
        [Separator("Extensions")]
        [SerializeField] private KenoUI1059 kenoUI;
        [SerializeField] private KenoBallPot1059 kenoBallPot;
        [SerializeField] private RollingMessage1059 rollingMsg;
        [SerializeField] private RectTransform targetBallPos;

        [Separator("Time")]
        [SerializeField] private float freeChangeTime;
        [SerializeField] private float resultOpenTime;
        [SerializeField] private float multiOpenTime;
        [SerializeField] private float spinEndWaitTime;
        [SerializeField] private float spinEndHitWaitTime;
        [SerializeField] private float freeBlockCreatTime;

        [Space(10)]
        [SerializeField] private float hitWaitTime;
        [SerializeField] private float hitWait4XTime;
        [SerializeField] private float hitWait16XTime;

        [Separator("Sounds")]
        [SerializeField] private SoundPlayer bgmBase;

        [SerializeField] private SoundPlayer freeAppearSound;
        [SerializeField] private SoundPlayer freeAppearSound2;
        [SerializeField] private SoundPlayer normalMatchSound;
        [SerializeField] private SoundPlayer freeMatchSound;
        [SerializeField] private SoundPlayer userPickSound;
        [SerializeField] private SoundPlayer serverPickSound;

        [SerializeField] private SoundPlayer freeHitSound;

        [SerializeField] private SoundPlayer multiPopupSound;

#pragma warning restore 0649

        private readonly int EXTRA_INDEX_FREE = 10;
        private readonly int EXTRA_COUNT_FREE = 3;

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

        private readonly string POPUP_FREE_START = "FreeSpinPopup";
        private readonly string POPUP_RESULT_WIN = "FreeSpinResultPopup";
        private readonly string POPUP_MULTI_4X = "MultiplePopup_4X";
        private readonly string POPUP_MULTI_16X = "MultiplePopup_16X";

        //---------------------------------------------------------------------

        private readonly int JUKE_BOX_BASE = 0;
        private readonly int JUKE_BOX_FREE = 1;

        private readonly int MULTI_COUNT_4X = 4;
        private readonly int MULTI_COUNT_16X = 16;

        //---------------------------------------------------------------------

        private int superballHitCount = 0;

        // 슬롯머신 진입시 최초 한번 실행되어야 할 스테이트
        // 이 부분에 슬롯머신 진입 애니메이션 등을 구현해야함.
        protected override IEnumerator EnterState()
        {
            kenoUI.SetTotalBet(Info.CurrentTotalBet);

            if (Info.IsFreespin)
            {
                kenoBallPot.SetFreeSpin();
            }

            yield return base.EnterState();
        }

        protected override void SetupPopups()
        {
            base.SetupPopups();

            PopupSystem.Instance.AddCachedPopup<PopupBehaviour>(slotAssets, POPUP_FREE_START);
            PopupSystem.Instance.AddCachedPopup<ResultWinPopup>(slotAssets, POPUP_RESULT_WIN);
        }

        protected override void RemovePopups()
        {
            base.RemovePopups();

            PopupSystem.Instance.RemoveCachedPopup(GetPopupNameForOrientation(POPUP_FREE_START));
            PopupSystem.Instance.RemoveCachedPopup(GetPopupNameForOrientation(POPUP_RESULT_WIN));
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

        protected override void OnReelModeChanged(ReelMode reelMode)
        {
            if (reelMode == ReelMode.Free)
            {
                winSoundPlayer.SelectJukeBox(JUKE_BOX_FREE);
            }
            else
            {
                winSoundPlayer.SelectJukeBox(JUKE_BOX_BASE);
            }
        }


        // 기본 상태.
        // 기본적으로 유저의 입력을 기다리지만 프리스핀, 리스핀, 오토스핀등의 상황에선 자동으로 스핀 상태로 변경.
        protected override IEnumerator IdleState()
        {
            superballHitCount = 0;

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

            kenoBallPot.SetFallOutOpen();

            yield return base.SpinStartState();

            rollingMsg.OnSpinEndHitWaiting();

            yield return kenoBallPot.Release();
        }

        // 스핀이 완전히 종료되었을때 호출 되는 스테이트
        // 스핀 종료 후 특별한 연출등을 표현 (ex : 스택드 와일드 합체 등등)
        protected override IEnumerator SpinEndState()
        {
            // 프리 블록 세팅
            int startIndex = EXTRA_INDEX_FREE;
            int endIndex = EXTRA_INDEX_FREE + EXTRA_COUNT_FREE;

            if (CurrentReelMode == ReelMode.Regular)
            {
                freeAppearSound.Play();
            }
            else if (CurrentReelMode == ReelMode.Free)
            {
                freeAppearSound2.Play();
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                var freeNum = Info.GetExtraValue(i);
                KenoManager.SetFreeBlock((int)freeNum);

                yield return WaitForSeconds(freeBlockCreatTime);
            }

            yield return WaitForSeconds(freeChangeTime);

            // 유저 입력 매칭

            startIndex = EXTRA_INDEX_RESULT;
            endIndex = EXTRA_INDEX_RESULT + EXTRA_COUNT_RESULT;

            int lastIndex = (CurrentReelMode == ReelMode.Free) ? (endIndex - 2) : (endIndex - 1);

            for (int i = startIndex; i < endIndex; i++)
            {
                var resultNum = (int)Info.GetExtraValue(i);

                bool isLast = false;

                if (lastIndex <= i)
                {
                    isLast = true;
                }

                bool isHit = false;

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
                    isHit = KenoManager.GetResultBlock(resultNum);
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
            if (multiple == 0)
            {
                yield return WaitForSeconds(hitWaitTime);
            }
            else if (multiple == 4)
            {
                yield return WaitForSeconds(hitWait4XTime);
            }
            else
            {
                yield return WaitForSeconds(hitWait16XTime);
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
            return base.HitNormal(winType, ballRefs, winCoinsOriginal, winCoins, totalWinCoinsOriginal, totalWinCoins, multiple);
        }

        protected override void HitKenoBlock()
        {
            base.HitKenoBlock();
        }

        protected override IEnumerator OpenMultiplierPopup(long multiple)
        {
            yield return WaitForSeconds(multiOpenTime);

            rollingMsg.FadeMultiText();
            string popupName = (multiple == 4) ? POPUP_MULTI_4X : POPUP_MULTI_16X;

            multiPopupSound.Play();

            yield return PopupSystem.Instance.Open<PopupBehaviour>(slotAssets, popupName)
                                            .Cache()
                                            .WaitForClose();
        }
        

        // 스캐터 히트 스테이트
        protected override IEnumerator ScatterState()
        {
            rollingMsg.OnFreeSpin();

            yield return base.ScatterState();
        }

        protected override void HitScatterSymbols()
        {
            freeHitSound.Play();
            base.HitScatterSymbols();
        }

        // 프리스핀을 시작하는 스테이트
        // 프리스핀 팝업을 보여줌
        protected override IEnumerator FreespinStartState()
        {
            bgmBase.Stop();

            yield return base.FreespinStartState();

            kenoBallPot.SetFreeSpin();
        }

        protected override IEnumerator OpenFreespinPopup()
        {
            yield return PopupSystem.Instance.Open<PopupBehaviour>(slotAssets, POPUP_FREE_START)
                                                .Cache()
                                                .WaitForClose();
        }

        // 프리스핀 종료 스테이트
        // 프리스핀 종료 팝업을 보여줌
        protected override IEnumerator FreespinEndState()
        {
            yield return base.FreespinEndState();

            kenoBallPot.SetNormal();
        }

        protected override IEnumerator OpenResultWinPopup()
        {
            yield return PopupSystem.Instance.Open<ResultWinPopup>(commonAssets, POPUP_RESULT_WIN)
                                           .OnInitialize(p =>
                                           {
                                               p.Initialize(Info.TotalWinCoins,
                                                            holdType != HoldType.On,
                                                            null);
                                           })
                                           .Cache()
                                           .WaitForStartClosing();
        }

        protected override void DispatchErase()
        {
            HideHitBar();
            SetKenoDimd(false);

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

        protected override void OnSpecialBallHit(int num)
        {
            if (Info.SpinResult.WinCoinsHit == 0)
            {
                Debug.Log("No Hit - Super Ball passed");
                return;
            }

            int mul = (superballHitCount == 0) ? MULTI_COUNT_4X : MULTI_COUNT_16X;

            rollingMsg.OnMultiplier(mul);

            superballHitCount++;
        }

        private void HideHitBar()
        {
            kenoUI.HideHitBar();
        }

        protected override void OnNormalMatch()
        {
            normalMatchSound.Play();
        }

        protected override void OnFreeMatch()
        {
            freeMatchSound.Play();
        }

        protected override void OnUserPickBlock()
        {
            userPickSound.Play();
        }

        protected override void OnServerPickBlock()
        {
            serverPickSound.Play();
        }
    }
}