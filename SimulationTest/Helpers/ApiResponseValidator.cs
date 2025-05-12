using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using CommonLib.Models;
using SimulationTest.Core;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SimulationTest.Helpers
{
    /// <summary>
    /// Helper class for validating API responses in service tests
    /// </summary>
    public static class ApiResponseValidator
    {
        /// <summary>
        /// Validates a general API response against common failure patterns
        /// </summary>
        /// <typeparam name="T">The expected response type</typeparam>
        /// <param name="response">The response object</param>
        /// <param name="stopwatch">Stopwatch for timing</param>
        /// <param name="callerMemberName">Name of the calling method</param>
        /// <returns>ApiTestResult indicating success or failure</returns>
        public static ApiTestResult ValidateResponse<T>(T response, Stopwatch stopwatch, [CallerMemberName] string callerMemberName = null)
        {
            // Basic null check
            if (response == null)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    $"Response is null",
                    null,
                    stopwatch.Elapsed);
            }

            // Handle collections
            if (response is IEnumerable<object> collection)
            {
                // Empty collections are generally valid, but can be checked by caller
                return ApiTestResult.Passed(callerMemberName, stopwatch.Elapsed);
            }

            // Handle paginated results
            if (response is PaginatedResult<object> paginatedResult)
            {
                if (paginatedResult.Items == null)
                {
                    return ApiTestResult.Failed(
                        callerMemberName,
                        "Paginated items collection is null",
                        null,
                        stopwatch.Elapsed);
                }

                // Empty paginated results are valid
                return ApiTestResult.Passed(callerMemberName, stopwatch.Elapsed);
            }

            // Handle common empty container types
            // If response has properties that are null collections, verify they're properly initialized
            var nullCollectionProperties = typeof(T).GetProperties()
                .Where(p => p.PropertyType.IsGenericType &&
                       (p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) ||
                        p.PropertyType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                        p.PropertyType.GetGenericTypeDefinition() == typeof(ICollection<>)))
                .Where(p => p.GetValue(response) == null)
                .ToList();

            if (nullCollectionProperties.Any())
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    $"The following collection properties are null: {string.Join(", ", nullCollectionProperties.Select(p => p.Name))}",
                    null,
                    stopwatch.Elapsed);
            }

            // Basic successful response
            return ApiTestResult.Passed(callerMemberName, stopwatch.Elapsed);
        }

        /// <summary>
        /// Validates a response with expected direct field equality
        /// </summary>
        /// <typeparam name="T">The response type</typeparam>
        /// <param name="response">The response object</param>
        /// <param name="fieldChecks">Dictionary of field name and expected value pairs</param>
        /// <param name="stopwatch">Stopwatch for timing</param>
        /// <param name="callerMemberName">Name of the calling method</param>
        /// <returns>ApiTestResult indicating success or failure</returns>
        public static ApiTestResult ValidateFieldValues<T>(
            T response,
            Dictionary<string, object> fieldChecks,
            Stopwatch stopwatch,
            [CallerMemberName] string callerMemberName = null)
        {
            // First do basic validation
            var baseResult = ValidateResponse(response, stopwatch, callerMemberName);
            if (!baseResult.Success)
            {
                return baseResult;
            }

            // Check each expected field value
            foreach (var check in fieldChecks)
            {
                var property = typeof(T).GetProperty(check.Key);
                if (property == null)
                {
                    return ApiTestResult.Failed(
                        callerMemberName,
                        $"Property {check.Key} not found in type {typeof(T).Name}",
                        null,
                        stopwatch.Elapsed);
                }

                var actualValue = property.GetValue(response);
                var expectedValue = check.Value;

                // Handle special numeric comparison cases
                if (expectedValue is int intVal && actualValue is long longVal)
                {
                    if (longVal != intVal)
                    {
                        return ApiTestResult.Failed(
                            callerMemberName,
                            $"Property {check.Key} expected value {expectedValue}, but was {actualValue}",
                            null,
                            stopwatch.Elapsed);
                    }
                    continue;
                }

                // Handle double/decimal comparison with tolerance
                if ((expectedValue is double || expectedValue is decimal) &&
                    (actualValue is double || actualValue is decimal))
                {
                    double expected = Convert.ToDouble(expectedValue);
                    double actual = Convert.ToDouble(actualValue);

                    if (Math.Abs(actual - expected) > 0.0001)
                    {
                        return ApiTestResult.Failed(
                            callerMemberName,
                            $"Property {check.Key} expected value {expectedValue}, but was {actualValue}",
                            null,
                            stopwatch.Elapsed);
                    }
                    continue;
                }

                // Default equality check
                if (!Equals(actualValue, expectedValue))
                {
                    return ApiTestResult.Failed(
                        callerMemberName,
                        $"Property {check.Key} expected value {expectedValue}, but was {actualValue}",
                        null,
                        stopwatch.Elapsed);
                }
            }

            return ApiTestResult.Passed(callerMemberName, stopwatch.Elapsed);
        }

        /// <summary>
        /// Validates a deposit/withdrawal transaction
        /// </summary>
        /// <param name="transaction">The transaction to validate</param>
        /// <param name="expectedType">The expected transaction type</param>
        /// <param name="expectedAsset">The expected asset</param>
        /// <param name="expectedAmount">The expected amount</param>
        /// <param name="stopwatch">Stopwatch for timing</param>
        /// <param name="callerMemberName">Name of the calling method</param>
        /// <returns>ApiTestResult indicating success or failure</returns>
        public static ApiTestResult ValidateTransaction(
            object transaction,
            string expectedType,
            string expectedAsset,
            decimal expectedAmount,
            Stopwatch stopwatch,
            [CallerMemberName] string callerMemberName = null)
        {
            // First do basic validation
            var baseResult = ValidateResponse(transaction, stopwatch, callerMemberName);
            if (!baseResult.Success)
            {
                return baseResult;
            }

            // Extract common fields using reflection
            var typeProperty = transaction.GetType().GetProperty("Type");
            var assetProperty = transaction.GetType().GetProperty("Asset");
            var amountProperty = transaction.GetType().GetProperty("Amount");

            if (typeProperty == null || assetProperty == null || amountProperty == null)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    $"Transaction object is missing required properties",
                    null,
                    stopwatch.Elapsed);
            }

            var type = typeProperty.GetValue(transaction)?.ToString();
            var asset = assetProperty.GetValue(transaction)?.ToString();
            var amount = Convert.ToDecimal(amountProperty.GetValue(transaction));

            // Validate transaction properties
            if (type != expectedType)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    $"Transaction type should be {expectedType}, but was {type}",
                    null,
                    stopwatch.Elapsed);
            }

            if (asset != expectedAsset)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    $"Transaction asset should be {expectedAsset}, but was {asset}",
                    null,
                    stopwatch.Elapsed);
            }

            if (amount != expectedAmount)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    $"Transaction amount should be {expectedAmount}, but was {amount}",
                    null,
                    stopwatch.Elapsed);
            }

            return ApiTestResult.Passed(callerMemberName, stopwatch.Elapsed);
        }

        /// <summary>
        /// Validates an error response against expected error code or message
        /// </summary>
        /// <typeparam name="T">The error response type</typeparam>
        /// <param name="errorResponse">The error response</param>
        /// <param name="expectedCode">The expected error code (optional)</param>
        /// <param name="expectedMessage">The expected error message substring (optional)</param>
        /// <param name="stopwatch">Stopwatch for timing</param>
        /// <param name="callerMemberName">Name of the calling method</param>
        /// <returns>ApiTestResult indicating the validation result</returns>
        public static ApiTestResult ValidateErrorResponse<T>(
            T errorResponse,
            string expectedCode = null,
            string expectedMessage = null,
            Stopwatch stopwatch = null,
            [CallerMemberName] string callerMemberName = null)
        {
            if (stopwatch == null)
            {
                stopwatch = new Stopwatch();
                stopwatch.Start();
            }

            // Check if we have a null response
            if (errorResponse == null)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    "Error response is null",
                    null,
                    stopwatch.Elapsed);
            }

            // Get the message, code, and success properties
            var messageProperty = typeof(T).GetProperty("Message");
            if (messageProperty == null)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    "Error response does not have a Message property",
                    null,
                    stopwatch.Elapsed);
            }

            var codeProperty = typeof(T).GetProperty("Code");
            var successProperty = typeof(T).GetProperty("Success");

            // Get the values
            var actualMessage = messageProperty.GetValue(errorResponse)?.ToString();
            var actualCode = codeProperty?.GetValue(errorResponse)?.ToString();
            var isSuccess = successProperty != null ?
                Convert.ToBoolean(successProperty.GetValue(errorResponse)) :
                false;

            // Success should be false for error responses
            if (isSuccess)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    "Expected error response but Success=true",
                    null,
                    stopwatch.Elapsed);
            }

            // Check expected code if provided
            if (!string.IsNullOrEmpty(expectedCode) && actualCode != expectedCode)
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    $"Expected error code {expectedCode}, but received {actualCode}",
                    null,
                    stopwatch.Elapsed);
            }

            // Check expected message if provided
            if (!string.IsNullOrEmpty(expectedMessage) &&
                (string.IsNullOrEmpty(actualMessage) || !actualMessage.Contains(expectedMessage)))
            {
                return ApiTestResult.Failed(
                    callerMemberName,
                    $"Expected error message to contain '{expectedMessage}', but received '{actualMessage}'",
                    null,
                    stopwatch.Elapsed);
            }

            return ApiTestResult.Passed(callerMemberName, stopwatch.Elapsed);
        }
    }
}