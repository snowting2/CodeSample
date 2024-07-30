using DG.Tweening;
using SlotGame.Attribute;

using SlotGame.Popup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using SlotGame.Sound;
using Spine.Unity;

namespace SlotGame.Machine.S1017
{
    public class SlotMachine1017 : SlotMachine
    {
#pragma warning disable 0649
        [Separator("Extensions")]
        [Header("Intro")]
        [SerializeField] private GameObject intro;
        [SerializeField] private SpriteRenderer introDimm;
        [SerializeField] private float introWaitingTime;
        [SerializeField] private float introFadeoutTime;
        [SerializeField] private SkeletonAnimation introAnimation;

        [Header("Wild Symbol")]
        [SerializeField] private string wildSymbolTargetID;
        [SerializeField] private float wildChangeTime;
        [SerializeField] private float wildSymbolAppearTimeInFree;
        [SerializeField] private WildEffect1017 wildEffectReference;
        [SerializeField] private ParticleSystem wildParticleReference;
        [SerializeField] private GameObject wildEffectPool;
        [SerializeField] private float wildEndTime;

        [Header("Bonus")]
        [SerializeField] private Slider bonusSlider;
        [SerializeField] private Transform bonusPos;
        [SerializeField] private BonusIcon1017 bonusIconReference;
        [SerializeField] private GameObject bonusIconPool;
        [SerializeField] private GameObject bonusIconRoot;
        [SerializeField] private float bonusIconMoveDelay = 0.02f;
        [SerializeField] private float bonusIntervalTime = 0.2f;
        [SerializeField] private float bonusLayerOffset = 0.01f;
        [SerializeField] private float bonusIconPosOffest = 0.3f;
        [SerializeField] private float bonusIconOffsetDuration = 0.3f;
        [SerializeField] private float bonusIconOffsetDurationForFast = 0.2f;
        [SerializeField] private float bonusIconOffsetDurationForTurbo = 0.1f;
        [SerializeField] private string bonusSymbolID;
        [SerializeField] private int bonusMaxCount = 120;
        [SerializeField] private int bonusGaugeStart;     // Compass UI 로 가려지는 부분 커버
        [SerializeField] private int bonusLastGaugeLength = 5; // 119~120 사이 간격
        [SerializeField] private Animator bonusFullEffectAnimator;
        [SerializeField] private GameObject bonusFreeMode;
        [SerializeField] private GameObject bonusButton;
        [SerializeField] private float bonusFullEffectTime;
        [SerializeField] private float bonusIntroTime;

        [Header("Map")]
        [SerializeField] private MapFeaturePopup1017 mapFeaturePopup;

        [Header("BG")]
        [SerializeField] private Animator backgroundAnimator;

        [Header("Shake Effect")]
        [SerializeField] private float shakeDuration;
        [SerializeField] private float shakeStrength;
        [SerializeField] private int shakeVibrato;
        [SerializeField] private float shakeRandomness;

        [SerializeField] private Transform[] bgObjectlist;
        [SerializeField] private float shakeBgDuration;
        [SerializeField] private float shakeBgStrength;
        [SerializeField] private int shakeBgVibrato;
        [SerializeField] private float shakeBgRandomness;

        [Header("BGM")]
        [SerializeField] private SoundPlayer normalBGM;
        [SerializeField] private SoundPlayer freeBGM;

        [Header("Sound")]
        [SerializeField] private SoundPlayer wildAppearSound;
        [SerializeField] private SoundPlayer wildDropSound;
        [SerializeField] private SoundPlayer wildFrameSound;
        [SerializeField] private SoundPlayer vikingSigSound;
        [SerializeField] private SoundPlayer bonusSound;
        [SerializeField] private SoundPlayer bonusGaugePullSound;

#if UNITY_EDITOR
        [SerializeField] private bool isTestModeOn;
        [SerializeField] private int stageIndex_test = 0;
#endif

#pragma warning restore 0649

        private long myBonusCount;
        private List<int> includeWildReelIndexList;
        private List<Symbol> freeModeWildSymbolList;
        private GameObjectPool<BonusIcon1017> bonusIconOP;
        private GameObjectPool<WildEffect1017> wildEffectOP;
        private GameObjectPool<ParticleSystem> wildParticleOP;
        private bool isAutoSpin = false;
        private Button bonusCachedButton = null;
        private bool isDoneUpdateSlider = false;

        // 0: 현재 스테이지(0 ~ 19. 보너스 심볼 추가 시 추가 후 스테이지. 보너스 금액이 있는데 스테이지가 0이면 최종 스테이지 달성으로 처리)
        // 1: 추가된 보너스 심볼 개수
        // 2: 현재 스테이지에서 보너스 심볼 누적 개수(0 ~ 119. 보너스 추가 시 0으로 리셋됨)
        // 3: 보너스 금액(스테이지 완료 시)
        // 4: 선택된 픽 보너스 인덱스(픽 보너스 시. 0: Red, 1: Blue, 2: Green, 3: Yellow)
        // 5: 픽 보너스 Red 배당금(픽 보너스 시)
        // 6: 픽 보너스 Blue 배당금(픽 보너스 시)
        // 7: 픽 보너스 Green 배당금(픽 보너스 시)
        // 8: 픽 보너스 Yellow 배당금(픽 보너스 시)
        private readonly int EXTRAS_INDEX_STAGE = 0;
        private readonly int EXTRAS_INDEX_BNS_COUNT = 1;
        private readonly int EXTRAS_INDEX_BNS_TOTAL_COUNT = 2;
        //private readonly int EXTRAS_INDEX_BNS_COINS = 3;
        // 추가
        private readonly int EXTRAS_INDEX_BNS_NORMAL_PICK1 = 4;
        private readonly int EXTRAS_INDEX_BNS_NORMAL_PICK2 = 5;

        private readonly int EXTRAS_INDEX_BNS_PICK_INDEX = 6;
        private readonly int EXTRAS_INDEX_COIN_GRAND = 7;
        private readonly int EXTRAS_INDEX_COIN_MEGA = 8;
        private readonly int EXTRAS_INDEX_COIN_MAJOR = 9;
        private readonly int EXTRAS_INDEX_COIN_MINOR = 10;

        private readonly string STATE_BONUS_MAX = "BonusMaxState";
        private readonly string STATE_MAP_FEATURE = "MapFeatureState";

        private readonly int MAX_STAGE_INDEX = 19;

        private void Awake()
        {
            includeWildReelIndexList = new List<int>();
            freeModeWildSymbolList = new List<Symbol>();

            normalBGM.delay = introWaitingTime + introFadeoutTime;
            freeBGM.delay = introWaitingTime + introFadeoutTime;
        }

        private void OnSlotMachineExit()
        {
            try
            {
                PopupSystem.Instance.RemoveCachedPopup("FreeSpinPopup");
            }
            catch (System.Exception) { }
        }

        private void SetupBonus()
        {
            int bonusCount = GetExtraValueIndex(EXTRAS_INDEX_BNS_TOTAL_COUNT);
            bonusCachedButton = bonusButton.GetComponent<Button>();

            UpdateBonusPointSlider(bonusCount, 0.0f);

            bonusIconOP = new GameObjectPool<BonusIcon1017>(bonusIconPool,
                                                       5,
                                                       () =>
                                                       {
                                                           return Instantiate(bonusIconReference);
                                                       });

            // 보너스 게이지 Max 초기값 세팅
            bonusSlider.maxValue = bonusMaxCount + bonusGaugeStart + bonusLastGaugeLength;

#if UNITY_2022_2_OR_NEWER
            bonusFullEffectAnimator.keepAnimatorStateOnDisable = true;
#else
            bonusFullEffectAnimator.keepAnimatorControllerStateOnDisable = true;
#endif

            bonusFreeMode.SetActive(false);
        }

        private void SetupWild()
        {
            wildEffectOP = new GameObjectPool<WildEffect1017>(wildEffectPool,
                                                               5,
                                                               () =>
                                                               {
                                                                   return Instantiate(wildEffectReference);
                                                               });

            wildParticleOP = new GameObjectPool<ParticleSystem>(wildEffectPool,
                                                                 5,
                                                                 () =>
                                                                 {
                                                                     return Instantiate(wildParticleReference);
                                                                 });
        }


        protected override string GetStateBeforeEnd(bool isExtraSpin, bool isFreespinStart, bool isFreespinEnd, bool isRespin, bool isFreeChoiceStart)
        {
            if (IsBonusCountMax() == true)
            {
                return STATE_BONUS_MAX;
            }
            else
            {
                return base.GetStateBeforeEnd(isExtraSpin, isFreespinStart, isFreespinEnd, isRespin, isFreeChoiceStart);
            }
        }
        // 슬롯머신 진입시 최초 한번 실행되어야 할 스테이트
        // 이 부분에 슬롯머신 진입 애니메이션 등을 구현해야함.
        protected override IEnumerator EnterState()
        {
            SetupBonus();
            SetupWild();
            ChangeEnvReelMode(CurrentReelMode);

            // Intro 연출
            yield return new WaitForSeconds(0.3f);
            intro.SetActive(true);
            introAnimation.state.SetAnimation(0, "intro", false);
            yield return new WaitForSeconds(introWaitingTime);
            introDimm.DOFade(0, introFadeoutTime);
            yield return new WaitForSeconds(introFadeoutTime);
            intro.SetActive(false);

            normalBGM.delay = 0.0f;
            freeBGM.delay = 0.0f;

            yield return base.EnterState();
        }

        private IEnumerator BonusAppearState()
        {
            isDoneUpdateSlider = false;

            int bonusCount = GetExtraValueIndex(EXTRAS_INDEX_BNS_COUNT);
            bool isFirstComplete = true;

            if (IsBonusCountMax() == true)
            {

                //-------------------------------------------------------

                if (isAutoSpin == true)
                {
                    Debug.Log("-- bonus stage start, auto spin 일시정지");
                    AutoSpin = false;
                }
                //-------------------------------------------------------
            }

            var bonusWaitForSeconds = new WaitForSeconds(bonusIconMoveDelay);
            float offet = 0.0f;
            float intervalHalf = bonusIntervalTime / 2;
            float intervalTwice = bonusIntervalTime * 2;

            for (int reelIndex = 0; reelIndex < ReelGroup.Column; reelIndex++)
            {
                var reel = ReelGroup.GetReel(reelIndex);

                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);
                    if (symbol.id == bonusSymbolID)
                    {
                        symbol.SetVisual(SymbolVisualType.Idle);

                        var bonusIcon = bonusIconOP.Get();
                        bonusIcon.CurveMovement.CachedTransfom.SetParent(bonusIconRoot.transform, false);

                        var bonusIconPos = symbol.transform.position;
                        bonusIconPos.z = bonusIconRoot.transform.position.z + offet;

                        offet -= bonusLayerOffset;

                        bonusIcon.CurveMovement.CachedTransfom.position = bonusIconPos;

                        bonusIcon.CurveMovement.CachedTransfom.DOKill(false);
                        bonusIcon.CurveMovement.CachedTransfom.localScale = Vector3.one;

                        var targetPos = new Vector3(bonusPos.position.x, bonusPos.position.y, bonusIconPos.z);
                        var leftTopMoveTarget = new Vector3(bonusIconPos.x - bonusIconPosOffest,
                                                            bonusIconPos.y + bonusIconPosOffest,
                                                            bonusIconPos.z);

                        var leftTopMoveDuration = bonusIconOffsetDuration;

                        if (CurrentPlayModeInProgress == PlayMode.Fast)
                        {
                            leftTopMoveDuration = bonusIconOffsetDurationForFast;
                        }
                        else if (CurrentPlayModeInProgress == PlayMode.Turbo)
                        {
                            leftTopMoveDuration = bonusIconOffsetDurationForTurbo;
                        }

                        //--------------------------------------------------------------------------------------------------
                        // DoTween 시퀀스
                        //--------------------------------------------------------------------------------------------------
                        var seq = DOTween.Sequence();
                        seq.Append(bonusIcon.transform.DOMove(leftTopMoveTarget, leftTopMoveDuration));
                        seq.AppendInterval(intervalHalf);
                        seq.AppendCallback(() =>
                        {
                            bonusIcon.CurveMovement.Move(targetPos);
                        });
                        seq.AppendInterval(bonusIntervalTime);
                        seq.AppendCallback(() =>
                        {
                            bonusIcon.CurveMovement.CachedTransfom.DOScale(Vector3.one / 2.0f, bonusIcon.CurveMovement.duration);
                        });
                        seq.AppendInterval(bonusIntervalTime);
                        seq.AppendCallback(() =>
                        {
                            StartCoroutine(bonusIcon.StartEffect());

                            if (isFirstComplete == true)
                            {
                                isFirstComplete = false;
                                UpdateBonusPointSlider(bonusCount);
                            }

                        });
                        seq.AppendInterval(intervalTwice);
                        seq.AppendCallback(() =>
                        {
                            bonusIconOP.Return(bonusIcon);
                        });

                        yield return bonusWaitForSeconds;
                        //--------------------------------------------------------------------------------------------------
                    }
                }

                bonusSound.Play();
            }
        }

        // 기본 상태.
        // 기본적으로 유저의 입력을 기다리지만 프리스핀, 리스핀, 오토스핀등의 상황에선 자동으로 스핀 상태로 변경.
        protected override IEnumerator IdleState()
        {
            if (AutoSpin == true)
            {
                bonusCachedButton.interactable = false;
            }
            else
            {
                if (bonusCachedButton.interactable == false)
                    bonusCachedButton.interactable = true;
            }


            yield return base.IdleState();
        }

        // 스핀을 시작하는 스테이트.
        protected override IEnumerator SpinStartState()
        {
            isAutoSpin = AutoSpin;
            bonusCachedButton.interactable = false;

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
        // 현재 프리 스핀일 때, 와일드 심볼 변환 연출 추가
        protected override IEnumerator SpinEndState()
        {
            if (CurrentReelMode == ReelMode.Free)
            {
                yield return ChangeWildSymbol();
            }
            yield return base.SpinEndState();
        }

        // 스핀이 멈추고 결과를 시작 되는 스테이트
        // 잭팟, 빅윈 등에 분기점
        protected override IEnumerator ResultStartState()
        {
            // 보너스 등장 ( 결과와 함께 등장 )
            if (Info.GetExtrasCount() >= EXTRAS_INDEX_BNS_COUNT)
            {
                long bonusCount = Info.GetExtraValue(EXTRAS_INDEX_BNS_COUNT);

                if (bonusCount != 0)
                {
                    StartCoroutine(BonusAppearState());
                }
            }
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
            yield return base.HitState();
        }

        protected override IEnumerator HitNormal(WinGrade winGrade, long winCoins, long totalWinCoins, bool hitSymbols = true)
        {
            return base.HitNormal(winGrade, winCoins, totalWinCoins, hitSymbols);
        }

        // 일반 히트 스테이트
        protected override IEnumerator HitNormalState()
        {
            yield return base.HitNormalState();
        }

        // 빅윈 히트 스테이트
        protected override IEnumerator HitBigwinState()
        {
            yield return base.HitBigwinState();
        }

        // 잭팟 히트 스테이트
        protected override IEnumerator HitJackpotState()
        {
            yield return base.HitJackpotState();
        }

        // 스캐터 히트 스테이트
        protected override IEnumerator ScatterState()
        {
            yield return base.ScatterState();
        }

        // 프리스핀을 시작하는 스테이트
        // 프리스핀 팝업을 보여줌
        protected override IEnumerator FreespinStartState()
        {
            yield return base.FreespinStartState();
            ChangeEnvReelMode(ReelMode.Free);
        }

        protected override IEnumerator OpenFreespinPopup()
        {
            freeBGM.Play();
            yield return PopupSystem.Instance.Open<FreeSpinPopup1017>(slotAssets, "FreeSpinPopup")
                                                .OnInitialize(p => p.Initialize(Info.FreespinTotalCount, holdType != HoldType.On, useVibrate))
                                                .Cache()
                                                .SetBackgroundAlpha(0.8f)
                                                .WaitForClose();
        }

        // 프리스핀 종료 스테이트
        // 프리스핀 종료 팝업을 보여줌
        protected override IEnumerator FreespinEndState()
        {
            yield return base.FreespinEndState();
            ChangeEnvReelMode(ReelMode.Regular);
            RemoveAllSubstituteVisuals();
        }

        // 엑스트라 스핀 스테이트
        // 엑스트라 스핀 팝업을 보여줌
        protected override IEnumerator ExtraspinState()
        {
            yield return base.ExtraspinState();
        }

        // 초이스 시작 스테이트
        protected override IEnumerator ChoiceStartState()
        {
            yield return base.ChoiceStartState();
        }

        // 초이스 종료 스테이트
        protected override IEnumerator ChoiceEndState()
        {
            yield return base.ChoiceEndState();
        }

        #region 와일드 심볼 연출

        //---------------------------------------------------
        // ETC1 = 긴 와일드 심볼 히트
        // ETC2 = 와일드 심볼 등장 ( 프리 모드 )
        // ETC3 = 긴 와일드 심볼 Idle
        // ETC4 = Empty
        //---------------------------------------------------

        // 프리모드일 때 와일드심볼 등장시 비주얼 타입 변경( Etc2 )
        private IEnumerator ChangeWildSymbol()
        {
            freeModeWildSymbolList.Clear();
            includeWildReelIndexList.Clear();

            bool isWild = false;

            for (int reelIndex = 0; reelIndex < ReelGroup.Column; reelIndex++)
            {
                var reel = ReelGroup.GetReel(reelIndex);

                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);
                    if (symbol.IsWild == true)
                    {
                        symbol.SetVisual(SymbolVisualType.Etc2);
                        isWild = true;
                        includeWildReelIndexList.Add(reelIndex);

                        wildAppearSound.Play();
                        break;
                    }
                }
            }

            if (isWild == true)
            {
                yield return new WaitForSeconds(wildSymbolAppearTimeInFree);
                yield return AddWildSymbolSetting();
            }
            else
            {
                yield return null;
            }
        }

        // 프리모드에서 와일드 심볼 등장시 같은 릴의 모든 심볼 와일드로 변경
        // 대체 비주얼 추가
        private IEnumerator AddWildSymbolSetting()
        {
            yield return StartCoroutine(WildEffectGetPool());

            for (int reelIndex = 0; reelIndex < includeWildReelIndexList.Count; reelIndex++)
            {
                int wildReelIndex = includeWildReelIndexList[reelIndex];

                var reel = ReelGroup.GetReel(wildReelIndex);
                int targetIndex = reel.MainSymbolsLength - 1;
                // 릴 심볼 변경
                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    reel.ChangeMainSymbol(mainSymbolIndex, wildSymbolTargetID);
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);

                    // 해당 릴의 와일드 심볼을 Empty
                    if (mainSymbolIndex != targetIndex)
                    {
                        freeModeWildSymbolList.Add(symbol);
                        symbol.SetVisual(SymbolVisualType.Etc4);
                    }
                }

                var firstSymbol = reel.GetMainSymbol(targetIndex);
                firstSymbol.SetVisual(SymbolVisualType.Etc3);

                // 대체 비주얼 추가
                firstSymbol.AddSubstituteVisual(SymbolVisualType.Hit, 0, SymbolVisualType.Etc1, 0);
                firstSymbol.AddSubstituteVisual(SymbolVisualType.Idle, 0, SymbolVisualType.Etc3, 0);
                firstSymbol.AddSubstituteVisual(SymbolVisualType.Spin, 0, SymbolVisualType.Etc3, 0);

                AddWildVisualDelegate(firstSymbol, wildReelIndex);
            }

            yield return new WaitForSeconds(wildEndTime);

            StartCoroutine(RemoveWildSymbolVisual());
        }

        // 비주얼 델리게이트 추가
        private void AddWildVisualDelegate(Symbol targetSymbol, int reelIndex)
        {
            var reel = ReelGroup.GetReel(reelIndex);
            int targetIndex = reel.MainSymbolsLength - 1;

            for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
            {
                if (mainSymbolIndex != targetIndex)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);
                    symbol.SetVisualDelegate(targetSymbol);
                }
            }
        }

        // 와일드 장심볼 이펙트 연출부
        private IEnumerator WildEffectGetPool()
        {
            for (int reelIndex = 0; reelIndex < includeWildReelIndexList.Count; reelIndex++)
            {
                int wildReelIndex = includeWildReelIndexList[reelIndex];
                var reel = ReelGroup.GetReel(wildReelIndex);

                int targetIndex = reel.MainSymbolsLength - 1;
                var firstSymbolPos = reel.GetMainSymbol(targetIndex).transform.position;

                var wildEffect = wildEffectOP.Get();

                wildEffect.Play(firstSymbolPos, ReturnWildEffectPool);
            }

            wildFrameSound.Play();

            yield return new WaitForSeconds(wildChangeTime);

            wildDropSound.Play();
        }

        private void ReturnWildEffectPool(WildEffect1017 wildObj, Transform particlePos)
        {
            wildEffectOP.Return(wildObj);
            StartCoroutine(PlayWildParticle(particlePos.position));
            Shake();
        }

        private IEnumerator PlayWildParticle(Vector3 pos)
        {
            var wildParticle = wildParticleOP.Get();
            wildParticle.transform.position = pos;

            yield return new WaitForSeconds(1.0f);

            wildParticleOP.Return(wildParticle);
        }

        // 장심볼이 내려올 때 흔들리는 연출
        private void Shake()
        {
            ReelGroup.transform.DOKill(true);
            ReelGroup.transform.DOShakePosition(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness).SetEase(Ease.OutQuint);

            for (int i = 0; i < bgObjectlist.Length; i++)
            {
                var bg = bgObjectlist[i];
                bg.DOKill(true);
                bg.DOShakePosition(shakeBgDuration, shakeBgStrength, shakeBgVibrato, shakeBgRandomness).SetEase(Ease.OutQuint);
            }
        }

        // 대체 비주얼 제거
        private IEnumerator RemoveWildSymbolVisual()
        {
            while (CurrentState != SlotMachineState.SPIN_START)
            {
                yield return null;
            }

            for (int reelIndex = 0; reelIndex < includeWildReelIndexList.Count; reelIndex++)
            {
                int wildReelIndex = includeWildReelIndexList[reelIndex];
                var reel = ReelGroup.GetReel(wildReelIndex);
                int targetIndex = reel.MainSymbolsLength - 1;

                var targetSymbol = reel.GetMainSymbol(targetIndex);
                targetSymbol.RemoveSubstituteVisual(SymbolVisualType.Hit, 0);
            }

            yield return new WaitForSeconds(0.2f);

            for (int i = 0; i < freeModeWildSymbolList.Count; i++)
            {
                freeModeWildSymbolList[i].ClearVisualDelegate();
                freeModeWildSymbolList[i].SetVisual(SymbolVisualType.Idle);
            }
        }

        private void RemoveAllSubstituteVisuals()
        {
            for (int reelIndex = 0; reelIndex < ReelGroup.Column; reelIndex++)
            {
                var reel = ReelGroup.GetReel(reelIndex);

                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);
                    symbol.RemoveAllSubstituteVisuals();
                }
            }

            for (int i = 0; i < freeModeWildSymbolList.Count; i++)
            {
                freeModeWildSymbolList[i].ClearVisualDelegate();
                freeModeWildSymbolList[i].SetVisual(SymbolVisualType.Idle);
            }

            reelGroupHelper.SetVisualAllSymbols(SymbolVisualType.Idle);
        }


        #endregion


        private void UpdateBonusPointSlider(long points, float animationTime = 1.0f)
        {
            if (myBonusCount < bonusGaugeStart)
            {
                myBonusCount = bonusGaugeStart;
            }

            myBonusCount += points;

            int id = bonusSlider.GetInstanceID();
            DOTween.Kill(id, false);

            // Max Count 도달시 맵 피쳐 등장                  
            if (IsBonusCountMax() == true)
            {
                myBonusCount = (int)bonusSlider.maxValue;
            }

            bonusSlider.DOValue(myBonusCount, animationTime).SetId(id);

            isDoneUpdateSlider = true;
        }

        private IEnumerator BonusMaxState()
        {
            Debug.Log("BonusMaxState");

            StopRepeatWinningLines();
            paylinesHelper.Clear();

            if (isDoneUpdateSlider == false)
            {
                yield return null;
            }

            int id = bonusSlider.GetInstanceID();

            yield return new WaitForSeconds(1.0f);

            bonusGaugePullSound.Play();
            bonusFullEffectAnimator.SetTrigger("GaugeFull");

            yield return new WaitForSeconds(bonusFullEffectTime);

            intro.SetActive(true);
            vikingSigSound.Play();
            introAnimation.state.SetAnimation(0, "gauge", false);
            yield return new WaitForSeconds(bonusIntroTime);

            intro.SetActive(false);
            // 맵 피쳐 스테이트 진입
            NextState(STATE_MAP_FEATURE);

            // 게이지 초기화
            myBonusCount = 0;

            bonusSlider.DOValue(myBonusCount, 1.0f).SetId(id);
        }

        private bool IsBonusCountMax()
        {
            int bonusCount = GetExtraValueIndex(EXTRAS_INDEX_BNS_COUNT);
            int totalcount = GetExtraValueIndex(EXTRAS_INDEX_BNS_TOTAL_COUNT);

            // Max Count 도달시 EXTRAS_INDEX_BNS_TOTAL_COUNT = 0
            if (totalcount == 0 && bonusCount > 0 && myBonusCount > 0)
            {
                return true;
            }
            return false;
        }

        // STATE_MAP_FEATURE
        private IEnumerator MapFeatureState()
        {
            mapFeaturePopup.AwakeFeature(holdType);

            int stageIndex = GetNextStageIndex();

            long winCoins = Info.SpinResult.WinCoinsFeature;
            long[] normalPickOthers = new long[2]
            {
                 GetExtraValueCoin(EXTRAS_INDEX_BNS_NORMAL_PICK1),
                 GetExtraValueCoin(EXTRAS_INDEX_BNS_NORMAL_PICK2)
            };

            int pickBonusIndex = GetExtraValueIndex(EXTRAS_INDEX_BNS_PICK_INDEX);
            long[] gradeCoins = new long[4]
            {
               GetExtraValueCoin(EXTRAS_INDEX_COIN_GRAND),
               GetExtraValueCoin(EXTRAS_INDEX_COIN_MEGA),
               GetExtraValueCoin(EXTRAS_INDEX_COIN_MAJOR),
               GetExtraValueCoin(EXTRAS_INDEX_COIN_MINOR)
            };

            yield return mapFeaturePopup.OpenMapFeature(stageIndex, winCoins, normalPickOthers, pickBonusIndex, gradeCoins);

            yield return StartCoroutine(ResultMapFeature());
        }

        // 맵 피쳐 결과
        private IEnumerator ResultMapFeature()
        {
            if (normalBGM.IsInPlay() == false)
            {
                normalBGM.Play();
            }

            // 일반 보너스
            long winCoins = Info.SpinResult.WinCoinsFeature;

            var winGrade = SlotMachineUtils.GetWinGrade(winCoins, Info.CurrentTotalBet);

            if (IsBigWin(winCoins))
            {
                yield return HitBigwin(winGrade, winCoins, winCoins, false);
            }
            else
            {
                yield return HitNormal(winGrade, winCoins, winCoins, false);
            }
            //-----------------------------------------------------------

            if (isAutoSpin == true)
            {
                AutoSpin = true;
            }

            //EarnCoins(winCoins);

            //-----------------------------------------------------------
            string state = base.GetStateBeforeEnd(Info.IsExtraSpin, Info.IsFreespinStart, Info.IsFreespinEnd, Info.IsRespin, Info.IsFreeChoiceStart);
            NextState(state);
        }

        // 버튼 클릭시 맵피쳐 오픈
        public void OnClickedBtn_MapFeature()
        {
            int stageIndex = GetExtraValueIndex(EXTRAS_INDEX_STAGE);

#if UNITY_EDITOR
            if (isTestModeOn == true)
            {
                StartCoroutine(TestMapFeature());
            }
            else
#endif
            {
                mapFeaturePopup.OpenMapFeature(stageIndex);
            }
        }

#if UNITY_EDITOR
        private IEnumerator TestMapFeature()
        {
            long[] testOthers = new long[2] { 9999, 9999, };
            long[] testGrade = new long[4] { 9999, 9999, 9999, 9999 };
            yield return mapFeaturePopup.OpenMapFeature(stageIndex_test, 0, testOthers, 0, testGrade);
            yield return ResultMapFeature();
        }
#endif

        // 노말, 프리 배경 애니메이션 트리거
        private void ChangeEnvReelMode(ReelMode mode)
        {
            if (mode == ReelMode.Free)
            {
                bonusButton.SetActive(false);

                backgroundAnimator.ResetTrigger("Off");
                backgroundAnimator.SetTrigger("On");
                bonusFullEffectAnimator.gameObject.SetActive(false);
                bonusFreeMode.SetActive(true);
                if (freeBGM.IsInPlay() == false)
                    freeBGM.Play();
            }
            else
            {
                bonusButton.SetActive(true);

                backgroundAnimator.SetTrigger("Off");
                bonusFullEffectAnimator.gameObject.SetActive(true);
                bonusFreeMode.SetActive(false);
            }
        }

        private int GetExtraValueIndex(int index)
        {
            if (Info.GetExtrasCount() <= index)
            {
                Debug.Log("[slot1017] GetExtraValue index overflow : " + index);
                return 0;
            }

            return (int)Info.GetExtraValue(index);
        }

        private long GetExtraValueCoin(int index)
        {
            if (Info.GetExtrasCount() <= index)
            {
                return 999999;
            }
            return Info.GetExtraValue(index);
        }

        private int GetNextStageIndex()
        {
            // EXTRAS_INDEX_STAGE :: BNS 갯수가 120이 넘어가면 다음 인덱스로 넘어간다 (0-19) -> (1-19, 0)
            int stageIndex = GetExtraValueIndex(EXTRAS_INDEX_STAGE);

            if (stageIndex == 0)
            {
                stageIndex = MAX_STAGE_INDEX;
            }
            else
            {
                stageIndex -= 1;
            }
            return stageIndex;
        }


        public void OnReelEffectSymbolCanceled(ReelEffect.TriggeredInfo[] info)
        {
            for (int i = 0; i < info.Length; i++)
            {
                int column = info[i].symbolInfo.column;
                int row = info[i].symbolInfo.row;

                var symbol = ReelGroup.GetReel(column).GetMainSymbol(row);
                if (symbol.IsScatter)
                {
                    symbol.AddSubstituteVisual(SymbolVisualType.Idle, 0, SymbolVisualType.Etc2, 0);

                    StartCoroutine(ReelEffectSymbolRemoveSubstituteVisuel(symbol));
                }
            }
        }

        private IEnumerator ReelEffectSymbolRemoveSubstituteVisuel(Symbol symbol)
        {
            while (CurrentState != SlotMachineState.SPIN_START)
            {
                yield return null;
            }

            symbol.RemoveSubstituteVisual(SymbolVisualType.Idle, 0);
        }
    }
}