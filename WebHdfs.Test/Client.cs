﻿using System;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace WebHdfs.Test
{
    [TestFixture]
	public class Integration
	{
		private const string BASE_URL = "http://test.me/plz/";
		private const string USER = "hdfs";
        private const string TEST_FILE = "test.txt";
        private const string BOOL_RESULT = "{ \"boolean\" : true }";

        [Test]
		public void GetStatus()
		{
			var path = "/path/to/file";
			CallClient(c => c.GetFileStatus(path).Wait(), HttpMethod.Get, path, "GETFILESTATUS");
		}

		[Test]
		public void CreateDirectory()
		{
			var path = "/path/to/file";
			CallClient(c => c.CreateDirectory(path).Wait(), HttpMethod.Put, path, "MKDIRS");
		}

        [Test]
        public void CreateFile()
        {
            var path = "/path/to/file";
            CallClient(c => c.CreateFile(TEST_FILE, path).Wait(), HttpMethod.Put, path, "CREATE");
            CallClient(c => c.CreateFile(TEST_FILE, path, CancellationToken.None).Wait(), HttpMethod.Put, path, "CREATE");
            CallClient(c => c.CreateFile(File.OpenRead(TEST_FILE), path).Wait(), HttpMethod.Put, path, "CREATE");
            CallClient(c => c.CreateFile(File.OpenRead(TEST_FILE), path, CancellationToken.None).Wait(), HttpMethod.Put, path, "CREATE");
        }

        [Test]
        public void DeleteDirectory()
        {
            var path = "/path/to/file";
            CallClient(c => c.DeleteDirectory(path).Wait(), HttpMethod.Delete, path, "DELETE");
        }

        [Test]
        public void GetContentSummary()
        {
            var path = "/path/to/file";
            CallClient(c => c.GetContentSummary(path).Wait(), HttpMethod.Get, path, "GETCONTENTSUMMARY");
        }

        [Test]
        public void GetDirectoryStatus()
        {
            var path = "/path/to/file";
            CallClient(c => c.GetDirectoryStatus(path).Wait(), HttpMethod.Get, path, "LISTSTATUS", "{\"FileStatuses\":{\"FileStatus\":[{ \"accessTime\":0,\"blockSize\":0,\"childrenNum\":4,\"fileId\":308665,\"group\":\"hdfs\",\"length\":0,\"modificationTime\":1429776977330,\"owner\":\"hdfs\",\"pathSuffix\":\"hdfs\",\"permission\":\"755\",\"replication\":0,\"type\":\"DIRECTORY\"}]}}");
        }

        [Test]
        public void GetFileChecksum()
        {
            var path = "/path/to/file";
            CallClient(c => c.GetFileChecksum(path).Wait(), HttpMethod.Get, path, "GETFILECHECKSUM");
        }

        [Test]
        public void GetFileStatus()
        {
            var path = "/path/to/file";
            CallClient(c => c.GetFileStatus(path).Wait(), HttpMethod.Get, path, "GETFILESTATUS");
        }

        [Test]
        public void GetHomeDirectory()
        {
            var path = "/";
            CallClient(c => c.GetHomeDirectory().Wait(), HttpMethod.Get, path, "GETHOMEDIRECTORY");
        }

        [Test]
        public void OpenFile()
        {
            var path = "/path/to/file";
            CallClient(c => c.OpenFile(path, CancellationToken.None).Wait(), HttpMethod.Get, path, "OPEN");
            CallClient(c => c.OpenFile(path, CancellationToken.None).Wait(), HttpMethod.Get, path, "OPEN");
        }

        [Test]
        public void RenameDirectory()
        {
            var path = "/path/to/file";
            var newPath = path + "-new";
            CallClient(c => c.RenameDirectory(path, newPath).Wait(), HttpMethod.Put, path, "RENAME&destination=" + newPath, BOOL_RESULT);
        }

        [Test]
        public void SetAccessTime()
        {
            var path = "/path/to/file";
            var time = "123";
            CallClient(c => c.SetAccessTime(path, time).Wait(), HttpMethod.Put, path, "SETTIMES&accesstime=" + time, BOOL_RESULT);
        }

        [Test]
        public void SetGroup()
        {
            var path = "/path/to/file";
            var param = "123";
            CallClient(c => c.SetGroup(path, param).Wait(), HttpMethod.Put, path, "SETOWNER&group=" + param, BOOL_RESULT);
        }

        [Test]
        public void SetModificationTime()
        {
            var path = "/path/to/file";
            var param = "123";
            CallClient(c => c.SetModificationTime(path, param).Wait(), HttpMethod.Put, path, "SETTIMES&modificationtime=" + param, BOOL_RESULT);
        }

        [Test]
        public void SetOwner()
        {
            var path = "/path/to/file";
            var param = "123";
            CallClient(c => c.SetOwner(path, param).Wait(), HttpMethod.Put, path, "SETOWNER&owner=" + param, BOOL_RESULT);
        }

        [Test]
        public void SetPermissions()
        {
            var path = "/path/to/file";
            var param = "123";
            CallClient(c => c.SetPermissions(path, param).Wait(), HttpMethod.Put, path, "SETPERMISSION&permission=" + param, BOOL_RESULT);
        }

        [Test]
        public void SetReplicationFactor()
        {
            var path = "/path/to/file";
            var param = 100;
            CallClient(c => c.SetReplicationFactor(path, param).Wait(), HttpMethod.Put, path, "SETREPLICATION&replication=" + param, BOOL_RESULT);
        }

        [Test]
        public void GetEmptyResult()
        {
            var path = "/path/to/file";
            CallClient(async c =>
                {
                    var file = await c.GetFileStatus(path);
                    Assert.IsNull(file);
                }, HttpMethod.Get, path, "GETFILESTATUS", status: HttpStatusCode.NotFound);
        }

        private void CallClient(Action<WebHdfsClient> caller, HttpMethod method, string url, string operation, string result = "{}", HttpStatusCode status = HttpStatusCode.OK)
		{
			var handler = new Mock<FakeHttpMessageHandler>();
			handler.CallBase = true;

            Expression<Func<FakeHttpMessageHandler, HttpResponseMessage>> homeCall = t => t.Send(It.Is<HttpRequestMessage>(
                            msg =>
                               msg.Method == HttpMethod.Get &&
                               msg.RequestUri.ToString() == "http://test.me/plz/webhdfs/v1/?user.name=hdfs&op=GETHOMEDIRECTORY"));

            handler.Setup(homeCall)
                   .Returns(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"Path\":\"/user/hdfs\"}", System.Text.Encoding.UTF8, "application/json") })
                   .Verifiable();


            if (!operation.StartsWith("GETHOMEDIRECTORY", StringComparison.OrdinalIgnoreCase))
            {
                Expression<Func<FakeHttpMessageHandler, HttpResponseMessage>> innerCall = t => t.Send(It.Is<HttpRequestMessage>(
                    msg =>
                        msg.Method == method &&
                        msg.RequestUri.ToString().StartsWith(BASE_URL + WebHdfsClient.PREFIX + url + "?user.name=" + USER + "&op=" + operation, StringComparison.OrdinalIgnoreCase)));

                handler.Setup(innerCall)
                        .Returns(new HttpResponseMessage(status) { Content = new StringContent(result, System.Text.Encoding.UTF8, "application/json") })
                        .Verifiable();
            }

            var client = new WebHdfsClient(handler.Object, BASE_URL, USER);
			caller(client);
			handler.Verify();
		}

		public class FakeHttpMessageHandler : HttpMessageHandler
		{
			public virtual HttpResponseMessage Send(HttpRequestMessage request)
			{
				return new HttpResponseMessage(HttpStatusCode.NoContent);
			}

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				return Task.FromResult(Send(request));
			}
		}
	}
}
