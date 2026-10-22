namespace Tests
{
    [TestClass]
    public sealed class Test1
    {
        [TestMethod]
        public void Success()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Fail()
        {
            Assert.IsFalse(false);
        }
    }
}
