using System;
using Newtonsoft.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ws_sharp;

namespace ws_sharp_test
{
    [TestClass]
    public class MessageTest
    {
        [TestMethod]
        public void SerializeTest()
        {
            Message msg = new Message();
            msg.header = new Header();
            msg.content = new Content();
            msg.header.ID = "123abc";
            msg.header.Type = "request";
            msg.header.TimeStamp = "12:20";
            msg.content.CMD = "SiteOnline";
            msg.content.Params = JsonConvert.DeserializeObject("{\"SiteID\":\"\",\"Cameras\":[{\"ID\":\"1\",\"Status\":\"live\"},{\"ID\":\"2\",\"Status\":\"offline\"}]}");
            string str = JsonConvert.SerializeObject(msg);
            Assert.AreEqual(str, "{\"header\":{\"ID\":\"123abc\",\"Type\":\"request\",\"TimeStamp\":\"12:20\"},\"content\":{\"CMD\":\"SiteOnline\",\"REQ\":{\"SiteID\":\"\",\"Cameras\":[{\"ID\":\"1\",\"Status\":\"live\"},{\"ID\":\"2\",\"Status\":\"offline\"}]}}}");
        }

        [TestMethod]
        public void DeserializeTest()
        {
            Message msg = JsonConvert.DeserializeObject<Message>("{\"header\":{\"ID\":\"123abc\",\"Type\":\"request\",\"TimeStamp\":\"12:20\"},\"content\":{\"CMD\":\"SiteOnline\",\"REQ\":{\"SiteID\":\"\",\"Cameras\":[{\"ID\":\"1\",\"Status\":\"live\"},{\"ID\":\"2\",\"Status\":\"offline\"}]}}}");
            Assert.IsNotNull(msg.header);
            Assert.AreEqual("123abc", msg.header.ID);
            Assert.AreEqual("request", msg.header.Type);
            Assert.AreEqual("12:20", msg.header.TimeStamp);
            Assert.IsNotNull(msg.content);
            Assert.AreEqual("SiteOnline", msg.content.CMD);
            Assert.IsNotNull(msg.content.Params);
        }
    }
}
