using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using NUnit.Framework;
using System.IO;
using Autodesk.Revit.DB.Visual;
using TrudeSerializer.Debug;
using TrudeSerializer;
using TrudeSerializer.Importer;
using Newtonsoft.Json;
using TrudeSerializer.Utils;


namespace UnitTests.Serializer
{
    public class UnitConversionTests
    {
        UIApplication uiApp;
        [SetUp]
        public void SetUp(UIApplication uiApplication)
        {
            Assert.NotNull(uiApplication);
            Assert.NotNull(uiApplication.Application);

            uiApp = uiApplication;
        }

        [TearDown]
        public void TearDown(UIApplication application)
        {
            Assert.NotNull(application);
            Assert.NotNull(application.Application);
            Assert.NotNull(uiApp);
        }

        [Test]
        public void UnitTypeTests()
        {
            Assert.NotNull(uiApp);

            // TRUDE UNIT TO REVIT UNIT
#if REVIT2021 || REVIT2022 || REVIT2023 || REVIT2024
            var unit_test_impl = new UnitProvider_21_22_23_24();
            ForgeTypeId unit_id;
            unit_id = (ForgeTypeId)unit_test_impl.GetRevitUnit(TRUDE_UNIT_TYPE.FEET);
            Assert.AreEqual(unit_id, UnitTypeId.Feet);

            unit_id = (ForgeTypeId)unit_test_impl.GetRevitUnit(TRUDE_UNIT_TYPE.INCH);
            Assert.AreEqual(unit_id, UnitTypeId.Inches);

            unit_id = (ForgeTypeId)unit_test_impl.GetRevitUnit(TRUDE_UNIT_TYPE.METER);
            Assert.AreEqual(unit_id, UnitTypeId.Meters);

            unit_id = (ForgeTypeId)unit_test_impl.GetRevitUnit(TRUDE_UNIT_TYPE.CENTIMETER);
            Assert.AreEqual(unit_id, UnitTypeId.Centimeters);

            unit_id = (ForgeTypeId)unit_test_impl.GetRevitUnit(TRUDE_UNIT_TYPE.MILLIMETER);
            Assert.AreEqual(unit_id, UnitTypeId.Millimeters);
#endif
        }

        void TestConvertToSnaptrudeUnits(TRUDE_UNIT_TYPE unit_type, double value, double expected)
        {
            double convertedProvider = UnitConversion.ConvertToSnaptrudeUnits(value, unit_type);
            Assert.AreEqual(expected, convertedProvider, 0.001);
        }


        [Test]
        public void ConvertToSnaptrudeUnitsTest()
        {
            Assert.NotNull(uiApp);
            TestConvertToSnaptrudeUnits(TRUDE_UNIT_TYPE.FEET, 123.123, 123.123 * 1.2);
            TestConvertToSnaptrudeUnits(TRUDE_UNIT_TYPE.INCH, 123.123, 123.123 / 10);
            TestConvertToSnaptrudeUnits(TRUDE_UNIT_TYPE.METER, 123.123, 123.123 * (39.37 / 10));
            TestConvertToSnaptrudeUnits(TRUDE_UNIT_TYPE.CENTIMETER, 123.123, 123.123 / 25.4);
            TestConvertToSnaptrudeUnits(TRUDE_UNIT_TYPE.MILLIMETER, 123.123, 123.123 / 254);
        }


        [Test]
        public void ConvertToSnaptrudeUnitsFromFeetTest()
        {
            Assert.NotNull(uiApp);
            double value = 123.123;
            double expected = value * 1.2;

            double result = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(value);
            Assert.AreEqual(expected, result);

            double[] valueList = new double[] { 123.123, 321.321, 515.15 };
            double[] expectedList = new double[] { valueList[0] * 1.2, valueList[1] * 1.2, valueList[2] * 1.2 };

            double[] resultList = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(valueList);
            Assert.AreEqual(expectedList, resultList);

            XYZ valueXYZ = new XYZ(123.123, 321.321, 515.15);
            double[] resultXYZ = UnitConversion.ConvertToSnaptrudeUnitsFromFeet(valueXYZ);

            Assert.AreEqual(expectedList, resultXYZ);
        }



    }
}
