using System;

namespace Toggl.Core.Models
{
    public class UserModel
    {
        public string fullname { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string timezone { get; set; }
        public DateTime at { get; set; }
        public string api_token { get; set; }
    }
}
