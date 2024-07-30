using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace SlotGame.Machine.S1088
{
    public class ExtraInfo1088 : MonoBehaviour
    {
        //--------------------------------------------------------------------------------------------------
        // Extra Info
        //--------------------------------------------------------------------------------------------------

        // 0 ~ 4: 1열에 적용되는 배수
        // private readonly int EXTRA_INDEX_MUL_1 = 0;

        // 5 ~ 9: 5열에 적용되는 배수
        // private readonly int EXTRA_INDEX_MUL_5 = 5;

        // 10 ~ 34: 플립 시 릴별 릴스탑 위치
        private readonly int EXTRA_INDEX_FLIP_REEL_STOP = 10;

        // 35 ~ 59: 플립 시 스핀 결과 syms
        private readonly int EXTRA_INDEX_FLIP_RESULT_SYMS = 35;

        // 60: 플립 심볼 위치
        private readonly int EXTRA_INDEX_FLIP_SYMS_POS = 60;

        // 61: 플립 중 누적 윈금액
        private readonly int EXTRA_INDEX_FLIP_WIN_COINS = 61;

        //--------------------------------------------------------------------------------------------------

#pragma warning disable 0649
        [SerializeField] private SlotMachine1088 slotMachine;
#pragma warning restore 0649

        public bool IsFlip { get; private set; }

        public List<FlipInfo> PrevFlipInfoList { get; private set; }
        public List<FlipInfo> CurrentFlipInfoList { get; private set; }

        public long FlipTotalWinCoins { get; private set; }

        public long CurrentFlipCoins { get; private set; }

        // {reelindex - extraindex} Pair
        private readonly Dictionary<int, int> MultiPairExtraInfo = new Dictionary<int, int>()
        {
            { 0, 0 }, {5, 1}, {10, 2 }, {15, 3 }, {20, 4 },
            { 4, 5 }, {9, 6}, {14, 7 }, {19, 8 }, {24, 9 }
        };

        private readonly List<int> Reel_L = new List<int>() { 0, 5, 10, 15, 20, 25 };
        private readonly List<int> Reel_R = new List<int>() { 4, 9, 14, 19, 24, 29 };

        private readonly List<int> FlipOrder = new List<int>() { 0, 5, 10, 15, 20, 6, 11, 16, 21, 12, 17, 22, 8, 13, 18, 23, 4, 9, 14, 19, 24 };


        private SlotMachineInfo info;

        public struct FlipInfo
        {
            public string id;
            public SymbolPosition pos;
            public int mul;
            public int reelStopIndex;

            public bool EqualPos(int reelIndex, int mainSymbolIndex)
            {
                return (reelIndex == pos.reelIndex) && (mainSymbolIndex == pos.mainIndex);
            }
            public bool Equals(FlipInfo other)
            {
                return (id == other.id) && (pos.Equals(other.pos));
            }

            public override string ToString()
            {
                return string.Format("@ [{0}], pos = {1}, mul = {2}, reelStop = {3}", id, pos.ToString(), mul, reelStopIndex);
            }
        }

        private readonly int COLUMN_COUNT = 25;
        private readonly int ROW_COUNT = 1;

        public void Initialize()
        {
            if (slotMachine == null) slotMachine = this.GetComponent<SlotMachine1088>();
            if (info == null) info = slotMachine.Info;

            IsFlip = false;
            PrevFlipInfoList = new List<FlipInfo>();
            CurrentFlipInfoList = new List<FlipInfo>();
            FlipTotalWinCoins = 0;
            CurrentFlipCoins = 0;

            OnUpdateInfo();
        }

        public void OnUpdateInfo()
        {
            PrevFlipInfoList.Clear();
            PrevFlipInfoList.AddRange(CurrentFlipInfoList);

            CurrentFlipInfoList.Clear();
            IsFlip = false;

            if (info.GetExtrasCount() < EXTRA_INDEX_FLIP_SYMS_POS) return;

            Debug.Log("@----------------------------------------------------------------------------");

            if (info.IsRespinEnd == false)
            {
                FlipTotalWinCoins = info.GetExtraValue(EXTRA_INDEX_FLIP_WIN_COINS);
            }

            Debug.LogFormat("@ FlipTotalWin[{1}] : {0}", FlipTotalWinCoins, info.IsRespinEnd);

            var flipPos = (int)info.GetExtraValue(EXTRA_INDEX_FLIP_SYMS_POS);

            var flipSymbolPos = SlotMachineUtils.ConvertBinaryPosAsSymbolPos(COLUMN_COUNT, ROW_COUNT, flipPos);

            if (flipSymbolPos.Count == 0)
            {
                Debug.Log("@ Non - Flip ");
                Debug.Log("@----------------------------------------------------------------------------");

                if (PrevFlipInfoList.Count > 0)
                {
                    PrevFlipInfoList = PrevFlipInfoList.OrderBy(x => FlipOrder.IndexOf(x.pos.reelIndex)).ToList();
                }
                return;
            }

            IsFlip = true;

            for (int i = 0; i < flipSymbolPos.Count; i++)
            {
                int reelIndex = flipSymbolPos[i].reelIndex;
                int reelStopIndex = (int)info.GetExtraValue(EXTRA_INDEX_FLIP_REEL_STOP + reelIndex);
                int symbolId = (int)info.GetExtraValue(EXTRA_INDEX_FLIP_RESULT_SYMS + reelIndex);
                int mulValue = 0;

                if (MultiPairExtraInfo.ContainsKey(reelIndex))
                {
                    mulValue = (int)info.GetExtraValue(MultiPairExtraInfo[reelIndex]);
                }

                var flipInfo = new FlipInfo()
                {
                    id = symbolId.ToString(),
                    pos = flipSymbolPos[i],
                    mul = mulValue,
                    reelStopIndex = reelStopIndex
                };

                Debug.Log(flipInfo.ToString());

                CurrentFlipInfoList.Add(flipInfo);
            }
            Debug.Log("@----------------------------------------------------------------------------");

            // Set Order

            PrevFlipInfoList = PrevFlipInfoList.OrderBy(x => FlipOrder.IndexOf(x.pos.reelIndex)).ToList();
        }

        public void ResetPrevInfo()
        {
            PrevFlipInfoList.Clear();
            CurrentFlipCoins = 0;
        }

        public void SetCurrentFlipWinCoins()
        {
            CurrentFlipCoins = info.SpinResult.WinCoinsHit;
        }
        public bool IsReelLeft(int reelIndex)
        {
            return Reel_L.Contains(reelIndex);
        }

        public bool IsReelRight(int reelIndex)
        {
            return Reel_R.Contains(reelIndex);
        }

        public bool IsContainPrevFlipSyms(int reelIndex)
        {
            for (int i = 0; i < PrevFlipInfoList.Count; i++)
            {
                var info = PrevFlipInfoList[i];
                if (info.pos.reelIndex == reelIndex) return true;
            }

            return false;
        }

        public int GetMultiIndex(int reelIndex)
        {
            if (MultiPairExtraInfo.ContainsKey(reelIndex) == false) return -1;

            return MultiPairExtraInfo[reelIndex];
        }

    }
}