namespace OrmMock.EF6.Test
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class MemDbSetTest
    {
        private MemDb.MemDb memDb;

        private DataGenerator.DataGenerator dataGenerator;

        private MemDbSet<TestObject> set;

        [SetUp]
        public void Setup()
        {
            this.memDb = new MemDb.MemDb();
            this.dataGenerator = new DataGenerator.DataGenerator();
            this.memDb.AddMany(this.dataGenerator.CreateMany<TestObject>());
            this.memDb.Commit();
            this.set = this.memDb.DbSet<TestObject>();
        }

        [Test]
        public async Task TestAsync()
        {
            var data = await this.set.Where(d => d.Name != "foo").ToListAsync();
            Assert.AreEqual(3, data.Count);
        }

        public class TestObject
        {
            public Guid Id { get; set; }

            public string Name { get; set; }
        }
    }
}
