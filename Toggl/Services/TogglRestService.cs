using System;
using System.Threading.Tasks;
using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.Plugins.Network.Rest;
using Toggl.Core.Models;

namespace Toggl.Core.Services
{
    public class TogglRestService : ITogglRestService
    {
        private readonly IMvxJsonRestClient _restClient;
        private readonly IMvxJsonConverter _jsonConverter;

        public TogglRestService()
        {
            _restClient = Mvx.Resolve<IMvxJsonRestClient>();
            _jsonConverter = Mvx.Resolve<IMvxJsonConverter>();
        }

        public async Task<User> Signup(string email, string password)
        {
            var tcs = new TaskCompletionSource<User>();
            var model = new UserModel
            {
                user = new User
                {
                    email = email,
                    password = password,
                    timezone = "Europe/Tallinn",
                    store_start_and_stop_time = true,
                    created_with = "TogglRoss/7.0.0",
                    at = DateTime.UtcNow       
                }
            };
            _restClient.MakeRequestFor<Wrapper<User>>(BuildRequestFor(model), r => tcs.SetResult(r.Result.data), e => tcs.SetResult(null));         
            await tcs.Task;
            return tcs.Task.Result;
        }

        public async Task<bool> Signin(string email, string password)
        {
            var tcs = new TaskCompletionSource<bool>();
            //var model = new UserModel { email = email, password = password };
            //_restClient.MakeRequestFor<UserModel>(BuildRequestFor(null), r => tcs.SetResult(true), e => tcs.SetResult(true));
            await tcs.Task;
            return tcs.Task.Result;
        }

        private MvxRestRequest BuildRequestFor<T>(T model)
        {
            var json = _jsonConverter.SerializeObject(model);
            var request = new MvxStringRestRequest("https://toggl.com/api/v8/signups", json);
            return request;
        }
    }

    public interface ITogglRestService
    {
        Task<User> Signup(string email, string password);
        Task<bool> Signin(string email, string password);
    }
}
