using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;

namespace PwM.Tests.Util
{
    public class ClipboardClearTest
    {
        private const string SetText = "brehrehrth4%Y&£%H£";

        [Theory]
        [InlineData(SetText, true)]
        [InlineData("test1", false)]
        public void ClearSpecificTextOnlyClipboard(string password, bool shouldBeClear)
        {
            RunInSta(() =>
            {
                Clipboard.SetText(SetText);
                PwM.Utils.ClipBoardUtil.ClearClipboard(password);
                Assert.Equal(shouldBeClear ? "" : SetText, Clipboard.GetText());
                Clipboard.Clear();
            });
        }

        private static void RunInSta(Action action)
        {
            Exception? exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception caught)
                {
                    exception = caught;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (exception != null)
                ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }
}
