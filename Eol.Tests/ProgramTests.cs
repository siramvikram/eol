using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using Xunit;

public class ProgramTests
{
    [Fact]
    public void GetOperatingSystemNames_ReturnsExpectedNames()
    {
        var expected = new HashSet<string> { "alpine", "amazon-linux", "android" };
        var actual = Program.GetOperatingSystemNames();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task GetOperatingSystemInfo_ReturnsExpectedResponse()
    {
        var osName = "alpine";
        var expectedResponse = "[{\"cycle\":\"3.12\",\"releaseDate\":\"2020-05-29\",\"eol\":\"2022-11-01\"}]";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(expectedResponse),
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var actualResponse = await Program.GetOperatingSystemInfo(osName, httpClient);

        Assert.Equal(expectedResponse, actualResponse);
    }

    [Fact]
    public void ValidateEol_ReturnsExpectedMessages()
    {
        var osName = "alpine";
        var apiResponse = "[{\"cycle\":\"3.12\",\"releaseDate\":\"2020-05-29\",\"eol\":\"2022-11-01\"}]";
        var expectedMessages = new List<string> { "OSName: alpine, EOL: 2022-11-01, Days until EOL: -1" };

        var actualMessages = Program.ValidateEol(osName, apiResponse);

        Assert.Equal(expectedMessages, actualMessages);
    }

    [Fact]
    public async Task CreateGitHubIssue_CreatesIssueSuccessfully()
    {
        var title = "EOL List - 2022-11-01 12:00";
        var body = "Test issue body";

        var clientMock = new Mock<IGitHubClient>();
        var issuesClientMock = new Mock<IIssuesClient>();
        clientMock.SetupGet(c => c.Issue).Returns(issuesClientMock.Object);

        await Program.CreateGitHubIssue(title, body, clientMock.Object);

        issuesClientMock.Verify(i => i.Create("siramvikram", "Code", It.Is<NewIssue>(ni => ni.Title == title && ni.Body == body)), Times.Once);
    }
}