﻿using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using NSubstitute;
using PactNet.Mocks.MockHttpService.Mappers;
using PactNet.Mocks.MockHttpService.Models;
using Xunit;

namespace PactNet.Tests.Mocks.MockHttpService.Mappers
{
    public class ProviderServiceResponseMapperTests
    {
        [Fact]
        public void Convert_WithNullHttpResponseMessage_ReturnsNull()
        {
            IProviderServiceResponseMapper mapper = new ProviderServiceResponseMapper();

            mapper.Convert(null);
        }

        [Fact]
        public void Convert_WithStatusCode_CorrectlyMapsStatusCode()
        {
            var message = new HttpResponseMessage { StatusCode = HttpStatusCode.Accepted };

            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();

            IProviderServiceResponseMapper mapper = new ProviderServiceResponseMapper(mockHttpBodyContentMapper);

            var result = mapper.Convert(message);

            Assert.Equal(202, result.Status);
        }

        [Fact]
        public void Convert_WithResponseHeaders_CorrectlyMapsHeaders()
        {
            const string headerValue = "Customer Header Value";
            var message = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            };
            message.Headers.Add("X-Custom", headerValue);


            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();

            IProviderServiceResponseMapper mapper = new ProviderServiceResponseMapper(mockHttpBodyContentMapper);

            var result = mapper.Convert(message);

            Assert.Equal(headerValue, result.Headers["X-Custom"]);
        }

        [Fact]
        public void Convert_WithResponseContentHeaders_CorrectlyMapsHeaders()
        {
            var stringContent = new StringContent("", Encoding.UTF8, "text/plain");

            var message = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = stringContent
            };

            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();
            mockHttpBodyContentMapper.Convert(Arg.Any<string>(), Arg.Any<IDictionary<string, string>>()).Returns(new HttpBodyContent("", "text/plain", Encoding.UTF8));
            
            IProviderServiceResponseMapper mapper = new ProviderServiceResponseMapper(mockHttpBodyContentMapper);

            var result = mapper.Convert(message);

            Assert.Equal("text/plain; charset=utf-8", result.Headers["Content-Type"]);
        }

        [Fact]
        public void Convert_WithResponseAndResponseContentHeaders_CorrectlyMapsHeaders()
        {
            var stringContent = new StringContent("", Encoding.UTF8, "text/plain");
            const string headerValue = "Customer Header Value";

            var message = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = stringContent
            };
            message.Headers.Add("X-Custom", headerValue);


            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();
            mockHttpBodyContentMapper.Convert(Arg.Any<string>(), Arg.Any<IDictionary<string, string>>()).Returns(new HttpBodyContent("", "text/plain", Encoding.UTF8));

            IProviderServiceResponseMapper mapper = new ProviderServiceResponseMapper(mockHttpBodyContentMapper);

            var result = mapper.Convert(message);

            Assert.Equal(headerValue, result.Headers["X-Custom"]);
            Assert.Equal("text/plain; charset=utf-8", result.Headers["Content-Type"]);
        }

        [Fact]
        public void Convert_WithPlainTextContent_CallsConvertOnHttpBodyContentMapperAndCorrectlyMapsBody()
        {
            const string content = "some plaintext content";

            var stringContent = new StringContent(content, Encoding.UTF8, "text/plain");

            var message = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = stringContent
            };

            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();
            mockHttpBodyContentMapper.Convert(Arg.Any<string>(), Arg.Any<IDictionary<string, string>>()).Returns(new HttpBodyContent(content, "text/plain", Encoding.UTF8));

            IProviderServiceResponseMapper mapper = new ProviderServiceResponseMapper(mockHttpBodyContentMapper);

            var result = mapper.Convert(message);

            Assert.Equal(content, result.Body);
            mockHttpBodyContentMapper.Received(1).Convert(content, Arg.Any<Dictionary<string, string>>());
        }

        [Fact]
        public void Convert_WithJsonContent_CallsConvertOnHttpBodyContentMapperAndCorrectlyMapsBody()
        {
            var body = new
            {
                Test = "tester",
                test2 = 1
            };
            const string content = "{\"Test\":\"tester\",\"test2\":1}";

            var stringContent = new StringContent(content, Encoding.UTF8, "application/json");

            var message = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = stringContent
            };

            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();
            mockHttpBodyContentMapper.Convert(Arg.Any<string>(), Arg.Any<IDictionary<string, string>>()).Returns(new HttpBodyContent(content, "application/json", Encoding.UTF8));

            IProviderServiceResponseMapper mapper = new ProviderServiceResponseMapper(mockHttpBodyContentMapper);

            var result = mapper.Convert(message);

            Assert.Equal(body.Test, (string)result.Body.Test);
            Assert.Equal(body.test2, (int)result.Body.test2);
            mockHttpBodyContentMapper.Received(1).Convert(content, Arg.Any<Dictionary<string, string>>());
        }
    }
}
