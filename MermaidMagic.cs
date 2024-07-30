using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

using DG.Tweening;


using System.Linq;
using SlotGame.Sound;

namespace SlotGame.Machine.S1048
{
    public class LinkFeature1048 : MonoBehaviour
    {
#pragma warning disable 0649
        [Header("Component")]
        [SerializeField] private SlotMachine1048 slotMachine;
        [SerializeField] private ReelGroup reelGroup;
        [SerializeField] private ReelStripFeed feed;
        [SerializeField] private Transform[] stickyRoots;
        [SerializeField] private SymbolPool symbolPool;
        [SerializeField] private LinkHUD1048 linkHUD;
        [SerializeField] private LinkMultiplierPopup1048 multiplierPopup;
        [SerializeField] private LinkRandomReel1048 linkRandomReel;
        [SerializeField] private Transform linkRandomReelRoot;

        //[Header("SymbolID")]
        //[SerializeField] private string defaultStripChunk;

        [Header("Time")]
        [SerializeField, Tooltip("링크로 릴 변경 전 딜레이")] private float initDelayTime;
        [SerializeField, Tooltip("스핀 시작 후 딜레이( 이후 UpdateRespin )")] private float spinStartDelayTime;
        [SerializeField, Tooltip("심볼 이동 후 당첨 내역 없을 때 딜레이")] private float spinMoveEndDelayTime;
        [SerializeField, Tooltip("스핀 종료, 드랍히트 이후 딜레이 ( 이후 ResetRespin _")] private float spinEndDelayTime;
        [SerializeField, Tooltip("링크 스핀 시작전 딜레이")] private float linkStartDelayTime;
        [SerializeField, Tooltip("링크 종료, 결과 시작 전 딜레이")] private float resultStartDelayTime;
        [SerializeField, Tooltip("Coin 정산 Hit 연출 타임")] private float coinHitTime;
        [SerializeField, Tooltip("빈 심볼 삭제 연출 시간")] private float blankHitTime;
        [SerializeField, Tooltip("Coin 2배 적용 연출 타임(개별)")] private float coinMultiplyTime;
        [SerializeField, Tooltip("랜덤 릴 시작 전 딜레이")] private float randomReelStartDelayTime;
        [SerializeField, Tooltip("랜덤 릴 종료 후 딜레이 ( 이후 Destroy )")] private float randomReelEndDelayTime;
        [SerializeField, Tooltip("Appear 연출 시간 ( 이후 Sticky )")] private float appearTime = 0.667f;
        [SerializeField, Tooltip("멀티플라이 팝업 뜨기전 딜레이")] private float multiplyBeforeDelayTime;
        [SerializeField, Tooltip("멀티플라이 팝업 뜬 후딜레이")] private float multiplyAfterDelayTime;

        [Header("Symbol Move Drop")]
        [SerializeField, Tooltip("Drop Hit 시작전 딜레이")] private float dropStartDelayTime;
        [SerializeField, Tooltip("Drop Hit 이동되는 시간")] private float dropTime;
        [SerializeField, Tooltip("Drop Hit Move Ease")] private Ease dropEase;


        [Header("Drop Hit Indicator")]
        [SerializeField, Tooltip("Drop Hit Indicator")] private Animator[] dropHitAnims;
        [SerializeField, Tooltip("Drop Hit 라인 연출 등장 시간(On)")] private float dropHitOnTime;
        [SerializeField, Tooltip("Drop Hit 이후 딜레이 타임")] private float dropHitAfterDelayTime;
        [SerializeField, Tooltip("Drop Hit 연출 시간")] private float dropHitTime;
        [SerializeField, Tooltip("Drop Hit Idle Loop DelayTime")] private float dropHitIdleLoopDelayTime;

        [Header("MoveEffect")]
        [SerializeField, Tooltip("정산 연출 Path")] private Transform moveEffectRoot;
        [SerializeField, Tooltip("정산 연출 프리팹")] private Animator moveEffectReference;
        [SerializeField, Tooltip("정산 연출 Instance Root")] private RectTransform moveTarget;
        [SerializeField, Tooltip("정산 연출 이동 시간")] private float moveTime;
        [SerializeField, Tooltip("정산 연출 하단 UI Hit 딜레이")] private float moveHitDelayTime;
        [SerializeField, Tooltip("정산 연출 이동 Ease")] private Ease moveEffectEase;

        [Header("Sounds")]
        [SerializeField] private SoundPlayer dropSound;
        [SerializeField] private SoundPlayer dropEndSound;
        [SerializeField] private SoundPlayer afterDropSound;

        [SerializeField] private SoundPlayer frameOpenSound;
        [SerializeField] private SoundPlayer frameHitSound;

        [SerializeField] private SoundPlayer multiPopupSound;
        [SerializeField] private SoundPlayer multiApplySound;

        [SerializeField] private SoundPlayer winCoinTrailSound;

        [SerializeField] private SoundPlayer respinResetSound;
        [SerializeField] private SoundPlayer spinStartSound;
        [SerializeField] private SoundPlayer reelStopSound;
#pragma warning restore 0649

        private readonly int[] SPIN_ORDERS = new int[] { 0, 5, 10, 1, 6, 11, 2, 7, 12, 3, 8, 13, 4, 9, 14 };
        private readonly int MAIN_SYMBOL_INDEX = 0;
        private readonly string EMPTY_SYMBOL_ID = "0";
        private readonly int[] linkSymsMinMax = new int[] { 17, 41 };
        private readonly string BLANK_SYMBOL_ID = "23";
        private readonly int ROW_COUNT = 5;
        private readonly int COLUMN_COUNT = 3;
        private readonly int LAST_COLUMN_INDEX = 2;
        private readonly int MAX_RESPIN_COUNT = 3;
        private readonly string[] jackpotSymbolIDs = new string[] { "18", "19", "20", "21", "22" };
        private readonly string randomJackpotSymbolID = "17";
        private readonly int jackpotGradeParse = 18;
        private readonly List<string> onlyNormalSyms = new List<string> { "12", "13", "14" };
        private readonly int[] linkDimdSyms = new int[] { 1, 11 };

        private readonly string DROPHIT_TRIGGER_ONOFF = "onoff";
        private readonly string TRIGGER_HIT = "hit";

        private List<ReelStopInfo> reelStopInfoList = new List<ReelStopInfo>();
        private List<int> spinOrder = new List<int>();
        private List<string> linkSyms = new List<string>();
        private ReelStripHelper reelStripHelper = new ReelStripHelper();
        private CoinManager1048 coinManager = null;

        private bool isInitialize = false;
        public bool IsInitialize { get { return isInitialize; } }

        private class LinkJackpotInfo
        {
            public int jp = 0;
            public long coins = 0;
            public bool fix = false;
            public bool used = false;

            public override string ToString()
            {
                return string.Format("jp {0}, coins {1} , fix {2}", jp, coins, fix);
            }
        }
        private List<LinkJackpotInfo> jackpotInfos = new List<LinkJackpotInfo>();
        private Dictionary<int, Queue<long>> coinQueue = new Dictionary<int, Queue<long>>();

        private GameObjectPool<Animator> moveEffectOP;

        private readonly SymbolVisualType emptyVisual = SymbolVisualType.Etc9;
        private readonly SymbolVisualType hitVisual = SymbolVisualType.Hit;
        private readonly SymbolVisualType appearVisual = SymbolVisualType.Etc1;
        private readonly SymbolVisualType getVisual = SymbolVisualType.Etc3;// get
        private readonly SymbolVisualType multiVisual = SymbolVisualType.Etc5;
        private readonly SymbolVisualType getHitVisual = SymbolVisualType.Etc8; // git idle
        private readonly SymbolVisualType getIdleLoopVisual = SymbolVisualType.Etc4; // get idle loop

        private struct MoveInfo
        {
            public int targetIndex;
            public int destIndex;
            public string symbolID;
            public Vector3 originPos;
        }

        private class RowInfo
        {
            public int columnIndex = -1;
            public string symbolID = string.Empty;
            public int reelIndex = -1;

            public RowInfo(int reelIndex, int columnIndex, string symbolID)
            {
                this.columnIndex = columnIndex;
                this.symbolID = symbolID;
                this.reelIndex = reelIndex;
            }

            public override string ToString()
            {
                string text = string.Format("columnIndex : {0}, reelIndex : {1}, symbolID : {2}", columnIndex, reelIndex, symbolID);

                return text;
            }
        }

        private Dictionary<int, List<RowInfo>> rowInfos = new Dictionary<int, List<RowInfo>>();
        //
        #region SortingGroup Enable

        private SortingGroup[] sortingGroups = new SortingGroup[0];

        private void Awake()
        {
            sortingGroups = this.GetComponentsInChildren<SortingGroup>();

            linkSyms.Clear();
            int startNum = linkSymsMinMax[0];
            int endNum = linkSymsMinMax[1];

            for (int i = startNum; i <= endNum; i++)
            {
                linkSyms.Add(i.ToString());
            }

            moveEffectOP = new GameObjectPool<Animator>(moveEffectRoot.gameObject, 5, () => { return Instantiate(moveEffectReference); });
        }

        private void OnEnable()
        {
            reelGroup.OnSymbolVisualActivationEvent.AddListener(OnSymbolVisualActive);

            foreach (SortingGroup sortingGroup in sortingGroups)
            {
                sortingGroup.enabled = true;
            }
        }
        private void OnDisable()
        {
            reelGroup.OnSymbolVisualActivationEvent.RemoveListener(OnSymbolVisualActive);

            foreach (SortingGroup sortingGroup in sortingGroups)
            {
                sortingGroup.enabled = false;
            }
        }
        #endregion

        public IEnumerator Initialize(List<string> startSymsList, List<long> jackpotCoins)
        {
            isInitialize = false;

            if (startSymsList != null)
            {
                yield return new WaitForSeconds(initDelayTime);
            }

            Debug.Log("Link Initialize");
            OverrideReelGroup();

            SetupSpinOrder();
            SetupReelGroup(startSymsList);

            reelStopInfoList.Clear();

            AddSubstitute();

            linkHUD.OnStart(slotMachine.Info, (startSymsList != null), jackpotCoins);

            for (int i = 0; i < dropHitAnims.Length; i++)
            {
                dropHitAnims[i].gameObject.SetActive(true);
                dropHitAnims[i].ResetAllTriggers();
            }

            yield return OnDropLink();

            yield return new WaitForSeconds(linkStartDelayTime);

            isInitialize = true;

            yield break;
        }

        private void OnSymbolVisualActive(int reelIndex, Symbol symbol, SymbolVisualType visual, bool active)
        {
            // hit 후 종료 상태이면 SetEmpty
            if (visual == SymbolVisualType.Idle && active == true)
            {
                if (symbol.GetVisualElement() != null)
                {
                    var spine = symbol.GetVisualElement().skeletonAnimation;
                    if (spine != null && spine.state != null)
                    {
                        var spineAnim = spine.state.GetCurrent(0);

                        if (spineAnim != null)
                        {
                            if (spineAnim.Animation.Name == "get_idle_loop")
                            {
                                spine.state.SetEmptyAnimation(0, 0.0f);
                            }
                        }
                    }
                }
            }
        }

        private void AddSubstitute()
        {
            // 링크에서 나올 수 있는 일반 심볼을 어두운 버전으로 변경
            Symbol.RemoveAllSubstituteVisualsAtGlobal();
            for (int i = linkDimdSyms[0]; i <= linkDimdSyms[1]; ++i)
            {
                string symbolID = i.ToString();
                Symbol.AddSubstituteVisualAtGlobal(symbolID, SymbolVisualType.Idle, 0, SymbolVisualType.Idle, 1);
            }

            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                var reel = reelGroup.GetReel(reelIndex);
                reel.SetVisualAllMainSymbols(SymbolVisualType.Idle, 1);
            }

        }
        private void SetupSpinOrder()
        {
            spinOrder.Clear();
            spinOrder.AddRange(SPIN_ORDERS);
        }

        private void SetupReelGroup(List<string> startSymsList)
        {
            FeedToReelStrip();

            if (coinManager == null)
            {
                coinManager = new CoinManager1048(slotMachine, reelGroup);
            }
            coinManager.SetReelstripCoinValue();

            if (startSymsList == null)
            {
                InitializeReels();
            }
            else
            {
                InitializeReels(startSymsList);
            }



            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                SetStickySymbol(reelIndex);
            }

            reelGroup.OnStateStart.AddListener(OnReelStateStart);
            reelGroup.OnStateEnd.AddListener(OnReelStateEnd);
        }

        #region OnReelState
        private void OnReelStateStart(int reelIndex, ReelState state, BaseReel reel)
        {
            if (state == ReelState.Slowdown)
            {
                ConvertReelStrip(reel, reelIndex);
            }
        }

        private void OnReelStateEnd(int reelIndex, ReelState state, BaseReel reel)
        {
            if (state == ReelState.Slowdown)
            {
                reelStopSound.Play();

                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);
                    if (IsLinkSymbol(symbol.id, false))
                    {
                        StartCoroutine(SetAppaer(symbol, reelIndex));
                    }
                }
            }
        }

        private void ConvertReelStrip(BaseReel reel, int reelIndex)
        {
            var reelStrip = reel.ReelStrip;

            string firstID = reelStrip.GetID(0);
            string endID = reelStrip.GetID(reelStrip.Length - 1);

            if (IsLinkSymbol(firstID, false) && IsLinkSymbol(endID, false))
            {
                int randIndex = Random.Range(0, linkDimdSyms.Length - 1);
                string randSymsId = linkDimdSyms[randIndex].ToString();
                reelStrip.AddID(randSymsId);

                Debug.LogFormat("convert! {0} => {1}", reelIndex, reelStrip.GetStripString());
            }
        }

        private void ConvertSymbol(BaseReel reel)
        {
            int prevIndex = 0;
            int nextIndex = 2;

            var prevSymbol = reel.GetSymbol(prevIndex);
            var nextSymbol = reel.GetSymbol(nextIndex);

            if (IsLinkSymbol(prevSymbol.id, false))
            {
                int randIndex = Random.Range(0, linkDimdSyms.Length - 1);
                string randSymsId = linkDimdSyms[randIndex].ToString();
                reel.ChangeSymbol(prevIndex, randSymsId);
            }

            if (IsLinkSymbol(nextSymbol.id, false))
            {
                int randIndex = Random.Range(0, linkDimdSyms.Length - 1);
                string randSymsId = linkDimdSyms[randIndex].ToString();
                reel.ChangeSymbol(nextIndex, randSymsId);
            }
        }

        private IEnumerator SetAppaer(Symbol symbol, int reelIndex)
        {
            symbol.SetVisual(appearVisual);
            yield return new WaitForSeconds(appearTime);
            SetStickySymbol(reelIndex);
        }
        #endregion

        public void InitializeReels()
        {
            Debug.Log("InitializeReels to start");


            ReelStripJumpToSyms();

            for (int i = 0; i < reelGroup.Column; i++)
            {
                var reel = reelGroup.GetReel(i);
                reel.Initialize();
                ConvertSymbol(reel);
            }
        }

        private void InitializeReels(List<string> startSymsList)
        {
            Debug.Log("InitializeReels");

            for (int i = 0; i < reelGroup.Column; i++)
            {
                reelGroup.GetReel(i).Initialize();
            }

            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                var reel = reelGroup.GetReel(reelIndex);
                string targetSymbolID = startSymsList[reelIndex];

                if (onlyNormalSyms.Contains(targetSymbolID))
                {
                    targetSymbolID = Random.Range(linkDimdSyms[0], linkDimdSyms[1]).ToString();
                }
                reel.ChangeMainSymbol(MAIN_SYMBOL_INDEX, targetSymbolID);

                ConvertSymbol(reel);
            }
        }

        private void SetStickySymbol(int reelIndex)
        {
            var reel = reelGroup.GetReel(reelIndex);

            if (reel.IsStickyMainSymbol(MAIN_SYMBOL_INDEX) == false)
            {
                var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX);
                if (linkSyms.Contains(symbol.id) == true)
                {
                    Vector3 targetPos = new Vector3(symbol.transform.position.x, symbol.transform.position.y, -1.0f);
                    int rowIndex = reelIndex / ROW_COUNT;

                    var stickySymbol = reel.AddStickyMainSymbol(MAIN_SYMBOL_INDEX, symbol.id, targetPos);
                    stickySymbol.Value = coinManager.GetStringCoinValue(stickySymbol.id);
                    stickySymbol.transform.SetParent(stickyRoots[rowIndex], true);
                    stickySymbol.SetVisual(SymbolVisualType.Idle, 1);
                    spinOrder.Remove(reelIndex);

                    // 기존 심볼 empty
                    reel.ChangeMainSymbol(MAIN_SYMBOL_INDEX, EMPTY_SYMBOL_ID);
                    symbol.Value = string.Empty;
                }
            }
        }

        private void ReelStripJumpToSyms()
        {
            int[] syms = slotMachine.Info.GetSyms();

            if (syms != null)
            {
                List<List<string>> symsList = SlotMachineUtils.ConvertSymsToList(syms, reelGroup.Column, reelGroup.Row);
                for (int reelIndex = 0; reelIndex < symsList.Count; reelIndex++)
                {
                    var reel = reelGroup.GetReel(reelIndex);
                    List<string> symbols = symsList[reelIndex];

                    for (int i = 0; i < symbols.Count; i++)
                    {
                        if (symbols[i] == EMPTY_SYMBOL_ID)
                        {
                            int newSymbolID = Random.Range(1, 11);
                            symbols[i] = newSymbolID.ToString();
                        }

                    }

                    int startIndex = reelStripHelper.FindStopIndexOnReelStrip(reel.ReelStrip, symbols);
                    reel.ReelStrip.Jump(startIndex);
                }
            }
        }

        public void FeedToReelStrip(bool init = false)
        {
            Debug.Log("FeedToReelStrip");

            string[] serverReels = slotMachine.Info.GetReelsAt(0);

            feed.Feed(serverReels);
        }

        public void OverrideReelGroup()
        {
            //슬롯머신이 이후 스핀을 돌릴 때 base 릴그룹이 아니라 link 릴그룹을 돌리도록 한다.
            slotMachine.MainMachine.SetOverrideReelGroup(reelGroup);
        }

        #region Play

        public IEnumerator SpinStart()
        {
            slotMachine.SetSpinReelIndex(spinOrder);

            yield return new WaitForSeconds(spinStartDelayTime);

            spinStartSound.Play();
            linkHUD.UpdateRespinCount();
        }

        public void SpinPreStop()
        {
            if (slotMachine.Info.IsReelChanged == true)
            {
                FeedToReelStrip();
            }
        }

        public void SpinStop()
        {
            reelStopInfoList.Clear();

            for (int i = 0; i < spinOrder.Count; ++i)
            {
                var reelIndex = spinOrder[i];
                ReelStopInfo info = slotMachine.Info.SpinResult.GetStopInfo(reelIndex);
                reelStopInfoList.Add(info);
            }

            slotMachine.Info.SpinResult.ReelStopInfoList.Clear();
            for (int i = 0; i < reelStopInfoList.Count; ++i)
            {
                slotMachine.Info.SpinResult.AddStopInfo(reelStopInfoList[i]);
            }

            coinManager.SetReelstripCoinValue();
        }

        public IEnumerator SpinEnd()
        {
            yield return OnDropLink();

            if (slotMachine.Info.IsLinkRespinEnd == true)
            {
                yield return OnResult();
            }

            yield return new WaitForSeconds(spinEndDelayTime);

            if (slotMachine.Info.LinkRespinRemainCount == MAX_RESPIN_COUNT)
            {
                respinResetSound.Play();
                yield return linkHUD.ResetRespinCount(MAX_RESPIN_COUNT);
            }
        }

        private IEnumerator OnResult()
        {
            Debug.Log("OnResult ");

            Symbol.RemoveAllSubstituteVisualsAtGlobal();
            reelGroup.OnStateEnd.RemoveAllListeners();
            reelGroup.OnStateStart.RemoveAllListeners();

            yield return new WaitForSeconds(resultStartDelayTime);
        }

        public void LinkEnd()
        {
            Debug.Log("LinkEnd");

            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                var reel = reelGroup.GetReel(reelIndex);
                reel.RemoveAllStickySymbols();
            }

            slotMachine.MainMachine.ClearOverrideReelGroup();
            slotMachine.ResetSpinReelIndex();
        }
        #endregion

        private void SetRowInfo()
        {
            rowInfos.Clear();

            for (int reelIndex = 0; reelIndex < reelGroup.Column; reelIndex++)
            {
                var reel = reelGroup.GetReel(reelIndex);
                var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX, true);

                int columneIndex = reelIndex / ROW_COUNT;
                int rowIndex = reelIndex % ROW_COUNT;
                var rowInfo = new RowInfo(reelIndex, columneIndex, symbol.id);

                if (rowInfos.ContainsKey(rowIndex) == false)
                {
                    rowInfos.Add(rowIndex, new List<RowInfo>());
                }

                rowInfos[rowIndex].Add(rowInfo);
            }
        }

        private void GetMatchesSymbol(ref List<int> matchIndexes)
        {
            Debug.Log("GetMatchesSymbol");
            for (int columnIndex = LAST_COLUMN_INDEX; columnIndex >= 0; columnIndex--)
            {
                bool allLineLink = true;
                foreach (var rowInfo in rowInfos)
                {
                    var info = rowInfo.Value.Find(x => x.columnIndex == columnIndex);
                    allLineLink &= IsLinkSymbol(info.symbolID, false);
                }

                if (allLineLink)
                {
                    matchIndexes.Add(columnIndex);
                }
            }
        }

        private void SetJackpotInfo()
        {
            jackpotInfos.Clear();
            coinQueue.Clear();

            // Win Info 에 등장하는 잭팟 리스트
            for (int i = 0; i < slotMachine.Info.SpinResult.GetWinInfoCountOfAllMachines(); i++)
            {
                var winInfo = slotMachine.Info.SpinResult.GetWinInfoFromAllMachines(i);

                if (winInfo.jackpot < 0) continue;

                int jackpotGrade = winInfo.jackpot - 1;
                LinkJackpotInfo info = new LinkJackpotInfo()
                {
                    jp = jackpotGrade,
                    coins = winInfo.winCoins,
                    fix = false,
                    used = false
                };

                Debug.Log(info.ToString());

                jackpotInfos.Add(info);
                if (coinQueue.ContainsKey(jackpotGrade) == false)
                {
                    coinQueue.Add(jackpotGrade, new Queue<long>());
                    coinQueue[jackpotGrade].Enqueue(winInfo.winCoins);
                }
                else
                {
                    coinQueue[jackpotGrade].Enqueue(winInfo.winCoins);
                }

                Debug.LogFormat("coin Enqueue => {0} / {1}", jackpotGrade, winInfo.winCoins);
            }
        }

        private void SetFixJackpotInfo(List<int> matchIndexes)
        {
            Debug.Log("SetFixJackpotInfo");
            List<int> fixJackpots = new List<int>();

            // 등장한 고정 잭팟 리스트
            for (int i = 0; i < matchIndexes.Count; i++)
            {
                int columnIndex = matchIndexes[i];

                foreach (var rowInfo in rowInfos)
                {
                    var lastInfo = rowInfo.Value.Find(x => x.columnIndex == columnIndex);
                    var reel = reelGroup.GetReel(lastInfo.reelIndex);
                    var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX, true);

                    int jackpotGrade = IsJackpotSymbol(symbol.id);
                    if (jackpotGrade != -1)
                    {
                        fixJackpots.Add(jackpotGrade);
                    }
                }
            }

            // 랜덤 잭팟 파싱
            for (int i = 0; i < fixJackpots.Count; i++)
            {
                int jp = fixJackpots[i];

                var infos = jackpotInfos.FindAll(x => x.jp == jp && x.fix == false);
                if (infos.Count == 0)
                {
                    Debug.Log("not matched jackpot info to fix info");
                }
                else
                {
                    infos = infos.OrderByDescending(x => x.coins).ToList();
                    // 고정 잭팟으로 변경한다.
                    infos[0].fix = true;
                }
            }
        }

        private long GetJackpotInfo(int jackpotGrade)
        {
            Debug.Log("GetJackpotInfo : " + jackpotGrade);
            var info = jackpotInfos.Find(x => x.jp == jackpotGrade && x.fix == true && x.used == false);

            long winCoin = 0;

            if (info == null)
            {
                Debug.Log("not available fix jackpot info");
                return winCoin;
            }

            winCoin = coinQueue[jackpotGrade].Dequeue();

            info.used = true;

            return winCoin;
        }

        private LinkJackpotInfo GetRandomJackpotInfo()
        {

            var infoList = jackpotInfos.FindAll(x => x.fix == false);

            if (infoList.Count == 0)
            {
                Debug.Log("not available random jackpot info");
                return null;
            }

            var info = infoList[0];
            Debug.Log("GetRandomJackpotInfo : " + info.jp);

            info.coins = coinQueue[info.jp].Dequeue();

            jackpotInfos.Remove(info);

            return info;
        }


        private IEnumerator OnDropLink()
        {
            Debug.Log("OnDropLink");
            SetJackpotInfo();

            List<int> matchIndexes = new List<int>();

            bool isFirstDrop = true;

            do
            {
                matchIndexes.Clear();

                SetRowInfo();

                // 심볼 이동
                yield return OnMoveSymbol(isFirstDrop);

                // 매치 심볼 판정
                GetMatchesSymbol(ref matchIndexes);

                int indicatorIndex = matchIndexes.Count - 1;

                // 당첨라인이 있으면 Effect On
                if (matchIndexes.Count > 0)
                {
                    matchIndexes.Sort();

                    dropHitAnims[indicatorIndex].SetBool(DROPHIT_TRIGGER_ONOFF, true);
                    frameOpenSound.Play();

                    yield return new WaitForSeconds(dropHitOnTime);
                }
                else
                {
                    yield return new WaitForSeconds(spinMoveEndDelayTime);
                    yield break;
                }

                SetFixJackpotInfo(matchIndexes);

                // 라인 히트
                dropHitAnims[indicatorIndex].SetTrigger(TRIGGER_HIT);
                frameHitSound.Play();

                yield return new WaitForSeconds(dropHitIdleLoopDelayTime);
                // getting 연출 on
                OnGetterSymbol(matchIndexes);

                // 배수 팝업 On
                if (matchIndexes.Count > 1)
                {
                    yield return new WaitForSeconds(multiplyBeforeDelayTime);
                    multiPopupSound.Play();
                    multiplierPopup.gameObject.SetActive(true);
                    multiplierPopup.Open(matchIndexes.Count);
                    yield return new WaitForSeconds(multiplyAfterDelayTime);
                }

                yield return new WaitForSeconds(dropHitTime);

                // 두줄이면 2배 멀티플라이 적용
                if (matchIndexes.Count > 1)
                {
                    multiApplySound.Play();
                    yield return SetMultiPly(matchIndexes);
                }

                // 정산
                yield return OnDispatchCoin(matchIndexes);

                dropHitAnims[indicatorIndex].SetBool(DROPHIT_TRIGGER_ONOFF, false);

                yield return new WaitForSeconds(dropHitAfterDelayTime);

                isFirstDrop = false;

            } while (matchIndexes.Count > 0);
        }

        private IEnumerator OnDispatchCoin(List<int> matchIndexes)
        {
            Debug.Log("OnDispatchCoin");
            int multiCount = matchIndexes.Count;

            // 심볼 정산
            for (int i = 0; i < multiCount; i++)
            {
                int columnIndex = matchIndexes[i];

                foreach (var rowInfo in rowInfos)
                {
                    var lastInfo = rowInfo.Value.Find(x => x.columnIndex == columnIndex);
                    var reel = reelGroup.GetReel(lastInfo.reelIndex);
                    var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX, true);

                    // Blank 심볼 스킵
                    if (symbol.id == BLANK_SYMBOL_ID)
                    {
                        reel.RemoveStickyMainSymbol(MAIN_SYMBOL_INDEX);
                        spinOrder.Add(lastInfo.reelIndex);

                        spinOrder = spinOrder.OrderBy(SPIN_ORDERS.IndexOf).ToList();

                        continue;
                    }

                    // 잭팟 팝업
                    int jackpotGrade = IsJackpotSymbol(symbol.id);
                    LinkJackpotInfo jpInfo = null;
                    bool isRandomJackpot = IsRandomJackpot(symbol.id);
                    long jpWinCoins = 0;

                    if (isRandomJackpot)
                    {
                        jpInfo = GetRandomJackpotInfo();

                        if (jpInfo != null)
                        {
                            string resultSymbolID = (jpInfo.jp + jackpotGradeParse).ToString();
                            // 랜덤 잭팟 릴 스핀 
                            yield return OnRandomReel(symbol, jpInfo, resultSymbolID, multiCount);

                            reel.RemoveStickyMainSymbol(MAIN_SYMBOL_INDEX);

                            symbol = reel.AddStickyMainSymbol(MAIN_SYMBOL_INDEX, resultSymbolID);
                            int rowIndex = lastInfo.reelIndex / ROW_COUNT;
                            symbol.transform.SetParent(stickyRoots[rowIndex], true);
                            symbol.SetVisual(getHitVisual);

                            // 잭팟 팝업
                            linkHUD.OnJackpot(jpInfo.jp);

                            yield return slotMachine.QuickHitLinkJackpotPopup(jpInfo.jp, multiCount, jpInfo.coins);


                        }
                    }
                    else if (jackpotGrade != -1)
                    {
                        linkHUD.OnJackpot(jackpotGrade);
                        // 잭팟 팝업
                        jpWinCoins = GetJackpotInfo(jackpotGrade);
                        yield return slotMachine.QuickHitLinkJackpotPopup(jackpotGrade, multiCount, jpWinCoins);
                    }

                    // 이동 연출
                    symbol.SetVisual(emptyVisual);
                    symbol.Value = string.Empty;



                    var moveEffect = moveEffectOP.Get();
                    moveEffect.ResetAllTriggers();
                    moveEffect.transform.SetParent(moveEffectRoot);
                    moveEffect.transform.position = symbol.transform.position;
                    moveEffect.transform.localScale = Vector3.one;

                    moveEffect.transform.DOMove(moveTarget.position, moveTime).SetEase(moveEffectEase);
                    winCoinTrailSound.Play();

                    yield return new WaitForSeconds(moveTime);

                    moveEffect.SetTrigger(TRIGGER_HIT);
                    // 정산
                    if (isRandomJackpot)
                    {
                        yield return slotMachine.SetLinkWinCoin(jpInfo.coins);

                        linkHUD.OffJackpot(jpInfo.jp);
                        linkHUD.OffMultiplier(jpInfo.jp);
                        linkHUD.ResetJackpot(jpInfo.jp);
                    }
                    else if (jackpotGrade != -1)
                    {
                        yield return slotMachine.SetLinkWinCoin(jpWinCoins);

                        linkHUD.OffJackpot(jackpotGrade);
                        linkHUD.OffMultiplier(jackpotGrade);
                        linkHUD.ResetJackpot(jackpotGrade);
                    }
                    else
                    {
                        long coinValue = coinManager.GetLongCoinValue(symbol.id);
                        coinValue = (multiCount > 1) ? coinValue * multiCount : coinValue;

                        if (coinValue > 0)
                        {
                            yield return slotMachine.SetLinkWinCoin(coinValue);
                        }
                    }

                    yield return new WaitForSeconds(moveHitDelayTime);
                    moveEffectOP.Return(moveEffect);

                    reel.RemoveStickyMainSymbol(MAIN_SYMBOL_INDEX);

                    spinOrder.Add(lastInfo.reelIndex);

                    spinOrder = spinOrder.OrderBy(SPIN_ORDERS.IndexOf).ToList();

                    yield return new WaitForEndOfFrame();
                }
            }
        }

        private IEnumerator OnRandomReel(Symbol symbol/*sticky*/, LinkJackpotInfo info, string stopSymbolID, int multiCount)
        {
            yield return new WaitForSeconds(randomReelStartDelayTime);

            symbol.SetVisual(emptyVisual);

            var currentReel = Instantiate(linkRandomReel);
            currentReel.transform.SetParent(linkRandomReelRoot, true);

            yield return currentReel.OnStart(symbol.transform.position, stopSymbolID);

            AddSubstitute();

            while (currentReel.IsEnded == false)
            {
                yield return null;
            }

            // HUD 멀티
            if (multiCount > 1)
            {
                linkHUD.OnMultiplier(info.jp, multiCount);
            }
            Debug.Log("LinkFeatuer ::OnRandomReel End, stopSymbolID = " + stopSymbolID);

            yield return new WaitForSeconds(randomReelEndDelayTime);

            Destroy(currentReel.gameObject);
        }

        private void OnGetterSymbol(List<int> matchIndexes)
        {
            // 심볼 정산
            for (int i = 0; i < matchIndexes.Count; i++)
            {
                int columnIndex = matchIndexes[i];

                // 심볼 정산
                foreach (var rowInfo in rowInfos)
                {
                    var lastInfo = rowInfo.Value.Find(x => x.columnIndex == columnIndex);
                    var reel = reelGroup.GetReel(lastInfo.reelIndex);
                    var symbol = reel.GetMainSymbol(MAIN_SYMBOL_INDEX, true);
                    symbol.SetVisual(getVisual);
                    if (symbol.id != BLANK_SYMBOL_ID)
                    {
                        symbol.SetVisual(getIdleLoopVisual);
                    }
                    reel.ChangeMainSymbol(MAIN_SYMBOL_INDEX, EMPTY_SYMBOL_ID);
                }
            }
        }

        private void DeleteBlankSymbol(List<int> matchIndexes)
        {
            for (int i = 0; i < matchIndexes.Count; i++)
            {
                int columnIndex = matchIndexes[i];

                // 심볼 삭제
                foreach (var rowInfo in rowInfos)
                {
                    var lastInfo = rowInfo.Value.Find(x => x.columnIndex == columnIndex);
                    var symbol = reelGroup.GetReel(lastInfo.reelIndex).GetMainSymbol(MAIN_SYMBOL_INDEX, true);
                    if (IsLinkSymbol(symbol.id, true))
                    {
                        symbol.SetVisual(getVisual);
                    }
                }
            }
        }

        private IEnumerator DeleteBlankSymbolAction(Symbol symbol)
        {
            symbol.SetVisual(getVisual);
            yield return new WaitForSeconds(blankHitTime);
            symbol.SetVisual(emptyVisual);
        }

        private IEnumerator SetMultiPly(List<int> matchIndexes)
        {
            Debug.Log("SetMultiply");
            for (int i = 0; i < matchIndexes.Count; i++)
            {
                int columnIndex = matchIndexes[i];

                // 심볼 배수 연출
                foreach (var rowInfo in rowInfos)
                {
                    var lastInfo = rowInfo.Value.Find(x => x.columnIndex == columnIndex);
                    var symbol = reelGroup.GetReel(lastInfo.reelIndex).GetMainSymbol(MAIN_SYMBOL_INDEX, true);
                    if (IsLinkSymbol(symbol.id, true))
                    {
                        SetMultiplierSymbol(symbol, matchIndexes.Count);
                    }
                }
            }

            yield return new WaitForSeconds(coinMultiplyTime);
        }

        private void SetMultiplierSymbol(Symbol symbol, int multiCount)
        {
            int jackpotGrade = IsJackpotSymbol(symbol.id);

            // 랜덤 잭팟 심볼인 경우
            if (IsRandomJackpot(symbol.id))
            {
                symbol.SetVisual(hitVisual, multiCount - 1);
            }
            // 잭팟 심볼인 경우
            else if (jackpotGrade != -1)
            {
                symbol.SetVisual(hitVisual, multiCount - 1);

                // HUD 멀티
                linkHUD.OnMultiplier(jackpotGrade, multiCount);
            }
            // 일반 심볼인 경우
            else
            {
                long symbolValue = coinManager.GetLongCoinValue(symbol.id);
                if (symbolValue > 0)
                {
                    long coinValueMul = symbolValue * multiCount;
                    symbol.SetVisual(multiVisual);
                    symbol.Value = coinValueMul.ToString();
                }
            }
        }

        private IEnumerator OnMoveSymbol(bool isFirstDrop)
        {
            Debug.Log("OnMoveSymbol");
            // 심볼 이동 구간 찾기
            List<MoveInfo> moveInfo = GetMoveSymbolList();

            if (moveInfo.Count == 0)
            {
                yield break;
            }

            yield return new WaitForSeconds(dropStartDelayTime);

            if (isFirstDrop)
            {
                dropSound.Play();
            }
            else
            {
                afterDropSound.Play();
            }

            for (int i = 0; i < moveInfo.Count; i++)
            {
                var info = moveInfo[i];

                Symbol targetSymbol = reelGroup.GetReel(info.targetIndex).GetMainSymbol(MAIN_SYMBOL_INDEX, true);

                Vector3 destPos = reelGroup.GetReel(info.destIndex).transform.position;

                targetSymbol.transform.DOMoveY(destPos.y, dropTime).SetEase(dropEase);
            }

            yield return new WaitForSeconds(dropTime);

            dropEndSound.Play();
            for (int i = 0; i < moveInfo.Count; i++)
            {
                var info = moveInfo[i];

                // 스핀 오더 리세팅
                spinOrder.Add(info.targetIndex);
                spinOrder.Remove(info.destIndex);

                spinOrder = spinOrder.OrderBy(SPIN_ORDERS.IndexOf).ToList();

                // 심볼 이동
                var targetReel = reelGroup.GetReel(info.targetIndex);
                targetReel.RemoveStickyMainSymbol(MAIN_SYMBOL_INDEX);
                var targetSymbol = targetReel.ChangeMainSymbol(MAIN_SYMBOL_INDEX, EMPTY_SYMBOL_ID);
                targetSymbol.transform.position = info.originPos;

                var destReel = reelGroup.GetReel(info.destIndex);
                destReel.ChangeMainSymbol(MAIN_SYMBOL_INDEX, info.symbolID);

                SetStickySymbol(info.destIndex);
            }

            SetRowInfo();

            yield return new WaitForEndOfFrame();
        }


        private List<MoveInfo> GetMoveSymbolList()
        {
            List<MoveInfo> resultList = new List<MoveInfo>();

            // 심볼 이동 구간 찾기
            for (int rowIndex = 0; rowIndex < ROW_COUNT; rowIndex++)
            {
                var reelRow = rowInfos[rowIndex];
                int destIndex = -1;

                for (int columnIndex = COLUMN_COUNT - 1; columnIndex >= 0; columnIndex--)
                {
                    var info = reelRow.Find(x => x.columnIndex == columnIndex);
                    if (IsLinkSymbol(info.symbolID, false) == false)
                    {
                        if (destIndex == -1)
                        {
                            destIndex = info.reelIndex;
                        }
                    }
                    else
                    {
                        if (destIndex != -1)
                        {
                            Vector3 targetPos = reelGroup.GetReel(info.reelIndex).GetMainSymbol(MAIN_SYMBOL_INDEX).transform.position;
                            var moveInfo = new MoveInfo() { targetIndex = info.reelIndex, destIndex = destIndex, symbolID = info.symbolID, originPos = targetPos };
                            resultList.Add(moveInfo);

                            destIndex = info.reelIndex;
                        }
                    }
                }
            }

            for (int i = 0; i < resultList.Count; i++)
            {
                var info = resultList[i];
            }

            return resultList;
        }

        private bool IsLinkSymbol(string symbolID, bool exceptBlank)
        {
            int symbolId = int.Parse(symbolID);

            int linkSymbolStart = linkSymsMinMax[0];
            int linkSymEnd = linkSymsMinMax[1];

            for (int i = linkSymbolStart; i <= linkSymEnd; i++)
            {
                if (exceptBlank == true && symbolId.ToString() == BLANK_SYMBOL_ID) continue;

                if (symbolId == i) return true;
            }

            return false;
        }

        private int IsJackpotSymbol(string symbolID)
        {
            for (int i = 0; i < jackpotSymbolIDs.Length; i++)
            {
                if (jackpotSymbolIDs[i] == symbolID)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool IsRandomJackpot(string symbolID)
        {
            if (symbolID == randomJackpotSymbolID)
            {
                return true;
            }
            return false;
        }
    }
}