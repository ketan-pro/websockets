using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ws_sharp;

namespace ws_sharp_test
{
    [TestClass]
    public class SecurityBuilderTest
    {
        [TestMethod]
        public void buildCertificateTest()
        {
            SecurityBuilder builder = new SecurityBuilder();
            builder.build();
        }
    }
}
