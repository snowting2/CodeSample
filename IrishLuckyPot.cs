using System.Collections;
using UnityEngine;
using SlotGame.Attribute;
using System.Collections.Generic;
using SlotGame.Sound;

namespace SlotGame.Machine.S1036
{
    public class SlotMachine1036 : SlotMachine
    {
#pragma warning disable 0649
        [Separator("Extensions")]
        [SerializeField] private Animator reelAnimator;
        [Space(10)]
        [SerializeField] private Animator payNumberHeadAnimator;
        [SerializeField] private GameObject[] payNumberEachObject;
        [Space(10)]
        [SerializeField] private Animator payInfoHeadAnimator;
        [SerializeField] private GameObject[] payInfoEachObject;
        [Space(10)]
        [SerializeField] private Animator luckyPotHeadAnimator;
        [SerializeField] private GameObject[] luckyPotEachObject;
        [Space(10)]
        [SerializeField] private Animator treeAnimator;
        [SerializeField] private GameObject LightEffect;
        [Space(10)]
        [SerializeField] private Animator dimmAnimator;

        [Space(10)]
        [SerializeField] private string[] lineBetSymbolIDs = new string[5];
        [SerializeField] private string luckyPotSymbolID;
        [SerializeField] private string high1SymbolID;
        [SerializeField] private string high2SymbolID;
        [SerializeField] private string blankSymbolID;
        [SerializeField] private GameObject reelEffectObj;
        [SerializeField] private GameObject luckyPotReelEffectInfo;

        [Header("Sound")]
        [SerializeField] private SoundPlayer[] AccSound = new SoundPlayer[2];
        [SerializeField] private SoundPlayer HighSound_2_1;
        [SerializeField] private SoundPlayer HighSound_2_2;
        [SerializeField] private SoundPlayer HighSound_2_3;
        [SerializeField] private SoundPlayer jackpotSound;


#pragma warning restore 0649

        private readonly string REEL_SPIN_TRIGGER = "reel_spin_onoff";
        private readonly string REEL_STOP_TRIGGER = "reel_stop_onoff";

        private readonly string PAY_NUMBER_TRIGGER = "pay_number_onoff";
        private readonly string PAY_INFO_TRIGGER = "pay_info_onoff";

        private readonly string LUCKY_POT_TRIGGER = "pot_info_onoff";

        private readonly string TREE_TRIGGER = "tree_onoff";

        private readonly string DIMM_TRIGGER = "dimd_onoff";

        private readonly int LINE_BET_HIT_COUNT = 3;
        private readonly int LUCKY_POT_COUNT_WIN_9 = 2;
        private readonly int LUCKY_POT_COUNT_JACKPOT = 3;

        private bool isHitPayline = false;
        private ReelEffect reelEffect = null;
        private bool isAlreadyShowReelEffect = false;
        private int accSoundPlayCount = 0;

        // 슬롯머신 진입시 최초 한번 실행되어야 할 스테이트
        // 이 부분에 슬롯머신 진입 애니메이션 등을 구현해야함.
        protected override IEnumerator EnterState()
        {
            paylinesHelper.OnPaylineDrawAllEvent.AddListener(OnPaylineDrawAllEvent);
            paylinesHelper.OnPaylineDrawEvent.AddListener(OnPaylineDrawEvent);

            LightEffect.SetActive(false);
            reelEffect = this.GetComponentInChildren<ReelEffect>();

            yield return base.EnterState();
        }

        // 기본 상태.
        // 기본적으로 유저의 입력을 기다리지만 프리스핀, 리스핀, 오토스핀등의 상황에선 자동으로 스핀 상태로 변경.
        protected override IEnumerator IdleState()
        {
            reelAnimator.SetBool(REEL_STOP_TRIGGER, false);

            if (isHitPayline == false)
            {
                payNumberHeadAnimator.SetBool(PAY_NUMBER_TRIGGER, false);
                payInfoHeadAnimator.SetBool(PAY_INFO_TRIGGER, false);
                luckyPotHeadAnimator.SetBool(LUCKY_POT_TRIGGER, false);

                PayNumberObjectInitialize();
                LineBetObjectInitialize();
                LuckyPotObjectInitialize();
            }

            yield return base.IdleState();
        }

        // 스핀을 시작하는 스테이트.
        protected override IEnumerator SpinStartState()
        {
            payNumberHeadAnimator.SetBool(PAY_NUMBER_TRIGGER, true);
            payInfoHeadAnimator.SetBool(PAY_INFO_TRIGGER, true);
            luckyPotHeadAnimator.SetBool(LUCKY_POT_TRIGGER, true);

            PayNumberObjectInitialize();
            LineBetObjectInitialize();
            LuckyPotObjectInitialize();

            isHitPayline = false;
            isAlreadyShowReelEffect = false;
            accSoundPlayCount = 0;

            reelAnimator.SetBool(REEL_SPIN_TRIGGER, true);

            LightEffect.SetActive(false);

            yield return base.SpinStartState();
        }

        // 스핀을 멈추는 스테이트.
        // 스핀 정지 명령을 내리고 실제 스핀이 멈추는 순간까지 유지된다.
        protected override IEnumerator SpinStopState()
        {
            yield return base.SpinStopState();
        }

        // 스핀이 완전히 종료되었을때 호출 되는 스테이트
        // 스핀 종료 후 특별한 연출등을 표현 (ex : 스택드 와일드 합체 등등)
        protected override IEnumerator SpinEndState()
        {
            reelAnimator.SetBool(REEL_SPIN_TRIGGER, false);
            reelAnimator.SetBool(REEL_STOP_TRIGGER, true);

            yield return base.SpinEndState();
        }

        public void OnReelEffectAppear(int presetIndex, int reelIndex, BaseReel reel)
        {
            dimmAnimator.SetBool(DIMM_TRIGGER, true);

            var preset = reelEffect.presets[presetIndex];


            if (preset.triggerSymbols.Count == 1)
            {
                // 럭키팟 점멸
                luckyPotReelEffectInfo.SetActive(true);
                isAlreadyShowReelEffect = true;
                ReelAccSoundPlay();
            }
            else
            {
                if (reelIndex == 2 && isAlreadyShowReelEffect == false)
                {
                    reelEffectObj.SetActive(true);
                    ReelAccSoundPlay();
                }

                for (int triggerIndex = 0; triggerIndex < preset.triggerSymbols.Count; triggerIndex++)
                {
                    if (preset.triggerSymbols[triggerIndex].id == high1SymbolID)
                    {
                        // high1 점멸
                        payInfoEachObject[3].SetActive(true);
                    }
                    else if (preset.triggerSymbols[triggerIndex].id == high2SymbolID)
                    {
                        //high2 점멸
                        payInfoEachObject[4].SetActive(true);
                    }
                }
            }
        }

        private void ReelAccSoundPlay()
        {
            AccSound[accSoundPlayCount].Play();
            accSoundPlayCount++;
        }

        public void OnReelEffectDisappear()
        {
            if (reelEffectObj.activeSelf == true)
            {
                reelEffectObj.SetActive(false);
            }

            if (luckyPotReelEffectInfo.activeSelf == true)
            {
                luckyPotReelEffectInfo.SetActive(false);

            }
            isAlreadyShowReelEffect = false;

            dimmAnimator.SetBool(DIMM_TRIGGER, false);
        }

        // Pay Number Hit All    
        private void OnPaylineDrawAllEvent()
        {
            PayNumberObjectInitialize();
            LineBetObjectInitialize();
            LuckyPotObjectInitialize();

            for (int infoIndex = 0; infoIndex < Info.SpinResult.GetWinInfoCountOfAllMachines(); infoIndex++)
            {
                var winInfo = Info.SpinResult.GetWinInfoFromAllMachines(infoIndex);
                int paylineIndex = winInfo.paylineIndex;
                payNumberEachObject[paylineIndex].SetActive(true);
                SetLineBetHit(winInfo.hitSymbolPositions, true);
            }
        }

        private void OnPaylineDrawEvent(SpinResult.WinInfo winInfo)
        {
            // Pay Number Hit     
            PayNumberObjectInitialize();
            LineBetObjectInitialize();
            LuckyPotObjectInitialize();

            payNumberEachObject[winInfo.paylineIndex].SetActive(true);

            SetLineBetHit(winInfo.hitSymbolPositions, false);
        }

        private void PayNumberObjectInitialize()
        {
            for (int i = 0; i < payNumberEachObject.Length; i++)
            {
                payNumberEachObject[i].SetActive(false);
            }
        }

        private void LineBetObjectInitialize()
        {
            for (int i = 0; i < payInfoEachObject.Length; i++)
            {
                payInfoEachObject[i].SetActive(false);
            }
        }

        private void LuckyPotObjectInitialize()
        {
            for (int i = 0; i < luckyPotEachObject.Length; i++)
            {
                luckyPotEachObject[i].SetActive(false);
            }
        }

        private void SetLineBetHit(List<List<int>> hitSymbol, bool allHit)
        {
            string currentSymbolID = string.Empty;
            int symbolCount = 0;
            int potCount = 0;
            int allCount = 0;

            for (int reelIndex = 0; reelIndex < hitSymbol.Count; reelIndex++)
            {
                var symbolList = hitSymbol[reelIndex];
                for (int symbolIndex = 0; symbolIndex < symbolList.Count; symbolIndex++)
                {
                    int mainSymbolIndex = symbolList[symbolIndex];

                    var symbol = ReelGroup.GetReel(reelIndex).GetMainSymbol(mainSymbolIndex);

                    // 첫 심볼이 와일드가 나오는 경우 포함
                    if (currentSymbolID == string.Empty || currentSymbolID == luckyPotSymbolID)
                    {
                        currentSymbolID = symbol.id;
                    }

                    if (symbol.id == luckyPotSymbolID)
                    {
                        symbolCount++;
                        potCount++;
                    }
                    else if (currentSymbolID == symbol.id)
                    {
                        symbolCount++;
                    }
                    allCount++;
                }
            }

            if (symbolCount == LINE_BET_HIT_COUNT)
            {
                PayInfoHit(currentSymbolID);
            }

            LuckyPotHit(potCount, allCount, allHit);
        }

        private void PayInfoHit(string symbolID)
        {
            int index = -1;
            for (int i = 0; i < lineBetSymbolIDs.Length; i++)
            {
                if (lineBetSymbolIDs[i] == symbolID)
                {
                    index = i;
                    break;
                }
            }

            if (index != -1)
            {
                payInfoEachObject[index].SetActive(true);
            }
        }

        // 럭키팟 히트
        private void LuckyPotHit(int potCount, int allCount, bool allHit)
        {
            bool enableHit = false;

            if (allHit)
            {
                if (allCount == LUCKY_POT_COUNT_JACKPOT)
                {
                    enableHit = true;
                }
            }
            else
            {
                if (allCount != LUCKY_POT_COUNT_JACKPOT)
                {
                    LuckyPotObjectInitialize();
                    return;
                }
                enableHit = true;
            }

            if (enableHit == true)
            {
                if (potCount > 0 && potCount <= luckyPotEachObject.Length)
                {
                    if (potCount >= LUCKY_POT_COUNT_WIN_9)
                    {
                        StartCoroutine(OnTreeBG());
                    }
                    luckyPotEachObject[potCount - 1].SetActive(true);
                }
            }
        }

        // Tree Background On
        private IEnumerator OnTreeBG()
        {
            treeAnimator.SetBool(TREE_TRIGGER, true);

            while (CurrentState != SlotMachineState.SPIN_START)
            {
                yield return null;
            }

            treeAnimator.SetBool(TREE_TRIGGER, false);
        }

        // 스핀이 멈추고 결과를 시작 되는 스테이트
        // 잭팟, 빅윈 등에 분기점
        protected override IEnumerator ResultStartState()
        {
            yield return base.ResultStartState();
        }

        // 결과 종료 스테이트
        // idle 상태로 돌아감
        protected override IEnumerator ResultEndState()
        {
            yield return base.ResultEndState();
        }

        // 심볼 히트 스테이트
        protected override IEnumerator HitState()
        {
            payNumberHeadAnimator.SetBool(PAY_NUMBER_TRIGGER, true);
            payInfoHeadAnimator.SetBool(PAY_INFO_TRIGGER, true);
            luckyPotHeadAnimator.SetBool(LUCKY_POT_TRIGGER, true);

            isHitPayline = true;

            LightEffect.SetActive(true);

            yield return base.HitState();
        }        
    }
}