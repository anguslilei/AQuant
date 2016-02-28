using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeAPI;
using QuoteAPI;
using System.Threading.Timer;
namespace AQuant
{
    public class AContract
    {
        Trade t;
        bool AllowTrade;//交易锁，true可交易
        private int _Shift;
        private Dictionary<int, OrderField> dicOrder;
        private int _orderId;
        //接收行情
        public delegate void DelOnRtnTick(object sender, TickEventArgs e);
        public DelOnRtnTick OnRtnTick;

        //报单响应，每次成交
        public delegate void DelOnRtnOrder(object sender, OrderArgs e);
        public DelOnRtnOrder OnRtnOrder;

        //成交通知
        public delegate void DelOnRtnTrade(object sender, TradeArgs e);
        public DelOnRtnTrade OnRtnTrade;

        //撤单通知
        public delegate void DelOnRtnCancel(object sender, OrderArgs e);
        public DelOnRtnCancel OnRtnCancel;



        public List<MarketData> Ticks
        {
            private set;
            get;
        }
        private int TicksLen = 60;
        public InstrumentField Instrument;

        public AContract(Trade t)
        {
            this.t = t;
            AllowTrade = true;
            dicOrder = new Dictionary<int, OrderField>();
            Ticks = new List<MarketData>();
            OnRtnTick += DefOnRtnTick;
            OnRtnOrder += DefOnRtnOrder;
            OnRtnTrade += DefOnRtnTrade;
            OnRtnCancel += DefOnRtnCancel;
        }
        public int Shift
        {
            set
            {
                Console.WriteLine(AllowTrade);
                if (AllowTrade)
                {
                    _orderId = -1;
                    if ((value > 0) && (value > _Shift))
                    {
                        AllowTrade = false;
                        _orderId = InputOrder(DirectionType.Buy, OffsetType.Open, Ticks[0].LastPrice, value - _Shift);
                    }
                    if ((value < 0) && (value < _Shift))
                    {
                        AllowTrade = false;
                        _orderId = InputOrder(DirectionType.Sell, OffsetType.Open, Ticks[0].LastPrice, _Shift - value);
                    }
                    if ((value == 0) && (_Shift > 0))
                    {
                        AllowTrade = false;
                        _orderId = InputOrder(DirectionType.Sell, OffsetType.Close, Ticks[0].LastPrice, _Shift);
                    }
                    if ((value == 0) && (_Shift < 0))
                    {
                        AllowTrade = false;
                        _orderId = InputOrder(DirectionType.Buy, OffsetType.Close, Ticks[0].LastPrice, -_Shift);
                    }
                    if (_orderId != -1)
                    {
                        this._Shift = value;
                    }

                }
            }
            get
            {
                return this._Shift;
            }
        }
        private void DefOnRtnTick(object sender, TickEventArgs e)//存储最近一段时间行情
        {
            Ticks.Insert(0, e.Tick);
            if (Ticks.Count > TicksLen)
            {
                Ticks.RemoveAt(TicksLen);
            }

        }
        private void DefOnRtnTrade(object sender, TradeArgs e)
        {

        }

        private void DefOnRtnOrder(object sender, OrderArgs e)
        {
            Console.WriteLine("收到报单回报:" + e.Value.OrderID + "Status=" + e.Value.Status);
            System.Threading.Timer timer = new System.Threading.Timer(new System.Threading.TimerCallback(CancelOrder), e.Value.OrderID, 3000, System.Threading.Timeout.Infinite);

            if (e.Value.Status == OrderStatus.Normal)
            {
                dicOrder[e.Value.OrderID] = e.Value;
            }

            if (dicOrder.ContainsKey(e.Value.OrderID) && (e.Value.Status == OrderStatus.Filled))//全部成交
            {
                Console.WriteLine("订单" + e.Value.OrderID + "全部成交");
                AllowTrade = true;
            }
        }

        private void DefOnRtnCancel(object sender, OrderArgs e)
        {
            if (e.Value.Status == OrderStatus.Canceled)//被撤单
            {
                AllowTrade = true;
            }
        }
        public int InputOrder(DirectionType pDirection, OffsetType pOffset, double pPrice, int pVolume)
        {
            Console.WriteLine(Ticks[0].UpdateTime + " " + Instrument.InstrumentID + "以价格" + pPrice.ToString() + "报单" + pDirection + " " + pVolume.ToString() + "手");
            return t.ReqOrderInsert(Instrument.InstrumentID, pDirection, pOffset, pPrice, pVolume);

        }
        private void CancelOrder(object state)
        {

        }

    }
}
