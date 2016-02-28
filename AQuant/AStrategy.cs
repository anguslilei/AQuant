using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeAPI;
using QuoteAPI;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
namespace AQuant
{
    public class AStrategy             //策略基类：完成行情、交易登录
    {
        [DllImport("kernel32")]//返回0表示失败，非0为成功
        private static extern long WritePrivateProfileString(string section, string key,
            string val, string filePath);

        [DllImport("kernel32")]//返回取得字符串缓冲区的长度
        private static extern int GetPrivateProfileString(string section, string key,
            string def, StringBuilder retVal, int size, string filePath);
        /// <summary>
        /// 行情
        /// </summary>
        private Quote q;
        /// <summary>
        /// 交易
        /// </summary>  
        private Trade t;           //交易
        /// <summary>
        /// 合约列表
        /// </summary>
        protected List<AContract> Contracts;

        public AStrategy()      //子类会调用基类构造函数，该方法定会被执行
        {
            Contracts = new List<AContract>();
            string iniFile = @"./config.ini";
            try
            {
                Console.WriteLine("正在读取用户配置...");
                StringBuilder DLL = new StringBuilder();
                StringBuilder Server = new StringBuilder();
                StringBuilder Broker = new StringBuilder();
                StringBuilder Investor = new StringBuilder();
                StringBuilder Password = new StringBuilder();
                GetPrivateProfileString("Quote", "DLL", "", DLL, 1024, iniFile);
                GetPrivateProfileString("Quote", "Server", "", Server, 1024, iniFile);
                GetPrivateProfileString("Quote", "Broker", "", Broker, 1024, iniFile);
                q = new Quote(DLL.ToString())
                {
                    Server = Server.ToString(),
                    Broker = Broker.ToString(),
                };

                GetPrivateProfileString("Trade", "DLL", "", DLL, 1024, iniFile);
                GetPrivateProfileString("Trade", "Server", "", Server, 1024, iniFile);
                GetPrivateProfileString("Trade", "Broker", "", Broker, 1024, iniFile);
                t = new Trade(DLL.ToString())
                {
                    Server = Server.ToString(),
                    Broker = Broker.ToString(),
                };

                GetPrivateProfileString("Account", "Investor", "", Investor, 1024, iniFile);
                GetPrivateProfileString("Account", "Password", "", Password, 1024, iniFile);
                t.Investor = q.Investor = Investor.ToString();
                t.Password = q.Password = Password.ToString();
                t.ReqConnect();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("按任意键中止程序");
                Console.ReadKey();
                Environment.Exit(0);
            }


            q.OnFrontConnected += (sender, e) => q.ReqUserLogin();

            q.OnRspUserLogin += (sender, e) =>
            {
                if (e.Value == 0)
                    Console.WriteLine("行情账户登录成功!");
                else
                    Console.WriteLine("行情账户登录失败{0}!", e.Value);
            };
            q.OnRspUserLogout += (sender, e) => Console.WriteLine("OnRspUserLogout:{0}", e.Value);
            q.OnRtnError += (sender, e) => Console.WriteLine("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
            q.OnRtnTick += OnRtnTick;



            t.OnFrontConnected += (sender, e) => t.ReqUserLogin();
            t.OnRspUserLogin += (sender, e) =>
            {
                if (e.Value == 0)
                {
                    Console.WriteLine("交易账户登录成功，即将登录行情...");
                    q.ReqConnect();
                }
                else
                {
                    Console.WriteLine("交易账户登录失败{0}", e.Value);
                }

            };
            t.OnRspUserLogout += (sender, e) => Console.WriteLine("OnRspUserLogout:{0}", e.Value);
            t.OnRtnCancel += OnRtnCancel;
            t.OnRtnError += (sender, e) => Console.WriteLine("OnRtnError:{0}=>{1}", e.ErrorID, e.ErrorMsg);
            // t.OnRtnExchangeStatus += (sender, e) => Console.WriteLine("OnRtnExchangeStatus:{0}=>{1}", e.Exchange, e.Status);
            t.OnRtnNotice += (sender, e) => Console.WriteLine("OnRtnNotice:{0}", e.Value);
            t.OnRtnOrder += OnRtnOrder;
            t.OnRtnTrade += OnRtnTrade;


        }
        /// <summary>
        /// 添加策略需要的合约
        /// </summary>
        /// <param name="instrumentID"></param>
        public void AddContract(string instrumentID)
        {
            AContract contract = new AContract(this.t);
            try
            {
                contract.Instrument = t.DicInstrumentField[instrumentID];
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Contracts.Add(contract);
            SubscribeMarketData(instrumentID);
        }
        /// <summary>
        /// 策略初始化工作，比如添加合约，为合约添加响应等
        /// </summary>
        public virtual void Init() { }
        /// <summary>
        /// 订阅行情
        /// </summary>
        /// <param name="instrumentID">合约代码</param>
        public void SubscribeMarketData(string instrumentID)
        {
            q.ReqSubscribeMarketData(instrumentID);
        }
        private void OnRtnTick(object sender, TickEventArgs e)//将行情分发给各个合约
        {
            AContract contract = Contracts.Find(
                delegate(AContract c) { return string.Compare(c.Instrument.InstrumentID, e.Tick.InstrumentID) == 0; }
                );
            if (contract != null)
            {
                contract.OnRtnTick(sender, e);
            }
        }
        private void OnRtnOrder(object sender, OrderArgs e)//将报单回报分发给各个合约
        {
            AContract contract = Contracts.Find(
                delegate(AContract c) { return string.Compare(c.Instrument.InstrumentID, e.Value.InstrumentID) == 0; }
                );
            if (contract != null)
            {
                contract.OnRtnOrder(sender, e);
            }
        }
        private void OnRtnTrade(object sender, TradeArgs e)//将成交回报分发给各个合约
        {
            AContract contract = Contracts.Find(
                delegate(AContract c) { return string.Compare(c.Instrument.InstrumentID, e.Value.InstrumentID) == 0; }
                );
            if (contract != null)
            {
                contract.OnRtnTrade(sender, e);
            }
        }
        private void OnRtnCancel(object sender, OrderArgs e)//将撤单回报分发给各个合约
        {
            AContract contract = Contracts.Find(
                delegate(AContract c) { return string.Compare(c.Instrument.InstrumentID, e.Value.InstrumentID) == 0; }
                );
            if (contract != null)
            {
                contract.OnRtnCancel(sender, e);
            }
        }

    }
}
