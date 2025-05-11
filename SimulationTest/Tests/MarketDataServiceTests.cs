using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using CommonLib.Models.Market;
using SimulationTest.Core;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Tests for the Market Data Service API
    /// </summary>
    public class MarketDataServiceTests : ApiTestBase
    {
        /// <summary>
        /// Test that trading symbols can be retrieved
        /// </summary>
        [ApiTest("Test getting trading symbols")]
        public async Task<ApiTestResult> GetSymbols_ShouldReturnSymbols()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Act
                var symbols = await GetAsync<List<Symbol>>("market-data", "/market/symbols", false);

                // Assert
                stopwatch.Stop();

                if (symbols == null)
                {
                    return ApiTestResult.Failed(
                        "Symbols response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (symbols.Count == 0)
                {
                    return ApiTestResult.Failed(
                        "Symbols list is empty",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that ticker data can be retrieved for a symbol
        /// </summary>
        [ApiTest("Test getting ticker data for a valid symbol")]
        public async Task<ApiTestResult> GetTicker_WithValidSymbol_ShouldReturnMarketData()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Act
                var ticker = await GetAsync<MarketData>("market-data", "/market/ticker?symbol=BTC-USDT", false);

                // Assert
                stopwatch.Stop();

                if (ticker == null)
                {
                    return ApiTestResult.Failed(
                        "Ticker response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (ticker.Symbol != "BTC-USDT")
                {
                    return ApiTestResult.Failed(
                        $"Symbol should be BTC-USDT, but was {ticker.Symbol}",
                        null,
                        stopwatch.Elapsed);
                }

                if (ticker.LastPrice <= 0)
                {
                    return ApiTestResult.Failed(
                        $"Last price should be greater than 0, but was {ticker.LastPrice}",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that market summary can be retrieved
        /// </summary>
        [ApiTest("Test getting market summary")]
        public async Task<ApiTestResult> GetMarketSummary_ShouldReturnSummary()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Act
                var summary = await GetAsync<MarketSummaryResponse>("market-data", "/market/summary", false);

                // Assert
                stopwatch.Stop();

                if (summary == null)
                {
                    return ApiTestResult.Failed(
                        "Market summary response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (summary.Markets == null) { return ApiTestResult.Failed("Market summary markets is null", null, stopwatch.Elapsed); }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that order book depth can be retrieved
        /// </summary>
        [ApiTest("Test getting order book depth for a valid symbol")]
        public async Task<ApiTestResult> GetOrderBookDepth_WithValidSymbol_ShouldReturnOrderBook()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Act
                var orderBook = await GetAsync<OrderBook>("market-data", "/market/depth?symbol=BTC-USDT", false);

                // Assert
                stopwatch.Stop();

                if (orderBook == null)
                {
                    return ApiTestResult.Failed(
                        "Order book response is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (orderBook.Symbol != "BTC-USDT")
                {
                    return ApiTestResult.Failed(
                        $"Symbol should be BTC-USDT, but was {orderBook.Symbol}",
                        null,
                        stopwatch.Elapsed);
                }

                if (orderBook.Asks == null)
                {
                    return ApiTestResult.Failed(
                        "Order book asks is null",
                        null,
                        stopwatch.Elapsed);
                }

                if (orderBook.Bids == null)
                {
                    return ApiTestResult.Failed(
                        "Order book bids is null",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that candlestick data can be retrieved
        /// </summary>
        [ApiTest("Test getting candlestick data with valid parameters")]
        public async Task<ApiTestResult> GetKlines_WithValidParameters_ShouldReturnKlines()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Act
                var klines = await GetAsync<List<Kline>>("market-data", "/market/klines?symbol=BTC-USDT&interval=1h", false);

                // Assert
                stopwatch.Stop();

                if (klines == null)
                {
                    return ApiTestResult.Failed(
                        "Klines response is null",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Test that recent trades can be retrieved
        /// </summary>
        [ApiTest("Test getting recent trades for a valid symbol")]
        public async Task<ApiTestResult> GetRecentTrades_WithValidSymbol_ShouldReturnTrades()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Act
                var trades = await GetAsync<List<TradeResponse>>("market-data", "/market/trades?symbol=BTC-USDT", false);

                // Assert
                stopwatch.Stop();

                if (trades == null)
                {
                    return ApiTestResult.Failed(
                        "Trades response is null",
                        null,
                        stopwatch.Elapsed);
                }

                return ApiTestResult.Passed(stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }
    }


}