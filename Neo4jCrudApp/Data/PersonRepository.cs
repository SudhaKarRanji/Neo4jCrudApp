using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neo4j.Driver;
using Neo4jCrudApp.Models;

namespace Neo4jCrudApp.Data
{
    /// <summary>
    /// One class, four operations — Create, Read, Update, Delete against a
    /// single :Person label. Kept deliberately small so the UI code stays the focus.
    /// </summary>
    public class PersonRepository : IAsyncDisposable
    {
        private readonly IDriver _driver;
        private readonly string _database;

        public PersonRepository(string uri, string user, string password, string database)
        {
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));
            _database = database;
        }

        public Task VerifyConnectivityAsync() => _driver.VerifyConnectivityAsync();

        private IAsyncSession NewSession(AccessMode mode = AccessMode.Write) =>
            _driver.AsyncSession(o => o.WithDatabase(_database).WithDefaultAccessMode(mode));

        // CREATE (MERGE keeps it safe to click twice without duplicating a node)
        public async Task CreateAsync(Person person)
        {
            await using var session = NewSession();
            await session.ExecuteWriteAsync(async tx =>
            {
                await tx.RunAsync(
                    "MERGE (p:Person {name: $name}) " +
                    "SET p.age = $age, p.city = $city",
                    new { name = person.Name, age = person.Age, city = person.City ?? "" });
                return 0;
            });
        }

        // READ — all people, for the grid
        public async Task<List<Person>> GetAllAsync()
        {
            await using var session = NewSession(AccessMode.Read);
            return await session.ExecuteReadAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    "MATCH (p:Person) RETURN p.name AS name, p.age AS age, p.city AS city ORDER BY p.name");

                var list = new List<Person>();
                var records = await cursor.ToListAsync();
                foreach (var record in records)
                {
                    list.Add(new Person
                    {
                        Name = record["name"].As<string>(),
                        Age = record["age"] == null ? 0 : record["age"].As<int>(),
                        City = record["city"]?.As<string>() ?? ""
                    });
                }
                return list;
            });
        }

        // UPDATE — by original name (handles the case where the name itself was edited)
        public async Task<bool> UpdateAsync(string originalName, Person updated)
        {
            await using var session = NewSession();
            return await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    "MATCH (p:Person {name: $originalName}) " +
                    "SET p.name = $newName, p.age = $age, p.city = $city " +
                    "RETURN p",
                    new
                    {
                        originalName,
                        newName = updated.Name,
                        age = updated.Age,
                        city = updated.City ?? ""
                    });

                var summary = await cursor.ConsumeAsync();
                return summary.Counters.PropertiesSet > 0;
            });
        }

        // DELETE
        public async Task<bool> DeleteAsync(string name)
        {
            await using var session = NewSession();
            return await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    "MATCH (p:Person {name: $name}) DETACH DELETE p",
                    new { name });

                var summary = await cursor.ConsumeAsync();
                return summary.Counters.NodesDeleted > 0;
            });
        }

        public async Task<bool> DeleteAllAsync()
        {
            await using var session = NewSession();
            return await session.ExecuteWriteAsync(async tx =>
            {
                var cursor = await tx.RunAsync(
                    "MATCH (p:Person) DETACH DELETE p");
                var summary = await cursor.ConsumeAsync();
                return summary.Counters.NodesDeleted > 0;
            });
        }

        public async ValueTask DisposeAsync()
        {
            if (_driver != null)
            {
                await _driver.CloseAsync();
                await _driver.DisposeAsync();
            }
        }
    }
}
