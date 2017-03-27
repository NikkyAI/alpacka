using System;
using System.Threading.Tasks;
using System.IO;
using RestEase;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Newtonsoft.Json.Serialization;
using System.Net;
using System.Collections.Concurrent;

namespace GitMC.Lib.Curse
{
    //TODO: move or remove 
    public static class PrettyPrintExtensions
    {
        private static JsonSerializerSettings settings = 
            new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
        public static string ToPrettyJson(this object obj) => JsonConvert.SerializeObject(obj, settings);
    }
    
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
    
            [Get("addon/{addon_id}")]
            [AllowAnyStatusCode]
            Task<Response<Addon>> Addon([Path("addon_id")] int addon);
            
            [Get("addon/{addon_id}/description")]
            [AllowAnyStatusCode]
            Task<Response<AddonDescription>> AddonDescription([Path("addon_id")] int addon);
            
            [Get("addon/{addon_id}/files")]
            [AllowAnyStatusCode]
            Task<Response<AddonFiles>> AddonFiles([Path("addon_id")] int addon);
            
            [Get("addon/{addon_id}/file/{file_id}")]
            [AllowAnyStatusCode]
            Task<Response<AddonFile>> AddonFile([Path("addon_id")] int addon, [Path("file_id")] int file);
            
            [Get("addon/{addon_id}/file/{file_id}/changelog")]
            [AllowAnyStatusCode]
            Task<Response<AddonFileChangelog>> AddonFileChangelog([Path("addon_id")] int addon, [Path("file_id")] int file);
        }
        
        public static async Task<CurseStatus> GetStatus() 
        {
            var settings = new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new DefaultContractResolver {
                            NamingStrategy = new PascalToSnakeCaseStrategy()
                        },
                    MissingMemberHandling = MissingMemberHandling.Error
                };
            var api = new RestClient("https://curse-rest-proxy.azurewebsites.net/api")
                {
                    JsonSerializerSettings = settings
                }.For<ICurseProxyApi>();
            var response = await api.Status();
            var status = response.GetContent();
            return status;
        }
        
        private static Lazy<Task<ICurseProxyApi>> LazyApi = new Lazy<Task<ICurseProxyApi>>(async () => 
        {
            var settings = new JsonSerializerSettings {
                    NullValueHandling = NullValueHandling.Ignore,
                    ContractResolver = new DefaultContractResolver {
                            NamingStrategy = new PascalToSnakeCaseStrategy()
                        },
                    MissingMemberHandling = MissingMemberHandling.Error
                };
                var api = new RestClient("https://curse-rest-proxy.azurewebsites.net/api")
                {
                    JsonSerializerSettings = settings
                }.For<ICurseProxyApi>();
            return await Authenticate(api);
        });
        
        
        private static async Task<ICurseProxyApi> Authenticate(ICurseProxyApi api = null) 
        {
            if (api == null)
                api = await LazyApi.Value;
            
            Console.WriteLine("Authenticate");
            
            string path = Path.Combine(Constants.ConfigPath, "curse_auth.yaml");
            var deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
                
            //TODO: ask for username / password -> save to config
            
            LoginRequest auth;
            using (var reader = new StreamReader(File.OpenRead(path)))
                auth = deserializer.Deserialize<LoginRequest>(reader);
            
            var response = await api.Authenticate(auth);
            var authResponse = response.GetContent();
            
            var token = $"Token {authResponse.Session.UserId}:{authResponse.Session.Token}";
            if(authResponse.Status != AuthenticationStatus.Success) {
                throw new Exception($"authentication failed with status { authResponse.Status } ");
            }
            api.Authorization = token;
            
            Console.WriteLine($"auth token received: { token } "); //TODO: log debug
            return api;
        }
        
        private static ConcurrentDictionary<int, Task<Addon>> AddonTasks = new ConcurrentDictionary<int, Task<Addon>>();
        public static Task<Addon> GetAddon(int addon_id) => AddonTasks.GetOrAdd(addon_id, (id) => _GetAddon(id));
        private static async Task<Addon> _GetAddon(int addon_id) 
        {
           var api = await LazyApi.Value;
            
            Console.WriteLine($"getAddon {addon_id}"); //TODO: verbose logging
            var response = await api.Addon(addon_id);
            if (!response.ResponseMessage.IsSuccessStatusCode)
            {
                if(response.ResponseMessage.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await Authenticate();
                    return await _GetAddon(addon_id);
                }
                throw new Exception($"getAddon {addon_id} failed with status code {response.ResponseMessage.StatusCode}");
            }
            var addon = response.GetContent();
            
            return addon;
        }
        private static ConcurrentDictionary<int, Task<AddonDescription>> AddonDescriptionTasks = new ConcurrentDictionary<int, Task<AddonDescription>>();
        public static Task<AddonDescription> GetAddonDescription(int addon_id) => AddonDescriptionTasks.GetOrAdd(addon_id, (id) => _GetAddonDescription(id));
        private static async Task<AddonDescription> _GetAddonDescription(int addon_id) 
        {
            var api = await LazyApi.Value;
            
            Console.WriteLine($"getAddonDescption {addon_id}"); //TODO: verbose logging
            var response = await api.AddonDescription(addon_id);
            if (!response.ResponseMessage.IsSuccessStatusCode)
            {
                if(response.ResponseMessage.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await Authenticate();
                    return await _GetAddonDescription(addon_id);
                }
                throw new Exception($"getAddonDescption {addon_id} failed with status code {response.ResponseMessage.StatusCode}");
            }
            var addonDescription = response.GetContent();
            
            return addonDescription;
        }
        
        private static ConcurrentDictionary<int, Task<AddonFiles>> AddonFilesTasks = new ConcurrentDictionary<int, Task<AddonFiles>>();
        public static Task<AddonFiles> GetAddonFiles(int addon_id) => AddonFilesTasks.GetOrAdd(addon_id, (id) => _GetAddonFiles(id));
        private static async Task<AddonFiles> _GetAddonFiles(int addon_id) 
        {
            var api = await LazyApi.Value;
            
            Console.WriteLine($"getAddonFiles {addon_id}"); //TODO: verbose logging
            var response = await api.AddonFiles(addon_id);
            if (!response.ResponseMessage.IsSuccessStatusCode)
            {
                if(response.ResponseMessage.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await Authenticate();
                    return await _GetAddonFiles(addon_id);
                }
                throw new Exception($"GetAddonFiles {addon_id} failed with status code {response.ResponseMessage.StatusCode}");
            }
            var addonFiles = response.GetContent();
            
            return addonFiles;
        }
        
        private static ConcurrentDictionary<Tuple<int,int>, Task<AddonFile>> AddonFileTasks = new ConcurrentDictionary<Tuple<int,int>, Task<AddonFile>>();
        public static Task<AddonFile> GetAddonFile(int addon_id, int file_id) => AddonFileTasks.GetOrAdd(Tuple.Create(addon_id, file_id), (id) => _GetAddonFile(id.Item1, id.Item2));
        private static async Task<AddonFile> _GetAddonFile(int addon_id, int file_id) 
        {
            var api = await LazyApi.Value;
            
            Console.WriteLine($"getAddonFile {addon_id} {file_id}"); //TODO: verbose logging
            var response = await api.AddonFile(addon_id, file_id);
            if (!response.ResponseMessage.IsSuccessStatusCode)
            {
                if(response.ResponseMessage.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await Authenticate();
                    return await _GetAddonFile(addon_id, file_id);
                }
                throw new Exception($"GetAddonFile {addon_id} {file_id} failed with status code {response.ResponseMessage.StatusCode}");
            }
            var addonFile = response.GetContent();
            
            return addonFile;
        }
        
        private static ConcurrentDictionary<Tuple<int,int>, Task<AddonFileChangelog>> AddonFileChangelogTasks = new ConcurrentDictionary<Tuple<int,int>, Task<AddonFileChangelog>>();
        public static Task<AddonFileChangelog> GetAddonFileChangelog(int addon_id, int file_id) => AddonFileChangelogTasks.GetOrAdd(Tuple.Create(addon_id, file_id), (id) => _GetAddonFileChangelog(id.Item1, id.Item2));
        private static async Task<AddonFileChangelog> _GetAddonFileChangelog(int addon_id, int file_id) 
        {
           var api = await LazyApi.Value;
            
            Console.WriteLine($"getAddonFileChangelog {addon_id} {file_id}"); //TODO: verbose logging
            var response = await api.AddonFileChangelog(addon_id, file_id);
            if (!response.ResponseMessage.IsSuccessStatusCode)
            {
                if(response.ResponseMessage.StatusCode == HttpStatusCode.Unauthorized)
                {
                    await Authenticate();
                    return await _GetAddonFileChangelog(addon_id, file_id);
                }
                throw new Exception($"GetAddonFileChangelog {addon_id} {file_id} failed with status code {response.ResponseMessage.StatusCode}");
            }
            var addonFileChangelog = response.GetContent();
            
            return addonFileChangelog;
        }
    }

}