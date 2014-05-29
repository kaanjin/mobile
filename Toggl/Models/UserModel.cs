using System;

namespace Toggl.Core.Models
{
    public class UserModel
    {
        public User user { get; set; }
    }

    public class User
    {
        public string fullname { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public int beginning_of_week { get; set; }
        public string date_format { get; set; }
        public string timeofday_format { get; set; }
        public string image_url { get; set; }
        public string language { get; set; }
        public string timezone { get; set; }
        public bool send_product_emails { get; set; }
        public bool send_weekly_report { get; set; }
        public bool store_start_and_stop_time { get; set; }
        public string created_with { get; set; }
        public string default_wid { get; set; }
        public DateTime at { get; set; }
        public string api_token { get; set; }
    }

    public class Wrapper<T>
    {
        public T data { get; set; }
        public long? since { get; set; }
    }
}
