using UnityEngine;
using System.Collections;
using UnityEngine.Events;
using SlotGame.Sound;

namespace SlotGame.Machine.S1061
{
    public class LockRowManager1061 : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField] private LockRow1061[] lockRows;
        [SerializeField] private float closeIntervalTime;
        [SerializeField] private float blueDisableDelayTime;
        [Header("Sounds")]
        [SerializeField] private SoundPlayer unlockSound;
        [SerializeField] private SoundPlayer[] unlockSoundRand;
#pragma warning restore 0649

        private ExtraInfo1061 extraInfo = null;
        private LinkFeature1061 linkFeature = null;

        private readonly string SYMBOL_ID_UNLOCK = "35";
        private bool UnlockWaiting { get; set; }

        public void Initialize(ExtraInfo1061 extraInfo, LinkFeature1061 linkFeature, bool init)
        {
            this.extraInfo = extraInfo;
            this.linkFeature = linkFeature;

            UnlockWaiting = false;

            Debug.LogFormat("LockRowManager.Initialze() => init {0}, IncludeBluePot {1}", init, extraInfo.IncludeBluePot);

            if (extraInfo.IncludeBluePot)
            {
                if (init)
                {
                    SetEnableAll();
                }
                else
                {
                    foreach (var row in lockRows)
                    {
                        row.CurrentState = LockRow1061.State.Disable;
                    }
                }
            }
            else
            {
                // unlockCount 초기값 세팅
                // order => 상단부터 0 ~ 4
                var lockRowInfo = extraInfo.CurrentUnlockRowCount;

                for (int i = 0; i < lockRowInfo.Count; i++)
                {
                    int symbolCount = lockRowInfo[i];
                    lockRows[i].Initialize(symbolCount, i);
                    lockRows[i].OnStateUnlockEnd.AddListener(OnCountDownUnlockRow);
                    lockRows[i].OnStateUnlockTrigger.AddListener(OnUnlockTrigger);
                }
            }
        }

        public void OnAppearSymbol(int count = 1)
        {
            bool isUnlock = false;

            for (int i = 0; i < lockRows.Length; i++)
            {
                int serverCount = extraInfo.GetCurrentUnlockRowCount(i);
                isUnlock |= lockRows[i].SetAppearSymbolCountDown(count, serverCount);
            }

            UnlockWaiting = isUnlock;
        }

        public void OnAppearUnlock()
        {
            bool isUnlock = false;

            for (int i = 0; i < lockRows.Length; i++)
            {
                int serverCount = extraInfo.GetCurrentUnlockRowCount(i);
                isUnlock |= lockRows[i].SetAppearSymbolCountDown(extraInfo.UnlockMaxCount, serverCount);
            }

            UnlockWaiting = isUnlock;
        }

        public IEnumerator OnUnlockWait()
        {
            yield return new WaitUntil(() => UnlockWaiting == false);

            if (extraInfo.IncludeBluePot == false)
            {
                // 언락 작업이 끝났으면 서버와 싱크를 맞춰준다.
                for (int i = 0; i < lockRows.Length; i++)
                {
                    int serverCount = extraInfo.GetCurrentUnlockRowCount(i);
                    lockRows[i].SyncServerCount(serverCount);
                }
            }
        }

        private void OnCountDownUnlockRow(int index)
        {
            var rowList = linkFeature.GetLinkSymbolToRow(index);

            Debug.LogFormat("OnCountDownUnlockRow::Find Index : {0}, count {1}", index, rowList.Count);

            if (rowList.Count == 0)
            {
                UnlockWaiting = false;
                Debug.Log("** UnlockWaiting -- false");
                return;
            }

            var unlockList = rowList.FindAll(x => x.id == SYMBOL_ID_UNLOCK);
            var linkList = rowList.FindAll(x => x.id != SYMBOL_ID_UNLOCK);
            int resultCount = linkList.Count + (unlockList.Count * extraInfo.UnlockMaxCount);

            Debug.Log("OnCountDownUnlockRow::UnlockSymbols Count : " + resultCount);

            OnAppearSymbol(resultCount);
        }

        private void OnUnlockTrigger(int Index)
        {
            if (extraInfo.IncludeBluePot)
            {
                return;
            }


            unlockSound.Play();

            bool randomSoundIsPlaying = false;
            foreach (var sound in unlockSoundRand)
            {
                randomSoundIsPlaying |= sound.IsInPlay();
            }

            if (randomSoundIsPlaying == false)
            {
                int randIndex = Random.Range(0, unlockSoundRand.Length);
                unlockSoundRand[randIndex].Play();
            }
        }

        public bool IsValidLockRow(int rowIndex)
        {
            if (rowIndex < lockRows.Length)
            {
                return (lockRows[rowIndex].CurrentState == LockRow1061.State.Disable);
            }
            return true;
        }

        private void SetEnableAll()
        {
            foreach (var row in lockRows)
            {
                row.CurrentState = LockRow1061.State.Locked;
                row.SetDefaultCount();
            }
        }

        public IEnumerator SetIncludeBluePotDisable()
        {
            yield return new WaitForSeconds(blueDisableDelayTime);

            int randIndex = Random.Range(0, unlockSoundRand.Length);
            unlockSoundRand[randIndex].Play();

            for (int i = lockRows.Length - 1; i >= 0; i--)
            {
                unlockSound.Play();

                var row = lockRows[i];
                row.OnTriggerUnlock();

                yield return new WaitForSeconds(closeIntervalTime);

                row.CurrentState = LockRow1061.State.Disable;
            }
        }
    }
}