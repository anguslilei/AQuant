using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using TradeAPI;
using QuoteAPI;
using AQuant;
namespace ArbitrageSample
{
    class ArbStrategy : AStrategy
    {
        //定义策略内部参数

        private int MaPeriod;//均线周期

        private List<double> PriceList;//价差列表
        double OldMa;
        double OldBuy;
        double OldSell;
        double OldClose;
        bool OpenBuy;//开多
        bool OpenSell;//开空
        bool CloseSell;//平空
        bool CloseBuy;//平多
        public ArbStrategy()
        {
            MaPeriod = 60;
            PriceList = new List<double>();//价差列表
            OldMa = 0;
            OldBuy = 0;
            OldSell = 0;
            OldClose = 0;
            OpenBuy = false;//开多
            OpenSell = false;//开空
            CloseBuy = false;
            CloseSell = false;
        }
        public override void Init()
        {

            Thread.Sleep(5000);
            AddContract("IC1603");
            AddContract("IC1604");
            Contracts[0].OnRtnTick += OnTick;
            Contracts[1].OnRtnTick += OnTick;
            Console.ReadKey();

        }
        public void OnTick(object sender, TickEventArgs e)
        {
            Console.WriteLine(e.Tick.ToString());
            if (this.Contracts[0].Ticks.Count > 0 && this.Contracts[1].Ticks.Count > 0)
            {
                double PriceDiff = 0;//开仓价差
                double BuySpread = 0;//做多价差
                double SellSpread = 0;//做空价差
                double CloseSpread = 0;//最新价差
                double MaPrice = 0;
                PriceDiff = 5 * this.Contracts[0].Instrument.PriceTick;//取第一腿最小变动价位计算价差
                BuySpread = this.Contracts[0].Ticks[0].AskPrice - this.Contracts[1].Ticks[0].BidPrice;
                SellSpread = this.Contracts[0].Ticks[0].BidPrice - this.Contracts[1].Ticks[0].AskPrice;
                CloseSpread = this.Contracts[0].Ticks[0].LastPrice - this.Contracts[1].Ticks[0].LastPrice;
                // Console.WriteLine("closeSpread:{0}-{1}={2}", this.Contracts[0].Ticks[0].LastPrice, this.Contracts[1].Ticks[0].LastPrice, CloseSpread);
                PriceList.Add(CloseSpread); //记录均线价差


                if (PriceList.Count > this.MaPeriod)
                {
                    double SumPrice = 0;
                    MaPrice = 0;
                    //计算均值
                    for (int i = PriceList.Count - 2; i >= PriceList.Count - this.MaPeriod - 1; i--)
                    {
                        SumPrice = SumPrice + this.PriceList[i];
                    }
                    MaPrice = SumPrice / this.MaPeriod;
                    if (OldMa > 0)
                    {


                        if ((OldSell > MaPrice - PriceDiff) && (OldClose < OldMa - PriceDiff) && !CloseBuy)
                        {
                            this.OpenBuy = true;
                        }
                        if ((OldBuy < MaPrice + PriceDiff) && (OldClose > OldMa + PriceDiff) && !CloseSell)
                        {
                            this.OpenSell = true;
                        }

                        //做多价差
                        if (this.OpenBuy & !CloseSell)
                        {
                            this.OpenBuy = false;
                            Console.WriteLine("做多价差");
                            this.Contracts[0].Shift = 0;
                            this.Contracts[1].Shift = -1;
                            CloseBuy = true;
                        }
                        //做空价差
                        if (this.OpenSell & !CloseBuy)
                        {
                            this.OpenSell = false;
                            Console.WriteLine("做空价差");
                            this.Contracts[0].Shift = 0;
                            this.Contracts[1].Shift = 1;
                            CloseSell = true;
                        }
                        //平空仓
                        if (CloseSpread < MaPrice && CloseSell)
                        {
                            Console.WriteLine("平空仓");
                            this.Contracts[0].Shift = 0;
                            this.Contracts[1].Shift = 0;
                            CloseSell = false;
                            OpenSell = true;
                        }
                        //平多仓
                        if (CloseSpread > MaPrice && CloseBuy)
                        {
                            Console.WriteLine("平多仓");
                            this.Contracts[0].Shift = 0;
                            this.Contracts[1].Shift = 0;
                            CloseBuy = false;
                            OpenBuy = true;
                        }

                    }
                    PriceList.RemoveAt(0);
                }
                //  OldDiff = PriceDiff;
                OldMa = MaPrice;
                OldBuy = BuySpread;
                OldSell = SellSpread;
                OldClose = CloseSpread;
            }
        }
    }
}
