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
            // Generate your api key and secret with Paper Trading enabled
            const string apiKey = "your-api-key";
            const string apiSecret = "your-api-secret";

            // Create client
            Console.WriteLine("Connecting");
            var client = new XCommasApi(apiKey, apiSecret);

            // Perform GET request - Get Currency Rate
            {
                var response =
                    await client.GetBotsAsync(botScope: BotScope.Enabled);
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
                var response =
                    await client.GetDealsAsync(dealScope: DealScope.Active);
                var positiveDeals = new HashSet<Deal>();
                foreach (var deal in response.Data)
                {
                    Console
                        .WriteLine($"Bot {deal.BotId} - {deal.Pair} - BO: {deal.BaseOrderVolume} - Bought: {deal.BoughtAmount} - AvgPrice: {deal.BoughtAveragePrice} - Current Price: {deal.CurrentPrice} - Actual Profit: {deal.ActualProfitPercentage} / {deal.ActualProfit} - TP: {deal.TakeProfit} / {deal.TakeProfitPrice}");

                    if (deal.ActualProfit > decimal.Zero)
                        positiveDeals.Add(deal);
                }

                Console
                    .WriteLine($"There are {positiveDeals.Count} deals in profit:");

                foreach (var deal in positiveDeals)
                {
                    Console
                        .WriteLine($"{deal.Pair} - +{deal.ActualProfitPercentage} % - +{deal.ActualProfit} {deal.FromCurrency}");
                }

                candidatesForTsl =
                    new HashSet<Deal>(positiveDeals
                            .Where(d =>
                                d.ActualProfitPercentage.GetValueOrDefault() >
                                0.5m));
            }

            // For every candidate for TSL, we calculate and update it
            foreach (var deal in candidatesForTsl)
            {
                var newStopLossPercentage =
                    (deal.ActualProfitPercentage ?? 100) / 2;

                var isStopLossTakeProfit =
                    deal.StopLossPercentage.HasValue &&
                    deal.StopLossPercentage < 0;
                if (
                    isStopLossTakeProfit &&
                    Math.Abs(newStopLossPercentage) >= newStopLossPercentage
                ) continue;

                var dealId = deal.Id;
                var updateBody =
                    new DealUpdateData(dealId)
                    { StopLossPercentage = -newStopLossPercentage };

                Console.WriteLine($"Updating deal {dealId} - {deal.Pair}");
                var response = await client.UpdateDealAsync(dealId, updateBody);
                Console
                    .WriteLine($"Success {dealId} - {deal.Pair} - Currently +{deal.ActualProfitPercentage} - TSL to +{newStopLossPercentage}");

                Console.WriteLine(response.Error ?? response.RawData);
            }

            // Wait until key from dev
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}
