﻿namespace PwM.Tests.Util
{
    public class ClipboardClearTest
    {
        private const string SetText = "brehrehrth4%Y&£%H£";
        [Theory]
        [InlineData(SetText, true)]
        [InlineData("test1", false)]
        public void ClearSpecificTextOnlyClipboard(string password, bool shouldBeClear)
        {
            ClipBoardManager.SetText(SetText);
            PwM.Utils.ClipBoardUtil.ClearClipboard(password);
            Assert.Equal(shouldBeClear ? "" : SetText, ClipBoardManager.GetText());
        }
    }
}
