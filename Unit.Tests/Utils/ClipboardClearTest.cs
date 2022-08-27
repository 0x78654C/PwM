using Mkb;
using Xunit;

namespace Unit.Tests.Utils
{
    public class ClipboardClearTest
    {
        private const string SetText = "brehrehrth4%Y&£%H£";
        [StaTheory]
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
