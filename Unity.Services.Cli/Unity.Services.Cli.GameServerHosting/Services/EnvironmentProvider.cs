using System;
namespace Unity.Services.Cli.GameServerHosting.Services
{
    public class EnvironmentProvider : IEnvironment
    {
        public string GetUserHomeDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
    }
}
