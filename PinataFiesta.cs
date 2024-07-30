using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine.Events;
using System;
using SlotGame.Sound;
using System.Linq;
using DG.Tweening;
using SlotGame;


namespace SlotGame.Machine.S1042
{
    public struct SymbolInfo
    {
        public string id;
        public long value;
    }

    public class LinkMerge1042 : MonoBehaviour
    {
        [Serializable] public class OnDispatchGetCoin : UnityEvent<long> { }
        public OnDispatchGetCoin onDispatchGetCoin = null;

#pragma warning disable 0649
        [SerializeField] private SlotMachine1042 slotMachine;
        [SerializeField] private LinkResultReel1042[] linkResultReels;
        [SerializeField] private Transform resultReelRoots;
        [SerializeField] private Transform resultMoveTarget;
        [SerializeField] private Transform stickyRoots;
        [SerializeField] private SymbolPool symbolPool;
        [SerializeField] private GameObject availableTitle;
        [SerializeField] private Transform availableTitleRoot;
        [SerializeField] private RectTransform[] mergeTransform;

        [SerializeField] private float idleAfterMergeDelay = 1.0f;
        [SerializeField] private float coinVisualTimeBefore;
        [SerializeField] private float coinVisualTimeAfter;
        [SerializeField] private float normalResultDelayTime = 0.5f;
        [SerializeField] private float changeTime = 1.0f;
        [SerializeField] private float resultDelayTime;
        [SerializeField] private float winCoinDelayTime;
        [SerializeField] private float jackpotDelayTime;

        [Header("WayPoint")]
        //[SerializeField] private float intervalX;
        [SerializeField] private float intervalY;
        [SerializeField] private float coinMoveDuration;
        [SerializeField] private float coinScaleValue;
        [SerializeField] private Ease coinScaleEase;
        [SerializeField] private Ease coinMoveEase;

        [Header("Super Coin")]
        [SerializeField] private Transform coinRoot;
        [SerializeField] private TextMesh coinTextPrefab;
        [SerializeField] private TextMesh coinText5xPrefab;
        [SerializeField] private float coinMoveTime;
        [SerializeField] private float coinTextPosY;


        [Header("BottomUI")]
        [SerializeField] private BottomUI1042 bottomUI;
        [SerializeField] private float mergeResultDelayTime;
        [SerializeField] private float bottomFontSize;

        [Header("Sounds")]
        [SerializeField] private SoundPlayer mergeSound1;
        [SerializeField] private SoundPlayer mergeSound2;
        [SerializeField] private SoundPlayer mergeSound3;
        [SerializeField] private SoundPlayer bigReelSpin;
        [SerializeField] private SoundPlayer bigMergeSound;

        [SerializeField] private SoundPlayer normalExplod1;
        [SerializeField] private SoundPlayer normalExplod2;
        [SerializeField] private SoundPlayer normalExplod3;

        [SerializeField] private SoundPlayer reverseMoveSound;
        [SerializeField] private SoundPlayer resultCoinSound;

        [SerializeField] private SoundPlayer showBottomUISound;
        [SerializeField] private SoundPlayer availableJackptEndSound;
        [SerializeField] private SoundPlayer pressPlaySound;

#pragma warning restore 0649

        private readonly int MAIN_SYMBOL_INDEX = 0;
        private readonly int ROW_COUNT = 5;
        private readonly int COLUMN_COUNT = 3;

        private readonly int EXTRA_INDEX_START = 2;
        private readonly int EXTRA_INDEX_MERGE_TYPE = 3;
        private readonly int LENGTH_EXTRA_NORMAL = 3;

        #region Merge Info Struct       
        public struct MergeSymbolInfo
        {
            public string name;
            public string id;
            public int colCount;
            public int rowCount;

            public int GetCount()
            {
                return colCount * rowCount;
            }

            public override string ToString()
            {
                return string.Format("name {0}, id {1}, count {2}, columnCount {3}, rowCount {4}", name, id, GetCount(), colCount, rowCount);
            }
        }

        public struct ResultMergeReelInfo
        {
            public List<int> pos;
            public MergeSymbolInfo mergeInfo;
            public int visualIndex;

            public List<SymbolInfo> symbolList;

            public void PosSort()
            {
                pos.Sort();
            }

            public override string ToString()
            {
                string posText = "[";
                for (int i = 0; i < pos.Count; i++)
                {
                    posText += pos[i] + ", ";
                }

                posText += "]";

                string text = "";
                for (int i = 0; i < symbolList.Count; i++)
                {
                    text += string.Format("({2})[{0}, {1}]/", symbolList[i].id, symbolList[i].value, i);
                }
                return string.Format("[ResultMergeReelInfo] \n mergeInfo => {0} \n SymbolList => {1} \n pos => {2} \n visualIndex => {3}",
                    mergeInfo.ToString(), text, posText, visualIndex);
            }
        }

        private struct ResultSymbolInfo
        {
            public List<int> pos;
            public SymbolInfo info;

            public void PosSort()
            {
                pos.Sort();
            }

            public override string ToString()
            {
                string posText = "[";
                for (int i = 0; i < pos.Count; i++)
                {
                    posText += pos[i] + ", ";
                }

                posText += "]";

                return string.Format("[ResultSymbolInfo] pos {0}, id {1}, value {2} ", posText, info.id, info.value);
            }
        }

        private List<ResultMergeReelInfo> resultMergeReelInfos = new List<ResultMergeReelInfo>();
        private List<ResultSymbolInfo> resultSymbolInfos = new List<ResultSymbolInfo>();

        private readonly List<MergeSymbolInfo> mergeSymbolInfos = new List<MergeSymbolInfo>()
            {
                new MergeSymbolInfo(){ name = "1x1", id = "19", colCount = 1, rowCount = 1},
                new MergeSymbolInfo(){ name = "2x1", id = "20", colCount = 2, rowCount = 1},
                new MergeSymbolInfo(){ name = "3x1", id = "21", colCount = 3, rowCount = 1},
                new MergeSymbolInfo(){ name = "2x2", id = "22", colCount = 2, rowCount = 2},
                new MergeSymbolInfo(){ name = "2x3", id = "23", colCount = 2, rowCount = 3},
                new MergeSymbolInfo(){ name = "2x4", id = "24", colCount = 2, rowCount = 4},
                new MergeSymbolInfo(){ name = "2x5", id = "25", colCount = 2, rowCount = 5},
                new MergeSymbolInfo(){ name = "3x2", id = "26", colCount = 3, rowCount = 2},
                new MergeSymbolInfo(){ name = "3x3", id = "27", colCount = 3, rowCount = 3},
                new MergeSymbolInfo(){ name = "3x4", id = "28", colCount = 3, rowCount = 4},
                new MergeSymbolInfo(){ name = "3x5", id = "29", colCount = 3, rowCount = 5}
            };
        private List<int> mergeReelIndexList = new List<int>();

        private struct ResultMergeInfo
        {
            public LinkResultReel1042 resultReel;
            public long totalWinCoin;
        }
        private List<ResultMergeInfo> resultMergeInfos = new List<ResultMergeInfo>();

        #endregion

        // 피냐타 링크 심볼 비주얼 타입
        private readonly SymbolVisualType mergeVisual = SymbolVisualType.Etc1;
        private readonly SymbolVisualType changeVisual = SymbolVisualType.Etc2;
        private readonly SymbolVisualType idleVisual = SymbolVisualType.Etc3;

        private readonly SymbolVisualType bgVisual = SymbolVisualType.Etc6;
        private readonly SymbolVisualType emptyVisual = SymbolVisualType.Etc7;
        private readonly SymbolVisualType coinVisual = SymbolVisualType.Etc8;
        private readonly SymbolVisualType coinHitVisual = SymbolVisualType.Etc9;

        private readonly string SYMBOL_ID_1X1 = "19";


        private GameObjectPool<TextMesh> coinTextOP;
        private GameObjectPool<TextMesh> coinText5xOP;
        private ReelGroup reelGroup = null;
        private long totalWinCoin = 0;
        private StringUtils.KMBOption kmbOption = new StringUtils.KMBOption();

        private void Awake()
        {
            kmbOption = StringUtils.DefaultKMBOption();
            kmbOption.minLength = 3;
            kmbOption.ignorZeroDecimalPoint = true;

            coinTextOP = new GameObjectPool<TextMesh>(coinRoot.gameObject, 5, () => { return Instantiate(coinTextPrefab); });
            coinText5xOP = new GameObjectPool<TextMesh>(coinRoot.gameObject, 5, () => { return Instantiate(coinText5xPrefab); });
        }

        public void Initialize(ReelGroup reelGroup)
        {
            this.reelGroup = reelGroup;
            totalWinCoin = 0;
            EnableMergeButton(string.Empty, Vector3.zero, null);
            mergeReelIndexList.Clear();
        }

        public List<int> GetMergeSymbolPosForSuperBonusInit(SlotMachineInfo info)
        {
            int count = info.GetExtrasCount() - EXTRA_INDEX_START;
            if (count == 0)
            {
                return null;
            }

            List<int> mergeSyms = new List<int>();

            for (int i = EXTRA_INDEX_START; i < info.GetExtrasCount(); i++)
            {
                var binaryPos = (int)info.GetExtraValue(i);

                var symsPos = SlotMachineUtils.ConvertBinaryPosAsSymbolPos(ROW_COUNT,
                                                                                  COLUMN_COUNT,
                                                                                  binaryPos);
                mergeSyms.AddRange(GetConvertPos(symsPos));
            }

            return mergeSyms;
        }

        public List<List<int>> GetMergeSymbolPos(SlotMachineInfo info)
        {
            int count = info.GetExtrasCount() - EXTRA_INDEX_START;
            if (count == 0)
            {
                return null;
            }

            // 머지 심볼이 존재한다.
            List<List<int>> mergeSyms = new List<List<int>>();

            for (int i = EXTRA_INDEX_START; i < info.GetExtrasCount(); i++)
            {
                var binaryPos = (int)info.GetExtraValue(i);

                var symsPos = SlotMachineUtils.ConvertBinaryPosAsSymbolPos(ROW_COUNT,
                                                                                  COLUMN_COUNT,
                                                                                  binaryPos);
                mergeSyms.Add(GetConvertPos(symsPos));
            }

            return mergeSyms;
        }

        private List<int> GetConvertPos(List<SymbolPosition> pos)
        {
            List<int> syms = new List<int>();
            string text = "";
            for (int i = 0; i < pos.Count; i++)
            {
                int reelIndex = pos[i].reelIndex;
                int rowIndex = Mathf.Abs(pos[i].mainIndex - 2);

                int convertReelIndex = rowIndex * ROW_COUNT + reelIndex;

                text += string.Format("reelIndex = [{0}] ", convertReelIndex);

                syms.Add(convertReelIndex);
            }

            if (string.IsNullOrEmpty(text) == false)
            {
                Debug.Log(text);
            }

            return syms;
        }

        public bool OnTransitionSymbol(List<int> symbolPos, bool init)
        {
            int mainIndex = GetMainMergeReelIndex(symbolPos);
            if (mainIndex == -1)
            {
                return false;
            }
            Debug.LogFormat("[OnTransitionSymbol] mainIndex = {0}", mainIndex);

            var currentSymbol = reelGroup.GetReel(mainIndex).GetMainSymbol(MAIN_SYMBOL_INDEX, true);
            MergeSymbolInfo info = GetMergeSymbol(symbolPos);

            int visualIndex = 0;

            // 전환되는 머지 심볼에 대한 정보가 같으면 트랜지션을 하지 않는다.
            if (info.id == currentSymbol.id)
            {
                bool mergeExeption = false;
                if (currentSymbol.id == SYMBOL_ID_1X1)
                {
                    if (currentSymbol.GetVisual().visualType != idleVisual)
                    {
                        visualIndex = currentSymbol.GetVisual().index;
                        currentSymbol.SetVisual(idleVisual, visualIndex);
                    }
                }
                // 우선순위 병합으로 인해 머지 이동이 있는 경우 체크
                else
                {
                    for (int i = 0; i < symbolPos.Count; i++)
                    {
                        int pos = symbolPos[i];

                        var reel = reelGroup.GetReel(pos);
                        var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX, true);
                        if (symbol.id != info.id)
                        {
                            mergeExeption |= true;
                        }
                    }
                }
                if (mergeExeption == false)
                {
                    return false;
                }
            }

            if (symbolPos.Count == 1)
            {
                int pos = symbolPos[0];
                var reel = reelGroup.GetReel(pos);
                var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX, true);

                if (symbol.GetVisual().visualType != emptyVisual)
                {
                    visualIndex = symbol.GetVisual().index;
                    symbol.SetVisual(changeVisual, visualIndex);
                }
            }
            else
            {
                for (int i = 0; i < symbolPos.Count; i++)
                {
                    int pos = symbolPos[i];

                    var reel = reelGroup.GetReel(pos);
                    var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX, true);

                    // 엠티 비주얼(이미 머지되어서 가려진 심볼)이 아니면 트랜지션 연출을 한다.                   
                    if (symbol.id != SYMBOL_ID_1X1)
                    {
                        if (symbol.GetVisual().visualType == emptyVisual)
                        {
                            continue;
                        }
                    }

                    if (symbol.GetVisual().visualType != emptyVisual)
                    {
                        visualIndex = symbol.GetVisual().index;
                        symbol.SetVisual(changeVisual, visualIndex);
                    }
                }
            }

            if (init == false)
            {
                OnMergeSound(info);
            }

            return true;
        }


        public bool OnMergeSymbol(List<int> symbolPos, int visualIndex)
        {
            mergeReelIndexList.Clear();

            int mainIndex = GetMainMergeReelIndex(symbolPos);
            if (mainIndex == -1)
            {
                return false;
            }

            // 현재 머지에 대한 정보
            MergeSymbolInfo info = GetMergeSymbol(symbolPos);

            // 기존 머지에 대한 정보
            var currentSymbol = reelGroup.GetReel(mainIndex).GetMainSymbol(MAIN_SYMBOL_INDEX, true);

            if (info.id == currentSymbol.id)
            {
                // 변화가 없다.
                return false;
            }
            else
            {
                // 2x2 이상의 머지 심볼에서만 빅릴이 등장
                if (int.Parse(info.id) >= 22)
                {
                    mergeReelIndexList.Add(mainIndex);
                }
            }

            for (int i = 0; i < symbolPos.Count; i++)
            {
                int pos = symbolPos[i];
                var reel = reelGroup.GetReel(pos);

                Symbol symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX, true);

                reel.RemoveStickyMainSymbol(MAIN_SYMBOL_INDEX);
                symbol = reel.ChangeMainSymbol(MAIN_SYMBOL_INDEX, info.id);
                symbol.Value = string.Empty;

                Vector3 targetPos = new Vector3(symbol.transform.position.x, symbol.transform.position.y, -1.0f);

                var stickySymbol = reel.AddStickyMainSymbol(MAIN_SYMBOL_INDEX, info.id, targetPos);
                stickySymbol.Value = string.Empty;
                stickySymbol.transform.SetParent(stickyRoots, true);

                // 실제 머지 연출이 들어가는 메인 심볼
                if (pos == mainIndex)
                {
                    stickySymbol.SetVisual(mergeVisual, visualIndex);
                    StartCoroutine(SetIdleForMergeSymbol(stickySymbol, visualIndex));
                }
                else
                {
                    stickySymbol.SetVisual(emptyVisual, 0);
                }

                symbol.SetVisual(emptyVisual, 0);
                symbol.AddSubstituteVisual(SymbolVisualType.Idle, 0, emptyVisual, 0);
            }

            return true;
        }

        public void SetMergeForSuperBonusInit(List<int> symbolPos)
        {
            int mainIndex = GetMainMergeReelIndex(symbolPos);
            if (mainIndex == -1)
            {
                return;
            }

            MergeSymbolInfo info = GetMergeSymbol(symbolPos);

            for (int i = 0; i < symbolPos.Count; i++)
            {
                int pos = symbolPos[i];
                var reel = reelGroup.GetReel(pos);

                Symbol symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX, true);

                reel.RemoveStickyMainSymbol(MAIN_SYMBOL_INDEX);
                symbol = reel.ChangeMainSymbol(MAIN_SYMBOL_INDEX, info.id);
                symbol.Value = string.Empty;

                Vector3 targetPos = new Vector3(symbol.transform.position.x, symbol.transform.position.y, -1.0f);

                var stickySymbol = reel.AddStickyMainSymbol(MAIN_SYMBOL_INDEX, info.id, targetPos);
                stickySymbol.Value = string.Empty;
                stickySymbol.transform.SetParent(stickyRoots, true);

                // 실제 머지 연출이 들어가는 메인 심볼
                if (pos == mainIndex)
                {
                    stickySymbol.SetVisual(idleVisual, 1);
                }
                else
                {
                    stickySymbol.SetVisual(emptyVisual, 0);
                }

                symbol.SetVisual(emptyVisual, 0);
                symbol.AddSubstituteVisual(SymbolVisualType.Idle, 0, emptyVisual, 0);
            }
        }

        private void OnMergeSound(MergeSymbolInfo info)
        {
            if (info.GetCount() < 5)
            {
                mergeSound1.Play();
            }
            else if (info.GetCount() < 13)
            {
                mergeSound2.Play();
            }
            else
            {
                mergeSound3.Play();
            }
        }

        private IEnumerator SetIdleForMergeSymbol(Symbol symbol, int visualIndex)
        {
            yield return new WaitForSeconds(idleAfterMergeDelay);
            symbol.SetVisual(idleVisual, visualIndex);
        }

        public List<int> GetBigReelIndex()
        {
            return mergeReelIndexList;
        }


        // 합성되는 메인 심볼을 얻는다. 좌상단 심볼.
        public int GetMainMergeReelIndex(List<int> symbolPos)
        {
            int reelIndex = -1;

            for (int i = 0; i < symbolPos.Count; i++)
            {
                int index = symbolPos[i];

                if (reelIndex == -1)
                {
                    reelIndex = index;
                }
                else
                {
                    if (index < reelIndex)
                    {
                        reelIndex = index;
                    }
                }
            }

            return reelIndex;
        }

        private MergeSymbolInfo GetMergeSymbol(List<int> symbolPos)
        {
            int count = symbolPos.Count;

            var infos = mergeSymbolInfos.FindAll(x => x.GetCount() == count);

            symbolPos.Sort();

            if (infos.Count > 1)
            {
                List<int> rowList = new List<int>();
                List<int> colList = new List<int>();

                for (int i = 0; i < symbolPos.Count; i++)
                {
                    int pos = symbolPos[i];
                    int reelIndex = pos % ROW_COUNT;
                    int mainSymbolIndex = pos / ROW_COUNT;

                    if (rowList.Contains(reelIndex) == false)
                    {
                        rowList.Add(reelIndex);
                    }

                    if (colList.Contains(mainSymbolIndex) == false)
                    {
                        colList.Add(mainSymbolIndex);
                    }
                }

                var info = mergeSymbolInfos.Find(x => (x.rowCount == rowList.Count) && (x.colCount == colList.Count));
                return info;
            }
            else
            {
                return infos[0];
            }
        }

        public void SetResultMerge(SlotMachineInfo info)
        {
            resultMergeReelInfos.Clear();
            resultSymbolInfos.Clear();

            int startIndex = EXTRA_INDEX_START;

            if (info.GetExtrasCount() <= startIndex)
            {
                Debug.Log("Not have merge");
                return;
            }

            SetResultReelInfo(info);
        }

        public bool OnResultTransition()
        {
            Debug.Log("OnResultTransition");
            bool isTransition = false;

            for (int i = 0; i < resultMergeReelInfos.Count; i++)
            {
                isTransition |= OnTransitionSymbol(resultMergeReelInfos[i].pos, false);
            }

            for (int i = 0; i < resultSymbolInfos.Count; i++)
            {
                isTransition |= OnTransitionSymbol(resultSymbolInfos[i].pos, false);
            }
            return isTransition;
        }

        public bool OnResultMerge()
        {
            Debug.Log("OnResultMerge");
            bool isMerge = false;
            for (int i = 0; i < resultMergeReelInfos.Count; i++)
            {
                isMerge |= OnMergeSymbol(resultMergeReelInfos[i].pos, resultMergeReelInfos[i].visualIndex);
            }

            for (int i = 0; i < resultSymbolInfos.Count; i++)
            {
                isMerge |= OnMergeSymbol(resultSymbolInfos[i].pos, 0);
            }

            return isMerge;
        }

        private void SetResultReelInfo(SlotMachineInfo info)
        {
            int startIndex = EXTRA_INDEX_START;
            var mergeFlag = info.GetExtraValue(EXTRA_INDEX_MERGE_TYPE);

            if (mergeFlag == 0 || mergeFlag == 1)
            {
                // 머지 릴 정산 결과
                while ((startIndex + 1) < info.GetExtrasCount())
                {
                    if (info.GetExtraValue(startIndex + 1) == 0 || info.GetExtraValue(startIndex + 1) == 1)
                    {
                        SetResultMergeReelInfo(ref startIndex, info, (int)mergeFlag);
                        startIndex += 1;
                    }
                    else
                    {
                        break;
                    }
                }
                // 일반 정산 결과
                SetResultNormalReelInfo(ref startIndex, info);
            }
            else
            {
                // 일반 정산 결과
                SetResultNormalReelInfo(ref startIndex, info);
                // 머지 릴 정산 결과
                while ((startIndex + 1) < info.GetExtrasCount())
                {
                    int mergeVisualIndex = (int)info.GetExtraValue(startIndex + 1);
                    if (mergeVisualIndex == 0 || mergeVisualIndex == 1)
                    {
                        SetResultMergeReelInfo(ref startIndex, info, mergeVisualIndex);
                        startIndex += 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void SetResultMergeReelInfo(ref int startIndex, SlotMachineInfo info, int visualIndex)
        {
            ResultMergeReelInfo mergeReelInfo = new ResultMergeReelInfo();

            // [0] , [1] , [2] posINdex, [3] 0, [4] length
            int posIndex = startIndex;
            int lengthIndex = posIndex + 2;

            // 머지 심볼 위치 
            int binaryPos = (int)info.GetExtraValue(posIndex);
            var convertPos = GetConvertPos(binaryPos);

            mergeReelInfo.pos = new List<int>();
            mergeReelInfo.pos = convertPos;
            mergeReelInfo.PosSort();
            // 머지 심볼 정보
            var mergeInfo = GetMergeSymbol(convertPos);

            mergeReelInfo.mergeInfo = mergeInfo;
            mergeReelInfo.symbolList = new List<SymbolInfo>();
            mergeReelInfo.visualIndex = visualIndex;

            int length = (int)info.GetExtraValue(lengthIndex);
            int bindLength = 2;

            for (int i = 0; i < length; i++)
            {
                var symbolInfo = new SymbolInfo();

                int symbolIdIndex = startIndex + 3 + bindLength * i;
                int payIndex = symbolIdIndex + 1;

                symbolInfo.id = info.GetExtraValue(symbolIdIndex).ToString();
                symbolInfo.value = info.GetExtraValue(payIndex);

                mergeReelInfo.symbolList.Add(symbolInfo);
            }
            startIndex = lengthIndex + length * bindLength;

            Debug.Log(mergeReelInfo.ToString());

            resultMergeReelInfos.Add(mergeReelInfo);
        }

        private void SetResultNormalReelInfo(ref int startIndex, SlotMachineInfo info)
        {
            bool loop = true;
            int count = 0;

            int coinPosIndex = startIndex;
            int coinIDIndex = startIndex + 1;
            int coinValueIndex = startIndex + 2;

            if (info.GetExtrasCount() < (coinValueIndex + 1))
            {
                Debug.Log("not have normal result");
                return;
            }

            while (loop)
            {
                ResultSymbolInfo symbolInfo = new ResultSymbolInfo();

                int binaryPos = (int)info.GetExtraValue(coinPosIndex + LENGTH_EXTRA_NORMAL * count);
                var convertPos = GetConvertPos(binaryPos);

                symbolInfo.pos = new List<int>();
                symbolInfo.pos.AddRange(convertPos);
                symbolInfo.PosSort();
                string extraId = info.GetExtraValue(coinIDIndex + LENGTH_EXTRA_NORMAL * count).ToString();
                if (extraId == "20")
                {
                    symbolInfo.info.id = GetResultSymbolID(symbolInfo);
                }
                else
                {
                    symbolInfo.info.id = extraId;
                }
                symbolInfo.info.value = info.GetExtraValue(coinValueIndex + LENGTH_EXTRA_NORMAL * count);

                Debug.Log(symbolInfo.ToString());

                resultSymbolInfos.Add(symbolInfo);

                count++;

                int endCheckIndex = LENGTH_EXTRA_NORMAL * count + startIndex + 1;
                if (info.GetExtrasCount() >= endCheckIndex)
                {
                    // 빅릴이 뒤에 나온 경우
                    if (info.GetExtraValue(endCheckIndex) == 0 || info.GetExtraValue(endCheckIndex) == 1)
                    {
                        // 빅릴 인덱스 계산 필요
                        loop = false;
                    }
                }
                else
                {
                    loop = false;
                }
            }

            startIndex += LENGTH_EXTRA_NORMAL * count;
        }

        private string GetResultSymbolID(ResultSymbolInfo info)
        {
            var mergeSymbolInfo = mergeSymbolInfos.Find(x => x.GetCount() == info.pos.Count);
            return mergeSymbolInfo.id;
        }

        private List<int> GetConvertPos(int binaryPos)
        {
            // 머지 심볼 위치 
            var symsPos = SlotMachineUtils.ConvertBinaryPosAsSymbolPos(ROW_COUNT,
                                                                      COLUMN_COUNT,
                                                                      binaryPos);

            var convertPos = GetConvertPos(symsPos);

            return convertPos;
        }

        public IEnumerator OnResultNormal()
        {
            // bottom ui on
            if (resultSymbolInfos.Count == 0 && resultMergeReelInfos.Count == 0)
            {
                yield break;
            }

            resultSymbolInfos = resultSymbolInfos.OrderBy(info => info.pos.Count).ThenBy(info => info.pos[0]).ToList();

            for (int i = 0; i < resultSymbolInfos.Count; i++)
            {
                ResultSymbolInfo symbolInfos = resultSymbolInfos[i];

                List<int> symbolPos = symbolInfos.pos;
                int mainPos = GetMainMergeReelIndex(symbolPos);
                var reel = reelGroup.GetReel(mainPos);
                var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX, true);

                yield return OnResultGetCoin(symbol, 0, symbolInfos.info, true);
                // 코인 정산 연출
                yield return new WaitForSeconds(normalResultDelayTime);
            }
        }

        private void PlayNormalCoinResult(string id)
        {
            switch (id)
            {
                case "19":
                    normalExplod1.Play();
                    break;
                case "20":
                    normalExplod2.Play();
                    break;
                case "21":
                    normalExplod3.Play();
                    break;
            }
        }

        private IEnumerator SetCoinVisual(Symbol symbol, SymbolInfo info, int visualIndex, bool isStayCoinVisual)
        {
            //change
            if (isStayCoinVisual)
            {
                PlayNormalCoinResult(info.id);
                symbol.SetVisual(changeVisual, visualIndex);

                yield return new WaitForSeconds(changeTime);

                // 코인값 세팅
                symbol.Value = StringUtils.ToKMB(SlotMachineUtils.CurrencyExchange(info.value), kmbOption);
                symbol.SetVisual(coinVisual, visualIndex);

                // 정산
                onDispatchGetCoin.Invoke(info.value);

                yield return new WaitForSeconds(coinVisualTimeBefore);
            }
            else
            {
                bigMergeSound.Play();
                symbol.SetVisual(SymbolVisualType.Hit, visualIndex);
            }
        }

        private void OnResultGetCoinCallback(Symbol symbol, int visualIndex, SymbolInfo info, bool isStayCoinVisual)
        {
            StartCoroutine(OnResultGetCoin(symbol, visualIndex, info, isStayCoinVisual));
        }

        private IEnumerator OnResultGetCoin(Symbol symbol, int visualIndex, SymbolInfo info, bool isStayCoinVisual)
        {
            if (isStayCoinVisual)
            {
                yield return SetCoinVisual(symbol, info, visualIndex, isStayCoinVisual);
            }
            else
            {
                StartCoroutine(SetCoinVisual(symbol, info, visualIndex, isStayCoinVisual));

                if (string.IsNullOrEmpty(symbol.Value) == false)
                {
                    // 코인 위치
                    var coinvisual = symbol.GetComponent<CoinVisual>();
                    if (coinvisual == null)
                    {
                        yield break;
                    }

                    float charSize = coinvisual.GetCoinText().characterSize;
                    var coinTransform = coinvisual.SortingGroup.transform;
                    symbol.Value = string.Empty;

                    yield return OnMoveCoinText(info.value, coinTransform.position, resultMoveTarget.position, charSize, visualIndex);

                    // 하단 정산
                    OnResultBottomUI(info.value);
                    symbol.Value = string.Empty;
                }
            }
        }

        private void DoText(long curr, TextMesh textMesh)
        {
            long currValue = curr / 5;
            DOTween.To(getter: () => currValue,
                  setter: value => currValue = value,
                  endValue: curr,
                  duration: 0.5f)
              .OnUpdate(() =>
              {
                  textMesh.text = StringUtils.ToKMB(SlotMachineUtils.CurrencyExchange(currValue), kmbOption);
              });
        }

        public void OnResultBottomUI(long coinValue)
        {
            slotMachine.OnDispatchPlayWinSound(coinValue);

            totalWinCoin += coinValue;
            bottomUI.OnHit(totalWinCoin, slotMachine.WinSoundInfo.duration);
        }

        public IEnumerator OnResultMergePress()
        {
            if (resultMergeReelInfos.Count == 0)
            {
                yield break;
            }

            // 머지 프레스 등장 순서
            // 일반 머지부터 -> 슈퍼보너스는 나중에
            // 머지 크기 작은 순서부터
            // 크기가 같으면 정렬 순서대로
            resultMergeReelInfos = resultMergeReelInfos.OrderBy(info => info.visualIndex)
                                                       .ThenBy(info => info.pos.Count)
                                                       .ThenBy(info => info.pos[0]).ToList();

            resultMergeInfos.Clear();

            for (int i = 0; i < resultMergeReelInfos.Count; i++)
            {
                var mergeReelInfo = resultMergeReelInfos[i];
                var mergeInfo = mergeReelInfo.mergeInfo;
                string mergeReelName = mergeInfo.name;
                int mainReelIndex = GetMainMergeReelIndex(mergeReelInfo.pos);
                LinkResultReel1042 resultReel = GetLinkResultReel(mergeReelName);

                var reel = reelGroup.GetReel(mainReelIndex);
                var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX, true);

                pressPlaySound.Play();
                symbol.SetVisual(changeVisual, mergeReelInfo.visualIndex);

                yield return new WaitForSeconds(changeTime);

                symbol.SetVisual(bgVisual, 0);

                var currentReel = Instantiate(resultReel);
                currentReel.transform.SetParent(resultReelRoots, true);

                Vector3 targetPos = new Vector3(symbol.transform.position.x, symbol.transform.position.y, -1.0f);
                currentReel.transform.position = targetPos;

                int availableJackpotCount = GetAvailableJackpot(mergeReelInfo.pos.Count);

                EnableMergeButton(mergeReelName, currentReel.SymbolPos, currentReel);
                yield return currentReel.OnStart(mergeReelInfo.symbolList, targetPos, mergeReelInfo.visualIndex, slotMachine.holdType != SlotMachine.HoldType.On);
                EnableMergeButton(string.Empty, Vector3.zero, null);
                // Press Play 클릭 이후
                if (bottomUI.gameObject.activeSelf == false)
                {
                    bottomUI.gameObject.SetActive(true);
                    showBottomUISound.Play();
                }
                availableJackptEndSound.Play();
                bottomUI.SetNum(availableJackpotCount);

                totalWinCoin = 0;

                yield return OnResultMergeReelSpin(currentReel, mergeReelInfo.symbolList);

                ResultMergeInfo resultMergeInfo = new ResultMergeInfo()
                {
                    resultReel = currentReel,
                    totalWinCoin = totalWinCoin
                };

                resultMergeInfos.Add(resultMergeInfo);
            }
        }

        private IEnumerator OnResultMergeReelSpin(LinkResultReel1042 resultReel, List<SymbolInfo> Infos)
        {
            bigReelSpin.Play();

            yield return resultReel.MoveSubtitle();

            for (int i = 0; i < Infos.Count; i++)
            {
                var info = Infos[i];
                yield return resultReel.MoveNext(info, OnResultGetCoinCallback);

                Debug.LogFormat("OnResultMergereelSpin = id {0}, value {1}", info.id, info.value);

                if (resultReel.IsJackpot(info.id))
                {
                    bigReelSpin.Pause();

                    yield return resultReel.OnJackpotHit();

                    int jackpotGrade = resultReel.GetJackpotGrade(info.id);

                    yield return slotMachine.OnJackpotPopup(jackpotGrade, info.value);
                    OnResultBottomUI(info.value);

                    yield return new WaitForSeconds(jackpotDelayTime);
                    bigReelSpin.Resume();
                    resultReel.EndJackpot();
                }
            }

            Symbol lastSymbol = null;
            int visualIndex = 0;

            // 최종 정산 플로우 추가 ( 마지막 스핀)
            yield return resultReel.MoveLast((symbol, index, value, stay) =>
            {
                lastSymbol = symbol;
                visualIndex = index;
            });

            bigReelSpin.Stop();

            yield return new WaitForSeconds(resultDelayTime);

            // 최종 코인 심볼 전환
            slotMachine.OnDispatchPlayWinSound(totalWinCoin);
            bottomUI.OnHit(0, slotMachine.WinSoundInfo.duration);

            var coinvisual = lastSymbol.GetComponent<CoinVisual>();
            float charSize = coinvisual.GetCoinText().characterSize;

            yield return OnMoveCoinText(totalWinCoin, resultMoveTarget.position, coinvisual.SortingGroup.transform.position, charSize, visualIndex, true);

            resultReel.SetCoinValue(totalWinCoin);
            lastSymbol.SetVisual(coinHitVisual, 0);
            resultCoinSound.Play();

            bigMergeSound.Play();

            StartCoroutine(OutBottomUI());

            yield return new WaitForSeconds(mergeResultDelayTime);

            onDispatchGetCoin.Invoke(totalWinCoin);
        }

        private IEnumerator OutBottomUI()
        {
            yield return bottomUI.Out();
            bottomUI.gameObject.SetActive(false);
        }

        private IEnumerator OnMoveCoinText(long coinValue, Vector3 startPos, Vector3 endPos, float fontSize, int visualIndex, bool reverse = false)
        {
            if (reverse == false)
            {
                startPos.y += coinTextPosY;
            }


            // 프리팹 생성
            TextMesh coinText = (visualIndex == 0 || reverse == true) ? coinTextOP.Get() : coinText5xOP.Get();

            coinText.characterSize = fontSize;
            coinText.transform.SetParent(coinRoot, true);
            coinText.transform.position = startPos;

            coinText.text = StringUtils.ToKMB(SlotMachineUtils.CurrencyExchange(coinValue), kmbOption);

            coinText.transform.localScale = Vector3.one;


            if (reverse == false)
            {
                var wayPoint = GetCoinMoveWayPoints(startPos, endPos);

                coinText.transform.DOPath(wayPoint, coinMoveDuration, PathType.CatmullRom)
                                    .SetEase(coinMoveEase);

                coinText.transform.DOScale(coinScaleValue, coinMoveDuration).SetEase(coinScaleEase);
            }
            else
            {
                reverseMoveSound.Play();
                coinText.transform.DOMove(endPos, coinMoveDuration);
            }
            yield return new WaitForSeconds(coinMoveDuration);

            if (visualIndex == 0)
            {
                coinTextOP.Return(coinText);
            }
            else
            {
                coinText5xOP.Return(coinText);
            }
        }

        private Vector3[] GetCoinMoveWayPoints(Vector3 symbolPos, Vector3 targetPos)
        {
            Vector3[] wayPoints = new Vector3[3];

            Vector3 newPoint = symbolPos;

            newPoint.x = symbolPos.x / 2;
            newPoint.y += intervalY;

            if (symbolPos.x < 0.0f)
            {
                newPoint.x = Mathf.Clamp(newPoint.x, symbolPos.x, 0.0f);
            }
            else if (symbolPos.x > 0.0f)
            {
                newPoint.x = Mathf.Clamp(newPoint.x, 0.0f, newPoint.x);
            }

            wayPoints[0] = symbolPos;
            wayPoints[1] = newPoint;
            wayPoints[2] = targetPos;

            return wayPoints;
        }

        public void OnEnd()
        {
            bottomUI.gameObject.SetActive(false);

            foreach (LinkResultReel1042 resultReel in resultReelRoots.GetComponentsInChildren<LinkResultReel1042>())
            {
                Destroy(resultReel.gameObject);
            }

            foreach (Transform tr in coinRoot.GetComponentsInChildren<Transform>())
            {
                if (tr != coinRoot)
                {
                    Destroy(tr.gameObject);
                }
            }

            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                var reel = reelGroup.GetReel(reelIndex);

                for (int symbolIndex = 0; symbolIndex < reel.AllSymbolsLength; symbolIndex++)
                {
                    var symbol = reel.GetSymbol(symbolIndex, true);
                    symbol.Value = string.Empty;
                }
            }
        }

        private LinkResultReel1042 GetLinkResultReel(string name)
        {
            foreach (LinkResultReel1042 reel in linkResultReels)
            {
                if (reel.reelName == name)
                {
                    return reel;
                }
            }
            return null;
        }

        private void EnableMergeButton(string name, Vector3 pos, LinkResultReel1042 resultReel)
        {
            foreach (var btn in mergeTransform)
            {
                if (btn.name == name)
                {
                    btn.position = pos;
                    btn.gameObject.SetActive(true);
                    if (resultReel != null)
                    {
                        btn.GetComponent<Button>().onClick.AddListener(resultReel.OnTouched);
                    }
                }
                else
                {
                    btn.gameObject.SetActive(false);
                    btn.GetComponent<Button>().onClick.RemoveAllListeners();
                }
            }
        }

        public int GetAvailableJackpot(int count)
        {
            int availableJackPot = 0;
            if (count == 4)
            {
                availableJackPot = 5;
            }
            else if (count == 6)
            {
                availableJackPot = 6;
            }
            else if (count == 8)
            {
                availableJackPot = 7;
            }
            else if (count == 9)
            {
                availableJackPot = 7;
            }
            else if (count == 10)
            {
                availableJackPot = 8;
            }
            else if (count == 12)
            {
                availableJackPot = 8;
            }
            else if (count == 15)
            {
                availableJackPot = 9;
            }

            return availableJackPot;
        }
    }
}