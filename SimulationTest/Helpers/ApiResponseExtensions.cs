using System;
using System.Collections.Generic;
using System.Diagnostics;
using SimulationTest.Core;
using CommonLib.Models;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SimulationTest.Helpers
{
    /// <summary>
    /// Extensions for handling API responses in tests
    /// </summary>
    public static class ApiResponseExtensions
    {
        /// <summary>
        /// Validates a collection contains at least one item
        /// </summary>
        /// <typeparam name="T">The type of collection items</typeparam>
        /// <param name="collection">The collection to check</param>
        /// <param name="stopwatch">Stopwatch for timing</param>
        /// <param name="errorMessage">Optional custom error message</param>
        /// <param name="callerMemberName">Name of the calling method</param>
        /// <returns>ApiTestResult indicating success or failure</returns>
        public static ApiTestResult ShouldNotBeEmpty<T>(
            this ICollection<T> collection,
            Stopwatch stopwatch,
            string errorMessage = null,
            [CallerMemberName] string callerMemberName = null)
        {
            // First validate the response
            var baseResult = ApiResponseValidator.ValidateResponse(collection, stopwatch);
            if (!baseResult.Success)
            {
                return baseResult;
            }

            if (collection.Count == 0)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    errorMessage ?? "Collection is empty",
                    null,
                    stopwatch.Elapsed);
            }

            return ApiTestResult.Passed(callerMemberName, stopwatch.Elapsed);
        }

        /// <summary>
        /// Validates that a collection contains an item that matches the specified predicate
        /// </summary>
        /// <typeparam name="T">The type of collection items</typeparam>
        /// <param name="collection">The collection to check</param>
        /// <param name="predicate">The condition items should meet</param>
        /// <param name="stopwatch">Stopwatch for timing</param>
        /// <param name="errorMessage">Optional custom error message</param>
        /// <param name="callerMemberName">Name of the calling method</param>
        /// <returns>ApiTestResult indicating success or failure</returns>
        public static ApiTestResult ShouldContain<T>(
            this ICollection<T> collection,
            Func<T, bool> predicate,
            Stopwatch stopwatch,
            string errorMessage = null,
            [CallerMemberName] string callerMemberName = null)
        {
            // First validate the collection is not empty
            var emptyCheckResult = collection.ShouldNotBeEmpty(stopwatch);
            if (!emptyCheckResult.Success)
            {
                return emptyCheckResult;
            }

            // Check if any item matches the predicate
            if (!collection.Any(predicate))
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    errorMessage ?? "Collection does not contain an item matching the specified criteria",
                    null,
                    stopwatch.Elapsed);
            }

            return ApiTestResult.Passed(callerMemberName, stopwatch.Elapsed);
        }

        /// <summary>
        /// Validates that a paginated result contains at least one item
        /// </summary>
        /// <typeparam name="T">The type of items in the paginated result</typeparam>
        /// <param name="paginatedResult">The paginated result to check</param>
        /// <param name="stopwatch">Stopwatch for timing</param>
        /// <param name="errorMessage">Optional custom error message</param>
        /// <param name="callerMemberName">Name of the calling method</param>
        /// <returns>ApiTestResult indicating success or failure</returns>
        public static ApiTestResult ShouldHaveItems<T>(
            this PaginatedResult<T> paginatedResult,
            Stopwatch stopwatch,
            string errorMessage = null,
            [CallerMemberName] string callerMemberName = null) where T : class
        {
            // First validate the paginated result structure
            var baseResult = ApiResponseValidator.ValidateResponse(paginatedResult, stopwatch);
            if (!baseResult.Success)
            {
                return baseResult;
            }

            if (paginatedResult.Items == null)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    "Items collection is null in paginated result",
                    null,
                    stopwatch.Elapsed);
            }

            var items = paginatedResult.Items.ToList();
            if (items.Count == 0 && paginatedResult.TotalItems > 0)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    errorMessage ?? "Paginated result claims to have items but the Items collection is empty",
                    null,
                    stopwatch.Elapsed);
            }

            return ApiTestResult.Passed(callerMemberName, stopwatch.Elapsed);
        }

        /// <summary>
        /// Validates an individual API response property
        /// </summary>
        /// <typeparam name="T">The type of the response</typeparam>
        /// <typeparam name="TProperty">The type of the property</typeparam>
        /// <param name="response">The response object</param>
        /// <param name="propertyName">The name of the property to check</param>
        /// <param name="expectedValue">The expected value</param>
        /// <param name="stopwatch">Stopwatch for timing</param>
        /// <param name="callerMemberName">Name of the calling method</param>
        /// <returns>ApiTestResult indicating success or failure</returns>
        public static ApiTestResult ShouldHaveProperty<T, TProperty>(
            this T response,
            string propertyName,
            TProperty expectedValue,
            Stopwatch stopwatch,
            [CallerMemberName] string callerMemberName = null)
        {
            // Create result from ValidateFieldValues and add testName if needed
            var result = ApiResponseValidator.ValidateFieldValues(
                response,
                new Dictionary<string, object> { { propertyName, expectedValue } },
                stopwatch);

            // If result doesn't have a test name, set it
            if (string.IsNullOrEmpty(result.TestName))
            {
                result.TestName = callerMemberName;
            }

            return result;
        }

        /// <summary>
        /// Validates that the response was received within an acceptable time
        /// </summary>
        /// <typeparam name="T">The type of the response</typeparam>
        /// <param name="response">The response object</param>
        /// <param name="stopwatch">Stopwatch for timing</param>
        /// <param name="maxAcceptableMs">Maximum acceptable response time in milliseconds</param>
        /// <param name="errorMessage">Optional custom error message</param>
        /// <param name="callerMemberName">Name of the calling method</param>
        /// <returns>ApiTestResult indicating success or failure</returns>
        public static ApiTestResult ShouldRespondWithin<T>(
            this T response,
            Stopwatch stopwatch,
            int maxAcceptableMs = 5000,
            string errorMessage = null,
            [CallerMemberName] string callerMemberName = null)
        {
            // First validate the response itself
            var baseResult = ApiResponseValidator.ValidateResponse(response, stopwatch);
            if (!baseResult.Success)
            {
                return baseResult;
            }

            // Check the elapsed time
            if (stopwatch.ElapsedMilliseconds > maxAcceptableMs)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    errorMessage ?? $"Response time exceeded acceptable limit of {maxAcceptableMs}ms (actual: {stopwatch.ElapsedMilliseconds}ms)",
                    null,
                    stopwatch.Elapsed);
            }

            return ApiTestResult.Passed(callerMemberName, stopwatch.Elapsed);
        }
    }
}