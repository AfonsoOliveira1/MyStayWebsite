/*
using Microsoft.AspNetCore.Identity;

namespace Booking.web.Services
{
    public class Authentication
    {
        private IHttpClientFactory _context;
        private PasswordHasher<> _passwordHasher;
        private User _currentuser;

        public Authentication()
        {
            _passwordHasher = new PasswordHasher<User>();
        }
        public User? FinByLogin(string login)
        {
            if (string.IsNullOrWhiteSpace(login))
                return null;

            return _context.User.FirstOrDefault(u => u.UserName.Equals(login) || u.Email.Equals(login));
        }
        public User CurrentUser() => _currentuser;

        public bool Login(string username, string password)
        {
            User user = FinByLogin(username);
            if (user == null) return false;

            PasswordVerificationResult result = _passwordHasher.VerifyHashedPassword(user, user.Passwordhash, password);
            if (result == PasswordVerificationResult.Success)
            {
                _currentuser = user;
                return true;
            }
            return false;
        }

        public void Logout()
        {
            _currentuser = null;
        }
    }
}
*/