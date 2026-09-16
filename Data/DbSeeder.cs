using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SideQuestApp.Models;
using SideQuestApp.Services;

namespace SideQuestApp.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context =
                services.GetRequiredService<ApplicationDbContext>();

            var userManager =
                services.GetRequiredService<UserManager<ApplicationUser>>();

            var badgeService =
                services.GetRequiredService<BadgeService>();


            
            // ADMIN USER
            

            var admin =
                await userManager.FindByEmailAsync("admin@questapp.com");

            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin@questapp.com",
                    Email = "admin@questapp.com",
                    EmailConfirmed = true
                };

                var adminResult =
                    await userManager.CreateAsync(admin, "Admin123!");

                if (adminResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            if (admin == null)
                return;


            
            // CATEGORIES
            

            var categoryNames = new[]
            {
                "Adventure",
                "Creative",
                "Mind & Learning",
                "Fitness",
                "Sports",
                "Social",
                "Lifestyle",
                "Money",
                "Fun & Random",
                "Food",
                "Nightlife",
                "Nature"
            };

            var categories = new Dictionary<string, Category>();

            foreach (var name in categoryNames)
            {
                var category =
                    await context.Categories
                        .FirstOrDefaultAsync(c => c.Name == name);

                if (category == null)
                {
                    category = new Category
                    {
                        Name = name,
                        Description = GetCategoryDescription(name)
                    };

                    context.Categories.Add(category);
                    await context.SaveChangesAsync();
                }

                categories[name] = category;
            }


            
            // SEED USERS
            

            var seedUsers = new[]
            {
                new
                {
                    Username = "alex",
                    Email = "alex@questapp.com",
                    Password = "User123!",
                    Bio = "Always looking for the next place to explore."
                },

                new
                {
                    Username = "mia",
                    Email = "mia@questapp.com",
                    Password = "User123!",
                    Bio = "Making ordinary moments a little more creative."
                },

                new
                {
                    Username = "noah",
                    Email = "noah@questapp.com",
                    Password = "User123!",
                    Bio = "Curious about almost everything."
                },

                new
                {
                    Username = "lena",
                    Email = "lena@questapp.com",
                    Password = "User123!",
                    Bio = "Always working on the next challenge."
                },

                new
                {
                    Username = "leo",
                    Email = "leo@questapp.com",
                    Password = "User123!",
                    Bio = "The best quests are the ones shared with people."
                },

                new
                {
                    Username = "nina",
                    Email = "nina@questapp.com",
                    Password = "User123!",
                    Bio = "Most adventures start after sunset."
                },

                new
                {
                    Username = "mark",
                    Email = "mark@questapp.com",
                    Password = "User123!",
                    Bio = "Early mornings, fresh starts."
                },

                new
                {
                    Username = "zoe",
                    Email = "zoe@questapp.com",
                    Password = "User123!",
                    Bio = "No plan is sometimes the best plan."
                },

                new
                {
                    Username = "daniel",
                    Email = "daniel@questapp.com",
                    Password = "User123!",
                    Bio = "Always ready for another challenge."
                },

                new
                {
                    Username = "ella",
                    Email = "ella@questapp.com",
                    Password = "User123!",
                    Bio = "Small steps, every single day."
                }
            };

            var users = new Dictionary<string, ApplicationUser>();

            foreach (var data in seedUsers)
            {
                var user =
                    await userManager.FindByNameAsync(data.Username);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = data.Username,
                        Email = data.Email,
                        EmailConfirmed = true,
                        Bio = data.Bio,
                        IsPrivate = false
                    };

                    var result =
                        await userManager.CreateAsync(
                            user,
                            data.Password);

                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(
                            user,
                            "User");
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Could not create user {data.Username}: " +
                            string.Join(
                                ", ",
                                result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    user.Bio = data.Bio;
                    user.IsPrivate = false;

                    await userManager.UpdateAsync(user);
                }

                users[data.Username] = user;
            }

            await context.SaveChangesAsync();


            
            // BADGES
            

            await SeedBadgeAsync(
                context,
                "First Step",
                "Complete your first quest.",
                BadgeCriteriaType.CompletionCount,
                1);

            await SeedBadgeAsync(
                context,
                "Getting Started",
                "Complete 5 quests.",
                BadgeCriteriaType.CompletionCount,
                5);

            await SeedBadgeAsync(
                context,
                "Quest Addict",
                "Complete 30 quests.",
                BadgeCriteriaType.CompletionCount,
                30);

            await SeedBadgeAsync(
                context,
                "Quest Master",
                "Complete 100 quests.",
                BadgeCriteriaType.CompletionCount,
                100);

            await SeedBadgeAsync(
                context,
                "Legend",
                "Complete 365 quests.",
                BadgeCriteriaType.CompletionCount,
                365);

            await SeedBadgeAsync(
                context,
                "Creative Spark",
                "Complete 5 creative quests.",
                BadgeCriteriaType.CategoryCount,
                5,
                categories["Creative"]);

            await SeedBadgeAsync(
                context,
                "Masterpiece",
                "Complete 25 creative quests.",
                BadgeCriteriaType.CategoryCount,
                25,
                categories["Creative"]);

            await SeedBadgeAsync(
                context,
                "Off The Map",
                "Complete 10 adventure quests.",
                BadgeCriteriaType.CategoryCount,
                10,
                categories["Adventure"]);

            await SeedBadgeAsync(
                context,
                "Fitness Regular",
                "Complete 10 fitness quests.",
                BadgeCriteriaType.CategoryCount,
                10,
                categories["Fitness"]);

            await SeedBadgeAsync(
                context,
                "Social Butterfly",
                "Complete 10 social quests.",
                BadgeCriteriaType.CategoryCount,
                10,
                categories["Social"]);

            await SeedBadgeAsync(
                context,
                "Food Explorer",
                "Complete 10 food quests.",
                BadgeCriteriaType.CategoryCount,
                10,
                categories["Food"]);

            await SeedBadgeAsync(
                context,
                "Night Creature",
                "Complete 10 nightlife quests.",
                BadgeCriteriaType.CategoryCount,
                10,
                categories["Nightlife"]);

            await SeedBadgeAsync(
                context,
                "Nature Lover",
                "Complete 10 nature quests.",
                BadgeCriteriaType.CategoryCount,
                10,
                categories["Nature"]);

            await SeedBadgeAsync(
                context,
                "Mind Explorer",
                "Complete 10 Mind & Learning quests.",
                BadgeCriteriaType.CategoryCount,
                10,
                categories["Mind & Learning"]);


            
            // QUESTS
            

            var quests = new[]
            {
                // ADVENTURE

                QuestData(
                    "Hidden Gem",
                    "Find a place in your city that you have never visited before.",
                    20,
                    DifficultyLevel.Easy,
                    "Adventure"),

                QuestData(
                    "Go to Your City Center",
                    "Spend some time exploring the center of your city.",
                    10,
                    DifficultyLevel.Easy,
                    "Adventure"),

                QuestData(
                    "Sunset Hunter",
                    "Find a beautiful place to watch the sunset and capture the moment.",
                    25,
                    DifficultyLevel.Easy,
                    "Adventure"),

                QuestData(
                    "Mini Road Trip",
                    "Take a spontaneous road trip and explore somewhere outside your usual route.",
                    80,
                    DifficultyLevel.Hard,
                    "Adventure"),


                // CREATIVE

                QuestData(
                    "One-Minute Masterpiece",
                    "Create something artistic in exactly one minute.",
                    15,
                    DifficultyLevel.Easy,
                    "Creative"),

                QuestData(
                    "Color Hunt: Red",
                    "Find and photograph five interesting red objects.",
                    20,
                    DifficultyLevel.Easy,
                    "Creative"),

                QuestData(
                    "Color Hunt: Blue",
                    "Find and photograph five interesting blue objects.",
                    20,
                    DifficultyLevel.Easy,
                    "Creative"),

                QuestData(
                    "Color Hunt: Yellow",
                    "Find and photograph five interesting yellow objects.",
                    20,
                    DifficultyLevel.Easy,
                    "Creative"),

                QuestData(
                    "Color Hunt: Green",
                    "Find and photograph five interesting green objects.",
                    20,
                    DifficultyLevel.Easy,
                    "Creative"),

                QuestData(
                    "Color Hunt: Pink",
                    "Find and photograph five interesting pink objects.",
                    20,
                    DifficultyLevel.Easy,
                    "Creative"),

                QuestData(
                    "Album Cover",
                    "Create a photo that could be used as the cover of an imaginary album.",
                    35,
                    DifficultyLevel.Medium,
                    "Creative"),

                QuestData(
                    "Street Photographer",
                    "Take a creative photograph of everyday life around you.",
                    30,
                    DifficultyLevel.Medium,
                    "Creative"),

                QuestData(
                    "Make an Animal Out of Clay",
                    "Create an animal using clay or another modeling material.",
                    20,
                    DifficultyLevel.Easy,
                    "Creative"),

                QuestData(
                    "Paint a Flower",
                    "Paint or draw a flower and upload your finished creation.",
                    15,
                    DifficultyLevel.Easy,
                    "Creative"),

                QuestData(
                    "Write It Down",
                    "Write a short story, poem or personal reflection.",
                    35,
                    DifficultyLevel.Medium,
                    "Creative"),


                // MIND & LEARNING

                QuestData(
                    "Wikipedia Roulette",
                    "Open a random Wikipedia article and learn three new facts.",
                    15,
                    DifficultyLevel.Easy,
                    "Mind & Learning"),

                QuestData(
                    "Teach Me Something",
                    "Learn something interesting and teach it to another person.",
                    35,
                    DifficultyLevel.Medium,
                    "Mind & Learning"),

                QuestData(
                    "Brain Workout",
                    "Complete a puzzle, crossword, sudoku or similar brain challenge.",
                    25,
                    DifficultyLevel.Easy,
                    "Mind & Learning"),

                QuestData(
                    "New Skill Unlocked",
                    "Spend meaningful time learning a completely new skill.",
                    70,
                    DifficultyLevel.Hard,
                    "Mind & Learning"),

                QuestData(
                    "Explain Like I'm Five",
                    "Take a complicated topic and explain it as simply as possible.",
                    45,
                    DifficultyLevel.Medium,
                    "Mind & Learning"),

                QuestData(
                    "Make a Puzzle",
                    "Create a puzzle that another person can solve.",
                    35,
                    DifficultyLevel.Medium,
                    "Mind & Learning"),

                QuestData(
                    "Meditate",
                    "Take at least ten quiet minutes to meditate.",
                    15,
                    DifficultyLevel.Easy,
                    "Mind & Learning"),


                // FITNESS

                QuestData(
                    "10K Steps",
                    "Walk at least 10,000 steps in one day.",
                    30,
                    DifficultyLevel.Medium,
                    "Fitness"),

                QuestData(
                    "Morning Mission",
                    "Start your day with a short physical activity.",
                    15,
                    DifficultyLevel.Easy,
                    "Fitness"),

                QuestData(
                    "Stairs > Elevator",
                    "Choose the stairs instead of the elevator throughout the day.",
                    20,
                    DifficultyLevel.Easy,
                    "Fitness"),

                QuestData(
                    "Move It",
                    "Do at least 30 minutes of intentional physical activity.",
                    30,
                    DifficultyLevel.Medium,
                    "Fitness"),

                QuestData(
                    "Challenge Accepted",
                    "Complete a difficult physical challenge of your choice.",
                    70,
                    DifficultyLevel.Hard,
                    "Fitness"),


                // SPORTS

                QuestData(
                    "Try a New Sport",
                    "Try a sport you have never played before.",
                    40,
                    DifficultyLevel.Medium,
                    "Sports"),

                QuestData(
                    "30-Minute Game",
                    "Play a sport or physical game for at least 30 minutes.",
                    30,
                    DifficultyLevel.Medium,
                    "Sports"),

                QuestData(
                    "Beat Your Personal Best",
                    "Beat one of your previous personal records.",
                    70,
                    DifficultyLevel.Hard,
                    "Sports"),

                QuestData(
                    "Play Something You Haven't Played",
                    "Play a sport or game you have not played in a long time.",
                    30,
                    DifficultyLevel.Medium,
                    "Sports"),

                QuestData(
                    "One-on-One Basketball",
                    "Play a one-on-one basketball game.",
                    30,
                    DifficultyLevel.Medium,
                    "Sports"),

                QuestData(
                    "Watch a Football Match",
                    "Watch a football match from beginning to end.",
                    20,
                    DifficultyLevel.Easy,
                    "Sports"),


                // SOCIAL

                QuestData(
                    "Call Someone",
                    "Call someone you care about instead of sending a message.",
                    10,
                    DifficultyLevel.Easy,
                    "Social"),

                QuestData(
                    "Old Friend",
                    "Reconnect with someone you have not spoken to in a while.",
                    20,
                    DifficultyLevel.Easy,
                    "Social"),

                QuestData(
                    "Squad Quest",
                    "Complete a quest together with your friends.",
                    40,
                    DifficultyLevel.Medium,
                    "Social"),

                QuestData(
                    "New Connection",
                    "Start a genuine conversation with someone you do not know well.",
                    30,
                    DifficultyLevel.Medium,
                    "Social"),


                // LIFESTYLE

                QuestData(
                    "Digital Detox",
                    "Spend a few hours away from social media and unnecessary screen time.",
                    35,
                    DifficultyLevel.Medium,
                    "Lifestyle"),

                QuestData(
                    "Room Reset",
                    "Completely reset and organize your room.",
                    20,
                    DifficultyLevel.Easy,
                    "Lifestyle"),

                QuestData(
                    "Closet Quest",
                    "Organize your wardrobe and create a better system for it.",
                    25,
                    DifficultyLevel.Easy,
                    "Lifestyle"),

                QuestData(
                    "Early Bird",
                    "Wake up earlier than usual and use the morning intentionally.",
                    15,
                    DifficultyLevel.Easy,
                    "Lifestyle"),

                QuestData(
                    "Self-Care Hour",
                    "Spend one full hour doing things that genuinely help you recharge.",
                    20,
                    DifficultyLevel.Easy,
                    "Lifestyle"),


                // MONEY

                QuestData(
                    "No-Spend Day",
                    "Spend an entire day without buying anything unnecessary.",
                    35,
                    DifficultyLevel.Medium,
                    "Money"),

                QuestData(
                    "Budget Boss",
                    "Create a realistic personal budget for the next month.",
                    70,
                    DifficultyLevel.Hard,
                    "Money"),

                QuestData(
                    "Coffee at Home",
                    "Make your coffee at home instead of buying one.",
                    10,
                    DifficultyLevel.Easy,
                    "Money"),

                QuestData(
                    "Smart Shopper",
                    "Compare prices before buying something you need.",
                    25,
                    DifficultyLevel.Easy,
                    "Money"),

                QuestData(
                    "Save €10 Today",
                    "Find a realistic way to save at least €10 today.",
                    30,
                    DifficultyLevel.Medium,
                    "Money"),


                // FUN & RANDOM

                QuestData(
                    "Pick a Random Direction",
                    "Choose a random direction and explore for at least 15 minutes.",
                    20,
                    DifficultyLevel.Easy,
                    "Fun & Random"),

                QuestData(
                    "Do Something You've Never Done",
                    "Try something completely new to you.",
                    60,
                    DifficultyLevel.Hard,
                    "Fun & Random"),

                QuestData(
                    "Rock Paper Scissors Challenge",
                    "Challenge someone to a best-of-five rock paper scissors match.",
                    10,
                    DifficultyLevel.Easy,
                    "Fun & Random"),

                QuestData(
                    "Mystery Challenge",
                    "Complete a random challenge chosen by someone else.",
                    40,
                    DifficultyLevel.Medium,
                    "Fun & Random"),

                QuestData(
                    "Left or Right",
                    "Choose left or right at every intersection for ten minutes. Where did you end up?",
                    30,
                    DifficultyLevel.Medium,
                    "Fun & Random"),


                // FOOD

                QuestData(
                    "Cook Something New",
                    "Cook a dish you have never made before.",
                    40,
                    DifficultyLevel.Medium,
                    "Food"),

                QuestData(
                    "Make Carbonara",
                    "Make a carbonara from scratch.",
                    35,
                    DifficultyLevel.Medium,
                    "Food"),

                QuestData(
                    "Cook Chocolate Chip Cookies",
                    "Bake chocolate chip cookies from scratch.",
                    30,
                    DifficultyLevel.Medium,
                    "Food"),

                QuestData(
                    "Mystery Ingredient",
                    "Create a dish using an ingredient chosen at random.",
                    60,
                    DifficultyLevel.Hard,
                    "Food"),

                QuestData(
                    "Food Explorer",
                    "Try a food you have never eaten before.",
                    30,
                    DifficultyLevel.Medium,
                    "Food"),

                QuestData(
                    "Visit a New Café",
                    "Visit a café you have never been to before.",
                    20,
                    DifficultyLevel.Easy,
                    "Food"),

                QuestData(
                    "Try Gelato at Your Local Gelateria",
                    "Try a gelato flavor you have never ordered before.",
                    15,
                    DifficultyLevel.Easy,
                    "Food"),

                QuestData(
                    "Find the Best Chocolate Ice Cream",
                    "Try chocolate ice cream from two different places and choose your favorite.",
                    40,
                    DifficultyLevel.Medium,
                    "Food"),


                // NIGHTLIFE

                QuestData(
                    "City Lights",
                    "Go outside after dark and explore the city lights.",
                    20,
                    DifficultyLevel.Easy,
                    "Nightlife"),

                QuestData(
                    "Night Photograph",
                    "Take a creative photograph after sunset.",
                    30,
                    DifficultyLevel.Medium,
                    "Nightlife"),

                QuestData(
                    "Stargazer",
                    "Spend some time outside looking at the night sky.",
                    20,
                    DifficultyLevel.Easy,
                    "Nightlife"),

                QuestData(
                    "Late Night Playlist",
                    "Create a playlist specifically for a late-night mood.",
                    15,
                    DifficultyLevel.Easy,
                    "Nightlife"),

                QuestData(
                    "After Dark",
                    "Do something fun after dark that you would not normally do.",
                    40,
                    DifficultyLevel.Medium,
                    "Nightlife"),

                QuestData(
                    "Try a Tequila Shot",
                    "If you are legally allowed to drink, try a tequila shot with friends.",
                    20,
                    DifficultyLevel.Easy,
                    "Nightlife"),

                QuestData(
                    "Go to a Nightclub",
                    "Spend an evening at a nightclub with friends.",
                    50,
                    DifficultyLevel.Medium,
                    "Nightlife"),

                QuestData(
                    "Drink an Aperol",
                    "If you are legally allowed to drink, enjoy an Aperol drink.",
                    15,
                    DifficultyLevel.Easy,
                    "Nightlife"),

                QuestData(
                    "Try a Non-Alcoholic Drink",
                    "Order a non-alcoholic drink you have never tried before.",
                    10,
                    DifficultyLevel.Easy,
                    "Nightlife"),


                // NATURE

                QuestData(
                    "Take a Nature Walk",
                    "Go for a walk somewhere surrounded by nature.",
                    20,
                    DifficultyLevel.Easy,
                    "Nature"),

                QuestData(
                    "Find a Hidden View",
                    "Find a viewpoint or scenic place you have never visited before.",
                    30,
                    DifficultyLevel.Medium,
                    "Nature"),

                QuestData(
                    "Leave No Trace",
                    "Spend time outdoors and leave the area cleaner than you found it.",
                    20,
                    DifficultyLevel.Easy,
                    "Nature"),

                QuestData(
                    "Sunrise Spot",
                    "Find a beautiful place and watch the sunrise.",
                    40,
                    DifficultyLevel.Medium,
                    "Nature"),

                QuestData(
                    "Picnic Outside",
                    "Have a picnic outdoors.",
                    30,
                    DifficultyLevel.Medium,
                    "Nature"),

                QuestData(
                    "Spend the Day at the Lake",
                    "Spend a full day enjoying the outdoors at a lake.",
                    60,
                    DifficultyLevel.Hard,
                    "Nature"),

                QuestData(
                    "Kayak at Matka Canyon",
                    "Spend time kayaking and exploring Matka Canyon.",
                    80,
                    DifficultyLevel.Hard,
                    "Nature")
            };


            
            // INSERT / REPAIR QUESTS
            

            foreach (var data in quests)
            {
                var quest =
                    await context.Quests
                        .FirstOrDefaultAsync(q => q.Title == data.Title);

                if (quest == null)
                {
                    quest = new Quest
                    {
                        Title = data.Title,
                        Description = data.Description,
                        PointsReward = data.PointsReward,
                        Difficulty = data.Difficulty,
                        IsActive = true,
                        CreatedByUserId = admin.Id,
                        CategoryId = categories[data.Category].Id
                    };

                    context.Quests.Add(quest);
                }
                else
                {
                    quest.Description = data.Description;
                    quest.PointsReward = data.PointsReward;
                    quest.Difficulty = data.Difficulty;
                    quest.CategoryId = categories[data.Category].Id;
                    quest.IsActive = true;
                }
            }

            await context.SaveChangesAsync();


            
            // DEMO COMPLETIONS
            

            await SeedDemoCompletionsAsync(
                context,
                users,
                badgeService);


            
            // FINISHED
            

            Console.WriteLine(
                "SideQuest database seed completed successfully.");
        }


        
        // DEMO COMPLETIONS
        

        private static async Task SeedDemoCompletionsAsync(
            ApplicationDbContext context,
            Dictionary<string, ApplicationUser> users,
            BadgeService badgeService)
        {
            var completionPlan = new Dictionary<string, string[]>
            {
                {
                    "alex",
                    new[]
                    {
                        "Hidden Gem",
                        "Go to Your City Center",
                        "Sunset Hunter",
                        "One-Minute Masterpiece",
                        "Album Cover",
                        "10K Steps",
                        "Move It",
                        "Call Someone",
                        "Old Friend",
                        "Cook Something New",
                        "Food Explorer",
                        "Visit a New Café",
                        "Take a Nature Walk",
                        "Find a Hidden View",
                        "Leave No Trace",
                        "Pick a Random Direction",
                        "Room Reset",
                        "Smart Shopper"
                    }
                },

                {
                    "mia",
                    new[]
                    {
                        "One-Minute Masterpiece",
                        "Color Hunt: Red",
                        "Color Hunt: Blue",
                        "Color Hunt: Green",
                        "Color Hunt: Pink",
                        "Album Cover",
                        "Street Photographer",
                        "Paint a Flower",
                        "Write It Down",
                        "Cook Chocolate Chip Cookies",
                        "Visit a New Café",
                        "Digital Detox",
                        "Self-Care Hour",
                        "Call Someone",
                        "Old Friend"
                    }
                },

                {
                    "noah",
                    new[]
                    {
                        "Wikipedia Roulette",
                        "Teach Me Something",
                        "Brain Workout",
                        "Explain Like I'm Five",
                        "Make a Puzzle",
                        "Meditate",
                        "Hidden Gem",
                        "Go to Your City Center",
                        "One-Minute Masterpiece",
                        "Write It Down",
                        "Digital Detox",
                        "Room Reset",
                        "Coffee at Home"
                    }
                },

                {
                    "lena",
                    new[]
                    {
                        "10K Steps",
                        "Morning Mission",
                        "Stairs > Elevator",
                        "Move It",
                        "Try a New Sport",
                        "30-Minute Game",
                        "Play Something You Haven't Played",
                        "No-Spend Day",
                        "Coffee at Home",
                        "Smart Shopper",
                        "Hidden Gem"
                    }
                },

                {
                    "leo",
                    new[]
                    {
                        "Call Someone",
                        "Old Friend",
                        "Squad Quest",
                        "New Connection",
                        "Cook Something New",
                        "Food Explorer",
                        "Visit a New Café",
                        "City Lights",
                        "Late Night Playlist"
                    }
                },

                {
                    "nina",
                    new[]
                    {
                        "City Lights",
                        "Night Photograph",
                        "Stargazer",
                        "Late Night Playlist",
                        "After Dark",
                        "Call Someone",
                        "One-Minute Masterpiece",
                        "Visit a New Café"
                    }
                },

                {
                    "mark",
                    new[]
                    {
                        "Room Reset",
                        "Closet Quest",
                        "Early Bird",
                        "Self-Care Hour",
                        "Morning Mission",
                        "Take a Nature Walk",
                        "Wikipedia Roulette"
                    }
                },

                {
                    "zoe",
                    new[]
                    {
                        "Pick a Random Direction",
                        "Rock Paper Scissors Challenge",
                        "Mystery Challenge",
                        "Left or Right",
                        "Hidden Gem",
                        "One-Minute Masterpiece"
                    }
                },

                {
                    "daniel",
                    new[]
                    {
                        "Try a New Sport",
                        "30-Minute Game",
                        "One-on-One Basketball",
                        "10K Steps",
                        "Take a Nature Walk"
                    }
                },

                {
                    "ella",
                    new[]
                    {
                        "Morning Mission",
                        "Room Reset",
                        "Take a Nature Walk",
                        "Visit a New Café"
                    }
                }
            };


            foreach (var plan in completionPlan)
            {
                if (!users.TryGetValue(plan.Key, out var user))
                    continue;

                var submittedAt =
                    DateTime.UtcNow.AddDays(-plan.Value.Length);

                foreach (var questTitle in plan.Value)
                {
                    var quest =
                        await context.Quests
                            .FirstOrDefaultAsync(q => q.Title == questTitle);

                    if (quest == null)
                        continue;

                    var alreadyExists =
                        await context.QuestCompletions
                            .AnyAsync(qc =>
                                qc.UserId == user.Id &&
                                qc.QuestId == quest.Id &&
                                qc.Status == "Approved");

                    if (alreadyExists)
                        continue;

                    var completion = new QuestCompletion
                    {
                        UserId = user.Id,
                        QuestId = quest.Id,
                        PhotoUrl =
                            $"https://picsum.photos/seed/sidequest-{user.UserName}-{quest.Id}/900/600",
                        Caption = GetDemoCaption(quest.Title),
                        Status = "Approved",
                        SubmittedAt = submittedAt,
                        ReviewedAt = submittedAt.AddHours(4)
                    };

                    context.QuestCompletions.Add(completion);

                    submittedAt = submittedAt.AddDays(1);
                }
            }

            await context.SaveChangesAsync();


            
            // RECALCULATE USER POINTS + LEVELS
            

            foreach (var user in users.Values)
            {
                var approvedCompletions =
                    await context.QuestCompletions
                        .Include(qc => qc.Quest)
                        .Where(qc =>
                            qc.UserId == user.Id &&
                            qc.Status == "Approved")
                        .ToListAsync();

                var points =
                    approvedCompletions
                        .Where(c => c.Quest != null)
                        .Sum(c => c.Quest!.PointsReward);

                user.Points = points;
                user.Level = 1 + (points / 100);
            }

            await context.SaveChangesAsync();


            
            // AWARD BADGES
            

            foreach (var user in users.Values)
            {
                await badgeService.CheckAndAwardBadgesAsync(user.Id);
            }
        }


        
        // DEMO CAPTIONS
        

        private static string GetDemoCaption(string questTitle)
        {
            return questTitle switch
            {
                "Hidden Gem" =>
                    "Found a place I had never noticed before.",

                "Go to Your City Center" =>
                    "Spent some time exploring the city center.",

                "Sunset Hunter" =>
                    "Definitely worth stopping for this sunset.",

                "One-Minute Masterpiece" =>
                    "One minute. No overthinking.",

                "Album Cover" =>
                    "This felt like an album cover moment.",

                "Street Photographer" =>
                    "Found something interesting in an ordinary moment.",

                "10K Steps" =>
                    "Hit the goal and kept going.",

                "Move It" =>
                    "Thirty minutes well spent.",

                "Call Someone" =>
                    "Put the phone down and actually called.",

                "Old Friend" =>
                    "Finally caught up after way too long.",

                "Cook Something New" =>
                    "First attempt, surprisingly successful.",

                "Food Explorer" =>
                    "Tried something completely new today.",

                "Visit a New Café" =>
                    "New place, new favorite spot.",

                "Take a Nature Walk" =>
                    "A quiet walk outside was exactly what I needed.",

                "Find a Hidden View" =>
                    "Found a view I had never seen before.",

                "Leave No Trace" =>
                    "Left the place cleaner than I found it.",

                "Pick a Random Direction" =>
                    "Let the road decide where I ended up.",

                "Room Reset" =>
                    "Everything finally has a place.",

                "Smart Shopper" =>
                    "Compared prices before buying.",

                "Color Hunt: Red" =>
                    "Red was everywhere once I started looking.",

                "Color Hunt: Blue" =>
                    "A surprisingly blue day.",

                "Color Hunt: Green" =>
                    "Found green in all the right places.",

                "Color Hunt: Pink" =>
                    "Pink hunt complete.",

                "Paint a Flower" =>
                    "A small creative break.",

                "Write It Down" =>
                    "Put some thoughts on paper.",

                "Wikipedia Roulette" =>
                    "Started with one random article and ended up learning way too much.",

                "Teach Me Something" =>
                    "Learned it, then passed it on.",

                "Brain Workout" =>
                    "A little brain exercise for today.",

                "Explain Like I'm Five" =>
                    "Turns out simple explanations are harder than they look.",

                "Make a Puzzle" =>
                    "Made one. Now someone else has to solve it.",

                "Meditate" =>
                    "Ten quiet minutes with no distractions.",

                "Morning Mission" =>
                    "Started the day with some movement.",

                "Stairs > Elevator" =>
                    "Took the stairs all day.",

                "Try a New Sport" =>
                    "Definitely not a pro yet, but I tried.",

                "30-Minute Game" =>
                    "Good game and a good workout.",

                "Play Something You Haven't Played" =>
                    "Forgot how fun this one was.",

                "One-on-One Basketball" =>
                    "Competitive enough to count.",

                "Squad Quest" =>
                    "Much better with the squad.",

                "New Connection" =>
                    "A surprisingly good conversation.",

                "Digital Detox" =>
                    "A few hours without constantly checking my phone.",

                "Closet Quest" =>
                    "Finally organized the wardrobe.",

                "Early Bird" =>
                    "Got up early and actually used the morning.",

                "Self-Care Hour" =>
                    "An hour dedicated to doing absolutely nothing unnecessary.",

                "No-Spend Day" =>
                    "Managed to get through the day without unnecessary spending.",

                "Coffee at Home" =>
                    "Made it at home and honestly didn't miss the café.",

                "Mystery Challenge" =>
                    "Had no idea what was coming. That's the point.",

                "Left or Right" =>
                    "No map. Just vibes.",

                "City Lights" =>
                    "The city looks completely different at night.",

                "Night Photograph" =>
                    "Night photography hits differently.",

                "Stargazer" =>
                    "Stayed outside long enough to actually look up.",

                "Late Night Playlist" =>
                    "Built the perfect late-night soundtrack.",

                "After Dark" =>
                    "Definitely outside my usual routine.",

                _ =>
                    "Quest completed."
            };
        }


        
        // QUEST DATA HELPER
        

        private static QuestSeedData QuestData(
            string title,
            string description,
            int points,
            DifficultyLevel difficulty,
            string category)
        {
            return new QuestSeedData
            {
                Title = title,
                Description = description,
                PointsReward = points,
                Difficulty = difficulty,
                Category = category
            };
        }


        
        // BADGE HELPER
        

        private static async Task SeedBadgeAsync(
            ApplicationDbContext context,
            string name,
            string description,
            BadgeCriteriaType criteriaType,
            int requiredCount,
            Category? category = null)
        {
            var badge =
                await context.Badges
                    .FirstOrDefaultAsync(b => b.Name == name);

            if (badge == null)
            {
                badge = new Badge
                {
                    Name = name,
                    Description = description,
                    CriteriaType = criteriaType,
                    RequiredCount = requiredCount,
                    RequiredCategoryId = category?.Id
                };

                context.Badges.Add(badge);
            }
            else
            {
                badge.Description = description;
                badge.CriteriaType = criteriaType;
                badge.RequiredCount = requiredCount;
                badge.RequiredCategoryId = category?.Id;
            }

            await context.SaveChangesAsync();
        }


        
        // CATEGORY DESCRIPTIONS
        

        private static string GetCategoryDescription(string name)
        {
            return name switch
            {
                "Adventure" =>
                    "Explore new places, discover hidden spots and step outside your usual routine.",

                "Creative" =>
                    "Make, draw, photograph, write and create something of your own.",

                "Mind & Learning" =>
                    "Learn something new, challenge your brain and stay curious.",

                "Fitness" =>
                    "Move your body, challenge yourself and build healthier habits.",

                "Sports" =>
                    "Play, compete, practice and try something athletic.",

                "Social" =>
                    "Connect with people, reconnect with friends and create shared memories.",

                "Lifestyle" =>
                    "Improve your routines, environment and everyday life.",

                "Money" =>
                    "Make smarter financial decisions and build better money habits.",

                "Fun & Random" =>
                    "Unexpected challenges designed to make ordinary days less predictable.",

                "Food" =>
                    "Cook, taste, explore and discover new food experiences.",

                "Nightlife" =>
                    "Experience the city, evening atmosphere and after-dark adventures.",

                "Nature" =>
                    "Spend time outside and reconnect with the natural world.",

                _ =>
                    "Complete quests and make everyday life more interesting."
            };
        }


        
        // INTERNAL QUEST DATA CLASS
        

        private class QuestSeedData
        {
            public string Title { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            public int PointsReward { get; set; }

            public DifficultyLevel Difficulty { get; set; }

            public string Category { get; set; } = string.Empty;
        }
    }
}
