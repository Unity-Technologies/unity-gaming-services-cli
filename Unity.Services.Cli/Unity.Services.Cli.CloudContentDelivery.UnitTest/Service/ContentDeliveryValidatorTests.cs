using Moq;
using NUnit.Framework;
using Unity.Services.Cli.CloudContentDelivery.Service;
using Unity.Services.Cli.Common.Exceptions;
using Unity.Services.Cli.Common.Models;
using Unity.Services.Cli.Common.Validator;

namespace CloudContentDeliveryTest.Service

{
    [TestFixture]
    public class ContentDeliveryValidatorTests
    {

        readonly Mock<IConfigurationValidator> m_ConfigValidator = new();
        ContentDeliveryValidator m_ContentDeliveryValidator = null!;

        [SetUp]
        public void Setup()
        {
            m_ContentDeliveryValidator = new ContentDeliveryValidator(m_ConfigValidator.Object);
        }

        [Test]
        public void ValidateProjectIdAndEnvironmentId_ValidIds_DoesNotThrowException()
        {
            Assert.DoesNotThrow(
                () => m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(
                    CloudContentDeliveryTestsConstants.ProjectId,
                    CloudContentDeliveryTestsConstants.EnvironmentId));
        }

        [Test]
        public void ValidateProjectIdAndEnvironmentId_InvalidProjectId_ThrowsConfigValidationException()
        {
            m_ConfigValidator.Setup(v => v.ThrowExceptionIfConfigInvalid(Keys.ConfigKeys.ProjectId, ""))
                .Throws(new ConfigValidationException(Keys.ConfigKeys.ProjectId, "", It.IsAny<string>()));
            Assert.Throws<ConfigValidationException>(
                () => m_ContentDeliveryValidator.ValidateProjectIdAndEnvironmentId(
                    "",
                    CloudContentDeliveryTestsConstants.EnvironmentId));
        }

        [Test]
        public void ValidateBucketId_ValidBucketId_DoesNotThrowException()
        {
            Assert.DoesNotThrow(
                () => m_ContentDeliveryValidator.ValidateBucketId(CloudContentDeliveryTestsConstants.BucketId));
        }

        [Test]
        public void ValidateEntryId_ValidEntryId_DoesNotThrowException()
        {
            Assert.DoesNotThrow(
                () => m_ContentDeliveryValidator.ValidateEntryId(CloudContentDeliveryTestsConstants.EntryId));
        }

        [Test]
        public void ValidateEntryId_InvalidEntryId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => m_ContentDeliveryValidator.ValidateEntryId(""));
        }

        [Test]
        public void ValidatePath_ValidPath_DoesNotThrowException()
        {
            const string path = "folder/file.jpg";
            Assert.DoesNotThrow(() => m_ContentDeliveryValidator.ValidatePath(path));
        }

        [Test]
        public void ValidatePath_InvalidPath_ThrowsArgumentException()
        {
            string? path = null;
            Assert.Throws<ArgumentException>(() => m_ContentDeliveryValidator.ValidatePath(path));
        }
    }
}
