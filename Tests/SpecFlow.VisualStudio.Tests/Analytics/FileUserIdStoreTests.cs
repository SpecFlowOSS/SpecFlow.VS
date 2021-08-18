using System.IO.Abstractions;
using FluentAssertions;
using Moq;
using SpecFlow.VisualStudio.Analytics;
using Xunit;

namespace SpecFlow.VisualStudio.Tests.Analytics
{
    public class FileUserIdStoreTests
    {
        private const string UserId = "491ed5c0-9f25-4c27-941a-19b17cc81c87";
        private const string UserIdInRegistry = "{491ed5c0-9f25-4c27-941a-19b17cc81c87}";        
        Mock<IRegistryManager> registryManagerStub;     
        Mock<IFileSystem> fileSystemStub;        

        [Fact]
        public void Should_GetUserIdFromFile_WhenFileExists()
        {
            var sut = CreateSut();

            GivenFileExists();
            GivenUserIdStringInFile(UserId);

            string userId = sut.GetUserId();

            userId.Should().Be(UserId);
        }

        [Fact]
        public void Should_GetUserIdFromRegistry_WhenFileDoesNotExist()
        {
            var sut = CreateSut();

            GivenFileDoesNotExists();
            GivenUserIdStringInRegistry(UserIdInRegistry);

            string userId = sut.GetUserId();

            userId.Should().Be(UserId);
        }

        [Fact]
        public void Should_MigrateExistingUserIdFromRegistryToFile_WhenFileDoesNotExist()
        {
            var sut = CreateSut();

            GivenFileDoesNotExists();
            GivenUserIdStringInRegistry(UserIdInRegistry);

            string userId = sut.GetUserId();

            userId.Should().Be(UserId);
        }

        [Fact]
        public void Should_NotMigrateNotValidUserIdFromRegistryToFile()
        {
            var sut = CreateSut();

            var notValidGuid = "not valid guid";

            GivenFileDoesNotExists();
            GivenUserIdStringInRegistry(notValidGuid);

            string userId = sut.GetUserId();

            userId.Should().NotBe(notValidGuid);
        }

        [Fact]
        public void Should_NotMigrateExistingUserIdFromRegistryToFile_WhenFileExists()
        {
            var sut = CreateSut();

            GivenFileExists();
            GivenUserIdStringInFile(UserId);

            string userId = sut.GetUserId();

            userId.Should().Be(UserId);
        }

        [Fact]
        public void Should_GenerateNewUserId_WhenNoUserIdExists()
        {
            var sut = CreateSut();

            GivenFileDoesNotExists();
            GivenNoUserIdStringInRegistry();

            string userId = sut.GetUserId();

            userId.Should().NotBeEmpty();
        }

        [Fact]
        public void Should_PersistUserId_WhenUserIdExistsInRegistry()
        {
            var sut = CreateSut();

            GivenFileDoesNotExists();
            GivenUserIdStringInRegistry(UserIdInRegistry);

            string userId = sut.GetUserId();

            fileSystemStub.Verify(fileSystem => fileSystem.File.WriteAllText(FileUserIdStore.UserIdFilePath, userId), Times.Once());
        }

        [Fact]
        public void Should_PersistNewlyGeneratedUserId_WhenNoUserIdExists()
        {
            var sut = CreateSut();

            GivenFileDoesNotExists();
            GivenNoUserIdStringInRegistry();

            string userId = sut.GetUserId();

            fileSystemStub.Verify(fileSystem => fileSystem.File.WriteAllText(FileUserIdStore.UserIdFilePath, userId), Times.Once());
        }


        public FileUserIdStore CreateSut()
        {
            registryManagerStub = new Mock<IRegistryManager>();
            fileSystemStub = new Mock<IFileSystem>();
            return new FileUserIdStore(registryManagerStub.Object, fileSystemStub.Object);
        }

        private void GivenFileExists()
        {
            fileSystemStub.Setup(fileSystem => fileSystem.File.Exists(It.IsAny<string>())).Returns(true);
        }

        private void GivenFileDoesNotExists()
        {
            fileSystemStub.Setup(fileSystem => fileSystem.File.Exists(It.IsAny<string>())).Returns(false);
            fileSystemStub.Setup(fileSystem => fileSystem.Directory.Exists(It.IsAny<string>())).Returns(true);
        }

        private void GivenUserIdStringInFile(string userIdString)
        {
            fileSystemStub.Setup(fileSystem => fileSystem.File.ReadAllText(It.IsAny<string>())).Returns(userIdString);
        }

        private void GivenUserIdStringInRegistry(string userIdString)
        {
            registryManagerStub.Setup(rm =>
                    rm.GetValueForKey(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(userIdString);
        }

        private void GivenNoUserIdStringInRegistry()
        {
            registryManagerStub.Setup(rm =>
                    rm.GetValueForKey(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(null);
        }
    }
}
