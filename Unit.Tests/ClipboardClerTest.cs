using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Xunit;
namespace Unit.Tests
{
    public class ClipboardClerTest
    {
        [Theory]
        [InlineData("test")]
        public void ClearSpecificTextOnlyClipboard(string password)
        {
            if (Clipboard.ContainsText(TextDataFormat.Text))
            {
                bool checkClip = false;
                string clipboardContent = Clipboard.GetText(TextDataFormat.Text);
                checkClip = clipboardContent == password ? true : false;
                Assert.True(checkClip);
            }
        }
    }
}
