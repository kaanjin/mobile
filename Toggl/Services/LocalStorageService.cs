using Cirrious.CrossCore;
using Cirrious.CrossCore.Platform;
using Cirrious.MvvmCross.Plugins.File;

namespace Toggl.Core.Services
{
    public class LocalStorageService : ILocalStorageService
    {
        private readonly IMvxFileStore _fileStore;
        private readonly IMvxJsonConverter _jsonConverter;

        public LocalStorageService()
        {
            _fileStore = Mvx.Resolve<IMvxFileStore>();
            _jsonConverter = Mvx.Resolve<IMvxJsonConverter>();
        }

        public T Get<T>(string key)
        {
            string contents;
            
            if (_fileStore.TryReadTextFile(key, out contents))
            {
                return _jsonConverter.DeserializeObject<T>(contents);
            }

            return default (T);
        }

        public void Set<T>(string key, T val)
        {
            _fileStore.WriteFile(key, _jsonConverter.SerializeObject(val));
        }
    }

    public interface ILocalStorageService
    {
        T Get<T>(string key);
        void Set<T>(string key, T val);
    }
}
