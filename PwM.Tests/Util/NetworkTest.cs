﻿using PwMLib;

namespace PwM.Tests.Util
{
    public class NetworkTest
    {
        [Theory]
        [InlineData("https://google.com")]
        public void Check_Web_Response(string address)
        {
            var network = new Network(address);
            var isGooglUp = network.IsWebResponding();
            Assert.True(isGooglUp);
        }

        [Theory]
        [InlineData("api.pwnedpasswords.com")]
        public void Check_Ping_Response(string address)
        {
            var network = new Network(address);
            var isApiUp = network.PingHost();
            Assert.True(isApiUp);
        }
    }
}
