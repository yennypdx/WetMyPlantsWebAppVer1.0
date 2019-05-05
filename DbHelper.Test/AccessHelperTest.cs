using DbHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DbHelper.Test
{
    [TestClass]
    public class AccessHelperTest
    {
        [TestMethod]
        public void AccessHelperGetConnectionStringTest()
        {
            var str = AccessHelper.GetDbConnectionString();

            Assert.IsNotNull(str);
        }
    }
}
