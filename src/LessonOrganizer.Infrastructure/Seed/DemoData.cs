using LessonOrganizer.Domain.LessonPlans;
using LessonOrganizer.Domain.Students;

namespace LessonOrganizer.Infrastructure.Seed;

internal sealed record SeedPlan(string Title, LessonPlanStatus Status, int DayOffset, string? Description = null);

internal sealed record SeedStudent(string Name, EnglishLevel Level, string? Email, string? Notes, SeedPlan[] Plans);

internal sealed record SeedTeacher(string Name, string Email, SeedStudent[] Students);

internal static class DemoData
{
    public static readonly SeedTeacher[] Teachers = [Maria(), John()];

    private static SeedTeacher Maria() =>
        new("Maria Silva", "maria@example.com", [AnaSouza(), BrunoLima(), CarlaMendes(), DiegoAlves(), FernandaRocha()]);

    private static SeedTeacher John() =>
        new("John Smith", "john@example.com",
        [
            new("David", EnglishLevel.Elementary, null, null,
            [
                new("Past simple stories", LessonPlanStatus.Delivered, -3),
                new("Ordering at a restaurant", LessonPlanStatus.ToPrepare, 4),
            ]),
            new("Emma", EnglishLevel.UpperIntermediate, null, null, [new("Job interview practice", LessonPlanStatus.Ready, 1)]),
        ]);

    private static SeedStudent AnaSouza() =>
        new("Ana Souza", EnglishLevel.Intermediate, "ana.souza@example.com",
            "Software developer at a fintech, working with a team in Chicago. Goal: speak up in daily stand-ups and " +
            "explain technical decisions without reading notes. Classes on Tuesdays and Thursdays at 7 pm, online.",
        [
            new("Present perfect — conversation", LessonPlanStatus.Delivered, -7,
                Lines(
                    "Warm-up: \"What have you done this week?\" (5 min).",
                    "Present perfect vs. past simple with work examples: deploys, code reviews, incidents.",
                    "Pair role-play: a stand-up update using already, yet and just.",
                    "Homework: write five sentences about this sprint.")),
            new("Phrasal verbs at work", LessonPlanStatus.Ready, 2,
                Lines(
                    "Review the homework from the last class.",
                    "Ten phrasal verbs for meetings: bring up, follow up, put off, sort out, run into, carry on, " +
                    "look into, point out, set up, take over.",
                    "Matching exercise, then use them to describe a real project problem.",
                    "Material: slides and a two-minute video from a tech conference.")),
        ]);

    private static SeedStudent BrunoLima() =>
        new("Bruno Lima", EnglishLevel.Beginner, "bruno.lima@example.com",
            "Restaurant owner who receives tourists in the summer. Needs simple, polite English for the counter and " +
            "the phone. Prefers short lessons with lots of speaking. Saturdays at 9 am.",
        [
            new("Numbers, prices and paying the bill", LessonPlanStatus.Delivered, -4,
                Lines(
                    "Numbers 1–100 with a listening game.",
                    "How much is it? / It's ... / Card or cash?",
                    "Role-play at the counter using the real menu of his restaurant.")),
            new("Greetings and introductions", LessonPlanStatus.ToPrepare, 5,
                Lines(
                    "Greeting customers at different times of the day.",
                    "Introducing himself and the restaurant in three sentences.",
                    "Prepare: flashcards with greetings and a short dialogue to read aloud.")),
        ]);

    private static SeedStudent CarlaMendes() =>
        new("Carla Mendes", EnglishLevel.Advanced, "carla.mendes@example.com",
            "Lawyer preparing for an LL.M. in Toronto. Starts next month: placement interview first, then weekly " +
            "classes on legal writing and academic presentations.",
            []);

    private static SeedStudent DiegoAlves() =>
        new("Diego Alves", EnglishLevel.UpperIntermediate, "diego.alves@example.com",
            "Sales manager who negotiates with suppliers in English every week. Wants more natural phrasing and fewer " +
            "literal translations from Portuguese. Mondays at 6 pm.",
        [
            new("Conditionals in business emails", LessonPlanStatus.Delivered, -10,
                Lines(
                    "First and second conditionals in offers and counter-offers.",
                    "Rewrite three real emails he sent last month (names removed).",
                    "Common mistakes: \"if I would have\", \"in case of\" vs. \"if\".")),
            new("Negotiation phrases for meetings", LessonPlanStatus.Ready, 3,
                Lines(
                    "Phrases to make proposals, push back politely and close a deal.",
                    "Simulation: renegotiating delivery times with a supplier (teacher plays the supplier).",
                    "Feedback on tone and register at the end.")),
        ]);

    private static SeedStudent FernandaRocha() =>
        new("Fernanda Rocha", EnglishLevel.Elementary, "fernanda.rocha@example.com",
            "Nurse planning a trip to Ireland in March and thinking about working abroad later. Shy when speaking; " +
            "responds well to games and visual material. Wednesdays at 8 pm.",
        [
            new("Daily routine and present simple", LessonPlanStatus.ToPrepare, 7,
                Lines(
                    "Vocabulary: daily activities and times of the day.",
                    "Present simple with adverbs of frequency (always, usually, sometimes, never).",
                    "Speaking: describe a typical day at the hospital.",
                    "Prepare: picture cards and a short reading about a nurse in Dublin.")),
            new("At the airport", LessonPlanStatus.ToPrepare, 12,
                Lines(
                    "Check-in, security and boarding vocabulary.",
                    "Listening: airport announcements at a slower speed, then at normal speed.",
                    "Role-play: checking in and asking for help at the gate.")),
        ]);

    private static string Lines(params string[] lines) => string.Join("\n", lines);
}
