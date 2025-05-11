using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using CommonLib.Models;
using CommonLib.Models.Trading;
using MongoDB.Bson;
using SimulationTest.Core;
using System.Text.Json;
using System.Diagnostics;
using System.Linq;

namespace SimulationTest.Tests
{
    /// <summary>
    /// Tests for Trading Service API
    /// </summary>
    public class TradingServiceTests : ApiTestBase
    {
        private static string _createdOrderId;
        private static readonly string TestDependencyPrefix = "SimulationTest.Tests.";

        /// <summary>
        /// Test connectivity to Trading Service before running tests
        /// </summary>
        [ApiTest("Test connectivity to Trading Service")]
        public async Task<ApiTestResult> CheckConnectivity_TradingService_ShouldBeAccessible()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Try to connect to the health endpoint
                var client = _httpClientFactory.GetClient("trading");
                var response = await client.GetAsync("/health");

                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    return ApiTestResult.Passed(stopwatch.Elapsed);
                }
                else
                {
                    return ApiTestResult.Failed(
                        $"Failed to connect to Trading Service. Status code: {response.StatusCode}",
                        null,
                        stopwatch.Elapsed);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return ApiTestResult.Failed($"Exception while connecting to Trading Service: {ex.Message}", ex, stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// Tests order creation functionality
        /// </summary>
        [ApiTest("Test creating an order with valid data")]
        public async Task<ApiTestResult> CreateOrder_WithValidData_ShouldReturnOrder()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();
                var orderRequest = new CreateOrderRequest
                {
                    Symbol = "BTC-USDT",
                    Side = "BUY",
                    Type = "LIMIT",
                    Price = 50000.0m,
                    Quantity = 0.001m,
                    TimeInForce = "GTC"
                };

                // Act
                var response = await PostAsync<CreateOrderRequest, OrderResponse>("trading", "/order", orderRequest);

                if (response == null)
                {
                    stopwatch.Stop();
                    return ApiTestResult.Failed(
                        "API request failed: null response",
                        new HttpRequestException("No response received"),
                        stopwatch.Elapsed);
                }

                // Store order ID for subsequent tests
                _createdOrderId = response.OrderId;

                // Assert
                bool isValid = response.Symbol == "BTC-USDT"
                    && response.Side == "BUY"
                    && response.Price == 50000.0m
                    && response.OrigQty == 0.001m;

                stopwatch.Stop();

                if (!isValid)
                {
                    return ApiTestResult.Failed(
                        $"Order data mismatch. Expected Symbol=BTC-USDT, Side=BUY, Price=50000.0, OrigQty=0.001, " +
                        $"but received Symbol={response.Symbol}, Side={response.Side}, Price={response.Price}, OrigQty={response.OrigQty}",
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
        /// Tests getting an order by ID functionality
        /// </summary>
        [ApiTest("Test getting an order with valid order ID")]
        public async Task<ApiTestResult> GetOrder_WithValidOrderId_ShouldReturnOrder()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // Skip if we don't have an order ID from previous test
                if (string.IsNullOrEmpty(_createdOrderId))
                {
                    // Create a new order if we don't have one
                    var orderRequest = new CreateOrderRequest
                    {
                        Symbol = "BTC-USDT",
                        Side = "BUY",
                        Type = "LIMIT",
                        Price = 50000.0m,
                        Quantity = 0.001m,
                        TimeInForce = "GTC"
                    };

                    var createdOrder = await PostAsync<CreateOrderRequest, OrderResponse>("trading", "/order", orderRequest);
                    if (createdOrder == null || string.IsNullOrEmpty(createdOrder.OrderId))
                    {
                        stopwatch.Stop();
                        return ApiTestResult.Failed(
                            "Failed to create order for test",
                            null,
                            stopwatch.Elapsed);
                    }

                    _createdOrderId = createdOrder.OrderId;
                }

                // Act - Get the order
                var retrievedOrder = await GetAsync<OrderResponse>("trading", $"/order/{_createdOrderId}");

                // Assert
                if (retrievedOrder == null)
                {
                    stopwatch.Stop();
                    return ApiTestResult.Failed(
                        "Retrieved order is null",
                        null,
                        stopwatch.Elapsed);
                }

                bool isValid = retrievedOrder.OrderId == _createdOrderId
                    && retrievedOrder.Symbol == "BTC-USDT"
                    && retrievedOrder.Side == "BUY"
                    && retrievedOrder.Price == 50000.0m
                    && retrievedOrder.OrigQty == 0.001m;

                stopwatch.Stop();

                if (!isValid)
                {
                    return ApiTestResult.Failed(
                        "Retrieved order data does not match created order data",
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
        /// Tests getting open orders functionality
        /// </summary>
        [ApiTest("Test getting open orders when authenticated")]
        public async Task<ApiTestResult> GetOpenOrders_WhenAuthenticated_ShouldReturnOpenOrders()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // Ensure we have at least one open order
                if (string.IsNullOrEmpty(_createdOrderId))
                {
                    var orderRequest = new CreateOrderRequest
                    {
                        Symbol = "BTC-USDT",
                        Side = "BUY",
                        Type = "LIMIT",
                        Price = 45000.0m, // Set a low price to ensure it stays open
                        Quantity = 0.002m,
                        TimeInForce = "GTC"
                    };

                    // Create an open order
                    var createdOrder = await PostAsync<CreateOrderRequest, OrderResponse>("trading", "/order", orderRequest);
                    if (createdOrder == null)
                    {
                        stopwatch.Stop();
                        return ApiTestResult.Failed(
                            "Failed to create test order",
                            null,
                            stopwatch.Elapsed);
                    }

                    _createdOrderId = createdOrder.OrderId;
                }

                // Act
                var openOrders = await GetAsync<List<OrderResponse>>("trading", "/order/open");

                // Assert
                if (openOrders == null)
                {
                    stopwatch.Stop();
                    return ApiTestResult.Failed(
                        "Open orders response is null",
                        null,
                        stopwatch.Elapsed);
                }

                stopwatch.Stop();

                if (openOrders.Count == 0)
                {
                    return ApiTestResult.Failed(
                        "No open orders found when there should be at least one",
                        null,
                        stopwatch.Elapsed);
                }

                bool foundCreatedOrder = openOrders.Any(o => o.OrderId == _createdOrderId);
                if (!foundCreatedOrder)
                {
                    return ApiTestResult.Failed(
                        "Created order not found in open orders list",
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
        /// Tests order cancellation functionality
        /// </summary>
        [ApiTest("Test cancelling an order with valid order ID")]
        public async Task<ApiTestResult> CancelOrder_WithValidOrderId_ShouldSucceed()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // Skip if we don't have an order ID from previous test
                if (string.IsNullOrEmpty(_createdOrderId))
                {
                    var orderRequest = new CreateOrderRequest
                    {
                        Symbol = "BTC-USDT",
                        Side = "BUY",
                        Type = "LIMIT",
                        Price = 50000.0m,
                        Quantity = 0.001m,
                        TimeInForce = "GTC"
                    };

                    // First create an order
                    var createdOrder = await PostAsync<CreateOrderRequest, OrderResponse>("trading", "/order", orderRequest);
                    if (createdOrder == null || string.IsNullOrEmpty(createdOrder.OrderId))
                    {
                        stopwatch.Stop();
                        return ApiTestResult.Failed(
                            "Created order is invalid or missing order ID",
                            null,
                            stopwatch.Elapsed);
                    }

                    _createdOrderId = createdOrder.OrderId;
                }

                // Act - Cancel the order
                var cancelSuccess = await DeleteAsync("trading", $"/order/{_createdOrderId}");
                if (!cancelSuccess)
                {
                    stopwatch.Stop();
                    return ApiTestResult.Failed(
                        "Order cancellation failed",
                        new HttpRequestException("Failed to cancel order"),
                        stopwatch.Elapsed);
                }

                // Assert - Check that the order status is changed to cancelled
                var canceledOrder = await GetAsync<OrderResponse>("trading", $"/order/{_createdOrderId}");
                if (canceledOrder == null)
                {
                    stopwatch.Stop();
                    return ApiTestResult.Failed(
                        "Cancelled order response is empty",
                        null,
                        stopwatch.Elapsed);
                }

                stopwatch.Stop();

                if (canceledOrder.Status != "CANCELED")
                {
                    return ApiTestResult.Failed(
                        $"Order status should be CANCELED, but was {canceledOrder.Status}",
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
        /// Tests getting order history functionality
        /// </summary>
        [ApiTest("Test getting order history when authenticated")]
        public async Task<ApiTestResult> GetOrderHistory_WhenAuthenticated_ShouldReturnOrderHistory()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // Act
                var orderHistory = await GetAsync<PaginatedResult<OrderResponse>>("trading", "/order/history");

                // Assert
                if (orderHistory == null || orderHistory.Items == null)
                {
                    stopwatch.Stop();
                    return ApiTestResult.Failed(
                        "Order history response is null or missing items",
                        null,
                        stopwatch.Elapsed);
                }

                stopwatch.Stop();

                if (orderHistory.Items.Count() == 0)
                {
                    return ApiTestResult.Failed(
                        "No order history found when there should be at least one",
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
        /// Tests getting trade history functionality
        /// </summary>
        [ApiTest("Test getting trade history when authenticated")]
        public async Task<ApiTestResult> GetTradeHistory_WhenAuthenticated_ShouldReturnTradeHistory()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                // Arrange
                await EnsureAuthenticatedAsync();

                // Act
                var tradeHistory = await GetAsync<PaginatedResult<TradeResponse>>("trading", "/trade/history");

                // Assert
                if (tradeHistory == null)
                {
                    stopwatch.Stop();
                    return ApiTestResult.Failed(
                        "Trade history response is null",
                        null,
                        stopwatch.Elapsed);
                }

                stopwatch.Stop();

                // Note: We're not checking for trades since there might not be any in a test environment
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