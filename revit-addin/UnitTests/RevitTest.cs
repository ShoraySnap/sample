using Autodesk.Revit.DB;
using NUnit.Framework;
using TrudeSerializer.Importer;
using TrudeSerializer.Utils;

namespace UnitTests
{
    public class RevitTest
    {
        [Test]
        public void XyzTest()
        {
            XYZ xyz = new XYZ( 1, 2, 3 );
            XYZ test = xyz.Add( new XYZ( 5, 6, 7 ) );
            Assert.AreEqual( test.X, 6 );
            Assert.AreEqual( test.Y, 8 );
            Assert.AreEqual( test.Z, 10 );
        }

        [Test]
        public void XyzMultiplyTest()
        {
            XYZ xyz = new XYZ( 1, 2, 3 );
            XYZ test = xyz.Multiply( 10 );
            Assert.AreEqual( test.X, 10 );
            Assert.AreEqual( test.Y, 20 );
            Assert.AreEqual( test.Z, 30 );
        }


        [Test]
        public void ComponentHandlerSingletonTest()
        {
            Assert.IsNotNull(ComponentHandler.Instance);
        }

        


    }
}
