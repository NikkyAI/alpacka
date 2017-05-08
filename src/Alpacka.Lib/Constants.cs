using System;
using System.IO;

namespace Alpacka.Lib
{
    public static class Constants
    {
        public static readonly string MC_DIR   = "minecraft";
        public static readonly string MC_MODS_DIR   = Path.Combine(MC_DIR, "mods");
        public static readonly string MC_CONFIG_DIR = Path.Combine(MC_DIR, "config");
        
        // TODO: Rename files to "alpacka.packxxx.yyy"?
        public static readonly string PACK_CONFIG_FILE = "packconfig.yaml";
        public static readonly string PACK_BUILD_FILE  = "packbuild.json";
        
        public static readonly string INSTANCE_INFO_FILE = "alpacka.instance.json";
        
        public static string CachePath { get; } = Path.Combine(
            Environment.GetEnvironmentVariable("LocalAppData")
                ?? Environment.GetEnvironmentVariable("XDG_CACHE_HOME")
                ?? Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".cache")
            , "alpacka");
        
        public static string ConfigPath { get; } = Path.Combine(
            Environment.GetEnvironmentVariable("AppData")
                ?? Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                ?? Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".config")
            , "alpacka");
    }
}
