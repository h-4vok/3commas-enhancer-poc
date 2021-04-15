using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XCommas.Net;
using XCommas.Net.Objects;

namespace _3commasEnhancer
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // Get variables from config file
            const string apiKey = "c2f4b133a78846dc967ac9608384e22a24145ec7fb624376be7d55145069ab26";
            const string apiSecret = "4550938bc255be5df1cbd7ddcef43d7f788c3108a2826904b546f60340dc7caadf8e60fc4b56d1cf49e1ec2aaa2116edfd8c3d44d5d18c13a515f881e652232a755f8fb06420c9a52e96f4aea9a887a9dc5a061b04bee3fa6827635df81e4df88aa0a680";

            // Create client
            Console.WriteLine("Connecting");
            var client = new XCommasApi(apiKey, apiSecret);

            // Perform GET request - Get Currency Rate
            {
                var response = await client.GetBotsAsync(botScope: BotScope.Enabled);
                foreach (var bot in response.Data)
                {
                    Console.WriteLine($"{bot.Id} - {bot.Name}");
                }
            }
            Console.WriteLine("Press any key for next query");
            Console.ReadKey();

            // Perform GET request - Get Active Deals
            IReadOnlyCollection<Deal> candidatesForTsl;
            {
                var response = await client.GetDealsAsync(dealScope: DealScope.Active);
                var positiveDeals = new HashSet<Deal>();
                foreach(var deal in response.Data)
                {
                    Console.WriteLine($"Bot {deal.BotId} - {deal.Pair} - BO: {deal.BaseOrderVolume} - Bought: {deal.BoughtAmount} - AvgPrice: {deal.BoughtAveragePrice} - Current Price: {deal.CurrentPrice} - Actual Profit: {deal.ActualProfitPercentage} / {deal.ActualProfit} - TP: {deal.TakeProfit} / {deal.TakeProfitPrice}");

                    if (deal.ActualProfit > decimal.Zero) positiveDeals.Add(deal);
                }

                Console.WriteLine($"There are {positiveDeals.Count} deals in profit:");

                foreach(var deal in positiveDeals)
                {
                    Console.WriteLine($"{deal.Pair} - +{deal.ActualProfitPercentage} % - +{deal.ActualProfit} {deal.FromCurrency}");
                }

                candidatesForTsl = new HashSet<Deal>(positiveDeals.Where(d => d.ActualProfitPercentage.GetValueOrDefault() > 0.5m));
            }

            // For every candidate for TSL, we calculate and update it
            foreach(var deal in candidatesForTsl)
            {
                var newStopLossPercentage = (deal.ActualProfitPercentage ?? 100) / 2;

                var isStopLossTakeProfit = deal.StopLossPercentage.HasValue && deal.StopLossPercentage < 0;
                if (isStopLossTakeProfit && Math.Abs(newStopLossPercentage) >= newStopLossPercentage)
                    continue;

                var dealId = deal.Id;
                var updateBody = new DealUpdateData(dealId)
                {
                    StopLossPercentage = -newStopLossPercentage,
                };

                Console.WriteLine($"Updating deal {dealId} - {deal.Pair}");
                var response = await client.UpdateDealAsync(dealId, updateBody);
                Console.WriteLine($"Success {dealId} - {deal.Pair} - Currently +{deal.ActualProfitPercentage} - TSL to +{newStopLossPercentage}");

                Console.WriteLine(response.Error ?? response.RawData);
            }


            // Wait until key from dev
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
