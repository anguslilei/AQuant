using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeAPI;
using QuoteAPI;
using System.Threading;
using AQuant;
namespace ArbitrageSample
{
    class MdTest
    {

        public static void Main(string[] args)
        {
            ArbStrategy st = new ArbStrategy();
            st.Init();
        }


    }
}
