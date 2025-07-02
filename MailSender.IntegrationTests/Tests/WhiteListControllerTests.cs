using System.Net;
using System.Text;
using Dictionaries.DTO;
using Dictionaries.Enums;
using Dictionaries.Extensions;
using MailSender.Domain.DTOs.EntitiesDTO;
using MailSender.IntegrationTests.Base;
using Newtonsoft.Json;
using static MailSender.TestConstants.Constants;

namespace MailSender.IntegrationTests.Tests
{
    public class WhiteListControllerTests
        : IClassFixture<TestWebApplicationFactory>
    {
        public const string BaseEndpoint = "/whitelist";

        private readonly TestWebApplicationFactory _factory;

        public WhiteListControllerTests(TestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetAll_NotEmpty()
        {
            // Arrange
            Guid id = await GetCreatedSuccessWhiteList();
            HttpClient client = _factory.CreateClient();

            // Act

            string json = JsonConvert.SerializeObject(
                new QueryDataDTO
                {
                    Pagination = new()
                    {
                        Page = 0,
                        ItemCount = 10
                    }
                });

            StringContent content = new(json, Encoding.UTF8, JsonMediaType);


            HttpResponseMessage response = await client.PostAsync($"{BaseEndpoint}/Get", content);
            PaginatiedDataDTO<WhiteListDTO> result = await response.Content.GetFromJsonAsync<PaginatiedDataDTO<WhiteListDTO>>();

            // Assert
            Assert.True(HttpStatusCode.OK == response.StatusCode, await response.Content.ReadAsStringAsync());
            Assert.NotEmpty(result.Data);
            Assert.Contains(result.Data, x => x.Id.Equals(id));
        }

        [Fact]
        public async Task GetById_Success()
        {
            // Arrange
            Guid whiteListId = await GetCreatedSuccessWhiteList();

            HttpClient client = _factory.CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync($"{BaseEndpoint}/{whiteListId}");
            WhiteListDTO whiteList = await response.Content.GetFromJsonAsync<WhiteListDTO>();

            // Assert
            Assert.True(HttpStatusCode.OK == response.StatusCode, await response.Content.ReadAsStringAsync());
            Assert.NotNull(whiteList);
        }

        [Fact]
        public async Task Create_Success()
        {
            // Act
            HttpResponseMessage response = await CreateWhiteList();
            ResponseDTO<Guid> whiteListId = await response.Content.GetFromJsonAsync<ResponseDTO<Guid>>();

            // Assert
            Assert.True(HttpStatusCode.OK == response.StatusCode, await response.Content.ReadAsStringAsync());
            Assert.Equal(MessageType.Success, whiteListId.MessageType);
        }

        [Fact]
        public async Task CreateSeveral_Success()
        {
            // Act
            HttpResponseMessage firstResponse = await CreateWhiteList();

            ResponseDTO<Guid> firstWhiteListId = await firstResponse.Content.GetFromJsonAsync<ResponseDTO<Guid>>();
            WhiteListDTO firstWhiteList = await GetWhiteListById(firstWhiteListId.Data);

            HttpResponseMessage secondResponse = await CreateWhiteList();

            ResponseDTO<Guid> secondWhiteListId = await secondResponse.Content.GetFromJsonAsync<ResponseDTO<Guid>>();
            WhiteListDTO secondWhiteList = await GetWhiteListById(secondWhiteListId.Data);

            // Assert
            Assert.True(HttpStatusCode.OK == firstResponse.StatusCode, await firstResponse.Content.ReadAsStringAsync());
            Assert.True(HttpStatusCode.OK == secondResponse.StatusCode, await secondResponse.Content.ReadAsStringAsync());

            Assert.NotNull(firstWhiteList);
            Assert.NotNull(secondWhiteList);
        }

        [Fact]
        public async Task Delete_Success()
        {
            // Act
            HttpResponseMessage response = await CreateWhiteList();

            ResponseDTO<Guid> whiteListId = await response.Content.GetFromJsonAsync<ResponseDTO<Guid>>();

            HttpClient client = _factory.CreateClient();

            var httpRequestMessage = new HttpRequestMessage
            {
                Content = new StringContent(JsonConvert.SerializeObject(new[] { whiteListId.Data.ToString() }), Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"{BaseEndpoint}", UriKind.Relative)
            };
            HttpResponseMessage deleteResponse = await client.SendAsync(httpRequestMessage);

            HttpResponseMessage getByIdresponse = await client.GetAsync($"{BaseEndpoint}/{whiteListId.Data}");

            // Assert
            Assert.True(HttpStatusCode.OK == deleteResponse.StatusCode, await deleteResponse.Content.ReadAsStringAsync());
            Assert.True(HttpStatusCode.NotFound == getByIdresponse.StatusCode, await getByIdresponse.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task UpdatePut_AllFields()
        {
            // Act
            string newEmail = "mailnew@mail.ru";
            string newDescription = "newDescription";

            HttpResponseMessage createResponse = await CreateWhiteList();

            ResponseDTO<Guid> whiteListId = await createResponse.Content.GetFromJsonAsync<ResponseDTO<Guid>>();
            WhiteListDTO assertWhiteList = await GetWhiteListById(whiteListId.Data);

            WhiteListUpdateDTO createdMailTemplate = new()
            {
                Email = newEmail,
                Description = newDescription
            };

            HttpClient client = _factory.CreateClient();

            string json = JsonConvert.SerializeObject(createdMailTemplate);
            StringContent content = new(json, Encoding.UTF8, JsonMediaType);

            HttpResponseMessage updateResponse = await client.PutAsync($"{BaseEndpoint}/{whiteListId.Data}", content);

            WhiteListDTO updatedWhiteList = await GetWhiteListById(whiteListId.Data);

            // Assert
            Assert.True(HttpStatusCode.OK == updateResponse.StatusCode, await updateResponse.Content.ReadAsStringAsync());
            Assert.NotEqual(assertWhiteList.Email, updatedWhiteList.Email);
            Assert.NotEqual(assertWhiteList.Description, updatedWhiteList.Description);
            Assert.Equal(newEmail, updatedWhiteList.Email);
            Assert.Equal(newDescription, updatedWhiteList.Description);
        }

        private async Task<HttpResponseMessage> CreateWhiteList()
        {
            HttpClient client = _factory.CreateClient();
            string prefix = Guid.NewGuid().ToString().Replace("-", "");
            WhiteListCreateDTO whiteList = new()
            {
                Email = $"ivanivanov{prefix}@mail.ru",
                Description = "Ivan Ivanov mail, Test",
            };

            string json = JsonConvert.SerializeObject(whiteList);
            StringContent content = new(json, Encoding.UTF8, JsonMediaType);
            return await client.PostAsync($"{BaseEndpoint}", content);
        }

        private async Task<Guid> GetCreatedSuccessWhiteList()
        {
            HttpResponseMessage response = await CreateWhiteList();
            ResponseDTO<Guid> whiteListId = await response.Content.GetFromJsonAsync<ResponseDTO<Guid>>();
            return whiteListId.Data;
        }

        private async Task<WhiteListDTO> GetWhiteListById(Guid id)
        {
            HttpClient client = _factory.CreateClient();
            HttpResponseMessage response = await client.GetAsync($"{BaseEndpoint}/{id}");
            return await response.Content.GetFromJsonAsync<WhiteListDTO>();
        }
    }
}