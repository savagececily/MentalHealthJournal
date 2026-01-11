namespace MentalHealthJournal.Models
{
    public class CrisisResource
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string TextNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsAvailable24_7 { get; set; }
    }

    public static class CrisisResources
    {
        public static List<CrisisResource> GetDefaultResources()
        {
            return new List<CrisisResource>
            {
                new CrisisResource
                {
                    Name = "988 Suicide & Crisis Lifeline",
                    PhoneNumber = "988",
                    TextNumber = "988",
                    Description = "Free, confidential support 24/7 for people in distress, prevention and crisis resources.",
                    Url = "https://988lifeline.org",
                    IsAvailable24_7 = true
                },
                new CrisisResource
                {
                    Name = "Crisis Text Line",
                    PhoneNumber = "",
                    TextNumber = "741741",
                    Description = "Free, 24/7 support via text. Text HOME to 741741.",
                    Url = "https://www.crisistextline.org",
                    IsAvailable24_7 = true
                },
                new CrisisResource
                {
                    Name = "SAMHSA National Helpline",
                    PhoneNumber = "1-800-662-4357",
                    TextNumber = "",
                    Description = "Free, confidential, 24/7 treatment referral and information service.",
                    Url = "https://www.samhsa.gov/find-help/national-helpline",
                    IsAvailable24_7 = true
                },
                new CrisisResource
                {
                    Name = "Veterans Crisis Line",
                    PhoneNumber = "988 (Press 1)",
                    TextNumber = "838255",
                    Description = "Support for Veterans, service members, National Guard, Reserve, and their families.",
                    Url = "https://www.veteranscrisisline.net",
                    IsAvailable24_7 = true
                },
                new CrisisResource
                {
                    Name = "The Trevor Project (LGBTQ Youth)",
                    PhoneNumber = "1-866-488-7386",
                    TextNumber = "678678",
                    Description = "Crisis support for LGBTQ young people under 25.",
                    Url = "https://www.thetrevorproject.org",
                    IsAvailable24_7 = true
                }
            };
        }
    }
}
