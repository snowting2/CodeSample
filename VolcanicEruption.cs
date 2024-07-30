using UnityEngine;
using System;
using SlotGame.Attribute;

namespace SlotGame.Machine.S1078
{
    public class LinkSymbolManager1078 : MonoBehaviour
    {
        public enum LinkSymbolType
        {
            CREDIT = 0,
            Jackpot = 1,
        }

        public enum JackpotValueType
        {
            grand = -1,
            major = -2,
            minor = -3,
            mini = -4,
        }

#pragma warning disable 0649
        [SerializeField] private LinkSymbolInfo[] linkSymbolInfoList;

        [SerializeField] private ReelGroup[] reelGroups; // normal : 0, link : 1
#pragma warning restore 0649

        private DirectPayData directPayData = null;
        private ExtraInfo1078 extraInfo = null;
        private SlotMachineInfo slotMachineInfo = null;

        private int currentReelIndex = 0;
        private ReelGroup currentReelgroup = null;

        private readonly int NORMAL_REEL_INDEX = 0;
        private readonly int LINK_REEL_INDEX = 1;

        public void Initialize(DirectPayData directPayData, ExtraInfo1078 extraInfo, SlotMachineInfo info)
        {
            if (this.extraInfo == null)
            {
                this.extraInfo = extraInfo;
            }

            if (slotMachineInfo == null)
            {
                slotMachineInfo = info;
            }

            this.directPayData = directPayData;

            // 링크심볼 추가 시 이벤트 등록
            for (int featureIndex = 0; featureIndex < reelGroups.Length; featureIndex++)
            {
                var reelGroup = reelGroups[featureIndex];
                if (reelGroup != null)
                {
                    for (int i = 0; i < reelGroup.GetCount(); i++)
                    {
                        var reelIndex = i;
                        var reel = reelGroup.GetReel(reelIndex);

                        reel.OnSymbolAdded += (symbol, reelStripIndex) =>
                        {
                            OnSymbolAdded(reelIndex, reel, symbol, reelStripIndex);
                        };
                    }
                }
            }

            UpdateReelGroup();
        }

        public void UpdateReelGroup()
        {
            currentReelIndex = (slotMachineInfo.IsLinkRespin) ? LINK_REEL_INDEX : NORMAL_REEL_INDEX;
            currentReelgroup = reelGroups[currentReelIndex];
        }

        public void OnSymbolAdded(int reelIndex, BaseReel reel, Symbol symbol, int reelStripIndex)
        {
            if (reel.CurrentState == ReelState.Slowdown)
            {
                var info = GetLinkSymbolInfo(symbol.id);

                if (info != null)
                {
                    SetLinkSymbol(symbol, reelIndex, true);
                }
                else
                {
                    SetLinkSymbol(symbol, reelIndex, false);
                }
            }
            else
            {
                SetLinkSymbol(symbol, reelIndex, false);
            }
        }
        public LinkSymbolInfo GetLinkSymbolInfo(string id)
        {
            foreach (var info in linkSymbolInfoList)
            {
                if (info.ID == id)
                {
                    return info;
                }
            }

            return null;
        }

        public LinkSymbolInfo GetLinkSymbolInfo(long resultValue)
        {
            if (resultValue > 0)
            {
                foreach (var info in linkSymbolInfoList)
                {
                    if (info.GetCredit(resultValue) == true)
                    {
                        return info;
                    }
                }
            }
            else
            {
                foreach (var info in linkSymbolInfoList)
                {
                    if ((int)info.JackpotType == resultValue)
                    {
                        return info;
                    }
                }
            }

            return null;
        }

        private void SetLinkSymbol(Symbol symbol, int reelIndex, bool useRespinInfo)
        {
            if (symbol.IsLink == false) return;

            var linkSymbol = symbol.GetComponent<LinkSymbol1078>();
            if (linkSymbol == null) return;

            string linkValue = GetValue(symbol.id);

            if (currentReelIndex == LINK_REEL_INDEX)
            {
                linkSymbol.HideAll();
            }
            else
            {
                if (useRespinInfo)
                {
                    var respinInfo = extraInfo.GetRespinInfo(reelIndex);
                    if (respinInfo != null && respinInfo.PayValue != 0)
                    {
                        linkValue = respinInfo.CreditValue.ToString();
                    }
                }

                //Debug.LogFormat("SetLinkSymbol : id {0} value {2} - reel {1} " , symbol.id, reelIndex, linkValue);
                if (symbol.IsJackpot == false)
                {
                    linkSymbol.SetValue(linkValue);
                }
            }
        }

        public string GetValue(string id)
        {
            var info = GetLinkSymbolInfo(id);
            if (info.IsjackpotType())
            {
                return info.JackpotType.ToString();
            }
            else
            {
                return info.CREDIT.ToString();
            }
        }

        public void TotalBetChangeUpdateInfo(long? totalBet)
        {
            for (int i = 0; i < linkSymbolInfoList.Length; i++)
            {
                linkSymbolInfoList[i].SetDirectPay(directPayData, (long)totalBet);
            }

            // Debug.LogFormat("** DirectPayUpdate = {0}", linkSymbolInfoList.ToEachString("\n"));

            if (currentReelIndex == NORMAL_REEL_INDEX)
            {
                // 기존 심볼 업데이트
                for (int reelIndex = 0; reelIndex < currentReelgroup.Column; reelIndex++)
                {
                    var reel = currentReelgroup.GetReel(reelIndex);
                    for (int mainSymbolIndex = 0; mainSymbolIndex < reel.MainSymbolsLength; mainSymbolIndex++)
                    {
                        var symbol = reel.GetMainSymbol(mainSymbolIndex);

                        SetLinkSymbol(symbol, reelIndex, false);
                    }
                }
            }
        }

        [Serializable]
        public sealed class LinkSymbolInfo
        {
#pragma warning disable 0649
            [SerializeField] private string id;
            [SerializeField] private LinkSymbolType type;
#pragma warning restore 0649
            [SerializeField, ConditionalHide("type", 1, true)] private JackpotValueType jackpotType;

            public string ID { get { return id; } }
            public int IntID { get { return int.Parse(id); } }
            public LinkSymbolType SymbolType { get { return type; } }
            //public long DP { get; private set; }
            public int CREDIT { get; private set; }
            public JackpotValueType JackpotType { get { return jackpotType; } }

            private int dpIndex = 0;
            public int DP_INDEX { get { return dpIndex; } }

            public override string ToString()
            {
                return string.Format("ID {0}, Credit {1}", id, CREDIT);
            }

            public long GetRandomDirectPay(DirectPayData data, long totalBet)
            {
                int randomIndex = UnityEngine.Random.Range(0, 9);
                if (data.syms[randomIndex] >= 21 && data.syms[randomIndex] < 28)
                {
                    return data.pays[randomIndex];
                }

                return CREDIT;
            }
            public void SetDirectPay(DirectPayData data, long totalBet)
            {
                if (type == LinkSymbolType.Jackpot)
                {
                    CREDIT = (int)jackpotType;
                    return;
                }

                int payIndex = -1;

                for (int i = 0; i < data.syms.Length; i++)
                {
                    if (data.syms[i] == IntID)
                    {
                        payIndex = i;
                        break;
                    }
                }

                if (payIndex == -1)
                {
                    Debug.Log(" -- Not Found DirectPayData == " + IntID);
                    return;
                }

                CREDIT = (int)data.pays[payIndex];
            }

            public bool GetCredit(long resultValue)
            {
                if (resultValue == CREDIT)
                {
                    return true;
                }
                return false;
            }

            public bool IsCreditType()
            {
                return (type == LinkSymbolType.CREDIT);
            }

            public bool IsjackpotType()
            {
                return (type == LinkSymbolType.Jackpot);
            }
        }
    }
}