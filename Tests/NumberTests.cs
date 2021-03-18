using Microsoft.Xna.Framework;
using MLEM.Extensions;
using NUnit.Framework;

namespace Tests {
    public class NumberTests {

        [Test]
        public void TestRounding() {
            Assert.AreEqual(1.25F.Floor(), 1);
            Assert.AreEqual(-1.25F.Floor(), -1);

            Assert.AreEqual(1.25F.Ceil(), 2);
            Assert.AreEqual(-1.25F.Ceil(), -2);

            Assert.AreEqual(new Vector2(5, 2.5F).FloorCopy(), new Vector2(5, 2));
            Assert.AreEqual(new Vector2(5, 2.5F).CeilCopy(), new Vector2(5, 3));
            Assert.AreEqual(new Vector2(5.25F, 2).FloorCopy(), new Vector2(5, 2));
            Assert.AreEqual(new Vector2(5.25F, 2).CeilCopy(), new Vector2(6, 2));
        }

        [Test]
        public void TestEquals() {
            Assert.IsTrue(0.25F.Equals(0.26F, 0.01F));
            Assert.IsFalse(0.25F.Equals(0.26F, 0.009F));
        }

        [Test]
        public void TestMatrixOps() {
            var matrix = Matrix.CreateRotationX(2) * Matrix.CreateScale(2.5F);
            Assert.AreEqual(matrix.Scale(), new Vector3(2.5F));
            Assert.AreEqual(matrix.Rotation(), Quaternion.CreateFromAxisAngle(Vector3.UnitX, 2));
        }

    }
}