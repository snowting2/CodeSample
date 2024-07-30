using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Sound;

namespace SlotGame.Machine.S1064
{
    public class Feature1064 : MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField] protected ReelEffect reelEffect;
        [SerializeField] protected GameObject spinPanel;
        [SerializeField] protected ReelGroup reelGroup;
        [SerializeField] protected SoundPlayer bgmSound;

        [SerializeField] protected float appearTime;

        [SerializeField] protected GameObject machineAnim;

        [Space]
        [SerializeField] protected SoundPlayer reelEffectSound;
#pragma warning restore 0649

        protected readonly int JUDGE_REEL_INDEX = 2;

        protected SlotMachine1064 slotMachine = null;
        protected ExtraInfo1064 extraInfo = null;
        protected SlotMachineInfo slotMachineInfo = null;
        protected readonly string WILD_SYMBOL_ID = "12";

        public bool IsInitialize { get; protected set; }
        protected float reelEffectDuration = 0.0f;

        protected readonly SymbolVisualType appearVisual = SymbolVisualType.Etc2;
        protected readonly SymbolVisualType loopVisual = SymbolVisualType.Etc1;

        protected virtual void OnSlotMachineAwake()
        {
            if (reelEffect != null)
            {
                reelEffectDuration = reelEffect.GetPreset(0).duration;
            }

            SetSpinPanel(false);
        }

        public virtual void Initialize(List<string> startSymsList, ExtraInfo1064 extraInfo, SlotMachine1064 slotMachine, SlotMachineInfo slotMachineInfo)
        {
            if (this.slotMachine == null)
            {
                this.slotMachine = slotMachine;
            }

            if (this.slotMachineInfo == null)
            {
                this.slotMachineInfo = slotMachineInfo;
            }

            if (this.extraInfo == null)
            {
                this.extraInfo = extraInfo;
            }

            if (reelGroup != null)
            {
                reelGroup.OnStateEnd.AddListener(OnReelStateEnd);
            }

            machineAnim.SetActive(false);

            SetSpinPanel(true);

            StartBGM();
        }

        public virtual void Release()
        {
            if (reelGroup != null)
            {
                reelGroup.OnStateEnd.RemoveListener(OnReelStateEnd);
            }

            StopBGM();
        }

        // Connected Transition Timeline Signal
        public virtual void FeatureDisable()
        {
            this.gameObject.SetActive(false);
        }

        public virtual void StartBGM()
        {
            bgmSound.Play();
        }

        public virtual void StopBGM()
        {
            bgmSound.Stop();
        }

        public virtual void PauseBGM()
        {
            bgmSound.Pause();
        }

        public virtual void SpinStart()
        {
            if (machineAnim.activeSelf)
            {
                machineAnim.SetActive(false);
            }
        }

        public virtual IEnumerator SpinEnd()
        {
            yield return null;
        }

        public virtual void OnEnd()
        {
            SetSpinPanel(false);
            machineAnim.SetActive(false);
        }

        protected virtual void SetSpinPanel(bool active)
        {
            if (spinPanel != null)
            {
                spinPanel.SetActive(active);
            }
        }

        protected virtual void OnReelStateEnd(int reelIndex, ReelState state, BaseReel reel)
        {
            if (state == ReelState.Slowdown)
            {
                var triggeredSymbols = reelEffect.GetTriggeredSymbols(reelIndex);
                if (triggeredSymbols.Count == 0)
                {
                    for (int i = 0; i < reelIndex; i++)
                    {
                        var prevReel = reelGroup.GetReel(i);
                        for (int mainSymbolIndex = 0; mainSymbolIndex < prevReel.MainSymbolsLength; mainSymbolIndex++)
                        {
                            var symbol = prevReel.GetMainSymbol(mainSymbolIndex);

                            if (symbol.IsScatter || symbol.IsWild)
                            {
                                if (IsWildVisualGive(symbol))
                                {
                                    continue;
                                }

                                symbol.SetVisual(SymbolVisualType.Idle);
                            }
                        }
                    }
                }

                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    OnWildPotTrail(reelIndex, mainSymbolIndex);
                }
            }
        }

        protected virtual void OnWildPotTrail(int reelIndex, int mainSymbolIndex)
        {
            var reel = reelGroup.GetReel(reelIndex);
            var symbol = reel.GetMainSymbol(mainSymbolIndex);

            if (symbol.IsWild && IsValidUnlockRow(reelIndex, mainSymbolIndex))
            {
                Debug.LogFormat("**OnWildPotTrail reel {0}, main {1}", reelIndex, mainSymbolIndex);

                bool lastWildReel = (GetLastWildReelIndex() == reelIndex);

                if (IsWildVisualMatch(symbol) == false)
                {
                    symbol.SetVisual(appearVisual, 1);
                }

                slotMachine.OnPotTrail(symbol, lastWildReel, mainSymbolIndex);
            }
        }

        protected virtual int GetLastWildReelIndex()
        {
            var symsList = SlotMachineUtils.ConvertSymsToList(slotMachineInfo.GetSyms(), reelGroup.Column, reelGroup.Row);

            for (int i = symsList.Count - 1; i >= 0; i--)
            {
                symsList[i].Reverse();
                var syms = symsList[i];
                for (int mainSymbolIndex = 0; mainSymbolIndex < syms.Count; mainSymbolIndex++)
                {
                    if (IsValidUnlockRow(i, mainSymbolIndex) == false) continue;

                    if (syms[mainSymbolIndex] == WILD_SYMBOL_ID)
                    {
                        Debug.LogFormat("** LastWild = reelIndex {0} , mainIndex {1}", i, mainSymbolIndex);
                        return i;
                    }
                }
            }

            return -1;
        }

        private bool IsWildVisualGive(Symbol symbol)
        {
            if (symbol.IsWild)
            {
                var currentVisual = symbol.GetVisual();
                if (currentVisual.visualType == appearVisual && currentVisual.index == 1)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsWildVisualMatch(Symbol symbol)
        {
            if (symbol.IsWild)
            {
                var currentVisual = symbol.GetVisual();
                if (currentVisual.visualType == appearVisual && currentVisual.index == 0)
                {
                    return true;
                }
            }

            return false;
        }

        // Connect Event - ReelEffect Component
        public virtual void OnSymbolAppeared(ReelEffect.TriggeredInfo info)
        {
            if (info.presetIndex == 0)
            {
                int reelIndex = info.symbolInfo.column;
                int mainSymbolIndex = info.symbolInfo.row;

                var reel = reelGroup.GetReel(reelIndex);
                var symbol = reel.GetMainSymbol(mainSymbolIndex);
                if (symbol.IsScatter || symbol.IsWild)
                {
                    StartCoroutine(SetAppearLoop(symbol, mainSymbolIndex, reelIndex));
                }
            }
        }

        public virtual void OnReelEffectAppear(int presetIndex, int reelIndex, BaseReel reel)
        {

        }

        protected virtual IEnumerator SetAppearLoop(Symbol symbol, int mainSymbolIndex, int reelIndex)
        {
            var addOrder = (reelGroup.Row - mainSymbolIndex - 1) + reelIndex;

            symbol.SetAdditionalSortingOrder(addOrder);
            symbol.SetVisual(appearVisual, 0);

            yield return new WaitForSeconds(appearTime);

            symbol.SetVisual(loopVisual);
        }

        public virtual bool IsValidUnlockRow(int reelIndex, int mainSymbolIndex)
        {
            return true;
        }

        public virtual void HitScatterSymbols()
        {
            int prevIndex = -1;

            for (int reelIndex = 0; reelIndex < reelGroup.GetCount(); reelIndex++)
            {
                var reel = reelGroup.GetReel(reelIndex);

                for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                {
                    var symbol = reel.GetMainSymbol(mainSymbolIndex);
                    bool isContinuous = ((reelIndex - prevIndex) == 1);

                    if (symbol.IsScatter && isContinuous)
                    {
                        symbol.SetVisual(SymbolVisualType.Hit, 1);
                        prevIndex = reelIndex;
                    }
                }
            }
        }


        public virtual void OnInitializeReelEffectDuration()
        {
            for (int i = 0; i < reelEffect.presets.Count; i++)
            {
                reelEffect.GetPreset(i).duration = 0.0f;
            }
        }

        public virtual void SetReelEffectDuration()
        {
            for (int i = 0; i < reelEffect.presets.Count; i++)
            {
                reelEffect.GetPreset(i).duration = reelEffectDuration;
            }
        }

        public virtual void OnHitBigWin()
        {
            machineAnim.SetActive(true);
        }
    }
}