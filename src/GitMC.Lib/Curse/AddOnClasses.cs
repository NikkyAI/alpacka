using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GitMC.Lib.Curse
{
    public class Addon
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public PackageTypes PackageType { get; set; }
        public string Summary { get; set; }
        public string WebSiteURL { get; set; }
        public List<AddOnAttachment> Attachments { get; set; }
        public List<AddOnAuthor> Authors { get; set; }
        public string AvatarUrl { get; set; }
        public List<AddOnCategory> Categories { get; set; }
        public CategorySection CategorySection { get; set; }
        public int CommentCount { get; set; }
        public int DefaultFileId { get; set; }
        public string DonationUrl { get; set; }
        public float DownloadCount { get; set; }
        public string ExternalUrl { get; set; }
        public int GameId { get; set; }
        public int IconId { get; set; }
        public int InstallCount { get; set; }
        public int IsFeatured { get; set; }
        
        public List<AddonFile> LatestFiles { get; set; }
        public List<GameVersionLatestFile> GameVersionLatestFiles { get; set; }
        
        public int Likes { get; set; }
        public double PopularityScore { get; set; }
        public string PrimaryAuthorName { get; set; }
        public string PrimaryCategoryAvatarUrl { get; set; }
        public int PrimaryCategoryId { get; set; }
        public string PrimaryCategoryName { get; set; }
        public int Rating { get; set; }
        public ProjectStage Stage { get; set; }
        
        public ProjectStatus Status { get; set; }
        public int GamePopularityRank { get; set; }
    }
    
    public enum PackageTypes
    {
        Folder = 1,
        Ctoc = 2,
        SingleFile = 3,
        Cmod2 = 4,
        ModPack = 5,
        Mod = 6
    }
    
    public class AddOnAttachment
    {
        public string Description { get; set; }
        public bool IsDefault { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }
    
     public class AddOnCategory {
        public int Id { get; set; }
        public string Name { get; set; }
        [JsonProperty("url")]
        public string URL { get; set; }
    }
    
    public class AddOnAuthor {
        public string Name { get; set; }
        public string Url { get; set; }
    }
    
    public class GameVersionLatestFile
    {
        public ReleaseType FileType { get; set; }
        public string GameVesion { get; set; }
        public int ProjectFileId { get; set; }
        public string ProjectFileName { get; set; }
    }
    
    public class CategorySection {
        public string ExtraIncludePattern { get; set; }
        public int GameID { get; set; }
        public int ID { get; set; }
        public string InitialInclusionPattern { get; set; }
        public string Name { get; set; }
        public string PackageType { get; set; }
        public string Path { get; set; }
    }
    
    public class AddonFile
    {
        public int AlternateFileId { get; set; }
        public List<AddOnFileDependency> Dependencies { get; set; }
        public string DownloadURL { get; set; }
        public DateTime FileDate { get; set; }
        public string FileName { get; set; }
        public string FileNameOnDisk { get; set; }
        public FileStatus FileStatus { get; set; }
        public List<String> GameVersion { get; set; }
        public int Id { get; set; }
        public bool IsAlternate { get; set; }
        public bool IsAvailable { get; set; }
        public List<AddOnModule> Modules { get; set; }
        public long PackageFingerprint { get; set; }
        public ReleaseType ReleaseType { get; set; }
        
    }
    
    public class AddOnFileDependency
    {
        public int AddOnId { get; set; }
        public DependencyType Type { get; set; }
    }
    
    public enum ProjectStage
    {
        Alpha = 1,
        Beta = 2,
        Deleted = 3,
        Inactive = 4,
        Mature = 5,
        Planning = 6,
        Release = 7,
        Abandoned = 8
    }
    
    public enum ReleaseType
    {
        Release = 1,
        Beta = 2,
        Alpha = 3
    }
    
    public enum DependencyType
    {
        Required = 1,
        Optional = 2,
        Embedded = 3
    }
    
    public class AddOnModule
    {
        public long Fingerprint { get; set; }
        public string Foldername { get; set; }
    }
    
    public enum ProjectStatus
    {
        Normal = 1,
        Hidden = 2,
        Deleted = 3
    }
    
    public enum FileStatus
    {
        Normal = 1,
        SemiNormal = 2,
        Reported = 3,
        Malformed = 4,
        Locked = 5,
        InvalidLayout = 6,
        Hidden = 7,
        NeedsApproval = 8,
        Deleted = 9,
        UnderReview = 10,
        MalwareDetected = 11,
        WaitingOnProject = 12,
        ClientOnly = 13
    }
    
    public class AddonDescription
    {
        public string Description { get; set; }
    }
    
    public class AddonFiles
    {
        public List<AddonFile> Files { get; set; }
    }
    
    public class AddonFileChangelog
    {
        public string changelog { get; set; }
    }
}
