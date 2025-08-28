using System;

using ResourceUtilityLib;

namespace BrutTest
{
    [TestClass]
    public class LibraryTest
    {
        [TestMethod]
        public void CreateEmptyResourceFile()
        {
            ResourceUtility resfile = new(new MemoryStream());

            Assert.AreEqual((uint)0, resfile.Count());
            Assert.AreEqual((uint)4, resfile.FileVersion());
        }

        [TestMethod]
        public void OpenEmptyResourceFile()
        {
            MemoryStream resfile = new MemoryStream();
            ResourceUtility brut = new(resfile);
            brut.SaveFileHeader();
            ResourceUtility brut2 = new(resfile);
            Assert.AreEqual((uint)0, brut2.Count());
            Assert.AreEqual((uint)4, brut2.FileVersion());
        }

        [TestMethod]
        public void CannotOpenInvalidFileVersion()
        {
            MemoryStream resfile = new MemoryStream();
            ResourceUtility brut = new(resfile);
            brut.SaveFileHeader();
            resfile.Position = 0;
            BinaryWriter writer = new BinaryWriter(resfile);
            writer.Write((uint)3);

            Assert.ThrowsExactly<UnsupportedVersionException>(() => new ResourceUtility(resfile));
        }

        [TestMethod]
        public void CannotOpenInvalidDIrectory()
        {
            MemoryStream resfile = new MemoryStream();
            ResourceUtility brut = new(resfile);
            brut.SaveFileHeader();
            BinaryWriter writer = new BinaryWriter(resfile);
            resfile.Position = 4;
            writer.Write((uint)resfile.Length + 1);

            Assert.ThrowsExactly<IndexOutOfRangeException>(() => new ResourceUtility(resfile));
        }

        [TestMethod]
        public void AddResource()
        {
            MemoryStream resfile = new MemoryStream();
            MemoryStream flcfile = new MemoryStream();

            ResourceUtility brut = new(resfile);
            brut.DisableCompression(); // disable compression, because it's not what we're testing.
            brut.AddFile("FILENAME.FLC", flcfile);
            Assert.AreEqual((uint)1, brut.Count());
            Assert.AreEqual("FILENAME.FLC", ResourceUtility.CharArrayToString(brut.ListContents()[0].filename));
        }

        [TestMethod]
        public void CannotLoadResourceWithInvalidStarCode()
        {
            MemoryStream resfile = new MemoryStream();
            MemoryStream flcfile = new MemoryStream();

            ResourceUtility brut = new(resfile);
            brut.DisableCompression(); // disable compression, because it's not what we're testing.
            brut.AddFile("FILENAME.FLC", flcfile);

            BinaryWriter writer = new BinaryWriter(resfile);
            resfile.Position = brut.GetFileOffset("FILENAME.FLC");
            writer.Write((uint)1129878754);

            Assert.ThrowsExactly<InvalidResourceException>(() => brut.GetFileInformation("FILENAME.FLC"));
        }

        [TestMethod]
        public void CannotLoadResourceWithInvalidSize()
        {
            MemoryStream resfile = new MemoryStream();
            MemoryStream flcfile = new MemoryStream();

            ResourceUtility brut = new(resfile);
            brut.DisableCompression(); // disable compression, because it's not what we're testing.
            brut.AddFile("FILENAME.FLC", flcfile);

            BinaryWriter writer = new BinaryWriter(resfile);
            resfile.Position = brut.GetFileOffset("FILENAME.FLC") + 8;
            writer.Write((uint)100);

            Assert.ThrowsExactly<InvalidResourceException>(() => brut.GetFileInformation("FILENAME.FLC"));
        }

        [TestMethod]
        public void ConvertCharArrayToString()
        {
            char[] array = ['R', 'e', 's', 'o', 'u', 'r', 'c', 'e', 'U', 't', 'i', 'l', 'i', 't', 'y', '\0'];

            Assert.AreEqual("ResourceUtility", ResourceUtility.CharArrayToString(array));
        }

        [TestMethod]
        public void ConvertStringToCharArray()
        {
            char[] shortArray = ['R', 'e', 'z', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0', '\0'];
            char[] longArray = ['R', 'e', 's', 'o', 'u', 'r', 'c', 'e', 'U', 't', 'i', 'l', 'i', 't', 'y'];

            CollectionAssert.AreEqual(shortArray, ResourceUtility.StringToCharArray("Rez"));
            CollectionAssert.AreEqual(longArray, ResourceUtility.StringToCharArray("ResourceUtility"));
        }

        [TestMethod]
        public void GetHashTypes()
        {
            ResourceUtility brut = new(new MemoryStream());

            Assert.AreEqual(HashAlgorithm.HashCrc, brut.GetHashType());
            brut.UseIDHash();
            Assert.AreEqual(HashAlgorithm.HashId, brut.GetHashType());
            brut.UseCRCHash();
            Assert.AreEqual(HashAlgorithm.HashCrc, brut.GetHashType());
        }

        [TestMethod]
        public void GetRestoreSetting()
        {
            ResourceUtility brut = new(new MemoryStream());

            Assert.AreEqual(false, brut.GetRestoreSetting());
            brut.RestorePCX();
            Assert.AreEqual(true, brut.GetRestoreSetting());
            brut.RetainBitmap();
            Assert.AreEqual(false, brut.GetRestoreSetting());
        }

        [TestMethod]
        public void GetSupportedExtensionsAndCompressionTypes()
        {
            string[] extensions = ["", "PCX", "FLC", "WAV"];
            string[] compression = ["not  compressed", "RLE  compressed", "LZSS compressed"];

            CollectionAssert.AreEqual(extensions, ResourceUtility.GetSupportedExtensions());
            CollectionAssert.AreEqual(compression, ResourceUtility.GetCompressionTypes());
        }

        [TestMethod]
        public void CheckFlags()
        {
            Assert.AreEqual(true, ResourceUtility.CheckFlag(0b0111, 4));
            Assert.AreEqual(true, ResourceUtility.CheckFlag(0b0111, 2));
            Assert.AreEqual(false, ResourceUtility.CheckFlag(0b1011, 4));
            Assert.AreEqual(true, ResourceUtility.CheckFlag(0b1011, 3));
        }
    }
}
