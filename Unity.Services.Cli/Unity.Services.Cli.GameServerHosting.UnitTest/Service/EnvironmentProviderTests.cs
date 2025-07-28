using Moq;
using System;
using Unity.Services.Cli.GameServerHosting.Services;

namespace Unity.Services.Cli.GameServerHosting.UnitTest.Services
{
    public class EnvironmentProviderTests
    {
        [Test]
        public void GetUserHomeDirectory_ShouldReturnCorrectUserProfilePath()
        {
            var environmentProvider = new EnvironmentProvider();
            var expectedUserProfilePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

            var result = environmentProvider.GetUserHomeDirectory();

            Assert.That(result, Is.EqualTo(expectedUserProfilePath));
        }
    }
}
