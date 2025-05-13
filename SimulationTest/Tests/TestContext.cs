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
        public string AdminToken { get; set; } = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhZG1pbiIsImh0dHA6Ly9zY2hlbWFzLnhtbHNvYXAub3JnL3dzLzIwMDUvMDUvaWRlbnRpdHkvY2xhaW1zL25hbWVpZGVudGlmaWVyIjoiYWRtaW4iLCJVc2VySWQiOiJhZG1pbiIsIm5hbWUiOiJhZG1pbiIsImVtYWlsIjoiYWRtaW5AdHJhZGluZ3N5c3RlbS5jb20iLCJqdGkiOiIyZDg3ZjFkMi0xNDc5LTQ2MDEtODg2Zi1jNzZlMGU1NTQwNzYiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOlsiQWRtaW4iLCJVc2VyIiwiTWFuYWdlciJdLCJJc1N5c3RlbVRva2VuIjoidHJ1ZSIsImV4cCI6MTc0NzE3NzE5MywiaXNzIjoiVHJhZGluZ1N5c3RlbSIsImF1ZCI6IlRyYWRpbmdTeXN0ZW1DbGllbnRzIn0.j-GVBO76xQMc1Jjyl5tpDq9eH6puvj8Bi0fmDgPeCug";

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