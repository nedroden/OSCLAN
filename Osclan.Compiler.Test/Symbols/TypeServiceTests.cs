using Osclan.Compiler.Symbols;

namespace Osclan.Compiler.Test.Symbols;

public abstract class TypeServiceTests
{
    public class SizeComputationTests
    {
        [Fact]
        public void Type_Offset_Is_Computed_For_Elementary_Type()
        {
            // Arrange
            var type = new Type(BuiltInType.String)
            {
                SizeInBytes = 8
            };

            // Act
            TypeService.IndexType(type);

            // Assert
            Assert.Equal((uint)0, type.AddressOffset);
        }

        [Fact]
        public void Type_Offset_Is_Computed_For_Composite_Type_Of_Single_Elementary_Type()
        {
            // Arrange
            var type = new Type("some-field");
            var stringType = new Type("some-string-type");
            stringType.SizeInBytes = 32;
            type.Fields.Add("some-string", stringType);

            // Act
            TypeService.IndexType(type);

            // Assert
            Assert.Equal((uint)0, type.AddressOffset);
            Assert.Equal((uint)0, stringType.AddressOffset);
        }

        [Fact]
        public void Type_Offset_Is_Computed_For_Composite_Type_Of_Double_Elementary_Type()
        {
            // Arrange
            var type = new Type("some-field");

            var stringType = new Type("some-string-type");
            stringType.SizeInBytes = 32;

            var intType = new Type("some-int-type");
            intType.SizeInBytes = 8;

            type.Fields.Add("some-string", stringType);
            type.Fields.Add("some-int-type", intType);

            // Act
            TypeService.IndexType(type);

            // Assert
            Assert.Equal((uint)0, type.AddressOffset);
            Assert.Equal((uint)0, stringType.AddressOffset);
            Assert.Equal((uint)32, intType.AddressOffset);
        }

        [Fact]
        public void Test_Offset_Is_Computed_For_Nested_Composite_Type()
        {
            // Arrange
            var type = new Type("some-field");

            var subCompositeType = new Type("some-other-composite-type");
            var subStringType = new Type("some-string-type") { SizeInBytes = 10 };
            var subIntType = new Type("some-first-int-type") { SizeInBytes = 10 };
            subCompositeType.Fields.Add("some-string-type", subStringType);
            subCompositeType.Fields.Add("some-first-int-type", subIntType);

            var intType = new Type("some-second-int-type");
            intType.SizeInBytes = 8;

            type.Fields.Add("some-composite", subCompositeType);
            type.Fields.Add("some-second-int-type", intType);

            // Act
            TypeService.IndexType(type);

            // Assert
            Assert.Equal((uint)0, type.AddressOffset);
            Assert.Equal((uint)0, subCompositeType.AddressOffset);
            Assert.Equal((uint)0, subStringType.AddressOffset);

            Assert.Equal((uint)10, subIntType.AddressOffset);
            Assert.Equal((uint)20, intType.AddressOffset);
        }
    }
}