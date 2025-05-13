namespace SimulationTest.Tests
{
    /// <summary>
    /// Context to maintain state between test operations
    /// </summary>
    public class TestContext
    {
        /// <summary>
        /// User ID from registration/login
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Username for test user
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Email address for test user
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Password for test user
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Authentication token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Admin authentication token for privileged operations
        /// </summary>
        public string AdminToken { get; set; }

        /// <summary>
        /// Refresh token for authentication
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// Test symbol for trading and market data tests
        /// </summary>
        public string TestSymbol { get; set; } = "BTC-USDT";

        /// <summary>
        /// Order ID from order creation
        /// </summary>
        public string OrderId { get; set; }

        /// <summary>
        /// Order lock ID for testing lock operations
        /// </summary>
        public string OrderLockId { get; set; }

        /// <summary>
        /// Deposit ID from deposit creation
        /// </summary>
        public string DepositId { get; set; }

        /// <summary>
        /// Withdrawal ID from withdrawal creation
        /// </summary>
        public string WithdrawalId { get; set; }

        /// <summary>
        /// Notification ID for testing notification operations
        /// </summary>
        public string NotificationId { get; set; }
    }
}