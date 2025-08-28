using ResourceUtilityLib;

namespace BrutTest
{
    [TestClass]
    public class LZSSTest
    {
        public TestContext TestContext { get; set; }  // instance property

        [TestMethod]
        [Ignore("Not implemented")]
        public void LZSSEncode()
        {
            byte[] uncompressed = File.ReadAllBytes(Path.Combine("TestData","test_uncomp.bin"));
            byte[] expected = File.ReadAllBytes(Path.Combine("TestData", "test_comp.bin"));
            byte[] actual = LZSS.Encode(uncompressed);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LZSSDecode()
        {
            byte[] compressed = File.ReadAllBytes(Path.Combine("TestData", "test_comp.bin"));
            byte[] expected = File.ReadAllBytes(Path.Combine("TestData", "test_uncomp.bin"));
            byte[] actual = LZSS.Decode(compressed, (uint)expected.Length);

            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
