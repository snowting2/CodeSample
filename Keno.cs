using SlotGame.Popup;
using UnityEngine;

using SlotGame.Attribute;
using System.Collections.Generic;
using DG.Tweening.Plugins.Options;


namespace SlotGame.Machine
{
    ///--------------------------------------------------------------------------------
    /// SlotMachine
    ///--------------------------------------------------------------------------------
    public abstract partial class Keno : SlotMachine
    {
        protected bool IsPickWaiting = false;

#pragma warning disable 0649
        [Separator("* User Input Extra Index")]
        [SerializeField] private int userInputStartIndex;
        [SerializeField] private int userInputCount;
        public int UserInputCount { get { return userInputCount; } }

        [Separator("* KenoBlockGroup")]
        [SerializeField] private KenoManager kenoManager;

        public KenoManager KenoManager
        {
            get { return kenoManager; }
        }
#pragma warning restore 0649

        // Keno Popup Ball Object Pool
        [Separator("* Keno Popup Balls")]
        [SerializeField] protected KenoWinPopupBall popupBallRef = null;
        [SerializeField] protected GameObject popupBallRoots = null;

        protected GameObjectPool<KenoWinPopupBall> popupBallOP = null;
        private List<int> userInputList = new List<int>();

        private const int POPUP_BALL_COUNT = 10;

        private void OnDestroy()
        {
            popupBallOP.Release();

            try
            {
                RemovePopups();
            }
            catch (System.Exception)
            {
                Debug.LogError("Failed to Remove cached Popups");
            }
        }

        protected override void SetupPopups()
        {
            PopupSystem.Instance.AddCachedPopup<KenoWinPopup>(commonAssets, GetPopupNameForOrientation(POPUP_NAME_KENO_WIN));
            PopupSystem.Instance.AddCachedPopup<KenoMultiplePopup>(commonAssets, GetPopupNameForOrientation(POPUP_NAME_KENO_MULTIPLE));
        }

        protected override void RemovePopups()
        {
            PopupSystem.Instance.RemoveCachedPopup(GetPopupNameForOrientation(POPUP_NAME_KENO_WIN));
            PopupSystem.Instance.RemoveCachedPopup(GetPopupNameForOrientation(POPUP_NAME_KENO_MULTIPLE));
        }

        public override void StartSpin()
        {
            if (CurrentState != SlotMachineState.IDLE)
            {
                SkipResult();
                return;
            }

            KenoManager.GetUserPickInfo(ref userInputList);

            if (userInputList.Count == 0)
            {
                return;
            }

            if (kenoManager.IsSameInputValue() == false)
            {
                DispatchKenoSpecialRequest(userInputList.ToArray());
            }

            if (CurrentState == SlotMachineState.IDLE)
            {
                NextState(SlotMachineState.KENO_PICK);
            }
        }

        private void StartSpin(bool isPaid)
        {
            if (IsInitialized() == false)
            {
                return;
            }

            if (CurrentState != SlotMachineState.KENO_PICK)
            {
                Debug.Log("[SlotMachine] This machine is busy.");
                return;
            }

            Info.isPaid = isPaid;

            if (isPaid == true)
            {
                if (HasEnoughCoinsForSpin() == false)
                {
                    Debug.Log("[SlotMachine] Not enough coins.");

                    spinFailures = SpinFailures.ShortOfCoins;

                    if (AutoSpin)
                    {
                        AutoSpin = false;
                        UpdateAutoPlayData();
                    }

                    DispatchMoreCoins();

                    NextState(SlotMachineState.IDLE);
                    return;
                }
            }

            Debug.Log("[SlotMachine] Start spin");
            Debug.LogFormat("[SlotMachine] Pay for spin : {0} Coins", (isPaid ? StringUtils.ToComma(Info.CurrentTotalBet) : "free"));
            Debug.LogFormat("[SlotMachine] My coins : {0} Coins", Info.coins);

            NextState(SlotMachineState.SPIN_START);
        }

        public override void StopSpin(SpinData spinData)
        {
            if (IsInitialized() == false)
            {
                return;
            }

            if (CurrentState != SlotMachineState.SPIN_START)
            {
                Debug.Log("[SlotMachine] wrong state.");
                return;
            }

            if (Info.isPaid == true)
            {
                PayCoins(Info.CurrentTotalBet);
            }

            Info.SetData(spinData, machineGroup.EnabledCount, MachineGroup.MainItem.reelGroup.Column, MachineGroup.MainItem.reelGroup.Row, includeFeatureToLineWin);
            DispatchChangeJackpotCoins(Info.jackpotCoins);

            NextState(SlotMachineState.SPIN_UPDATE);
        }

        protected override void ReelStripJumpToStartIndex(EnterData enterData)
        {
            return;
        }

        protected override void SaveSymbols(bool includeStickySymbol = false)
        {
            // base.SaveSymbols(includeStickySymbol);
        }

        protected override void LoadSymbols()
        {
            // base.LoadSymbols();
        }

        protected virtual void KenoSetup()
        {
            SetKenoGroup();
            SetPopupBalls();
        }

        protected virtual void SetKenoGroup()
        {
            List<int> enterUserInput = new List<int>();

            // 유저 입력 초기값 세팅
            if (Info.GetExtrasCount() > UserInputCount)
            {
                int endIndex = userInputStartIndex + UserInputCount;
                for (int i = userInputStartIndex; i < endIndex; i++)
                {
                    var extraValue = Info.GetExtraValue(i);
                    enterUserInput.Add((int)extraValue);
                }
            }

            kenoManager.Initialize(enterUserInput);
            KenoManager.OnSpinStateLock.AddListener(OnSpinStateLock);
            KenoManager.OnHitCountUpdate.AddListener(OnHitCountUpdate);
            KenoManager.OnMarkCountUpdate.AddListener(OnMarkCountUpdate);
            KenoManager.OnUserPickBlock.AddListener(OnUserPickBlock);
            KenoManager.OnHitState.AddListener(OnHitState);
            KenoManager.OnSpecialBallHit.AddListener(OnSpecialBallHit);
            KenoManager.OnNormalMatch.AddListener(OnNormalMatch);
            KenoManager.OnServerPickBlock.AddListener(OnServerPickBlock);
            KenoManager.OnFreeMatch.AddListener(OnFreeMatch);

            OnMarkCountUpdate(KenoManager.GetPickCount());
        }

        protected virtual void OnSpinStateLock(bool locked)
        {
            DispatchEnableSpinButton(!locked);
            DispatchEnableAutoModeButton(!locked);
        }

        protected virtual void OnHitCountUpdate(int count) { }

        protected virtual void OnMarkCountUpdate(int count)
        {
            SetKenoDimd(false);
        }

        protected virtual void OnUserPickBlock() { }
        protected virtual void OnHitState(int count) { }
        protected virtual void OnSpecialBallHit(int num) { }
        protected virtual void OnNormalMatch() { }
        protected virtual void OnServerPickBlock() { }
        protected virtual void OnFreeMatch() { }

        protected virtual void SetPopupBalls()
        {
            popupBallOP = new GameObjectPool<KenoWinPopupBall>(popupBallRoots, POPUP_BALL_COUNT,
                                                () =>
                                                {
                                                    return Instantiate(popupBallRef);
                                                });
        }

        protected virtual KenoWinPopup.WinType GetWinType(bool jackpot, long hitCount)
        {
            KenoWinPopup.WinType winType = KenoWinPopup.WinType.NONE;

            if (jackpot)
            {
                if (hitCount == 0) winType = KenoWinPopup.WinType.JACKPOT_GRAND;
                else if (hitCount == 1) winType = KenoWinPopup.WinType.JACKPOT_MAJOR;
                else if (hitCount == 2) winType = KenoWinPopup.WinType.JACKPOT_MINOR;
            }
            else
            {
                if (hitCount == 2) winType = KenoWinPopup.WinType.HIT_2;
                else if (hitCount == 3) winType = KenoWinPopup.WinType.HIT_3;
                else if (hitCount == 4) winType = KenoWinPopup.WinType.HIT_4;
                else if (hitCount == 5) winType = KenoWinPopup.WinType.HIT_5;
                else if (hitCount == 6) winType = KenoWinPopup.WinType.HIT_6;
                else if (hitCount == 7) winType = KenoWinPopup.WinType.HIT_7;
                else if (hitCount == 8) winType = KenoWinPopup.WinType.HIT_8;
                else if (hitCount == 9) winType = KenoWinPopup.WinType.HIT_9;
            }

            return winType;
        }
    }
}