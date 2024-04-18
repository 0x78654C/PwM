namespace PwM.Tests.Utils
{
    public class JsonTest
    {
        [Fact]
        public void Create_Json_File()
        {
            var jsonFile = Path.Combine(Directory.GetCurrentDirectory(), "PwM.Json");
            if (File.Exists(jsonFile))
                File.Delete(jsonFile);
            JsonManage.CreateJsonFile(jsonFile, new { FirstName = "Michael", TestItem = "Whatever" });
            Assert.True(File.Exists(jsonFile));
        }

        [Fact]
        public void Read_Json_File()
        {
            const string FirstName = "Mike";
            const string TestItem = "test2";
            var path = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, "{\"FirstName\":\"" + FirstName + "\",\"TestItem\":\"" + TestItem + "\"}");
            var item = JsonManage.ReadJsonFromFile<Person>(path);
            Assert.Equal(FirstName, item.FirstName);
            Assert.Equal(TestItem, item.TestItem);
        }

        [Fact]
        public void Update_Json_File_if_file_exist()
        {
            string firstName = "Mike";
            string testItem = "test2";
            string FirstName1 = "Mike1";
            string TestItem1 = "test21";
            var path = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, "[{\"FirstName\":\"" + firstName + "\",\"TestItem\":\"" + testItem + "\"}]");
            JsonManage.UpdateJsonFile(path, new Person { FirstName = FirstName1, TestItem = TestItem1 });
            var item = JsonManage.ReadJsonFromFile<Person[]>(path);
            Assert.Equal(2, item.Length);
            var first = item.First(t => t.FirstName == firstName);
            Assert.Equal(first.TestItem, testItem);
            var secound = item.First(t => t.FirstName == FirstName1);
            Assert.Equal(secound.TestItem, TestItem1);
        }

        [Fact]
        public void Update_Json_File_if_file_does_not_exist()
        {
            string FirstName1 = "Mike1";
            string TestItem1 = "test21";
            var path = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString("N"));
            JsonManage.UpdateJsonFile(path, new Person { FirstName = FirstName1, TestItem = TestItem1 });
            var item = JsonManage.ReadJsonFromFile<Person[]>(path);
            Assert.Single(item);
            var first = item.First(t => t.FirstName == FirstName1);
            Assert.Equal(first.TestItem, TestItem1);
        }

        [Fact]
        public void Delete_Json_Data_From_File()
        {
            string firstName = "Mike";
            string testItem = "test2";
            string FirstName1 = "Mike1";
            string TestItem1 = "test21";
            var path = Path.Combine(Directory.GetCurrentDirectory(), Guid.NewGuid().ToString("N"));
            File.WriteAllText(path, "[{\"FirstName\":\"" + firstName + "\",\"TestItem\":\"" + testItem + "\"}]");
            JsonManage.UpdateJsonFile(path, new Person { FirstName = FirstName1, TestItem = TestItem1 });
            JsonManage.DeleteJsonData<Person>(path, f => f.Where(t => t.FirstName == FirstName1));
            var item = JsonManage.ReadJsonFromFile<Person[]>(path);
            Assert.Single(item);
        }

        private class Person
        {
            public string FirstName { get; set; }
            public string TestItem { get; set; }

            public override bool Equals(object obj)
            {
                return obj is Person person &&
                       FirstName == person.FirstName &&
                       TestItem == person.TestItem;
            }

            public override int GetHashCode()
            {
                int hashCode = 834734480;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FirstName);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TestItem);
                return hashCode;
            }
        }
    }
}
