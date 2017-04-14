namespace Alpacka.Lib.Curse
{
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string EncryptedPassword { get; set; }
    }
    
    public class LoginResponse
    {
        public AuthenticationSession Session { get; set; }
        public AuthenticationStatus Status { get; set; }
    }
    
    public enum AuthenticationStatus
    {
        Unsuccessful,
        Success,
        InvalidSession,
        UnauthorizedLogin,
        InvalidPassword,
        UnknownUsername,
        UnknownEmail,
        UnknownError = 100,
        IncorrectTime = 101,
        CorruptLibrary = 102,
        OutdatedClient = 103,
        SubscriptionMismatch = 104,
        SubscriptionExpired = 105,
        InsufficientAccessLevel = 106,
        InvalidApiKey = 107,
        MissingGrant = 108
    }
    
    public class AuthenticationSession {
        public bool ActualPremiumStatus { get; set; }
        public bool EffectivePremiumStatus { get; set; }
        public string EmailAddress { get; set; }
        public string SessionId { get; set; }
        public int SubscriptionToken { get; set; }
        public string Token { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; }
    }
}
