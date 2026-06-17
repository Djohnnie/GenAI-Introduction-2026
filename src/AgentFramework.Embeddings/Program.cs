using Azure.AI.OpenAI;
using Microsoft.Data.SqlTypes;
using Microsoft.EntityFrameworkCore;
using System.ClientModel;

var endpoint = Environment.GetEnvironmentVariable("OPENAI_ENDPOINT") ?? string.Empty;
var key = Environment.GetEnvironmentVariable("OPENAI_KEY") ?? string.Empty;

var client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key));
var embeddingClient = client.GetEmbeddingClient("text-embedding-ada-002");

using var dbContext = new FirstNamesDbContext();
if (!await dbContext.FirstNames.AnyAsync())
{
    foreach (var name in Data.FirstNames)
    {
        var embedding = await embeddingClient.GenerateEmbeddingAsync(name);
        dbContext.FirstNames.Add(new FirstName
        {
            Name = name,
            Embedding = new SqlVector<float>(embedding.Value.ToFloats())
        });
    }

    await dbContext.SaveChangesAsync();
}

Console.Write("What is your name? ");
var input = Console.ReadLine();
Console.WriteLine();

var inputEmbedding = await embeddingClient.GenerateEmbeddingAsync(input);
var sqlVector = new SqlVector<float>(inputEmbedding.Value.ToFloats());
var topSimilarNames = await dbContext.FirstNames
    .OrderBy(b => EF.Functions.VectorDistance("cosine", b.Embedding, sqlVector))
    .Take(5)
    .ToListAsync();

Console.WriteLine("Top 5 similar names:");

foreach (var topSimilarName in topSimilarNames)
{
    Console.WriteLine($"Name: {topSimilarName.Name}");
}

Console.ReadKey();




internal class FirstName
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public SqlVector<float> Embedding { get; set; }
}

internal class FirstNamesDbContext : DbContext
{
    public DbSet<FirstName> FirstNames { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=.\\SQLDEV;Database=AgentFrameworkEmbeddings;Integrated Security=true;Trusted_Connection=True;Encrypt=false");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FirstName>()
            .Property(b => b.Embedding)
            .HasColumnType("vector(1536)");
    }
}

static class Data
{
    public static string[] FirstNames =
    [
        "Aaron", "Abby", "Abigail", "Adam", "Adrian", "Aidan", "Aisha", "Albert", "Alejandro", "Alexa",
        "Alexander", "Alexandra", "Alfred", "Alice", "Alicia", "Allison", "Alma", "Alyssa", "Amanda", "Amber",
        "Amelia", "Amy", "Andre", "Andrea", "Andrew", "Angela", "Anita", "Ann", "Anna", "Anne",
        "Annie", "Anthony", "Antonio", "April", "Ariana", "Arianna", "Arthur", "Ashley", "Ashton", "Aubrey",
        "Audrey", "Austin", "Ava", "Barbara", "Barry", "Beatrice", "Becky", "Belinda", "Benjamin", "Bernard",
        "Beth", "Bethany", "Beverly", "Bianca", "Bill", "Billy", "Blake", "Bob", "Bobby", "Bonnie",
        "Brad", "Bradley", "Brady", "Brandi", "Brandon", "Brandy", "Brayden", "Brenda", "Brent", "Brett",
        "Brian", "Brianna", "Brittany", "Brooke", "Bruce", "Bryan", "Bryce", "Caleb", "Calvin", "Cameron",
        "Candace", "Carla", "Carmen", "Carol", "Caroline", "Carrie", "Casey", "Cassandra", "Catherine", "Cathy",
        "Cecilia", "Celeste", "Chad", "Charlene", "Charles", "Charlie", "Charlotte", "Chase", "Chelsea", "Cheryl",
        "Chris", "Christian", "Christina", "Christine", "Christopher", "Cindy", "Claire", "Clara", "Clarence", "Claudia",
        "Clayton", "Clifford", "Clint", "Clinton", "Cody", "Colby", "Cole", "Colin", "Connor", "Constance",
        "Corey", "Courtney", "Craig", "Crystal", "Curtis", "Cynthia", "Daisy", "Dakota", "Dale", "Dallas",
        "Damian", "Dan", "Dana", "Daniel", "Danielle", "Danny", "Daphne", "Darlene", "Darrell", "Darren",
        "Dave", "David", "Dawn", "Dean", "Deanna", "Debbie", "Deborah", "Debra", "Denise", "Dennis",
        "Derek", "Desiree", "Destiny", "Devin", "Diana", "Diane", "Diego", "Dominic", "Don", "Donald",
        "Donna", "Dora", "Doris", "Dorothy", "Doug", "Douglas", "Drew", "Duane", "Dustin", "Dylan",
        "Earl", "Ed", "Eddie", "Edgar", "Edith", "Edward", "Edwin", "Elaine", "Eleanor", "Elena",
        "Eli", "Elijah", "Elizabeth", "Ella", "Ellen", "Ellie", "Elmer", "Elsa", "Elvis", "Emilia",
        "Emily", "Emma", "Enrique", "Eric", "Erica", "Erik", "Erin", "Ernest", "Esmeralda", "Esther",
        "Ethan", "Ethel", "Eugene", "Eva", "Evan", "Evelyn", "Everett", "Faith", "Felicia", "Felix",
        "Fernando", "Fiona", "Flora", "Florence", "Frances", "Francis", "Francisco", "Frank", "Franklin", "Fred",
        "Freddie", "Gabriel", "Gail", "Garrett", "Gary", "Gavin", "Gene", "Geoffrey", "George", "Georgia",
        "Gerald", "Gerard", "Gina", "Glen", "Glenda", "Gloria", "Gordon", "Grace", "Grant", "Greg",
        "Gregory", "Gretchen", "Guy", "Hailey", "Haley", "Hannah", "Harley", "Harold", "Harry", "Hazel",
        "Heather", "Heidi", "Helen", "Henry", "Herbert", "Holly", "Hope", "Howard", "Hugh", "Hunter",
        "Ian", "Irene", "Isaac", "Isabel", "Isabella", "Isaiah", "Ivan", "Jack", "Jackie", "Jacob",
        "Jacqueline", "Jade", "Jaime", "Jake", "James", "Jamie", "Jan", "Jane", "Janet", "Janice",
        "Jared", "Jasmine", "Jason", "Javier", "Jay", "Jayden", "Jean", "Jeff", "Jeffery", "Jeffrey",
        "Jenna", "Jennifer", "Jenny", "Jeremiah", "Jeremy", "Jerry", "Jesse", "Jessica", "Jesus", "Jill",
        "Jim", "Jimmy", "Joan", "Joann", "Joanna", "Joanne", "Jocelyn", "Joe", "Joel", "John",
        "Johnny", "Jon", "Jonathan", "Jordan", "Jorge", "Jose", "Joseph", "Joshua", "Josiah", "Joy",
        "Joyce", "Juan", "Judith", "Judy", "Julia", "Julian", "Julie", "June", "Justin", "Kaitlyn",
        "Karen", "Katherine", "Kathleen", "Kathryn", "Kathy", "Katie", "Kayla", "Keith", "Kelly", "Kelsey",
        "Kenneth", "Kent", "Kerry", "Kevin", "Kim", "Kimberly", "Kirk", "Kristen", "Kristin", "Kyle",
        "Lance", "Larry", "Laura", "Lauren", "Laurie", "Lawrence", "Leah", "Lee", "Leo", "Leon",
        "Leonard", "Leslie", "Levi", "Lillian", "Lindsay", "Lisa", "Lloyd", "Logan", "Lois", "Lori",
        "Louis", "Lucas", "Luis", "Luke", "Lydia", "Lynn", "Mackenzie", "Madeline", "Madison", "Mae",
        "Maggie", "Malcolm", "Manuel", "Marc", "Marcia", "Marcus", "Margaret", "Maria", "Marian", "Marie",
        "Marilyn", "Mario", "Marion", "Mark", "Marlene", "Marshall", "Martha", "Martin", "Marvin", "Mary",
        "Mason", "Matthew", "Maureen", "Megan", "Melanie", "Melissa", "Melvin", "Mercedes", "Meredith", "Mia",
        "Michael", "Micheal", "Michelle", "Miguel", "Mildred", "Milton", "Mindy", "Miranda", "Misty", "Mitchell",
        "Molly", "Monica", "Morgan", "Nancy", "Naomi", "Natalie", "Nathan", "Nathaniel", "Neal", "Neil",
        "Nicholas", "Nicole", "Nina", "Noah", "Nora", "Norma", "Norman", "Olga", "Olivia", "Omar",
        "Oscar", "Owen", "Pam", "Pamela", "Pat", "Patricia", "Patrick", "Paul", "Paula", "Pauline",
        "Peggy", "Penny", "Peter", "Phil", "Philip", "Phillip", "Phyllis", "Preston", "Quentin", "Quinn",
        "Rachael", "Rachel", "Ralph", "Randall", "Randy", "Ray", "Raymond", "Rebecca", "Regina", "Renee",
        "Rex", "Rhonda", "Ricardo", "Richard", "Rick", "Ricky", "Rita", "Rob", "Robert", "Roberta",
        "Robin", "Rochelle", "Rodney", "Roger", "Roland", "Ron", "Ronald", "Rosa", "Rose", "Rosemary",
        "Ross", "Roy", "Ruby", "Russell", "Ruth", "Ryan", "Sabrina", "Sally", "Sam", "Samantha",
        "Samuel", "Sandra", "Sara", "Sarah", "Scott", "Sean", "Seth", "Shane", "Shannon", "Sharon",
        "Shaun", "Shawn", "Sheila", "Shelby", "Sherri", "Sherry", "Shirley", "Sidney", "Sierra", "Simon",
        "Sophia", "Spencer", "Stacey", "Stacy", "Stanley", "Stephanie", "Stephen", "Steve", "Steven", "Stuart",
        "Sue", "Summer", "Susan", "Suzanne", "Sydney", "Sylvia", "Tamara", "Tammy", "Tanya", "Tara",
        "Taylor", "Ted", "Teresa", "Terrance", "Terri", "Terry", "Thelma", "Theresa", "Thomas", "Tiffany",
        "Tim", "Timothy", "Tina", "Todd", "Tom", "Tommy", "Toni", "Tony", "Tonya", "Tracy",
        "Travis", "Trent", "Trevor", "Troy", "Tyler", "Valerie", "Vanessa", "Veronica", "Vicki", "Vickie",
        "Victor", "Victoria", "Vincent", "Violet", "Vivian", "Wade", "Walter", "Wanda", "Warren", "Wayne",
        "Wendy", "Wesley", "Whitney", "Will", "William", "Willie", "Wyatt", "Xavier", "Yolanda", "Yvonne",
        "Zachary", "Zoe"
    ];
}