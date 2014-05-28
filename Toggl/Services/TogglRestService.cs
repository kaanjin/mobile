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

        public async Task<bool> Signup(string email, string password)
        {
            var tcs = new TaskCompletionSource<bool>();
            var model = new UserModel {email = email, password = password, timezone = "UTC", at = DateTime.MinValue};
            var json = _jsonConverter.SerializeObject(model);
            _restClient.MakeRequestFor<UserModel>(new MvxStringRestRequest("http://toggl.com/somesignup", json), r => tcs.SetResult(true), e => tcs.SetResult(false));         
            await tcs.Task;
            return tcs.Task.Result;
        }

        public async Task<bool> Signin(string email, string password)
        {
            var tcs = new TaskCompletionSource<bool>();
            var model = new UserModel { email = email, password = password };
            var json = _jsonConverter.SerializeObject(model);
            _restClient.MakeRequestFor<UserModel>(new MvxStringRestRequest("http://toggl.com/somesignin", json), r => tcs.SetResult(true), e => tcs.SetResult(true));
            await tcs.Task;
            return tcs.Task.Result;
        }
    }

    public interface ITogglRestService
    {
        Task<bool> Signup(string email, string password);
        Task<bool> Signin(string email, string password);
    }
}
