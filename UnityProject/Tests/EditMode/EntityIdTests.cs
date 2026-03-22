using NUnit.Framework;
using IsoRPG.Core;

namespace IsoRPG.Tests
{
    public class EntityIdTests
    {
        [Test]
        public void New_GeneratesUniqueIds()
        {
            var a = EntityId.New();
            var b = EntityId.New();
            Assert.AreNotEqual(a, b);
        }

        [Test]
        public void None_IsNotValid()
        {
            Assert.IsFalse(EntityId.None.IsValid);
        }

        [Test]
        public void New_IsValid()
        {
            Assert.IsTrue(EntityId.New().IsValid);
        }

        [Test]
        public void Equality_SameId_AreEqual()
        {
            var id = EntityId.New();
            var copy = new EntityId(id.GuidString);
            Assert.AreEqual(id, copy);
            Assert.IsTrue(id == copy);
            Assert.IsFalse(id != copy);
        }

        [Test]
        public void Equality_DifferentIds_AreNotEqual()
        {
            var a = EntityId.New();
            var b = EntityId.New();
            Assert.AreNotEqual(a, b);
            Assert.IsTrue(a != b);
            Assert.IsFalse(a == b);
        }

        [Test]
        public void None_EqualsNone()
        {
            Assert.AreEqual(EntityId.None, EntityId.None);
        }

        [Test]
        public void GetHashCode_SameId_SameHash()
        {
            var id = EntityId.New();
            var copy = new EntityId(id.GuidString);
            Assert.AreEqual(id.GetHashCode(), copy.GetHashCode());
        }

        [Test]
        public void ToString_Valid_ReturnsShortGuid()
        {
            var id = EntityId.New();
            Assert.AreEqual(8, id.ToString().Length);
        }

        [Test]
        public void ToString_None_ReturnsNoneString()
        {
            Assert.AreEqual("None", EntityId.None.ToString());
        }
    }
}
