using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using RestEase;

namespace GitMC.Lib.Curse
{
    public class CurseStatus
    {
        public string CurseRestProxy { get; set; }
        public string LoginService { get; set; }
        public string AddOnService { get; set; }
    }
    
    public class CurseProxy
    {
        public interface ICurseProxyApi
        {
            [Get("")]
            Task<Response<string>> Home();
            
            [Get("status")]
            Task<Response<CurseStatus>> Status();
            
            [Post("authenticate")]
            Task<Response<LoginResponse>> Authenticate([Body] LoginRequest auth);
            
            [Header("Authorization")]
            string Authorization { get; set; }
    
            [Get("addon/{addonId}")]
            [AllowAnyStatusCode]
            Task<Response<Addon>> Addon([Path("addonId")] int addon);
            
            [Get("addon/{addonId}/description")]
            [AllowAnyStatusCode]
            Task<Response<AddonDescription>> AddonDescription([Path("addonId")] int addon);
            
            [Get("addon/{addonId}/files")]
            [AllowAnyStatusCode]
            Task<Response<AddonFiles>> AddonFiles([Path("addonId")] int addon);
            
            [Get("addon/{addonId}/file/{file_id}")]
            [AllowAnyStatusCode]
            Task<Response<AddonFile>> AddonFile([Path("addonId")] int addon, [Path("file_id")] int file);
            
            [Get("addon/{addonId}/file/{file_id}/changelog")]
            [AllowAnyStatusCode]
            Task<Response<AddonFileChangelog>> AddonFileChangelog([Path("addonId")] int addon, [Path("file_id")] int file);
        }
        
        public static async Task<CurseStatus> GetStatus()
        {
            var settings = new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new DefaultContractResolver
                        { NamingStrategy = new PascalToSnakeCaseStrategy() },
                    MissingMemberHandling = MissingMemberHandling.Error };
            var api = new RestClient("https://curse-rest-proxy.azurewebsites.net/api")
                { JsonSerializerSettings = settings }
                .For<ICurseProxyApi>();
            var response = await api.Status();
            return response.GetContent();
        }
        
        private static Lazy<Task<ICurseProxyApi>> LazyApi = new Lazy<Task<ICurseProxyApi>>(async () => {
            var settings = new JsonSerializerSettings {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new DefaultContractResolver
                    { NamingStrategy = new PascalToSnakeCaseStrategy() },
                MissingMemberHandling = MissingMemberHandling.Error };
            var api = new RestClient("https://curse-rest-proxy.azurewebsites.net/api")
                { JsonSerializerSettings = settings }
                .For<ICurseProxyApi>();
            return await Authenticate(api);
        });
        
        private static async Task<ICurseProxyApi> Authenticate(ICurseProxyApi api = null) 
        {
            if (api == null) api = await LazyApi.Value;
            
            Console.WriteLine("Authenticate");
            
            string path = Path.Combine(Constants.ConfigPath, "curse_auth.yaml");
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
                
            // TODO: ask for username / password -> save to config
            
            LoginRequest auth;
            using (var reader = new StreamReader(File.OpenRead(path)))
                auth = deserializer.Deserialize<LoginRequest>(reader);
            
            var response = await api.Authenticate(auth);
            var authResponse = response.GetContent();
            
            var token = $"Token { authResponse.Session.UserId }:{ authResponse.Session.Token }";
            if (authResponse.Status != AuthenticationStatus.Success)
                throw new Exception($"authentication failed with status { authResponse.Status } ");
            api.Authorization = token;
            
            Console.WriteLine($"auth token received: { token } "); // TODO: log debug
            return api;
        }
        
        // Get Addon
        
        private static readonly ConcurrentDictionary<int, Task<Addon>> _addonTasks =
            new ConcurrentDictionary<int, Task<Addon>>();
        
        public static Task<Addon> GetAddon(int addonId) =>
            _addonTasks.GetOrAdd(addonId, id => GetAddonInternal(id));
        
        private static async Task<Addon> GetAddonInternal(int addonId)
        {
           var api = await LazyApi.Value;
            
            Console.WriteLine($"getAddon { addonId }"); // TODO: verbose logging
            var response = await api.Addon(addonId);
            if (!response.ResponseMessage.IsSuccessStatusCode) {
                if (response.ResponseMessage.StatusCode == HttpStatusCode.Unauthorized) {
                    await Authenticate();
                    return await GetAddonInternal(addonId);
                }
                throw new Exception($"getAddon { addonId } failed with status code { response.ResponseMessage.StatusCode }");
            }
            var addon = response.GetContent();
            
            return addon;
        }
        
        // Get Addon Description
        
        private static readonly ConcurrentDictionary<int, Task<AddonDescription>> _addonDescriptionTasks =
            new ConcurrentDictionary<int, Task<AddonDescription>>();
        
        public static Task<AddonDescription> GetAddonDescription(int addonId) =>
            _addonDescriptionTasks.GetOrAdd(addonId, id => GetAddonDescriptionInternal(id));
        
        private static async Task<AddonDescription> GetAddonDescriptionInternal(int addonId)
        {
            var api = await LazyApi.Value;
            
            Console.WriteLine($"getAddonDescption { addonId }"); // TODO: verbose logging
            var response = await api.AddonDescription(addonId);
            if (!response.ResponseMessage.IsSuccessStatusCode) {
                if (response.ResponseMessage.StatusCode == HttpStatusCode.Unauthorized) {
                    await Authenticate();
                    return await GetAddonDescriptionInternal(addonId);
                }
                throw new Exception($"getAddonDescption { addonId } failed with status code { response.ResponseMessage.StatusCode }");
            }
            var addonDescription = response.GetContent();
            
            return addonDescription;
        }
        
        // Get Addon Files
        
        private static readonly ConcurrentDictionary<int, Task<AddonFiles>> AddonFilesTasks =
            new ConcurrentDictionary<int, Task<AddonFiles>>();
        
        public static Task<AddonFiles> GetAddonFiles(int addonId) =>
            AddonFilesTasks.GetOrAdd(addonId, id => GetAddonFilesInternal(id));
        
        private static async Task<AddonFiles> GetAddonFilesInternal(int addonId)
        {
            var api = await LazyApi.Value;
            
            Console.WriteLine($"getAddonFiles { addonId }"); // TODO: verbose logging
            var response = await api.AddonFiles(addonId);
            if (!response.ResponseMessage.IsSuccessStatusCode) {
                if (response.ResponseMessage.StatusCode == HttpStatusCode.Unauthorized) {
                    await Authenticate();
                    return await GetAddonFilesInternal(addonId);
                }
                throw new Exception($"GetAddonFiles { addonId } failed with status code { response.ResponseMessage.StatusCode }");
            }
            var addonFiles = response.GetContent();
            
            return addonFiles;
        }
        
        // Get Addon File
        
        private static readonly ConcurrentDictionary<Tuple<int, int>, Task<AddonFile>> AddonFileTasks =
            new ConcurrentDictionary<Tuple<int, int>, Task<AddonFile>>();
        
        public static Task<AddonFile> GetAddonFile(int addonId, int fileId) =>
            AddonFileTasks.GetOrAdd(Tuple.Create(addonId, fileId), id => GetAddonFileInternal(id.Item1, id.Item2));
        
        private static async Task<AddonFile> GetAddonFileInternal(int addonId, int fileId) 
        {
            var api = await LazyApi.Value;
            
            Console.WriteLine($"getAddonFile { addonId } { fileId }"); // TODO: verbose logging
            var response = await api.AddonFile(addonId, fileId);
            if (!response.ResponseMessage.IsSuccessStatusCode) {
                if (response.ResponseMessage.StatusCode == HttpStatusCode.Unauthorized) {
                    await Authenticate();
                    return await GetAddonFileInternal(addonId, fileId);
                }
                throw new Exception($"GetAddonFile { addonId } { fileId } failed with status code { response.ResponseMessage.StatusCode }");
            }
            var addonFile = response.GetContent();
            
            return addonFile;
        }
        
        // Get Addon File Changelog
        
        private static readonly ConcurrentDictionary<Tuple<int, int>, Task<AddonFileChangelog>> AddonFileChangelogTasks =
            new ConcurrentDictionary<Tuple<int, int>, Task<AddonFileChangelog>>();
        
        public static Task<AddonFileChangelog> GetAddonFileChangelog(int addonId, int fileId) =>
            AddonFileChangelogTasks.GetOrAdd(Tuple.Create(addonId, fileId), id => GetAddonFileChangelogInternal(id.Item1, id.Item2));
        
        private static async Task<AddonFileChangelog> GetAddonFileChangelogInternal(int addonId, int fileId) 
        {
           var api = await LazyApi.Value;
            
            Console.WriteLine($"getAddonFileChangelog { addonId } { fileId }"); // TODO: verbose logging
            var response = await api.AddonFileChangelog(addonId, fileId);
            if (!response.ResponseMessage.IsSuccessStatusCode) {
                if (response.ResponseMessage.StatusCode == HttpStatusCode.Unauthorized) {
                    await Authenticate();
                    return await GetAddonFileChangelogInternal(addonId, fileId);
                }
                throw new Exception($"GetAddonFileChangelog { addonId } { fileId } failed with status code { response.ResponseMessage.StatusCode }");
            }
            var addonFileChangelog = response.GetContent();
            
            return addonFileChangelog;
        }
    }
}
