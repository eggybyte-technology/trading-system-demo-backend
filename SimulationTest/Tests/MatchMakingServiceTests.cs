// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using System.Diagnostics;
// using CommonLib.Models.Trading;
// using SimulationTest.Core;
// using MongoDB.Bson;

// namespace SimulationTest.Tests
// {
//     /// <summary>
//     /// Tests for the Match Making Service API
//     /// </summary>
//     public class MatchMakingServiceTests : ApiTestBase
//     {
//         /// <summary>
//         /// Test that match engine status can be retrieved
//         /// </summary>
//         [ApiTest("Test getting match engine status when authenticated")]
//         public async Task<ApiTestResult> GetMatchEngineStatus_WhenAuthenticated_ShouldReturnStatus()
//         {
//             var stopwatch = new Stopwatch();
//             stopwatch.Start();

//             try
//             {
//                 // Arrange
//                 await EnsureAuthenticatedAsync();

//                 // Act
//                 var status = await GetAsync<MatchStatusInfo>("match-making", "/match/status");

//                 // Assert
//                 stopwatch.Stop();

//                 if (status == null)
//                 {
//                     return ApiTestResult.Failed(
//                         "Match engine status response is null",
//                         null,
//                         stopwatch.Elapsed);
//                 }

//                 if (!status.IsRunning)
//                 {
//                     return ApiTestResult.Failed(
//                         "Match engine should be running",
//                         null,
//                         stopwatch.Elapsed);
//                 }

//                 return ApiTestResult.Passed(stopwatch.Elapsed);
//             }
//             catch (Exception ex)
//             {
//                 stopwatch.Stop();
//                 return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
//             }
//         }

//         /// <summary>
//         /// Test that matching jobs history can be retrieved
//         /// </summary>
//         [ApiTest("Test getting matching jobs history when authenticated")]
//         public async Task<ApiTestResult> GetMatchingJobsHistory_WhenAuthenticated_ShouldReturnJobs()
//         {
//             var stopwatch = new Stopwatch();
//             stopwatch.Start();

//             try
//             {
//                 // Arrange
//                 await EnsureAuthenticatedAsync();

//                 // Act
//                 var jobs = await GetAsync<List<MatchingJobInfo>>("match-making", "/match/jobs");

//                 // Assert
//                 stopwatch.Stop();

//                 if (jobs == null)
//                 {
//                     return ApiTestResult.Failed(
//                         "Matching jobs response is null",
//                         null,
//                         stopwatch.Elapsed);
//                 }

//                 return ApiTestResult.Passed(stopwatch.Elapsed);
//             }
//             catch (Exception ex)
//             {
//                 stopwatch.Stop();
//                 return ApiTestResult.Failed($"Exception occurred during test: {ex.Message}", ex, stopwatch.Elapsed);
//             }
//         }
//     }

//     /// <summary>
//     /// Match engine status information model for tests
//     /// </summary>
//     public class MatchStatusInfo
//     {
//         /// <summary>
//         /// Gets or sets whether the match engine is running
//         /// </summary>
//         public bool IsRunning { get; set; }

//         /// <summary>
//         /// Gets or sets the engine start time
//         /// </summary>
//         public DateTime StartTime { get; set; }

//         /// <summary>
//         /// Gets or sets the engine uptime in seconds
//         /// </summary>
//         public long UptimeSeconds { get; set; }

//         /// <summary>
//         /// Gets or sets the current order processing rate
//         /// </summary>
//         public int OrdersPerSecond { get; set; }

//         /// <summary>
//         /// Gets or sets the total processed orders
//         /// </summary>
//         public long TotalProcessedOrders { get; set; }

//         /// <summary>
//         /// Gets or sets the total executed trades
//         /// </summary>
//         public long TotalExecutedTrades { get; set; }

//         /// <summary>
//         /// Gets or sets the symbols being processed
//         /// </summary>
//         public List<string> ActiveSymbols { get; set; } = new List<string>();

//         /// <summary>
//         /// Gets or sets the health status
//         /// </summary>
//         public string Health { get; set; } = string.Empty;
//     }

//     /// <summary>
//     /// Matching job information model for tests
//     /// </summary>
//     public class MatchingJobInfo
//     {
//         /// <summary>
//         /// Gets or sets the job ID
//         /// </summary>
//         public ObjectId Id { get; set; }

//         /// <summary>
//         /// Gets or sets the job type
//         /// </summary>
//         public string Type { get; set; } = string.Empty;

//         /// <summary>
//         /// Gets or sets the status
//         /// </summary>
//         public string Status { get; set; } = string.Empty;

//         /// <summary>
//         /// Gets or sets the start time
//         /// </summary>
//         public DateTime StartTime { get; set; }

//         /// <summary>
//         /// Gets or sets the end time
//         /// </summary>
//         public DateTime? EndTime { get; set; }

//         /// <summary>
//         /// Gets or sets the symbol
//         /// </summary>
//         public string Symbol { get; set; } = string.Empty;

//         /// <summary>
//         /// Gets or sets the processed orders count
//         /// </summary>
//         public int ProcessedOrders { get; set; }

//         /// <summary>
//         /// Gets or sets the executed trades count
//         /// </summary>
//         public int ExecutedTrades { get; set; }

//         /// <summary>
//         /// Gets or sets the error message (if any)
//         /// </summary>
//         public string ErrorMessage { get; set; } = string.Empty;
//     }
// }