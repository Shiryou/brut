using ResourceUtilityLib;

namespace BrutTest
{
    [TestClass]
    public class ImageHandlerTest
    {
        public TestContext TestContext { get; set; }  // instance property

        [TestMethod]
        public void ConvertPCXtoBitmapWithoutRotation()
        {
            byte[] pcx = File.ReadAllBytes(Path.Combine("TestData", "test.pcx"));
            byte[] expected = File.ReadAllBytes(Path.Combine("TestData", "test_uncomp.bin"));
            byte[] actual = ImageHandler.ConvertPCXToBitmap(pcx, false);
            
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
