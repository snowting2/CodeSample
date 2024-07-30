using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace SlotGame.Machine.S1058
{
    public enum KenoUIState
    {
        PayTable,
        HotCold,
        None
    }

    public abstract class KenoUIEvent : MonoBehaviour
    {
        protected class OnStateChange : UnityEvent<KenoUIState> { };
        protected OnStateChange onStateChange = new OnStateChange();
    }

    public class KenoUI1058 : KenoUIEvent
    {
#pragma warning disable 0649
        [SerializeField] private Text markCountText;
        [SerializeField] private Text hitCountText;

        [SerializeField] private PayTable1058 payTableUI = null;
        [SerializeField] private HotCold1058 hotColdUI = null;

#pragma warning restore 0649

        private void Awake()
        {
            hotColdUI.Initialize(OnChangeState);
            payTableUI.Initialize(OnChangeState);
        }
        private void Start()
        {
            onStateChange.RemoveAllListeners();
            onStateChange.AddListener(payTableUI.SetState);
            onStateChange.AddListener(hotColdUI.SetState);
        }

        private void OnChangeState(KenoUIState state)
        {
            onStateChange.Invoke(state);
        }

        public void SetTotalBet(long totalBet)
        {
            payTableUI.SetTotalBet(totalBet);
        }

        public void SetMarkCount(int count)
        {
            markCountText.text = count.ToString();

            payTableUI.OnUpdateMarkCount(count);

            HideHitBar();
        }

        public void SetHitCount(int count)
        {
            hitCountText.text = count.ToString();
        }

        public void SetHotColdNumber(List<string> hot, List<string> cold, List<string> supers)
        {
            hotColdUI.UpdateInfo(hot, cold, supers);
        }

        public void OnChangeTotalBet(long totalBet)
        {
            payTableUI.OnUpdateTotalBet(totalBet);
        }

        public void OnChangeJackpotCoins(List<long> jackpotCoins)
        {
            payTableUI.OnUpdateJackpot(jackpotCoins);
        }

        public void ShowHitBar(int count)
        {
            payTableUI.ShowHitBar(count);
        }

        public void HideHitBar()
        {
            payTableUI.HideHitBar();
        }
    }

    public abstract class KenoUIBase : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField] protected Button changeStateBtn;
#pragma warning restore 0649

        protected KenoUIState currentState = KenoUIState.None;

        public virtual void Initialize(UnityAction<KenoUIState> onChangeState)
        {
            changeStateBtn.onClick.AddListener(() => onChangeState(currentState));
        }

        public virtual void SetState(KenoUIState state)
        {
            if (currentState == state)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        protected virtual void Open()
        {
            this.gameObject.SetActive(true);
        }
        protected virtual void Close()
        {
            this.gameObject.SetActive(false);
        }
    }
}