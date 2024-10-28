using ResourceUtilityLib;

namespace ResourceUtilityTest
{
    [TestClass]
    public class HashTest
    {
        [TestMethod]
        public void HashWithCrc()
        {
            string filename = "BUYMORE.PCX";
            uint expected = 4127938;

            uint hash = HashCalculator.HashCRC(filename);

            Assert.AreEqual(expected, hash);
        }
        [TestMethod]
        public void HashWithCrcLowercase()
        {
            string filename = "buymore.pcx";
            uint expected = 4127938;

            uint hash = HashCalculator.HashCRC(filename);

            Assert.AreEqual(expected, hash);
        }

        [TestMethod]
        public void HashWithId()
        {
            string filename = "WAV1209348.WAV";
            uint expected = 1209348;

            uint hash = HashCalculator.HashID(filename);

            Assert.AreEqual(expected, hash);
        }

        [TestMethod]
        public void HashWithIdAndPeriod()
        {
            string filename = "WAV120934B8.WAV";
            uint expected = 120934;

            uint hash = HashCalculator.HashID(filename);

            Assert.AreEqual(expected, hash);
        }
    }
}
