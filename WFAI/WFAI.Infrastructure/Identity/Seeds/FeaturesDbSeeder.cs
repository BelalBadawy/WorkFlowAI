
namespace WFAI.Infrastructure.Persistence.DbInitializers
{
    public class FeaturesDbSeeder
    {
        private readonly ApplicationDbContext _dbContext;

        public FeaturesDbSeeder(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SeedFeaturesDatabaseAsync()
        {
            await SeedCategoriesAsync();
        }

        private async Task SeedCategoriesAsync()
        {

            if (!_dbContext.Categories.Any())
            {
                // 1. Define Parent Categories
                var fiction = new Category { Name = "Fiction", Slug = "fiction", ParentId = null, SortOrder = 1, IsActive = true };
                var nonFiction = new Category { Name = "Non-Fiction", Slug = "non-fiction", ParentId = null, SortOrder = 2, IsActive = true };
                var childrenYa = new Category { Name = "Children & Young Adult", Slug = "children-young-adult", ParentId = null, SortOrder = 3, IsActive = true };
                var academic = new Category { Name = "Academic & Educational", Slug = "academic-educational", ParentId = null, SortOrder = 4, IsActive = true };
                var arts = new Category { Name = "Arts & Humanities", Slug = "arts-humanities", ParentId = null, SortOrder = 5, IsActive = true };

                var categories = new List<Category>
{
    // --- PARENTS ---
    fiction,
    nonFiction,
    childrenYa,
    academic,
    arts,

    // --- FICTION CHILDREN ---
    new Category { Name = "Literary Fiction", Slug = "literary-fiction", Parent = fiction, SortOrder = 1, IsActive = true },
    new Category { Name = "Historical Fiction", Slug = "historical-fiction", Parent = fiction, SortOrder = 2, IsActive = true },
    new Category { Name = "Science Fiction", Slug = "science-fiction", Parent = fiction, SortOrder = 3, IsActive = true },
    new Category { Name = "Fantasy", Slug = "fantasy", Parent = fiction, SortOrder = 4, IsActive = true },
    new Category { Name = "Mystery & Thriller", Slug = "mystery-thriller", Parent = fiction, SortOrder = 5, IsActive = true },
    new Category { Name = "Horror", Slug = "horror", Parent = fiction, SortOrder = 6, IsActive = true },
    new Category { Name = "Romance", Slug = "romance", Parent = fiction, SortOrder = 7, IsActive = true },
    new Category { Name = "Contemporary Fiction", Slug = "contemporary-fiction", Parent = fiction, SortOrder = 8, IsActive = true },
    new Category { Name = "Action & Adventure", Slug = "action-adventure", Parent = fiction, SortOrder = 9, IsActive = true },
    new Category { Name = "Graphic Novels", Slug = "graphic-novels", Parent = fiction, SortOrder = 10, IsActive = true },
    new Category { Name = "Dystopian", Slug = "dystopian", Parent = fiction, SortOrder = 11, IsActive = true },
    new Category { Name = "Crime Fiction", Slug = "crime-fiction", Parent = fiction, SortOrder = 12, IsActive = true },

    // --- NON-FICTION CHILDREN ---
    new Category { Name = "Biography & Memoir", Slug = "biography-memoir", Parent = nonFiction, SortOrder = 1, IsActive = true },
    new Category { Name = "History", Slug = "history", Parent = nonFiction, SortOrder = 2, IsActive = true },
    new Category { Name = "Science & Nature", Slug = "science-nature", Parent = nonFiction, SortOrder = 3, IsActive = true },
    new Category { Name = "Self-Help", Slug = "self-help", Parent = nonFiction, SortOrder = 4, IsActive = true },
    new Category { Name = "Business & Economics", Slug = "business-economics", Parent = nonFiction, SortOrder = 5, IsActive = true },
    new Category { Name = "Health & Wellness", Slug = "health-wellness", Parent = nonFiction, SortOrder = 6, IsActive = true },
    new Category { Name = "Travel & Tourism", Slug = "travel-tourism", Parent = nonFiction, SortOrder = 7, IsActive = true },
    new Category { Name = "Politics & Social Sciences", Slug = "politics-social-sciences", Parent = nonFiction, SortOrder = 8, IsActive = true },
    new Category { Name = "True Crime", Slug = "true-crime", Parent = nonFiction, SortOrder = 9, IsActive = true },
    new Category { Name = "Religion & Spirituality", Slug = "religion-spirituality", Parent = nonFiction, SortOrder = 10, IsActive = true },
    new Category { Name = "Cooking & Food", Slug = "cooking-food", Parent = nonFiction, SortOrder = 11, IsActive = true },
    new Category { Name = "Pets & Animals", Slug = "pets-animals", Parent = nonFiction, SortOrder = 12, IsActive = true },
    new Category { Name = "Parenting & Family", Slug = "parenting-family", Parent = nonFiction, SortOrder = 13, IsActive = true },
    new Category { Name = "Sports & Outdoors", Slug = "sports-outdoors", Parent = nonFiction, SortOrder = 14, IsActive = true },

    // --- CHILDREN & YOUNG ADULT CHILDREN ---
    new Category { Name = "Picture Books", Slug = "picture-books", Parent = childrenYa, SortOrder = 1, IsActive = true },
    new Category { Name = "Early Readers", Slug = "early-readers", Parent = childrenYa, SortOrder = 2, IsActive = true },
    new Category { Name = "Middle Grade", Slug = "middle-grade", Parent = childrenYa, SortOrder = 3, IsActive = true },
    new Category { Name = "Young Adult Fiction", Slug = "young-adult-fiction", Parent = childrenYa, SortOrder = 4, IsActive = true },
    new Category { Name = "Young Adult Fantasy", Slug = "young-adult-fantasy", Parent = childrenYa, SortOrder = 5, IsActive = true },
    new Category { Name = "Fairy Tales & Folklore", Slug = "fairy-tales-folklore", Parent = childrenYa, SortOrder = 6, IsActive = true },
    new Category { Name = "Activity Books", Slug = "activity-books", Parent = childrenYa, SortOrder = 7, IsActive = true },

    // --- ACADEMIC & EDUCATIONAL CHILDREN ---
    new Category { Name = "Computer Science & IT", Slug = "computer-science-it", Parent = academic, SortOrder = 1, IsActive = true },
    new Category { Name = "Engineering", Slug = "engineering", Parent = academic, SortOrder = 2, IsActive = true },
    new Category { Name = "Medicine", Slug = "medicine", Parent = academic, SortOrder = 3, IsActive = true },
    new Category { Name = "Mathematics", Slug = "mathematics", Parent = academic, SortOrder = 4, IsActive = true },
    new Category { Name = "Law", Slug = "law", Parent = academic, SortOrder = 5, IsActive = true },
    new Category { Name = "Reference & Dictionaries", Slug = "reference-dictionaries", Parent = academic, SortOrder = 6, IsActive = true },
    new Category { Name = "Test Preparation", Slug = "test-preparation", Parent = academic, SortOrder = 7, IsActive = true },

    // --- ARTS & HUMANITIES CHILDREN ---
    new Category { Name = "Art & Design", Slug = "art-design", Parent = arts, SortOrder = 1, IsActive = true },
    new Category { Name = "Music & Film", Slug = "music-film", Parent = arts, SortOrder = 2, IsActive = true },
    new Category { Name = "Photography", Slug = "photography", Parent = arts, SortOrder = 3, IsActive = true },
    new Category { Name = "Poetry", Slug = "poetry", Parent = arts, SortOrder = 4, IsActive = true },
    new Category { Name = "Humor & Satire", Slug = "humor-satire", Parent = arts, SortOrder = 5, IsActive = true }
};

                // FIX: Populate NormalizedName and NormalizedSlug before saving
                foreach (var category in categories)
                {
                    category.NormalizedName = category.Name.ToUpperInvariant();
                    category.NormalizedSlug = category.Slug.ToUpperInvariant();
                }

                _dbContext.Categories.AddRange(categories);
                await _dbContext.SaveChangesAsync();
            }
        }

    }
}